using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Enemy : CharacterMovement
{
    public PlayerMovement playerMovement;

    public override void Initialize()
    {
        base.Initialize();
        currentTile = TileController.instance.GetClosestTile(transform.position);
        currentTile.enemyOnTile = this;
    }

    public void Attack()
    {
        Vector3 forward = transform.forward;
        
        Tile bestCandidate = null;
        float bestDot = -1f;

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestCandidate = neighbor;
            }
        }

        if (bestCandidate != null)
        {
            if (playerMovement.currentTile == bestCandidate)
            {
                MoveSmoothlyTo(bestCandidate);
            }
        }
    }

    public void Die()
    {
        List<Enemy> enemiesList = GameController.instance.enemies.ToList();
        enemiesList.Remove(this);
        GameController.instance.enemies = enemiesList.ToArray();
        
        currentTile.enemyOnTile = null;
        anim.SetBool(DeadHash, true);
        Destroy(this.gameObject, 1f);
    }

    protected override void CheckTile(Tile targetTile)
    {
        currentTile = targetTile;
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(true);
        }
    }
}
