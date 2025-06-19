using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private void Awake()
    {
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
