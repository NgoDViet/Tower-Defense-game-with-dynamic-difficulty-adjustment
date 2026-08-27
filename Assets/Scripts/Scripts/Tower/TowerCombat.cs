using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Enemy;

namespace TowerDefense.Tower
{
    public class TowerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TowerData towerData;

        [SerializeField] private Transform firePoint;

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayer;

        [Tooltip("Automatically find enemy layer if LayerMask is empty.")]
        [SerializeField] private bool autoFindEnemyLayer = true;

        private EnemyHealth currentTarget;

        private float attackTimer;

        public TowerData TowerData => towerData;

        public EnemyHealth CurrentTarget =>
            currentTarget;

        private void Start()
        {
            if (towerData == null)
            {
                Debug.LogError(
                    $"[TowerCombat] {name}: TowerData is missing."
                );

                enabled = false;

                return;
            }

            attackTimer = 0f;
        }

        private void Update()
        {
            if (towerData == null)
                return;

            // Check target
            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindTarget();
            }

            if (currentTarget == null)
                return;

            // Attack timer
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
        // TARGET
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
                    continue;

                if (enemy.IsDead)
                    continue;

                float distance =
                    Vector3.Distance(
                        transform.position,
                        enemy.transform.position
                    );

                if (distance >
                    towerData.Range)
                {
                    continue;
                }

                if (distance <
                    bestDistance)
                {
                    bestDistance = distance;

                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private bool IsTargetValid(
            EnemyHealth target)
        {
            if (target == null)
                return false;

            if (target.IsDead)
                return false;

            float distance =
                Vector3.Distance(
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
                return;

            switch (towerData.Type)
            {
                case TowerType.Cannon:
                    FireCannon();

                    break;

                case TowerType.Ice:
                    FireIce();

                    break;

                case TowerType.Mage:
                    FireMage();

                    break;

                case TowerType.Archer:
                case TowerType.Fast:
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
            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamage(
                    towerData.Damage
                );

                return;
            }

            SpawnProjectile(
                false,
                false,
                false
            );
        }

        // =========================================================
        // MAGE
        // =========================================================

        private void FireMage()
        {
            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamageIgnoringArmor(
                    towerData.Damage
                );

                return;
            }

            SpawnProjectile(
                true,
                false,
                false
            );
        }

        // =========================================================
        // CANNON
        // =========================================================

        private void FireCannon()
        {
            if (towerData.ProjectilePrefab == null)
            {
                ExplodeAtTarget(
                    currentTarget.transform.position
                );

                return;
            }

            SpawnProjectile(
                false,
                true,
                false
            );
        }

        // =========================================================
        // ICE
        // =========================================================

        private void FireIce()
        {
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

            SpawnProjectile(
                false,
                false,
                true
            );
        }

        // =========================================================
        // PROJECTILE
        // =========================================================

        private void SpawnProjectile(
            bool ignoreArmor,
            bool explosive,
            bool slowing)
        {
            Vector3 spawnPosition =
                firePoint != null
                    ? firePoint.position
                    : transform.position;

            GameObject projectileObject =
                Instantiate(
                    towerData.ProjectilePrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            TowerProjectile projectile =
                projectileObject.GetComponent<TowerProjectile>();

            if (projectile == null)
            {
                Debug.LogError(
                    $"[TowerCombat] Projectile prefab " +
                    $"{towerData.ProjectilePrefab.name} " +
                    $"does not contain TowerProjectile."
                );

                Destroy(projectileObject);

                return;
            }

            projectile.Initialize(
                currentTarget,
                towerData.Damage,
                towerData.ProjectileSpeed,
                ignoreArmor,
                explosive,
                towerData.ExplosionRadius,
                slowing,
                towerData.SlowPercent,
                towerData.SlowDuration
            );
        }

        // =========================================================
        // CANNON EXPLOSION
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
                    continue;

                EnemyHealth enemy =
                    hit.GetComponentInParent<EnemyHealth>();

                if (enemy == null)
                    continue;

                if (enemy.IsDead)
                    continue;

                if (damaged.Contains(enemy))
                    continue;

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
                return;

            Gizmos.DrawWireSphere(
                transform.position,
                towerData.Range
            );

            if (towerData.Type ==
                TowerType.Cannon)
            {
                Gizmos.DrawWireSphere(
                    transform.position,
                    towerData.ExplosionRadius
                );
            }
        }
    }
}