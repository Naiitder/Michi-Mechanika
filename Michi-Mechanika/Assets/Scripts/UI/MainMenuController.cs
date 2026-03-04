using System;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{

    public GameObject levelSelectBTN;
    private void Start()
    {
        if (SQLiteDB.instance != null)
        {
            if (SQLiteDB.instance.playerProgress.chapter != 1 && SQLiteDB.instance.playerProgress.level != 1) levelSelectBTN.SetActive(true);
            else levelSelectBTN.SetActive(false);
        }
    }
}
