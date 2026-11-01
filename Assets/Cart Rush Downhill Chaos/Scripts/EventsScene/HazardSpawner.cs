using System.Collections.Generic;
using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    [Header("Configuración del Pool")]
    public GameObject[] prefabsPeligros; // Arrastra tus prefabs de rocas/cactus aquí
    public int tamañoPool = 20;

    [Header("Configuración de Caída")]
    public float radioAparicion = 8f; // Área para que no caigan en línea recta
    public float tiempoEntreApariciones = 1.5f;
    public float tiempoDeVida = 5f; // Tiempo antes de desaparecer

    [Tooltip("Fuerza de impulso para tu carro de 1500 de masa")]
    public float fuerzaEmpuje = 8000f;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private float temporizador = 0f;

    void Start()
    {
        for (int i = 0; i < tamañoPool; i++)
        {
            GameObject obj = Instantiate(prefabsPeligros[Random.Range(0, prefabsPeligros.Length)]);
            obj.SetActive(false);

            HazardBehavior comportamiento = obj.GetComponent<HazardBehavior>();
            if (comportamiento == null) comportamiento = obj.AddComponent<HazardBehavior>();

            comportamiento.Configurar(this, tiempoDeVida, fuerzaEmpuje);
            pool.Enqueue(obj);
        }
    }

    void Update()
    {
        temporizador += Time.deltaTime;
        if (temporizador >= tiempoEntreApariciones && pool.Count > 0)
        {
            temporizador = 0f;
            SoltarPeligro();
        }
    }

    void SoltarPeligro()
    {
        GameObject peligro = pool.Dequeue();

        Vector2 posAleatoria = Random.insideUnitCircle * radioAparicion;
        peligro.transform.position = transform.position + new Vector3(posAleatoria.x, 0, posAleatoria.y);

        Rigidbody rb = peligro.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        peligro.SetActive(true);
    }

    public void DevolverAlPool(GameObject peligro)
    {
        peligro.SetActive(false);
        pool.Enqueue(peligro);
    }
}
