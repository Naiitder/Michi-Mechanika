using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [HideInInspector] public Vector3 position;
    public Tile[] connectedTiles;
    [SerializeField] private Transform pivotPosition;
    [SerializeField] public enum Type { Floor, Roof }
    
    [SerializeField] public Type tileType = Type.Floor;
    
    public enum Direction { Forward, Back, Right, Left }
    [Header("Blocked Directions")]
    public List<Direction> blockedDirections = new List<Direction>();

    public void Initialize()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        position = pivotPosition.position;
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
}
