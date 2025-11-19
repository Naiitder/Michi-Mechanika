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
    private Camera mainCamera;

    private void Start()
    {
        if (grid == null) grid = GetComponent<Grid>();
        mainCamera = Camera.main;
        currentGridPosition = new Vector3Int(0, 0, 0);
        UpdatePreview();
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseInput();
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
                UpdateGridLevel();
                UpdatePreviewPosition();
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (GetGridPositionFromMouse(out Vector3Int hitGridPos))
            {
                if (hitGridPos == currentGridPosition)
                {
                    if (selectedItem != null && !grid.IsCellOccupied(currentGridPosition))
                    {
                        PlaceItem(currentGridPosition);
                    }
                }
                else
                {
                    currentGridPosition = hitGridPos;
                    UpdateGridLevel();
                    UpdatePreviewPosition();
                }
            }
        }
    }
    

    private bool GetGridPositionFromMouse(out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        float currentY = currentGridPosition.y * grid.CellSize;
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, currentY, 0) + grid.transform.position);

        if (gridPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            grid.GetXZ(hitPoint, out int x, out int z);
            
            gridPos = new Vector3Int(x, currentGridPosition.y, z);
            
            if (grid.IsValidGridPosition(gridPos))
            {
                return true;
            }
        }
        return false;
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

    private void UpdateGridLevel()
    {
        grid.UpdateGridLevel(currentGridPosition.y);
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
                Renderer[] ts = previewObject.GetComponentsInChildren<Renderer>();
    
                foreach (Renderer t in ts)
                {
                    Material mat = t.material; 
                    
                    mat.SetFloat("_Surface", 1); 
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;

                    Color baseColor = Color.blue;
                    baseColor.a = 0.75f;
                    mat.color = baseColor;
                    
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        if (mat.IsKeywordEnabled("_EMISSION"))
                        {
                            mat.SetColor("_EmissionColor", Color.blue);
                        }
                    }
                }
                
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
