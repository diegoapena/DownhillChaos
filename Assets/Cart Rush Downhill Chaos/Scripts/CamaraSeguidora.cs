using UnityEngine;

public class CamaraSeguidora : MonoBehaviour
{
    public Transform objetivo; // Arrastra tu Carrito aquí
    public Vector3 offset = new Vector3(0, 3, -6); // Distancia (X, Y, Z)

    void LateUpdate()
    {
        if (objetivo != null)
        {
            // Sigue la posición del carrito manteniendo la distancia
            transform.position = objetivo.position + offset;

            // Mira hacia el carrito
            transform.LookAt(objetivo);
        }
    }
}