using UnityEngine;

[CreateAssetMenu(fileName = "New Level Item", menuName = "Level Builder/Level Item")]
public class LevelItem : ScriptableObject
{
    public string itemName;
    public GameObject prefab;
    public Sprite icon;
}
