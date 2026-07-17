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

            // Check if ShopPanel already exists
            Transform shopPanelTrans = gameplayHUD.Find("ShopPanel");
            if (shopPanelTrans != null)
            {
                shopPanelTrans.gameObject.SetActive(true);
                return;
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
            slotRect.anchoredPosition = new Vector2(0f, 130f);
            slotRect.sizeDelta = new Vector2(180f, 130f);

            // Add TowerSlot component
            TowerSlot towerSlot = slotGO.AddComponent<TowerSlot>();

            // Icon Image
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
            iconGO.transform.SetParent(slotGO.transform, false);
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.color = Color.cyan; // Cyan color representing Basic Tower
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

        /// <summary>
        /// Updates the position of the visual preview and evaluates placement validity.
        /// </summary>
        public void UpdatePlacement(Vector3 worldPosition)
        {
            if (!_isPlacing || _previewInstance == null) return;

            _previewInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            
            bool isValid = IsPositionValid(_previewInstance.transform.position);
            UpdatePreviewVisuals(isValid);
        }

        /// <summary>
        /// Confirms placement, instantiating the real tower and spending resources if valid.
        /// </summary>
        public void CompletePlacement(Vector3 worldPosition)
        {
            if (!_isPlacing) return;

            Vector3 finalPos = new Vector3(worldPosition.x, worldPosition.y, 0f);
            
            if (IsPositionValid(finalPos))
            {
                if (GameManager.Instance != null && GameManager.Instance.TrySpendGold(_activeTowerData.Cost))
                {
                    GameObject newTower = Instantiate(_towerPrefab, finalPos, Quaternion.identity);
                    newTower.name = $"{_activeTowerData.TowerName}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
                    _placedTowers.Add(newTower);

                    TowerController controller = newTower.GetComponent<TowerController>();
                    if (controller != null)
                    {
                        controller.enabled = true;
                    }
                    SpriteRenderer sr = newTower.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = Color.white;
                    }

                    Debug.Log($"[TowerPlacementManager] Placed {_activeTowerData.TowerName} at {finalPos}.");
                }
            }
            else
            {
                Debug.LogWarning("[TowerPlacementManager] Cannot place tower: Invalid position or insufficient gold.");
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
        /// Performs checks for Gold, Waypoint Path distance, and Collider Overlaps.
        /// </summary>
        public bool IsPositionValid(Vector3 position)
        {
            if (_activeTowerData == null) return false;

            if (GameManager.Instance != null && GameManager.Instance.CurrentGold < _activeTowerData.Cost)
            {
                return false;
            }

            if (_cachedPath != null)
            {
                int count = _cachedPath.WaypointCount;
                for (int i = 0; i < count - 1; i++)
                {
                    Transform wpStart = _cachedPath.GetWaypoint(i);
                    Transform wpEnd = _cachedPath.GetWaypoint(i + 1);
                    if (wpStart != null && wpEnd != null)
                    {
                        float dist = DistanceToSegment(position, wpStart.position, wpEnd.position);
                        if (dist < pathClearanceRadius)
                        {
                            return false;
                        }
                    }
                }
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, towerOverlapRadius);
            foreach (var col in colliders)
            {
                if (col.gameObject != _previewInstance && !col.CompareTag("Enemy") && col.gameObject.name != "WaypointPath")
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdatePreviewVisuals(bool isValid)
        {
            if (_previewRenderer != null)
            {
                _previewRenderer.color = isValid ? validColor : invalidColor;
            }
        }

        private float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq < 0.0001f) return Vector3.Distance(point, start);
            
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSq);
            Vector3 projection = start + t * segment;
            return Vector3.Distance(point, projection);
        }
    }
}
