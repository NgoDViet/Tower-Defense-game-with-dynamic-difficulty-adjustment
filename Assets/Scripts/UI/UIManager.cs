using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// Coordinates all UI panels and text overlays.
    /// Handles:
    /// Main Menu
    /// Difficulty Selection
    /// Level Selection
    /// Gameplay HUD
    /// Pause
    /// Victory
    /// Defeat
    /// Tower / Enemy information
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // =========================================================
        // UI PANELS
        // =========================================================

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHUDPanel;
        [SerializeField] private GameObject pauseOverlayPanel;
        [SerializeField] private GameObject victoryOverlayPanel;
        [SerializeField] private GameObject defeatOverlayPanel;

        // =========================================================
        // HUD
        // =========================================================

        [Header("HUD Text Elements")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI waveText;

        // =========================================================
        // LEVEL DATA
        // =========================================================

        [Header("Level Data")]
        [SerializeField] private LevelData levelDataToPlay;

        [Header("Level Selection")]
        [SerializeField]
        private List<LevelData> levels =
            new List<LevelData>();

        // =========================================================
        // EDITOR PROPERTIES
        // =========================================================

        public LevelData LevelDataToPlay
        {
            get => levelDataToPlay;
            set => levelDataToPlay = value;
        }

        public List<LevelData> Levels
        {
            get => levels;
            set => levels = value;
        }

        // =========================================================
        // GENERATED UI
        // =========================================================

        private GameObject _difficultySelectionPanel;
        private GameObject _levelSelectionPanel;

        // =========================================================
        // SELECTED OBJECTS
        // =========================================================

        private TowerDefense.Tower.TowerController _selectedTower;
        private TowerDefense.Enemy.EnemyHealth _selectedEnemy;

        private GameObject _infoPanel;
        private TextMeshProUGUI _infoTitleText;
        private TextMeshProUGUI _infoStatsText;

        private GameObject _lvlUpBtnGO;
        private Button _lvlUpBtn;
        private TextMeshProUGUI _lvlUpBtnText;

        // =========================================================
        // UNITY EVENTS
        // =========================================================

        private void OnEnable()
        {
            EventBus<GameStateChangedEvent>.Subscribe(
                OnGameStateChanged
            );

            EventBus<BaseHealthChangedEvent>.Subscribe(
                OnBaseHealthChanged
            );

            EventBus<GoldChangedEvent>.Subscribe(
                OnGoldChanged
            );

            EventBus<WaveStartedEvent>.Subscribe(
                OnWaveStarted
            );
        }

        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(
                OnGameStateChanged
            );

            EventBus<BaseHealthChangedEvent>.Unsubscribe(
                OnBaseHealthChanged
            );

            EventBus<GoldChangedEvent>.Unsubscribe(
                OnGoldChanged
            );

            EventBus<WaveStartedEvent>.Unsubscribe(
                OnWaveStarted
            );
        }

        private void Start()
        {
            if (levels == null)
            {
                levels = new List<LevelData>();
            }

            if (levels.Count == 0 &&
                levelDataToPlay != null)
            {
                levels.Add(levelDataToPlay);
            }

            if (GameManager.Instance != null)
            {
                UpdatePanelVisibility(
                    GameManager.Instance.CurrentState
                );
            }
            else
            {
                UpdatePanelVisibility(
                    GameManager.GameState.MainMenu
                );
            }
        }

        // =========================================================
        // GAME STATE UI
        // =========================================================

        private void UpdatePanelVisibility(
            GameManager.GameState state)
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (gameplayHUDPanel != null)
                gameplayHUDPanel.SetActive(false);

            if (pauseOverlayPanel != null)
                pauseOverlayPanel.SetActive(false);

            if (victoryOverlayPanel != null)
                victoryOverlayPanel.SetActive(false);

            if (defeatOverlayPanel != null)
                defeatOverlayPanel.SetActive(false);

            switch (state)
            {
                case GameManager.GameState.MainMenu:

                    if (mainMenuPanel != null)
                        mainMenuPanel.SetActive(true);

                    break;

                case GameManager.GameState.Playing:

                    if (gameplayHUDPanel != null)
                        gameplayHUDPanel.SetActive(true);

                    break;

                case GameManager.GameState.Pause:

                    if (gameplayHUDPanel != null)
                        gameplayHUDPanel.SetActive(true);

                    if (pauseOverlayPanel != null)
                        pauseOverlayPanel.SetActive(true);

                    break;

                case GameManager.GameState.Victory:

                    if (victoryOverlayPanel != null)
                        victoryOverlayPanel.SetActive(true);

                    break;

                case GameManager.GameState.Defeat:

                    if (defeatOverlayPanel != null)
                        defeatOverlayPanel.SetActive(true);

                    break;
            }
        }

        // =========================================================
        // EVENT BUS
        // =========================================================

        private void OnGameStateChanged(
            GameStateChangedEvent evt)
        {
            UpdatePanelVisibility(evt.NewState);
        }

        private void OnBaseHealthChanged(
            BaseHealthChangedEvent evt)
        {
            if (healthText != null)
            {
                healthText.text =
                    $"HP: {evt.CurrentHealth}/{evt.MaxHealth}";
            }
        }

        private void OnGoldChanged(
            GoldChangedEvent evt)
        {
            if (goldText != null)
            {
                goldText.text =
                    $"Gold: {evt.CurrentGold}";
            }

            UpdateSelectedStatsDisplay();
        }

        private void OnWaveStarted(
            WaveStartedEvent evt)
        {
            if (waveText != null)
            {
                waveText.text =
                    $"Wave: {evt.WaveIndex + 1}/{evt.TotalWaves}";
            }
        }

        // =========================================================
        // PLAY BUTTON
        // =========================================================

        public void OnPlayButtonClicked()
        {
            Debug.Log(
                "[UIManager] PLAY GAME clicked."
            );

            EnsureDifficultySelectionUI();

            if (_difficultySelectionPanel == null)
            {
                Debug.LogError(
                    "[UIManager] Difficulty Selection Panel could not be created."
                );

                return;
            }

            // Hide main menu
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            // Hide level selection
            if (_levelSelectionPanel != null)
            {
                _levelSelectionPanel.SetActive(false);
            }

            // Show difficulty selection
            _difficultySelectionPanel.SetActive(true);

            Debug.Log(
                "[UIManager] Difficulty Selection opened."
            );
        }

        // =========================================================
        // DIFFICULTY SELECTION
        // =========================================================

        private void EnsureDifficultySelectionUI()
        {
            if (_difficultySelectionPanel != null)
                return;

            Transform parent =
                mainMenuPanel != null
                ? mainMenuPanel.transform.parent
                : transform;

            _difficultySelectionPanel =
                new GameObject(
                    "DifficultySelectionPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            _difficultySelectionPanel.transform.SetParent(
                parent,
                false
            );

            RectTransform panelRect =
                _difficultySelectionPanel
                    .GetComponent<RectTransform>();

            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage =
                _difficultySelectionPanel
                    .GetComponent<Image>();

            panelImage.color =
                new Color(
                    0.05f,
                    0.05f,
                    0.08f,
                    0.98f
                );

            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            CreateText(
                _difficultySelectionPanel.transform,
                "DifficultyTitle",
                "SELECT DIFFICULTY",
                46,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.85f),
                new Vector2(0.5f, 0.85f),
                new Vector2(700f, 100f)
            );

            // -----------------------------------------------------
            // DESCRIPTION
            // -----------------------------------------------------

            CreateText(
                _difficultySelectionPanel.transform,
                "DifficultyDescription",
                "Choose your challenge",
                22,
                FontStyles.Normal,
                new Color(
                    0.75f,
                    0.75f,
                    0.8f
                ),
                new Vector2(0.5f, 0.76f),
                new Vector2(0.5f, 0.76f),
                new Vector2(600f, 60f)
            );

            // -----------------------------------------------------
            // BUTTON CONTAINER
            // -----------------------------------------------------

            GameObject container =
                new GameObject(
                    "DifficultyButtons",
                    typeof(RectTransform)
                );

            container.transform.SetParent(
                _difficultySelectionPanel.transform,
                false
            );

            RectTransform containerRect =
                container.GetComponent<RectTransform>();

            containerRect.anchorMin =
                new Vector2(0.5f, 0.35f);

            containerRect.anchorMax =
                new Vector2(0.5f, 0.65f);

            containerRect.pivot =
                new Vector2(0.5f, 0.5f);

            containerRect.anchoredPosition =
                Vector2.zero;

            containerRect.sizeDelta =
                new Vector2(1100f, 350f);

            HorizontalLayoutGroup layout =
                container.AddComponent<
                    HorizontalLayoutGroup
                >();

            layout.spacing = 25f;

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlWidth = true;
            layout.childControlHeight = true;

            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // -----------------------------------------------------
            // NORMAL
            // -----------------------------------------------------

            CreateDifficultyButton(
                container.transform,
                "NORMAL",
                "HP ×1\nSPEED ×1",
                DifficultyMode.Normal
            );

            // -----------------------------------------------------
            // NORMAL+
            // -----------------------------------------------------

            CreateDifficultyButton(
                container.transform,
                "NORMAL+",
                "HP ×1.5\nSPEED ×1.15",
                DifficultyMode.NormalPlus
            );

            // -----------------------------------------------------
            // HARD
            // -----------------------------------------------------

            CreateDifficultyButton(
                container.transform,
                "HARD",
                "HP ×2.5\nSPEED ×1.3",
                DifficultyMode.Hard
            );

            // -----------------------------------------------------
            // HELL
            // -----------------------------------------------------

            CreateDifficultyButton(
                container.transform,
                "HELL",
                "HP ×4\nSPEED ×1.5",
                DifficultyMode.Hell
            );

            // -----------------------------------------------------
            // BACK BUTTON
            // -----------------------------------------------------

            GameObject back =
                CreateSimpleButton(
                    _difficultySelectionPanel.transform,
                    "BACK TO MENU",
                    new Vector2(
                        0.5f,
                        0.12f
                    ),
                    new Vector2(220f, 55f)
                );

            Button backButton =
                back.GetComponent<Button>();

            backButton.onClick.AddListener(
                OnDifficultyBackButtonClicked
            );

            _difficultySelectionPanel.SetActive(false);
        }

        private void CreateDifficultyButton(
            Transform parent,
            string title,
            string description,
            DifficultyMode difficulty)
        {
            GameObject buttonObject =
                new GameObject(
                    title + "Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            buttonObject.transform.SetParent(
                parent,
                false
            );

            LayoutElement layoutElement =
                buttonObject.AddComponent<
                    LayoutElement
                >();

            layoutElement.preferredWidth = 240f;
            layoutElement.preferredHeight = 280f;

            Image image =
                buttonObject.GetComponent<Image>();

            Color buttonColor =
                new Color(
                    0.12f,
                    0.15f,
                    0.22f,
                    1f
                );

            image.color = buttonColor;

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                buttonColor;

            colors.highlightedColor =
                new Color(
                    0.2f,
                    0.35f,
                    0.55f,
                    1f
                );

            colors.pressedColor =
                new Color(
                    0.08f,
                    0.12f,
                    0.18f,
                    1f
                );

            button.colors = colors;

            // Title
            GameObject titleObject =
                new GameObject(
                    "Title",
                    typeof(RectTransform)
                );

            titleObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            TextMeshProUGUI titleText =
                titleObject.AddComponent<
                    TextMeshProUGUI
                >();

            titleText.text = title;
            titleText.fontSize = 28;
            titleText.fontStyle =
                FontStyles.Bold;

            titleText.color =
                Color.white;

            titleText.alignment =
                TextAlignmentOptions.Center;

            RectTransform titleRect =
                titleObject.GetComponent<
                    RectTransform
                >();

            titleRect.anchorMin =
                new Vector2(0.05f, 0.55f);

            titleRect.anchorMax =
                new Vector2(0.95f, 0.9f);

            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Description
            GameObject descriptionObject =
                new GameObject(
                    "Description",
                    typeof(RectTransform)
                );

            descriptionObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            TextMeshProUGUI descriptionText =
                descriptionObject.AddComponent<
                    TextMeshProUGUI
                >();

            descriptionText.text =
                description;

            descriptionText.fontSize = 18;

            descriptionText.color =
                new Color(
                    0.8f,
                    0.8f,
                    0.85f
                );

            descriptionText.alignment =
                TextAlignmentOptions.Center;

            RectTransform descriptionRect =
                descriptionObject.GetComponent<
                    RectTransform
                >();

            descriptionRect.anchorMin =
                new Vector2(0.05f, 0.15f);

            descriptionRect.anchorMax =
                new Vector2(0.95f, 0.55f);

            descriptionRect.offsetMin =
                Vector2.zero;

            descriptionRect.offsetMax =
                Vector2.zero;

            // Click
            button.onClick.AddListener(
                () =>
                {
                    SelectDifficulty(
                        difficulty
                    );
                }
            );
        }

        private void SelectDifficulty(
            DifficultyMode difficulty)
        {
            DifficultyManager.SetDifficulty(
                difficulty
            );

            Debug.Log(
                $"[UIManager] Selected difficulty: " +
                $"{DifficultyManager.DifficultyName}"
            );

            // Hide difficulty screen
            if (_difficultySelectionPanel != null)
            {
                _difficultySelectionPanel.SetActive(false);
            }

            // Open level selection
            EnsureLevelSelectionUI();

            if (_levelSelectionPanel != null)
            {
                _levelSelectionPanel.SetActive(true);
            }
        }

        private void OnDifficultyBackButtonClicked()
        {
            if (_difficultySelectionPanel != null)
            {
                _difficultySelectionPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        // =========================================================
        // LEVEL SELECTION
        // =========================================================

        private void EnsureLevelSelectionUI()
        {
            if (_levelSelectionPanel != null)
                return;

            if (mainMenuPanel == null)
            {
                Debug.LogError(
                    "[UIManager] Main Menu Panel is missing!"
                );

                return;
            }

            _levelSelectionPanel =
                new GameObject(
                    "LevelSelectionPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            Transform parent =
                mainMenuPanel.transform.parent;

            _levelSelectionPanel.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                _levelSelectionPanel.GetComponent<
                    RectTransform
                >();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img =
                _levelSelectionPanel.GetComponent<Image>();

            img.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.12f,
                    0.98f
                );

            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            CreateText(
                _levelSelectionPanel.transform,
                "TitleText",
                "SELECT LEVEL",
                46,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.9f),
                new Vector2(0.5f, 0.9f),
                new Vector2(600f, 100f)
            );

            // -----------------------------------------------------
            // CURRENT DIFFICULTY
            // -----------------------------------------------------

            CreateText(
                _levelSelectionPanel.transform,
                "SelectedDifficultyText",
                "DIFFICULTY: " +
                DifficultyManager.DifficultyName,
                24,
                FontStyles.Bold,
                new Color(
                    1f,
                    0.8f,
                    0.2f
                ),
                new Vector2(0.5f, 0.82f),
                new Vector2(0.5f, 0.82f),
                new Vector2(600f, 50f)
            );

            // -----------------------------------------------------
            // CONTAINER
            // -----------------------------------------------------

            GameObject container =
                new GameObject(
                    "LevelsContainer",
                    typeof(RectTransform)
                );

            container.transform.SetParent(
                _levelSelectionPanel.transform,
                false
            );

            RectTransform containerRect =
                container.GetComponent<
                    RectTransform
                >();

            containerRect.anchorMin =
                new Vector2(0.1f, 0.25f);

            containerRect.anchorMax =
                new Vector2(0.9f, 0.75f);

            containerRect.offsetMin =
                Vector2.zero;

            containerRect.offsetMax =
                Vector2.zero;

            HorizontalLayoutGroup layout =
                container.AddComponent<
                    HorizontalLayoutGroup
                >();

            layout.spacing = 50f;

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlHeight = true;
            layout.childControlWidth = true;

            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // -----------------------------------------------------
            // LEVEL CARDS
            // -----------------------------------------------------

            foreach (LevelData lvl in levels)
            {
                if (lvl == null)
                    continue;

                CreateLevelCard(
                    container.transform,
                    lvl
                );
            }

            // -----------------------------------------------------
            // BACK BUTTON
            // -----------------------------------------------------

            GameObject back =
                CreateSimpleButton(
                    _levelSelectionPanel.transform,
                    "BACK",
                    new Vector2(
                        0.5f,
                        0.12f
                    ),
                    new Vector2(
                        220f,
                        50f
                    )
                );

            Button backButton =
                back.GetComponent<Button>();

            backButton.onClick.AddListener(
                () =>
                {
                    _levelSelectionPanel.SetActive(false);

                    EnsureDifficultySelectionUI();

                    _difficultySelectionPanel.SetActive(true);
                }
            );

            _levelSelectionPanel.SetActive(false);
        }

        private void CreateLevelCard(
            Transform parent,
            LevelData lvl)
        {
            GameObject card =
                new GameObject(
                    $"Card_{lvl.LevelName}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            card.transform.SetParent(
                parent,
                false
            );

            Image cardImage =
                card.GetComponent<Image>();

            cardImage.color =
                new Color(
                    0.14f,
                    0.14f,
                    0.2f,
                    1f
                );

            LayoutElement element =
                card.AddComponent<
                    LayoutElement
                >();

            element.preferredWidth = 320f;
            element.preferredHeight = 420f;

            VerticalLayoutGroup layout =
                card.AddComponent<
                    VerticalLayoutGroup
                >();

            layout.padding =
                new RectOffset(
                    20,
                    20,
                    25,
                    25
                );

            layout.spacing = 15f;

            layout.childAlignment =
                TextAnchor.UpperCenter;

            layout.childControlHeight = false;
            layout.childControlWidth = true;

            // Level Name
            GameObject nameObject =
                new GameObject(
                    "NameText",
                    typeof(RectTransform)
                );

            nameObject.transform.SetParent(
                card.transform,
                false
            );

            TextMeshProUGUI nameText =
                nameObject.AddComponent<
                    TextMeshProUGUI
                >();

            nameText.text =
                lvl.LevelName.ToUpper();

            nameText.fontSize = 24;
            nameText.fontStyle =
                FontStyles.Bold;

            nameText.color =
                Color.white;

            nameText.alignment =
                TextAlignmentOptions.Center;

            // Stats
            GameObject statsObject =
                new GameObject(
                    "StatsText",
                    typeof(RectTransform)
                );

            statsObject.transform.SetParent(
                card.transform,
                false
            );

            TextMeshProUGUI statsText =
                statsObject.AddComponent<
                    TextMeshProUGUI
                >();

            statsText.text =
                $"STARTING GOLD\n" +
                $"<color=#FFD700>{lvl.StartingGold} G</color>\n\n" +
                $"BASE HP\n" +
                $"<color=#FF5555>{lvl.BaseMaxHealth} HP</color>\n\n" +
                $"TOTAL WAVES\n" +
                $"<color=#55FFFF>{lvl.Waves.Count}</color>";

            statsText.fontSize = 18;
            statsText.lineSpacing = 8f;

            statsText.color =
                new Color(
                    0.85f,
                    0.85f,
                    0.9f
                );

            statsText.alignment =
                TextAlignmentOptions.Center;

            // Spacer
            GameObject spacer =
                new GameObject(
                    "Spacer",
                    typeof(RectTransform)
                );

            spacer.transform.SetParent(
                card.transform,
                false
            );

            LayoutElement spacerElement =
                spacer.AddComponent<
                    LayoutElement
                >();

            spacerElement.flexibleHeight = 1f;

            // Select Button
            GameObject buttonObject =
                new GameObject(
                    "PlayButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            buttonObject.transform.SetParent(
                card.transform,
                false
            );

            Image buttonImage =
                buttonObject.GetComponent<Image>();

            Color buttonColor =
                new Color(
                    0.12f,
                    0.75f,
                    0.38f,
                    1f
                );

            buttonImage.color =
                buttonColor;

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                buttonColor;

            colors.highlightedColor =
                new Color(
                    0.15f,
                    0.85f,
                    0.45f,
                    1f
                );

            colors.pressedColor =
                new Color(
                    0.08f,
                    0.65f,
                    0.3f,
                    1f
                );

            button.colors = colors;

            LayoutElement buttonElement =
                buttonObject.AddComponent<
                    LayoutElement
                >();

            buttonElement.preferredHeight = 50f;

            GameObject buttonTextObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform)
                );

            buttonTextObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            TextMeshProUGUI buttonText =
                buttonTextObject.AddComponent<
                    TextMeshProUGUI
                >();

            buttonText.text =
                "SELECT LEVEL";

            buttonText.fontSize = 18;

            buttonText.fontStyle =
                FontStyles.Bold;

            buttonText.color =
                Color.white;

            buttonText.alignment =
                TextAlignmentOptions.Center;

            RectTransform textRect =
                buttonTextObject.GetComponent<
                    RectTransform
                >();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            LevelData targetLevel =
                lvl;

            button.onClick.AddListener(
                () =>
                {
                    SelectAndPlayLevel(
                        targetLevel
                    );
                }
            );
        }

        // =========================================================
        // START LEVEL
        // =========================================================

        private void SelectAndPlayLevel(
            LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[UIManager] Selected LevelData is NULL!"
                );

                return;
            }

            Debug.Log(
                $"[UIManager] Starting level: " +
                $"{levelData.LevelName}"
            );

            Debug.Log(
                $"[UIManager] Difficulty: " +
                $"{DifficultyManager.DifficultyName}"
            );

            Debug.Log(
                $"[UIManager] HP multiplier: " +
                $"x{DifficultyManager.HealthMultiplier}"
            );

            Debug.Log(
                $"[UIManager] Speed multiplier: " +
                $"x{DifficultyManager.SpeedMultiplier}"
            );

            if (_difficultySelectionPanel != null)
            {
                _difficultySelectionPanel.SetActive(false);
            }

            if (_levelSelectionPanel != null)
            {
                _levelSelectionPanel.SetActive(false);
            }

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevel(
                    levelData
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager
                    .LoadScene(levelData.LevelName);
            }
        }

        // =========================================================
        // SIMPLE BUTTON CREATOR
        // =========================================================

        private GameObject CreateSimpleButton(
            Transform parent,
            string text,
            Vector2 anchor,
            Vector2 size)
        {
            GameObject buttonObject =
                new GameObject(
                    text.Replace(" ", "") + "Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            buttonObject.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                buttonObject.GetComponent<
                    RectTransform
                >();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta = size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.35f,
                    0.35f,
                    0.4f,
                    1f
                );

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                new Color(
                    0.35f,
                    0.35f,
                    0.4f,
                    1f
                );

            colors.highlightedColor =
                new Color(
                    0.45f,
                    0.45f,
                    0.5f,
                    1f
                );

            colors.pressedColor =
                new Color(
                    0.25f,
                    0.25f,
                    0.3f,
                    1f
                );

            button.colors = colors;

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform)
                );

            textObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            TextMeshProUGUI tmp =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();

            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle =
                FontStyles.Bold;

            tmp.color =
                Color.white;

            tmp.alignment =
                TextAlignmentOptions.Center;

            RectTransform textRect =
                textObject.GetComponent<
                    RectTransform
                >();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            return buttonObject;
        }

        // =========================================================
        // TEXT CREATOR
        // =========================================================

        private TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            string text,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size)
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform)
                );

            textObject.transform.SetParent(
                parent,
                false
            );

            TextMeshProUGUI tmp =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;

            tmp.alignment =
                TextAlignmentOptions.Center;

            RectTransform rect =
                textObject.GetComponent<
                    RectTransform
                >();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta = size;

            return tmp;
        }

        // =========================================================
        // GAMEPLAY BUTTONS
        // =========================================================

        public void OnResumeButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        public void OnPauseButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        public void OnRestartButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        public void OnReturnToMainMenuButtonClicked()
        {
            Time.timeScale = 1f;

            UnityEngine.SceneManagement.SceneManager
                .LoadScene("MainMenu");
        }

        public void OnQuitButtonClicked()
        {
            Debug.Log(
                "[UIManager] Quitting Game..."
            );

            Application.Quit();
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing)
            {
                if (_infoPanel != null &&
                    _infoPanel.activeSelf)
                {
                    _infoPanel.SetActive(false);
                }

                return;
            }

            UpdateSelectedStatsDisplay();

            bool leftClick = false;
            Vector2 mouseScreenPos =
                Vector2.zero;

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current
                    .leftButton.wasPressedThisFrame)
                {
                    leftClick = true;

                    mouseScreenPos =
                        UnityEngine.InputSystem.Mouse.current
                        .position.ReadValue();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    leftClick = true;
                    mouseScreenPos =
                        Input.mousePosition;
                }
            }

            if (!leftClick)
                return;

            if (IsPointerOverInteractiveUI(
                mouseScreenPos))
            {
                return;
            }

            if (Camera.main == null)
                return;

            Vector3 worldPos =
                Camera.main.ScreenToWorldPoint(
                    new Vector3(
                        mouseScreenPos.x,
                        mouseScreenPos.y,
                        Camera.main.nearClipPlane
                    )
                );

            Vector2 worldPos2D =
                new Vector2(
                    worldPos.x,
                    worldPos.y
                );

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    worldPos2D,
                    0.6f
                );

            Collider2D closestHit = null;

            float closestDist =
                float.MaxValue;

            TowerDefense.Tower.TowerController
                targetTower = null;

            TowerDefense.Enemy.EnemyHealth
                targetEnemy = null;

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                TowerDefense.Tower.TowerController
                    tower =
                    hit.GetComponent<
                        TowerDefense.Tower.TowerController
                    >();

                TowerDefense.Enemy.EnemyHealth
                    enemy =
                    hit.GetComponent<
                        TowerDefense.Enemy.EnemyHealth
                    >();

                if (tower != null ||
                    enemy != null)
                {
                    float dist =
                        Vector2.Distance(
                            worldPos2D,
                            hit.transform.position
                        );

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestHit = hit;
                        targetTower = tower;
                        targetEnemy = enemy;
                    }
                }
            }

            if (closestHit != null)
            {
                if (targetTower != null)
                {
                    SelectTower(targetTower);
                }
                else if (targetEnemy != null)
                {
                    SelectEnemy(targetEnemy);
                }
            }
            else
            {
                Deselect();
            }
        }

        // =========================================================
        // INFO PANEL
        // =========================================================

        private void SelectTower(
            TowerDefense.Tower.TowerController tower)
        {
            _selectedTower = tower;
            _selectedEnemy = null;

            EnsureInfoPanel();

            _infoPanel.SetActive(true);

            UpdateSelectedStatsDisplay();
        }

        private void SelectEnemy(
            TowerDefense.Enemy.EnemyHealth enemy)
        {
            _selectedEnemy = enemy;
            _selectedTower = null;

            EnsureInfoPanel();

            _infoPanel.SetActive(true);

            UpdateSelectedStatsDisplay();
        }

        private void Deselect()
        {
            _selectedTower = null;
            _selectedEnemy = null;

            if (_infoPanel != null)
            {
                _infoPanel.SetActive(false);
            }
        }

        private void UpdateSelectedStatsDisplay()
        {
            if (_infoPanel == null ||
                !_infoPanel.activeSelf)
                return;

            if (_selectedTower != null)
            {
                if (_selectedTower == null ||
                    _selectedTower.gameObject == null)
                {
                    Deselect();
                    return;
                }

                TowerData data =
                    _selectedTower.TowerData;

                string name =
                    data != null
                    ? data.TowerName
                    : "Tower";

                float fireRate =
                    data != null
                    ? data.FireRate
                    : 0f;

                if (_infoTitleText != null)
                {
                    _infoTitleText.text =
                        name.ToUpper() +
                        $" (LVL {_selectedTower.CurrentLevel})";
                }

                if (_infoStatsText != null)
                {
                    _infoStatsText.text =
                        $"DAMAGE: <color=#FFD700>" +
                        $"{_selectedTower.CurrentDamage}" +
                        $"</color>\n\n" +

                        $"FIRE RATE: <color=#55FFFF>" +
                        $"{fireRate:F1}/s" +
                        $"</color>\n\n" +

                        $"RANGE: <color=#55FF55>" +
                        $"{_selectedTower.CurrentRange:F1}" +
                        $"</color>";
                }

                if (_lvlUpBtnGO != null)
                {
                    if (_selectedTower.CurrentLevel <
                        _selectedTower.MaxLevel)
                    {
                        _lvlUpBtnGO.SetActive(true);

                        int cost =
                            _selectedTower.UpgradeCost;

                        bool canAfford =
                            GameManager.Instance == null ||
                            GameManager.Instance.CurrentGold >= cost;

                        if (_lvlUpBtnText != null)
                        {
                            _lvlUpBtnText.text =
                                $"UPGRADE ({cost} G)";
                        }

                        if (_lvlUpBtn != null)
                        {
                            _lvlUpBtn.interactable =
                                canAfford;
                        }
                    }
                    else
                    {
                        _lvlUpBtnGO.SetActive(false);
                    }
                }
            }
            else if (_selectedEnemy != null)
            {
                if (_selectedEnemy == null ||
                    _selectedEnemy.gameObject == null ||
                    _selectedEnemy.IsDead)
                {
                    Deselect();
                    return;
                }

                string name =
                    _selectedEnemy.EnemyData != null
                    ? _selectedEnemy.EnemyData.EnemyName
                    : "Enemy";

                int hp =
                    _selectedEnemy.CurrentHealth;

                int maxHp =
                    _selectedEnemy.MaxHealth;

                float speed =
                    _selectedEnemy.MoveSpeed;

                int armor =
                    _selectedEnemy.Armor;

                int attack =
                    _selectedEnemy.Attack;

                if (_infoTitleText != null)
                {
                    _infoTitleText.text =
                        name.ToUpper();
                }

                if (_infoStatsText != null)
                {
                    _infoStatsText.text =
                        $"HP: <color=#FF5555>" +
                        $"{hp}/{maxHp}" +
                        $"</color>\n\n" +

                        $"SPEED: <color=#55FF55>" +
                        $"{speed:F1}" +
                        $"</color>\n\n" +

                        $"ARMOR: <color=#AAAAAA>" +
                        $"{armor}" +
                        $"</color>\n\n" +

                        $"DAMAGE TO BASE: " +
                        $"<color=#FF5555>" +
                        $"{attack}" +
                        $"</color>";
                }

                if (_lvlUpBtnGO != null)
                {
                    _lvlUpBtnGO.SetActive(false);
                }
            }
            else
            {
                Deselect();
            }
        }

        // =========================================================
        // INFO PANEL CREATION
        // =========================================================

        private void EnsureInfoPanel()
        {
            if (_infoPanel != null)
                return;

            Transform parent =
                gameplayHUDPanel != null
                ? gameplayHUDPanel.transform
                : transform;

            _infoPanel =
                new GameObject(
                    "InfoPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            _infoPanel.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                _infoPanel.GetComponent<
                    RectTransform
                >();

            rect.anchorMin =
                new Vector2(1f, 0.5f);

            rect.anchorMax =
                new Vector2(1f, 0.5f);

            rect.pivot =
                new Vector2(1f, 0.5f);

            rect.anchoredPosition =
                new Vector2(
                    -20f,
                    0f
                );

            rect.sizeDelta =
                new Vector2(
                    280f,
                    250f
                );

            Image bg =
                _infoPanel.GetComponent<
                    Image
                >();

            bg.color =
                new Color(
                    0.08f,
                    0.09f,
                    0.15f,
                    0.92f
                );

            // Title
            GameObject title =
                new GameObject(
                    "TitleText",
                    typeof(RectTransform)
                );

            title.transform.SetParent(
                _infoPanel.transform,
                false
            );

            RectTransform titleRect =
                title.GetComponent<
                    RectTransform
                >();

            titleRect.anchorMin =
                new Vector2(0f, 1f);

            titleRect.anchorMax =
                new Vector2(1f, 1f);

            titleRect.pivot =
                new Vector2(0f, 1f);

            titleRect.anchoredPosition =
                new Vector2(
                    15f,
                    -15f
                );

            titleRect.sizeDelta =
                new Vector2(
                    -50f,
                    35f
                );

            _infoTitleText =
                title.AddComponent<
                    TextMeshProUGUI
                >();

            _infoTitleText.fontSize = 18f;
            _infoTitleText.fontStyle =
                FontStyles.Bold;

            _infoTitleText.color =
                Color.white;

            _infoTitleText.alignment =
                TextAlignmentOptions.Left;

            // Stats
            GameObject stats =
                new GameObject(
                    "StatsText",
                    typeof(RectTransform)
                );

            stats.transform.SetParent(
                _infoPanel.transform,
                false
            );

            RectTransform statsRect =
                stats.GetComponent<
                    RectTransform
                >();

            statsRect.anchorMin =
                new Vector2(0f, 0f);

            statsRect.anchorMax =
                new Vector2(1f, 1f);

            statsRect.anchoredPosition =
                new Vector2(
                    0f,
                    -30f
                );

            statsRect.sizeDelta =
                new Vector2(
                    -30f,
                    -80f
                );

            _infoStatsText =
                stats.AddComponent<
                    TextMeshProUGUI
                >();

            _infoStatsText.fontSize = 14f;

            _infoStatsText.color =
                new Color(
                    0.85f,
                    0.85f,
                    0.9f,
                    1f
                );

            _infoStatsText.alignment =
                TextAlignmentOptions.TopLeft;

            // Close
            GameObject close =
                new GameObject(
                    "CloseButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            close.transform.SetParent(
                _infoPanel.transform,
                false
            );

            RectTransform closeRect =
                close.GetComponent<
                    RectTransform
                >();

            closeRect.anchorMin =
                new Vector2(1f, 1f);

            closeRect.anchorMax =
                new Vector2(1f, 1f);

            closeRect.pivot =
                new Vector2(1f, 1f);

            closeRect.anchoredPosition =
                new Vector2(
                    -10f,
                    -10f
                );

            closeRect.sizeDelta =
                new Vector2(
                    25f,
                    25f
                );

            Image closeImage =
                close.GetComponent<Image>();

            closeImage.color =
                new Color(
                    0.8f,
                    0.2f,
                    0.2f,
                    0.8f
                );

            Button closeButton =
                close.GetComponent<Button>();

            closeButton.onClick.AddListener(
                Deselect
            );

            // Upgrade
            _lvlUpBtnGO =
                new GameObject(
                    "UpgradeButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            _lvlUpBtnGO.transform.SetParent(
                _infoPanel.transform,
                false
            );

            RectTransform upgradeRect =
                _lvlUpBtnGO.GetComponent<
                    RectTransform
                >();

            upgradeRect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );

            upgradeRect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );

            upgradeRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );

            upgradeRect.anchoredPosition =
                new Vector2(
                    0f,
                    15f
                );

            upgradeRect.sizeDelta =
                new Vector2(
                    240f,
                    40f
                );

            Image upgradeImage =
                _lvlUpBtnGO.GetComponent<
                    Image
                >();

            upgradeImage.color =
                new Color(
                    0.12f,
                    0.75f,
                    0.38f,
                    1f
                );

            _lvlUpBtn =
                _lvlUpBtnGO.GetComponent<
                    Button
                >();

            _lvlUpBtn.onClick.AddListener(
                OnUpgradeButtonClicked
            );

            GameObject upgradeText =
                new GameObject(
                    "Text",
                    typeof(RectTransform)
                );

            upgradeText.transform.SetParent(
                _lvlUpBtnGO.transform,
                false
            );

            RectTransform upgradeTextRect =
                upgradeText.GetComponent<
                    RectTransform
                >();

            upgradeTextRect.anchorMin =
                Vector2.zero;

            upgradeTextRect.anchorMax =
                Vector2.one;

            upgradeTextRect.offsetMin =
                Vector2.zero;

            upgradeTextRect.offsetMax =
                Vector2.zero;

            _lvlUpBtnText =
                upgradeText.AddComponent<
                    TextMeshProUGUI
                >();

            _lvlUpBtnText.text =
                "UPGRADE";

            _lvlUpBtnText.fontSize = 14f;

            _lvlUpBtnText.fontStyle =
                FontStyles.Bold;

            _lvlUpBtnText.color =
                Color.white;

            _lvlUpBtnText.alignment =
                TextAlignmentOptions.Center;

            _lvlUpBtnGO.SetActive(false);
        }

        // =========================================================
        // UPGRADE
        // =========================================================

        private void OnUpgradeButtonClicked()
        {
            if (_selectedTower != null &&
                GameManager.Instance != null)
            {
                int cost =
                    _selectedTower.UpgradeCost;

                if (_selectedTower.CurrentLevel <
                    _selectedTower.MaxLevel &&
                    GameManager.Instance.TrySpendGold(
                        cost))
                {
                    _selectedTower.LevelUp();

                    UpdateSelectedStatsDisplay();
                }
            }
        }

        // =========================================================
        // UI RAYCAST
        // =========================================================

        private bool IsPointerOverInteractiveUI(
            Vector2 screenPos)
        {
            if (
                UnityEngine.EventSystems
                .EventSystem.current == null)
            {
                return false;
            }

            UnityEngine.EventSystems
                .PointerEventData eventData =
                new UnityEngine.EventSystems
                    .PointerEventData(
                        UnityEngine.EventSystems
                            .EventSystem.current
                    );

            eventData.position =
                screenPos;

            List<UnityEngine.EventSystems
                .RaycastResult> results =
                new List<UnityEngine.EventSystems
                    .RaycastResult>();

            UnityEngine.EventSystems
                .EventSystem.current.RaycastAll(
                    eventData,
                    results
                );

            foreach (var result in results)
            {
                if (result.gameObject == null)
                    continue;

                string name =
                    result.gameObject.name;

                if (name ==
                    "GameplayHUDPanel" ||
                    name ==
                    "Canvas" ||
                    name ==
                    "EventSystem")
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}