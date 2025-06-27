using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public bool canInteract = true;
    public bool isGamePaused;
    public PlayerMovement playerMovement;
    
    [Header ("Lists")]
    public Enemy[] enemies;
    public List<Saw> saws = new List<Saw>();
    
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
        
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerMovement.Initialize();
        
        enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            enemy.playerMovement = playerMovement;
            enemy.Initialize();
        }
        
        saws = FindObjectsByType<Saw>(FindObjectsSortMode.None).ToList();
        foreach (Saw saw in saws)
        {
            saw.Initialize();
        }
        
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

    public void UpdateGameFlow()
    {
        foreach (Enemy enemy in enemies)
        {
            if(enemy.DettectPlayer())enemy.Attack();
            if(enemy is MovingEnemy)
            {            
                MovingEnemy me = (MovingEnemy)enemy;
                me.UpdatePosition();
            }

            if (enemy is PursuerEnemy)
            {
                PursuerEnemy pe = (PursuerEnemy)enemy;
                pe.Chase();
                pe.CheckForPlayer();
            }

        }

        foreach (Saw saw in saws)
        {
            saw.UpdatePosition();
        }
    }
}
