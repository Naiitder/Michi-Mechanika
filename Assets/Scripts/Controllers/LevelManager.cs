using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviour
{

    public static LevelManager instance;

    [SerializeField] private GameObject loaderCanvas;
    private float targetProgress;
    
    private bool isLoading = false;

    [Header ("FadeTransition")]
    public Image fadeImage;
    public float fadeDuration = 1f;
    
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if(loaderCanvas != null) loaderCanvas.SetActive(false);
    }
    
    private void Start()
    {
        if(fadeImage != null) StartCoroutine(FadeIn());
    }


    public async void LoadScene(string sceneName)
    {
        if (isLoading) return; 
        isLoading = true;
        
        SaveNextLevelProgress(sceneName);
        if(GameController.instance.isGamePaused) GameController.instance.ResumeGame();
        
        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        loaderCanvas.SetActive(true);

        do
        {
            await Task.Delay(100);
            targetProgress = scene.progress;
        } while (scene.progress < 0.9f);
        
        //TO DO Fade LOADSCENE CANVAS
        
        await Task.Delay(1500);

        scene.allowSceneActivation = true;
    }
    
    public void RestartScene()
    {
        StartCoroutine(FadeOutAndRestart());
    }

    public void LoadSceneFromUI(string sceneName)
    {
        StartCoroutine(LoadSceneFade(sceneName));
    }

    public IEnumerator LoadSceneFade(string sceneName)
    {
        Time.timeScale = 1;
        yield return Fade(0, 1); 
        LoadScene(sceneName);
    }
    
    private IEnumerator FadeIn()
    {
        yield return Fade(1, 0);
    }
    
    private IEnumerator FadeOutAndRestart()
    {
        Time.timeScale = 1;
        yield return Fade(0, 1); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, endAlpha);
    }
    
    private void SaveNextLevelProgress(string sceneName)
    {
        try
        {
            string numbersPart = sceneName.Replace("Level", ""); 
            
            string[] parts = numbersPart.Split('-');

            if(parts.Length == 2)
            {
                int chapter = int.Parse(parts[0]);
                int level = int.Parse(parts[1]);
                
                if (SQLiteDB.instance != null)
                    SQLiteDB.instance.SaveLevelCompleted(chapter, level);
            }
            else
            {
                Debug.LogWarning("Nombre de escena no tiene el formato esperado: " + sceneName);
            }
        }
        catch(Exception e)
        {
            Debug.LogError("Error al parsear el nivel de la escena: " + e.Message);
        }
    }
}
