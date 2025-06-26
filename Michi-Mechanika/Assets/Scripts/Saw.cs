using UnityEngine;

public class Saw : MonoBehaviour
{
    public Tile currentTile;

    public void Initialize()
    {
        currentTile = TileController.instance.GetClosestTile(transform.position);
    }

    public void UpdatePosition()
    {
        
    }
}
