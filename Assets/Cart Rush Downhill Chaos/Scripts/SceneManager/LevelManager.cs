using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("UI Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;

    private bool isRestarting = false;

    private void Awake()
    {
        // Forzar alpha en 1 al inicio para tapar la pantalla antes del primer frame
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 1f;
        }
    }

    private void Start()
    {
        // Iniciar el fade in (de 1 a 0) al cargar la escena
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeInInitial());
        }
    }

    private IEnumerator FadeInInitial()
    {
        float timer = 0f;

        // Transición de alpha 1 a 0
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    public void TriggerRestart()
    {
        if (!isRestarting)
        {
            StartCoroutine(FadeAndRestart());
        }
    }

    private IEnumerator FadeAndRestart()
    {
        isRestarting = true;
        float timer = 0f;

        // Transición de alpha 0 a 1
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        // Cargar la escena actual
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}