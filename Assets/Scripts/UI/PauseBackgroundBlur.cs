using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Desenfoca el juego mientras el menú de pausa está abierto. Va en el objeto PauseMenu:
/// al activarse enciende un Volume global con el perfil de desenfoque, y al desactivarse lo apaga.
/// El Canvas se dibuja encima del posprocesado, así que el menú se ve nítido.
/// </summary>
public class PauseBackgroundBlur : MonoBehaviour
{
    [Tooltip("Perfil con el desenfoque (Depth Of Field) que se aplica al fondo durante la pausa.")]
    [SerializeField] private VolumeProfile profile;

    private GameObject volumeObject;

    private void OnEnable()
    {
        if (profile == null) return;

        if (volumeObject == null)
        {
            // Objeto propio en la capa Default: es la que lee la cámara para los Volumes.
            volumeObject = new GameObject("Pause Blur Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }

        volumeObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (volumeObject != null) volumeObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (volumeObject != null) Destroy(volumeObject);
    }
}
