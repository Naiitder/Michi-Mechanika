using UnityEngine;

public class Saw : CharacterMovement
{
    public override void Initialize()
    {
        base.Initialize();
        currentTile = TileController.instance.GetClosestTile(transform.position);
        currentTile.sawOnTile = this;
    }
    public void UpdatePosition()
    {
        Vector3 forward = transform.forward;
        
        Tile bestCandidate = null;
        float bestDot = -1f;

        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Vector3 dirToNeighbor = (neighbor.position - currentTile.position).normalized;
            float dot = Vector3.Dot(forward, dirToNeighbor);

            if (dot > bestDot && neighbor.sawRail)
            {
                bestDot = dot;
                bestCandidate = neighbor;
            }
        }

        if (bestCandidate != null)
        { 
            MoveSmoothlyTo(bestCandidate);
        }
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
