using System;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public Tile tileWhereLeverIs;
    public bool activated = false;
    
    public GameObject[] tilesGameObjects;
    
    private void Awake()
    {
        
    }

    public void PullLever(Tile tileWherePlayerIs)
    {
        if (tileWherePlayerIs != tileWherePlayerIs) return;

        if (activated)
        {

        }
        else
        {
            
        }
        
        activated = !activated;
    }
}
