using UnityEngine;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data", order = 2)]
    public class TowerData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string towerName = "Basic Tower";
        [SerializeField] private Sprite towerSprite;

        [Header("Costs & Requirements")]
        [SerializeField] private int cost = 100;

        [Header("Gameplay Stats")]
        [Tooltip("The targeting radius of the tower in units.")]
        [SerializeField] private float range = 5f;

        [Tooltip("Number of attacks per second.")]
        [SerializeField] private float fireRate = 1f;

        [SerializeField] private int damage = 2;

        [Header("Projectile Settings")]
        [Tooltip("Prefab of the projectile to shoot.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Speed of the projectile in units per second.")]
        [SerializeField] private float projectileSpeed = 8f;

        // Getters
        public string TowerName => towerName;
        public Sprite TowerSprite => towerSprite;
        public int Cost => cost;
        public float Range => range;
        public float FireRate => fireRate;
        public int Damage => damage;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
    }
}
