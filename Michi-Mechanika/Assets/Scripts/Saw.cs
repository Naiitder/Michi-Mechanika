using System.Collections;
using UnityEngine;

public class Saw : CharacterMovement
{
    [SerializeField] private GameObject trailGO;
    public override void Initialize()
    {
        base.Initialize();
        currentTile = TileController.instance.GetClosestTile(transform.position);
        currentTile.characterOnTile = this;
        if(trailGO != null) trailGO.SetActive(false);
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
                Tile.Direction? directionOfTile;
                directionOfTile = TileController.GetCardinalDirection(currentTile.transform.position, neighbor.transform.position);
                if (directionOfTile.HasValue)
                {
                    if(!currentTile.blockedSawDirections.Contains(directionOfTile.Value))
                    {
                        Tile.Direction opposite = Tile.GetOppositeDirection(directionOfTile.Value);
                        
                        if (!neighbor.blockedSawDirections.Contains(opposite))
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
            if(trailGO != null) trailGO.SetActive(true);
            yield return StartCoroutine(MoveSmoothlyTo(bestCandidate));
            if(trailGO != null) trailGO.SetActive(false);
        }
        
        yield return null;
    }
    
    protected override void CheckTile(Tile targetTile)
    {
        currentTile.characterOnTile = null;
        currentTile = targetTile;

        foreach (Enemy enemy in GameFlow.instance.enemies)
        {
            if(enemy.currentTile == currentTile) enemy.Die();
        }

        if (GameFlow.instance.playerMovement.currentTile == currentTile)
        {
            GameFlow.instance.playerMovement.Die();
        }
        
        targetTile.characterOnTile = this;
    }
}
