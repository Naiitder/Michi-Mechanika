using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public bool canInteract = true;
    
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
        
        Cursor.visible = false;
        
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        TileController tc = FindFirstObjectByType<TileController>();
        tc.Initialize();
        Lever[] levers = FindObjectsByType<Lever>(FindObjectsSortMode.None);
        foreach (Lever lever in levers)
            lever.Initialize();
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        pm.Initialize();
    }
    
    
}
