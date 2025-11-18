using System;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    
    public static TileController instance;
    private const float horizontalOffset = 3f;
    private const float verticalOffset = 3f;
    
    private const float horizontalRoofOffset = 1.47f;
    private const float horizontalRoofUpOffset = 1.53f;
    private const float verticalRoofUpOffset = 1.53f;
    private const float verticalRoofDownOffset = 1.49f;
    
    private const float minimumThreshold = 0.1f;

    Tile[] allTiles;
    
    public void Initialize()
    {
        if (instance == null) instance = this;
        else Destroy(this);
        
        allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in allTiles) tile.Initialize();
        ConnectTiles();
    }

    public void DisconnectTiles()
    {
        foreach (Tile tile in allTiles)
        {
            tile.connectedTiles = Array.Empty<Tile>();
        }
        
    }

    public void ConnectTiles()
    {
        DisconnectTiles();
        foreach (Tile tile in allTiles)
        {
            List<Tile> neighbors = new List<Tile>();

            foreach (Tile other in allTiles)
            {
                if (tile == other) continue;
                
                Vector3 diff = other.position - tile.position;
                
                bool isHorizontalXNeighbor =
                    Mathf.Abs(diff.y) < minimumThreshold && Mathf.Abs(Math.Abs(diff.z)) < minimumThreshold &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.x), horizontalOffset) 
                    );
                bool isHorizontalZNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.y)) < minimumThreshold &&  Mathf.Abs(Math.Abs(diff.x)) < minimumThreshold&&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.z), horizontalOffset)
                        );
                
                bool isVerticalNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.z)) < minimumThreshold &&  Mathf.Abs(Math.Abs(diff.x)) < minimumThreshold &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.y), verticalOffset)
                    );
                
                bool isVerticalXNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.z)) < minimumThreshold &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofDownOffset) < minimumThreshold
                        && Mathf.Abs(Math.Abs(diff.x) - horizontalRoofOffset) < minimumThreshold
                    );
                
                bool isVerticalZNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.x)) < minimumThreshold &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofDownOffset) < minimumThreshold
                        && Mathf.Abs(Math.Abs(diff.z) - horizontalRoofOffset) < minimumThreshold
                    );
                
                bool isVerticalXUpNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.z)) < minimumThreshold &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofUpOffset) < minimumThreshold
                        && Mathf.Abs(Math.Abs(diff.x) - horizontalRoofUpOffset) < minimumThreshold
                    );
                
                bool isVerticalZUpNeighbor =                     
                    Mathf.Abs(Math.Abs(diff.x)) < minimumThreshold &&
                    (
                        Mathf.Abs(Math.Abs(diff.y) - verticalRoofUpOffset) < minimumThreshold
                        && Mathf.Abs(Math.Abs(diff.z) - horizontalRoofUpOffset) < minimumThreshold
                    );
                
                if (isHorizontalXNeighbor || isHorizontalZNeighbor || isVerticalNeighbor || isVerticalXNeighbor || isVerticalZNeighbor || 
                    isVerticalXUpNeighbor || isVerticalZUpNeighbor)
                {
                    Tile.Direction? dir = GetCardinalDirection(tile.position, other.position);
                    if (dir.HasValue)
                    {
                        if (tile.blockedDirections.Contains(dir.Value))
                            continue;
                        
                        Tile.Direction opposite = Tile.GetOppositeDirection(dir.Value);
                        if (other.blockedDirections.Contains(opposite))
                            continue;
                    }
                    neighbors.Add(other);
                }
            }

            tile.connectedTiles = neighbors.ToArray();
        }
    }

    public Tile GetClosestTile(Vector3 position, Tile ignoreTile = null)
    {
        Tile closest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (Tile tile in allTiles) 
        {
            if (tile == ignoreTile) continue; 
            
            float dist = Vector3.Distance(tile.position, position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = tile;
            }
        }

        return closest;
    }
    
    public static Tile.Direction? GetCardinalDirection(Vector3 from, Vector3 to)
    {
        Vector3 diff = to - from;

        if (diff.x <= horizontalOffset && diff.x > 0)
            return Tile.Direction.Forward;
        if (diff.x >= -horizontalOffset && diff.x < 0)
            return Tile.Direction.Back;
        if (diff.z <= horizontalOffset && diff.z > 0)
            return Tile.Direction.Left;
        if (diff.z >= -horizontalOffset && diff.z < 0)
            return Tile.Direction.Right;

        return null;
    }

    public Tile GetForwardTile(Tile currentTile, Transform goTransform)
    {
        Vector3 forward = goTransform.forward;
        
        float bestDot = -1f;
        Tile bestCandidate = null;
        
        float dotThreshold = 0.9f; 

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot && dot >= dotThreshold)
            {
                bestDot = dot;
                bestCandidate = neighbor;
            }
        }
        
        return bestCandidate != null  ? bestCandidate : null;
    }
}
