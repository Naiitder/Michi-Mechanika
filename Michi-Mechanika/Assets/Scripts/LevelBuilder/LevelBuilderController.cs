using UnityEngine;
using UnityEngine.InputSystem;

public class LevelBuilderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private LevelItem selectedItem;
    [SerializeField] private LayerMask placementLayerMask;

    private InputActions inputActions;
    private Camera mainCamera;

    private void Awake()
    {
        inputActions = new InputActions();
        mainCamera = Camera.main;
        grid = GetComponent<Grid>();
    }

    private void OnEnable()
    {
        inputActions.Pointer.Enable();
        inputActions.Pointer.PointerClick.performed += OnPointerClick;
    }

    private void OnDisable()
    {
        inputActions.Pointer.PointerClick.performed -= OnPointerClick;
        inputActions.Pointer.Disable();
    }

    private void OnPointerClick(InputAction.CallbackContext context)
    {
        if (selectedItem == null) return;
        
        // Check if pointer is over UI
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 pointerPos = inputActions.Pointer.PointerPosition.ReadValue<Vector2>();
        Ray ray = mainCamera.ScreenPointToRay(pointerPos);
        
        Plane gridPlane = new Plane(Vector3.up, grid.transform.position);
        
        if (gridPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            
            if (grid.TryGetGridPosition(hitPoint, out Vector3 snapPos))
            {
                PlaceItem(snapPos);
            }
        }
    }

    private void PlaceItem(Vector3 position)
    {
        if (selectedItem.prefab != null)
        {
            Instantiate(selectedItem.prefab, position, Quaternion.identity);
        }
    }

    public void SelectItem(LevelItem item)
    {
        selectedItem = item;
    }
}
