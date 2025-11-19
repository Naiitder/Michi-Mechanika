using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelBuilderUI : MonoBehaviour
{
    [SerializeField] private LevelBuilderController controller;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemButtonPrefab;
    [SerializeField] private List<LevelItem> items;

    private void Start()
    {
        foreach (var item in items)
        {
            GameObject buttonObj = Instantiate(itemButtonPrefab, contentContainer);
            LevelItemButton buttonScript = buttonObj.GetComponent<LevelItemButton>();
            if (buttonScript != null)
            {
                buttonScript.Setup(item, controller);
            }
        }
    }
}
