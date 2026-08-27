using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Projectile;

namespace TowerDefense.Tower
{
    public class TowerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TowerData towerData;
        [SerializeField] private Transform firePoint;

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private bool autoFindEnemyLayer = true;

        private EnemyHealth currentTarget;
        private float attackTimer;

        public TowerData TowerData => towerData;
        public EnemyHealth CurrentTarget => currentTarget;

        private void Awake()
        {
            if (firePoint == null)
                firePoint = transform;

            if (towerData == null)
            {
                TowerController controller = GetComponent<TowerController>();
                if (controller != null)
                    towerData = controller.TowerData;
            }

            if (autoFindEnemyLayer && enemyLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0)
                    enemyLayer = 1 << layer;
            }
        }

        private void Start()
        {
            if (towerData == null)
            {
                Debug.LogError("[TowerCombat] " + gameObject.name + " has no TowerData.");
                enabled = false;
                return;
            }

            // Gold and Laser use their own/passive systems.
            if (towerData.Type == TowerType.Gold || towerData.Type == TowerType.Laser)
            {
                enabled = false;
                return;
            }

            if (firePoint == null)
                firePoint = transform;

            attackTimer = 0f;
        }

        private void Update()
        {
            if (towerData == null)
                return;

            if (!IsTargetValid(currentTarget))
                currentTarget = FindTarget();

            if (currentTarget == null)
                return;

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                Attack();
                float fireRate = Mathf.Max(towerData.FireRate, 0.01f);
                attackTimer = 1f / fireRate;
            }
        }

        private EnemyHealth FindTarget()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                if (enemyLayer.value != 0)
                {
                    int enemyLayerBit = 1 << enemy.gameObject.layer;
                    if ((enemyLayer.value & enemyLayerBit) == 0)
                        continue;
                }

                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance > towerData.Range)
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private bool IsTargetValid(EnemyHealth target)
        {
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
                return false;

            return Vector2.Distance(transform.position, target.transform.position) <= towerData.Range;
        }

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

                case TowerType.Archer:
                case TowerType.Fast:
                    FireNormal();
                    break;
            }
        }

        private void FireNormal()
        {
            if (currentTarget == null)
                return;

            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamage(towerData.Damage);
                return;
            }

            GameObject projectileObject = Instantiate(
                towerData.ProjectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

            if (projectileObject == null)
                return;

            TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();
            if (projectile == null)
                projectile = projectileObject.GetComponentInChildren<TowerProjectile>();

            if (projectile == null)
            {
                Debug.LogError(
                    "[TowerCombat] Normal projectile " +
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

        private void FireIce()
        {
            if (currentTarget == null)
                return;

            if (towerData.ProjectilePrefab == null)
            {
                currentTarget.TakeDamage(towerData.Damage);
                currentTarget.ApplySlow(towerData.SlowPercent, towerData.SlowDuration);
                return;
            }

            GameObject projectileObject = Instantiate(
                towerData.ProjectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

            if (projectileObject == null)
                return;

            TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();
            if (projectile == null)
                projectile = projectileObject.GetComponentInChildren<TowerProjectile>();

            if (projectile == null)
            {
                Debug.LogError(
                    "[TowerCombat] Ice projectile " +
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

        private void FireCannon()
        {
            if (currentTarget == null)
                return;

            if (towerData.ProjectilePrefab == null)
            {
                ExplodeAtTarget(currentTarget.transform.position);
                return;
            }

            GameObject projectileObject = Instantiate(
                towerData.ProjectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

            if (projectileObject == null)
                return;

            ExplosiveProjectileController projectile =
                projectileObject.GetComponent<ExplosiveProjectileController>();

            if (projectile == null)
                projectile = projectileObject.GetComponentInChildren<ExplosiveProjectileController>();

            if (projectile == null)
            {
                Debug.LogError(
                    "[Cannon] " +
                    towerData.ProjectilePrefab.name +
                    " does not contain ExplosiveProjectileController."
                );
                Destroy(projectileObject);
                return;
            }

            projectile.Initialize(
                currentTarget,
                towerData.Damage,
                towerData.ProjectileSpeed
            );
        }

        private void ExplodeAtTarget(Vector3 position)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                position,
                towerData.ExplosionRadius
            );

            HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                if (!damaged.Add(enemy))
                    continue;

                enemy.TakeDamage(towerData.Damage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (towerData == null)
                return;

            Gizmos.DrawWireSphere(transform.position, towerData.Range);

            if (towerData.Type == TowerType.Cannon)
                Gizmos.DrawWireSphere(transform.position, towerData.ExplosionRadius);
        }
    }
}
