using System;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    private const float horizontalOffset = 3f;
    Tile[] allTiles;
    
    private void Awake()
    {
        allTiles = FindObjectsOfType<Tile>();
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
                
                bool isGroundNeighbor =
                    Mathf.Approximately(diff.y, 0f) &&
                    (
                        Mathf.Approximately(Mathf.Abs(diff.x), horizontalOffset) ||
                        Mathf.Approximately(Mathf.Abs(diff.z), horizontalOffset)
                    );
                
                //Falta detectar paredes de suelo lateral a pared
                // de suelo a pared delante
                // de pared a pared
                
                if (isGroundNeighbor)
                {
                    neighbors.Add(other);
                }
                else
                {
                    Debug.Log(other.position);
                    Debug.Log(tile.position);
                    Debug.Log("----------------------");
                }
            }

            tile.connectedTiles = neighbors.ToArray();
        }
    }
}
