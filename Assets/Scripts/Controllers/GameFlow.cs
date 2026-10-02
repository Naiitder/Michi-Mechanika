using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameFlow : MonoBehaviour
{
    public static GameFlow instance;
    public bool canInteract = true;
    public bool levelEnded = false;
    
    [Header ("Lists")]
    public PlayerMovement playerMovement;
    public Enemy[] enemies;
    public List<Saw> saws = new List<Saw>();
    public List<TilePression> tilePressions = new List<TilePression>();
    
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
        
        TileController tc = FindFirstObjectByType<TileController>();
        if(tc != null) tc.Initialize();
        
        Lever[] levers = FindObjectsByType<Lever>(FindObjectsSortMode.None);
        foreach (Lever lever in levers)
            lever.Initialize();
        
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if(playerMovement != null)playerMovement.Initialize();
        
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
    }
    
    public void RunExclusiveRoutine(IEnumerator routine)
    {
        StartCoroutine(RunExclusive(routine));
    }
    
    private IEnumerator RunExclusive(IEnumerator routine)
    {
        LockInteraction();
        yield return StartCoroutine(routine);
        UnlockInteraction();
    }
    
    private IEnumerator RunPhase<T>(IEnumerable<T> actors, Func<T, IEnumerator> routineSelector)
    {
        List<IEnumerator> routines = new List<IEnumerator>();

        foreach (var actor in actors)
        {
            var r = routineSelector(actor);
            if (r != null)
                routines.Add(r);
        }

        if (routines.Count > 0)
            yield return StartCoroutine(WaitForAll(routines));
    }

    public IEnumerator UpdateGameFlow()
    {
        LockInteraction();
        
        yield return RunPhase(enemies, e =>
        {
            if (e.DettectPlayer())
                e.Attack();

            if (e is MovingEnemy me)
                return me.UpdatePosition();

            return null;
        });
        
        yield return RunPhase(enemies, e =>
        {
            if (e is PursuerEnemy pe)
            {
                if (!pe.hasSeenPlayer)
                {
                    pe.CheckForPlayer();
                    return null;
                }
                else
                {
                    return pe.Chase();
                }
            }
            return null;
        });
        
        yield return RunPhase(saws, s => s.UpdatePosition());
        
        yield return RunPhase(tilePressions, t => t.ActivateOrDeactivate());

        UnlockInteraction();
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
    public void LockInteraction()
    {
        canInteract = false;
    }

    public void UnlockInteraction()
    {
        if (!GameController.instance.isGamePaused && !levelEnded)
            canInteract = true;
    }

    
}
