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

        [Header("HUD Prefabs")]
        [SerializeField] private GameObject goldPrefab;
        [SerializeField] private GameObject healthPrefab;
        [SerializeField] private GameObject pausePrefab;
        [SerializeField] private Sprite waveSprite;

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
        private bool _endlessModeEnabled;
        private bool _passiveGoldEnabled = true;
        // Values are remembered even when the corresponding option is
        // temporarily disabled.
        private int _selectedEnemyMultiplier = 2;
        private float _selectedTimeLimitMinutes = 3f;

        private Toggle _enemyCountToggle;
        private Toggle _timeLimitToggle;
        private Toggle _oneLifeToggle;
        private Toggle _endlessModeToggle;
        private Toggle _passiveGoldToggle;

        private Button[] _enemyCountButtons;
        private Button[] _timeButtons;
        private TMP_InputField _customTimeInput;
        private Button _customTimeApplyButton;

        private TextMeshProUGUI _enemyCountStatusText;
        private TextMeshProUGUI _timeStatusText;
        private TextMeshProUGUI _oneLifeStatusText;
        private TextMeshProUGUI _endlessModeStatusText;
        private TextMeshProUGUI _challengeSummaryText;
        private TextMeshProUGUI _passiveGoldStatusText;

        private readonly Dictionary<LevelData, TextMeshProUGUI>
    _levelStatsTextByLevel =
        new Dictionary<LevelData, TextMeshProUGUI>();

        /// <summary>
        /// True when Endless Mode was selected in Challenge Settings.
        /// Stored in PlayerPrefs so the wave system can read the setting
        /// after the game scene starts.
        /// </summary>
        public static bool IsEndlessModeEnabled
        {
            get => PlayerPrefs.GetInt("TowerDefense_EndlessMode", 0) == 1;
            private set => PlayerPrefs.SetInt(
                "TowerDefense_EndlessMode",
                value ? 1 : 0
            );
        }

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

            _endlessModeEnabled = false;

            PlayerPrefs.SetInt(
                "TowerDefense_EndlessMode",
                0
            );
            PlayerPrefs.Save();

            EnsureInfoPanel();

            EnsureTimeTextsExist();

            if (GameManager.Instance != null)
            {
                UpdatePanelVisibility(GameManager.Instance.CurrentState);
            }
            else
            {
                UpdatePanelVisibility(GameManager.GameState.MainMenu);
            }

            InitializeHUDGraphics();
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
                healthText.text = $"{evt.CurrentHealth}/{evt.MaxHealth}";
        }

        // =========================================================
        // GOLD
        // =========================================================

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            if (goldText != null)
                goldText.text = $"{evt.CurrentGold}";

            UpdateSelectedStatsDisplay();
        }

        // =========================================================
        // WAVE
        // =========================================================

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (waveText != null)
                waveText.text = $"Wave: {evt.WaveIndex + 1}";
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

            // =========================================================
            // RESET ALL OPTIONAL CHALLENGE SETTINGS
            // =========================================================
            // A new difficulty selection starts with all optional
            // modifiers OFF. This prevents Endless Mode from being
            // carried over from a previous selection/session.
            _enemyCountEnabled = false;
            _timeLimitEnabled = false;
            _oneLifeEnabled = false;
            _endlessModeEnabled = false;
            _passiveGoldEnabled = true;
            _selectedEnemyMultiplier = 2;
            _selectedTimeLimitMinutes = 3f;

            DifficultyManager.SetEnemyCountMultiplier(1);
            DifficultyManager.SetTimeLimitMinutes(0f);
            DifficultyManager.SetOneLifeMode(false);
            DifficultyManager.SetPassiveGoldEnabled(true);
            // Explicitly clear the saved Endless state.
            IsEndlessModeEnabled = false;
            PlayerPrefs.SetInt("TowerDefense_EndlessMode", 0);
            PlayerPrefs.Save();

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
            // FOUR INDEPENDENT OPTIONAL FEATURES
            // 1. Enemy Count
            // 2. Time Limit
            // 3. One Life Challenge
            // 4. Endless Waves
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

            cardsLayout.spacing = 12f;
            cardsLayout.padding = new RectOffset(4, 4, 5, 5);
            cardsLayout.childAlignment = TextAnchor.MiddleCenter;
            cardsLayout.childControlWidth = true;
            cardsLayout.childControlHeight = true;
            cardsLayout.childForceExpandWidth = true;
            cardsLayout.childForceExpandHeight = true;

            CreateEnemyCountCard(cards.transform);
            CreateTimeLimitCard(cards.transform);
            CreateOneLifeCard(cards.transform);
            CreateEndlessModeCard(cards.transform);
            CreatePassiveGoldCard(cards.transform);

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
            _endlessModeEnabled = false;
            IsEndlessModeEnabled = false;

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

    inputElement.preferredWidth = 105f;
    inputElement.preferredHeight = 55f;
    inputElement.minWidth = 105f;
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

    applyElement.preferredWidth = 75f;
    applyElement.preferredHeight = 55f;
    applyElement.minWidth = 75f;
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
        // ENDLESS MODE CARD
        // =========================================================

        private void CreateEndlessModeCard(Transform parent)
        {
            GameObject card = CreateModifierCard(
                parent,
                "EndlessModeCard"
            );

            _endlessModeToggle = CreateToggle(
                card.transform,
                "EnableEndlessMode",
                "ENDLESS MODE",
                new Vector2(0f, 0.82f),
                new Vector2(1f, 0.98f)
            );

            _endlessModeStatusText = CreateText(
                card.transform,
                "Status",
                "OFF • Fixed wave count",
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
                "Waves continue forever\nSurvive as long as possible",
                16,
                FontStyles.Normal,
                new Color(0.82f, 0.82f, 0.88f),
                new Vector2(0.08f, 0.28f),
                new Vector2(0.92f, 0.53f),
                Vector2.zero
            );

            _endlessModeToggle.onValueChanged.AddListener(
                OnEndlessModeToggleChanged
            );
        }
private void CreatePassiveGoldCard(
    Transform parent)
{
    GameObject card =
        CreateModifierCard(
            parent,
            "PassiveGoldCard"
        );


    // =====================================================
    // TOGGLE
    // =====================================================

    _passiveGoldToggle =
        CreateToggle(
            card.transform,
            "EnablePassiveGold",
            "GOLD OVER TIME",
            new Vector2(0f, 0.82f),
            new Vector2(1f, 0.98f)
        );


    // =====================================================
    // STATUS
    // =====================================================

    _passiveGoldStatusText =
        CreateText(
            card.transform,
            "Status",
            "ON • +10 G every 10 seconds",
            15,
            FontStyles.Normal,
            new Color(
                0.65f,
                0.65f,
                0.72f
            ),
            new Vector2(
                0.05f,
                0.60f
            ),
            new Vector2(
                0.95f,
                0.76f
            ),
            Vector2.zero
        );


    // =====================================================
    // DESCRIPTION
    // =====================================================

    CreateText(
        card.transform,
        "Description",
        "+10 G every 10 seconds\n" +
        "Turn OFF for harder economy",
        15,
        FontStyles.Normal,
        new Color(
            0.82f,
            0.82f,
            0.88f
        ),
        new Vector2(
            0.08f,
            0.27f
        ),
        new Vector2(
            0.92f,
            0.54f
        ),
        Vector2.zero
    );


    // =====================================================
    // EVENT
    // =====================================================

    _passiveGoldToggle.onValueChanged.AddListener(
        OnPassiveGoldToggleChanged
    );


    // =====================================================
    // DEFAULT
    // =====================================================

    _passiveGoldToggle.SetIsOnWithoutNotify(
        true
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
        private void OnEndlessModeToggleChanged(bool enabled)
        {
            _endlessModeEnabled = enabled;
            IsEndlessModeEnabled = enabled;

            // Always persist the current value explicitly.
            PlayerPrefs.SetInt(
                "TowerDefense_EndlessMode",
                enabled ? 1 : 0
            );
            PlayerPrefs.Save();

            RefreshChallengeUI();

            Debug.Log(
                $"[UIManager] Endless mode: {(enabled ? "ON" : "OFF")}"
            );
        }
// =========================================================
// PASSIVE GOLD TOGGLE
// =========================================================

private void OnPassiveGoldToggleChanged(
    bool enabled)
{
    _passiveGoldEnabled =
        enabled;


    DifficultyManager.SetPassiveGoldEnabled(
        enabled
    );


    RefreshChallengeUI();


    Debug.Log(
        $"[UIManager] Passive Gold: " +
        $"{(enabled ? "ON" : "OFF")}"
    );
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
// REFRESH LEVEL CARD WAVE DISPLAY
// =========================================================

private void RefreshLevelCards()
{
    if (_levelStatsTextByLevel == null)
        return;

    foreach (var pair in _levelStatsTextByLevel)
    {
        LevelData lvl = pair.Key;
        TextMeshProUGUI statsText = pair.Value;

        if (lvl == null || statsText == null)
            continue;

        int waveCount =
            lvl.Waves != null
                ? lvl.Waves.Count
                : 0;

        string waveDisplay =
            _endlessModeEnabled
                ? "∞"
                : waveCount.ToString();

        string waveLabel =
            _endlessModeEnabled
                ? "ENDLESS WAVES"
                : "TOTAL WAVES";

        statsText.text =
            $"STARTING GOLD\n" +
            $"<color=#FFD700>{lvl.StartingGold} G</color>\n\n" +
            $"BASE HP\n" +
            $"<color=#FF5555>{lvl.BaseMaxHealth} HP</color>\n\n" +
            $"{waveLabel}\n" +
            $"<color=#55FFFF>{waveDisplay}</color>";
    }
}

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

    if (_endlessModeToggle != null)
        _endlessModeToggle.SetIsOnWithoutNotify(
            _endlessModeEnabled
        );
        if (_passiveGoldToggle != null)
    _passiveGoldToggle.SetIsOnWithoutNotify(
        _passiveGoldEnabled
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
                ? "ON  •  One life challenge"
                : "OFF  •  Normal base health";
    }

    if (_endlessModeStatusText != null)
    {
        _endlessModeStatusText.text =
            _endlessModeEnabled
                ? "ON  •  Infinite waves"
                : "OFF  •  Fixed wave count";
    }
    if (_passiveGoldStatusText != null)
{
    _passiveGoldStatusText.text =
        _passiveGoldEnabled
            ? "ON  •  +10 G every 10 seconds"
            : "OFF  •  No passive gold";
}

    if (_challengeSummaryText != null)
    {
        string enemySummary = _enemyCountEnabled
            ? $"ENEMIES ×{_selectedEnemyMultiplier}"
            : "ENEMIES NORMAL";

        string timeSummary = _timeLimitEnabled
            ? $"TIME {_selectedTimeLimitMinutes:0.##} MIN"
            : "TIME UNLIMITED";

        string lifeSummary = _oneLifeEnabled
            ? "ONE LIFE"
            : "NORMAL HEALTH";

       string endlessSummary =
    _endlessModeEnabled
        ? "ENDLESS WAVES"
        : "FIXED WAVES";


string passiveGoldSummary =
    _passiveGoldEnabled
        ? "PASSIVE GOLD ON"
        : "PASSIVE GOLD OFF";


_challengeSummaryText.text =
    $"{enemySummary}   •   " +
    $"{timeSummary}   •   " +
    $"{lifeSummary}   •   " +
    $"{endlessSummary}   •   " +
    $"{passiveGoldSummary}";
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
    RefreshLevelCards();
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

            DifficultyManager.SetPassiveGoldEnabled(
    _passiveGoldEnabled
);

            // IMPORTANT:
            // The Endless setting is independent from the selected level.
            IsEndlessModeEnabled = _endlessModeEnabled;

            PlayerPrefs.SetInt(
                "TowerDefense_EndlessMode",
                _endlessModeEnabled ? 1 : 0
            );
            PlayerPrefs.Save();

            Debug.Log(
                $"[UIManager] Final challenge settings: " +
                $"Endless={_endlessModeEnabled}"
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

            _challengeSummaryText = CreateText(
                _levelSelectionPanel.transform,
                "ChallengeSummaryText",
                "",
                17,
                FontStyles.Normal,
                new Color(0.75f, 0.8f, 0.88f),
                new Vector2(0.5f, 0.75f),
                new Vector2(0.5f, 0.75f),
                new Vector2(1100f, 45f)
            );

            RefreshChallengeUI();

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

            containerRect.anchorMin = new Vector2(0.035f, 0.22f);
containerRect.anchorMax = new Vector2(0.965f, 0.70f);
containerRect.offsetMin = Vector2.zero;
containerRect.offsetMax = Vector2.zero;

HorizontalLayoutGroup layout =
    container.AddComponent<HorizontalLayoutGroup>();

layout.spacing = 14f;
layout.padding = new RectOffset(0, 0, 0, 0);


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
            RefreshLevelCards();
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

    // =========================================================
    // CARD SIZE
    // =========================================================

    Image cardImage =
        card.GetComponent<Image>();

    cardImage.color =
        new Color(0.14f, 0.14f, 0.20f, 1f);

    LayoutElement cardElement =
        card.AddComponent<LayoutElement>();

    cardElement.preferredWidth = 165f;
    cardElement.minWidth = 165f;

    cardElement.preferredHeight = 350f;
    cardElement.minHeight = 350f;

    // =========================================================
    // CARD LAYOUT
    // =========================================================

    VerticalLayoutGroup layout =
        card.AddComponent<VerticalLayoutGroup>();

    layout.padding =
        new RectOffset(10, 10, 18, 12);

    layout.spacing = 8f;

    layout.childAlignment =
        TextAnchor.UpperCenter;

    layout.childControlWidth = true;
    layout.childControlHeight = true;

    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;

    // =========================================================
    // LEVEL TITLE
    // =========================================================

    GameObject titleObject =
        new GameObject(
            "NameText",
            typeof(RectTransform)
        );

    titleObject.transform.SetParent(
        card.transform,
        false
    );

    LayoutElement titleElement =
        titleObject.AddComponent<LayoutElement>();

    titleElement.preferredHeight = 45f;
    titleElement.minHeight = 45f;

    TextMeshProUGUI titleText =
        titleObject.AddComponent<TextMeshProUGUI>();

    titleText.text =
        lvl.LevelName.ToUpper();

    titleText.fontSize = 24f;
    titleText.fontStyle =
        FontStyles.Bold;

    titleText.color =
        Color.white;

    titleText.alignment =
        TextAlignmentOptions.Center;

    titleText.enableWordWrapping = false;

    titleText.overflowMode =
        TextOverflowModes.Ellipsis;

    // =========================================================
    // STATS
    // =========================================================

    GameObject statsObject =
        new GameObject(
            "StatsText",
            typeof(RectTransform)
        );

    statsObject.transform.SetParent(
        card.transform,
        false
    );

    LayoutElement statsElement =
        statsObject.AddComponent<LayoutElement>();

    statsElement.preferredHeight = 190f;
    statsElement.minHeight = 190f;

    TextMeshProUGUI statsText =
        statsObject.AddComponent<TextMeshProUGUI>();

    int waveCount =
        lvl.Waves != null
            ? lvl.Waves.Count
            : 0;

    string waveDisplay =
        _endlessModeEnabled
            ? "∞"
            : waveCount.ToString();

    string waveLabel =
        _endlessModeEnabled
            ? "ENDLESS WAVES"
            : "TOTAL WAVES";

    statsText.text =
        $"STARTING GOLD\n" +
        $"<color=#FFD700>{lvl.StartingGold} G</color>\n\n" +

        $"BASE HP\n" +
        $"<color=#FF5555>{lvl.BaseMaxHealth} HP</color>\n\n" +

        $"{waveLabel}\n" +
        $"<color=#55FFFF>{waveDisplay}</color>";

    // Bigger text
    statsText.fontSize = 17f;

    statsText.lineSpacing = 4f;

    statsText.color =
        new Color(0.85f, 0.85f, 0.90f, 1f);

    statsText.alignment =
        TextAlignmentOptions.Center;

    statsText.enableWordWrapping = true;

    statsText.overflowMode =
        TextOverflowModes.Ellipsis;

    // =========================================================
    // SAVE STATS REFERENCE
    // =========================================================

    if (_levelStatsTextByLevel.ContainsKey(lvl))
    {
        _levelStatsTextByLevel[lvl] =
            statsText;
    }
    else
    {
        _levelStatsTextByLevel.Add(
            lvl,
            statsText
        );
    }

    // =========================================================
    // SPACER
    // =========================================================

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
        spacer.AddComponent<LayoutElement>();

    spacerElement.flexibleHeight = 1f;

    // =========================================================
    // SELECT LEVEL BUTTON
    // =========================================================

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

    LayoutElement buttonElement =
        buttonObject.AddComponent<LayoutElement>();

    buttonElement.preferredHeight = 52f;
    buttonElement.minHeight = 52f;

    Image buttonImage =
        buttonObject.GetComponent<Image>();

    Color buttonColor =
        new Color(
            0.00f,
            0.62f,
            0.12f,
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
            0.10f,
            0.75f,
            0.25f,
            1f
        );

    colors.pressedColor =
        new Color(
            0.00f,
            0.48f,
            0.08f,
            1f
        );

    button.colors =
        colors;

    CreateText(
        buttonObject.transform,
        "Text",
        "SELECT LEVEL",
        15,
        FontStyles.Bold,
        Color.white,
        Vector2.zero,
        Vector2.one,
        Vector2.zero
    );

    LevelData targetLevel =
        lvl;

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
        Debug.LogError(
            "[UIManager] Selected LevelData is NULL!"
        );

        return;
    }

    // ---------------------------------------------------------
    // SAVE THE FINAL ENDLESS SETTING
    // ---------------------------------------------------------

    bool endlessEnabled = _endlessModeEnabled;

    IsEndlessModeEnabled = endlessEnabled;

    PlayerPrefs.SetInt(
        "TowerDefense_EndlessMode",
        endlessEnabled ? 1 : 0
    );

    PlayerPrefs.Save();

    Debug.Log(
        $"[UIManager] Starting level: {levelData.LevelName} | " +
        $"Endless = {endlessEnabled}"
    );

    // ---------------------------------------------------------
    // HIDE MENUS
    // ---------------------------------------------------------

    if (_difficultySelectionPanel != null)
        _difficultySelectionPanel.SetActive(false);

    if (_challengeSettingsPanel != null)
        _challengeSettingsPanel.SetActive(false);

    if (_levelSelectionPanel != null)
        _levelSelectionPanel.SetActive(false);

    Time.timeScale = 1f;

    // ---------------------------------------------------------
    // START THE CORRECT GAME MODE
    // ---------------------------------------------------------

    if (GameManager.Instance != null)
    {
        if (endlessEnabled)
        {
            Debug.Log(
                "[UIManager] Starting ENDLESS MODE."
            );

            GameManager.Instance.StartEndlessMode(
                levelData
            );
        }
        else
        {
            Debug.Log(
                "[UIManager] Starting FIXED WAVES MODE."
            );

            GameManager.Instance.StartLevel(
                levelData
            );
        }

        return;
    }

    // ---------------------------------------------------------
    // FALLBACK: LOAD SCENE
    // ---------------------------------------------------------

    GameManager.startAsEndless = endlessEnabled;

    UnityEngine.SceneManagement.SceneManager.LoadScene(
        levelData.LevelName
    );
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

        public void EnsureInfoPanel()
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
                    "Title",
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
                    "Stats",
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

        // =========================================================
        // DYNAMIC HUD GRAPHICS SETUP
        // =========================================================

        private void InitializeHUDGraphics()
        {
            if (gameplayHUDPanel == null) return;
            if (gameplayHUDPanel.transform.Find("Gold_Panel") != null) return;

            // Load prefabs and sprites if not assigned in Editor
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            if (goldPrefab == null)
                goldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/gold.prefab");
            if (healthPrefab == null)
                healthPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/health.prefab");
            if (pausePrefab == null)
                pausePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/pause.prefab");
            if (waveSprite == null)
                waveSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Palettes/bound.png");
            #endif

            // Fallback for standalone builds if assets are missing
            if (waveSprite == null) waveSprite = LoadSpriteRuntime("Palettes/bound.png");

            // Format initial values
            if (healthText != null)
            {
                healthText.text = healthText.text.Replace("HP: ", "").Replace("HP:", "");
            }
            if (goldText != null)
            {
                goldText.text = goldText.text.Replace("Gold: ", "").Replace("Gold:", "");
            }

            // Setup Gold Panel
            if (goldText != null && goldPrefab != null)
            {
                GameObject goldGO = ConvertSpritePrefabToUI(goldPrefab, gameplayHUDPanel.transform);
                if (goldGO != null)
                {
                    goldGO.name = "Gold_Panel";
                    ConfigureHUDPanel(goldGO, ref goldText, "Gold");
                    goldGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 480f);
                }
            }

            // Setup Health Panel
            if (healthText != null && healthPrefab != null)
            {
                GameObject healthGO = ConvertSpritePrefabToUI(healthPrefab, gameplayHUDPanel.transform);
                if (healthGO != null)
                {
                    healthGO.name = "Health_Panel";
                    ConfigureHUDPanel(healthGO, ref healthText, "Health");
                    healthGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-500f, 480f);
                }
            }

            // Setup Wave Panel
            SetupWavePanel();
            Transform waveBackground = gameplayHUDPanel.transform.Find("Wave_Background");
            if (waveBackground != null)
            {
                waveBackground.GetComponent<RectTransform>().anchoredPosition = new Vector2(500f, 480f);
            }

            // Setup Pause Button
            SetupPausePrefabButton();

            // Destroy Start Wave Button if it exists
            Transform startWaveBtn = gameplayHUDPanel.transform.Find("StartWaveButton");
            if (startWaveBtn != null)
            {
                Destroy(startWaveBtn.gameObject);
            }

            // Position Time text at top-left
            if (timeText != null)
            {
                RectTransform timeRect = timeText.GetComponent<RectTransform>();
                if (timeRect != null)
                {
                    timeRect.anchorMin = new Vector2(0f, 1f);
                    timeRect.anchorMax = new Vector2(0f, 1f);
                    timeRect.pivot = new Vector2(0f, 1f);
                    timeRect.anchoredPosition = new Vector2(30f, -30f);
                    timeText.alignment = TextAlignmentOptions.Left;
                }
            }
        }

        private GameObject ConvertSpritePrefabToUI(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, parent);
            ConvertSpriteToImageRecursive(go, true);
            return go;
        }

        private void ConvertSpriteToImageRecursive(GameObject go, bool isRoot)
        {
            if (go == null) return;

            Vector3 localPos = go.transform.localPosition;
            Vector3 localScale = go.transform.localScale;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            Sprite sprite = null;
            Color color = Color.white;
            if (sr != null)
            {
                sprite = sr.sprite;
                color = sr.color;
                DestroyImmediate(sr);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }

            // Convert localPosition from world units to UI pixels (1 unit = 100 pixels)
            if (!isRoot)
            {
                rect.anchoredPosition = new Vector2(localPos.x * 100f, localPos.y * 100f);
            }
            rect.localScale = localScale;

            if (sprite != null)
            {
                Image img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.color = color;
                img.preserveAspect = true;
                img.SetNativeSize();
            }

            // Gather children first to handle Unity replacing standard Transform with RectTransform safely
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in go.transform)
            {
                if (child != null)
                {
                    children.Add(child.gameObject);
                }
            }

            foreach (GameObject child in children)
            {
                ConvertSpriteToImageRecursive(child, false);
            }
        }

        private void ConfigureHUDPanel(GameObject panelGO, ref TextMeshProUGUI textComp, string namePrefix)
        {
            RectTransform mainRect = panelGO.GetComponent<RectTransform>();
            RectTransform oldTextRect = textComp != null ? textComp.GetComponent<RectTransform>() : null;

            // 1. Try to find a pre-existing TextMeshProUGUI component in the prefab hierarchy
            TextMeshProUGUI prefabText = panelGO.GetComponentInChildren<TextMeshProUGUI>(true);
            if (prefabText != null)
            {
                // Deactivate the old scene text and redirect reference to the prefab's text
                if (textComp != null)
                {
                    textComp.gameObject.SetActive(false);
                }
                textComp = prefabText;

                // Native prefab positioning: map panel to the old text position if not custom positioned in prefab
                if (oldTextRect != null && mainRect.anchoredPosition == Vector2.zero)
                {
                    mainRect.anchorMin = oldTextRect.anchorMin;
                    mainRect.anchorMax = oldTextRect.anchorMax;
                    mainRect.pivot = oldTextRect.pivot;
                    mainRect.anchoredPosition = oldTextRect.anchoredPosition;
                }
                return;
            }

            // 2. Fallback: Reparent the scene's text component if no text is defined in the prefab
            if (textComp != null && oldTextRect != null)
            {
                mainRect.anchorMin = oldTextRect.anchorMin;
                mainRect.anchorMax = oldTextRect.anchorMax;
                mainRect.pivot = oldTextRect.pivot;
                mainRect.anchoredPosition = oldTextRect.anchoredPosition;

                // Set size of the panel to be exactly 240x80 visually
                mainRect.sizeDelta = new Vector2(240f / mainRect.localScale.x, 80f / mainRect.localScale.y);

                textComp.transform.SetParent(panelGO.transform, false);
                RectTransform textRect = textComp.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.45f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(10f, 0f);
                textRect.sizeDelta = Vector2.zero;
                textComp.alignment = TextAlignmentOptions.MidlineLeft;

                // Scale down font size dynamically so the text matches the wave text size on screen
                float targetFontSize = (waveText != null) ? waveText.fontSize : 28f;
                textComp.fontSize = targetFontSize / mainRect.localScale.x;

                // Scale down child icon by the parent's scale factor to preserve original prefab scale on screen
                if (panelGO.transform.childCount > 0)
                {
                    Transform iconTrans = panelGO.transform.GetChild(0);
                    if (iconTrans != textComp.transform)
                    {
                        RectTransform iconRect = iconTrans.GetComponent<RectTransform>();
                        Vector3 originalChildScale = iconRect.localScale;
                        iconRect.localScale = new Vector3(originalChildScale.x / mainRect.localScale.x, originalChildScale.y / mainRect.localScale.y, 1f);
                    }
                }
            }
        }

        private void SetupWavePanel()
        {
            if (waveText == null) return;

            // Create the background panel using the waveSprite (bound.png)
            GameObject bgGO = new GameObject("Wave_Background", typeof(RectTransform), typeof(CanvasRenderer));
            bgGO.transform.SetParent(gameplayHUDPanel.transform, false);

            if (waveSprite != null)
            {
                Image bgImg = bgGO.AddComponent<Image>();
                bgImg.sprite = waveSprite;
                bgImg.preserveAspect = false;
                bgImg.type = Image.Type.Simple;
            }

            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            RectTransform textRect = waveText.GetComponent<RectTransform>();

            bgRect.anchorMin = textRect.anchorMin;
            bgRect.anchorMax = textRect.anchorMax;
            bgRect.pivot = textRect.pivot;
            bgRect.anchoredPosition = textRect.anchoredPosition;
            bgRect.sizeDelta = new Vector2(240f, 80f);

            // Reparent wave text
            waveText.transform.SetParent(bgGO.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            waveText.alignment = TextAlignmentOptions.Center;
        }

        private void SetupPausePrefabButton()
        {
            if (gameplayHUDPanel == null || pausePrefab == null) return;

            Transform oldPauseBtnTrans = gameplayHUDPanel.transform.Find("PauseButton");
            Vector2 position = new Vector2(850f, 480f);
            if (oldPauseBtnTrans != null)
            {
                position = oldPauseBtnTrans.GetComponent<RectTransform>().anchoredPosition;
                DestroyImmediate(oldPauseBtnTrans.gameObject);
            }

            GameObject pauseGO = ConvertSpritePrefabToUI(pausePrefab, gameplayHUDPanel.transform);
            if (pauseGO != null)
            {
                pauseGO.name = "PauseButton";

                RectTransform rect = pauseGO.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;

                // Respect pre-existing Button component if one exists in the prefab
                Button btn = pauseGO.GetComponent<Button>();
                if (btn == null)
                {
                    btn = pauseGO.AddComponent<Button>();
                }
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPauseButtonClicked);

                Image img = pauseGO.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;

                foreach (Transform child in pauseGO.transform)
                {
                    Image childImg = child.GetComponent<Image>();
                    if (childImg != null) childImg.raycastTarget = true;
                }
            }
        }

        private Sprite LoadSpriteRuntime(string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Failed to load sprite at runtime: {relativePath}. Error: {e.Message}");
            }
            return null;
        }

        private void EnsureTimeTextsExist()
        {
            // 1. timeText
            if (timeText == null && gameplayHUDPanel != null)
            {
                Transform existing = gameplayHUDPanel.transform.Find("TimeText");
                if (existing != null)
                {
                    timeText = existing.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject go = new GameObject("TimeText", typeof(RectTransform), typeof(CanvasRenderer));
                    go.transform.SetParent(gameplayHUDPanel.transform, false);
                    timeText = go.AddComponent<TextMeshProUGUI>();
                    timeText.fontSize = 28f;
                    timeText.color = Color.white;
                    timeText.text = "TIME: 00:00";
                }
            }

            // 2. victoryTimeText
            if (victoryTimeText == null && victoryOverlayPanel != null)
            {
                Transform existing = victoryOverlayPanel.transform.Find("VictoryTimeText");
                if (existing != null)
                {
                    victoryTimeText = existing.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject go = new GameObject("VictoryTimeText", typeof(RectTransform), typeof(CanvasRenderer));
                    go.transform.SetParent(victoryOverlayPanel.transform, false);
                    victoryTimeText = go.AddComponent<TextMeshProUGUI>();
                    victoryTimeText.fontSize = 24f;
                    victoryTimeText.color = Color.white;
                    victoryTimeText.alignment = TextAlignmentOptions.Center;
                    victoryTimeText.text = "TIME: 00:00";

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 133f);
                    rect.sizeDelta = new Vector2(200f, 50f);
                }
            }

            // 3. defeatTimeText
            if (defeatTimeText == null && defeatOverlayPanel != null)
            {
                Transform existing = defeatOverlayPanel.transform.Find("DefeatTimeText");
                if (existing != null)
                {
                    defeatTimeText = existing.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject go = new GameObject("DefeatTimeText", typeof(RectTransform), typeof(CanvasRenderer));
                    go.transform.SetParent(defeatOverlayPanel.transform, false);
                    defeatTimeText = go.AddComponent<TextMeshProUGUI>();
                    defeatTimeText.fontSize = 24f;
                    defeatTimeText.color = Color.white;
                    defeatTimeText.alignment = TextAlignmentOptions.Center;
                    defeatTimeText.text = "TIME: 00:00";

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 133f);
                    rect.sizeDelta = new Vector2(200f, 50f);
                }
            }
        }
    }
}
