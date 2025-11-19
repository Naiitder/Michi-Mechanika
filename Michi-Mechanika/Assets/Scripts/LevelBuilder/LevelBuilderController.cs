using UnityEngine;
using UnityEngine.InputSystem;

public class LevelBuilderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private LevelItem selectedItem;

    private Vector3Int currentGridPosition;
    private GameObject previewObject;
    private LevelItem currentPreviewItem;

    private void Start()
    {
        if (grid == null) grid = GetComponent<Grid>();
        currentGridPosition = new Vector3Int(0, 0, 0);
        UpdatePreview();
    }

    private void Update()
    {
        HandleMovement();
        HandlePlacement();
    }

    private void HandleMovement()
    {
        Vector3Int moveDir = Vector3Int.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) moveDir.z = 1;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) moveDir.z = -1;
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) moveDir.x = -1;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) moveDir.x = 1;
        
        if (keyboard.eKey.wasPressedThisFrame) moveDir.y = 1;
        if (keyboard.qKey.wasPressedThisFrame) moveDir.y = -1;

        if (moveDir != Vector3Int.zero)
        {
            Vector3Int nextPos = currentGridPosition + moveDir;
            if (grid.IsValidGridPosition(nextPos))
            {
                currentGridPosition = nextPos;
                UpdatePreviewPosition();
            }
        }
    }

    private void HandlePlacement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (selectedItem != null && !grid.IsCellOccupied(currentGridPosition))
            {
                PlaceItem(currentGridPosition);
            }
        }
    }

    private void UpdatePreview()
    {
        if (selectedItem != currentPreviewItem)
        {
            if (previewObject != null) Destroy(previewObject);
            currentPreviewItem = selectedItem;
            
            if (selectedItem != null && selectedItem.prefab != null)
            {
                previewObject = Instantiate(selectedItem.prefab);
                var colliders = previewObject.GetComponentsInChildren<Collider>();
                foreach (var col in colliders) col.enabled = false;
                
                UpdatePreviewPosition();
            }
        }
    }

    private void UpdatePreviewPosition()
    {
        if (previewObject != null)
        {
            previewObject.transform.position = grid.GetCenterWorldPosition(currentGridPosition);
        }
    }

    private void PlaceItem(Vector3Int position)
    {
        if (selectedItem != null && selectedItem.prefab != null)
        {
            Instantiate(selectedItem.prefab, grid.GetCenterWorldPosition(position), Quaternion.identity);
            grid.SetCellOccupied(position, selectedItem);
        }
    }

    public void SelectItem(LevelItem item)
    {
        selectedItem = item;
        UpdatePreview();
    }
}
