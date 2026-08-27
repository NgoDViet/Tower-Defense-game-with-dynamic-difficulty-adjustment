using UnityEngine;
using TowerDefense.Enemy;

namespace TowerDefense.Tower
{
    public class TowerProjectile : MonoBehaviour
    {
        private EnemyHealth target;

        private int damage;

        private float speed;

        private bool ignoreArmor;

        private bool explosive;

        private float explosionRadius;

        private bool slowing;

        private float slowPercent;

        private float slowDuration;

        private bool initialized;

        public void Initialize(
            EnemyHealth target,
            int damage,
            float speed,
            bool ignoreArmor,
            bool explosive,
            float explosionRadius,
            bool slowing,
            float slowPercent,
            float slowDuration)
        {
            this.target = target;

            this.damage = damage;

            this.speed = speed;

            this.ignoreArmor =
                ignoreArmor;

            this.explosive =
                explosive;

            this.explosionRadius =
                explosionRadius;

            this.slowing =
                slowing;

            this.slowPercent =
                slowPercent;

            this.slowDuration =
                slowDuration;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (target == null ||
                target.IsDead)
            {
                Destroy(gameObject);

                return;
            }

            Vector3 targetPosition =
                target.transform.position;

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

            Vector3 direction =
                targetPosition -
                transform.position;

            if (direction.sqrMagnitude >
                0.001f)
            {
                float angle =
                    Mathf.Atan2(
                        direction.y,
                        direction.x
                    ) *
                    Mathf.Rad2Deg;

                transform.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle
                    );
            }

            if (Vector3.Distance(
                    transform.position,
                    targetPosition) <= 0.05f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (target == null)
            {
                Destroy(gameObject);

                return;
            }

            if (explosive)
            {
                Explode();
            }
            else
            {
                DealDamage(target);

                ApplySlow(target);
            }

            Destroy(gameObject);
        }

        // =========================================================
        // DAMAGE
        // =========================================================

        private void DealDamage(
            EnemyHealth enemy)
        {
            if (ignoreArmor)
            {
                enemy.TakeDamageIgnoringArmor(
                    damage
                );
            }
            else
            {
                enemy.TakeDamage(
                    damage
                );
            }
        }

        // =========================================================
        // SLOW
        // =========================================================

        private void ApplySlow(
            EnemyHealth enemy)
        {
            if (!slowing)
                return;

            enemy.ApplySlow(
                slowPercent,
                slowDuration
            );
        }

        // =========================================================
        // EXPLOSION
        // =========================================================

        private void Explode()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    explosionRadius
                );

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

                enemy.TakeDamage(
                    damage
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (explosive)
            {
                Gizmos.DrawWireSphere(
                    transform.position,
                    explosionRadius
                );
            }
        }
    }
}