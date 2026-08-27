using UnityEngine;

namespace TowerDefense.Data
{
    public enum TowerType
    {
        Archer,
        Fast,
        Mage,
        Cannon,
        Ice,
        Laser
    }

    [CreateAssetMenu(
        fileName = "NewTowerData",
        menuName = "Tower Defense/Tower Data",
        order = 2
    )]
    public class TowerData : ScriptableObject
    {
        // =========================================================
        // IDENTITY
        // =========================================================

        [Header("Identity")]

        [SerializeField]
        private string towerName = "Archer Tower";

        [SerializeField]
        private TowerType towerType = TowerType.Archer;

        [SerializeField]
        private Sprite towerSprite;


        // =========================================================
        // COST
        // =========================================================

        [Header("Costs")]

        [SerializeField]
        private int cost = 100;


        // =========================================================
        // GAMEPLAY STATS
        // =========================================================

        [Header("Gameplay Stats")]

        [SerializeField]
        private float range = 5f;

        [Tooltip("Number of attacks per second.")]
        [SerializeField]
        private float fireRate = 1f;

        [SerializeField]
        private int damage = 2;


        // =========================================================
        // PROJECTILE SETTINGS
        // =========================================================

        [Header("Projectile Settings")]

        [SerializeField]
        private GameObject projectilePrefab;

        [SerializeField]
        private float projectileSpeed = 7f;


        // =========================================================
        // CANNON SETTINGS
        // =========================================================

        [Header("Cannon Settings")]

        [Tooltip("Explosion radius for Cannon.")]
        [SerializeField]
        private float explosionRadius = 1.5f;


        // =========================================================
        // ICE SETTINGS
        // =========================================================

        [Header("Ice Settings")]

        [Range(0f, 0.95f)]
        [Tooltip("Slow percentage. 0.3 = 30% slow.")]
        [SerializeField]
        private float slowPercent = 0.3f;

        [Tooltip("Duration of slow effect in seconds.")]
        [SerializeField]
        private float slowDuration = 2f;


        // =========================================================
        // LASER SETTINGS
        // =========================================================

        [Header("Laser Settings")]

        [Tooltip("Damage interval for Laser.")]
        [SerializeField]
        private float laserDamageInterval = 0.2f;

        [Tooltip("Laser beam width.")]
        [SerializeField]
        private float laserWidth = 0.08f;


        // =========================================================
        // GETTERS
        // =========================================================

        public string TowerName
        {
            get
            {
                return towerName;
            }
        }

        public TowerType Type
        {
            get
            {
                return towerType;
            }
        }

        public Sprite TowerSprite
        {
            get
            {
                return towerSprite;
            }
        }

        public int Cost
        {
            get
            {
                return cost;
            }
        }

        public float Range
        {
            get
            {
                return range;
            }
        }

        public float FireRate
        {
            get
            {
                return fireRate;
            }
        }

        public int Damage
        {
            get
            {
                return damage;
            }
        }

        public GameObject ProjectilePrefab
        {
            get
            {
                return projectilePrefab;
            }
        }

        public float ProjectileSpeed
        {
            get
            {
                return projectileSpeed;
            }
        }

        public float ExplosionRadius
        {
            get
            {
                return explosionRadius;
            }
        }

        public float SlowPercent
        {
            get
            {
                return slowPercent;
            }
        }

        public float SlowDuration
        {
            get
            {
                return slowDuration;
            }
        }

        public float LaserDamageInterval
        {
            get
            {
                return laserDamageInterval;
            }
        }

        public float LaserWidth
        {
            get
            {
                return laserWidth;
            }
        }
    }
}