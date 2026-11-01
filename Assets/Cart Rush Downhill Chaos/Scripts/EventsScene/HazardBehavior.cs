using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HazardBehavior : MonoBehaviour
{
    private HazardSpawner spawner;
    private float tiempoDeVida;
    private float fuerzaDeEmpuje;
    private float temporizador;

    public void Configurar(HazardSpawner spawnerAsignado, float tiempo, float fuerza)
    {
        spawner = spawnerAsignado;
        tiempoDeVida = tiempo;
        fuerzaDeEmpuje = fuerza;
    }

    void OnEnable()
    {
        temporizador = 0f;
    }

    void Update()
    {
        temporizador += Time.deltaTime;
        if (temporizador >= tiempoDeVida)
        {
            spawner.DevolverAlPool(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Tu carro debe tener la etiqueta "Player" asignada en el Inspector
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rbCarro = collision.gameObject.GetComponent<Rigidbody>();
            if (rbCarro != null)
            {
                // Calcula la dirección del golpe, añadiendo un ligero empuje hacia arriba (0.2f) para desestabilizar
                Vector3 direccionEmpuje = (collision.transform.position - transform.position).normalized;
                direccionEmpuje.y = 0.2f;

                // Usamos ForceMode.Impulse para simular un impacto seco, ideal para la masa de 1500
                rbCarro.AddForce(direccionEmpuje * fuerzaDeEmpuje, ForceMode.Impulse);
            }
        }
    }
}
