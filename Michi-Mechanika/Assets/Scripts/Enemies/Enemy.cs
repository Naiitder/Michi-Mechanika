using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Enemy : CharacterMovement
{
    public PlayerMovement playerMovement;
    Tile bestCandidate = null;

    public override void Initialize()
    {
        base.Initialize();
        currentTile = TileController.instance.GetClosestTile(transform.position);
        currentTile.enemyOnTile = this;
    }

    public bool DettectPlayer()
    {
        bestCandidate = TileController.instance.GetForwardTile(currentTile, transform);

        if (bestCandidate != null)
        {
            if (playerMovement.currentTile == bestCandidate)
            {
               return true;
            }
        }
        return false;
    }

    public void Attack()
    {
        anim.SetBool(AttackHash,true);
        MoveSmoothlyTo(bestCandidate);
        playerMovement.Die();
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
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(false);
        }
        
        currentTile.enemyOnTile = null;
        currentTile = targetTile;
        targetTile.enemyOnTile = this;
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(true);
        }
        
        if(currentTile.sawOnTile != null) Die();
    }
}
