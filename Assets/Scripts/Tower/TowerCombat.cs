using System.Collections.Generic;
using UnityEngine;

using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Projectile;

namespace TowerDefense.Tower
{
    public class TowerCombat : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]

        [SerializeField]
        private TowerData towerData;

        [SerializeField]
        private Transform firePoint;


        // =========================================================
        // TARGETING
        // =========================================================

        [Header("Targeting")]

        [SerializeField]
        private LayerMask enemyLayer;

        [SerializeField]
        private bool autoFindEnemyLayer = true;


        // =========================================================
        // RUNTIME
        // =========================================================

        private EnemyHealth currentTarget;

        private float attackTimer;


        // =========================================================
        // PUBLIC
        // =========================================================

        public TowerData TowerData
        {
            get
            {
                return towerData;
            }
        }

        public EnemyHealth CurrentTarget
        {
            get
            {
                return currentTarget;
            }
        }


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            // -----------------------------------------------------
            // FIRE POINT
            // -----------------------------------------------------

            if (firePoint == null)
            {
                firePoint = transform;
            }


            // -----------------------------------------------------
            // GET DATA FROM TOWER CONTROLLER IF EMPTY
            // -----------------------------------------------------

            if (towerData == null)
            {
                TowerController controller =
                    GetComponent<TowerController>();

                if (controller != null)
                {
                    towerData =
                        controller.TowerData;
                }
            }


            // -----------------------------------------------------
            // AUTO FIND ENEMY LAYER
            // -----------------------------------------------------

            if (
                autoFindEnemyLayer &&
                enemyLayer.value == 0
            )
            {
                int layer =
                    LayerMask.NameToLayer("Enemy");

                if (layer >= 0)
                {
                    enemyLayer =
                        1 << layer;
                }
            }
        }


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            if (towerData == null)
            {
                Debug.LogError(
                    "[TowerCombat] " +
                    gameObject.name +
                    " has no TowerData."
                );

                enabled = false;

                return;
            }


            // -----------------------------------------------------
            // GOLD DOES NOT ATTACK
            // -----------------------------------------------------

          if (
    towerData.Type == TowerType.Gold ||
    towerData.Type == TowerType.Laser
)
{
    enabled = false;
    return;
}


            if (firePoint == null)
            {
                firePoint = transform;
            }


            attackTimer = 0f;


            Debug.Log(
                "[TowerCombat] READY | " +
                "Tower=" +
                gameObject.name +
                " | Type=" +
                towerData.Type +
                " | Range=" +
                towerData.Range +
                " | FireRate=" +
                towerData.FireRate +
                " | Damage=" +
                towerData.Damage +
                " | Projectile=" +
                (
                    towerData.ProjectilePrefab != null
                        ? towerData.ProjectilePrefab.name
                        : "NULL"
                )
            );
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (towerData == null)
            {
                return;
            }


            if (towerData.Type == TowerType.Gold)
            {
                return;
            }


            // -----------------------------------------------------
            // FIND TARGET
            // -----------------------------------------------------

            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindTarget();
            }


            if (currentTarget == null)
            {
                return;
            }


            // -----------------------------------------------------
            // ATTACK TIMER
            // -----------------------------------------------------

            attackTimer -= Time.deltaTime;


            if (attackTimer <= 0f)
            {
                Attack();


                float fireRate =
                    Mathf.Max(
                        towerData.FireRate,
                        0.01f
                    );


                attackTimer =
                    1f / fireRate;
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

            float bestDistance =
                float.MaxValue;


            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }


                if (enemy.IsDead)
                {
                    continue;
                }


                if (!enemy.gameObject.activeSelf)
                {
                    continue;
                }


                // -------------------------------------------------
                // ENEMY LAYER
                // -------------------------------------------------

                if (enemyLayer.value != 0)
                {
                    int enemyLayerBit =
                        1 << enemy.gameObject.layer;


                    if (
                        (enemyLayer.value &
                         enemyLayerBit) == 0
                    )
                    {
                        continue;
                    }
                }


                float distance =
                    Vector2.Distance(
                        transform.position,
                        enemy.transform.position
                    );


                if (distance > towerData.Range)
                {
                    continue;
                }


                if (distance < bestDistance)
                {
                    bestDistance =
                        distance;

                    bestTarget =
                        enemy;
                }
            }


            if (bestTarget != null)
            {
                Debug.Log(
                    "[TowerCombat] " +
                    gameObject.name +
                    " TARGET = " +
                    bestTarget.gameObject.name
                );
            }


            return bestTarget;
        }


        // =========================================================
        // TARGET VALIDATION
        // =========================================================

        private bool IsTargetValid(
            EnemyHealth target)
        {
            if (target == null)
            {
                return false;
            }


            if (target.IsDead)
            {
                return false;
            }


            if (!target.gameObject.activeSelf)
            {
                return false;
            }


            float distance =
                Vector2.Distance(
                    transform.position,
                    target.transform.position
                );


            return distance <=
                   towerData.Range;
        }


        // =========================================================
        // ATTACK
        // =========================================================

        private void Attack()
        {
            if (currentTarget == null)
            {
                return;
            }


            switch (towerData.Type)
            {
                case TowerType.Cannon:

                    FireCannon();

                    break;


                case TowerType.Ice:

                    FireIce();

                    break;


                case TowerType.Gold:

                    // Gold Tower không tấn công.
                    break;


                case TowerType.Archer:
                case TowerType.Fast:

                     FireNormal();

                     break;

                case TowerType.Laser:

                      break;


                default:

                    FireNormal();

                    break;
            }
        }


        // =========================================================
        // NORMAL
        // =========================================================

        private void FireNormal()
        {
            if (currentTarget == null)
            {
                return;
            }


            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamage(
                    towerData.Damage
                );

                return;
            }


            GameObject projectileObject =
                Instantiate(
                    towerData.ProjectilePrefab,
                    firePoint.position,
                    firePoint.rotation
                );


            if (projectileObject == null)
            {
                return;
            }


            TowerProjectile projectile =
                projectileObject.GetComponent<
                    TowerProjectile
                >();


            if (projectile == null)
            {
                Debug.LogError(
                    "[TowerCombat] " +
                    "Normal projectile " +
                    towerData.ProjectilePrefab.name +
                    " does not contain TowerProjectile."
                );


                Destroy(projectileObject);

                return;
            }


            projectile.Initialize(
                currentTarget,
                towerData.Damage,
                towerData.ProjectileSpeed,
                false,
                false,
                towerData.ExplosionRadius,
                false,
                0f,
                0f
            );
        }


        // =========================================================
        // ICE
        // =========================================================

        private void FireIce()
        {
            if (currentTarget == null)
            {
                return;
            }


            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamage(
                    towerData.Damage
                );


                currentTarget.ApplySlow(
                    towerData.SlowPercent,
                    towerData.SlowDuration
                );


                return;
            }


            GameObject projectileObject =
                Instantiate(
                    towerData.ProjectilePrefab,
                    firePoint.position,
                    firePoint.rotation
                );


            if (projectileObject == null)
            {
                return;
            }


            TowerProjectile projectile =
                projectileObject.GetComponent<
                    TowerProjectile
                >();


            if (projectile == null)
            {
                Debug.LogError(
                    "[TowerCombat] " +
                    "Ice projectile " +
                    towerData.ProjectilePrefab.name +
                    " does not contain TowerProjectile."
                );


                Destroy(projectileObject);

                return;
            }


            projectile.Initialize(
                currentTarget,
                towerData.Damage,
                towerData.ProjectileSpeed,
                false,
                false,
                towerData.ExplosionRadius,
                true,
                towerData.SlowPercent,
                towerData.SlowDuration
            );
        }


        // =========================================================
        // CANNON
        // =========================================================

        private void FireCannon()
        {
            if (currentTarget == null)
            {
                return;
            }


            Debug.Log(
                "[Cannon] FIRE | " +
                "Tower=" +
                gameObject.name +
                " | Target=" +
                currentTarget.gameObject.name
            );


            // -----------------------------------------------------
            // NO PROJECTILE
            // -----------------------------------------------------

            if (towerData.ProjectilePrefab == null)
            {
                Debug.LogWarning(
                    "[Cannon] ProjectilePrefab is NULL. " +
                    "Using direct explosion."
                );


                ExplodeAtTarget(
                    currentTarget.transform.position
                );


                return;
            }


            // -----------------------------------------------------
            // CREATE CANNON PROJECTILE
            // -----------------------------------------------------

            GameObject projectileObject =
                Instantiate(
                    towerData.ProjectilePrefab,
                    firePoint.position,
                    firePoint.rotation
                );


            if (projectileObject == null)
            {
                Debug.LogError(
                    "[Cannon] Failed to instantiate projectile."
                );

                return;
            }


            // -----------------------------------------------------
            // GET EXPLOSIVE CONTROLLER
            // -----------------------------------------------------

            ExplosiveProjectileController projectile =
                projectileObject.GetComponent<
                    ExplosiveProjectileController
                >();


            if (projectile == null)
            {
                Debug.LogError(
                    "[Cannon] " +
                    "CannonProjectile does not contain " +
                    "ExplosiveProjectileController."
                );


                Destroy(projectileObject);

                return;
            }


            // -----------------------------------------------------
            // INITIALIZE
            // -----------------------------------------------------

            projectile.Initialize(
                currentTarget,
                towerData.Damage,
                towerData.ProjectileSpeed
            );


            Debug.Log(
                "[Cannon] PROJECTILE CREATED | " +
                "Target=" +
                currentTarget.gameObject.name +
                " | Damage=" +
                towerData.Damage +
                " | Speed=" +
                towerData.ProjectileSpeed
            );
        }


        // =========================================================
        // FALLBACK CANNON EXPLOSION
        // =========================================================

        private void ExplodeAtTarget(
            Vector3 position)
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    position,
                    towerData.ExplosionRadius
                );


            HashSet<EnemyHealth> damaged =
                new HashSet<EnemyHealth>();


            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }


                EnemyHealth enemy =
                    hit.GetComponent<
                        EnemyHealth
                    >();


                if (enemy == null)
                {
                    enemy =
                        hit.GetComponentInParent<
                            EnemyHealth
                        >();
                }


                if (enemy == null)
                {
                    continue;
                }


                if (enemy.IsDead)
                {
                    continue;
                }


                if (!enemy.gameObject.activeSelf)
                {
                    continue;
                }


                if (damaged.Contains(enemy))
                {
                    continue;
                }


                damaged.Add(enemy);


                enemy.TakeDamage(
                    towerData.Damage
                );
            }
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            if (towerData == null)
            {
                return;
            }


            Gizmos.DrawWireSphere(
                transform.position,
                towerData.Range
            );


            if (towerData.Type == TowerType.Cannon)
            {
                Gizmos.DrawWireSphere(
                    transform.position,
                    towerData.ExplosionRadius
                );
            }
        }
    }
}