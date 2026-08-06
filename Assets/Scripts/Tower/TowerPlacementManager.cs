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
    /// Singleton manager that coordinates the validation and instantiation of towers placed freely in the scene.
    /// Supports drag-and-drop or click-and-place previews and renders visual feedback (green/red overlays).
    /// Persists across levels to automatically inject Shop UI and support placement in all scenes.
    /// </summary>
    public class TowerPlacementManager : MonoBehaviour
    {
        public static TowerPlacementManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0.8f, 0.5f); // semi-transparent cyan/green
        [SerializeField] private Color invalidColor = new Color(1f, 0.1f, 0.1f, 0.5f); // semi-transparent red
        [SerializeField] private float pathClearanceRadius = 0.8f;
        [SerializeField] private float towerOverlapRadius = 0.4f;

        [Header("Default Assets for Runtime UI Injection")]
        [SerializeField] private TowerData defaultTowerData;
        [SerializeField] private GameObject defaultTowerPrefab;

        [Header("Fast Tower Assets")]
        [SerializeField] private TowerData fastTowerData;
        [SerializeField] private GameObject fastTowerPrefab;

        private GameObject _previewInstance;
        private TowerData _activeTowerData;
        private GameObject _towerPrefab;
        private SpriteRenderer _previewRenderer;
        private bool _isPlacing = false;
        private WaypointPath _cachedPath;
        private System.Collections.Generic.List<GameObject> _placedTowers = new System.Collections.Generic.List<GameObject>();

        public bool IsPlacing => _isPlacing;
        public TowerData ActiveTowerData => _activeTowerData;

        // Setters for Editor Wizard
        public TowerData DefaultTowerData { get => defaultTowerData; set => defaultTowerData = value; }
        public GameObject DefaultTowerPrefab { get => defaultTowerPrefab; set => defaultTowerPrefab = value; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (defaultTowerData == null)
            {
                defaultTowerData = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
            }
            if (defaultTowerPrefab == null)
            {
                defaultTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            }
            if (fastTowerData == null)
            {
                fastTowerData = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/FastTowerData.asset");
            }
            if (fastTowerPrefab == null)
            {
                fastTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FastTower.prefab");
            }
#endif
        }

        private void OnEnable()
        {
            EventBus<LevelStartedEvent>.Subscribe(OnLevelStarted);
        }

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>.Unsubscribe(OnLevelStarted);
        }

        private void Start()
        {
            _cachedPath = FindObjectOfType<WaypointPath>();
            EnsureShopUI();
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            ClearPlacedTowers();
            ClearBuildSites();
            
            // Recache waypoint path for the new scene
            _cachedPath = FindObjectOfType<WaypointPath>();
            
            // Dynamically inject Shop UI on the new level canvas if missing
            EnsureShopUI();
        }

        private void ClearPlacedTowers()
        {
            if (_placedTowers != null)
            {
                foreach (var tower in _placedTowers)
                {
                    if (tower != null)
                    {
                        Destroy(tower);
                    }
                }
                _placedTowers.Clear();
            }

            // Fallback sweep: Find and destroy all non-prebuilt TowerController objects in the scene, or reset prebuilt ones.
            TowerController[] activeTowers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            foreach (var tower in activeTowers)
            {
                if (tower != null)
                {
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
        }

        private void ClearBuildSites()
        {
            BuildSite[] sites = FindObjectsByType<BuildSite>(FindObjectsSortMode.None);
            TowerController[] activeTowers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);

            foreach (var site in sites)
            {
                if (site != null)
                {
                    bool isOccupiedByPreBuilt = false;

                    // 1. Check if it is currently registered as occupied by a pre-built tower
                    if (site.IsOccupied && site.OccupyingTower != null)
                    {
                        TowerController occupyingTowerController = site.OccupyingTower.GetComponent<TowerController>();
                        if (occupyingTowerController != null && occupyingTowerController.IsPreBuilt)
                        {
                            isOccupiedByPreBuilt = true;
                        }
                    }

                    // 2. Perform physical proximity check to automatically pair pre-built towers with sites
                    if (!isOccupiedByPreBuilt)
                    {
                        foreach (var tower in activeTowers)
                        {
                            if (tower != null && tower.IsPreBuilt && Vector2.Distance(site.transform.position, tower.transform.position) < 0.2f)
                            {
                                site.SetOccupied(tower.gameObject);
                                isOccupiedByPreBuilt = true;
                                break;
                            }
                        }
                    }

                    // 3. Clear occupancy only if it is not occupied by a pre-built tower
                    if (!isOccupiedByPreBuilt)
                    {
                        site.ClearOccupied();
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates the Shop UI panel and slot at runtime if they don't exist in the active canvas.
        /// </summary>
        private void EnsureShopUI()
        {
            if (defaultTowerData == null || defaultTowerPrefab == null) return;

            // Find Canvas in active scene
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Find GameplayHUDPanel
            Transform gameplayHUD = canvas.transform.Find("GameplayHUDPanel");
            if (gameplayHUD == null) return;

            // Check if ShopPanel already exists and destroy it to rebuild with both towers
            Transform shopPanelTrans = gameplayHUD.Find("ShopPanel");
            if (shopPanelTrans != null)
            {
                Destroy(shopPanelTrans.gameObject);
            }

            // Create Left-Hand Side Shop Panel
            GameObject shopPanel = CreateRuntimePanel("ShopPanel", gameplayHUD, new Color(0.12f, 0.12f, 0.16f, 0.85f));
            RectTransform rect = shopPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(30f, 0f);
            rect.sizeDelta = new Vector2(220f, 600f);

            // Shop Title
            CreateRuntimeText("ShopTitle", shopPanel.transform, "TOWERS", new Vector2(0f, 250f), 24, Color.white);

            // Basic Tower Slot Container
            GameObject slotGO = new GameObject("TowerSlot_Basic", typeof(RectTransform), typeof(CanvasRenderer));
            slotGO.transform.SetParent(shopPanel.transform, false);

            Image slotImg = slotGO.AddComponent<Image>();
            slotImg.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Dark grey button background
            
            RectTransform slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.anchoredPosition = new Vector2(0f, 120f);
            slotRect.sizeDelta = new Vector2(180f, 130f);

            // Add TowerSlot component
            TowerSlot towerSlot = slotGO.AddComponent<TowerSlot>();

            // Icon Image
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
            iconGO.transform.SetParent(slotGO.transform, false);
            Image iconImg = iconGO.AddComponent<Image>();
            
            SpriteRenderer prefabSR = defaultTowerPrefab.GetComponent<SpriteRenderer>();
            if (prefabSR != null && prefabSR.sprite != null)
            {
                iconImg.sprite = prefabSR.sprite;
                iconImg.color = prefabSR.color;
            }
            else
            {
                iconImg.color = Color.cyan; // Fallback
            }

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0f, 20f);
            iconRect.sizeDelta = new Vector2(60f, 60f);

            // Name Text
            TextMeshProUGUI nameText = CreateRuntimeText("NameText", slotGO.transform, defaultTowerData.TowerName, new Vector2(0f, -25f), 18, Color.white);
            nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 30f);

            // Cost Text
            TextMeshProUGUI costText = CreateRuntimeText("CostText", slotGO.transform, $"{defaultTowerData.Cost} G", new Vector2(0f, -50f), 16, Color.yellow);
            costText.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 30f);

            // Hook up variables
            towerSlot.TowerData = defaultTowerData;
            towerSlot.TowerPrefab = defaultTowerPrefab;
            towerSlot.TowerNameText = nameText;
            towerSlot.TowerCostText = costText;
            towerSlot.TowerIcon = iconImg;
            towerSlot.SlotImage = slotImg;

            if (fastTowerData != null && fastTowerPrefab != null)
            {
                // Fast Tower Slot Container
                GameObject fastSlotGO = new GameObject("TowerSlot_Fast", typeof(RectTransform), typeof(CanvasRenderer));
                fastSlotGO.transform.SetParent(shopPanel.transform, false);

                Image fastSlotImg = fastSlotGO.AddComponent<Image>();
                fastSlotImg.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Dark grey button background

                RectTransform fastSlotRect = fastSlotGO.GetComponent<RectTransform>();
                fastSlotRect.anchoredPosition = new Vector2(0f, -30f);
                fastSlotRect.sizeDelta = new Vector2(180f, 130f);

                // Add TowerSlot component
                TowerSlot fastTowerSlot = fastSlotGO.AddComponent<TowerSlot>();

                // Icon Image
                GameObject fastIconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
                fastIconGO.transform.SetParent(fastSlotGO.transform, false);
                Image fastIconImg = fastIconGO.AddComponent<Image>();

                SpriteRenderer fastPrefabSR = fastTowerPrefab.GetComponent<SpriteRenderer>();
                if (fastPrefabSR != null && fastPrefabSR.sprite != null)
                {
                    fastIconImg.sprite = fastPrefabSR.sprite;
                    fastIconImg.color = fastPrefabSR.color;
                }
                else
                {
                    fastIconImg.color = new Color(1f, 0.6f, 0f, 1f); // Fallback
                }

                RectTransform fastIconRect = fastIconGO.GetComponent<RectTransform>();
                fastIconRect.anchoredPosition = new Vector2(0f, 20f);
                fastIconRect.sizeDelta = new Vector2(60f, 60f);

                // Name Text
                TextMeshProUGUI fastNameText = CreateRuntimeText("NameText", fastSlotGO.transform, fastTowerData.TowerName, new Vector2(0f, -25f), 18, Color.white);
                fastNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 30f);

                // Cost Text
                TextMeshProUGUI fastCostText = CreateRuntimeText("CostText", fastSlotGO.transform, $"{fastTowerData.Cost} G", new Vector2(0f, -50f), 16, Color.yellow);
                fastCostText.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 30f);

                // Hook up variables
                fastTowerSlot.TowerData = fastTowerData;
                fastTowerSlot.TowerPrefab = fastTowerPrefab;
                fastTowerSlot.TowerNameText = fastNameText;
                fastTowerSlot.TowerCostText = fastCostText;
                fastTowerSlot.TowerIcon = fastIconImg;
                fastTowerSlot.SlotImage = fastSlotImg;
            }
            else
            {
                Debug.LogWarning("[TowerPlacementManager] FastTowerData or FastTower prefab is not assigned.");
            }
        }

        private GameObject CreateRuntimePanel(string name, Transform parent, Color bgColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        private TextMeshProUGUI CreateRuntimeText(string name, Transform parent, string text, Vector2 anchoredPosition, float fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            
            TextMeshProUGUI textComp = go.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = color;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.font = TMP_Settings.defaultFontAsset;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(400f, 100f);

            return textComp;
        }

        /// <summary>
        /// Initiates the tower placement visual preview.
        /// </summary>
        public void StartPlacement(TowerData data, GameObject prefab)
        {
            if (_isPlacing)
            {
                CancelPlacement();
            }

            _activeTowerData = data;
            _towerPrefab = prefab;
            _isPlacing = true;

            if (_cachedPath == null)
            {
                _cachedPath = FindObjectOfType<WaypointPath>();
            }

            // Create preview instance
            _previewInstance = Instantiate(prefab);
            _previewInstance.name = "Tower_Placement_Preview";

            TowerController controller = _previewInstance.GetComponent<TowerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            Collider2D previewCollider = _previewInstance.GetComponent<Collider2D>();
            if (previewCollider != null)
            {
                previewCollider.enabled = false;
            }

            _previewRenderer = _previewInstance.GetComponent<SpriteRenderer>();
            UpdatePreviewVisuals(false);
        }

        private TextMeshProUGUI _warningTextInstance;

        /// <summary>
        /// Displays a warning message on the UI.
        /// </summary>
        public void ShowWarningMessage(string message)
        {
            if (_warningTextInstance == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform gameplayHUD = canvas.transform.Find("GameplayHUDPanel");
                    if (gameplayHUD != null)
                    {
                        Transform existing = gameplayHUD.Find("PlacementWarningText");
                        if (existing != null)
                        {
                            _warningTextInstance = existing.GetComponent<TextMeshProUGUI>();
                        }
                        else
                        {
                            GameObject warningGO = new GameObject("PlacementWarningText", typeof(RectTransform));
                            warningGO.transform.SetParent(gameplayHUD, false);
                            
                            _warningTextInstance = warningGO.AddComponent<TextMeshProUGUI>();
                            _warningTextInstance.fontSize = 32;
                            _warningTextInstance.color = Color.red;
                            _warningTextInstance.alignment = TextAlignmentOptions.Center;
                            _warningTextInstance.font = TMP_Settings.defaultFontAsset;

                            RectTransform rect = warningGO.GetComponent<RectTransform>();
                            rect.anchorMin = new Vector2(0.5f, 0.5f);
                            rect.anchorMax = new Vector2(0.5f, 0.5f);
                            rect.pivot = new Vector2(0.5f, 0.5f);
                            rect.anchoredPosition = new Vector2(0f, -150f);
                            rect.sizeDelta = new Vector2(800f, 100f);
                        }
                    }
                }
            }

            if (_warningTextInstance != null)
            {
                _warningTextInstance.text = message;
                _warningTextInstance.gameObject.SetActive(true);
                CancelInvoke(nameof(HideWarningMessage));
                Invoke(nameof(HideWarningMessage), 2.0f);
            }
        }

        private void HideWarningMessage()
        {
            if (_warningTextInstance != null)
            {
                _warningTextInstance.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the position of the visual preview and evaluates placement validity.
        /// Snaps the preview to the BuildSite center if hover is valid.
        /// </summary>
        public void UpdatePlacement(Vector3 worldPosition)
        {
            if (!_isPlacing || _previewInstance == null) return;

            Vector3 finalPos = new Vector3(worldPosition.x, worldPosition.y, 0f);
            
            // Find closest build site within snap radius
            BuildSite closestSite = null;
            float minDistance = 1.2f; // Snap radius threshold
            BuildSite[] sites = FindObjectsByType<BuildSite>(FindObjectsSortMode.None);
            foreach (var site in sites)
            {
                if (site == null) continue;
                float dist = Vector2.Distance(finalPos, site.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestSite = site;
                }
            }

            bool isValid = false;
            if (closestSite != null && !closestSite.IsOccupied)
            {
                _previewInstance.transform.position = closestSite.transform.position;
                isValid = (GameManager.Instance == null || GameManager.Instance.CurrentGold >= _activeTowerData.Cost);
            }
            else
            {
                _previewInstance.transform.position = finalPos;
                isValid = false;
            }

            UpdatePreviewVisuals(isValid);
        }

        /// <summary>
        /// Confirms placement, instantiating the real tower and spending resources if valid.
        /// </summary>
        public void CompletePlacement(Vector3 worldPosition)
        {
            if (!_isPlacing) return;

            Vector3 finalPos = new Vector3(worldPosition.x, worldPosition.y, 0f);
            
            // Find closest build site within snap radius
            BuildSite targetSite = null;
            float minDistance = 1.2f; // Snap radius threshold
            BuildSite[] sites = FindObjectsByType<BuildSite>(FindObjectsSortMode.None);
            foreach (var site in sites)
            {
                if (site == null) continue;
                float dist = Vector2.Distance(finalPos, site.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    targetSite = site;
                }
            }

            if (targetSite != null)
            {
                if (targetSite.IsOccupied)
                {
                    ShowWarningMessage("can only build towers on sites");
                    Debug.LogWarning("[TowerPlacementManager] Site is already occupied!");
                }
                else if (GameManager.Instance != null && GameManager.Instance.CurrentGold < _activeTowerData.Cost)
                {
                    ShowWarningMessage("Insufficient gold!");
                    Debug.LogWarning("[TowerPlacementManager] Insufficient gold.");
                }
                else
                {
                    Vector3 snapPos = targetSite.transform.position;
                    if (GameManager.Instance == null || GameManager.Instance.TrySpendGold(_activeTowerData.Cost))
                    {
                        GameObject newTower = Instantiate(_towerPrefab, snapPos, Quaternion.identity);
                        newTower.name = $"{_activeTowerData.TowerName}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
                        _placedTowers.Add(newTower);

                        targetSite.SetOccupied(newTower);

                        TowerController controller = newTower.GetComponent<TowerController>();
                        if (controller != null)
                        {
                            controller.enabled = true;
                        }
                        SpriteRenderer sr = newTower.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            SpriteRenderer prefabSR = _towerPrefab.GetComponent<SpriteRenderer>();
                            sr.color = prefabSR != null ? prefabSR.color : Color.white;
                        }

                        Debug.Log($"[TowerPlacementManager] Placed {_activeTowerData.TowerName} on site at {snapPos}.");
                    }
                }
            }
            else
            {
                ShowWarningMessage("can only build towers on sites");
                Debug.LogWarning("[TowerPlacementManager] can only build towers on sites");
            }

            Cleanup();
        }

        /// <summary>
        /// Cancels placement and removes visual preview.
        /// </summary>
        public void CancelPlacement()
        {
            if (!_isPlacing) return;
            Cleanup();
        }

        private void Cleanup()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
            _activeTowerData = null;
            _towerPrefab = null;
            _previewRenderer = null;
            _isPlacing = false;
        }

        /// <summary>
        /// Performs checks for Gold, and BuildSite occupancy.
        /// </summary>
        public bool IsPositionValid(Vector3 position)
        {
            if (_activeTowerData == null) return false;

            if (GameManager.Instance != null && GameManager.Instance.CurrentGold < _activeTowerData.Cost)
            {
                return false;
            }

            Collider2D hit = Physics2D.OverlapPoint(position);
            if (hit == null) return false;

            BuildSite site = hit.GetComponent<BuildSite>();
            if (site == null || site.IsOccupied) return false;

            return true;
        }

        private void UpdatePreviewVisuals(bool isValid)
        {
            if (_previewRenderer != null && _towerPrefab != null)
            {
                SpriteRenderer prefabSR = _towerPrefab.GetComponent<SpriteRenderer>();
                Color originalColor = prefabSR != null ? prefabSR.color : Color.white;
                Color tint = isValid ? validColor : invalidColor;
                _previewRenderer.color = new Color(
                    originalColor.r * tint.r,
                    originalColor.g * tint.g,
                    originalColor.b * tint.b,
                    originalColor.a * tint.a
                );
            }
        }

        private float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector2 p = point;
            Vector2 s = start;
            Vector2 e = end;
            Vector2 segment = e - s;
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq < 0.0001f) return Vector2.Distance(p, s);
            
            float t = Mathf.Clamp01(Vector2.Dot(p - s, segment) / lengthSq);
            Vector2 projection = s + t * segment;
            return Vector2.Distance(p, projection);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fastTowerData == null)
            {
                fastTowerData = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/FastTowerData.asset");
            }
            if (fastTowerPrefab == null)
            {
                fastTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FastTower.prefab");
            }
        }
#endif
    }
}
