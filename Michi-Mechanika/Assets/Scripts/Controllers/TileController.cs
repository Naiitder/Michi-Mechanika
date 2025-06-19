using System;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    
    public static TileController instance;
    private const float horizontalOffset = 3f;
    private const float verticalOffset = 3f;
    
    private const float horizontalRoofOffset = 1.41f;
    private const float horizontalRoofUpOffset = 1.59f;
    private const float verticalRoofUpOffset = 2.34f;
    private const float verticalRoofDownOffset = 0.71f;

    Tile[] allTiles;
    
    public void Initialize()
    {
        if (instance == null) instance = this;
        else Destroy(this);
        
        allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in allTiles) tile.Initialize();
        ConnectTiles();
    }

    public void ConnectTiles()
    {
        foreach (Tile tile in allTiles)
        {
            List<Tile> neighbors = new List<Tile>();

            foreach (Tile other in allTiles)
            {
                if (tile == other) continue;
                
                Vector3 diff = other.position - tile.position;
                
                bool isHorizontalXNeighbor =
                    Mathf.Abs(diff.y) < 0.1f && Mathf.Approximately(diff.z, 0f) &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.x), horizontalOffset) 
                    );
                bool isHorizontalZNeighbor =                     
                    Mathf.Approximately(diff.y, 0f) &&  Mathf.Approximately(diff.x, 0f) &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.z), horizontalOffset)
                        );
                
                bool isVerticalNeighbor =                     
                    Mathf.Approximately(diff.z, 0f) &&  Mathf.Approximately(diff.x, 0f) &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.y), verticalOffset)
                    );
                
                bool isVerticalXNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.z)) < 0.1f &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofDownOffset) < 0.1f
                        && Mathf.Abs(Math.Abs(diff.x) - horizontalRoofOffset) < 0.1f
                    );
                
                bool isVerticalZNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.x)) < 0.1f &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofDownOffset) < 0.1f
                        && Mathf.Abs(Math.Abs(diff.z) - horizontalRoofOffset) < 0.1f
                    );
                
                bool isVerticalXUpNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.z)) < 0.1f &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofUpOffset) < 0.1f
                        && Mathf.Abs(Math.Abs(diff.x) - horizontalRoofUpOffset) < 0.1f
                    );
                
                bool isVerticalZUpNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.x)) < 0.1f &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofUpOffset) < 0.1f
                        && Mathf.Abs(Math.Abs(diff.z) - horizontalRoofUpOffset) < 0.1f
                    );
                
                if (isHorizontalXNeighbor || isHorizontalZNeighbor || isVerticalNeighbor || isVerticalXNeighbor || isVerticalZNeighbor || 
                    isVerticalXUpNeighbor || isVerticalZUpNeighbor)
                {
                    neighbors.Add(other);
                }
            }

            tile.connectedTiles = neighbors.ToArray();
        }
    }

    public Tile GetClosestTile(Vector3 position)
    {
        Tile closest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (Tile tile in allTiles) 
        {
            float dist = Vector3.Distance(tile.position, position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = tile;
            }
        }

        return closest;
    }
}
