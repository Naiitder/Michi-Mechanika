using UnityEditor;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    
    [Header("Game Status")]
    public bool isGamePaused;
    public bool isLevelBuilding;
    
    [Header("Cursor")]
    [SerializeField] Texture2D cursorWindows; 
    [SerializeField] Texture2D cursorMac;
    
    [Header("Canvas")]
    [SerializeField] private GameObject pauseCanvas;
    
    [Header("Speed")]
    [SerializeField] private float normalTimeScale = 1f;
    [SerializeField] private float fastForwardTimeScale = 1.5f;
    [SerializeField] private float timeScaleBlendTime = 0.25f;
    private bool fastForward;
    
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
        
        GameSettings.ApplyFrameRate();
        
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        Cursor.SetCursor(cursorMac, Vector2.zero, CursorMode.Auto);
#else
        Cursor.SetCursor(cursorWindows, Vector2.zero, CursorMode.Auto);
#endif
        
        if(pauseCanvas != null) pauseCanvas.SetActive(false);
    }

    private void Update()
    {
        HandlePause();
        UpdateTimeScale();
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
        Time.timeScale = normalTimeScale;
        
    }
    
    public void SetPause()
    {
        pauseCanvas.SetActive(true);
        isGamePaused = true;
        Time.timeScale = 0f;
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
        
    }
    
    private void UpdateTimeScale()
    {
        if (isGamePaused)
            return;

        int pending = InputController.instance != null ? InputController.instance.BufferCount : 0;
        bool levelEnded = GameFlow.instance != null && GameFlow.instance.levelEnded;
        bool busy = GameFlow.instance != null && !GameFlow.instance.canInteract;

        if (levelEnded) fastForward = false;
        else if (pending > 0) fastForward = true;
        else if(!busy)  fastForward = false;

        float target = fastForward ? fastForwardTimeScale : normalTimeScale;
        if (timeScaleBlendTime <= 0f)
        {
            Time.timeScale = target;
            return;
        }

        // Transición suave en tiempo real (no escalado) para que el cambio no sea de golpe.
        float rate = Mathf.Abs(fastForwardTimeScale - normalTimeScale) / timeScaleBlendTime;
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, target, rate * Time.unscaledDeltaTime);
    }

}
