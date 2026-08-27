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
            this.damage = Mathf.Max(0, damage);
            this.speed = Mathf.Max(0f, speed);

            this.ignoreArmor = ignoreArmor;
            this.explosive = explosive;
            this.explosionRadius = Mathf.Max(0f, explosionRadius);

            this.slowing = slowing;
            this.slowPercent = Mathf.Clamp01(slowPercent);
            this.slowDuration = Mathf.Max(0f, slowDuration);

            initialized = true;

            gameObject.SetActive(true);

            EnableVisuals();
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (target == null ||
                target.IsDead ||
                !target.gameObject.activeSelf)
            {
                DestroyProjectile();
                return;
            }

            Vector3 targetPosition = target.transform.position;

            Vector3 direction =
                targetPosition - transform.position;

            if (direction.sqrMagnitude > 0.001f)
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

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

            if (Vector2.Distance(
                    transform.position,
                    targetPosition
                ) <= 0.08f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (!initialized)
                return;

            if (target == null)
            {
                DestroyProjectile();
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

            DestroyProjectile();
        }

        private void DealDamage(EnemyHealth enemy)
        {
            if (enemy == null)
                return;

            if (ignoreArmor)
            {
                enemy.TakeDamageIgnoringArmor(damage);
            }
            else
            {
                enemy.TakeDamage(damage);
            }
        }

        private void ApplySlow(EnemyHealth enemy)
        {
            if (!slowing)
                return;

            if (enemy == null)
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

                if (!enemy.gameObject.activeSelf)
                    continue;

                DealDamage(enemy);
            }
        }

        private void EnableVisuals()
        {
            SpriteRenderer[] sprites =
                GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer sprite in sprites)
            {
                if (sprite == null)
                    continue;

                sprite.enabled = true;
            }

            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                renderer.enabled = true;
            }
        }

        private void DestroyProjectile()
        {
            initialized = false;
            target = null;

            damage = 0;
            speed = 0f;

            ignoreArmor = false;

            explosive = false;
            explosionRadius = 0f;

            slowing = false;
            slowPercent = 0f;
            slowDuration = 0f;

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (!explosive)
                return;

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                transform.position,
                explosionRadius
            );
        }
    }
}