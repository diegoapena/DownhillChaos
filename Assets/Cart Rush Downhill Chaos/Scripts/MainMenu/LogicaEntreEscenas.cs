using UnityEngine;

public class LogicaEntreEscenas : MonoBehaviour
{
    private void Awake()
    {
        var noDestruirEntreEscenas = FindObjectsByType<LogicaEntreEscenas>(FindObjectsSortMode.None);
        if (noDestruirEntreEscenas.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
