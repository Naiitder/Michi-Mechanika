using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [HideInInspector] public Vector3 position;
    public Tile[] connectedTiles;
    [SerializeField] private float positionY = 4;

    private void Awake()
    {
        position = transform.position;
        position.y = positionY;
    }
}
