using System;
using System.Collections;
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
    public List<TilePression> tilePressions = new List<TilePression>();
    
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
        
        tilePressions = FindObjectsByType<TilePression>(FindObjectsSortMode.None).ToList();
        
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

    public IEnumerator UpdateGameFlow()
    {
        canInteract = false;

        List<IEnumerator> parallelRoutines = new List<IEnumerator>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy.DettectPlayer())
                enemy.Attack();

            if (enemy is MovingEnemy me)
            {
                parallelRoutines.Add(me.UpdatePosition());
            }

            if (enemy is PursuerEnemy pe)
            {
                if(!pe.hasSeenPlayer) pe.CheckForPlayer();
                else parallelRoutines.Add(pe.Chase());
            }
        }

        foreach (Saw saw in saws)
        {
            parallelRoutines.Add(saw.UpdatePosition());
        }

        foreach (TilePression tilePression in tilePressions)
        {
            parallelRoutines.Add(tilePression.ActivateOrDeactivate());
        }
        
        yield return StartCoroutine(WaitForAll(parallelRoutines));

        canInteract = true;
    }

    
    private IEnumerator WaitForAll(List<IEnumerator> routines)
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        foreach (var routine in routines)
        {
            coroutines.Add(StartCoroutine(routine));
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

}
