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

        [Header("HUD Prefabs & Sprites")]
        [SerializeField] private GameObject goldPrefab;
        [SerializeField] private GameObject healthPrefab;
        [SerializeField] private GameObject pausePrefab;
        [SerializeField] private Sprite waveSprite;

        [Header("Level Data (For Main Menu Play Button)")]
        [SerializeField] private LevelData levelDataToPlay;

        [Header("Level Selection")]
        [SerializeField] private System.Collections.Generic.List<LevelData> levels = new System.Collections.Generic.List<LevelData>();

        // Properties for editor setup bypass
        public LevelData LevelDataToPlay { get => levelDataToPlay; set => levelDataToPlay = value; }
        public System.Collections.Generic.List<LevelData> Levels { get => levels; set => levels = value; }

        private GameObject _levelSelectionPanel;

        // Selection system fields
        private TowerDefense.Tower.TowerController _selectedTower;
        private TowerDefense.Enemy.EnemyHealth _selectedEnemy;
        private GameObject _infoPanel;
        private TextMeshProUGUI _infoTitleText;
        private TextMeshProUGUI _infoStatsText;
        private GameObject _lvlUpBtnGO;
        private Button _lvlUpBtn;
        private TextMeshProUGUI _lvlUpBtnText;

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

            // Initialize HUD icons and pause button graphics
            InitializeHUDGraphics();
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
                healthText.text = $"{evt.CurrentHealth}/{evt.MaxHealth}";
            }
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            if (goldText != null)
            {
                goldText.text = $"{evt.CurrentGold}";
            }
            UpdateSelectedStatsDisplay();
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

        #region Stats Info Panel System

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                if (_infoPanel != null && _infoPanel.activeSelf)
                {
                    _infoPanel.SetActive(false);
                }
                return;
            }

            // Update live stats of selected target if valid
            UpdateSelectedStatsDisplay();

            // Left click triggers selection overlap point check
            bool leftClick = false;
            Vector2 mouseScreenPos = Vector2.zero;

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    leftClick = true;
                    mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
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

            if (leftClick)
            {
                // Check if clicking on interactive UI
                if (IsPointerOverInteractiveUI(mouseScreenPos))
                {
                    return;
                }

                if (Camera.main == null) return;

                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.nearClipPlane));
                Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

                Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos2D, 0.6f);
                Collider2D closestHit = null;
                float closestDist = float.MaxValue;
                TowerDefense.Tower.TowerController targetTower = null;
                TowerDefense.Enemy.EnemyHealth targetEnemy = null;

                foreach (var hit in hits)
                {
                    if (hit == null) continue;
                    TowerDefense.Tower.TowerController tower = hit.GetComponent<TowerDefense.Tower.TowerController>();
                    TowerDefense.Enemy.EnemyHealth enemy = hit.GetComponent<TowerDefense.Enemy.EnemyHealth>();

                    if (tower != null || enemy != null)
                    {
                        float dist = Vector2.Distance(worldPos2D, hit.transform.position);
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
        }

        private void SelectTower(TowerDefense.Tower.TowerController tower)
        {
            _selectedTower = tower;
            _selectedEnemy = null;
            EnsureInfoPanel();
            _infoPanel.SetActive(true);
            UpdateSelectedStatsDisplay();
        }

        private void SelectEnemy(TowerDefense.Enemy.EnemyHealth enemy)
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
            if (_infoPanel == null || !_infoPanel.activeSelf) return;

            if (_selectedTower != null)
            {
                if (_selectedTower == null || _selectedTower.gameObject == null)
                {
                    Deselect();
                    return;
                }

                TowerData data = _selectedTower.TowerData;
                string name = data != null ? data.TowerName : "Tower";
                float fireRate = data != null ? data.FireRate : 0f;

                if (_infoTitleText != null) _infoTitleText.text = name.ToUpper() + $" (LVL {_selectedTower.CurrentLevel})";
                if (_infoStatsText != null)
                {
                    _infoStatsText.text = $"DAMAGE: <color=#FFD700>{_selectedTower.CurrentDamage}</color>\n\n" +
                                          $"FIRE RATE: <color=#55FFFF>{fireRate:F1}/s</color>\n\n" +
                                          $"RANGE: <color=#55FF55>{_selectedTower.CurrentRange:F1}</color>";
                }

                if (_lvlUpBtnGO != null)
                {
                    if (_selectedTower.CurrentLevel < _selectedTower.MaxLevel)
                    {
                        _lvlUpBtnGO.SetActive(true);
                        int cost = _selectedTower.UpgradeCost;
                        bool canAfford = GameManager.Instance == null || GameManager.Instance.CurrentGold >= cost;

                        if (_lvlUpBtnText != null)
                        {
                            _lvlUpBtnText.text = $"UPGRADE ({cost} G)";
                        }

                        if (_lvlUpBtn != null)
                        {
                            _lvlUpBtn.interactable = canAfford;
                            Image btnImg = _lvlUpBtnGO.GetComponent<Image>();
                            if (btnImg != null)
                            {
                                btnImg.color = canAfford ? new Color(0.12f, 0.75f, 0.38f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);
                            }
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
                if (_selectedEnemy == null || _selectedEnemy.gameObject == null || _selectedEnemy.IsDead)
                {
                    Deselect();
                    return;
                }

                string name = _selectedEnemy.EnemyData != null ? _selectedEnemy.EnemyData.EnemyName : "Enemy";
                int hp = _selectedEnemy.CurrentHealth;
                int maxHp = _selectedEnemy.MaxHealth;
                float speed = _selectedEnemy.MoveSpeed;
                int armor = _selectedEnemy.Armor;
                int attack = _selectedEnemy.Attack;

                if (_infoTitleText != null) _infoTitleText.text = name.ToUpper();
                if (_infoStatsText != null)
                {
                    _infoStatsText.text = $"HP: <color=#FF5555>{hp}/{maxHp}</color>\n\n" +
                                          $"SPEED: <color=#55FF55>{speed:F1}</color>\n\n" +
                                          $"ARMOR: <color=#AAAAAA>{armor}</color>\n\n" +
                                          $"DAMAGE TO BASE: <color=#FF5555>{attack}</color>";
                }

                if (_lvlUpBtnGO != null) _lvlUpBtnGO.SetActive(false);
            }
            else
            {
                Deselect();
            }
        }

        private void EnsureInfoPanel()
        {
            if (_infoPanel != null) return;

            Transform parent = gameplayHUDPanel != null ? gameplayHUDPanel.transform : transform;

            // Create Main Panel
            _infoPanel = new GameObject("InfoPanel", typeof(RectTransform), typeof(CanvasRenderer));
            _infoPanel.transform.SetParent(parent, false);

            RectTransform rect = _infoPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-20f, 0f);
            rect.sizeDelta = new Vector2(280f, 250f);

            Image bgImage = _infoPanel.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.09f, 0.15f, 0.92f); // Sleek dark slate glass style

            // Highlight header bar
            GameObject topBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(CanvasRenderer));
            topBar.transform.SetParent(_infoPanel.transform, false);
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0f, 6f);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = new Color(0.2f, 0.6f, 1f, 1f); // Sleek blue highlight bar

            // Title Text
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
            titleGO.transform.SetParent(_infoPanel.transform, false);
            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(15f, -15f);
            titleRect.sizeDelta = new Vector2(-50f, 35f);

            _infoTitleText = titleGO.AddComponent<TextMeshProUGUI>();
            _infoTitleText.fontSize = 18f;
            _infoTitleText.fontStyle = FontStyles.Bold;
            _infoTitleText.color = Color.white;
            _infoTitleText.alignment = TextAlignmentOptions.Left;

            // Stats Text
            GameObject statsGO = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer));
            statsGO.transform.SetParent(_infoPanel.transform, false);
            RectTransform statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = new Vector2(0f, -30f);
            statsRect.sizeDelta = new Vector2(-30f, -80f);

            _infoStatsText = statsGO.AddComponent<TextMeshProUGUI>();
            _infoStatsText.fontSize = 14f;
            _infoStatsText.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            _infoStatsText.alignment = TextAlignmentOptions.TopLeft;

            // Close button
            GameObject closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer));
            closeBtnGO.transform.SetParent(_infoPanel.transform, false);
            RectTransform closeRect = closeBtnGO.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            closeRect.sizeDelta = new Vector2(25f, 25f);

            Image closeImg = closeBtnGO.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            Button closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.onClick.AddListener(Deselect);

            // Add text to close button
            GameObject closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            RectTransform closeTextRect = closeTextGO.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 12f;
            closeText.fontStyle = FontStyles.Bold;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;

            // Create Level Up / Upgrade Button
            _lvlUpBtnGO = new GameObject("UpgradeButton", typeof(RectTransform), typeof(CanvasRenderer));
            _lvlUpBtnGO.transform.SetParent(_infoPanel.transform, false);
            RectTransform lvlUpRect = _lvlUpBtnGO.GetComponent<RectTransform>();
            lvlUpRect.anchorMin = new Vector2(0.5f, 0f);
            lvlUpRect.anchorMax = new Vector2(0.5f, 0f);
            lvlUpRect.pivot = new Vector2(0.5f, 0f);
            lvlUpRect.anchoredPosition = new Vector2(0f, 15f);
            lvlUpRect.sizeDelta = new Vector2(240f, 40f);

            Image lvlUpImg = _lvlUpBtnGO.AddComponent<Image>();
            lvlUpImg.color = new Color(0.12f, 0.75f, 0.38f, 1f); // Sleek green

            _lvlUpBtn = _lvlUpBtnGO.AddComponent<Button>();
            _lvlUpBtn.onClick.AddListener(OnUpgradeButtonClicked);

            // Add Text
            GameObject lvlUpTextGO = new GameObject("Text", typeof(RectTransform));
            lvlUpTextGO.transform.SetParent(_lvlUpBtnGO.transform, false);
            RectTransform lvlUpTextRect = lvlUpTextGO.GetComponent<RectTransform>();
            lvlUpTextRect.anchorMin = Vector2.zero;
            lvlUpTextRect.anchorMax = Vector2.one;
            lvlUpTextRect.offsetMin = Vector2.zero;
            lvlUpTextRect.offsetMax = Vector2.zero;

            _lvlUpBtnText = lvlUpTextGO.AddComponent<TextMeshProUGUI>();
            _lvlUpBtnText.text = "UPGRADE";
            _lvlUpBtnText.fontSize = 14f;
            _lvlUpBtnText.fontStyle = FontStyles.Bold;
            _lvlUpBtnText.color = Color.white;
            _lvlUpBtnText.alignment = TextAlignmentOptions.Center;

            _lvlUpBtnGO.SetActive(false);
        }

        private void OnUpgradeButtonClicked()
        {
            if (_selectedTower != null && GameManager.Instance != null)
            {
                int cost = _selectedTower.UpgradeCost;
                if (_selectedTower.CurrentLevel < _selectedTower.MaxLevel && GameManager.Instance.TrySpendGold(cost))
                {
                    _selectedTower.LevelUp();
                    UpdateSelectedStatsDisplay();
                }
            }
        }

        private bool IsPointerOverInteractiveUI(Vector2 screenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = screenPos;

            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null)
                {
                    string name = result.gameObject.name;
                    // Ignore root canvas, gameplay HUD, and EventSystem
                    if (name == "GameplayHUDPanel" || name == "Canvas" || name == "EventSystem")
                    {
                        continue;
                    }
                    return true;
                }
            }
            return false;
        }

        private void InitializeHUDGraphics()
        {
            if (gameplayHUDPanel == null) return;
            if (gameplayHUDPanel.transform.Find("Gold_Panel") != null) return;

            // Load prefabs and sprites if not assigned in Editor
            #if UNITY_EDITOR
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

        #endregion
    }
}
