using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Global difficulty manager.
    ///
    /// Normal     : HP x1    | Speed x1
    /// NormalPlus : HP x1.5  | Speed x1.15
    /// Hard       : HP x2.5  | Speed x1.3
    /// Hell       : HP x4    | Speed x1.5
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        // =========================================================
        // CURRENT DIFFICULTY
        // =========================================================

        private static DifficultyMode currentDifficulty =
            DifficultyMode.Normal;

        public static DifficultyMode CurrentDifficulty
        {
            get
            {
                return currentDifficulty;
            }
        }

        // =========================================================
        // DIFFICULTY NAME
        // =========================================================

        public static string DifficultyName
        {
            get
            {
                switch (currentDifficulty)
                {
                    case DifficultyMode.Normal:
                        return "Bình thường";

                    case DifficultyMode.NormalPlus:
                        return "Bình thường+";

                    case DifficultyMode.Hard:
                        return "Khó";

                    case DifficultyMode.Hell:
                        return "Địa ngục";

                    default:
                        return "Bình thường";
                }
            }
        }

        // =========================================================
        // HP MULTIPLIER
        // =========================================================

        public static float HealthMultiplier
        {
            get
            {
                switch (currentDifficulty)
                {
                    case DifficultyMode.Normal:
                        return 1f;

                    case DifficultyMode.NormalPlus:
                        return 1.5f;

                    case DifficultyMode.Hard:
                        return 2.5f;

                    case DifficultyMode.Hell:
                        return 4f;

                    default:
                        return 1f;
                }
            }
        }

        // =========================================================
        // SPEED MULTIPLIER
        // =========================================================

        public static float SpeedMultiplier
        {
            get
            {
                switch (currentDifficulty)
                {
                    case DifficultyMode.Normal:
                        return 1f;

                    case DifficultyMode.NormalPlus:
                        return 1.15f;

                    case DifficultyMode.Hard:
                        return 1.3f;

                    case DifficultyMode.Hell:
                        return 1.5f;

                    default:
                        return 1f;
                }
            }
        }

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            Debug.Log(
                $"[DifficultyManager] Started | " +
                $"Difficulty = {DifficultyName} | " +
                $"HP x{HealthMultiplier} | " +
                $"Speed x{SpeedMultiplier}"
            );
        }

        // =========================================================
        // SET DIFFICULTY
        // =========================================================

        public static void SetDifficulty(DifficultyMode difficulty)
        {
            currentDifficulty = difficulty;

            Debug.Log(
                $"[DifficultyManager] Difficulty changed -> " +
                $"{DifficultyName} | " +
                $"HP x{HealthMultiplier} | " +
                $"Speed x{SpeedMultiplier}"
            );
        }

        // =========================================================
        // APPLY MULTIPLIERS
        // =========================================================

        public static int ApplyHealth(int baseHealth)
        {
            return Mathf.RoundToInt(
                baseHealth * HealthMultiplier
            );
        }

        public static float ApplySpeed(float baseSpeed)
        {
            return baseSpeed * SpeedMultiplier;
        }
    }
}