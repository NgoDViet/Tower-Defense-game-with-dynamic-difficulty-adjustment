using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// Coordinates all UI panels and text overlays.
    /// Listens to game state and player stat events via the EventBus to update UI dynamically.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHUDPanel;
        [SerializeField] private GameObject pauseOverlayPanel;
        [SerializeField] private GameObject victoryOverlayPanel;
        [SerializeField] private GameObject defeatOverlayPanel;

        [Header("HUD Text Elements")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI waveText;

        [Header("Level Data (For Main Menu Play Button)")]
        [SerializeField] private LevelData levelDataToPlay;

        // Properties for editor setup bypass
        public LevelData LevelDataToPlay { get => levelDataToPlay; set => levelDataToPlay = value; }

        private void OnEnable()
        {
            // Subscribe to state and stat events
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<BaseHealthChangedEvent>.Subscribe(OnBaseHealthChanged);
            EventBus<GoldChangedEvent>.Subscribe(OnGoldChanged);
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
        }

        private void OnDisable()
        {
            // Unsubscribe to avoid memory leaks
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<BaseHealthChangedEvent>.Unsubscribe(OnBaseHealthChanged);
            EventBus<GoldChangedEvent>.Unsubscribe(OnGoldChanged);
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
        }

        private void Start()
        {
            // Set initial UI state based on GameManager
            if (GameManager.Instance != null)
            {
                UpdatePanelVisibility(GameManager.Instance.CurrentState);
            }
            else
            {
                // Fallback UI initialization
                UpdatePanelVisibility(GameManager.GameState.MainMenu);
            }
        }

        /// <summary>
        /// Updates the visibility of overlay panels depending on the active game state.
        /// </summary>
        private void UpdatePanelVisibility(GameManager.GameState state)
        {
            // Disable all panels first
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
            if (pauseOverlayPanel != null) pauseOverlayPanel.SetActive(false);
            if (victoryOverlayPanel != null) victoryOverlayPanel.SetActive(false);
            if (defeatOverlayPanel != null) defeatOverlayPanel.SetActive(false);

            // Enable matching panels
            switch (state)
            {
                case GameManager.GameState.MainMenu:
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                    break;
                case GameManager.GameState.Playing:
                    if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                    break;
                case GameManager.GameState.Pause:
                    if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                    if (pauseOverlayPanel != null) pauseOverlayPanel.SetActive(true);
                    break;
                case GameManager.GameState.Victory:
                    if (victoryOverlayPanel != null) victoryOverlayPanel.SetActive(true);
                    break;
                case GameManager.GameState.Defeat:
                    if (defeatOverlayPanel != null) defeatOverlayPanel.SetActive(true);
                    break;
            }
        }

        #region Event Subscriptions

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdatePanelVisibility(evt.NewState);
        }

        private void OnBaseHealthChanged(BaseHealthChangedEvent evt)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {evt.CurrentHealth}/{evt.MaxHealth}";
            }
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {evt.CurrentGold}";
            }
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (waveText != null)
            {
                waveText.text = $"Wave: {evt.WaveIndex + 1}/{evt.TotalWaves}";
            }
        }

        #endregion

        #region Public UI Button Callbacks

        /// <summary>
        /// Starts the game level. Linked to the Main Menu Play button.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            if (GameManager.Instance != null && levelDataToPlay != null)
            {
                GameManager.Instance.StartLevel(levelDataToPlay);
            }
            else
            {
                string errorMsg = "[UIManager] Play button clicked, but configuration is missing:";
                if (GameManager.Instance == null) errorMsg += " GameManager.Instance is null!";
                if (levelDataToPlay == null) errorMsg += " levelDataToPlay (LevelData) is null!";
                Debug.LogError(errorMsg);
            }
        }

        /// <summary>
        /// Resumes gameplay from Pause state. Linked to the Resume button.
        /// </summary>
        public void OnResumeButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        /// <summary>
        /// Pauses the game. Linked to the HUD Pause button.
        /// </summary>
        public void OnPauseButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        /// <summary>
        /// Restarts the active level. Linked to the Victory/Defeat/Pause Restart button.
        /// </summary>
        public void OnRestartButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        /// <summary>
        /// Returns to Main Menu state. Linked to the Return button.
        /// </summary>
        public void OnReturnToMainMenuButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }
        }

        /// <summary>
        /// Quits the application. Linked to the Exit button.
        /// </summary>
        public void OnQuitButtonClicked()
        {
            Debug.Log("[UIManager] Quitting Game...");
            Application.Quit();
        }

        #endregion
    }
}
