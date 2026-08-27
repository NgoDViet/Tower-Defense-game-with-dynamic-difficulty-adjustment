using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Global difficulty and challenge settings.
    /// </summary>
    public static class DifficultyManager
    {
        // =========================================================
        // INTERNAL SETTINGS
        // =========================================================

        private static DifficultyMode _difficulty =
            DifficultyMode.Normal;

        private static int _enemyCountMultiplier =
            1;

        private static float _timeLimitMinutes =
            0f;

        private static bool _oneLifeMode =
            false;

        // Passive Gold:
        // true  = +10 G every 5 seconds
        // false = no passive gold
        private static bool _passiveGoldEnabled =
            true;


        // =========================================================
        // PROPERTIES
        // =========================================================

        public static DifficultyMode Difficulty
        {
            get
            {
                return _difficulty;
            }
        }

        public static int EnemyCountMultiplier
        {
            get
            {
                return _enemyCountMultiplier;
            }
        }

        public static float TimeLimitMinutes
        {
            get
            {
                return _timeLimitMinutes;
            }
        }

        public static bool OneLifeMode
        {
            get
            {
                return _oneLifeMode;
            }
        }

        public static bool PassiveGoldEnabled
        {
            get
            {
                return _passiveGoldEnabled;
            }
        }


        // =========================================================
        // DIFFICULTY NAME
        // =========================================================

        public static string DifficultyName
        {
            get
            {
                switch (_difficulty)
                {
                    case DifficultyMode.Normal:
                        return "Normal";

                    case DifficultyMode.NormalPlus:
                        return "Normal+";

                    case DifficultyMode.Hard:
                        return "Hard";

                    case DifficultyMode.Hell:
                        return "Hell";

                    default:
                        return "Normal";
                }
            }
        }


        // =========================================================
        // HEALTH MULTIPLIER
        // =========================================================

        public static float HealthMultiplier
        {
            get
            {
                switch (_difficulty)
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
                switch (_difficulty)
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

        public static void SetDifficulty(
            DifficultyMode mode)
        {
            _difficulty =
                mode;

            Debug.Log(
                "[DifficultyManager] Difficulty = " +
                DifficultyName +
                " | HP x" +
                HealthMultiplier +
                " | Speed x" +
                SpeedMultiplier
            );
        }


        // =========================================================
        // SET ENEMY COUNT
        // =========================================================

        public static void SetEnemyCountMultiplier(
            int multiplier)
        {
            _enemyCountMultiplier =
                Mathf.Clamp(
                    multiplier,
                    1,
                    5
                );

            Debug.Log(
                "[DifficultyManager] Enemy Count = x" +
                _enemyCountMultiplier
            );
        }


        // =========================================================
        // SET TIME LIMIT
        // =========================================================

        public static void SetTimeLimitMinutes(
            float minutes)
        {
            _timeLimitMinutes =
                Mathf.Max(
                    0f,
                    minutes
                );

            Debug.Log(
                "[DifficultyManager] Time Limit = " +
                GetTimeDescription()
            );
        }


        // =========================================================
        // SET ONE LIFE
        // =========================================================

        public static void SetOneLifeMode(
            bool enabled)
        {
            _oneLifeMode =
                enabled;

            Debug.Log(
                "[DifficultyManager] One Life = " +
                (enabled ? "ON" : "OFF")
            );
        }


        // =========================================================
        // SET PASSIVE GOLD
        // =========================================================

        public static void SetPassiveGoldEnabled(
            bool enabled)
        {
            _passiveGoldEnabled =
                enabled;

            Debug.Log(
                "[DifficultyManager] Passive Gold = " +
                (enabled ? "ON" : "OFF")
            );
        }


        // =========================================================
        // TIME DESCRIPTION
        // =========================================================

        public static string GetTimeDescription()
        {
            if (_timeLimitMinutes <= 0f)
            {
                return "Không giới hạn";
            }

            if (
                Mathf.Approximately(
                    _timeLimitMinutes,
                    Mathf.Round(
                        _timeLimitMinutes
                    )
                )
            )
            {
                return
                    Mathf.RoundToInt(
                        _timeLimitMinutes
                    ) +
                    " phút";
            }

            return
                _timeLimitMinutes.ToString(
                    "0.##"
                ) +
                " phút";
        }


        // =========================================================
        // CHALLENGE DESCRIPTION
        // =========================================================

        public static string GetChallengeDescription()
        {
            string lifeText =
                OneLifeMode
                    ? "1 mạng"
                    : "Nhiều mạng";

            string passiveGoldText =
                PassiveGoldEnabled
                    ? "Bật"
                    : "Tắt";

            return
                "Độ khó: " +
                DifficultyName +
                " | Quái x" +
                EnemyCountMultiplier +
                " | Thời gian: " +
                GetTimeDescription() +
                " | Mạng: " +
                lifeText +
                " | Vàng theo thời gian: " +
                passiveGoldText;
        }


        // =========================================================
        // RESET
        // =========================================================

        public static void ResetToDefaults()
        {
            _difficulty =
                DifficultyMode.Normal;

            _enemyCountMultiplier =
                1;

            _timeLimitMinutes =
                0f;

            _oneLifeMode =
                false;

            _passiveGoldEnabled =
                true;
        }
    }
}