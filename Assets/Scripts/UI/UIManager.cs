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

        [Header("Level Selection")]
        [SerializeField] private System.Collections.Generic.List<LevelData> levels = new System.Collections.Generic.List<LevelData>();

        // Properties for editor setup bypass
        public LevelData LevelDataToPlay { get => levelDataToPlay; set => levelDataToPlay = value; }
        public System.Collections.Generic.List<LevelData> Levels { get => levels; set => levels = value; }

        private GameObject _levelSelectionPanel;

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
            if (levels == null)
            {
                levels = new System.Collections.Generic.List<LevelData>();
            }
            if (levels.Count == 0 && levelDataToPlay != null)
            {
                levels.Add(levelDataToPlay);
            }

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
            EnsureLevelSelectionUI();

            if (_levelSelectionPanel != null && levels != null && levels.Count > 1)
            {
                _levelSelectionPanel.SetActive(true);
            }
            else
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
        }

        private void EnsureLevelSelectionUI()
        {
            if (_levelSelectionPanel != null) return;

            if (mainMenuPanel == null) return;

            // Create Level Selection Panel
            _levelSelectionPanel = new GameObject("LevelSelectionPanel", typeof(RectTransform), typeof(CanvasRenderer));
            _levelSelectionPanel.transform.SetParent(mainMenuPanel.transform, false);

            RectTransform rect = _levelSelectionPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = _levelSelectionPanel.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // Title Text
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform));
            titleGO.transform.SetParent(_levelSelectionPanel.transform, false);
            TextMeshProUGUI titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "SELECT LEVEL";
            titleTxt.fontSize = 46;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.font = TMP_Settings.defaultFontAsset;

            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.9f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(600f, 100f);

            // Container for cards
            GameObject container = new GameObject("LevelsContainer", typeof(RectTransform));
            container.transform.SetParent(_levelSelectionPanel.transform, false);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.25f);
            containerRect.anchorMax = new Vector2(0.9f, 0.75f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 50f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // Generate card for each level
            foreach (var lvl in levels)
            {
                if (lvl == null) continue;

                GameObject card = new GameObject($"Card_{lvl.LevelName}", typeof(RectTransform), typeof(CanvasRenderer));
                card.transform.SetParent(container.transform, false);

                Image cardImg = card.AddComponent<Image>();
                cardImg.color = new Color(0.14f, 0.14f, 0.2f, 1f);

                Outline cardOutline = card.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.2f, 0.6f, 1f, 0.3f);
                cardOutline.effectDistance = new Vector2(2f, -2f);

                LayoutElement layoutElement = card.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 320f;
                layoutElement.preferredHeight = 420f;

                VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(20, 20, 25, 25);
                cardLayout.spacing = 15f;
                cardLayout.childAlignment = TextAnchor.UpperCenter;
                cardLayout.childControlHeight = false;
                cardLayout.childControlWidth = true;

                // Level Title
                GameObject nameGO = new GameObject("NameText", typeof(RectTransform));
                nameGO.transform.SetParent(card.transform, false);
                TextMeshProUGUI nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
                nameTxt.text = lvl.LevelName.ToUpper();
                nameTxt.fontSize = 24;
                nameTxt.fontStyle = FontStyles.Bold;
                nameTxt.color = Color.white;
                nameTxt.alignment = TextAlignmentOptions.Center;
                nameTxt.font = TMP_Settings.defaultFontAsset;

                // Stats Text
                GameObject statsGO = new GameObject("StatsText", typeof(RectTransform));
                statsGO.transform.SetParent(card.transform, false);
                TextMeshProUGUI statsTxt = statsGO.AddComponent<TextMeshProUGUI>();
                statsTxt.text = $"STARTING GOLD\n<color=#FFD700>{lvl.StartingGold} G</color>\n\nBASE HP\n<color=#FF5555>{lvl.BaseMaxHealth} HP</color>\n\nTOTAL WAVES\n<color=#55FFFF>{lvl.Waves.Count}</color>";
                statsTxt.fontSize = 18;
                statsTxt.lineSpacing = 8f;
                statsTxt.color = new Color(0.85f, 0.85f, 0.9f);
                statsTxt.alignment = TextAlignmentOptions.Center;
                statsTxt.font = TMP_Settings.defaultFontAsset;

                // Spacer
                GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(card.transform, false);
                LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
                spacerLayout.flexibleHeight = 1f;

                // Play/Select Button
                GameObject btnGO = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer));
                btnGO.transform.SetParent(card.transform, false);

                Image btnImg = btnGO.AddComponent<Image>();
                Color btnColor = new Color(0.12f, 0.75f, 0.38f, 1f);
                btnImg.color = btnColor;

                Button playBtn = btnGO.AddComponent<Button>();
                ColorBlock cb = playBtn.colors;
                cb.normalColor = btnColor;
                cb.highlightedColor = new Color(0.15f, 0.85f, 0.45f, 1f);
                cb.pressedColor = new Color(0.08f, 0.65f, 0.3f, 1f);
                playBtn.colors = cb;

                LayoutElement btnLayout = btnGO.AddComponent<LayoutElement>();
                btnLayout.preferredHeight = 50f;

                GameObject btnTxtGO = new GameObject("Text", typeof(RectTransform));
                btnTxtGO.transform.SetParent(btnGO.transform, false);
                TextMeshProUGUI btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
                btnTxt.text = "SELECT LEVEL";
                btnTxt.fontSize = 18;
                btnTxt.fontStyle = FontStyles.Bold;
                btnTxt.color = Color.white;
                btnTxt.alignment = TextAlignmentOptions.Center;
                btnTxt.font = TMP_Settings.defaultFontAsset;

                RectTransform btnTxtRect = btnTxtGO.GetComponent<RectTransform>();
                btnTxtRect.anchorMin = Vector2.zero;
                btnTxtRect.anchorMax = Vector2.one;
                btnTxtRect.offsetMin = Vector2.zero;
                btnTxtRect.offsetMax = Vector2.zero;

                LevelData targetLvl = lvl;
                playBtn.onClick.AddListener(() => SelectAndPlayLevel(targetLvl));
            }

            // Back Button at the bottom
            GameObject backBtnGO = new GameObject("BackButton", typeof(RectTransform), typeof(CanvasRenderer));
            backBtnGO.transform.SetParent(_levelSelectionPanel.transform, false);

            Image backBtnImg = backBtnGO.AddComponent<Image>();
            Color backBtnColor = new Color(0.35f, 0.35f, 0.4f, 1f);
            backBtnImg.color = backBtnColor;

            Button backBtn = backBtnGO.AddComponent<Button>();
            ColorBlock bcb = backBtn.colors;
            bcb.normalColor = backBtnColor;
            bcb.highlightedColor = new Color(0.42f, 0.42f, 0.48f, 1f);
            bcb.pressedColor = new Color(0.25f, 0.25f, 0.3f, 1f);
            backBtn.colors = bcb;

            RectTransform backBtnRect = backBtnGO.GetComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0.5f, 0.12f);
            backBtnRect.anchorMax = new Vector2(0.5f, 0.12f);
            backBtnRect.pivot = new Vector2(0.5f, 0.5f);
            backBtnRect.anchoredPosition = Vector2.zero;
            backBtnRect.sizeDelta = new Vector2(220f, 50f);

            GameObject backTxtGO = new GameObject("Text", typeof(RectTransform));
            backTxtGO.transform.SetParent(backBtnGO.transform, false);
            TextMeshProUGUI backTxt = backTxtGO.AddComponent<TextMeshProUGUI>();
            backTxt.text = "BACK TO MENU";
            backTxt.fontSize = 18;
            backTxt.fontStyle = FontStyles.Bold;
            backTxt.color = Color.white;
            backTxt.alignment = TextAlignmentOptions.Center;
            backTxt.font = TMP_Settings.defaultFontAsset;

            RectTransform backTxtRect = backTxtGO.GetComponent<RectTransform>();
            backTxtRect.anchorMin = Vector2.zero;
            backTxtRect.anchorMax = Vector2.one;
            backTxtRect.offsetMin = Vector2.zero;
            backTxtRect.offsetMax = Vector2.zero;

            backBtn.onClick.AddListener(() => {
                _levelSelectionPanel.SetActive(false);
            });

            _levelSelectionPanel.SetActive(false);
        }

        private void SelectAndPlayLevel(LevelData levelData)
        {
            if (_levelSelectionPanel != null)
            {
                _levelSelectionPanel.SetActive(false);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevel(levelData);
            }
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(levelData.LevelName);
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
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
