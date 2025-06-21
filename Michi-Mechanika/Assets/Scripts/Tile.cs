using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [HideInInspector] public Vector3 position;
    public Tile[] connectedTiles;
    [SerializeField] private Transform pivotPosition;
    [SerializeField] public enum Type { Floor, Roof }
    
    [SerializeField] public Type tileType = Type.Floor;
    

    public void Initialize()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        position = pivotPosition.position;
    }
}
