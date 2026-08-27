using UnityEngine;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Effects;

namespace TowerDefense.Projectile
{
    public class ExplosiveProjectileController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float impactDistanceThreshold = 0.1f;
        [SerializeField] private bool rotateTowardsTarget = true;
        [SerializeField] private float spriteAngleOffset = 0f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 2.0f;
        [SerializeField] private Sprite explosionCircleSprite;
        [SerializeField] private Color explosionColor = new Color(1f, 0.4f, 0f, 0.8f);

        private EnemyHealth _target;
        private int _damage;
        private float _speed;
        private Vector3 _targetDestination;
        private bool _isInitialized;
        private bool _hasExploded;
        private SpriteRenderer[] _renderers;

        public void Initialize(EnemyHealth target, int damage, float speed)
        {
            _target = target;
            _damage = Mathf.Max(0, damage);
            _speed = Mathf.Max(0.1f, speed);
            _isInitialized = false;
            _hasExploded = false;

            EnsureProjectileVisible();

            if (_target == null)
            {
                Recycle();
                return;
            }

            _targetDestination = CalculateDestination(_target);
            _isInitialized = true;
        }

        private void Awake()
        {
            EnsureProjectileVisible();
        }

        private void OnEnable()
        {
            _isInitialized = false;
            _hasExploded = false;
            _target = null;
            EnsureProjectileVisible();
        }

        private void Update()
        {
            if (!_isInitialized || _hasExploded)
                return;

            if (_target == null || _target.IsDead || !_target.gameObject.activeInHierarchy)
            {
                Recycle();
                return;
            }

            Vector3 currentPos = transform.position;
            transform.position = Vector3.MoveTowards(
                currentPos,
                _targetDestination,
                _speed * Time.deltaTime
            );

            if (rotateTowardsTarget)
            {
                Vector3 direction = _targetDestination - currentPos;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(
                        angle + spriteAngleOffset,
                        Vector3.forward
                    );
                }
            }

            if (Vector2.Distance(transform.position, _targetDestination) <= impactDistanceThreshold)
            {
                Explode();
            }
        }

        private Vector3 CalculateDestination(EnemyHealth target)
        {
            Vector3 targetPos = target.transform.position;
            Vector3 targetVel = Vector3.zero;

            EnemyMovement movement = target.GetComponent<EnemyMovement>();
            if (movement != null && movement.ActivePath != null)
            {
                int wpIndex = movement.CurrentWaypointIndex;
                if (wpIndex < movement.ActivePath.WaypointCount)
                {
                    Transform waypoint = movement.ActivePath.GetWaypoint(wpIndex);
                    if (waypoint != null)
                    {
                        Vector3 direction = (waypoint.position - targetPos).normalized;
                        targetVel = direction * target.MoveSpeed;
                    }
                }
            }

            Vector3 relativePos = targetPos - transform.position;
            float a = targetVel.sqrMagnitude - _speed * _speed;
            float b = 2f * Vector3.Dot(relativePos, targetVel);
            float c = relativePos.sqrMagnitude;
            float t = -1f;

            if (Mathf.Abs(a) < 0.0001f)
            {
                if (Mathf.Abs(b) > 0.0001f)
                {
                    float candidate = -c / b;
                    if (candidate > 0f)
                        t = candidate;
                }
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant >= 0f)
                {
                    float sqrt = Mathf.Sqrt(discriminant);
                    float t1 = (-b - sqrt) / (2f * a);
                    float t2 = (-b + sqrt) / (2f * a);

                    if (t1 > 0f && t2 > 0f)
                        t = Mathf.Min(t1, t2);
                    else if (t1 > 0f)
                        t = t1;
                    else if (t2 > 0f)
                        t = t2;
                }
            }

            return t > 0f && t < 5f
                ? targetPos + targetVel * t
                : targetPos;
        }

        private void Explode()
        {
            if (_hasExploded)
                return;

            _hasExploded = true;
            _isInitialized = false;

            if (explosionCircleSprite != null)
            {
                GameObject visualGO = new GameObject("ExplosionEffect");
                visualGO.transform.position = transform.position;
                ExplosionVisual visual = visualGO.AddComponent<ExplosionVisual>();
                visual.Initialize(explosionRadius, explosionColor, explosionCircleSprite);
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(
                transform.position,
                explosionRadius
            );

            foreach (Collider2D col in colliders)
            {
                if (col == null)
                    continue;

                EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                enemy.TakeDamage(_damage);
            }

            Recycle();
        }

        private void Recycle()
        {
            _isInitialized = false;
            _hasExploded = true;
            _target = null;

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void EnsureProjectileVisible()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (_renderers == null || _renderers.Length == 0)
            {
                Debug.LogError(
                    "[ExplosiveProjectileController] " + gameObject.name +
                    " has no SpriteRenderer. Add a visible sprite to the cannon projectile prefab."
                );
                return;
            }

            foreach (SpriteRenderer renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.enabled = true;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 1000);
            }
        }
    }
}
