using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviour
{

    public static LevelManager instance;

    [SerializeField] private GameObject loaderCanvas;
    [SerializeField] private Image progressBar;
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
        StartCoroutine(FadeIn());
    }


    public async void LoadScene(string sceneName)
    {
        if (isLoading) return; 
        isLoading = true;
        
        
        if(GameController.instance.isGamePaused) GameController.instance.ResumeGame();
        
        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        loaderCanvas.SetActive(true);

        do
        {
            await Task.Delay(100);
            targetProgress = scene.progress;
        } while (scene.progress < 0.9f);

        await Task.Delay(1000);

        scene.allowSceneActivation = true;
    }
    
    public void UpdateProgressBar()
    {
        progressBar.fillAmount = Mathf.MoveTowards(progressBar.fillAmount, targetProgress, 3*Time.deltaTime);
    }

    
    public void RestartScene()
    {
        StartCoroutine(FadeOutAndRestart());
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
}
