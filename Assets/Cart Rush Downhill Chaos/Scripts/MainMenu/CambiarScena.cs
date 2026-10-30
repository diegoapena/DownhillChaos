using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarScena : MonoBehaviour
{
    public void CambiarScenaLevel(string name)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
    }
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}
