using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.UI;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Quản lý việc đặt tháp.
    /// UI ShopPanel được tạo sẵn trong Unity và KHÔNG bị xóa/tạo lại khi Play.
    /// Hỗ trợ Basic, Fast, Ice và Laser.
    /// </summary>
    public class TowerPlacementManager : MonoBehaviour
    {
        public static TowerPlacementManager Instance { get; private set; }

        // =========================================================
        // SETTINGS
        // =========================================================

        [Header("Settings")]
        [SerializeField]
        private Color validColor =
            new Color(0f, 1f, 0.8f, 0.5f);

        [SerializeField]
        private Color invalidColor =
            new Color(1f, 0.1f, 0.1f, 0.5f);

        [SerializeField]
        private float pathClearanceRadius = 0.8f;

        [SerializeField]
        private float towerOverlapRadius = 0.4f;

        // =========================================================
        // BASIC TOWER
        // =========================================================

        [Header("Basic Tower")]
        [SerializeField]
        private TowerData defaultTowerData;

        [SerializeField]
        private GameObject defaultTowerPrefab;

        // =========================================================
        // FAST TOWER
        // =========================================================

        [Header("Fast Tower")]
        [SerializeField]
        private TowerData fastTowerData;

        [SerializeField]
        private GameObject fastTowerPrefab;

        // =========================================================
        // ICE TOWER
        // =========================================================

        [Header("Ice Tower")]
        [SerializeField]
        private TowerData iceTowerData;

        [SerializeField]
        private GameObject iceTowerPrefab;

        // =========================================================
        // LASER TOWER
        // =========================================================

        [Header("Laser Tower")]
        [SerializeField]
        private TowerData laserTowerData;

        [SerializeField]
        private GameObject laserTowerPrefab;

        // =========================================================
        // RUNTIME
        // =========================================================

        private GameObject _previewInstance;

        private TowerData _activeTowerData;

        private GameObject _towerPrefab;

        private SpriteRenderer _previewRenderer;

        private bool _isPlacing;

        private WaypointPath _cachedPath;

        private readonly System.Collections.Generic.List<GameObject>
            _placedTowers =
            new System.Collections.Generic.List<GameObject>();

        private TextMeshProUGUI _warningTextInstance;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public bool IsPlacing =>
            _isPlacing;

        public TowerData ActiveTowerData =>
            _activeTowerData;

        // =========================================================
        // EDITOR PROPERTIES
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

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR

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

#endif
        }

        // =========================================================
        // ENABLE
        // =========================================================

        private void OnEnable()
        {
            EventBus<LevelStartedEvent>
                .Subscribe(OnLevelStarted);
        }

        // =========================================================
        // DISABLE
        // =========================================================

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>
                .Unsubscribe(OnLevelStarted);
        }

        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            _cachedPath =
                FindFirstObjectByType<WaypointPath>();

            EnsureShopUI();
        }

        // =========================================================
        // LEVEL START
        // =========================================================

        private void OnLevelStarted(
            LevelStartedEvent evt)
        {
            ClearPlacedTowers();

            ClearBuildSites();

            _cachedPath =
                FindFirstObjectByType<WaypointPath>();

            EnsureShopUI();
        }

        // =========================================================
        // SHOP UI
        // =========================================================

        /// <summary>
        /// Tìm ShopPanel đã được tạo trong Unity.
        /// Không xóa và không tạo ShopPanel mới.
        /// </summary>
        private void EnsureShopUI()
        {
            Canvas canvas =
                FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning(
                    "[TowerPlacementManager] Canvas not found."
                );

                return;
            }

            Transform gameplayHUD =
                canvas.transform.Find(
                    "GameplayHUDPanel"
                );

            if (gameplayHUD == null)
            {
                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "GameplayHUDPanel not found."
                );

                return;
            }

            Transform shopPanel =
                gameplayHUD.Find("ShopPanel");

            if (shopPanel == null)
            {
                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "ShopPanel not found. " +
                    "Please create it in the Unity Scene."
                );

                return;
            }

            // =====================================================
            // QUAN TRỌNG
            // =====================================================
            // KHÔNG Destroy ShopPanel
            // KHÔNG CreateRuntimePanel
            // KHÔNG CreateTowerShopSlot
            //
            // Sử dụng chính UI đã tạo trong Unity.

            shopPanel.gameObject.SetActive(true);

            Debug.Log(
                "[TowerPlacementManager] " +
                "Using existing ShopPanel from Scene."
            );
        }

        // =========================================================
        // CLEAR PLACED TOWERS
        // =========================================================

        private void ClearPlacedTowers()
        {
            if (_placedTowers != null)
            {
                foreach (GameObject tower
                         in _placedTowers)
                {
                    if (tower != null)
                    {
                        Destroy(tower);
                    }
                }

                _placedTowers.Clear();
            }

            TowerController[] activeTowers =
                FindObjectsByType<TowerController>(
                    FindObjectsSortMode.None
                );

            foreach (TowerController tower
                     in activeTowers)
            {
                if (tower == null)
                    continue;

                if (tower.IsPreBuilt)
                {
                    tower.ResetTowerState();
                }
                else
                {
                    Destroy(tower.gameObject);
                }
            }
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

            foreach (BuildSite site in sites)
            {
                if (site == null)
                    continue;

                bool isOccupiedByPreBuilt =
                    false;

                // -------------------------------------------------
                // Check existing occupant
                // -------------------------------------------------

                if (site.IsOccupied &&
                    site.OccupyingTower != null)
                {
                    TowerController controller =
                        site.OccupyingTower
                        .GetComponent<TowerController>();

                    if (controller != null &&
                        controller.IsPreBuilt)
                    {
                        isOccupiedByPreBuilt = true;
                    }
                }

                // -------------------------------------------------
                // Check nearby pre-built tower
                // -------------------------------------------------

                if (!isOccupiedByPreBuilt)
                {
                    foreach (TowerController tower
                             in activeTowers)
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

                // -------------------------------------------------
                // Clear normal build site
                // -------------------------------------------------

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
            GameObject prefab)
        {
            if (data == null)
            {
                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "TowerData is missing."
                );

                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "TowerPrefab is missing."
                );

                return;
            }

            if (_isPlacing)
            {
                CancelPlacement();
            }

            // -----------------------------------------------------
            // Check gold
            // -----------------------------------------------------

            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                data.Cost)
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                return;
            }

            _activeTowerData = data;

            _towerPrefab = prefab;

            _isPlacing = true;

            if (_cachedPath == null)
            {
                _cachedPath =
                    FindFirstObjectByType<WaypointPath>();
            }

            // -----------------------------------------------------
            // Create preview
            // -----------------------------------------------------

            _previewInstance =
                Instantiate(prefab);

            _previewInstance.name =
                "Tower_Placement_Preview";

            TowerController controller =
                _previewInstance
                .GetComponent<TowerController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            Collider2D collider =
                _previewInstance
                .GetComponent<Collider2D>();

            if (collider != null)
            {
                collider.enabled = false;
            }

            _previewRenderer =
                _previewInstance
                .GetComponent<SpriteRenderer>();

            UpdatePreviewVisuals(false);
        }

        // =========================================================
        // UPDATE PLACEMENT
        // =========================================================

        public void UpdatePlacement(
            Vector3 worldPosition)
        {
            if (!_isPlacing ||
                _previewInstance == null)
            {
                return;
            }

            Vector3 finalPos =
                new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    0f
                );

            BuildSite closestSite = null;

            float minDistance = 1.2f;

            BuildSite[] sites =
                FindObjectsByType<BuildSite>(
                    FindObjectsSortMode.None
                );

            foreach (BuildSite site in sites)
            {
                if (site == null)
                    continue;

                float distance =
                    Vector2.Distance(
                        finalPos,
                        site.transform.position
                    );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSite = site;
                }
            }

            bool isValid = false;

            if (closestSite != null &&
                !closestSite.IsOccupied)
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

                isValid = false;
            }

            UpdatePreviewVisuals(isValid);
        }

        // =========================================================
        // COMPLETE PLACEMENT
        // =========================================================

        public void CompletePlacement(
            Vector3 worldPosition)
        {
            if (!_isPlacing)
                return;

            Vector3 finalPos =
                new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    0f
                );

            BuildSite targetSite = null;

            float minDistance = 1.2f;

            BuildSite[] sites =
                FindObjectsByType<BuildSite>(
                    FindObjectsSortMode.None
                );

            foreach (BuildSite site in sites)
            {
                if (site == null)
                    continue;

                float distance =
                    Vector2.Distance(
                        finalPos,
                        site.transform.position
                    );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetSite = site;
                }
            }

            // =====================================================
            // NO BUILD SITE
            // =====================================================

            if (targetSite == null)
            {
                ShowWarningMessage(
                    "Can only build towers on sites"
                );

                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "Can only build towers on sites."
                );

                Cleanup();

                return;
            }

            // =====================================================
            // SITE OCCUPIED
            // =====================================================

            if (targetSite.IsOccupied)
            {
                ShowWarningMessage(
                    "Build site is occupied!"
                );

                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "Site is already occupied."
                );

                Cleanup();

                return;
            }

            // =====================================================
            // CHECK GOLD
            // =====================================================

            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                _activeTowerData.Cost)
            {
                ShowWarningMessage(
                    "Insufficient gold!"
                );

                Debug.LogWarning(
                    "[TowerPlacementManager] " +
                    "Insufficient gold."
                );

                Cleanup();

                return;
            }

            // =====================================================
            // SPEND GOLD
            // =====================================================

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

            // =====================================================
            // CREATE TOWER
            // =====================================================

            Vector3 snapPosition =
                targetSite.transform.position;

            GameObject newTower =
                Instantiate(
                    _towerPrefab,
                    snapPosition,
                    Quaternion.identity
                );

            newTower.name =
                $"{_activeTowerData.TowerName}_" +
                System.Guid.NewGuid()
                .ToString()
                .Substring(0, 4);

            _placedTowers.Add(newTower);

            // =====================================================
            // OCCUPY SITE
            // =====================================================

            targetSite.SetOccupied(
                newTower
            );

            // =====================================================
            // ENABLE TOWER CONTROLLER
            // =====================================================

            TowerController controller =
                newTower.GetComponent<TowerController>();

            if (controller != null)
            {
                controller.enabled = true;
            }

            // =====================================================
            // RESTORE SPRITE COLOR
            // =====================================================

            SpriteRenderer sr =
                newTower.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                SpriteRenderer prefabSR =
                    _towerPrefab
                    .GetComponent<SpriteRenderer>();

                sr.color =
                    prefabSR != null
                        ? prefabSR.color
                        : Color.white;
            }

            Debug.Log(
                $"[TowerPlacementManager] " +
                $"Placed {_activeTowerData.TowerName} " +
                $"for {_activeTowerData.Cost} gold."
            );

            Cleanup();
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

                _previewInstance = null;
            }

            _activeTowerData = null;

            _towerPrefab = null;

            _previewRenderer = null;

            _isPlacing = false;
        }

        // =========================================================
        // POSITION VALIDATION
        // =========================================================

        public bool IsPositionValid(
            Vector3 position)
        {
            if (_activeTowerData == null)
                return false;

            // -----------------------------------------------------
            // Gold
            // -----------------------------------------------------

            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                _activeTowerData.Cost)
            {
                return false;
            }

            // -----------------------------------------------------
            // BuildSite
            // -----------------------------------------------------

            Collider2D hit =
                Physics2D.OverlapPoint(
                    position
                );

            if (hit == null)
                return false;

            BuildSite site =
                hit.GetComponent<BuildSite>();

            if (site == null)
                return false;

            if (site.IsOccupied)
                return false;

            return true;
        }

        // =========================================================
        // PREVIEW VISUAL
        // =========================================================

        private void UpdatePreviewVisuals(
            bool isValid)
        {
            if (_previewRenderer == null ||
                _towerPrefab == null)
            {
                return;
            }

            SpriteRenderer prefabSR =
                _towerPrefab
                .GetComponent<SpriteRenderer>();

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
                    originalColor.r * tint.r,
                    originalColor.g * tint.g,
                    originalColor.b * tint.b,
                    originalColor.a * tint.a
                );
        }

        // =========================================================
        // WARNING MESSAGE
        // =========================================================

        public void ShowWarningMessage(
            string message)
        {
            if (_warningTextInstance == null)
            {
                Canvas canvas =
                    FindFirstObjectByType<Canvas>();

                if (canvas != null)
                {
                    Transform gameplayHUD =
                        canvas.transform.Find(
                            "GameplayHUDPanel"
                        );

                    if (gameplayHUD != null)
                    {
                        Transform existing =
                            gameplayHUD.Find(
                                "PlacementWarningText"
                            );

                        if (existing != null)
                        {
                            _warningTextInstance =
                                existing.GetComponent<
                                    TextMeshProUGUI>();
                        }
                        else
                        {
                            GameObject warningGO =
                                new GameObject(
                                    "PlacementWarningText",
                                    typeof(RectTransform)
                                );

                            warningGO.transform
                                .SetParent(
                                    gameplayHUD,
                                    false
                                );

                            _warningTextInstance =
                                warningGO.AddComponent<
                                    TextMeshProUGUI>();

                            _warningTextInstance.fontSize =
                                32;

                            _warningTextInstance.color =
                                Color.red;

                            _warningTextInstance.alignment =
                                TextAlignmentOptions.Center;

                            _warningTextInstance.font =
                                TMP_Settings.defaultFontAsset;

                            RectTransform rect =
                                warningGO.GetComponent<
                                    RectTransform>();

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
                    nameof(HideWarningMessage)
                );

                Invoke(
                    nameof(HideWarningMessage),
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

        // =========================================================
        // DISTANCE TO SEGMENT
        // =========================================================

        private float DistanceToSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            Vector2 p = point;

            Vector2 s = start;

            Vector2 e = end;

            Vector2 segment =
                e - s;

            float lengthSq =
                segment.sqrMagnitude;

            if (lengthSq < 0.0001f)
            {
                return Vector2.Distance(
                    p,
                    s
                );
            }

            float t =
                Mathf.Clamp01(
                    Vector2.Dot(
                        p - s,
                        segment
                    ) / lengthSq
                );

            Vector2 projection =
                s + t * segment;

            return Vector2.Distance(
                p,
                projection
            );
        }

        // =========================================================
        // EDITOR
        // =========================================================

#if UNITY_EDITOR

        private void OnValidate()
        {
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

#endif
    }
}