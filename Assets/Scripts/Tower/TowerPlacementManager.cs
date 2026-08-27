using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.UI;

namespace TowerDefense.Tower
{
    // =============================================================
    // TOWER INVESTMENT DATA
    // =============================================================

    [Serializable]
    public class TowerInvestmentData
    {
        public int purchaseCost;
        public int upgradeInvested;

        public int TotalInvested
        {
            get
            {
                return Mathf.Max(
                    0,
                    purchaseCost + upgradeInvested
                );
            }
        }

        public TowerInvestmentData(int purchase)
        {
            purchaseCost = Mathf.Max(
                0,
                purchase
            );

            upgradeInvested = 0;
        }

        public void AddUpgrade(int cost)
        {
            upgradeInvested += Mathf.Max(
                0,
                cost
            );
        }
    }


    // =============================================================
    // TOWER PLACEMENT MANAGER
    // =============================================================

    public class TowerPlacementManager : MonoBehaviour
    {
        // =========================================================
        // SINGLETON
        // =========================================================

        public static TowerPlacementManager Instance
        {
            get;
            private set;
        }


        // =========================================================
        // PLACEMENT SETTINGS
        // =========================================================

        [Header("Placement Settings")]

        [SerializeField]
        private Color validColor =
            new Color(
                0f,
                1f,
                0.8f,
                0.5f
            );

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.1f,
                0.1f,
                0.5f
            );


        // =========================================================
        // RADIAL MENU
        // =========================================================

        [Header("Radial Tower Menu")]

        // Tăng vùng click
        [SerializeField]
        private float radialMenuRadius = 145f;

        [SerializeField]
        private float radialButtonSize = 120f;

        [SerializeField]
        private Color radialButtonColor =
            new Color(
                0.12f,
                0.15f,
                0.22f,
                0.95f
            );

        [SerializeField]
        private Color radialButtonHighlightColor =
            new Color(
                0.2f,
                0.55f,
                0.85f,
                1f
            );


        // =========================================================
        // INFO PANEL
        // =========================================================

        [Header("Info Panel")]

        [SerializeField]
        private GameObject infoPanel;

        [SerializeField]
        private Color sellButtonColor =
            new Color(
                0.65f,
                0.10f,
                0.10f,
                1f
            );

        [SerializeField]
        private Color sellButtonHighlightColor =
            new Color(
                0.90f,
                0.18f,
                0.18f,
                1f
            );


        // =========================================================
        // SELL
        // =========================================================

        [Header("Sell Settings")]

        [SerializeField]
        [Range(0.1f, 1f)]
        private float sellRefundRate = 0.75f;


        // =========================================================
        // BASIC
        // =========================================================

        [Header("Basic Tower")]

        [SerializeField]
        private TowerData defaultTowerData;

        [SerializeField]
        private GameObject defaultTowerPrefab;


        // =========================================================
        // FAST
        // =========================================================

        [Header("Fast Tower")]

        [SerializeField]
        private TowerData fastTowerData;

        [SerializeField]
        private GameObject fastTowerPrefab;


        // =========================================================
        // GOLD
        // =========================================================

        [Header("Gold Tower")]

        [SerializeField]
        private TowerData goldTowerData;

        [SerializeField]
        private GameObject goldTowerPrefab;


        // =========================================================
        // CANNON
        // =========================================================

        [Header("Cannon Tower")]

        [SerializeField]
        private TowerData cannonTowerData;

        [SerializeField]
        private GameObject cannonTowerPrefab;


        // =========================================================
        // ICE
        // =========================================================

        [Header("Ice Tower")]

        [SerializeField]
        private TowerData iceTowerData;

        [SerializeField]
        private GameObject iceTowerPrefab;


        // =========================================================
        // LASER
        // =========================================================

        [Header("Laser Tower")]

        [SerializeField]
        private TowerData laserTowerData;

        [SerializeField]
        private GameObject laserTowerPrefab;


        // =========================================================
        // RUNTIME PLACEMENT
        // =========================================================

        private GameObject _previewInstance;
        private TowerData _activeTowerData;
        private GameObject _towerPrefab;
        private SpriteRenderer _previewRenderer;
        private bool _isPlacing;

        private WaypointPath _cachedPath;

        private readonly List<GameObject> _placedTowers =
            new List<GameObject>();

        private TextMeshProUGUI _warningTextInstance;


        // =========================================================
        // RADIAL
        // =========================================================

        private GameObject _radialMenu;
        private BuildSite _selectedBuildSite;
        private Canvas _canvas;


        // =========================================================
        // INFO
        // =========================================================

        private BuildSite _infoBuildSite;
        private TowerController _infoTowerController;

        private TextMeshProUGUI _towerInfoTitle;
        private TextMeshProUGUI _towerInfoStats;

        private Button _upgradeButton;
        private Button _sellInfoButton;

        private TextMeshProUGUI _upgradeButtonText;
        private TextMeshProUGUI _sellButtonText;


        // =========================================================
        // INVESTMENT
        // =========================================================

        private readonly Dictionary<
            GameObject,
            TowerInvestmentData
        > _towerInvestments =
            new Dictionary<
                GameObject,
                TowerInvestmentData
            >();


        // =========================================================
        // PROPERTIES
        // =========================================================

        public bool IsPlacing
        {
            get
            {
                return _isPlacing;
            }
        }

        public TowerData ActiveTowerData
        {
            get
            {
                return _activeTowerData;
            }
        }


        // =========================================================
        // DATA PROPERTIES
        // =========================================================

        public TowerData DefaultTowerData
        {
            get => defaultTowerData;
            set => defaultTowerData = value;
        }

        public GameObject DefaultTowerPrefab
        {
            get => defaultTowerPrefab;
            set => defaultTowerPrefab = value;
        }

        public TowerData FastTowerData
        {
            get => fastTowerData;
            set => fastTowerData = value;
        }

        public GameObject FastTowerPrefab
        {
            get => fastTowerPrefab;
            set => fastTowerPrefab = value;
        }

        public TowerData GoldTowerData
        {
            get => goldTowerData;
            set => goldTowerData = value;
        }

        public GameObject GoldTowerPrefab
        {
            get => goldTowerPrefab;
            set => goldTowerPrefab = value;
        }

        public TowerData CannonTowerData
        {
            get => cannonTowerData;
            set => cannonTowerData = value;
        }

        public GameObject CannonTowerPrefab
        {
            get => cannonTowerPrefab;
            set => cannonTowerPrefab = value;
        }

        public TowerData IceTowerData
        {
            get => iceTowerData;
            set => iceTowerData = value;
        }

        public GameObject IceTowerPrefab
        {
            get => iceTowerPrefab;
            set => iceTowerPrefab = value;
        }

        public TowerData LaserTowerData
        {
            get => laserTowerData;
            set => laserTowerData = value;
        }

        public GameObject LaserTowerPrefab
        {
            get => laserTowerPrefab;
            set => laserTowerPrefab = value;
        }


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            if (
                Instance != null &&
                Instance != this
            )
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            LoadTowerAssetsInEditor();
            EnsureGoldTowerDataLoaded();
#endif

            EnsureUIInputSystem();

            Debug.Log(
                "[TowerPlacementManager] Initialized."
            );
        }


        // =========================================================
        // ENABLE
        // =========================================================

        private void OnEnable()
        {
            EventBus<LevelStartedEvent>
                .Subscribe(
                    OnLevelStarted
                );
        }


        // =========================================================
        // DISABLE
        // =========================================================

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>
                .Unsubscribe(
                    OnLevelStarted
                );
        }


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            _cachedPath =
                FindAnyObjectByType<WaypointPath>();

            _canvas =
                FindAnyObjectByType<Canvas>();

            EnsureUIInputSystem();

            HideOldShopPanel();
            EnsureExistingInfoPanel();

            CloseTowerInfoPanel();
            CloseRadialMenu();
        }


        // =========================================================
        // UI INPUT SYSTEM
        // =========================================================

        private void EnsureUIInputSystem()
        {
            if (_canvas == null)
            {
                _canvas =
                    FindAnyObjectByType<Canvas>();
            }

            // -----------------------------------------------------
            // EVENT SYSTEM
            // -----------------------------------------------------

            EventSystem eventSystem =
                EventSystem.current;

            if (eventSystem == null)
            {
                eventSystem =
                    FindAnyObjectByType<EventSystem>();
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject =
                    new GameObject(
                        "EventSystem",
                        typeof(EventSystem)
                    );

                eventSystem =
                    eventSystemObject.GetComponent<
                        EventSystem
                    >();

                // Dùng InputSystem nếu package mới tồn tại.
                #if ENABLE_INPUT_SYSTEM
                eventSystemObject.AddComponent<
                    UnityEngine.InputSystem.UI.InputSystemUIInputModule
                >();
                #else
                eventSystemObject.AddComponent<
                    StandaloneInputModule
                >();
                #endif

                Debug.Log(
                    "[TowerPlacementManager] " +
                    "EventSystem created."
                );
            }

            if (eventSystem != null)
            {
                eventSystem.enabled = true;
            }


            // -----------------------------------------------------
            // CANVAS RAYCASTER
            // -----------------------------------------------------

            if (_canvas == null)
                return;

            GraphicRaycaster raycaster =
                _canvas.GetComponent<
                    GraphicRaycaster
                >();

            if (raycaster == null)
            {
                raycaster =
                    _canvas.gameObject.AddComponent<
                        GraphicRaycaster
                    >();

                Debug.Log(
                    "[TowerPlacementManager] " +
                    "GraphicRaycaster added."
                );
            }

            raycaster.enabled = true;
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (GameManager.Instance == null)
                return;

            if (
                GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing
            )
            {
                CloseRadialMenu();
                CloseTowerInfoPanel();

                if (_isPlacing)
                {
                    Cleanup();
                }

                return;
            }

            if (_isPlacing)
                return;

            if (_infoTowerController != null)
            {
                if (
                    !_infoTowerController
                        .gameObject
                        .activeInHierarchy
                )
                {
                    CloseTowerInfoPanel();
                }
                else
                {
                    UpdateTowerInfoPanel();
                }
            }
        }


        // =========================================================
        // ASSET LOADING
        // =========================================================

#if UNITY_EDITOR

        private void LoadTowerAssetsInEditor()
        {
            if (defaultTowerData == null)
            {
                defaultTowerData =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            "Assets/ScriptableObjects/TestTowerData.asset"
                        );
            }

            if (defaultTowerPrefab == null)
            {
                defaultTowerPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(
                            "Assets/Prefabs/Tower.prefab"
                        );
            }


            if (fastTowerData == null)
            {
                fastTowerData =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            "Assets/ScriptableObjects/FastTowerData.asset"
                        );
            }

            if (fastTowerPrefab == null)
            {
                fastTowerPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(
                            "Assets/Prefabs/FastTower.prefab"
                        );
            }


            EnsureGoldTowerDataLoaded();


            if (cannonTowerData == null)
            {
                cannonTowerData =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            "Assets/ScriptableObjects/CannonTowerData.asset"
                        );
            }

            if (cannonTowerPrefab == null)
            {
                cannonTowerPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(
                            "Assets/Prefabs/CannonTower.prefab"
                        );
            }


            if (iceTowerData == null)
            {
                iceTowerData =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            "Assets/ScriptableObjects/IceTowerData.asset"
                        );
            }

            if (iceTowerPrefab == null)
            {
                iceTowerPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(
                            "Assets/Prefabs/IceTower.prefab"
                        );
            }


            if (laserTowerData == null)
            {
                laserTowerData =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            "Assets/ScriptableObjects/LaserTowerData.asset"
                        );
            }

            if (laserTowerPrefab == null)
            {
                laserTowerPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(
                            "Assets/Prefabs/LaserTower.prefab"
                        );
            }
        }


        private void EnsureGoldTowerDataLoaded()
        {
            if (
                goldTowerData != null &&
                goldTowerData.Type == TowerType.Gold
            )
            {
                return;
            }

            string[] guids =
                UnityEditor.AssetDatabase.FindAssets(
                    "t:TowerData"
                );

            foreach (string guid in guids)
            {
                string path =
                    UnityEditor.AssetDatabase
                        .GUIDToAssetPath(guid);

                TowerData data =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<TowerData>(
                            path
                        );

                if (data == null)
                    continue;

                if (data.Type == TowerType.Gold)
                {
                    goldTowerData = data;

                    return;
                }
            }

            Debug.LogError(
                "[TowerPlacementManager] " +
                "Cannot find Gold TowerData."
            );
        }

#endif


        // =========================================================
        // OPEN RADIAL MENU
        // =========================================================

        public void OpenRadialMenu(
            BuildSite site
        )
        {
            if (site == null)
                return;

            if (
                site.IsOccupied &&
                site.OccupyingTower != null
            )
            {
                TowerController controller =
                    site.OccupyingTower.GetComponent<
                        TowerController
                    >();

                if (controller != null)
                {
                    CloseRadialMenu();

                    ShowTowerInfoPanel(
                        site,
                        controller
                    );

                    return;
                }
            }

            CloseTowerInfoPanel();

            _selectedBuildSite =
                site;

            CreateRadialMenu();

            if (_radialMenu == null)
                return;

            _radialMenu.transform.SetAsLastSibling();

            _radialMenu.SetActive(true);

            PositionRadialMenu(
                site
            );
        }


        // =========================================================
        // CREATE RADIAL MENU
        // =========================================================

        private void CreateRadialMenu()
        {
            EnsureUIInputSystem();

#if UNITY_EDITOR
            EnsureGoldTowerDataLoaded();
#endif

            if (_radialMenu != null)
                return;

            if (_canvas == null)
            {
                _canvas =
                    FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                Debug.LogError(
                    "[TowerPlacementManager] " +
                    "Canvas not found."
                );

                return;
            }


            // -----------------------------------------------------
            // MENU ROOT
            // -----------------------------------------------------

            _radialMenu =
                new GameObject(
                    "TowerRadialMenu",
                    typeof(RectTransform)
                );

            _radialMenu.transform.SetParent(
                _canvas.transform,
                false
            );

            _radialMenu.transform.SetAsLastSibling();

            RectTransform menuRect =
                _radialMenu.GetComponent<
                    RectTransform
                >();

            menuRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            menuRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            menuRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            menuRect.sizeDelta =
                new Vector2(
                    radialMenuRadius * 2f +
                    radialButtonSize,

                    radialMenuRadius * 2f +
                    radialButtonSize
                );


            // -----------------------------------------------------
            // BUTTONS
            // -----------------------------------------------------

            CreateRadialTowerButton(
                "BasicTowerButton",
                "BASIC",
                defaultTowerData,
                defaultTowerPrefab,
                180f
            );

            CreateRadialTowerButton(
                "FastTowerButton",
                "FAST",
                fastTowerData,
                fastTowerPrefab,
                120f
            );


            TowerData finalGoldData =
                goldTowerData;

            GameObject finalGoldPrefab =
                goldTowerPrefab;

#if UNITY_EDITOR

            if (finalGoldData == null)
            {
                EnsureGoldTowerDataLoaded();

                finalGoldData =
                    goldTowerData;
            }

#endif

            CreateRadialTowerButton(
                "GoldTowerButton",
                "GOLD",
                finalGoldData,
                finalGoldPrefab,
                60f
            );

            CreateRadialTowerButton(
                "CannonTowerButton",
                "CANNON",
                cannonTowerData,
                cannonTowerPrefab,
                0f
            );

            CreateRadialTowerButton(
                "IceTowerButton",
                "ICE",
                iceTowerData,
                iceTowerPrefab,
                300f
            );

            CreateRadialTowerButton(
                "LaserTowerButton",
                "LASER",
                laserTowerData,
                laserTowerPrefab,
                240f
            );


            _radialMenu.SetActive(false);
        }


        // =========================================================
        // CREATE RADIAL TOWER BUTTON
        // =========================================================

        private void CreateRadialTowerButton(
            string objectName,
            string title,
            TowerData data,
            GameObject prefab,
            float angle
        )
        {
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            buttonObject.transform.SetParent(
                _radialMenu.transform,
                false
            );


            // -----------------------------------------------------
            // RECT
            // -----------------------------------------------------

            RectTransform rect =
                buttonObject.GetComponent<
                    RectTransform
                >();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            float radians =
                angle *
                Mathf.Deg2Rad;

            rect.anchoredPosition =
                new Vector2(
                    Mathf.Cos(radians) *
                    radialMenuRadius,

                    Mathf.Sin(radians) *
                    radialMenuRadius
                );

            // Tăng hitbox
            rect.sizeDelta =
                new Vector2(
                    radialButtonSize,
                    radialButtonSize
                );


            // -----------------------------------------------------
            // IMAGE
            // -----------------------------------------------------

            Image image =
                buttonObject.GetComponent<
                    Image
                >();

            image.color =
                radialButtonColor;

            image.raycastTarget =
                true;


            // -----------------------------------------------------
            // BUTTON
            // -----------------------------------------------------

            Button button =
                buttonObject.GetComponent<
                    Button
                >();

            button.enabled =
                true;

            button.interactable =
                true;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                radialButtonColor;

            colors.highlightedColor =
                radialButtonHighlightColor;

            colors.pressedColor =
                new Color(
                    0.08f,
                    0.10f,
                    0.15f,
                    1f
                );

            colors.selectedColor =
                radialButtonHighlightColor;

            colors.disabledColor =
                new Color(
                    0.25f,
                    0.25f,
                    0.25f,
                    0.8f
                );

            button.colors =
                colors;


            // -----------------------------------------------------
            // ICON
            // -----------------------------------------------------

            GameObject iconObject =
                new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image)
                );

            iconObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            RectTransform iconRect =
                iconObject.GetComponent<
                    RectTransform
                >();

            iconRect.anchorMin =
                new Vector2(
                    0.10f,
                    0.28f
                );

            iconRect.anchorMax =
                new Vector2(
                    0.90f,
                    0.92f
                );

            iconRect.offsetMin =
                Vector2.zero;

            iconRect.offsetMax =
                Vector2.zero;

            Image icon =
                iconObject.GetComponent<
                    Image
                >();

            icon.preserveAspect =
                true;

            // KHÔNG chặn click
            icon.raycastTarget =
                false;

            if (prefab != null)
            {
                SpriteRenderer sr =
                    prefab.GetComponent<
                        SpriteRenderer
                    >();

                if (sr != null)
                {
                    icon.sprite =
                        sr.sprite;
                }
            }


            // -----------------------------------------------------
            // TEXT
            // -----------------------------------------------------

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform)
                );

            textObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            RectTransform textRect =
                textObject.GetComponent<
                    RectTransform
                >();

            textRect.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            textRect.anchorMax =
                new Vector2(
                    1f,
                    0.30f
                );

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            TextMeshProUGUI text =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();

            string towerName =
                data != null
                    ? data.TowerName
                    : title;

            int cost =
                data != null
                    ? data.Cost
                    : 0;

            text.text =
                $"{towerName}\n" +
                $"<color=#FFD700>{cost} G</color>";

            text.font =
                TMP_Settings.defaultFontAsset;

            text.fontSize =
                11f;

            text.fontStyle =
                FontStyles.Bold;

            text.color =
                Color.white;

            text.alignment =
                TextAlignmentOptions.Center;

            text.enableWordWrapping =
                false;

            // KHÔNG chặn click
            text.raycastTarget =
                false;


            // -----------------------------------------------------
            // CLICK
            // -----------------------------------------------------

            TowerData targetData =
                data;

            GameObject targetPrefab =
                prefab;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                delegate
                {
                    OnTowerButtonClicked(
                        targetData,
                        targetPrefab
                    );
                }
            );
        }


        // =========================================================
        // POSITION RADIAL MENU
        // =========================================================

        private void PositionRadialMenu(
            BuildSite site
        )
        {
            if (
                _radialMenu == null ||
                site == null ||
                _canvas == null
            )
            {
                return;
            }

            RectTransform canvasRect =
                _canvas.GetComponent<
                    RectTransform
                >();

            if (canvasRect == null)
                return;

            Camera eventCamera =
                _canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : _canvas.worldCamera;

            Camera worldCamera =
                Camera.main;

            if (worldCamera == null)
                return;

            Vector2 screenPosition =
                worldCamera.WorldToScreenPoint(
                    site.transform.position
                );

            Vector2 localPosition;

            if (
                !RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPosition,
                        eventCamera,
                        out localPosition
                    )
            )
            {
                return;
            }

            RectTransform menuRect =
                _radialMenu.GetComponent<
                    RectTransform
                >();

            if (menuRect == null)
                return;

            menuRect.anchoredPosition =
                localPosition;

            // Đảm bảo nằm trên cùng
            _radialMenu.transform.SetAsLastSibling();
        }


        // =========================================================
        // TOWER BUTTON CLICK
        // =========================================================

        private void OnTowerButtonClicked(
            TowerData data,
            GameObject prefab
        )
        {
            if (_selectedBuildSite == null)
            {
                return;
            }


            // -----------------------------------------------------
            // DATA FALLBACK
            // -----------------------------------------------------

            if (
                data == null &&
                prefab != null
            )
            {
                TowerController prefabController =
                    prefab.GetComponent<
                        TowerController
                    >();

                if (
                    prefabController != null &&
                    prefabController.TowerData != null
                )
                {
                    data =
                        prefabController.TowerData;
                }
            }


            // -----------------------------------------------------
            // CHECK DATA
            // -----------------------------------------------------

            if (data == null)
            {
                Debug.LogError(
                    "[TowerPlacementManager] " +
                    "TowerData is NULL!"
                );

                ShowWarningMessage(
                    "Tower data is missing!"
                );

                return;
            }


            // -----------------------------------------------------
            // CHECK PREFAB
            // -----------------------------------------------------

            if (prefab == null)
            {
                Debug.LogError(
                    "[TowerPlacementManager] " +
                    $"{data.TowerName} prefab is NULL!"
                );

                ShowWarningMessage(
                    "Tower prefab is missing!"
                );

                return;
            }


            // -----------------------------------------------------
            // CHECK SITE
            // -----------------------------------------------------

            if (
                _selectedBuildSite.IsOccupied
            )
            {
                ShowWarningMessage(
                    "Build site is occupied!"
                );

                CloseRadialMenu();

                return;
            }


            // -----------------------------------------------------
            // CHECK GAME MANAGER
            // -----------------------------------------------------

            if (
                GameManager.Instance == null
            )
            {
                Debug.LogError(
                    "[TowerPlacementManager] " +
                    "GameManager.Instance is NULL!"
                );

                return;
            }


            // -----------------------------------------------------
            // CHECK GOLD
            // -----------------------------------------------------

            if (
                GameManager.Instance.CurrentGold <
                data.Cost
            )
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }


            // -----------------------------------------------------
            // DIRECT PURCHASE
            // -----------------------------------------------------

            BuildTowerAtSite(
                _selectedBuildSite,
                data,
                prefab
            );
        }


        // =========================================================
        // GET / CREATE TOWER CONTROLLER
        // =========================================================

        private TowerController GetOrCreateTowerController(
            GameObject tower,
            TowerData data
        )
        {
            if (
                tower == null ||
                data == null
            )
            {
                return null;
            }

            TowerController controller =
                tower.GetComponent<
                    TowerController
                >();

            if (controller == null)
            {
                controller =
                    tower.AddComponent<
                        TowerController
                    >();
            }

            controller.InitializeTowerData(
                data
            );

            controller.enabled =
                true;

            return controller;
        }


        // =========================================================
        // BUILD TOWER
        // =========================================================

        private void BuildTowerAtSite(
            BuildSite site,
            TowerData data,
            GameObject prefab
        )
        {
            if (
                site == null ||
                data == null ||
                prefab == null
            )
            {
                return;
            }


            // -----------------------------------------------------
            // PAY FIRST
            // -----------------------------------------------------

            if (
                GameManager.Instance == null
            )
            {
                return;
            }

            if (
                GameManager.Instance.CurrentGold <
                data.Cost
            )
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }

            bool paid =
                GameManager.Instance.TrySpendGold(
                    data.Cost
                );

            if (!paid)
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }


            // -----------------------------------------------------
            // CREATE TOWER
            // -----------------------------------------------------

            GameObject newTower =
                Instantiate(
                    prefab,
                    site.transform.position,
                    Quaternion.identity
                );

            if (newTower == null)
            {
                // Hoàn tiền nếu instantiate thất bại
                GameManager.Instance.AddGold(
                    data.Cost
                );

                return;
            }


            // -----------------------------------------------------
            // NAME
            // -----------------------------------------------------

            newTower.name =
                $"{data.TowerName}_" +
                Guid.NewGuid()
                    .ToString()
                    .Substring(
                        0,
                        4
                    );


            // -----------------------------------------------------
            // REGISTER
            // -----------------------------------------------------

            _placedTowers.Add(
                newTower
            );

            RegisterTowerInvestment(
                newTower,
                data.Cost
            );


            // -----------------------------------------------------
            // OCCUPY
            // -----------------------------------------------------

            site.SetOccupied(
                newTower
            );


            // -----------------------------------------------------
            // CONTROLLER
            // -----------------------------------------------------

            TowerController controller =
                GetOrCreateTowerController(
                    newTower,
                    data
                );


            // -----------------------------------------------------
            // SPRITE
            // -----------------------------------------------------

            CopyTowerSpriteColor(
                newTower,
                prefab
            );


            // -----------------------------------------------------
            // CLOSE MENU IMMEDIATELY
            // -----------------------------------------------------

            BuildSite purchasedSite =
                site;

            CloseRadialMenu();


            // -----------------------------------------------------
            // INFO
            // -----------------------------------------------------

            if (controller != null)
            {
                ShowTowerInfoPanel(
                    purchasedSite,
                    controller
                );

                UIManager uiManager =
                    FindAnyObjectByType<
                        UIManager
                    >();

                if (uiManager != null)
                {
                    uiManager.ShowPurchasedTower(
                        controller
                    );
                }
            }


            Debug.Log(
                "[TowerPlacementManager] " +
                $"TOWER PURCHASED | " +
                $"Tower={data.TowerName} | " +
                $"Cost={data.Cost}"
            );
        }


        // =========================================================
        // COPY SPRITE COLOR
        // =========================================================

        private void CopyTowerSpriteColor(
            GameObject target,
            GameObject prefab
        )
        {
            if (target == null)
                return;

            SpriteRenderer sr =
                target.GetComponent<
                    SpriteRenderer
                >();

            if (sr == null)
                return;

            SpriteRenderer prefabSR =
                prefab != null
                    ? prefab.GetComponent<
                        SpriteRenderer
                    >()
                    : null;

            sr.color =
                prefabSR != null
                    ? prefabSR.color
                    : Color.white;
        }


        // =========================================================
        // INVESTMENT
        // =========================================================

        private void RegisterTowerInvestment(
            GameObject tower,
            int purchaseCost
        )
        {
            if (tower == null)
                return;

            _towerInvestments.Remove(
                tower
            );

            _towerInvestments.Add(
                tower,
                new TowerInvestmentData(
                    purchaseCost
                )
            );
        }


        private void AddUpgradeInvestment(
            GameObject tower,
            int upgradeCost
        )
        {
            if (tower == null)
                return;

            upgradeCost =
                Mathf.Max(
                    0,
                    upgradeCost
                );

            TowerInvestmentData investment;

            if (
                !_towerInvestments.TryGetValue(
                    tower,
                    out investment
                )
            )
            {
                TowerController controller =
                    tower.GetComponent<
                        TowerController
                    >();

                int purchaseCost =
                    controller != null &&
                    controller.TowerData != null
                        ? controller.TowerData.Cost
                        : 0;

                investment =
                    new TowerInvestmentData(
                        purchaseCost
                    );

                _towerInvestments.Add(
                    tower,
                    investment
                );
            }

            investment.AddUpgrade(
                upgradeCost
            );
        }


        private TowerInvestmentData GetInvestment(
            GameObject tower
        )
        {
            if (tower == null)
                return null;

            TowerInvestmentData investment;

            if (
                _towerInvestments.TryGetValue(
                    tower,
                    out investment
                )
            )
            {
                return investment;
            }

            return null;
        }


        private int GetTotalInvestedCost(
            GameObject tower
        )
        {
            TowerInvestmentData investment =
                GetInvestment(
                    tower
                );

            if (investment != null)
            {
                return investment.TotalInvested;
            }

            TowerController controller =
                tower != null
                    ? tower.GetComponent<
                        TowerController
                    >()
                    : null;

            if (
                controller == null ||
                controller.TowerData == null
            )
            {
                return 0;
            }

            return controller.TowerData.Cost;
        }


        private int GetSellRefund(
            GameObject tower
        )
        {
            int totalInvested =
                GetTotalInvestedCost(
                    tower
                );

            return Mathf.Max(
                0,
                Mathf.RoundToInt(
                    totalInvested *
                    sellRefundRate
                )
            );
        }


        // =========================================================
        // INFO PANEL
        // =========================================================

        private void EnsureExistingInfoPanel()
        {
            if (infoPanel != null)
            {
                FindInfoPanelComponents();
                return;
            }

            if (_canvas == null)
            {
                _canvas =
                    FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
                return;

            Transform gameplayHUD =
                _canvas.transform.Find(
                    "GameplayHUDPanel"
                );

            if (gameplayHUD == null)
                return;

            Transform existingInfoPanel =
                gameplayHUD.Find(
                    "InfoPanel"
                );

            if (
                existingInfoPanel == null
            )
            {
                UIManager uiManager =
                    FindAnyObjectByType<
                        UIManager
                    >();

                if (uiManager != null)
                {
                    uiManager.EnsureInfoPanel();

                    existingInfoPanel =
                        gameplayHUD.Find(
                            "InfoPanel"
                        );
                }
            }

            if (
                existingInfoPanel == null
            )
            {
                return;
            }

            infoPanel =
                existingInfoPanel.gameObject;

            FindInfoPanelComponents();

            infoPanel.SetActive(false);
        }


      private void FindInfoPanelComponents()
{
    if (infoPanel == null)
        return;

    // =========================================================
    // PANEL
    // =========================================================

    RectTransform panelRect =
        infoPanel.GetComponent<RectTransform>();

    if (panelRect != null)
    {
        panelRect.anchorMin =
            new Vector2(1f, 0.5f);

        panelRect.anchorMax =
            new Vector2(1f, 0.5f);

        panelRect.pivot =
            new Vector2(1f, 0.5f);

        panelRect.anchoredPosition =
            new Vector2(-25f, 0f);

        // Panel vừa đủ cho toàn bộ nội dung
        panelRect.sizeDelta =
            new Vector2(430f, 330f);
    }

    // =========================================================
    // BACKGROUND
    // =========================================================

    Image panelImage =
        infoPanel.GetComponent<Image>();

    if (panelImage != null)
    {
        panelImage.raycastTarget = true;
    }

    // =========================================================
    // TITLE
    // =========================================================

    Transform titleTransform =
        infoPanel.transform.Find("Title");

    if (titleTransform != null)
    {
        _towerInfoTitle =
            titleTransform.GetComponent<TextMeshProUGUI>();

        RectTransform titleRect =
            titleTransform.GetComponent<RectTransform>();

        if (titleRect != null)
        {
            titleRect.anchorMin =
                new Vector2(0f, 0.8f);

            titleRect.anchorMax =
                new Vector2(1f, 0.96f);

            titleRect.offsetMin =
                new Vector2(15f, 0f);

            titleRect.offsetMax =
                new Vector2(-15f, 0f);
        }

        if (_towerInfoTitle != null)
        {
            _towerInfoTitle.fontSize =
                20f;

            _towerInfoTitle.fontStyle =
                FontStyles.Bold;

            _towerInfoTitle.color =
                Color.white;

            _towerInfoTitle.alignment =
                TextAlignmentOptions.Center;

            _towerInfoTitle.enableWordWrapping =
                false;

            _towerInfoTitle.overflowMode =
                TextOverflowModes.Ellipsis;

            _towerInfoTitle.raycastTarget =
                false;
        }
    }

    // =========================================================
    // STATS
    // =========================================================

    Transform statsTransform =
        infoPanel.transform.Find("Stats");

    if (statsTransform != null)
    {
        _towerInfoStats =
            statsTransform.GetComponent<TextMeshProUGUI>();

        RectTransform statsRect =
            statsTransform.GetComponent<RectTransform>();

        if (statsRect != null)
        {
            statsRect.anchorMin =
                new Vector2(0.08f, 0.42f);

            statsRect.anchorMax =
                new Vector2(0.92f, 0.76f);

            statsRect.offsetMin =
                Vector2.zero;

            statsRect.offsetMax =
                Vector2.zero;
        }

        if (_towerInfoStats != null)
        {
            _towerInfoStats.fontSize =
                17f;

            _towerInfoStats.fontStyle =
                FontStyles.Bold;

            _towerInfoStats.color =
                Color.white;

            _towerInfoStats.alignment =
                TextAlignmentOptions.TopLeft;

            _towerInfoStats.enableWordWrapping =
                false;

            _towerInfoStats.overflowMode =
                TextOverflowModes.Overflow;

            _towerInfoStats.lineSpacing =
                8f;

            _towerInfoStats.raycastTarget =
                false;
        }
    }

    // =========================================================
    // UPGRADE BUTTON
    // =========================================================

    Transform upgradeTransform =
        infoPanel.transform.Find("UpgradeButton");

    if (upgradeTransform != null)
    {
        _upgradeButton =
            upgradeTransform.GetComponent<Button>();

        RectTransform upgradeRect =
            upgradeTransform.GetComponent<RectTransform>();

        if (upgradeRect != null)
        {
            upgradeRect.anchorMin =
                new Vector2(0.5f, 0.19f);

            upgradeRect.anchorMax =
                new Vector2(0.5f, 0.19f);

            upgradeRect.pivot =
                new Vector2(0.5f, 0.5f);

            upgradeRect.anchoredPosition =
                Vector2.zero;

            // Nhỏ hơn panel => không bị tràn
            upgradeRect.sizeDelta =
                new Vector2(320f, 42f);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable =
                true;

            _upgradeButton.onClick
                .RemoveListener(
                    OnUpgradeButtonClicked
                );

            _upgradeButton.onClick
                .AddListener(
                    OnUpgradeButtonClicked
                );

            _upgradeButtonText =
                _upgradeButton
                    .GetComponentInChildren<
                        TextMeshProUGUI
                    >();

            if (_upgradeButtonText != null)
            {
                _upgradeButtonText.fontSize =
                    15f;

                _upgradeButtonText.fontStyle =
                    FontStyles.Bold;

                _upgradeButtonText.alignment =
                    TextAlignmentOptions.Center;

                _upgradeButtonText.raycastTarget =
                    false;
            }
        }
    }

    // =========================================================
    // SELL BUTTON
    // =========================================================

    EnsureSellButton();
}


        // =========================================================
        // SELL BUTTON
        // =========================================================

      private void EnsureSellButton()
{
    if (infoPanel == null)
        return;

    Transform sellTransform =
        infoPanel.transform.Find(
            "SellButton"
        );

    // =========================================================
    // EXISTING BUTTON
    // =========================================================

    if (sellTransform != null)
    {
        _sellInfoButton =
            sellTransform.GetComponent<Button>();

        if (_sellInfoButton != null)
        {
            RectTransform rect =
                sellTransform.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin =
                    new Vector2(0.5f, 0.05f);

                rect.anchorMax =
                    new Vector2(0.5f, 0.05f);

                rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.anchoredPosition =
                    Vector2.zero;

                rect.sizeDelta =
                    new Vector2(320f, 42f);
            }

            _sellInfoButton.interactable =
                true;

            _sellInfoButton.onClick
                .RemoveListener(
                    OnSellInfoButtonClicked
                );

            _sellInfoButton.onClick
                .AddListener(
                    OnSellInfoButtonClicked
                );

            _sellButtonText =
                _sellInfoButton
                    .GetComponentInChildren<
                        TextMeshProUGUI
                    >();

            if (_sellButtonText != null)
            {
                _sellButtonText.fontSize =
                    15f;

                _sellButtonText.fontStyle =
                    FontStyles.Bold;

                _sellButtonText.alignment =
                    TextAlignmentOptions.Center;

                _sellButtonText.raycastTarget =
                    false;
            }

            return;
        }
    }

    // =========================================================
    // CREATE SELL BUTTON
    // =========================================================

    GameObject buttonObject =
        new GameObject(
            "SellButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

    buttonObject.transform.SetParent(
        infoPanel.transform,
        false
    );

    RectTransform buttonRect =
        buttonObject.GetComponent<RectTransform>();

    buttonRect.anchorMin =
        new Vector2(0.5f, 0.07f);

    buttonRect.anchorMax =
        new Vector2(0.5f, 0.07f);

    buttonRect.pivot =
        new Vector2(0.5f, 0.5f);

    buttonRect.anchoredPosition =
        Vector2.zero;

    buttonRect.sizeDelta =
        new Vector2(300f, 40f);

    Image image =
        buttonObject.GetComponent<Image>();

    image.color =
        sellButtonColor;

    image.raycastTarget =
        true;

    _sellInfoButton =
        buttonObject.GetComponent<Button>();

    _sellInfoButton.interactable =
        true;

    ColorBlock colors =
        _sellInfoButton.colors;

    colors.normalColor =
        sellButtonColor;

    colors.highlightedColor =
        sellButtonHighlightColor;

    colors.pressedColor =
        new Color(
            sellButtonColor.r * 0.7f,
            sellButtonColor.g * 0.7f,
            sellButtonColor.b * 0.7f,
            1f
        );

    colors.selectedColor =
        sellButtonHighlightColor;

    _sellInfoButton.colors =
        colors;

    // =========================================================
    // SELL TEXT
    // =========================================================

    GameObject textObject =
        new GameObject(
            "Text",
            typeof(RectTransform)
        );

    textObject.transform.SetParent(
        buttonObject.transform,
        false
    );

    RectTransform textRect =
        textObject.GetComponent<RectTransform>();

    textRect.anchorMin =
        Vector2.zero;

    textRect.anchorMax =
        Vector2.one;

    textRect.offsetMin =
        Vector2.zero;

    textRect.offsetMax =
        Vector2.zero;

    TextMeshProUGUI text =
        textObject.AddComponent<
            TextMeshProUGUI
        >();

    text.text =
        "SELL";

    text.font =
        TMP_Settings.defaultFontAsset;

    text.fontSize =
        15f;

    text.fontStyle =
        FontStyles.Bold;

    text.color =
        Color.white;

    text.alignment =
        TextAlignmentOptions.Center;

    text.enableWordWrapping =
        false;

    text.raycastTarget =
        false;

    _sellButtonText =
        text;

    _sellInfoButton.onClick
        .AddListener(
            OnSellInfoButtonClicked
        );
}

        // =========================================================
        // SHOW INFO
        // =========================================================

        private void ShowTowerInfoPanel(
            BuildSite site,
            TowerController controller
        )
        {
            if (
                site == null ||
                controller == null
            )
            {
                return;
            }

            EnsureExistingInfoPanel();

            if (infoPanel == null)
                return;

            CloseRadialMenu();

            _infoBuildSite =
                site;

            _infoTowerController =
                controller;

            infoPanel.transform
                .SetAsLastSibling();

            infoPanel.SetActive(true);

            UpdateTowerInfoPanel();
        }


        // =========================================================
        // UPDATE INFO
        // =========================================================

       private void UpdateTowerInfoPanel()
{
    if (
        infoPanel == null ||
        _infoTowerController == null
    )
    {
        return;
    }

    TowerController controller =
        _infoTowerController;

    if (controller == null)
    {
        CloseTowerInfoPanel();
        return;
    }

    TowerData data =
        controller.TowerData;

    if (data == null)
        return;

    // =========================================================
    // TITLE
    // =========================================================

    if (_towerInfoTitle != null)
    {
        _towerInfoTitle.text =
            $"{data.TowerName.ToUpper()} " +
            $"(LVL {controller.CurrentLevel})";
    }

    // =========================================================
    // STATS
    // =========================================================

    if (_towerInfoStats != null)
    {
        if (data.Type == TowerType.Gold)
        {
            _towerInfoStats.text =
                $"GOLD        <color=#FFD700>" +
                $"+{controller.CurrentGoldPerTick} G</color>\n" +

                $"INTERVAL    <color=#55FFFF>" +
                $"{controller.GoldInterval:0.#} SEC</color>\n" +

                $"TYPE        <color=#55FF55>" +
                $"PASSIVE INCOME</color>";
        }
        else
        {
            _towerInfoStats.text =
                $"DAMAGE      <color=#FFD700>" +
                $"{controller.CurrentDamage}</color>\n" +

                $"FIRE RATE   <color=#55FFFF>" +
                $"{data.FireRate:0.##}/s</color>\n" +

                $"RANGE       <color=#55FF55>" +
                $"{controller.CurrentRange:0.##}</color>";
        }
    }

    // =========================================================
    // UPGRADE
    // =========================================================

    bool canUpgrade =
        controller.CurrentLevel <
        controller.MaxLevel;

    int upgradeCost =
        canUpgrade
            ? Mathf.Max(
                0,
                controller.UpgradeCost
            )
            : 0;

    if (_upgradeButtonText != null)
    {
        _upgradeButtonText.text =
            canUpgrade
                ? $"UPGRADE ({upgradeCost} G)"
                : "MAX LEVEL";
    }

    bool enoughGold =
        GameManager.Instance == null ||
        GameManager.Instance.CurrentGold >=
        upgradeCost;

    if (_upgradeButton != null)
    {
        _upgradeButton.interactable =
            canUpgrade &&
            enoughGold;
    }

    // =========================================================
    // SELL
    // =========================================================

    int refund =
        GetSellRefund(
            controller.gameObject
        );

    if (_sellButtonText != null)
    {
        _sellButtonText.text =
            controller.IsPreBuilt
                ? "CANNOT SELL"
                : $"SELL ({refund} G)";
    }

    if (_sellInfoButton != null)
    {
        _sellInfoButton.interactable =
            !controller.IsPreBuilt;
    }
}

        // =========================================================
        // UPGRADE
        // =========================================================

        private void OnUpgradeButtonClicked()
        {
            if (
                _infoBuildSite == null ||
                _infoTowerController == null
            )
            {
                return;
            }

            TowerController controller =
                _infoTowerController;

            if (
                controller.CurrentLevel >=
                controller.MaxLevel
            )
            {
                ShowWarningMessage(
                    "Tower is already max level."
                );

                return;
            }

            int upgradeCost =
                Mathf.Max(
                    0,
                    controller.UpgradeCost
                );

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                upgradeCost
            )
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }

            bool paid =
                GameManager.Instance == null ||
                GameManager.Instance.TrySpendGold(
                    upgradeCost
                );

            if (!paid)
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }

            controller.LevelUp();

            AddUpgradeInvestment(
                controller.gameObject,
                upgradeCost
            );

            UpdateTowerInfoPanel();
        }


        // =========================================================
        // SELL
        // =========================================================

        private void OnSellInfoButtonClicked()
        {
            if (_infoBuildSite == null)
            {
                return;
            }

            SellTower(
                _infoBuildSite
            );
        }


        private void SellTower(
            BuildSite site
        )
        {
            if (site == null)
                return;

            if (
                !site.IsOccupied ||
                site.OccupyingTower == null
            )
            {
                ShowWarningMessage(
                    "There is no tower to sell."
                );

                CloseTowerInfoPanel();

                return;
            }

            GameObject towerObject =
                site.OccupyingTower;

            TowerController controller =
                towerObject.GetComponent<
                    TowerController
                >();

            if (controller == null)
                return;

            if (controller.IsPreBuilt)
            {
                ShowWarningMessage(
                    "This tower cannot be sold."
                );

                return;
            }

            int refund =
                GetSellRefund(
                    towerObject
                );

            string towerName =
                controller.TowerData != null
                    ? controller.TowerData.TowerName
                    : towerObject.name;

            site.ClearOccupied();

            _towerInvestments.Remove(
                towerObject
            );

            _placedTowers.Remove(
                towerObject
            );

            CloseTowerInfoPanel();

            Destroy(
                towerObject
            );

            if (
                refund > 0 &&
                GameManager.Instance != null
            )
            {
                GameManager.Instance.AddGold(
                    refund
                );
            }

            Debug.Log(
                "[SELL] SOLD | " +
                $"Tower={towerName} | " +
                $"Refund={refund}"
            );
        }


        // =========================================================
        // CLOSE INFO
        // =========================================================

        public void CloseTowerInfoPanel()
        {
            _infoBuildSite =
                null;

            _infoTowerController =
                null;

            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }
        }


        // =========================================================
        // CLOSE RADIAL
        // =========================================================

        public void CloseRadialMenu()
        {
            _selectedBuildSite =
                null;

            if (_radialMenu != null)
            {
                _radialMenu.SetActive(false);
            }
        }


        // =========================================================
        // CLOSE ALL
        // =========================================================

        private void CloseAllSiteUI()
        {
            CloseRadialMenu();
            CloseTowerInfoPanel();
        }


        // =========================================================
        // LEVEL START
        // =========================================================

        private void OnLevelStarted(
            LevelStartedEvent evt
        )
        {
            CloseAllSiteUI();

            ClearPlacedTowers();

            ClearBuildSites();

            _towerInvestments.Clear();

            _cachedPath =
                FindAnyObjectByType<WaypointPath>();

            _canvas =
                FindAnyObjectByType<Canvas>();

            EnsureUIInputSystem();

            HideOldShopPanel();

            EnsureExistingInfoPanel();

            CloseTowerInfoPanel();

            CloseRadialMenu();
        }


        // =========================================================
        // HIDE OLD SHOP
        // =========================================================

        private void HideOldShopPanel()
        {
            Canvas canvas =
                FindAnyObjectByType<Canvas>();

            if (canvas == null)
                return;

            Transform gameplayHUD =
                canvas.transform.Find(
                    "GameplayHUDPanel"
                );

            if (gameplayHUD == null)
                return;

            Transform shopPanel =
                gameplayHUD.transform.Find(
                    "ShopPanel"
                );

            if (shopPanel == null)
                return;

            shopPanel.gameObject.SetActive(false);
        }


        // =========================================================
        // CLEAR TOWERS
        // =========================================================

        private void ClearPlacedTowers()
        {
            foreach (
                GameObject tower
                in _placedTowers
            )
            {
                if (tower != null)
                {
                    Destroy(tower);
                }
            }

            _placedTowers.Clear();

            TowerController[] activeTowers =
                FindObjectsByType<TowerController>(
                    FindObjectsSortMode.None
                );

            foreach (
                TowerController tower
                in activeTowers
            )
            {
                if (tower == null)
                    continue;

                if (tower.IsPreBuilt)
                {
                    tower.ResetTowerState();
                }
                else
                {
                    Destroy(
                        tower.gameObject
                    );
                }
            }

            _towerInvestments.Clear();
        }


        // =========================================================
        // CLEAR BUILD SITES
        // =========================================================

        private void ClearBuildSites()
        {
            BuildSite[] sites =
                FindObjectsByType<BuildSite>(
                    FindObjectsSortMode.None
                );

            TowerController[] activeTowers =
                FindObjectsByType<TowerController>(
                    FindObjectsSortMode.None
                );

            foreach (
                BuildSite site
                in sites
            )
            {
                if (site == null)
                    continue;

                bool isOccupiedByPreBuilt =
                    false;

                if (
                    site.IsOccupied &&
                    site.OccupyingTower != null
                )
                {
                    TowerController controller =
                        site.OccupyingTower
                            .GetComponent<
                                TowerController
                            >();

                    if (
                        controller != null &&
                        controller.IsPreBuilt
                    )
                    {
                        isOccupiedByPreBuilt =
                            true;
                    }
                }

                if (!isOccupiedByPreBuilt)
                {
                    foreach (
                        TowerController tower
                        in activeTowers
                    )
                    {
                        if (tower == null)
                            continue;

                        if (!tower.IsPreBuilt)
                            continue;

                        float distance =
                            Vector2.Distance(
                                site.transform.position,
                                tower.transform.position
                            );

                        if (distance < 0.2f)
                        {
                            site.SetOccupied(
                                tower.gameObject
                            );

                            isOccupiedByPreBuilt =
                                true;

                            break;
                        }
                    }
                }

                if (!isOccupiedByPreBuilt)
                {
                    site.ClearOccupied();
                }
            }
        }


        // =========================================================
        // START PLACEMENT
        // =========================================================

        public void StartPlacement(
            TowerData data,
            GameObject prefab
        )
        {
            if (data == null)
            {
                ShowWarningMessage(
                    "Tower data is missing."
                );

                return;
            }

            if (prefab == null)
            {
                ShowWarningMessage(
                    "Tower prefab is missing."
                );

                return;
            }

            if (_isPlacing)
            {
                CancelPlacement();
            }

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                data.Cost
            )
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }

            _activeTowerData =
                data;

            _towerPrefab =
                prefab;

            _isPlacing =
                true;

            _previewInstance =
                Instantiate(
                    prefab
                );

            _previewInstance.name =
                "Tower_Placement_Preview";

            TowerController controller =
                _previewInstance.GetComponent<
                    TowerController
                >();

            if (controller != null)
            {
                controller.enabled =
                    false;
            }

            Collider2D collider =
                _previewInstance.GetComponent<
                    Collider2D
                >();

            if (collider != null)
            {
                collider.enabled =
                    false;
            }

            _previewRenderer =
                _previewInstance.GetComponent<
                    SpriteRenderer
                >();

            UpdatePreviewVisuals(false);
        }


        // =========================================================
        // UPDATE PLACEMENT
        // =========================================================

        public void UpdatePlacement(
            Vector3 worldPosition
        )
        {
            if (
                !_isPlacing ||
                _previewInstance == null
            )
            {
                return;
            }

            Vector3 finalPos =
                new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    0f
                );

            BuildSite closestSite =
                null;

            float minDistance =
                1.2f;

            BuildSite[] sites =
                FindObjectsByType<BuildSite>(
                    FindObjectsSortMode.None
                );

            foreach (
                BuildSite site
                in sites
            )
            {
                if (site == null)
                    continue;

                float distance =
                    Vector2.Distance(
                        finalPos,
                        site.transform.position
                    );

                if (
                    distance <
                    minDistance
                )
                {
                    minDistance =
                        distance;

                    closestSite =
                        site;
                }
            }

            bool isValid =
                false;

            if (
                closestSite != null &&
                !closestSite.IsOccupied
            )
            {
                _previewInstance.transform.position =
                    closestSite.transform.position;

                isValid =
                    GameManager.Instance == null ||
                    GameManager.Instance.CurrentGold >=
                    _activeTowerData.Cost;
            }
            else
            {
                _previewInstance.transform.position =
                    finalPos;

                isValid =
                    false;
            }

            UpdatePreviewVisuals(
                isValid
            );
        }


        // =========================================================
        // COMPLETE PLACEMENT
        // =========================================================

        public void CompletePlacement(
            Vector3 worldPosition
        )
        {
            if (!_isPlacing)
                return;

            Vector3 finalPos =
                new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    0f
                );

            BuildSite targetSite =
                null;

            float minDistance =
                1.2f;

            BuildSite[] sites =
                FindObjectsByType<BuildSite>(
                    FindObjectsSortMode.None
                );

            foreach (
                BuildSite site
                in sites
            )
            {
                if (site == null)
                    continue;

                float distance =
                    Vector2.Distance(
                        finalPos,
                        site.transform.position
                    );

                if (
                    distance <
                    minDistance
                )
                {
                    minDistance =
                        distance;

                    targetSite =
                        site;
                }
            }

            if (targetSite == null)
            {
                ShowWarningMessage(
                    "Can only build towers on sites"
                );

                Cleanup();

                return;
            }

            if (targetSite.IsOccupied)
            {
                ShowWarningMessage(
                    "Build site is occupied!"
                );

                Cleanup();

                return;
            }

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                _activeTowerData.Cost
            )
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                Cleanup();

                return;
            }

            bool paid =
                GameManager.Instance == null ||
                GameManager.Instance.TrySpendGold(
                    _activeTowerData.Cost
                );

            if (!paid)
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                Cleanup();

                return;
            }

            GameObject newTower =
                Instantiate(
                    _towerPrefab,
                    targetSite.transform.position,
                    Quaternion.identity
                );

            newTower.name =
                $"{_activeTowerData.TowerName}_" +
                Guid.NewGuid()
                    .ToString()
                    .Substring(
                        0,
                        4
                    );

            _placedTowers.Add(
                newTower
            );

            RegisterTowerInvestment(
                newTower,
                _activeTowerData.Cost
            );

            targetSite.SetOccupied(
                newTower
            );

            TowerController controller =
                GetOrCreateTowerController(
                    newTower,
                    _activeTowerData
                );

            CopyTowerSpriteColor(
                newTower,
                _towerPrefab
            );

            Cleanup();

            if (controller != null)
            {
                ShowTowerInfoPanel(
                    targetSite,
                    controller
                );

                UIManager uiManager =
                    FindAnyObjectByType<
                        UIManager
                    >();

                if (uiManager != null)
                {
                    uiManager.ShowPurchasedTower(
                        controller
                    );
                }
            }
        }


        // =========================================================
        // CANCEL
        // =========================================================

        public void CancelPlacement()
        {
            if (!_isPlacing)
                return;

            Cleanup();
        }


        // =========================================================
        // CLEANUP
        // =========================================================

        private void Cleanup()
        {
            if (_previewInstance != null)
            {
                Destroy(
                    _previewInstance
                );

                _previewInstance =
                    null;
            }

            _activeTowerData =
                null;

            _towerPrefab =
                null;

            _previewRenderer =
                null;

            _isPlacing =
                false;
        }


        // =========================================================
        // POSITION VALID
        // =========================================================

        public bool IsPositionValid(
            Vector3 position
        )
        {
            if (_activeTowerData == null)
                return false;

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                _activeTowerData.Cost
            )
            {
                return false;
            }

            Collider2D hit =
                Physics2D.OverlapPoint(
                    position
                );

            if (hit == null)
                return false;

            BuildSite site =
                hit.GetComponent<
                    BuildSite
                >();

            if (site == null)
                return false;

            return !site.IsOccupied;
        }


        // =========================================================
        // PREVIEW
        // =========================================================

        private void UpdatePreviewVisuals(
            bool isValid
        )
        {
            if (
                _previewRenderer == null ||
                _towerPrefab == null
            )
            {
                return;
            }

            SpriteRenderer prefabSR =
                _towerPrefab.GetComponent<
                    SpriteRenderer
                >();

            Color originalColor =
                prefabSR != null
                    ? prefabSR.color
                    : Color.white;

            Color tint =
                isValid
                    ? validColor
                    : invalidColor;

            _previewRenderer.color =
                new Color(
                    originalColor.r *
                    tint.r,

                    originalColor.g *
                    tint.g,

                    originalColor.b *
                    tint.b,

                    originalColor.a *
                    tint.a
                );
        }


        // =========================================================
        // WARNING
        // =========================================================

        public void ShowWarningMessage(
            string message
        )
        {
            if (_warningTextInstance == null)
            {
                Canvas canvas =
                    FindAnyObjectByType<Canvas>();

                if (canvas != null)
                {
                    Transform gameplayHUD =
                        canvas.transform.Find(
                            "GameplayHUDPanel"
                        );

                    if (gameplayHUD != null)
                    {
                        Transform existing =
                            gameplayHUD.transform.Find(
                                "PlacementWarningText"
                            );

                        if (existing != null)
                        {
                            _warningTextInstance =
                                existing.GetComponent<
                                    TextMeshProUGUI
                                >();
                        }
                        else
                        {
                            GameObject warningGO =
                                new GameObject(
                                    "PlacementWarningText",
                                    typeof(RectTransform)
                                );

                            warningGO.transform.SetParent(
                                gameplayHUD,
                                false
                            );

                            _warningTextInstance =
                                warningGO.AddComponent<
                                    TextMeshProUGUI
                                >();

                            _warningTextInstance.fontSize =
                                32f;

                            _warningTextInstance.fontStyle =
                                FontStyles.Bold;

                            _warningTextInstance.color =
                                Color.red;

                            _warningTextInstance.alignment =
                                TextAlignmentOptions.Center;

                            _warningTextInstance.font =
                                TMP_Settings.defaultFontAsset;

                            _warningTextInstance.raycastTarget =
                                false;

                            RectTransform rect =
                                warningGO.GetComponent<
                                    RectTransform
                                >();

                            rect.anchorMin =
                                new Vector2(
                                    0.5f,
                                    0.5f
                                );

                            rect.anchorMax =
                                new Vector2(
                                    0.5f,
                                    0.5f
                                );

                            rect.pivot =
                                new Vector2(
                                    0.5f,
                                    0.5f
                                );

                            rect.anchoredPosition =
                                new Vector2(
                                    0f,
                                    -150f
                                );

                            rect.sizeDelta =
                                new Vector2(
                                    800f,
                                    100f
                                );
                        }
                    }
                }
            }

            if (_warningTextInstance != null)
            {
                _warningTextInstance.text =
                    message;

                _warningTextInstance.gameObject
                    .SetActive(true);

                CancelInvoke(
                    nameof(
                        HideWarningMessage
                    )
                );

                Invoke(
                    nameof(
                        HideWarningMessage
                    ),
                    2f
                );
            }
        }


        // =========================================================
        // HIDE WARNING
        // =========================================================

        private void HideWarningMessage()
        {
            if (_warningTextInstance != null)
            {
                _warningTextInstance.gameObject
                    .SetActive(false);
            }
        }
    }
}