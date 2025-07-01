using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Tile : MonoBehaviour
{
    [HideInInspector] public Vector3 position;
    public Tile[] connectedTiles;
    [SerializeField] private Transform pivotPosition;
    [SerializeField] public enum Type { Floor, Roof }
    
    [SerializeField] public Type tileType = Type.Floor;

    public CharacterMovement characterOnTile;
    public bool sawRail = false;
    public bool endingTile = false;
    
    public enum Direction { Forward, Back, Right, Left }
    [Header("Blocked Directions")]
    public List<Direction> blockedDirections = new List<Direction>();

    public void Initialize()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        position = pivotPosition != null ? pivotPosition.position : transform.position;
    }
    
    public static  Direction GetOppositeDirection( Direction dir)
    {
        switch (dir)
        {
            case  Direction.Forward: return Direction.Back;
            case  Direction.Back: return Direction.Forward;
            case  Direction.Left: return Direction.Right;
            case  Direction.Right: return Direction.Left;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    public Tile GetConnectedTileInDirection(Direction dir)
    {
        return connectedTiles.FirstOrDefault(t => TileController.GetCardinalDirection(position, t.position) == dir);
    }

    public Tile GetConnectedTileAbove()
    {
        return connectedTiles.FirstOrDefault(t => t.position.y > position.y);
    }

    public Tile GetConnectedTileBelow()
    {
        return connectedTiles.FirstOrDefault(t => t.position.y < position.y);
    }
    
}
