using UnityEngine;
using TowerDefense.Enemy;

namespace TowerDefense.Projectile
{
    public class ProjectileController : MonoBehaviour
    {
        private EnemyHealth target;

        private int damage;
        private float speed;

        private bool slowing;
        private float slowPercent;
        private float slowDuration;

        private bool initialized;
        private bool hasHit;

        private SpriteRenderer[] projectileRenderers;

        // =========================================================
        // INITIALIZE
        // =========================================================

        public void Initialize(
            EnemyHealth target,
            int damage,
            float speed,
            bool slowing,
            float slowPercent,
            float slowDuration)
        {
            this.target = target;

            this.damage = Mathf.Max(0, damage);
            this.speed = Mathf.Max(0.1f, speed);

            this.slowing = slowing;
            this.slowPercent = Mathf.Clamp01(slowPercent);
            this.slowDuration = Mathf.Max(0f, slowDuration);

            initialized = false;
            hasHit = false;

            EnsureProjectileVisible();

            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            initialized = true;
        }

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            EnsureProjectileVisible();
        }

        // =========================================================
        // ON ENABLE
        // =========================================================

        private void OnEnable()
        {
            initialized = false;
            hasHit = false;

            EnsureProjectileVisible();
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (!initialized || hasHit)
                return;

            if (target == null ||
                target.IsDead ||
                !target.gameObject.activeInHierarchy)
            {
                DestroyProjectile();
                return;
            }

            Vector3 targetPosition =
                target.transform.position;

            Vector3 direction =
                targetPosition - transform.position;

            // =====================================================
            // ROTATION
            // =====================================================

            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle =
                    Mathf.Atan2(
                        direction.y,
                        direction.x
                    ) * Mathf.Rad2Deg;

                transform.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle
                    );
            }

            // =====================================================
            // MOVE
            // =====================================================

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

            // =====================================================
            // HIT
            // =====================================================

            if (Vector3.Distance(
                    transform.position,
                    targetPosition
                ) <= 0.08f)
            {
                HitTarget();
            }
        }

        // =========================================================
        // HIT TARGET
        // =========================================================

        private void HitTarget()
        {
            if (!initialized || hasHit)
                return;

            hasHit = true;
            initialized = false;

            if (target == null || target.IsDead)
            {
                DestroyProjectile();
                return;
            }

            DealDamage(target);
            ApplySlow(target);

            DestroyProjectile();
        }

        // =========================================================
        // DAMAGE
        // =========================================================

        private void DealDamage(
            EnemyHealth enemy)
        {
            if (enemy == null)
                return;

            if (enemy.IsDead)
                return;

            enemy.TakeDamage(damage);
        }

        // =========================================================
        // SLOW
        // =========================================================

        private void ApplySlow(
            EnemyHealth enemy)
        {
            if (!slowing)
                return;

            if (enemy == null)
                return;

            if (enemy.IsDead)
                return;

            if (slowPercent <= 0f)
                return;

            if (slowDuration <= 0f)
                return;

            enemy.ApplySlow(
                slowPercent,
                slowDuration
            );
        }

        // =========================================================
        // VISUAL
        // =========================================================

        private void EnsureProjectileVisible()
        {
            if (projectileRenderers == null ||
                projectileRenderers.Length == 0)
            {
                projectileRenderers =
                    GetComponentsInChildren<SpriteRenderer>(
                        true
                    );
            }

            if (projectileRenderers == null ||
                projectileRenderers.Length == 0)
            {
                Debug.LogError(
                    "[ProjectileController] " +
                    gameObject.name +
                    " has no SpriteRenderer. " +
                    "Add a visible sprite to the projectile prefab."
                );

                return;
            }

            foreach (SpriteRenderer renderer in projectileRenderers)
            {
                if (renderer == null)
                    continue;

                renderer.enabled = true;

                renderer.sortingOrder =
                    Mathf.Max(
                        renderer.sortingOrder,
                        1000
                    );
            }
        }

        // =========================================================
        // DESTROY
        // =========================================================

        private void DestroyProjectile()
        {
            initialized = false;
            hasHit = true;

            target = null;

            damage = 0;
            speed = 0f;

            slowing = false;
            slowPercent = 0f;
            slowDuration = 0f;

            Destroy(gameObject);
        }
    }
}