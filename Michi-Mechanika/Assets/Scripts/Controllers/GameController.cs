using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public bool canInteract = true;
    public bool isGamePaused;
    
    [Header("Canvas")]
    [SerializeField] private GameObject pauseCanvas;
    
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
        
        if(pauseCanvas != null) pauseCanvas.SetActive(false);
    }

    private void Update()
    {
        HandlePause();
    }

    private void HandlePause()
    {
        if (InputController.instance != null && InputController.instance.HasPaused)
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                SetPause();
            }
            InputController.instance.HasPaused = false;
        }
    }
    
    public void ResumeGame()
    {
        pauseCanvas.SetActive(false);
        isGamePaused = false;
        Time.timeScale = 1;
        
    }
    
    public void SetPause()
    {
        pauseCanvas.SetActive(true);
        isGamePaused = true;
        Time.timeScale = 0;
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
}
