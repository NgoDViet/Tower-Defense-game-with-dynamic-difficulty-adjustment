using UnityEngine;

using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Projectile;

namespace TowerDefense.Tower
{
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

        [Tooltip("TowerData configuration for this tower.")]
        [SerializeField]
        private TowerData towerData;

        [Tooltip("Optional point where projectile is spawned.")]
        [SerializeField]
        private Transform shootPoint;

        [Tooltip("If enabled, this tower already exists in the level.")]
        [SerializeField]
        private bool isPreBuilt = false;


        // =========================================================
        // TARGETING
        // =========================================================

        [Header("Targeting Settings")]

        [SerializeField]
        private TargetingMode targetingMode =
            TargetingMode.First;

        [SerializeField]
        private LayerMask enemyLayerMask;

        [Tooltip("How often the tower searches for a target.")]
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
        // LEVEL SYSTEM
        // =========================================================

        [Header("Upgrade Settings")]

        [SerializeField]
        private GameObject level2Prefab;

        [SerializeField]
        private GameObject level3Prefab;

        private int _currentLevel = 1;

        private const int MAX_LEVEL = 3;


        // =========================================================
        // SELL SETTINGS
        // =========================================================

        [Header("Sell Settings")]

        [Tooltip("Percentage returned when selling the tower.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float sellReturnPercent = 0.75f;


        // =========================================================
        // RUNTIME STATE
        // =========================================================

        private EnemyHealth _targetEnemy;

        private float _fireCooldownTimer;

        private float _targetReevaluateTimer;

        private float _goldTimer;

        private GameObject _visualTemplateParent;


        // =========================================================
        // PUBLIC PROPERTIES
        // =========================================================

        public bool IsPreBuilt
        {
            get
            {
                return isPreBuilt;
            }
        }


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
        // TOTAL INVESTED
        // =========================================================

        public int TotalInvestedCost
        {
            get
            {
                if (towerData == null)
                    return 0;

                int total =
                    towerData.Cost;

                for (
                    int level = 1;
                    level < _currentLevel;
                    level++
                )
                {
                    total +=
                        towerData.Cost *
                        level;
                }

                return total;
            }
        }


        // =========================================================
        // SELL VALUE
        // =========================================================

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

                return
                    towerData.Damage +
                    ((_currentLevel - 1) * 2);
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

                return
                    towerData.Range +
                    ((_currentLevel - 1) * 1f);
            }
        }


        // =========================================================
        // CURRENT FIRE RATE
        // =========================================================

        public float CurrentFireRate
        {
            get
            {
                if (towerData == null)
                    return 0f;

                return towerData.FireRate;
            }
        }


        // =========================================================
        // GOLD TOWER
        // =========================================================

        public bool IsGoldTower
        {
            get
            {
                return
                    towerData != null &&
                    towerData.Type == TowerType.Gold;
            }
        }


        public int CurrentGoldPerTick
        {
            get
            {
                if (!IsGoldTower)
                    return 0;

                return
                    towerData.GetGoldPerTick(
                        _currentLevel
                    );
            }
        }


        public float GoldInterval
        {
            get
            {
                if (!IsGoldTower)
                    return 999f;

                return
                    towerData.GetGoldInterval(
                        _currentLevel
                    );
            }
        }


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            EnsureVisualTemplates();

            if (shootPoint == null)
            {
                shootPoint = transform;
            }

            if (towerData != null)
            {
                _goldTimer =
                    GoldInterval;
            }
        }


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            if (shootPoint == null)
            {
                shootPoint = transform;
            }


            if (towerData == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"{gameObject.name}: TowerData is NULL."
                );

                enabled = false;

                return;
            }


            if (IsGoldTower)
            {
                _goldTimer =
                    GoldInterval;

                Debug.Log(
                    $"[GoldTower] READY | " +
                    $"Tower={gameObject.name} | " +
                    $"Level={CurrentLevel} | " +
                    $"Gold=+{CurrentGoldPerTick} | " +
                    $"Interval={GoldInterval}s"
                );

                return;
            }


            Debug.Log(
                $"[TowerController] READY | " +
                $"Tower={gameObject.name} | " +
                $"Type={towerData.Type} | " +
                $"Damage={CurrentDamage} | " +
                $"Range={CurrentRange:F1} | " +
                $"FireRate={towerData.FireRate:F2} | " +
                $"Projectile=" +
                (
                    towerData.ProjectilePrefab != null
                        ? towerData.ProjectilePrefab.name
                        : "NULL"
                )
            );
        }


        // =========================================================
        // RUNTIME DATA INITIALIZATION
        // =========================================================

        public void InitializeTowerData(
            TowerData data)
        {
            if (data == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"{gameObject.name}: " +
                    "Cannot initialize with NULL TowerData."
                );

                return;
            }


            towerData =
                data;


            if (shootPoint == null)
            {
                shootPoint =
                    transform;
            }


            _targetEnemy =
                null;

            _fireCooldownTimer =
                0f;

            _targetReevaluateTimer =
                0f;


            if (data.Type == TowerType.Gold)
            {
                _goldTimer =
                    data.GetGoldInterval(
                        _currentLevel
                    );
            }


            Debug.Log(
                $"[TowerController] " +
                $"Runtime initialized | " +
                $"Tower={gameObject.name} | " +
                $"Type={data.Type} | " +
                $"Data={data.name}"
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
                new GameObject(
                    "TemplatesContainer"
                );


            _visualTemplateParent.transform.SetParent(
                transform
            );


            _visualTemplateParent.transform.localPosition =
                Vector3.zero;


            _visualTemplateParent.transform.localRotation =
                Quaternion.identity;


            _visualTemplateParent.transform.localScale =
                Vector3.one;


            _visualTemplateParent.SetActive(
                false
            );


            foreach (Transform child in transform)
            {
                if (
                    child == null ||
                    !child.name.StartsWith(
                        "Visual_"
                    )
                )
                {
                    continue;
                }


                GameObject template =
                    Instantiate(
                        child.gameObject,
                        _visualTemplateParent.transform
                    );


                template.name =
                    child.name;


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
            // -----------------------------------------------------
            // GAME STATE
            // -----------------------------------------------------

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing
            )
            {
                return;
            }


            // -----------------------------------------------------
            // DATA
            // -----------------------------------------------------

            if (towerData == null)
                return;


            // -----------------------------------------------------
            // GOLD
            // -----------------------------------------------------

            if (IsGoldTower)
            {
                UpdateGoldTower();

                return;
            }


            // -----------------------------------------------------
            // LASER
            // -----------------------------------------------------

            if (
                towerData.Type ==
                TowerType.Laser
            )
            {
                return;
            }


            // -----------------------------------------------------
            // TARGET RE-EVALUATION
            // -----------------------------------------------------

            _targetReevaluateTimer -=
                Time.deltaTime;


            if (
                _targetReevaluateTimer <=
                0f
            )
            {
                UpdateTarget();


                _targetReevaluateTimer =
                    Mathf.Max(
                        0.01f,
                        targetReevaluateInterval
                    );
            }


            // -----------------------------------------------------
            // TARGET VALIDATION
            // -----------------------------------------------------

            if (
                !IsTargetValid(
                    _targetEnemy
                )
            )
            {
                _targetEnemy =
                    null;
            }


            // -----------------------------------------------------
            // NO TARGET
            // -----------------------------------------------------

            if (_targetEnemy == null)
            {
                _fireCooldownTimer =
                    Mathf.Max(
                        0f,
                        _fireCooldownTimer -
                        Time.deltaTime
                    );

                return;
            }


            // -----------------------------------------------------
            // AIM
            // -----------------------------------------------------

            if (rotateTowardsTarget)
            {
                AimAtTarget(
                    _targetEnemy.transform.position
                );
            }


            // -----------------------------------------------------
            // FIRE TIMER
            // -----------------------------------------------------

            _fireCooldownTimer -=
                Time.deltaTime;


            if (_fireCooldownTimer <= 0f)
            {
                Shoot();


                if (towerData.FireRate > 0f)
                {
                    _fireCooldownTimer =
                        1f /
                        towerData.FireRate;
                }
                else
                {
                    _fireCooldownTimer =
                        999f;
                }
            }
        }


        // =========================================================
        // GOLD UPDATE
        // =========================================================

        private void UpdateGoldTower()
        {
            if (GameManager.Instance == null)
                return;


            _goldTimer -=
                Time.deltaTime;


            if (_goldTimer > 0f)
                return;


            int goldAmount =
                CurrentGoldPerTick;


            if (goldAmount > 0)
            {
                GameManager.Instance.AddGold(
                    goldAmount
                );


                Debug.Log(
                    $"[GoldTower] " +
                    $"{gameObject.name} +" +
                    $"{goldAmount} G | " +
                    $"Level={CurrentLevel} | " +
                    $"Interval={GoldInterval}s"
                );
            }


            _goldTimer =
                GoldInterval;
        }


        // =========================================================
        // FIND TARGET
        // =========================================================

        private void UpdateTarget()
        {
            EnemyHealth[] enemies =
                FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None
                );


            EnemyHealth bestTarget =
                null;


            float bestMetric =
                float.MinValue;


            foreach (
                EnemyHealth enemy
                in enemies
            )
            {
                if (enemy == null)
                    continue;


                if (enemy.IsDead)
                    continue;


                if (!enemy.gameObject.activeSelf)
                    continue;


                float distance =
                    Vector2.Distance(
                        transform.position,
                        enemy.transform.position
                    );


                if (
                    distance >
                    CurrentRange
                )
                {
                    continue;
                }


                float metric =
                    0f;


                EnemyMovement movement =
                    enemy.GetComponent<
                        EnemyMovement
                    >();


                switch (targetingMode)
                {
                    case TargetingMode.First:

                        if (
                            movement != null &&
                            movement.ActivePath != null
                        )
                        {
                            int index =
                                movement.CurrentWaypointIndex;


                            Transform waypoint =
                                movement.ActivePath.GetWaypoint(
                                    index
                                );


                            float distanceToWaypoint =
                                waypoint != null
                                    ? Vector2.Distance(
                                        enemy.transform.position,
                                        waypoint.position
                                    )
                                    : 0f;


                            metric =
                                index *
                                1000f -
                                distanceToWaypoint;
                        }
                        else
                        {
                            metric =
                                -distance;
                        }

                        break;


                    case TargetingMode.Closest:

                        metric =
                            -distance;

                        break;


                    case TargetingMode.Strongest:

                        metric =
                            enemy.CurrentHealth;

                        break;
                }


                if (
                    bestTarget == null ||
                    metric > bestMetric
                )
                {
                    bestTarget =
                        enemy;


                    bestMetric =
                        metric;
                }
            }


            _targetEnemy =
                bestTarget;
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


            float distance =
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position
                );


            return distance <= CurrentRange;
        }


        // =========================================================
        // AIM
        // =========================================================

        private void AimAtTarget(
            Vector3 targetPosition)
        {
            if (rotatingVisual == null)
                return;


            Vector3 direction =
                targetPosition -
                rotatingVisual.position;


            if (
                direction.sqrMagnitude <=
                0.001f
            )
            {
                return;
            }


            float targetAngle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) *
                Mathf.Rad2Deg;


            Quaternion targetRotation =
                Quaternion.AngleAxis(
                    targetAngle +
                    spriteAngleOffset,
                    Vector3.forward
                );


            rotatingVisual.rotation =
                Quaternion.Slerp(
                    rotatingVisual.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }


        // =========================================================
        // SHOOT
        // =========================================================

        private void Shoot()
        {
            if (towerData == null)
                return;


            if (
                towerData.Type ==
                TowerType.Gold
            )
            {
                return;
            }


            if (
                towerData.Type ==
                TowerType.Laser
            )
            {
                return;
            }


            if (_targetEnemy == null)
                return;


            if (_targetEnemy.IsDead)
                return;


            if (
                !_targetEnemy.gameObject.activeSelf
            )
            {
                return;
            }


            if (shootPoint == null)
            {
                shootPoint =
                    transform;
            }


            // =====================================================
            // CANNON
            // =====================================================

            if (
                towerData.Type ==
                TowerType.Cannon
            )
            {
                ShootCannon();

                return;
            }


            // =====================================================
            // NORMAL / ICE / FAST / ARCHER
            // =====================================================

            ShootNormal();
        }


        // =========================================================
        // CANNON SHOOT
        // =========================================================

        private void ShootCannon()
        {
            if (
                towerData.ProjectilePrefab ==
                null
            )
            {
                Debug.LogWarning(
                    $"[Cannon] " +
                    $"{gameObject.name}: " +
                    "ProjectilePrefab is NULL."
                );

                return;
            }


            GameObject projectileObject =
                Instantiate(
                    towerData.ProjectilePrefab,
                    shootPoint.position,
                    shootPoint.rotation
                );


            if (projectileObject == null)
            {
                Debug.LogError(
                    "[Cannon] " +
                    "Failed to instantiate projectile."
                );

                return;
            }


            ExplosiveProjectileController projectile =
                projectileObject.GetComponent<
                    ExplosiveProjectileController
                >();


            if (projectile == null)
            {
                Debug.LogError(
                    "[Cannon] " +
                    $"Prefab " +
                    $"{towerData.ProjectilePrefab.name} " +
                    "does not contain " +
                    "ExplosiveProjectileController."
                );


                Destroy(
                    projectileObject
                );


                return;
            }


            projectile.Initialize(
                _targetEnemy,
                CurrentDamage,
                towerData.ProjectileSpeed
            );


            Debug.Log(
                $"[Cannon] FIRE | " +
                $"Tower={gameObject.name} | " +
                $"Target={_targetEnemy.gameObject.name} | " +
                $"Damage={CurrentDamage} | " +
                $"Speed={towerData.ProjectileSpeed}"
            );
        }


        // =========================================================
        // NORMAL SHOOT
        // =========================================================

        private void ShootNormal()
        {
            if (
                towerData.ProjectilePrefab ==
                null
            )
            {
                _targetEnemy.TakeDamage(
                    CurrentDamage
                );

                return;
            }


            GameObject projectileObject;


            if (ObjectPooler.Instance != null)
            {
                projectileObject =
                    ObjectPooler.Instance.GetPooledObject(
                        towerData.ProjectilePrefab,
                        shootPoint.position,
                        shootPoint.rotation
                    );
            }
            else
            {
                projectileObject =
                    Instantiate(
                        towerData.ProjectilePrefab,
                        shootPoint.position,
                        shootPoint.rotation
                    );
            }


            if (projectileObject == null)
                return;


            ProjectileController projectile =
                projectileObject.GetComponent<
                    ProjectileController
                >();


            if (projectile == null)
            {
                Debug.LogError(
                    "[TowerController] " +
                    $"Prefab " +
                    $"{towerData.ProjectilePrefab.name} " +
                    "does not contain " +
                    "ProjectileController."
                );


                if (ObjectPooler.Instance != null)
                {
                    ObjectPooler.Instance.ReturnToPool(
                        projectileObject
                    );
                }
                else
                {
                    Destroy(
                        projectileObject
                    );
                }


                return;
            }


            bool isIce =
                towerData.Type ==
                TowerType.Ice;


            float slowPercent =
                isIce
                    ? towerData.SlowPercent
                    : 0f;


            float slowDuration =
                isIce
                    ? towerData.SlowDuration
                    : 0f;


            projectile.Initialize(
                _targetEnemy,
                CurrentDamage,
                towerData.ProjectileSpeed,
                isIce,
                slowPercent,
                slowDuration
            );
        }


        // =========================================================
        // LEVEL UP
        // =========================================================

        public void LevelUp()
        {
            if (
                _currentLevel >=
                MAX_LEVEL
            )
            {
                Debug.Log(
                    $"[TowerController] " +
                    $"{gameObject.name} " +
                    "is already Level 3."
                );

                return;
            }


            if (towerData == null)
            {
                Debug.LogError(
                    $"[TowerController] " +
                    $"{gameObject.name}: " +
                    "TowerData is NULL."
                );

                return;
            }


            int oldLevel =
                _currentLevel;


            _currentLevel++;


            // -----------------------------------------------------
            // REMOVE OLD VISUALS
            // -----------------------------------------------------

            RemoveCurrentVisuals();


            // -----------------------------------------------------
            // SELECT PREFAB
            // -----------------------------------------------------

            GameObject prefabToUse =
                null;


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
            // APPLY VISUAL
            // -----------------------------------------------------

            if (prefabToUse != null)
            {
                ApplyVisualFromPrefab(
                    prefabToUse
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[TowerController] " +
                    $"{gameObject.name}: " +
                    $"No visual prefab for Level " +
                    $"{_currentLevel}."
                );
            }


            // -----------------------------------------------------
            // RESET COMBAT TARGET
            // -----------------------------------------------------

            _targetEnemy =
                null;


            _targetReevaluateTimer =
                0f;


            _fireCooldownTimer =
                0f;


            // -----------------------------------------------------
            // RESET GOLD TIMER AFTER UPGRADE
            // -----------------------------------------------------

            if (IsGoldTower)
            {
                _goldTimer =
                    GoldInterval;


                Debug.Log(
                    $"[GoldTower] " +
                    $"{gameObject.name} " +
                    $"Level {oldLevel} -> " +
                    $"{_currentLevel} | " +
                    $"Gold=+{CurrentGoldPerTick} | " +
                    $"Interval={GoldInterval}s"
                );
            }
            else
            {
                Debug.Log(
                    $"[TowerController] " +
                    $"{gameObject.name} " +
                    $"Level {oldLevel} -> " +
                    $"{_currentLevel} | " +
                    $"Damage={CurrentDamage} | " +
                    $"Range={CurrentRange:F1}"
                );
            }
        }


        // =========================================================
        // REMOVE CURRENT VISUALS
        // =========================================================

        private void RemoveCurrentVisuals()
        {
            for (
                int i = transform.childCount - 1;
                i >= 0;
                i--
            )
            {
                Transform child =
                    transform.GetChild(i);


                if (
                    child == null ||
                    !child.name.StartsWith(
                        "Visual_"
                    )
                )
                {
                    continue;
                }


                Destroy(
                    child.gameObject
                );
            }
        }


        // =========================================================
        // APPLY VISUAL FROM PREFAB
        // =========================================================

        private void ApplyVisualFromPrefab(
            GameObject prefab)
        {
            if (prefab == null)
                return;


            foreach (
                Transform child
                in prefab.transform
            )
            {
                if (
                    child == null ||
                    !child.name.StartsWith(
                        "Visual_"
                    )
                )
                {
                    continue;
                }


                GameObject visual =
                    Instantiate(
                        child.gameObject,
                        transform
                    );


                visual.name =
                    child.name;


                visual.transform.localPosition =
                    child.localPosition;


                visual.transform.localRotation =
                    child.localRotation;


                visual.transform.localScale =
                    child.localScale;
            }
        }


        // =========================================================
        // RESET
        // =========================================================

        public void ResetTowerState()
        {
            EnsureVisualTemplates();


            if (_currentLevel > 1)
            {
                _currentLevel =
                    1;


                RestoreInitialVisuals();
            }


            _targetEnemy =
                null;


            _fireCooldownTimer =
                0f;


            _targetReevaluateTimer =
                0f;


            if (IsGoldTower)
            {
                _goldTimer =
                    GoldInterval;
            }
        }


        // =========================================================
        // RESTORE INITIAL VISUALS
        // =========================================================

        private void RestoreInitialVisuals()
        {
            RemoveCurrentVisuals();


            if (_visualTemplateParent == null)
                return;


            foreach (
                Transform templateChild
                in _visualTemplateParent.transform
            )
            {
                if (templateChild == null)
                    continue;


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


                restoredVisual.SetActive(
                    true
                );
            }
        }


        // =========================================================
        // TARGETING MODE
        // =========================================================

        public void SetTargetingMode(
            TargetingMode mode)
        {
            targetingMode =
                mode;


            _targetEnemy =
                null;


            _targetReevaluateTimer =
                0f;
        }


        public TargetingMode GetTargetingMode()
        {
            return targetingMode;
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