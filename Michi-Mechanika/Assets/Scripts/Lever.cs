using System;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public Tile tileWhereLeverIs;
    public bool activated = false;
    
    public GameObject[] tilesGameObjects;

    [SerializeField] private Transform pivotA;
    [SerializeField] private Transform pivotB;
    
    private void Awake()
    {
        
    }

    public void PullLever(Tile tileWherePlayerIs)
    {
        if (tileWhereLeverIs != tileWherePlayerIs) return;

        if (activated)
        {
            //Mover los gameObjects al punto A
        }
        else
        {
            //Mover los gameObjects al punto B
        }
        
        activated = !activated;
        if(TileController.instance != null) TileController.instance.ConnectTiles();
    }
}
