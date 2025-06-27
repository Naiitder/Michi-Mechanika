using UnityEngine;

public class MovingEnemy : Enemy
{
    public void UpdatePosition()
    {
        Vector3 forward = transform.forward;
        Vector3 back = -transform.forward;
        
        Tile bestCandidate = null;
        float bestDot = -1f;

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot && dot > 0.5f )
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
                float dot = Vector3.Dot(back, dirToNeighbor);

                if (dot > 0.5f)
                {
                    bestCandidate = neighbor;
                    break;
                }
            }
        }

        if (bestCandidate != null)
        { 
            MoveSmoothlyTo(bestCandidate);
        }
    }
}
