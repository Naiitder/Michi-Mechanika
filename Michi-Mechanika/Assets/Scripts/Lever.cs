using System;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public Tile tileWhereLeverIs;
    public bool activated = false;
    
    public GameObject[] tilesGameObjects;
    
    [SerializeField] private Transform pivotGameObject;
    [SerializeField] private Quaternion originalRotation;
    
    [SerializeField] private Transform pivotA;
    [SerializeField] private Transform pivotB;
    
    public void Initialize()
    {
        if(TileController.instance != null) tileWhereLeverIs = TileController.instance.GetClosestTile(transform.position);
        originalRotation = pivotGameObject.localRotation;
    }

    public void PullLever(Tile tileWherePlayerIs)
    {
        if (tileWhereLeverIs != tileWherePlayerIs) return;
        
        activated = !activated;
        if (!activated)
        {
            //Mover los gameObjects al punto A
            pivotGameObject.localRotation = originalRotation;

        }
        else
        {
            pivotGameObject.localRotation =  Quaternion.Euler(0, 0, -70);
            //Mover los gameObjects al punto B
        }
        
        if(TileController.instance != null) TileController.instance.ConnectTiles();
    }
}
