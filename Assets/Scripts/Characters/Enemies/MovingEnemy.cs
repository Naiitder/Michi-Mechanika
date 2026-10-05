using System.Collections;
using UnityEngine;

public class MovingEnemy : Enemy
{
    public IEnumerator UpdatePosition()
    {
        Tile bestCandidate = GetForwardTile();

        if (bestCandidate != null)
        { 
           yield return StartCoroutine(MoveSmoothlyTo(bestCandidate));
           
           bestCandidate = GetForwardTile();
           if (bestCandidate == null)
           {
               bestCandidate = GetBackTile();
               if(bestCandidate != null) yield return StartCoroutine(RotateTowardsTarget(bestCandidate.position));   
           }
        }
        else
        {
            bestCandidate = GetBackTile();
            if (bestCandidate != null)
            {
                yield return StartCoroutine(MoveSmoothlyTo(bestCandidate));
           
                bestCandidate = GetForwardTile();
                if (bestCandidate == null)
                {
                    bestCandidate = GetBackTile();
                    if(bestCandidate != null) yield return StartCoroutine(RotateTowardsTarget(bestCandidate.position));   
                }
            }
        }
        
        yield return null;
    }

    private Tile GetForwardTile()
    {
        Vector3 forward = transform.forward;
        
        float bestDot = -1f;
        
        Tile possibleTile = null;

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot && dot > 0.5f )
            {
                bestDot = dot;
                possibleTile = neighbor;
            }
        }
        
        return possibleTile;
    }

    private Tile GetBackTile()
    {
        Vector3 back = -transform.forward;
        
        Tile possibleTile = null;
        
        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(back, dirToNeighbor);
                
            if (dot > 0.5f)
            {
                possibleTile = neighbor;
                
            }
        }
        
        return possibleTile;
    } 
}
