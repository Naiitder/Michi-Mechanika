using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PursuerEnemy : Enemy
{
    public bool hasSeenPlayer = false;
    private List<Tile> tilesWherePlayerHasBeen = new List<Tile>();
    private bool startedChase = false;
    
    [SerializeField] private GameObject exclamationPrefab;
    [SerializeField] private Transform exclamationSpawnPoint;
    [SerializeField] private float exclamationDuration = 2f;

    public void Activate()
    {
        hasSeenPlayer = true;
        startedChase = false;
        tilesWherePlayerHasBeen.Clear();
        tilesWherePlayerHasBeen.Add(playerMovement.currentTile);
        
        GameObject exclamation = Instantiate(exclamationPrefab, exclamationSpawnPoint.position, Quaternion.identity);
        exclamation.transform.SetParent(exclamationSpawnPoint); 
        Destroy(exclamation, exclamationDuration);
    }
    
    public IEnumerator Chase()
    {
        if (!hasSeenPlayer) yield break;

        Tile nextTile;

        if (!startedChase)
        {
            nextTile = TileController.instance.GetForwardTile(currentTile, transform);
            startedChase = true;
        }
        else
        {
            nextTile = tilesWherePlayerHasBeen[0];
            tilesWherePlayerHasBeen.RemoveAt(0);
        }

        tilesWherePlayerHasBeen.Add(playerMovement.currentTile);

        if (currentTile.connectedTiles.Contains(nextTile))
        {
            
            yield return StartCoroutine(MoveSmoothlyTo(nextTile));
            yield return StartCoroutine(RotateTowardsTarget(tilesWherePlayerHasBeen[0].transform.position));
        }
        else
        {
            hasSeenPlayer = false;
        }
    }

    public void CheckForPlayer()
    {
        if (hasSeenPlayer) return;

        Tile forwardTile = TileController.instance.GetForwardTile(currentTile, transform);
        Tile detectionTile = null;
        if(forwardTile != null)
        {
            detectionTile = TileController.instance.GetForwardTile(forwardTile, transform);
        }
        
        if(playerMovement.currentTile == detectionTile) Activate();
    }
}
