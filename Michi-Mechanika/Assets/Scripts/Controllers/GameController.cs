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
        
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        
        Texture2D cursorTexture = Resources.Load<Texture2D>("Steampunk_UI_icon_02");
        Vector2 hotspot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        
        TileController tc = FindFirstObjectByType<TileController>();
        tc.Initialize();
        Lever[] levers = FindObjectsByType<Lever>(FindObjectsSortMode.None);
        foreach (Lever lever in levers)
            lever.Initialize();
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        pm.Initialize();
    }
    
    
}
