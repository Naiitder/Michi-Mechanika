using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelItemButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;

    private LevelItem item;
    private LevelBuilderController controller;

    public void Setup(LevelItem newItem, LevelBuilderController newController)
    {
        item = newItem;
        controller = newController;

        if (iconImage != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (controller != null)
        {
            controller.SelectItem(item);
        }
    }
}
