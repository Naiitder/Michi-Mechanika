using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public bool isGamePaused;
    
    [Header("Canvas")]
    [SerializeField] private GameObject pauseCanvas;
    
    [Header("Speed")]
    [SerializeField] private float normalTimeScale = 1f;
    [SerializeField] private float fastForwardTimeScale = 2f;
    
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
        
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        
        Texture2D cursorTexture = Resources.Load<Texture2D>("Steampunk_UI_icon_02");
        Vector2 hotspot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        
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
        Application.Quit();
    }
    
    private void UpdateTimeScale()
    {
        if (isGamePaused)
            return;

        int pending = InputController.instance != null ? InputController.instance.BufferCount : 0;

        Time.timeScale = pending > 0 ? fastForwardTimeScale : normalTimeScale;
    }

}
