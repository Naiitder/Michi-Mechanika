using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
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
