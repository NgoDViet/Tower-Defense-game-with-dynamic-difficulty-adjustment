using UnityEngine;
using System.Collections.Generic;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Tower Defense/Level Data", order = 4)]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelName = "Level 1";

        [Header("Starting Stats")]
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int baseMaxHealth = 20;

        [Header("Wave Sequence")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        // Getters
        public string LevelName => levelName;
        public int StartingGold => startingGold;
        public int BaseMaxHealth => baseMaxHealth;
        public List<WaveData> Waves => waves;
    }
}
