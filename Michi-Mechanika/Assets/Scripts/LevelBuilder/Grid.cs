using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool showDebug = true;
    [SerializeField] private Material gridMaterial; // Assign a material for the runtime grid

    public float CellSize => cellSize;

    private void Start()
    {
        GenerateGridMesh();
    }

    private void GenerateGridMesh()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        
        if (gridMaterial != null) mr.material = gridMaterial;
        else mr.material = new Material(Shader.Find("Sprites/Default")); // Fallback

        Mesh mesh = new Mesh();
        
        int lineCount = (width + 1) + (height + 1);
        Vector3[] vertices = new Vector3[lineCount * 2];
        int[] indices = new int[lineCount * 2];

        int vIndex = 0;
        int iIndex = 0;

        // Vertical lines
        for (int x = 0; x <= width; x++)
        {
            vertices[vIndex] = new Vector3(x * cellSize, 0, 0);
            vertices[vIndex + 1] = new Vector3(x * cellSize, 0, height * cellSize);
            
            indices[iIndex] = vIndex;
            indices[iIndex + 1] = vIndex + 1;

            vIndex += 2;
            iIndex += 2;
        }

        // Horizontal lines
        for (int z = 0; z <= height; z++)
        {
            vertices[vIndex] = new Vector3(0, 0, z * cellSize);
            vertices[vIndex + 1] = new Vector3(width * cellSize, 0, z * cellSize);

            indices[iIndex] = vIndex;
            indices[iIndex + 1] = vIndex + 1;

            vIndex += 2;
            iIndex += 2;
        }

        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mf.mesh = mesh;
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        Gizmos.color = Color.white;
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(GetWorldPosition(x, 0), GetWorldPosition(x, height));
        }
        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(GetWorldPosition(0, z), GetWorldPosition(width, z));
        }
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + transform.position;
    }

    public Vector3 GetCenterWorldPosition(int x, int z)
    {
        return GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * 0.5f;
    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - transform.position).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - transform.position).z / cellSize);
    }

    public bool TryGetGridPosition(Vector3 worldPosition, out Vector3 snapPos)
    {
        GetXZ(worldPosition, out int x, out int z);
        
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            snapPos = GetCenterWorldPosition(x, z);
            return true;
        }
        
        snapPos = Vector3.zero;
        return false;
    }}
