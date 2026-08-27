using UnityEngine;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "NewWaveData", menuName = "Tower Defense/Wave Data", order = 3)]
    public class WaveData : ScriptableObject
    {
        [Header("Basic Enemy Setup")]
        [SerializeField] private int basicCount = 5;
        [SerializeField] private float basicSpawnInterval = 1.0f;

        [Header("Fast Enemy Setup")]
        [SerializeField] private int fastCount = 0;
        [SerializeField] private float fastSpawnInterval = 1.0f;

        [Header("Tank Enemy Setup")]
        [SerializeField] private int tankCount = 0;
        [SerializeField] private float tankSpawnInterval = 1.0f;

        [Header("Armor Enemy Setup")]
        [SerializeField] private int armorCount = 0;
        [SerializeField] private float armorSpawnInterval = 1.0f;

        // Public Getters and Setters
        public int BasicCount { get => basicCount; set => basicCount = value; }
        public float BasicSpawnInterval { get => basicSpawnInterval; set => basicSpawnInterval = value; }

        public int FastCount { get => fastCount; set => fastCount = value; }
        public float FastSpawnInterval { get => fastSpawnInterval; set => fastSpawnInterval = value; }

        public int TankCount { get => tankCount; set => tankCount = value; }
        public float TankSpawnInterval { get => tankSpawnInterval; set => tankSpawnInterval = value; }

        public int ArmorCount { get => armorCount; set => armorCount = value; }
        public float ArmorSpawnInterval { get => armorSpawnInterval; set => armorSpawnInterval = value; }
    }
}
