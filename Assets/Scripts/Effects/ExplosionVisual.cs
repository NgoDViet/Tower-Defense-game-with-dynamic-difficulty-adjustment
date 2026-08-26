using UnityEngine;

namespace TowerDefense.Effects
{
    /// <summary>
    /// Component managing a runtime explosion visual effect.
    /// Scales up a circular sprite to simulate a shockwave and fades it out over time.
    /// </summary>
    public class ExplosionVisual : MonoBehaviour
    {
        private float _maxRadius = 2f;
        private float _duration = 0.25f;
        private float _elapsed = 0f;
        private SpriteRenderer _sr;

        /// <summary>
        /// Initializes the visual parameters of the explosion.
        /// </summary>
        public void Initialize(float radius, Color color, Sprite sprite)
        {
            _maxRadius = radius;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.color = color;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Scale up smoothly to the full blast diameter (radius * 2)
            float currentScale = Mathf.Lerp(0f, _maxRadius * 2f, t);
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            // Fade out the sprite opacity
            Color col = _sr.color;
            col.a = Mathf.Lerp(0.8f, 0f, t);
            _sr.color = col;
        }
    }
}
