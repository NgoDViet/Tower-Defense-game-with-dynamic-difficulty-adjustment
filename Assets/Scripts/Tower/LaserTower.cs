using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;

namespace TowerDefense.Tower
{
    public class LaserTower : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TowerData towerData;

        [Tooltip("Point where the laser starts.")]
        [SerializeField] private Transform firePoint;

        [Tooltip("Laser LineRenderer.")]
        [SerializeField] private LineRenderer laserRenderer;

        [Header("Targeting")]
        [SerializeField] private bool targetClosestEnemy = true;
        [SerializeField] private LayerMask enemyLayerMask;

        [Header("Laser Settings")]
        [SerializeField] private float laserWidth = 0.15f;
        [SerializeField] private Color laserColor = Color.red;

        [Header("Upgrade")]
        [SerializeField] private GameObject level2Prefab;
        [SerializeField] private GameObject level3Prefab;

        private EnemyHealth currentTarget;

        private float damageTimer;

        private int currentLevel = 1;

        private const int MAX_LEVEL = 3;

        public int CurrentLevel => currentLevel;
        public int MaxLevel => MAX_LEVEL;

        public int UpgradeCost => 100 * currentLevel;

        public int CurrentDamage =>
            towerData != null
                ? towerData.Damage + (currentLevel - 1) * 2
                : 0;

        public float CurrentRange =>
            towerData != null
                ? towerData.Range + (currentLevel - 1) * 1f
                : 0f;

        public TowerData TowerData => towerData;

        private void Awake()
        {
            SetupLaserRenderer();
        }

        private void Start()
        {
            if (towerData == null)
            {
                Debug.LogError(
                    $"[LaserTower] {name}: TowerData is missing!"
                );

                enabled = false;
                return;
            }

            if (towerData.Type != TowerType.Laser)
            {
                Debug.LogWarning(
                    $"[LaserTower] {name}: TowerData is not Laser type!"
                );
            }

            damageTimer = 0f;

            Debug.Log(
                $"[LaserTower] {name} started | " +
                $"Damage = {CurrentDamage} | " +
                $"Range = {CurrentRange}"
            );
        }

        // =========================================================
        // SETUP LASER
        // =========================================================

        private void SetupLaserRenderer()
        {
            if (laserRenderer == null)
            {
                Debug.LogError(
                    $"[LaserTower] {name}: Laser Renderer is NOT assigned!"
                );

                return;
            }

            laserRenderer.enabled = false;

            // Quan trọng
            laserRenderer.useWorldSpace = true;

            laserRenderer.positionCount = 2;

            laserRenderer.startWidth = laserWidth;
            laserRenderer.endWidth = laserWidth;

            laserRenderer.startColor = laserColor;
            laserRenderer.endColor = laserColor;

            // Cho laser nằm phía trên map
            laserRenderer.sortingLayerName = "Default";
            laserRenderer.sortingOrder = 1000;

            // Đảm bảo material tồn tại
            if (laserRenderer.sharedMaterial == null)
            {
                Debug.LogWarning(
                    $"[LaserTower] {name}: Laser Renderer has no Material!"
                );
            }

            Vector3 start = transform.position;
            start.z = -0.1f;

            laserRenderer.SetPosition(0, start);
            laserRenderer.SetPosition(1, start);

            Debug.Log(
                $"[LaserTower] Renderer setup OK | " +
                $"Width = {laserWidth}"
            );
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (towerData == null)
                return;

            // Nếu GameManager tồn tại thì chỉ hoạt động khi Playing
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                HideLaser();
                return;
            }

            // Tìm mục tiêu mới nếu chưa có mục tiêu
            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindTarget();
            }

            // Không có enemy
            if (currentTarget == null)
            {
                HideLaser();
                return;
            }

            // Có enemy -> vẽ laser
            UpdateLaser();

            // Damage theo thời gian
            damageTimer -= Time.deltaTime;

            if (damageTimer <= 0f)
            {
                DealDamage();

                damageTimer = Mathf.Max(
                    towerData.LaserDamageInterval,
                    0.01f
                );
            }
        }

        // =========================================================
        // FIND TARGET
        // =========================================================

        private EnemyHealth FindTarget()
        {
            EnemyHealth[] enemies =
                FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None
                );

            EnemyHealth bestTarget = null;

            float bestDistance = float.MaxValue;

            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null)
                    continue;

                if (!enemy.gameObject.activeInHierarchy)
                    continue;

                if (enemy.IsDead)
                    continue;

                // Kiểm tra Layer nếu LayerMask được thiết lập
                if (enemyLayerMask.value != 0)
                {
                    int enemyLayer = enemy.gameObject.layer;

                    if ((enemyLayerMask.value & (1 << enemyLayer)) == 0)
                        continue;
                }

                float distance =
                    Vector3.Distance(
                        transform.position,
                        enemy.transform.position
                    );

                if (distance > CurrentRange)
                    continue;

                if (targetClosestEnemy)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = enemy;
                    }
                }
                else
                {
                    bestTarget = enemy;
                    break;
                }
            }

            if (bestTarget != null)
            {
                Debug.Log(
                    $"[LaserTower] {name} -> Target: {bestTarget.name} | " +
                    $"Distance = {bestDistance:F2}"
                );
            }

            return bestTarget;
        }

        // =========================================================
        // VALIDATE TARGET
        // =========================================================

        private bool IsTargetValid(EnemyHealth target)
        {
            if (target == null)
                return false;

            if (!target.gameObject.activeInHierarchy)
                return false;

            if (target.IsDead)
                return false;

            float distance =
                Vector3.Distance(
                    transform.position,
                    target.transform.position
                );

            return distance <= CurrentRange;
        }

        // =========================================================
        // DRAW LASER
        // =========================================================

        private void UpdateLaser()
        {
            if (laserRenderer == null)
                return;

            if (currentTarget == null)
                return;

            Vector3 startPosition;

            if (firePoint != null)
            {
                startPosition = firePoint.position;
            }
            else
            {
                startPosition = transform.position;
            }

            Vector3 endPosition =
                currentTarget.transform.position;

            // Ép laser lên phía trước map
            startPosition.z = -0.1f;
            endPosition.z = -0.1f;

            laserRenderer.positionCount = 2;

            laserRenderer.SetPosition(
                0,
                startPosition
            );

            laserRenderer.SetPosition(
                1,
                endPosition
            );

            // Đảm bảo màu + độ dày
            laserRenderer.startWidth = laserWidth;
            laserRenderer.endWidth = laserWidth;

            laserRenderer.startColor = laserColor;
            laserRenderer.endColor = laserColor;

            laserRenderer.sortingOrder = 1000;

            laserRenderer.enabled = true;

            Debug.DrawLine(
                startPosition,
                endPosition,
                Color.red
            );
        }

        // =========================================================
        // HIDE LASER
        // =========================================================

        private void HideLaser()
        {
            if (laserRenderer != null)
            {
                laserRenderer.enabled = false;
            }
        }

        // =========================================================
        // DAMAGE
        // =========================================================

        private void DealDamage()
        {
            if (currentTarget == null)
                return;

            if (currentTarget.IsDead)
                return;

            currentTarget.TakeDamage(CurrentDamage);
        }

        // =========================================================
        // UPGRADE
        // =========================================================

        public void LevelUp()
        {
            if (currentLevel >= MAX_LEVEL)
            {
                Debug.Log(
                    $"[LaserTower] {name} is already MAX LEVEL."
                );

                return;
            }

            currentLevel++;

            Debug.Log(
                $"[LaserTower] {name} upgraded to LEVEL {currentLevel} | " +
                $"Damage = {CurrentDamage} | " +
                $"Range = {CurrentRange}"
            );
        }

        // =========================================================
        // RESET
        // =========================================================

        public void ResetTowerState()
        {
            currentLevel = 1;

            damageTimer = 0f;

            currentTarget = null;

            HideLaser();

            Debug.Log(
                $"[LaserTower] {name} reset to Level 1."
            );
        }

        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            if (towerData == null)
                return;

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                transform.position,
                CurrentRange
            );
        }
    }
}