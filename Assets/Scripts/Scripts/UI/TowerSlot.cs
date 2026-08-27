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
    /// Quản lý một ô mua tháp trong Shop.
    /// Hỗ trợ:
    /// - Click để chọn tháp
    /// - Click để đặt tháp
    /// - Kéo thả tháp
    /// - Hiển thị tên
    /// - Hiển thị giá
    /// - Hiển thị icon
    /// - Kiểm tra vàng
    /// </summary>
    public class TowerSlot :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]

        [SerializeField]
        private TowerData towerData;

        [SerializeField]
        private GameObject towerPrefab;


        // =========================================================
        // UI
        // =========================================================

        [Header("UI Elements")]

        [SerializeField]
        private TextMeshProUGUI towerNameText;

        [SerializeField]
        private TextMeshProUGUI towerCostText;

        [SerializeField]
        private Image towerIcon;

        [SerializeField]
        private Image slotImage;


        // =========================================================
        // PUBLIC PROPERTIES
        // =========================================================
        // QUAN TRỌNG:
        // TowerPlacementManager đang sử dụng các property này.
        // Không được xóa.
        // =========================================================

        public TowerData TowerData
        {
            get => towerData;
            set => towerData = value;
        }

        public GameObject TowerPrefab
        {
            get => towerPrefab;
            set => towerPrefab = value;
        }

        public TextMeshProUGUI TowerNameText
        {
            get => towerNameText;
            set => towerNameText = value;
        }

        public TextMeshProUGUI TowerCostText
        {
            get => towerCostText;
            set => towerCostText = value;
        }

        public Image TowerIcon
        {
            get => towerIcon;
            set => towerIcon = value;
        }

        public Image SlotImage
        {
            get => slotImage;
            set => slotImage = value;
        }


        // =========================================================
        // INTERNAL
        // =========================================================

        private bool isClickPlacementActive;


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            InitializeUI();
        }


        // =========================================================
        // UI INITIALIZE
        // =========================================================

        private void InitializeUI()
        {
            if (towerData == null)
                return;


            // -----------------------------------------------------
            // NAME
            // -----------------------------------------------------

            if (towerNameText != null)
            {
                towerNameText.text =
                    towerData.TowerName;
            }


            // -----------------------------------------------------
            // COST
            // -----------------------------------------------------

            if (towerCostText != null)
            {
                towerCostText.text =
                    $"{towerData.Cost} G";
            }


            // -----------------------------------------------------
            // ICON
            // -----------------------------------------------------

            if (
                towerIcon != null &&
                towerData.TowerSprite != null
            )
            {
                towerIcon.sprite =
                    towerData.TowerSprite;

                towerIcon.gameObject.SetActive(true);

                towerIcon.preserveAspect = true;
            }
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            UpdateGoldUI();

            UpdateClickPlacement();
        }


        // =========================================================
        // GOLD UI
        // =========================================================

        private void UpdateGoldUI()
        {
            if (towerData == null)
                return;


            bool hasEnoughGold =
                GameManager.Instance == null ||
                GameManager.Instance.CurrentGold >=
                towerData.Cost;


            // -----------------------------------------------------
            // SLOT COLOR
            // -----------------------------------------------------

            if (slotImage != null)
            {
                slotImage.color =
                    hasEnoughGold
                        ? Color.white
                        : new Color(
                            0.4f,
                            0.4f,
                            0.4f,
                            1f
                        );
            }


            // -----------------------------------------------------
            // COST COLOR
            // -----------------------------------------------------

            if (towerCostText != null)
            {
                towerCostText.color =
                    hasEnoughGold
                        ? Color.yellow
                        : Color.red;
            }
        }


        // =========================================================
        // CLICK PLACEMENT
        // =========================================================

        private void UpdateClickPlacement()
        {
            if (!isClickPlacementActive)
                return;

            if (TowerPlacementManager.Instance == null)
                return;

            if (
                !TowerPlacementManager.Instance
                    .IsPlacing
            )
            {
                isClickPlacementActive = false;
                return;
            }


            // -----------------------------------------------------
            // MOUSE POSITION
            // -----------------------------------------------------

            Vector2 mouseScreenPosition;


            if (
                UnityEngine.InputSystem.Mouse.current
                != null
            )
            {
                mouseScreenPosition =
                    UnityEngine.InputSystem
                        .Mouse.current
                        .position
                        .ReadValue();
            }
            else
            {
                mouseScreenPosition =
                    Input.mousePosition;
            }


            Vector3 worldPosition =
                GetWorldPosition(
                    mouseScreenPosition
                );


            TowerPlacementManager.Instance
                .UpdatePlacement(
                    worldPosition
                );


            // -----------------------------------------------------
            // LEFT CLICK
            // -----------------------------------------------------

            bool leftClick;


            if (
                UnityEngine.InputSystem.Mouse.current
                != null
            )
            {
                leftClick =
                    UnityEngine.InputSystem
                        .Mouse.current
                        .leftButton
                        .wasPressedThisFrame;
            }
            else
            {
                leftClick =
                    Input.GetMouseButtonDown(0);
            }


            if (leftClick)
            {
                if (
                    EventSystem.current == null ||
                    !EventSystem.current
                        .IsPointerOverGameObject()
                )
                {
                    TowerPlacementManager.Instance
                        .CompletePlacement(
                            worldPosition
                        );

                    isClickPlacementActive = false;
                }
            }


            // -----------------------------------------------------
            // RIGHT CLICK
            // -----------------------------------------------------

            bool rightClick;


            if (
                UnityEngine.InputSystem.Mouse.current
                != null
            )
            {
                rightClick =
                    UnityEngine.InputSystem
                        .Mouse.current
                        .rightButton
                        .wasPressedThisFrame;
            }
            else
            {
                rightClick =
                    Input.GetMouseButtonDown(1);
            }


            // -----------------------------------------------------
            // ESCAPE
            // -----------------------------------------------------

            bool escapePressed;


            if (
                UnityEngine.InputSystem.Keyboard.current
                != null
            )
            {
                escapePressed =
                    UnityEngine.InputSystem
                        .Keyboard.current
                        .escapeKey
                        .wasPressedThisFrame;
            }
            else
            {
                escapePressed =
                    Input.GetKeyDown(
                        KeyCode.Escape
                    );
            }


            // -----------------------------------------------------
            // CANCEL
            // -----------------------------------------------------

            if (
                rightClick ||
                escapePressed
            )
            {
                TowerPlacementManager.Instance
                    .CancelPlacement();

                isClickPlacementActive = false;
            }
        }


        // =========================================================
        // DRAG START
        // =========================================================

        public void OnBeginDrag(
            PointerEventData eventData
        )
        {
            if (towerData == null)
                return;

            if (towerPrefab == null)
                return;


            // -----------------------------------------------------
            // CHECK GOLD
            // -----------------------------------------------------

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                towerData.Cost
            )
            {
                return;
            }


            // -----------------------------------------------------
            // START PLACEMENT
            // -----------------------------------------------------

            if (
                TowerPlacementManager.Instance != null
            )
            {
                isClickPlacementActive = false;

                TowerPlacementManager.Instance
                    .StartPlacement(
                        towerData,
                        towerPrefab
                    );
            }
        }


        // =========================================================
        // DRAG
        // =========================================================

        public void OnDrag(
            PointerEventData eventData
        )
        {
            if (
                TowerPlacementManager.Instance == null
            )
            {
                return;
            }


            if (
                !TowerPlacementManager.Instance
                    .IsPlacing
            )
            {
                return;
            }


            Vector3 worldPosition =
                GetWorldPosition(
                    eventData.position
                );


            TowerPlacementManager.Instance
                .UpdatePlacement(
                    worldPosition
                );
        }


        // =========================================================
        // DRAG END
        // =========================================================

        public void OnEndDrag(
            PointerEventData eventData
        )
        {
            if (
                TowerPlacementManager.Instance == null
            )
            {
                return;
            }


            if (
                TowerPlacementManager.Instance
                    .IsPlacing
            )
            {
                Vector3 worldPosition =
                    GetWorldPosition(
                        eventData.position
                    );


                TowerPlacementManager.Instance
                    .CompletePlacement(
                        worldPosition
                    );
            }
        }


        // =========================================================
        // CLICK
        // =========================================================

        public void OnPointerClick(
            PointerEventData eventData
        )
        {
            // Nếu vừa kéo thả thì bỏ qua click
            if (eventData.dragging)
                return;


            if (towerData == null)
                return;

            if (towerPrefab == null)
                return;


            // -----------------------------------------------------
            // CHECK GOLD
            // -----------------------------------------------------

            if (
                GameManager.Instance != null &&
                GameManager.Instance.CurrentGold <
                towerData.Cost
            )
            {
                return;
            }


            // -----------------------------------------------------
            // PLACEMENT MANAGER
            // -----------------------------------------------------

            if (
                TowerPlacementManager.Instance == null
            )
            {
                return;
            }


            // -----------------------------------------------------
            // CANCEL IF ALREADY PLACING
            // -----------------------------------------------------

            if (
                TowerPlacementManager.Instance
                    .IsPlacing
            )
            {
                TowerPlacementManager.Instance
                    .CancelPlacement();

                isClickPlacementActive = false;

                return;
            }


            // -----------------------------------------------------
            // START PLACEMENT
            // -----------------------------------------------------

            TowerPlacementManager.Instance
                .StartPlacement(
                    towerData,
                    towerPrefab
                );

            isClickPlacementActive = true;
        }


        // =========================================================
        // WORLD POSITION
        // =========================================================

        private Vector3 GetWorldPosition(
            Vector2 screenPosition
        )
        {
            if (Camera.main == null)
                return Vector3.zero;


            Vector3 worldPosition =
                Camera.main.ScreenToWorldPoint(
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        Camera.main.nearClipPlane
                    )
                );


            worldPosition.z = 0f;


            return worldPosition;
        }
    }
}