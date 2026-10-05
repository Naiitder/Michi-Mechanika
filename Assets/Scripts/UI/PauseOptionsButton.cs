using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en la tarjeta "Options" del menú de pausa (Canvas.prefab): al pulsarla abre la ventana de
/// opciones, la misma del menú principal. Se engancha por código porque OptionsMenu no es un
/// objeto de la escena al que el botón pueda apuntar desde el Inspector.
/// </summary>
public class PauseOptionsButton : MonoBehaviour
{
    private void Awake()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(OptionsMenu.Open);
    }

    // Al cerrarse la pausa (reanudar, reiniciar, salir) la ventana no debe quedarse abierta.
    private void OnDisable()
    {
        OptionsMenu.Close();
    }
}
