using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Global difficulty manager.
    /// Controls enemy HP and Speed multipliers.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
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
        // SET DIFFICULTY
        // =========================================================

        public static void SetDifficulty(DifficultyMode difficulty)
        {
            currentDifficulty = difficulty;

            Debug.Log(
                $"[DifficultyManager] " +
                $"Difficulty = {DifficultyName} | " +
                $"HP x{HealthMultiplier} | " +
                $"Speed x{SpeedMultiplier}"
            );
        }

        // =========================================================
        // APPLY HP
        // =========================================================

        public static int ApplyHealth(int baseHealth)
        {
            return Mathf.RoundToInt(
                baseHealth * HealthMultiplier
            );
        }

        // =========================================================
        // APPLY SPEED
        // =========================================================

        public static float ApplySpeed(float baseSpeed)
        {
            return baseSpeed * SpeedMultiplier;
        }
    }
}