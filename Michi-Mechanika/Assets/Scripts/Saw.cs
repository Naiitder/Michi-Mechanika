using System.Collections;
using UnityEngine;

public class Saw : CharacterMovement
{
    public override void Initialize()
    {
        base.Initialize();
        currentTile = TileController.instance.GetClosestTile(transform.position);
        currentTile.sawOnTile = this;
    }
    public IEnumerator UpdatePosition()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 left = -transform.right;
        Vector3 back = -transform.forward;
        
        Tile bestCandidate = null;
        float bestDot = -1f;

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot && dot > 0.5f && neighbor.sawRail)
            {
                bestDot = dot;
                bestCandidate = neighbor;
            }
        }
        
        if (bestCandidate == null)
        {
            foreach (Tile neighbor in currentTile.connectedTiles)
            {
                Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
                float dotLeft = Vector3.Dot(left, dirToNeighbor);
                float dotRight = Vector3.Dot(right, dirToNeighbor);

                if ((dotLeft > 0.5f || dotRight > 0.5f) && neighbor.sawRail)
                {
                    bestCandidate = neighbor;
                    break; 
                }
            }
        }
        
        if (bestCandidate == null)
        {
            foreach (Tile neighbor in currentTile.connectedTiles)
            {
                Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
                float dot = Vector3.Dot(back, dirToNeighbor);

                if (dot > 0.5f && neighbor.sawRail)
                {
                    bestCandidate = neighbor;
                    break;
                }
            }
        }

        if (bestCandidate != null)
        { 
            StartCoroutine(MoveSmoothlyTo(bestCandidate));
        }
        
        yield return null;
    }
    
    protected override void CheckTile(Tile targetTile)
    {
        currentTile.sawOnTile = null;
        currentTile = targetTile;
        targetTile.sawOnTile = this;
        
        foreach (Enemy enemy in GameController.instance.enemies)
        {
            if(enemy.currentTile == currentTile) enemy.Die();
        }

        if (GameController.instance.playerMovement.currentTile == currentTile)
        {
            GameController.instance.playerMovement.Die();
        }
    }
}
