using UnityEngine;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private int heightY = 5; 
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool showDebug = true;
    [SerializeField] private Material gridMaterial;

    public float CellSize => cellSize;
    public int Width => width;
    public int Height => height;
    public int HeightY => heightY;

    private Dictionary<Vector3Int, LevelItem> occupiedCells = new Dictionary<Vector3Int, LevelItem>();

    private void Start()
    {
        GenerateGridMesh();
    }

    private void GenerateGridMesh()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        
        if (gridMaterial != null) mr.material = gridMaterial;
        else mr.material = new Material(Shader.Find("Sprites/Default"));

        Mesh mesh = new Mesh();
        
        int lineCount = (width + 1) + (height + 1);
        Vector3[] vertices = new Vector3[lineCount * 2];
        int[] indices = new int[lineCount * 2];

        int vIndex = 0;
        int iIndex = 0;
        
        for (int x = 0; x <= width; x++)
        {
            vertices[vIndex] = new Vector3(x * cellSize, 0, 0);
            vertices[vIndex + 1] = new Vector3(x * cellSize, 0, height * cellSize);
            
            indices[iIndex] = vIndex;
            indices[iIndex + 1] = vIndex + 1;

            vIndex += 2;
            iIndex += 2;
        }
        
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

    public Vector3 GetWorldPosition(int x, int y, int z)
    {
        return new Vector3(x, y, z) * cellSize + transform.position;
    }

    public Vector3 GetCenterWorldPosition(Vector3Int gridPos)
    {
        return GetWorldPosition(gridPos.x, gridPos.y, gridPos.z) + new Vector3(cellSize, 0, cellSize) * 0.5f; 
    }

    public bool IsCellOccupied(Vector3Int gridPos)
    {
        return occupiedCells.ContainsKey(gridPos);
    }

    public void SetCellOccupied(Vector3Int gridPos, LevelItem item)
    {
        if (!IsCellOccupied(gridPos))
        {
            occupiedCells[gridPos] = item;
        }
    }

    public bool IsValidGridPosition(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.z >= 0 && gridPos.y >= 0 &&
               gridPos.x < width && gridPos.z < height && gridPos.y < heightY;
    }
}
