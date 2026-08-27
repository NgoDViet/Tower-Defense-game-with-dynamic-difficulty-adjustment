using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Global difficulty/challenge settings.
    /// UIManager is the only UI that controls these settings.
    /// </summary>
    public static class DifficultyManager
    {
        private static DifficultyMode _difficulty = DifficultyMode.Normal;
        private static int _enemyCountMultiplier = 1;
        private static float _timeLimitMinutes = 0f;
        private static bool _oneLifeMode = false;

        public static DifficultyMode Difficulty => _difficulty;
        public static int EnemyCountMultiplier => _enemyCountMultiplier;
        public static float TimeLimitMinutes => _timeLimitMinutes;
        public static bool OneLifeMode => _oneLifeMode;

        public static string DifficultyName
        {
            get
            {
                switch (_difficulty)
                {
                    case DifficultyMode.Normal: return "Normal";
                    case DifficultyMode.NormalPlus: return "Normal+";
                    case DifficultyMode.Hard: return "Hard";
                    case DifficultyMode.Hell: return "Hell";
                    default: return "Normal";
                }
            }
        }

        public static float HealthMultiplier
        {
            get
            {
                switch (_difficulty)
                {
                    case DifficultyMode.Normal: return 1f;
                    case DifficultyMode.NormalPlus: return 1.5f;
                    case DifficultyMode.Hard: return 2.5f;
                    case DifficultyMode.Hell: return 4f;
                    default: return 1f;
                }
            }
        }

        public static float SpeedMultiplier
        {
            get
            {
                switch (_difficulty)
                {
                    case DifficultyMode.Normal: return 1f;
                    case DifficultyMode.NormalPlus: return 1.15f;
                    case DifficultyMode.Hard: return 1.3f;
                    case DifficultyMode.Hell: return 1.5f;
                    default: return 1f;
                }
            }
        }

        public static void SetDifficulty(DifficultyMode mode)
        {
            _difficulty = mode;
            Debug.Log($"[DifficultyManager] Difficulty = {DifficultyName} | HP x{HealthMultiplier} | Speed x{SpeedMultiplier}");
        }

        public static void SetEnemyCountMultiplier(int multiplier)
        {
            _enemyCountMultiplier = Mathf.Clamp(multiplier, 1, 5);
        }

        public static void SetTimeLimitMinutes(float minutes)
        {
            _timeLimitMinutes = Mathf.Max(0f, minutes);
        }

        public static void SetOneLifeMode(bool enabled)
        {
            _oneLifeMode = enabled;
        }

        public static string GetTimeDescription()
        {
            if (_timeLimitMinutes <= 0f)
                return "Không giới hạn";

            if (Mathf.Approximately(_timeLimitMinutes, Mathf.Round(_timeLimitMinutes)))
                return $"{Mathf.RoundToInt(_timeLimitMinutes)} phút";

            return $"{_timeLimitMinutes:0.##} phút";
        }

        public static string GetChallengeDescription()
        {
            return $"Độ khó: {DifficultyName} | Quái x{EnemyCountMultiplier} | Thời gian: {GetTimeDescription()} | Mạng: {(OneLifeMode ? "1 mạng" : "Nhiều mạng")}";
        }

        public static void ResetToDefaults()
        {
            _difficulty = DifficultyMode.Normal;
            _enemyCountMultiplier = 1;
            _timeLimitMinutes = 0f;
            _oneLifeMode = false;
        }
    }
}
