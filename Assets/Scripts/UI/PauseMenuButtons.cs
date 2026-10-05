using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Respuesta de los botones del menú de pausa. Va en el objeto PauseMenu de Canvas.prefab:
/// cada tarjeta (Resume, Restart, Options, To Main Menu) crece al pasar el ratón por encima y
/// suena el click del menú principal al pulsarla. Usa tiempo real, porque en pausa el juego está parado.
/// </summary>
public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    [Header("Hover")]
    [Tooltip("Tamaño de la tarjeta con el ratón encima (1 = sin cambio).")]
    [SerializeField] private float hoverScale = 1.08f;
    [Tooltip("Segundos que tarda en crecer o encoger.")]
    [SerializeField] private float hoverTime = 0.1f;

    private void Awake()
    {
        // Así la ventana de opciones también suena aunque no se haya pasado por el menú principal.
        MainMenuController.SetSharedClickIfMissing(clickSound, clickVolume);

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            button.onClick.AddListener(PlayClick);

            // Una tarjeta es el botón de más arriba: el icono de dentro también lleva un Button.
            Transform parent = button.transform.parent;
            if (parent != null && parent.GetComponentInParent<Button>(true) != null) continue;
            button.gameObject.AddComponent<CardHover>().owner = this;
        }
    }

    private void PlayClick()
    {
        if (clickSound != null) MainMenuController.PlayClick(clickSound, clickVolume);
    }

    private class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PauseMenuButtons owner;

        private Vector3 baseScale;
        private bool hovered;

        private void Awake() => baseScale = transform.localScale;

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        // Al cerrarse el menú no llega el evento de salida: se deja la tarjeta como estaba.
        private void OnDisable()
        {
            hovered = false;
            transform.localScale = baseScale;
        }

        private void Update()
        {
            Vector3 target = baseScale * (hovered ? owner.hoverScale : 1f);
            float distance = Mathf.Abs(owner.hoverScale - 1f) * baseScale.x;
            float step = owner.hoverTime > 0f ? distance / owner.hoverTime * Time.unscaledDeltaTime : float.MaxValue;
            transform.localScale = Vector3.MoveTowards(transform.localScale, target, step);
        }
    }
}
