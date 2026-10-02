using System;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public static TileController instance;
    
    private const float horizontalOffset = 3f; 
    
    float _maxDistance = 4f;        
    float _maxLateral = 0.5f;       
    float _maxVertical = 2f;    

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
            
            TryAddNeighbor(tile, Tile.Direction.Forward,  Vector3.right,   _maxDistance, _maxLateral, _maxVertical, neighbors);
            TryAddNeighbor(tile, Tile.Direction.Back,     Vector3.left,    _maxDistance, _maxLateral, _maxVertical, neighbors);
            TryAddNeighbor(tile, Tile.Direction.Left,     Vector3.forward, _maxDistance, _maxLateral, _maxVertical, neighbors);
            TryAddNeighbor(tile, Tile.Direction.Right,    Vector3.back,    _maxDistance, _maxLateral, _maxVertical, neighbors);
            
            TryAddVerticalNeighbor(tile, Vector3.up, _maxDistance, _maxLateral, neighbors);
            TryAddVerticalNeighbor(tile, Vector3.down, _maxDistance, _maxLateral, neighbors);

            tile.connectedTiles = neighbors.ToArray();
        }
    }

    private void TryAddNeighbor(
        Tile tile,
        Tile.Direction dir,
        Vector3 worldDir,
        float maxDistance,
        float maxLateral,
        float maxVertical,
        List<Tile> neighbors
    )
    {
        if (tile.blockedDirections.Contains(dir))
            return;

        Tile candidate = FindClosestTileInDirection(tile.position, worldDir, maxDistance, maxLateral, maxVertical, tile);
        if (candidate == null) return;
        
        Tile.Direction opposite = Tile.GetOppositeDirection(dir);
        if (candidate.blockedDirections.Contains(opposite))
            return;

        if (!neighbors.Contains(candidate))
            neighbors.Add(candidate);
    }
    
    private void TryAddVerticalNeighbor(
        Tile tile,
        Vector3 worldDir,              
        float maxDistance,
        float maxLateral,
        List<Tile> neighbors
    )
    {
        Tile candidate = FindClosestTileInDirection(tile.position, worldDir, maxDistance, maxLateral, float.MaxValue, tile);
        if (candidate == null) return;

        if (!neighbors.Contains(candidate))
            neighbors.Add(candidate);
    }
    
    public Tile FindClosestTileInDirection(
        Vector3 from,
        Vector3 dir,
        float maxDistance,
        float maxLateralOffset,
        float maxVerticalOffset,
        Tile ignoreTile = null
    )
    {
        dir = dir.normalized;
        Tile best = null;
        float bestForwardDist = Mathf.Infinity;

        foreach (Tile tile in allTiles)
        {
            if (tile == null || tile == ignoreTile) continue;

            Vector3 toTile = tile.position - from;
            
            float forwardDist = Vector3.Dot(toTile, dir);
            if (forwardDist <= 0 || forwardDist > maxDistance) 
                continue; 
            
            Vector3 projected = dir * forwardDist;
            Vector3 lateral = toTile - projected;

            float lateralXZ = new Vector2(lateral.x, lateral.z).magnitude;
            float vertical = Mathf.Abs(lateral.y);

            if (lateralXZ > maxLateralOffset) 
                continue; 
            if (vertical > maxVerticalOffset) 
                continue; 

            if (forwardDist < bestForwardDist)
            {
                bestForwardDist = forwardDist;
                best = tile;
            }
        }

        return best;
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