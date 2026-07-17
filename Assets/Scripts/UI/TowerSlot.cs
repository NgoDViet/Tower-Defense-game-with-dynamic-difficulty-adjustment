using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Tower;

namespace TowerDefense.UI
{
    /// <summary>
    /// UI controller for a single tower purchase slot in the shop.
    /// Handles drag-and-drop and click-to-place workflows and shows gold cost feedback.
    /// </summary>
    public class TowerSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private TowerData towerData;
        [SerializeField] private GameObject towerPrefab;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI towerNameText;
        [SerializeField] private TextMeshProUGUI towerCostText;
        [SerializeField] private Image towerIcon;
        [SerializeField] private Image slotImage; // background image of slot

        // Properties for editor setup bypass
        public TowerData TowerData { get => towerData; set => towerData = value; }
        public GameObject TowerPrefab { get => towerPrefab; set => towerPrefab = value; }
        public TextMeshProUGUI TowerNameText { get => towerNameText; set => towerNameText = value; }
        public TextMeshProUGUI TowerCostText { get => towerCostText; set => towerCostText = value; }
        public Image TowerIcon { get => towerIcon; set => towerIcon = value; }
        public Image SlotImage { get => slotImage; set => slotImage = value; }

        private bool _isClickedPlacementActive = false;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (towerData != null)
            {
                if (towerNameText != null) towerNameText.text = towerData.TowerName;
                if (towerCostText != null) towerCostText.text = $"{towerData.Cost} G";
                if (towerIcon != null && towerData.TowerSprite != null)
                {
                    towerIcon.sprite = towerData.TowerSprite;
                    towerIcon.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            // Grey out slot dynamically based on gold availability
            if (towerData != null)
            {
                bool hasEnoughGold = GameManager.Instance == null || GameManager.Instance.CurrentGold >= towerData.Cost;
                
                if (slotImage != null)
                {
                    slotImage.color = hasEnoughGold ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
                }
                
                if (towerCostText != null)
                {
                    towerCostText.color = hasEnoughGold ? Color.yellow : Color.red;
                }
            }

            // Click-and-place mouse updates
            if (_isClickedPlacementActive && TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.IsPlacing)
            {
                Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current != null 
                    ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() 
                    : (Vector2)Input.mousePosition;

                Vector3 worldPos = GetWorldPos(mouseScreenPos);
                TowerPlacementManager.Instance.UpdatePlacement(worldPos);

                // Left click triggers final placement (avoid placing if clicking on UI)
                bool leftClick = UnityEngine.InputSystem.Mouse.current != null 
                    ? UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame 
                    : Input.GetMouseButtonDown(0);

                if (leftClick)
                {
                    if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
                    {
                        TowerPlacementManager.Instance.CompletePlacement(worldPos);
                        _isClickedPlacementActive = false;
                    }
                }

                // Right click or Escape cancels placement
                bool rightClick = UnityEngine.InputSystem.Mouse.current != null 
                    ? UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame 
                    : Input.GetMouseButtonDown(1);
                    
                bool escPress = UnityEngine.InputSystem.Keyboard.current != null
                    ? UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                    : Input.GetKeyDown(KeyCode.Escape);

                if (rightClick || escPress)
                {
                    TowerPlacementManager.Instance.CancelPlacement();
                    _isClickedPlacementActive = false;
                }
            }
        }

        #region Drag and Drop Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (towerData == null || towerPrefab == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGold < towerData.Cost) return;

            // Start placement dragging
            if (TowerPlacementManager.Instance != null)
            {
                _isClickedPlacementActive = false; // Disable click placement if drag starts
                TowerPlacementManager.Instance.StartPlacement(towerData, towerPrefab);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.IsPlacing)
            {
                TowerPlacementManager.Instance.UpdatePlacement(GetWorldPos(eventData.position));
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.IsPlacing)
            {
                TowerPlacementManager.Instance.CompletePlacement(GetWorldPos(eventData.position));
            }
        }

        #endregion

        #region Click to Place Handler

        public void OnPointerClick(PointerEventData eventData)
        {
            // Ignore click events if a drag operation is active
            if (eventData.dragging) return;
            
            if (towerData == null || towerPrefab == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGold < towerData.Cost) return;

            if (TowerPlacementManager.Instance != null)
            {
                if (TowerPlacementManager.Instance.IsPlacing)
                {
                    TowerPlacementManager.Instance.CancelPlacement();
                    _isClickedPlacementActive = false;
                }
                else
                {
                    TowerPlacementManager.Instance.StartPlacement(towerData, towerPrefab);
                    _isClickedPlacementActive = true;
                }
            }
        }

        #endregion

        private Vector3 GetWorldPos(Vector2 screenPos)
        {
            if (Camera.main == null) return Vector3.zero;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
            worldPos.z = 0f;
            return worldPos;
        }
    }
}
