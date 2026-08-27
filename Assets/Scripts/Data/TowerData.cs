using UnityEngine;

namespace TowerDefense.Data
{
    public enum TowerType
    {
        Archer,
        Fast,
        Gold,
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
        // GOLD TOWER SETTINGS
        // =========================================================
        //
        // ĐÚNG THEO BẢNG:
        //
        // Lv1:
        // +5 G / 5 giây
        //
        // Lv2:
        // +10 G / 4 giây
        //
        // Lv3:
        // +20 G / 3 giây
        //
        // Giá:
        // Lv1 = 250 G
        // Lv1 -> Lv2 = 250 G
        // Lv2 -> Lv3 = 500 G
        // =========================================================

        [Header("Gold Tower Settings")]

        [Tooltip("Gold generated at Level 1.")]
        [SerializeField]
        private int goldLevel1 = 5;

        [Tooltip("Gold generated at Level 2.")]
        [SerializeField]
        private int goldLevel2 = 10;

        [Tooltip("Gold generated at Level 3.")]
        [SerializeField]
        private int goldLevel3 = 20;

        [Tooltip("Gold interval at Level 1.")]
        [SerializeField]
        private float goldIntervalLevel1 = 5f;

        [Tooltip("Gold interval at Level 2.")]
        [SerializeField]
        private float goldIntervalLevel2 = 4f;

        [Tooltip("Gold interval at Level 3.")]
        [SerializeField]
        private float goldIntervalLevel3 = 3f;


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


        // =========================================================
        // GOLD GETTERS
        // =========================================================

        public int GetGoldPerTick(int level)
        {
            switch (Mathf.Clamp(level, 1, 3))
            {
                case 1:
                    return Mathf.Max(
                        0,
                        goldLevel1
                    );

                case 2:
                    return Mathf.Max(
                        0,
                        goldLevel2
                    );

                case 3:
                    return Mathf.Max(
                        0,
                        goldLevel3
                    );

                default:
                    return 0;
            }
        }

        public float GetGoldInterval(int level)
        {
            switch (Mathf.Clamp(level, 1, 3))
            {
                case 1:
                    return Mathf.Max(
                        0.1f,
                        goldIntervalLevel1
                    );

                case 2:
                    return Mathf.Max(
                        0.1f,
                        goldIntervalLevel2
                    );

                case 3:
                    return Mathf.Max(
                        0.1f,
                        goldIntervalLevel3
                    );

                default:
                    return 5f;
            }
        }
    }
}