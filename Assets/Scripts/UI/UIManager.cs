using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// Main UI controller.
    ///
    /// Flow:
    /// Main Menu
    /// -> Difficulty
    /// -> Optional Challenge Settings
    /// -> Level Selection
    /// -> Game
    ///
    /// Challenge modifiers are OPTIONAL and independent:
    /// 1. Enemy count
    /// 2. Time limit
    /// 3. One life
    ///
    /// If a modifier is not checked, it has NO effect.
    /// Difficulty is always kept.
    ///
    /// DifficultySelectionUI.cs is NOT used by this class.
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
        [SerializeField] private TextMeshProUGUI timeText;

        // =========================================================
        // RESULT
        // =========================================================

        [Header("Result Text")]
        [SerializeField] private TextMeshProUGUI victoryTimeText;
        [SerializeField] private TextMeshProUGUI victoryDamageText;
        [SerializeField] private TextMeshProUGUI defeatTimeText;
        [SerializeField] private TextMeshProUGUI defeatDamageText;

        // =========================================================
        // LEVEL DATA
        // =========================================================

        [Header("Level Data")]
        [SerializeField] private LevelData levelDataToPlay;

        [Header("Level Selection")]
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

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
        // GENERATED MENUS
        // =========================================================

        private GameObject _difficultySelectionPanel;
        private GameObject _challengeSettingsPanel;
        private GameObject _levelSelectionPanel;

        // =========================================================
        // CHALLENGE STATE
        // =========================================================

        // These three booleans decide whether a modifier is active.
        private bool _enemyCountEnabled;
        private bool _timeLimitEnabled;
        private bool _oneLifeEnabled;

        // Values are remembered even when the corresponding option is
        // temporarily disabled.
        private int _selectedEnemyMultiplier = 2;
        private float _selectedTimeLimitMinutes = 3f;

        private Toggle _enemyCountToggle;
        private Toggle _timeLimitToggle;
        private Toggle _oneLifeToggle;

        private Button[] _enemyCountButtons;
        private Button[] _timeButtons;
        private TMP_InputField _customTimeInput;
        private Button _customTimeApplyButton;

        private TextMeshProUGUI _enemyCountStatusText;
        private TextMeshProUGUI _timeStatusText;
        private TextMeshProUGUI _oneLifeStatusText;

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
        // EVENTS
        // =========================================================

        private void OnEnable()
        {
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<BaseHealthChangedEvent>.Subscribe(OnBaseHealthChanged);
            EventBus<GoldChangedEvent>.Subscribe(OnGoldChanged);
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
        }

        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<BaseHealthChangedEvent>.Unsubscribe(OnBaseHealthChanged);
            EventBus<GoldChangedEvent>.Unsubscribe(OnGoldChanged);
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
        }

        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            if (levels == null)
                levels = new List<LevelData>();

            if (levels.Count == 0 && levelDataToPlay != null)
                levels.Add(levelDataToPlay);

            if (GameManager.Instance != null)
            {
                UpdatePanelVisibility(GameManager.Instance.CurrentState);
            }
            else
            {
                UpdatePanelVisibility(GameManager.GameState.MainMenu);
            }
        }

        // =========================================================
        // TIME
        // =========================================================

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        // =========================================================
        // PANEL VISIBILITY
        // =========================================================

        private void UpdatePanelVisibility(GameManager.GameState state)
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
        // GAME STATE
        // =========================================================

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdatePanelVisibility(evt.NewState);

            if (GameManager.Instance == null)
                return;

            if (evt.NewState == GameManager.GameState.Victory)
                UpdateVictoryResult();
            else if (evt.NewState == GameManager.GameState.Defeat)
                UpdateDefeatResult();
        }

        // =========================================================
        // VICTORY
        // =========================================================

        private void UpdateVictoryResult()
        {
            string time = FormatTime(GameManager.Instance.PlayTime);
            int damage = GameManager.Instance.TotalDamageDealt;

            if (victoryTimeText != null)
                victoryTimeText.text = $"TIME: {time}";

            if (victoryDamageText != null)
                victoryDamageText.text = $"DAMAGE: {damage:N0}";
        }

        // =========================================================
        // DEFEAT
        // =========================================================

        private void UpdateDefeatResult()
        {
            string time = FormatTime(GameManager.Instance.PlayTime);
            int damage = GameManager.Instance.TotalDamageDealt;

            if (defeatTimeText != null)
                defeatTimeText.text = $"TIME: {time}";

            if (defeatDamageText != null)
                defeatDamageText.text = $"DAMAGE: {damage:N0}";
        }

        // =========================================================
        // BASE HP
        // =========================================================

        private void OnBaseHealthChanged(BaseHealthChangedEvent evt)
        {
            if (healthText != null)
                healthText.text = $"HP: {evt.CurrentHealth}/{evt.MaxHealth}";
        }

        // =========================================================
        // GOLD
        // =========================================================

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            if (goldText != null)
                goldText.text = $"Gold: {evt.CurrentGold}";

            UpdateSelectedStatsDisplay();
        }

        // =========================================================
        // WAVE
        // =========================================================

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (waveText != null)
                waveText.text = $"Wave: {evt.WaveIndex + 1}/{evt.TotalWaves}";
        }

        // =========================================================
        // PLAY
        // =========================================================

        public void OnPlayButtonClicked()
        {
            EnsureDifficultySelectionUI();

            if (_difficultySelectionPanel == null)
            {
                Debug.LogError("[UIManager] Difficulty panel could not be created.");
                return;
            }

            HideAllMenuPanels();

            _difficultySelectionPanel.SetActive(true);
        }

        private void HideAllMenuPanels()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (_difficultySelectionPanel != null)
                _difficultySelectionPanel.SetActive(false);

            if (_challengeSettingsPanel != null)
                _challengeSettingsPanel.SetActive(false);

            if (_levelSelectionPanel != null)
                _levelSelectionPanel.SetActive(false);
        }

        // =========================================================
        // DIFFICULTY UI
        // =========================================================

        private void EnsureDifficultySelectionUI()
        {
            if (_difficultySelectionPanel != null)
                return;

            Transform parent = mainMenuPanel != null
                ? mainMenuPanel.transform.parent
                : transform;

            _difficultySelectionPanel = CreateFullScreenPanel(
                parent,
                "DifficultySelectionPanel",
                new Color(0.035f, 0.035f, 0.07f, 0.99f)
            );

            CreateText(
                _difficultySelectionPanel.transform,
                "DifficultyTitle",
                "SELECT DIFFICULTY",
                46,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.88f),
                new Vector2(0.5f, 0.88f),
                new Vector2(700f, 80f)
            );

            CreateText(
                _difficultySelectionPanel.transform,
                "DifficultyDescription",
                "Choose the base difficulty",
                22,
                FontStyles.Normal,
                new Color(0.75f, 0.75f, 0.8f),
                new Vector2(0.5f, 0.79f),
                new Vector2(0.5f, 0.79f),
                new Vector2(600f, 50f)
            );

            GameObject container = new GameObject(
                "DifficultyButtons",
                typeof(RectTransform)
            );

            container.transform.SetParent(
                _difficultySelectionPanel.transform,
                false
            );

            RectTransform containerRect =
                container.GetComponent<RectTransform>();

            containerRect.anchorMin = new Vector2(0.5f, 0.38f);
            containerRect.anchorMax = new Vector2(0.5f, 0.67f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(1100f, 300f);

            HorizontalLayoutGroup layout =
                container.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateDifficultyButton(
                container.transform,
                "NORMAL",
                "HP ×1\nSPEED ×1",
                DifficultyMode.Normal
            );

            CreateDifficultyButton(
                container.transform,
                "NORMAL+",
                "HP ×1.5\nSPEED ×1.15",
                DifficultyMode.NormalPlus
            );

            CreateDifficultyButton(
                container.transform,
                "HARD",
                "HP ×2.5\nSPEED ×1.3",
                DifficultyMode.Hard
            );

            CreateDifficultyButton(
                container.transform,
                "HELL",
                "HP ×4\nSPEED ×1.5",
                DifficultyMode.Hell
            );

            GameObject back = CreateSimpleButton(
                _difficultySelectionPanel.transform,
                "BACK",
                new Vector2(0.5f, 0.12f),
                new Vector2(220f, 55f)
            );

            back.GetComponent<Button>()
                .onClick.AddListener(OnDifficultyBackButtonClicked);

            _difficultySelectionPanel.SetActive(false);
        }

        private void CreateDifficultyButton(
            Transform parent,
            string title,
            string description,
            DifficultyMode difficulty)
        {
            GameObject buttonObject = new GameObject(
                title + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            buttonObject.transform.SetParent(parent, false);

            LayoutElement element =
                buttonObject.AddComponent<LayoutElement>();

            element.preferredWidth = 250f;
            element.preferredHeight = 270f;

            Image image = buttonObject.GetComponent<Image>();

            Color normal = new Color(0.10f, 0.13f, 0.20f, 1f);
            image.color = normal;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(0.18f, 0.30f, 0.48f, 1f);
            colors.pressedColor = new Color(0.06f, 0.09f, 0.14f, 1f);
            button.colors = colors;

            CreateText(
                buttonObject.transform,
                "Title",
                title,
                29,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.05f, 0.56f),
                new Vector2(0.95f, 0.88f),
                Vector2.zero
            );

            CreateText(
                buttonObject.transform,
                "Description",
                description,
                18,
                FontStyles.Normal,
                new Color(0.82f, 0.82f, 0.88f),
                new Vector2(0.05f, 0.15f),
                new Vector2(0.95f, 0.54f),
                Vector2.zero
            );

            button.onClick.AddListener(
                () => SelectDifficulty(difficulty)
            );
        }

        private void SelectDifficulty(DifficultyMode difficulty)
        {
            DifficultyManager.SetDifficulty(difficulty);

            EnsureChallengeSettingsUI();

            if (_difficultySelectionPanel != null)
                _difficultySelectionPanel.SetActive(false);

            if (_challengeSettingsPanel != null)
                _challengeSettingsPanel.SetActive(true);

            RefreshChallengeUI();
        }

        private void OnDifficultyBackButtonClicked()
        {
            if (_difficultySelectionPanel != null)
                _difficultySelectionPanel.SetActive(false);

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }

        // =========================================================
        // CHALLENGE SETTINGS UI
        // =========================================================

        /// <summary>
        /// Creates the new challenge screen.
        ///
        /// There are three independent checkboxes:
        /// [ ] ENEMY COUNT
        /// [ ] TIME LIMIT
        /// [ ] ONE LIFE
        ///
        /// They can be used in any combination.
        /// </summary>
        private void EnsureChallengeSettingsUI()
        {
            if (_challengeSettingsPanel != null)
                return;

            Transform parent = mainMenuPanel != null
                ? mainMenuPanel.transform.parent
                : transform;

            _challengeSettingsPanel = CreateFullScreenPanel(
                parent,
                "ChallengeSettingsPanel",
                new Color(0.035f, 0.035f, 0.07f, 0.99f)
            );

            CreateText(
                _challengeSettingsPanel.transform,
                "Title",
                "CHALLENGE SETTINGS",
                40,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.91f),
                new Vector2(0.5f, 0.91f),
                new Vector2(800f, 70f)
            );

            CreateText(
                _challengeSettingsPanel.transform,
                "Difficulty",
                "DIFFICULTY: " + DifficultyManager.DifficultyName,
                20,
                FontStyles.Bold,
                new Color(1f, 0.8f, 0.2f),
                new Vector2(0.5f, 0.84f),
                new Vector2(0.5f, 0.84f),
                new Vector2(700f, 45f)
            );

            // -----------------------------------------------------
            // THREE INDEPENDENT MODIFIER CARDS
            // -----------------------------------------------------

            GameObject cards = new GameObject(
                "ModifierCards",
                typeof(RectTransform)
            );

            cards.transform.SetParent(
                _challengeSettingsPanel.transform,
                false
            );

            RectTransform cardsRect =
                cards.GetComponent<RectTransform>();

            cardsRect.anchorMin = new Vector2(0.05f, 0.28f);
            cardsRect.anchorMax = new Vector2(0.95f, 0.77f);
            cardsRect.offsetMin = Vector2.zero;
            cardsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup cardsLayout =
                cards.AddComponent<HorizontalLayoutGroup>();

            cardsLayout.spacing = 25f;
            cardsLayout.padding = new RectOffset(10, 10, 5, 5);
            cardsLayout.childAlignment = TextAnchor.MiddleCenter;
            cardsLayout.childControlWidth = true;
            cardsLayout.childControlHeight = true;
            cardsLayout.childForceExpandWidth = true;
            cardsLayout.childForceExpandHeight = true;

            CreateEnemyCountCard(cards.transform);
            CreateTimeLimitCard(cards.transform);
            CreateOneLifeCard(cards.transform);

            // -----------------------------------------------------
            // BOTTOM BUTTONS
            // -----------------------------------------------------

            GameObject back = CreateSimpleButton(
                _challengeSettingsPanel.transform,
                "BACK",
                new Vector2(0.32f, 0.10f),
                new Vector2(200f, 55f)
            );

            back.GetComponent<Button>()
                .onClick.AddListener(OnChallengeBackClicked);

            GameObject next = CreateSimpleButton(
                _challengeSettingsPanel.transform,
                "NEXT",
                new Vector2(0.68f, 0.10f),
                new Vector2(200f, 55f)
            );

            next.GetComponent<Button>()
                .onClick.AddListener(OnChallengeNextClicked);

            // Start with all optional modifiers OFF.
            _enemyCountEnabled = false;
            _timeLimitEnabled = false;
            _oneLifeEnabled = false;

            _selectedEnemyMultiplier = 2;
            _selectedTimeLimitMinutes = 3f;

            _challengeSettingsPanel.SetActive(false);
        }

        // =========================================================
        // ENEMY COUNT CARD
        // =========================================================

        private void CreateEnemyCountCard(Transform parent)
        {
            GameObject card = CreateModifierCard(
                parent,
                "EnemyCountCard"
            );

            _enemyCountToggle = CreateToggle(
                card.transform,
                "EnableEnemyCount",
                "ENEMY COUNT",
                new Vector2(0f, 0.82f),
                new Vector2(1f, 0.98f)
            );

            _enemyCountStatusText = CreateText(
                card.transform,
                "Status",
                "OFF - normal enemy amount",
                14,
                FontStyles.Normal,
                new Color(0.65f, 0.65f, 0.72f),
                new Vector2(0.05f, 0.68f),
                new Vector2(0.95f, 0.80f),
                Vector2.zero
            );

            GameObject optionContainer = new GameObject(
                "Options",
                typeof(RectTransform)
            );

            optionContainer.transform.SetParent(
                card.transform,
                false
            );

            RectTransform rect =
                optionContainer.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.08f, 0.15f);
            rect.anchorMax = new Vector2(0.92f, 0.66f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GridLayoutGroup grid =
                optionContainer.AddComponent<GridLayoutGroup>();

            grid.cellSize = new Vector2(80f, 45f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;

            _enemyCountButtons = new Button[4];

            _enemyCountButtons[0] = CreateOptionButton(
                optionContainer.transform, "×2",
                () => SelectEnemyMultiplier(2)
            );

            _enemyCountButtons[1] = CreateOptionButton(
                optionContainer.transform, "×3",
                () => SelectEnemyMultiplier(3)
            );

            _enemyCountButtons[2] = CreateOptionButton(
                optionContainer.transform, "×4",
                () => SelectEnemyMultiplier(4)
            );

            _enemyCountButtons[3] = CreateOptionButton(
                optionContainer.transform, "×5",
                () => SelectEnemyMultiplier(5)
            );

            _enemyCountToggle.onValueChanged.AddListener(
                OnEnemyCountToggleChanged
            );
        }

        // =========================================================
        // TIME CARD
        // =========================================================

        private void CreateTimeLimitCard(Transform parent)
{
    GameObject card = CreateModifierCard(
        parent,
        "TimeLimitCard"
    );

    // =========================================================
    // TITLE / TOGGLE
    // =========================================================

    _timeLimitToggle = CreateToggle(
        card.transform,
        "EnableTimeLimit",
        "TIME LIMIT",
        new Vector2(0f, 0.80f),
        new Vector2(1f, 0.98f)
    );

    _timeStatusText = CreateText(
        card.transform,
        "Status",
        "OFF - unlimited time",
        15,
        FontStyles.Normal,
        new Color(0.65f, 0.65f, 0.72f),
        new Vector2(0.05f, 0.67f),
        new Vector2(0.95f, 0.78f),
        Vector2.zero
    );

    // =========================================================
    // PRESET TIME BUTTONS
    // =========================================================

    GameObject optionContainer = new GameObject(
        "TimeOptions",
        typeof(RectTransform)
    );

    optionContainer.transform.SetParent(
        card.transform,
        false
    );

    RectTransform optionRect =
        optionContainer.GetComponent<RectTransform>();

    optionRect.anchorMin = new Vector2(0.06f, 0.37f);
    optionRect.anchorMax = new Vector2(0.94f, 0.61f);
    optionRect.offsetMin = Vector2.zero;
    optionRect.offsetMax = Vector2.zero;

    HorizontalLayoutGroup layout =
        optionContainer.AddComponent<HorizontalLayoutGroup>();

    layout.spacing = 12f;
    layout.padding = new RectOffset(5, 5, 5, 5);
    layout.childAlignment = TextAnchor.MiddleCenter;

    layout.childControlWidth = true;
    layout.childControlHeight = true;

    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = true;

    _timeButtons = new Button[3];

    _timeButtons[0] = CreateLargeTimeButton(
        optionContainer.transform,
        "3 MIN",
        () => SelectTimeLimit(3f)
    );

    _timeButtons[1] = CreateLargeTimeButton(
        optionContainer.transform,
        "5 MIN",
        () => SelectTimeLimit(5f)
    );

    _timeButtons[2] = CreateLargeTimeButton(
        optionContainer.transform,
        "10 MIN",
        () => SelectTimeLimit(10f)
    );

    // =========================================================
    // CUSTOM TIME INPUT
    // =========================================================

    GameObject customRow = new GameObject(
        "CustomTimeRow",
        typeof(RectTransform)
    );

    customRow.transform.SetParent(
        card.transform,
        false
    );

    RectTransform customRect =
        customRow.GetComponent<RectTransform>();

    customRect.anchorMin = new Vector2(0.08f, 0.13f);
    customRect.anchorMax = new Vector2(0.92f, 0.31f);
    customRect.offsetMin = Vector2.zero;
    customRect.offsetMax = Vector2.zero;

    HorizontalLayoutGroup customLayout =
        customRow.AddComponent<HorizontalLayoutGroup>();

    customLayout.spacing = 8f;
    customLayout.padding = new RectOffset(0, 0, 2, 2);
    customLayout.childAlignment = TextAnchor.MiddleCenter;

    customLayout.childControlWidth = false;
    customLayout.childControlHeight = true;

    customLayout.childForceExpandWidth = false;
    customLayout.childForceExpandHeight = true;

    // =========================================================
    // INPUT FIELD
    // =========================================================

    GameObject inputObject = new GameObject(
        "CustomMinutesInput",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image),
        typeof(TMP_InputField)
    );

    inputObject.transform.SetParent(
        customRow.transform,
        false
    );

    LayoutElement inputElement =
        inputObject.AddComponent<LayoutElement>();

    inputElement.preferredWidth = 145f;
    inputElement.preferredHeight = 55f;
    inputElement.minWidth = 145f;
    inputElement.minHeight = 55f;

    Image inputImage =
        inputObject.GetComponent<Image>();

    inputImage.color =
        new Color(0.10f, 0.12f, 0.19f, 1f);

    TMP_InputField input =
        inputObject.GetComponent<TMP_InputField>();

    _customTimeInput = input;

    // =========================================================
    // TEXT AREA
    // =========================================================

    GameObject textArea = new GameObject(
        "Text Area",
        typeof(RectTransform),
        typeof(RectMask2D)
    );

    textArea.transform.SetParent(
        inputObject.transform,
        false
    );

    RectTransform textAreaRect =
        textArea.GetComponent<RectTransform>();

    textAreaRect.anchorMin = Vector2.zero;
    textAreaRect.anchorMax = Vector2.one;
    textAreaRect.offsetMin = new Vector2(10f, 5f);
    textAreaRect.offsetMax = new Vector2(-10f, -5f);

    // =========================================================
    // ACTUAL INPUT TEXT
    // =========================================================

    GameObject inputTextObject = new GameObject(
        "Text",
        typeof(RectTransform)
    );

    inputTextObject.transform.SetParent(
        textArea.transform,
        false
    );

    TextMeshProUGUI inputText =
        inputTextObject.AddComponent<TextMeshProUGUI>();

    inputText.text = "";
    inputText.fontSize = 18f;
    inputText.color = Color.white;
    inputText.alignment = TextAlignmentOptions.Center;
    inputText.enableWordWrapping = false;

    RectTransform inputTextRect =
        inputTextObject.GetComponent<RectTransform>();

    inputTextRect.anchorMin = Vector2.zero;
    inputTextRect.anchorMax = Vector2.one;
    inputTextRect.offsetMin = Vector2.zero;
    inputTextRect.offsetMax = Vector2.zero;

    // THIS IS CRITICAL
    input.textComponent = inputText;

    // =========================================================
    // PLACEHOLDER
    // =========================================================

    GameObject placeholderObject = new GameObject(
        "Placeholder",
        typeof(RectTransform)
    );

    placeholderObject.transform.SetParent(
        textArea.transform,
        false
    );

    TextMeshProUGUI placeholder =
        placeholderObject.AddComponent<TextMeshProUGUI>();

    placeholder.text = "Minutes";
    placeholder.fontSize = 15f;
    placeholder.color =
        new Color(0.50f, 0.52f, 0.60f, 1f);

    placeholder.alignment =
        TextAlignmentOptions.Center;

    placeholder.enableWordWrapping = false;

    RectTransform placeholderRect =
        placeholderObject.GetComponent<RectTransform>();

    placeholderRect.anchorMin = Vector2.zero;
    placeholderRect.anchorMax = Vector2.one;
    placeholderRect.offsetMin = Vector2.zero;
    placeholderRect.offsetMax = Vector2.zero;

    input.placeholder = placeholder;

    // =========================================================
    // INPUT SETTINGS
    // =========================================================

    input.contentType =
        TMP_InputField.ContentType.IntegerNumber;

    input.characterValidation =
        TMP_InputField.CharacterValidation.Integer;

    input.lineType =
        TMP_InputField.LineType.SingleLine;

    input.interactable = false;

    // =========================================================
    // APPLY BUTTON
    // =========================================================

    GameObject applyObject = new GameObject(
        "Apply",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image),
        typeof(Button)
    );

    applyObject.transform.SetParent(
        customRow.transform,
        false
    );

    LayoutElement applyElement =
        applyObject.AddComponent<LayoutElement>();

    applyElement.preferredWidth = 100f;
    applyElement.preferredHeight = 55f;
    applyElement.minWidth = 100f;
    applyElement.minHeight = 55f;

    Image applyImage =
        applyObject.GetComponent<Image>();

    applyImage.color =
        new Color(0.18f, 0.20f, 0.30f, 1f);

    _customTimeApplyButton =
        applyObject.GetComponent<Button>();

    ColorBlock applyColors =
        _customTimeApplyButton.colors;

    applyColors.normalColor =
        new Color(0.18f, 0.20f, 0.30f, 1f);

    applyColors.highlightedColor =
        new Color(0.28f, 0.35f, 0.50f, 1f);

    applyColors.pressedColor =
        new Color(0.12f, 0.15f, 0.23f, 1f);

    applyColors.disabledColor =
        new Color(0.08f, 0.09f, 0.13f, 0.5f);

    applyColors.fadeDuration = 0.08f;

    _customTimeApplyButton.colors =
        applyColors;

    CreateText(
        applyObject.transform,
        "Text",
        "APPLY",
        15,
        FontStyles.Bold,
        Color.white,
        Vector2.zero,
        Vector2.one,
        Vector2.zero
    );

    _customTimeApplyButton.onClick.AddListener(
        ApplyCustomTime
    );

    // =========================================================
    // TOGGLE EVENT
    // =========================================================

    _timeLimitToggle.onValueChanged.AddListener(
        OnTimeLimitToggleChanged
    );
}

        // =========================================================
        // ONE LIFE CARD
        // =========================================================

        private void CreateOneLifeCard(Transform parent)
        {
            GameObject card = CreateModifierCard(
                parent,
                "OneLifeCard"
            );

            _oneLifeToggle = CreateToggle(
                card.transform,
                "EnableOneLife",
                "ONE LIFE MODE",
                new Vector2(0f, 0.82f),
                new Vector2(1f, 0.98f)
            );

            _oneLifeStatusText = CreateText(
                card.transform,
                "Status",
                "OFF - normal lives",
                15,
                FontStyles.Normal,
                new Color(0.65f, 0.65f, 0.72f),
                new Vector2(0.05f, 0.60f),
                new Vector2(0.95f, 0.76f),
                Vector2.zero
            );

            CreateText(
                card.transform,
                "Description",
                "Lose the base once = defeat",
                16,
                FontStyles.Normal,
                new Color(0.82f, 0.82f, 0.88f),
                new Vector2(0.08f, 0.38f),
                new Vector2(0.92f, 0.55f),
                Vector2.zero
            );

            _oneLifeToggle.onValueChanged.AddListener(
                OnOneLifeToggleChanged
            );
        }

        // =========================================================
        // MODIFIER CARD
        // =========================================================

        private GameObject CreateModifierCard(
            Transform parent,
            string objectName)
        {
            GameObject card = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            card.transform.SetParent(parent, false);

            Image image = card.GetComponent<Image>();
            image.color = new Color(0.075f, 0.09f, 0.14f, 1f);

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.22f, 0.32f, 1f);
            outline.effectDistance = new Vector2(1f, 1f);

            return card;
        }

        // =========================================================
        // TOGGLES
        // =========================================================

       private Toggle CreateToggle(
    Transform parent,
    string objectName,
    string label,
    Vector2 anchorMin,
    Vector2 anchorMax)
{
    GameObject toggleObject = new GameObject(
        objectName,
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Toggle)
    );

    toggleObject.transform.SetParent(
        parent,
        false
    );

    RectTransform rect =
        toggleObject.GetComponent<RectTransform>();

    rect.anchorMin = anchorMin;
    rect.anchorMax = anchorMax;

    rect.offsetMin = new Vector2(20f, 0f);
    rect.offsetMax = new Vector2(-20f, 0f);

    Toggle toggle =
        toggleObject.GetComponent<Toggle>();

    // =========================================================
    // SWITCH BACKGROUND
    // =========================================================

    GameObject background = new GameObject(
        "Background",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image)
    );

    background.transform.SetParent(
        toggleObject.transform,
        false
    );

    RectTransform bgRect =
        background.GetComponent<RectTransform>();

    bgRect.anchorMin =
        new Vector2(0f, 0.5f);

    bgRect.anchorMax =
        new Vector2(0f, 0.5f);

    bgRect.pivot =
        new Vector2(0f, 0.5f);

    bgRect.anchoredPosition =
        Vector2.zero;

    bgRect.sizeDelta =
        new Vector2(54f, 30f);

    Image bgImage =
        background.GetComponent<Image>();

    bgImage.color =
        new Color(0.15f, 0.16f, 0.23f, 1f);

    // =========================================================
    // CHECK / SWITCH HANDLE
    // =========================================================

    GameObject checkmark = new GameObject(
        "Checkmark",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image)
    );

    checkmark.transform.SetParent(
        background.transform,
        false
    );

    RectTransform checkRect =
        checkmark.GetComponent<RectTransform>();

    checkRect.anchorMin =
        new Vector2(0f, 0.5f);

    checkRect.anchorMax =
        new Vector2(0f, 0.5f);

    checkRect.pivot =
        new Vector2(0.5f, 0.5f);

    checkRect.anchoredPosition =
        new Vector2(15f, 0f);

    checkRect.sizeDelta =
        new Vector2(22f, 22f);

    Image checkImage =
        checkmark.GetComponent<Image>();

    checkImage.color =
        new Color(0.15f, 0.85f, 0.45f, 1f);

    toggle.graphic = checkImage;
    toggle.targetGraphic = bgImage;

    // =========================================================
    // LABEL
    // =========================================================

    CreateText(
        toggleObject.transform,
        "Label",
        label,
        20,
        FontStyles.Bold,
        Color.white,
        new Vector2(0.10f, 0f),
        new Vector2(1f, 1f),
        Vector2.zero
    );

    toggle.isOn = false;

    return toggle;
}

        // =========================================================
        // OPTION BUTTON
        // =========================================================

        private Button CreateOptionButton(
            Transform parent,
            string text,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(
                text + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            buttonObject.transform.SetParent(parent, false);

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(0.04f, 0.045f, 0.07f, 1f);

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors = button.colors;
            colors.normalColor =
                new Color(0.04f, 0.045f, 0.07f, 1f);
            colors.highlightedColor =
                new Color(0.15f, 0.23f, 0.36f, 1f);
            colors.pressedColor =
                new Color(0.08f, 0.12f, 0.20f, 1f);
            colors.disabledColor =
                new Color(0.03f, 0.035f, 0.05f, 0.45f);
            button.colors = colors;

            CreateText(
                buttonObject.transform,
                "Text",
                text,
                14,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero
            );

            button.onClick.AddListener(action);

            return button;
        }
        private Button CreateLargeTimeButton(
    Transform parent,
    string text,
    UnityEngine.Events.UnityAction action)
{
    GameObject buttonObject = new GameObject(
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

    LayoutElement element =
        buttonObject.AddComponent<LayoutElement>();

    element.minWidth = 90f;
    element.minHeight = 55f;
    element.preferredHeight = 55f;

    Image image =
        buttonObject.GetComponent<Image>();

    image.color =
        new Color(0.08f, 0.10f, 0.17f, 1f);

    Button button =
        buttonObject.GetComponent<Button>();

    ColorBlock colors =
        button.colors;

    colors.normalColor =
        new Color(0.08f, 0.10f, 0.17f, 1f);

    colors.highlightedColor =
        new Color(0.20f, 0.32f, 0.52f, 1f);

    colors.pressedColor =
        new Color(0.10f, 0.18f, 0.30f, 1f);

    colors.disabledColor =
        new Color(0.06f, 0.07f, 0.11f, 0.35f);

    colors.fadeDuration = 0.08f;

    button.colors = colors;

    CreateText(
        buttonObject.transform,
        "Text",
        text,
        16,
        FontStyles.Bold,
        Color.white,
        Vector2.zero,
        Vector2.one,
        Vector2.zero
    );

    button.onClick.AddListener(action);

    return button;
}

        // =========================================================
        // CHALLENGE TOGGLE LOGIC
        // =========================================================

        private void OnEnemyCountToggleChanged(bool enabled)
        {
            _enemyCountEnabled = enabled;

            if (enabled)
            {
                DifficultyManager.SetEnemyCountMultiplier(
                    _selectedEnemyMultiplier
                );
            }
            else
            {
                // 1 = normal amount. No extra enemies.
                DifficultyManager.SetEnemyCountMultiplier(1);
            }

            RefreshChallengeUI();
        }

        private void OnTimeLimitToggleChanged(bool enabled)
        {
            _timeLimitEnabled = enabled;

            if (enabled)
            {
                DifficultyManager.SetTimeLimitMinutes(
                    _selectedTimeLimitMinutes
                );
            }
            else
            {
                // 0 = unlimited/no time restriction.
                DifficultyManager.SetTimeLimitMinutes(0f);
            }

            RefreshChallengeUI();
        }

        private void OnOneLifeToggleChanged(bool enabled)
        {
            _oneLifeEnabled = enabled;

            DifficultyManager.SetOneLifeMode(enabled);

            RefreshChallengeUI();
        }

        private void SelectEnemyMultiplier(int multiplier)
        {
            _selectedEnemyMultiplier = multiplier;

            if (_enemyCountEnabled)
            {
                DifficultyManager.SetEnemyCountMultiplier(
                    multiplier
                );
            }

            RefreshChallengeUI();
        }

        private void SelectTimeLimit(float minutes)
{
    if (!_timeLimitEnabled)
        return;

    if (minutes <= 0f)
        return;

    _selectedTimeLimitMinutes = minutes;

    DifficultyManager.SetTimeLimitMinutes(
        _selectedTimeLimitMinutes
    );

    if (_customTimeInput != null)
    {
        _customTimeInput.SetTextWithoutNotify("");
    }

    RefreshChallengeUI();

    Debug.Log(
        $"[UIManager] Time limit selected: {minutes} minutes"
    );
}

        private void ApplyCustomTime()
{
    if (!_timeLimitEnabled)
        return;

    if (_customTimeInput == null)
        return;

    string input = _customTimeInput.text.Trim();

    if (string.IsNullOrEmpty(input))
    {
        Debug.LogWarning(
            "[UIManager] Please enter a time in minutes."
        );
        return;
    }

    if (!float.TryParse(
            input,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float minutes))
    {
        Debug.LogWarning(
            "[UIManager] Invalid custom time."
        );
        return;
    }

    if (minutes <= 0f)
    {
        Debug.LogWarning(
            "[UIManager] Time must be greater than 0."
        );
        return;
    }

    _selectedTimeLimitMinutes = minutes;

    DifficultyManager.SetTimeLimitMinutes(
        _selectedTimeLimitMinutes
    );

    RefreshChallengeUI();

    Debug.Log(
        $"[UIManager] Custom time applied: {minutes} minutes"
    );
}

        // =========================================================
        // REFRESH CHALLENGE UI
        // =========================================================

        private void RefreshChallengeUI()
{
    // =========================================================
    // TOGGLE STATES
    // =========================================================

    if (_enemyCountToggle != null)
        _enemyCountToggle.SetIsOnWithoutNotify(
            _enemyCountEnabled
        );

    if (_timeLimitToggle != null)
        _timeLimitToggle.SetIsOnWithoutNotify(
            _timeLimitEnabled
        );

    if (_oneLifeToggle != null)
        _oneLifeToggle.SetIsOnWithoutNotify(
            _oneLifeEnabled
        );

    // =========================================================
    // STATUS TEXT
    // =========================================================

    if (_enemyCountStatusText != null)
    {
        _enemyCountStatusText.text =
            _enemyCountEnabled
                ? $"ON  •  {_selectedEnemyMultiplier}× enemies"
                : "OFF  •  Normal enemy amount";
    }

    if (_timeStatusText != null)
    {
        _timeStatusText.text =
            _timeLimitEnabled
                ? $"ON  •  {_selectedTimeLimitMinutes:0.##} minutes"
                : "OFF  •  Unlimited time";
    }

    if (_oneLifeStatusText != null)
    {
        _oneLifeStatusText.text =
            _oneLifeEnabled
                ? "ON  •  One life only"
                : "OFF  •  Normal lives";
    }

    // =========================================================
    // ENEMY COUNT BUTTONS
    // =========================================================

    if (_enemyCountButtons != null)
    {
        for (int i = 0;
             i < _enemyCountButtons.Length;
             i++)
        {
            if (_enemyCountButtons[i] == null)
                continue;

            _enemyCountButtons[i].interactable =
                _enemyCountEnabled;
        }
    }

    // =========================================================
    // TIME BUTTONS
    // =========================================================

    if (_timeButtons != null)
    {
        for (int i = 0;
             i < _timeButtons.Length;
             i++)
        {
            if (_timeButtons[i] == null)
                continue;

            _timeButtons[i].interactable =
                _timeLimitEnabled;
        }
    }

    // =========================================================
    // CUSTOM INPUT
    // =========================================================

    if (_customTimeInput != null)
    {
        _customTimeInput.interactable =
            _timeLimitEnabled;
    }

    // =========================================================
    // APPLY
    // =========================================================

    if (_customTimeApplyButton != null)
    {
        _customTimeApplyButton.interactable =
            _timeLimitEnabled;
    }
}
        // =========================================================
        // CHALLENGE NAVIGATION
        // =========================================================

        private void OnChallengeBackClicked()
        {
            if (_challengeSettingsPanel != null)
                _challengeSettingsPanel.SetActive(false);

            EnsureDifficultySelectionUI();

            if (_difficultySelectionPanel != null)
                _difficultySelectionPanel.SetActive(true);
        }

        private void OnChallengeNextClicked()
        {
            // Make absolutely sure disabled modifiers have no effect.
            DifficultyManager.SetEnemyCountMultiplier(
                _enemyCountEnabled ? _selectedEnemyMultiplier : 1
            );

            DifficultyManager.SetTimeLimitMinutes(
                _timeLimitEnabled ? _selectedTimeLimitMinutes : 0f
            );

            DifficultyManager.SetOneLifeMode(
                _oneLifeEnabled
            );

            if (_challengeSettingsPanel != null)
                _challengeSettingsPanel.SetActive(false);

            EnsureLevelSelectionUI();

            if (_levelSelectionPanel != null)
                _levelSelectionPanel.SetActive(true);
        }

        // =========================================================
        // LEVEL UI
        // =========================================================

        private void EnsureLevelSelectionUI()
        {
            if (_levelSelectionPanel != null)
                return;

            if (mainMenuPanel == null)
            {
                Debug.LogError("[UIManager] Main Menu Panel is missing!");
                return;
            }

            Transform parent = mainMenuPanel.transform.parent;

            _levelSelectionPanel = CreateFullScreenPanel(
                parent,
                "LevelSelectionPanel",
                new Color(0.05f, 0.05f, 0.08f, 0.99f)
            );

            CreateText(
                _levelSelectionPanel.transform,
                "TitleText",
                "SELECT LEVEL",
                46,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.90f),
                new Vector2(0.5f, 0.90f),
                new Vector2(600f, 100f)
            );

            CreateText(
                _levelSelectionPanel.transform,
                "SelectedDifficultyText",
                "DIFFICULTY: " + DifficultyManager.DifficultyName,
                24,
                FontStyles.Bold,
                new Color(1f, 0.8f, 0.2f),
                new Vector2(0.5f, 0.82f),
                new Vector2(0.5f, 0.82f),
                new Vector2(700f, 50f)
            );

            CreateText(
                _levelSelectionPanel.transform,
                "ChallengeSummaryText",
                "",
                17,
                FontStyles.Normal,
                new Color(0.75f, 0.8f, 0.88f),
                new Vector2(0.5f, 0.75f),
                new Vector2(0.5f, 0.75f),
                new Vector2(900f, 45f)
            );

            GameObject container = new GameObject(
                "LevelsContainer",
                typeof(RectTransform)
            );

            container.transform.SetParent(
                _levelSelectionPanel.transform,
                false
            );

            RectTransform containerRect =
                container.GetComponent<RectTransform>();

            containerRect.anchorMin = new Vector2(0.1f, 0.25f);
            containerRect.anchorMax = new Vector2(0.9f, 0.68f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout =
                container.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            foreach (LevelData lvl in levels)
            {
                if (lvl == null)
                    continue;

                CreateLevelCard(container.transform, lvl);
            }

            GameObject back = CreateSimpleButton(
                _levelSelectionPanel.transform,
                "BACK",
                new Vector2(0.5f, 0.10f),
                new Vector2(220f, 50f)
            );

            back.GetComponent<Button>().onClick.AddListener(
                () =>
                {
                    _levelSelectionPanel.SetActive(false);
                    EnsureChallengeSettingsUI();
                    _challengeSettingsPanel.SetActive(true);
                    RefreshChallengeUI();
                }
            );

            _levelSelectionPanel.SetActive(false);
        }

        // =========================================================
        // LEVEL CARD
        // =========================================================

        private void CreateLevelCard(
            Transform parent,
            LevelData lvl)
        {
            GameObject card = new GameObject(
                $"Card_{lvl.LevelName}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            card.transform.SetParent(parent, false);

            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.14f, 0.14f, 0.2f, 1f);

            LayoutElement element =
                card.AddComponent<LayoutElement>();

            element.preferredWidth = 320f;
            element.preferredHeight = 420f;

            VerticalLayoutGroup layout =
                card.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20, 20, 25, 25);
            layout.spacing = 15f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            CreateText(
                card.transform,
                "NameText",
                lvl.LevelName.ToUpper(),
                24,
                FontStyles.Bold,
                Color.white,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 45f)
            );

            GameObject statsObject = new GameObject(
                "StatsText",
                typeof(RectTransform)
            );

            statsObject.transform.SetParent(card.transform, false);

            LayoutElement statsElement =
                statsObject.AddComponent<LayoutElement>();

            statsElement.preferredHeight = 210f;

            TextMeshProUGUI statsText =
                statsObject.AddComponent<TextMeshProUGUI>();

            int waveCount =
                lvl.Waves != null ? lvl.Waves.Count : 0;

            statsText.text =
                $"STARTING GOLD\n" +
                $"<color=#FFD700>{lvl.StartingGold} G</color>\n\n" +
                $"BASE HP\n" +
                $"<color=#FF5555>{lvl.BaseMaxHealth} HP</color>\n\n" +
                $"TOTAL WAVES\n" +
                $"<color=#55FFFF>{waveCount}</color>";

            statsText.fontSize = 18;
            statsText.lineSpacing = 8f;
            statsText.color = new Color(0.85f, 0.85f, 0.9f);
            statsText.alignment = TextAlignmentOptions.Center;

            GameObject spacer = new GameObject(
                "Spacer",
                typeof(RectTransform)
            );

            spacer.transform.SetParent(card.transform, false);

            LayoutElement spacerElement =
                spacer.AddComponent<LayoutElement>();

            spacerElement.flexibleHeight = 1f;

            GameObject buttonObject = new GameObject(
                "PlayButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            buttonObject.transform.SetParent(card.transform, false);

            Image buttonImage =
                buttonObject.GetComponent<Image>();

            Color buttonColor =
                new Color(0.12f, 0.75f, 0.38f, 1f);

            buttonImage.color = buttonColor;

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor =
                new Color(0.15f, 0.85f, 0.45f, 1f);
            colors.pressedColor =
                new Color(0.08f, 0.65f, 0.3f, 1f);
            button.colors = colors;

            LayoutElement buttonElement =
                buttonObject.AddComponent<LayoutElement>();

            buttonElement.preferredHeight = 50f;

            CreateText(
                buttonObject.transform,
                "Text",
                "SELECT LEVEL",
                18,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero
            );

            LevelData targetLevel = lvl;

            button.onClick.AddListener(
                () => SelectAndPlayLevel(targetLevel)
            );
        }

        // =========================================================
        // START LEVEL
        // =========================================================

        private void SelectAndPlayLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[UIManager] Selected LevelData is NULL!");
                return;
            }

            if (_difficultySelectionPanel != null)
                _difficultySelectionPanel.SetActive(false);

            if (_challengeSettingsPanel != null)
                _challengeSettingsPanel.SetActive(false);

            if (_levelSelectionPanel != null)
                _levelSelectionPanel.SetActive(false);

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevel(levelData);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    levelData.LevelName
                );
            }
        }

        // =========================================================
        // SIMPLE BUTTON
        // =========================================================

        private GameObject CreateSimpleButton(
            Transform parent,
            string text,
            Vector2 anchor,
            Vector2 size)
        {
            GameObject buttonObject = new GameObject(
                text.Replace(" ", "") + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            buttonObject.transform.SetParent(parent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(0.18f, 0.18f, 0.24f, 1f);

            Button button =
                buttonObject.GetComponent<Button>();

            ColorBlock colors = button.colors;
            colors.normalColor =
                new Color(0.18f, 0.18f, 0.24f, 1f);
            colors.highlightedColor =
                new Color(0.28f, 0.28f, 0.35f, 1f);
            colors.pressedColor =
                new Color(0.12f, 0.12f, 0.17f, 1f);
            button.colors = colors;

            CreateText(
                buttonObject.transform,
                "Text",
                text,
                18,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero
            );

            return buttonObject;
        }

        // =========================================================
        // FULL SCREEN PANEL
        // =========================================================

        private GameObject CreateFullScreenPanel(
            Transform parent,
            string objectName,
            Color color)
        {
            GameObject panel = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            panel.transform.SetParent(parent, false);

            RectTransform rect =
                panel.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image =
                panel.GetComponent<Image>();

            image.color = color;

            return panel;
        }

        // =========================================================
        // CREATE TEXT
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
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform)
            );

            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI tmp =
                textObject.AddComponent<TextMeshProUGUI>();

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform rect =
                textObject.GetComponent<RectTransform>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            return tmp;
        }

        // =========================================================
        // PLACEHOLDER
        // =========================================================

        private TMP_Text CreatePlaceholder(
            Transform parent,
            string text)
        {
            GameObject placeholder = new GameObject(
                "Placeholder",
                typeof(RectTransform)
            );

            placeholder.transform.SetParent(parent, false);

            TextMeshProUGUI tmp =
                placeholder.AddComponent<TextMeshProUGUI>();

            tmp.text = text;
            tmp.fontSize = 13f;
            tmp.color = new Color(0.55f, 0.55f, 0.62f);
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform rect =
                placeholder.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        // =========================================================
        // PAUSE
        // =========================================================

        public void OnResumeButtonClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TogglePause();
        }

        public void OnPauseButtonClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TogglePause();
        }

        // =========================================================
        // RESTART
        // =========================================================

        public void OnRestartButtonClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartLevel();
        }

        // =========================================================
        // MAIN MENU
        // =========================================================

        public void OnReturnToMainMenuButtonClicked()
        {
            Time.timeScale = 1f;

            UnityEngine.SceneManagement.SceneManager.LoadScene(
                "MainMenu"
            );
        }

        // =========================================================
        // QUIT
        // =========================================================

        public void OnQuitButtonClicked()
        {
            Application.Quit();
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (GameManager.Instance == null)
                return;

            if (timeText != null &&
                (GameManager.Instance.CurrentState ==
                    GameManager.GameState.Playing ||
                 GameManager.Instance.CurrentState ==
                    GameManager.GameState.Pause))
            {
                string formattedTime =
                    FormatTime(GameManager.Instance.PlayTime);

                timeText.text =
                    $"TIME: {formattedTime}";
            }

            if (GameManager.Instance.CurrentState !=
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
            Vector2 mouseScreenPos = Vector2.zero;

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
                    mouseScreenPos = Input.mousePosition;
                }
            }

            if (!leftClick)
                return;

            if (IsPointerOverInteractiveUI(mouseScreenPos))
                return;

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
                new Vector2(worldPos.x, worldPos.y);

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    worldPos2D,
                    0.6f
                );

            Collider2D closestHit = null;
            float closestDist = float.MaxValue;

            TowerDefense.Tower.TowerController targetTower = null;
            TowerDefense.Enemy.EnemyHealth targetEnemy = null;

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                TowerDefense.Tower.TowerController tower =
                    hit.GetComponent<
                        TowerDefense.Tower.TowerController>();

                TowerDefense.Enemy.EnemyHealth enemy =
                    hit.GetComponent<
                        TowerDefense.Enemy.EnemyHealth>();

                if (tower != null || enemy != null)
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
                    SelectTower(targetTower);
                else if (targetEnemy != null)
                    SelectEnemy(targetEnemy);
            }
            else
            {
                Deselect();
            }
        }

        // =========================================================
        // SELECT TOWER
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

        // =========================================================
        // SELECT ENEMY
        // =========================================================

        private void SelectEnemy(
            TowerDefense.Enemy.EnemyHealth enemy)
        {
            _selectedEnemy = enemy;
            _selectedTower = null;

            EnsureInfoPanel();

            _infoPanel.SetActive(true);

            UpdateSelectedStatsDisplay();
        }

        // =========================================================
        // DESELECT
        // =========================================================

        private void Deselect()
        {
            _selectedTower = null;
            _selectedEnemy = null;

            if (_infoPanel != null)
                _infoPanel.SetActive(false);
        }

        // =========================================================
        // UPDATE SELECTED STATS
        // =========================================================

        private void UpdateSelectedStatsDisplay()
        {
            if (_infoPanel == null ||
                !_infoPanel.activeSelf)
            {
                return;
            }

            // =====================================================
            // TOWER
            // =====================================================

            if (_selectedTower != null)
            {
                if (_selectedTower.gameObject == null)
                {
                    Deselect();
                    return;
                }

                TowerData data =
                    _selectedTower.TowerData;

                string name =
                    data != null ? data.TowerName : "Tower";

                float fireRate =
                    data != null ? data.FireRate : 0f;

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
                        $"{_selectedTower.CurrentDamage}</color>\n\n" +

                        $"FIRE RATE: <color=#55FFFF>" +
                        $"{fireRate:F1}/s</color>\n\n" +

                        $"RANGE: <color=#55FF55>" +
                        $"{_selectedTower.CurrentRange:F1}</color>";
                }

                if (_lvlUpBtnGO != null)
                {
                    _lvlUpBtnGO.SetActive(true);

                    if (_selectedTower.CurrentLevel <
                        _selectedTower.MaxLevel)
                    {
                        int cost = _selectedTower.UpgradeCost;

                        bool canAfford =
                            GameManager.Instance == null ||
                            GameManager.Instance.CurrentGold >= cost;

                        if (_lvlUpBtnText != null)
                            _lvlUpBtnText.text =
                                $"UPGRADE ({cost} G)";

                        if (_lvlUpBtn != null)
                            _lvlUpBtn.interactable = canAfford;
                    }
                    else
                    {
                        if (_lvlUpBtnText != null)
                            _lvlUpBtnText.text = "MAX LEVEL";

                        if (_lvlUpBtn != null)
                            _lvlUpBtn.interactable = false;
                    }
                }
            }

            // =====================================================
            // ENEMY
            // =====================================================

            else if (_selectedEnemy != null)
            {
                if (_selectedEnemy.gameObject == null ||
                    _selectedEnemy.IsDead)
                {
                    Deselect();
                    return;
                }

                string name =
                    _selectedEnemy.EnemyData != null
                        ? _selectedEnemy.EnemyData.EnemyName
                        : "Enemy";

                int hp = _selectedEnemy.CurrentHealth;
                int maxHp = _selectedEnemy.MaxHealth;
                float speed = _selectedEnemy.MoveSpeed;
                int armor = _selectedEnemy.Armor;
                int attack = _selectedEnemy.Attack;

                if (_infoTitleText != null)
                    _infoTitleText.text = name.ToUpper();

                if (_infoStatsText != null)
                {
                    _infoStatsText.text =
                        $"HP: <color=#FF5555>" +
                        $"{hp}/{maxHp}</color>\n\n" +

                        $"SPEED: <color=#55FF55>" +
                        $"{speed:F1}</color>\n\n" +

                        $"ARMOR: <color=#AAAAAA>" +
                        $"{armor}</color>\n\n" +

                        $"DAMAGE TO BASE: <color=#FF5555>" +
                        $"{attack}</color>";
                }

                if (_lvlUpBtnGO != null)
                    _lvlUpBtnGO.SetActive(false);
            }
            else
            {
                Deselect();
            }
        }

        // =========================================================
        // INFO PANEL
        // =========================================================

        private void EnsureInfoPanel()
        {
            if (_infoPanel != null)
                return;

            Transform parent =
                gameplayHUDPanel != null
                    ? gameplayHUDPanel.transform
                    : transform;

            _infoPanel = new GameObject(
                "InfoPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            _infoPanel.transform.SetParent(parent, false);

            RectTransform rect =
                _infoPanel.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-20f, 0f);
            rect.sizeDelta = new Vector2(280f, 250f);

            Image bg =
                _infoPanel.GetComponent<Image>();

            bg.color =
                new Color(0.08f, 0.09f, 0.15f, 0.92f);

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
                title.GetComponent<RectTransform>();

            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(15f, -15f);
            titleRect.sizeDelta = new Vector2(-50f, 35f);

            _infoTitleText =
                title.AddComponent<TextMeshProUGUI>();

            _infoTitleText.fontSize = 18f;
            _infoTitleText.fontStyle = FontStyles.Bold;
            _infoTitleText.color = Color.white;
            _infoTitleText.alignment =
                TextAlignmentOptions.Left;

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
                stats.GetComponent<RectTransform>();

            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.anchoredPosition = new Vector2(0f, -30f);
            statsRect.sizeDelta = new Vector2(-30f, -80f);

            _infoStatsText =
                stats.AddComponent<TextMeshProUGUI>();

            _infoStatsText.fontSize = 14f;
            _infoStatsText.color =
                new Color(0.85f, 0.85f, 0.9f, 1f);
            _infoStatsText.alignment =
                TextAlignmentOptions.TopLeft;

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
                close.GetComponent<RectTransform>();

            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition =
                new Vector2(-10f, -10f);
            closeRect.sizeDelta =
                new Vector2(25f, 25f);

            Image closeImage =
                close.GetComponent<Image>();

            closeImage.color =
                new Color(0.8f, 0.2f, 0.2f, 0.8f);

            close.GetComponent<Button>()
                .onClick.AddListener(Deselect);

            _lvlUpBtnGO = new GameObject(
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
                _lvlUpBtnGO.GetComponent<RectTransform>();

            upgradeRect.anchorMin = new Vector2(0.5f, 0f);
            upgradeRect.anchorMax = new Vector2(0.5f, 0f);
            upgradeRect.pivot = new Vector2(0.5f, 0f);
            upgradeRect.anchoredPosition =
                new Vector2(0f, 15f);
            upgradeRect.sizeDelta =
                new Vector2(240f, 40f);

            Image upgradeImage =
                _lvlUpBtnGO.GetComponent<Image>();

            upgradeImage.color =
                new Color(0.12f, 0.75f, 0.38f, 1f);

            _lvlUpBtn =
                _lvlUpBtnGO.GetComponent<Button>();

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
                upgradeText.GetComponent<RectTransform>();

            upgradeTextRect.anchorMin = Vector2.zero;
            upgradeTextRect.anchorMax = Vector2.one;
            upgradeTextRect.offsetMin = Vector2.zero;
            upgradeTextRect.offsetMax = Vector2.zero;

            _lvlUpBtnText =
                upgradeText.AddComponent<TextMeshProUGUI>();

            _lvlUpBtnText.text = "UPGRADE";
            _lvlUpBtnText.fontSize = 14f;
            _lvlUpBtnText.fontStyle = FontStyles.Bold;
            _lvlUpBtnText.color = Color.white;
            _lvlUpBtnText.alignment =
                TextAlignmentOptions.Center;

            _lvlUpBtnGO.SetActive(false);
        }

        // =========================================================
        // UPGRADE CLICK
        // =========================================================

        private void OnUpgradeButtonClicked()
        {
            if (_selectedTower == null)
                return;

            if (GameManager.Instance == null)
                return;

            if (_selectedTower.CurrentLevel >=
                _selectedTower.MaxLevel)
            {
                UpdateSelectedStatsDisplay();
                return;
            }

            int cost =
                _selectedTower.UpgradeCost;

            if (!GameManager.Instance.TrySpendGold(cost))
                return;

            _selectedTower.LevelUp();

            UpdateSelectedStatsDisplay();
        }

        // =========================================================
        // UI RAYCAST
        // =========================================================

        private bool IsPointerOverInteractiveUI(
            Vector2 screenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;

            UnityEngine.EventSystems.PointerEventData eventData =
                new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current
                );

            eventData.position = screenPos;

            List<UnityEngine.EventSystems.RaycastResult> results =
                new List<UnityEngine.EventSystems.RaycastResult>();

            UnityEngine.EventSystems.EventSystem.current.RaycastAll(
                eventData,
                results
            );

            foreach (var result in results)
            {
                if (result.gameObject == null)
                    continue;

                string name = result.gameObject.name;

                if (name == "GameplayHUDPanel" ||
                    name == "Canvas" ||
                    name == "EventSystem")
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
