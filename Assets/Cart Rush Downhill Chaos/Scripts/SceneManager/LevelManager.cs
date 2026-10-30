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
        // Se asegura de activar la imagen en runtime y mantenerla transparente al inicio
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;
        }
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