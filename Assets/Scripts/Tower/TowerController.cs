using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Projectile;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Controls tower targeting, aiming, shooting, upgrading and selling.
    ///
    /// Maximum tower level = 3.
    ///
    /// Upgrade:
    /// Level 1 -> Level 2 = base tower cost
    /// Level 2 -> Level 3 = base tower cost * 2
    ///
    /// Sell:
    /// Sell value = 75% of TOTAL money invested into THIS tower.
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        // =========================================================
        // TARGETING MODE
        // =========================================================

        public enum TargetingMode
        {
            First,
            Closest,
            Strongest
        }


        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]

        [Tooltip("The configuration data for this tower.")]
        [SerializeField]
        private TowerData towerData;

        [Tooltip("Optional transform where projectiles are spawned.")]
        [SerializeField]
        private Transform shootPoint;

        [Tooltip("If checked, this tower exists in the level design.")]
        [SerializeField]
        private bool isPreBuilt = false;

        public bool IsPreBuilt => isPreBuilt;

        private GameObject _visualTemplateParent;


        // =========================================================
        // TARGETING
        // =========================================================

        [Header("Targeting Settings")]

        [SerializeField]
        private TargetingMode targetingMode =
            TargetingMode.First;

        [SerializeField]
        private LayerMask enemyLayerMask;

        [Tooltip("How frequently in seconds the target is re-evaluated.")]
        [SerializeField]
        private float targetReevaluateInterval = 0.1f;


        // =========================================================
        // ROTATION
        // =========================================================

        [Header("Rotation")]

        [Tooltip("The visual part of the tower that rotates toward enemies.")]
        [SerializeField]
        private Transform rotatingVisual;

        [Tooltip("Should the tower rotate towards the target?")]
        [SerializeField]
        private bool rotateTowardsTarget = true;

        [SerializeField]
        private float rotationSpeed = 10f;

        [SerializeField]
        private float spriteAngleOffset = 0f;


        // =========================================================
        // RUNTIME STATE
        // =========================================================

        private EnemyHealth _targetEnemy;

        private float _fireCooldownTimer = 0f;

        private float _targetReevaluateTimer = 0f;


        // =========================================================
        // LEVEL SYSTEM
        // =========================================================

        private int _currentLevel = 1;

        private const int MAX_LEVEL = 3;


        [Header("Upgrade Prefabs")]

        [SerializeField]
        private GameObject level2Prefab;

        [SerializeField]
        private GameObject level3Prefab;


        // =========================================================
        // SELL SETTINGS
        // =========================================================

        [Header("Sell Settings")]

        [Tooltip("Percentage of total invested money returned when selling.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float sellReturnPercent = 0.75f;


        // =========================================================
        // PUBLIC PROPERTIES
        // =========================================================

        public int CurrentLevel
        {
            get
            {
                return _currentLevel;
            }
        }

        public int MaxLevel
        {
            get
            {
                return MAX_LEVEL;
            }
        }

        public TowerData TowerData
        {
            get
            {
                return towerData;
            }
        }

        public EnemyHealth TargetEnemy
        {
            get
            {
                return _targetEnemy;
            }
        }


        // =========================================================
        // BASE COST
        // =========================================================

        /// <summary>
        /// Original purchase price of THIS tower.
        ///
        /// Example:
        /// Basic = 100
        /// Fast = 130
        /// Ice = 150
        /// </summary>
        public int BaseCost
        {
            get
            {
                if (towerData == null)
                    return 0;

                return towerData.Cost;
            }
        }


        // =========================================================
        // UPGRADE COST
        // =========================================================

        /// <summary>
        /// Cost of the NEXT upgrade for THIS tower.
        ///
        /// Level 1 -> Level 2 = BaseCost
        /// Level 2 -> Level 3 = BaseCost * 2
        /// Level 3 = 0
        ///
        /// Examples:
        ///
        /// Basic 100:
        /// Lv1 -> Lv2 = 100
        /// Lv2 -> Lv3 = 200
        ///
        /// Fast 130:
        /// Lv1 -> Lv2 = 130
        /// Lv2 -> Lv3 = 260
        ///
        /// Ice 150:
        /// Lv1 -> Lv2 = 150
        /// Lv2 -> Lv3 = 300
        /// </summary>
        public int UpgradeCost
        {
            get
            {
                if (towerData == null)
                    return 0;

                if (_currentLevel >= MAX_LEVEL)
                    return 0;

                return towerData.Cost * _currentLevel;
            }
        }


        // =========================================================
        // TOTAL INVESTED COST
        // =========================================================

        /// <summary>
        /// Total amount of gold invested into THIS tower.
        ///
        /// Level 1:
        /// Base cost
        ///
        /// Level 2:
        /// Base cost + Level 1 upgrade
        ///
        /// Level 3:
        /// Base cost + Level 1 upgrade + Level 2 upgrade
        ///
        /// Example Ice = 150:
        ///
        /// Lv1 = 150
        /// Lv2 = 150 + 150 = 300
        /// Lv3 = 150 + 150 + 300 = 600
        /// </summary>
        public int TotalInvestedCost
        {
            get
            {
                if (towerData == null)
                    return 0;

                int total = towerData.Cost;

                for (int level = 1;
                     level < _currentLevel;
                     level++)
                {
                    total += towerData.Cost * level;
                }

                return total;
            }
        }


        // =========================================================
        // SELL VALUE
        // =========================================================

        /// <summary>
        /// Amount of gold returned when selling THIS tower.
        ///
        /// Always calculated from this tower's own TowerData
        /// and its own current level.
        ///
        /// 75% of total invested cost.
        ///
        /// Example:
        ///
        /// Basic 100:
        /// Lv1 = 75
        /// Lv2 = 187/188 depending on rounding
        /// Lv3 = 300
        ///
        /// Fast 130:
        /// Lv1 = 98
        /// Lv2 = 244/245 depending on rounding
        /// Lv3 = 390
        ///
        /// Ice 150:
        /// Lv1 = 113
        /// Lv2 = 225
        /// Lv3 = 450
        /// </summary>
        public int SellValue
        {
            get
            {
                if (towerData == null)
                    return 0;

                return Mathf.RoundToInt(
                    TotalInvestedCost *
                    sellReturnPercent
                );
            }
        }


        // =========================================================
        // CURRENT DAMAGE
        // =========================================================

        public int CurrentDamage
        {
            get
            {
                if (towerData == null)
                    return 0;

                return towerData.Damage +
                       (_currentLevel - 1) * 2;
            }
        }


        // =========================================================
        // CURRENT RANGE
        // =========================================================

        public float CurrentRange
        {
            get
            {
                if (towerData == null)
                    return 0f;

                return towerData.Range +
                       (_currentLevel - 1) * 1.0f;
            }
        }


        // =========================================================
        // UNITY - AWAKE
        // =========================================================

        private void Awake()
        {
            EnsureVisualTemplates();
        }


        // =========================================================
        // UNITY - START
        // =========================================================

        private void Start()
        {
            if (shootPoint == null)
            {
                shootPoint = transform;
            }

            Debug.Log(
                $"[TowerController Start] " +
                $"{gameObject.name} initialized. " +
                $"TowerData: " +
                $"{(towerData != null ? towerData.name : "NULL")}, " +
                $"BaseCost: {BaseCost}, " +
                $"UpgradeCost: {UpgradeCost}, " +
                $"SellValue: {SellValue}, " +
                $"Level: {_currentLevel}"
            );
        }


        // =========================================================
        // VISUAL TEMPLATE
        // =========================================================

        private void EnsureVisualTemplates()
        {
            if (_visualTemplateParent != null)
                return;

            _visualTemplateParent =
                new GameObject("TemplatesContainer");

            _visualTemplateParent.transform.SetParent(
                transform
            );

            _visualTemplateParent.SetActive(false);

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Visual_"))
                    continue;

                GameObject template =
                    Instantiate(
                        child.gameObject,
                        _visualTemplateParent.transform
                    );

                template.name = child.name;

                template.transform.localPosition =
                    child.localPosition;

                template.transform.localRotation =
                    child.localRotation;

                template.transform.localScale =
                    child.localScale;
            }
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing)
            {
                return;
            }

            if (towerData == null)
                return;

            // -----------------------------------------------------
            // TARGET RE-EVALUATION
            // -----------------------------------------------------

            _targetReevaluateTimer -= Time.deltaTime;

            if (_targetReevaluateTimer <= 0f)
            {
                UpdateTarget();

                _targetReevaluateTimer =
                    targetReevaluateInterval;
            }


            // -----------------------------------------------------
            // VALIDATE TARGET
            // -----------------------------------------------------

            if (!IsTargetValid(_targetEnemy))
            {
                _targetEnemy = null;
            }


            // -----------------------------------------------------
            // AIM + SHOOT
            // -----------------------------------------------------

            if (_targetEnemy != null)
            {
                if (rotateTowardsTarget)
                {
                    AimAtTarget(
                        _targetEnemy.transform.position
                    );
                }

                _fireCooldownTimer -= Time.deltaTime;

                if (_fireCooldownTimer <= 0f)
                {
                    Shoot();

                    if (towerData.FireRate > 0f)
                    {
                        _fireCooldownTimer =
                            1f / towerData.FireRate;
                    }
                }
            }
            else
            {
                _fireCooldownTimer =
                    Mathf.Max(
                        0f,
                        _fireCooldownTimer -
                        Time.deltaTime
                    );
            }
        }


        // =========================================================
        // TARGETING
        // =========================================================

        private void UpdateTarget()
        {
            Collider2D[] colliders =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    CurrentRange,
                    enemyLayerMask
                );

            if (colliders.Length == 0)
            {
                _targetEnemy = null;
                return;
            }

            EnemyHealth bestTarget = null;

            float bestMetric = float.MinValue;

            foreach (Collider2D col in colliders)
            {
                if (col == null)
                    continue;

                EnemyHealth enemy =
                    col.GetComponent<EnemyHealth>();

                if (enemy == null ||
                    enemy.IsDead)
                {
                    continue;
                }

                float dist =
                    Vector2.Distance(
                        transform.position,
                        enemy.transform.position
                    );

                if (dist > CurrentRange)
                    continue;

                float metric = 0f;

                EnemyMovement movement =
                    col.GetComponent<EnemyMovement>();

                switch (targetingMode)
                {
                    case TargetingMode.First:

                        if (movement != null &&
                            movement.ActivePath != null)
                        {
                            int wpIndex =
                                movement.CurrentWaypointIndex;

                            Transform targetWp =
                                movement.ActivePath
                                    .GetWaypoint(wpIndex);

                            float distToWp =
                                targetWp != null
                                    ? Vector2.Distance(
                                        movement.transform.position,
                                        targetWp.position
                                    )
                                    : 0f;

                            metric =
                                (wpIndex * 1000f) -
                                distToWp;
                        }
                        else
                        {
                            metric =
                                -Vector2.Distance(
                                    transform.position,
                                    col.transform.position
                                );
                        }

                        break;


                    case TargetingMode.Closest:

                        metric =
                            -Vector2.Distance(
                                transform.position,
                                col.transform.position
                            );

                        break;


                    case TargetingMode.Strongest:

                        metric =
                            enemy.CurrentHealth;

                        break;
                }

                if (metric > bestMetric)
                {
                    bestMetric = metric;
                    bestTarget = enemy;
                }
            }

            _targetEnemy = bestTarget;
        }


        // =========================================================
        // TARGET VALIDATION
        // =========================================================

        private bool IsTargetValid(
            EnemyHealth enemy)
        {
            if (enemy == null)
                return false;

            if (enemy.IsDead)
                return false;

            if (!enemy.gameObject.activeSelf)
                return false;

            float dist =
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position
                );

            return dist <= CurrentRange;
        }


        // =========================================================
        // AIM
        // =========================================================

        private void AimAtTarget(Vector3 targetPosition)
        {
            if (rotatingVisual == null)
                return;

            Vector3 direction = targetPosition - rotatingVisual.position;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            float targetAngle =
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Quaternion targetRotation =
                Quaternion.AngleAxis(
                    targetAngle + spriteAngleOffset,
                    Vector3.forward
                );

            rotatingVisual.rotation = Quaternion.Slerp(
                rotatingVisual.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }


        // =========================================================
        // SHOOT
        // =========================================================

        private void Shoot()
        {
            if (towerData.ProjectilePrefab == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"{gameObject.name} " +
                    "is missing a Projectile Prefab configuration!"
                );

                return;
            }

            if (_targetEnemy == null ||
                _targetEnemy.IsDead)
            {
                return;
            }

            GameObject projectileObj =
                ObjectPooler.Instance.GetPooledObject(
                    towerData.ProjectilePrefab,
                    shootPoint.position,
                    shootPoint.rotation
                );

            if (projectileObj == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"Failed to get projectile " +
                    $"for {gameObject.name}"
                );

                return;
            }

            ProjectileController projectile =
                projectileObj.GetComponent<
                    ProjectileController
                >();

            if (projectile == null)
            {
                Debug.LogWarning(
                    $"[TowerController] Spawned projectile " +
                    $"{projectileObj.name} does not have " +
                    "a ProjectileController attached."
                );

                return;
            }

            bool isIceTower =
                towerData.Type ==
                TowerType.Ice;

            projectile.Initialize(
                _targetEnemy,
                CurrentDamage,
                towerData.ProjectileSpeed,
                isIceTower,
                towerData.SlowPercent,
                towerData.SlowDuration
            );
        }


        // =========================================================
        // LEVEL UP
        // =========================================================

        public void LevelUp()
        {
            // -----------------------------------------------------
            // MAX LEVEL CHECK
            // -----------------------------------------------------

            if (_currentLevel >= MAX_LEVEL)
            {
                Debug.Log(
                    $"[TowerController] " +
                    $"{gameObject.name} is already MAX LEVEL."
                );

                return;
            }


            // -----------------------------------------------------
            // DATA CHECK
            // -----------------------------------------------------

            if (towerData == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"{gameObject.name} cannot upgrade because " +
                    "TowerData is NULL."
                );

                return;
            }


            int oldLevel =
                _currentLevel;


            // -----------------------------------------------------
            // UPGRADE
            // -----------------------------------------------------

            _currentLevel++;


            // -----------------------------------------------------
            // REMOVE OLD VISUALS
            // -----------------------------------------------------

            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Visual_"))
                {
                    Destroy(child.gameObject);
                }
            }


            // -----------------------------------------------------
            // GET NEW PREFAB
            // -----------------------------------------------------

            GameObject prefabToUse = null;

            if (_currentLevel == 2)
            {
                prefabToUse =
                    level2Prefab;
            }
            else if (_currentLevel == 3)
            {
                prefabToUse =
                    level3Prefab;
            }


            // -----------------------------------------------------
            // APPLY NEW VISUAL
            // -----------------------------------------------------

            if (prefabToUse != null)
            {
                foreach (Transform child in prefabToUse.transform)
                {
                    if (!child.name.StartsWith("Visual_"))
                        continue;

                    GameObject instantiated =
                        Instantiate(
                            child.gameObject,
                            transform
                        );

                    instantiated.name =
                        child.name;
                }
            }


            // -----------------------------------------------------
            // DEBUG
            // -----------------------------------------------------

            Debug.Log(
                $"[TowerController] " +
                $"{gameObject.name} upgraded " +
                $"Level {oldLevel} -> {_currentLevel}. " +
                $"Base Cost: {BaseCost} G, " +
                $"Upgrade Cost: {UpgradeCost} G, " +
                $"Total Invested: {TotalInvestedCost} G, " +
                $"Sell Value: {SellValue} G, " +
                $"Damage: {CurrentDamage}, " +
                $"Range: {CurrentRange}"
            );
        }


        // =========================================================
        // RESET
        // =========================================================

        public void ResetTowerState()
        {
            EnsureVisualTemplates();

            if (_currentLevel > 1)
            {
                _currentLevel = 1;

                RestoreInitialVisuals();
            }

            _fireCooldownTimer = 0f;

            _targetReevaluateTimer = 0f;

            _targetEnemy = null;

            Debug.Log(
                $"[TowerController] " +
                $"{gameObject.name} reset to Level 1. " +
                $"BaseCost: {BaseCost}, " +
                $"SellValue: {SellValue}"
            );
        }


        // =========================================================
        // RESTORE VISUALS
        // =========================================================

        private void RestoreInitialVisuals()
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Visual_"))
                {
                    Destroy(child.gameObject);
                }
            }

            if (_visualTemplateParent == null)
                return;

            foreach (
                Transform templateChild
                in _visualTemplateParent.transform)
            {
                GameObject restoredVisual =
                    Instantiate(
                        templateChild.gameObject,
                        transform
                    );

                restoredVisual.name =
                    templateChild.name;

                restoredVisual.transform.localPosition =
                    templateChild.localPosition;

                restoredVisual.transform.localRotation =
                    templateChild.localRotation;

                restoredVisual.transform.localScale =
                    templateChild.localScale;

                restoredVisual.SetActive(true);
            }
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            float range =
                towerData != null
                    ? CurrentRange
                    : 5f;

            Gizmos.color =
                new Color(
                    0f,
                    1f,
                    0f,
                    0.15f
                );

            Gizmos.DrawSphere(
                transform.position,
                range
            );

            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                transform.position,
                range
            );
        }
    }
}