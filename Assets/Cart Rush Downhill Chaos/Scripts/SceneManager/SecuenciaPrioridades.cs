using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Si usas Cinemachine v3. En Cinemachine v2 usa: using Cinemachine;

public class SecuenciaPrioridades : MonoBehaviour
{
    [Header("Lista de Cámaras Cinemáticas en Orden")]
    [SerializeField] private CinemachineCamera[] cinemachineCameras; // En v2 usa: CinemachineVirtualCamera[]

    [Header("Cámara Final del Juego")]
    [SerializeField] private CinemachineCamera gameplayCamera;      // En v2 usa: CinemachineVirtualCamera

    [Header("Tiempos")]
    [SerializeField] private float duracionPorCamara = 3f; // Segundos que dura cada toma

    [Header("Control del Carro")]
    [SerializeField] private RaycastCar carController; // Tu script de control

    private void Start()
    {
        StartCoroutine(ReproducirSecuencia());
    }

    private IEnumerator ReproducirSecuencia()
    {
        // 1. Bloquear el control del vehículo durante la cinemática
        if (carController != null)
        {
            carController.enabled = false;
        }

        // Bajar la prioridad de la cámara de juego al inicio
        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = 0;
        }

        // 2. Transicionar por la Cámara 1, 2, 3...
        for (int i = 0; i < cinemachineCameras.Length; i++)
        {
            // Resetear prioridades de todas las cámaras cinemáticas
            ResetearPrioridades();

            // Elevar la prioridad de la cámara actual
            cinemachineCameras[i].Priority = 20;

            // Esperar el tiempo de la toma
            yield return new WaitForSeconds(duracionPorCamara);
        }

        // 3. Activar la Cámara Meta/Juego del Carro
        ResetearPrioridades();
        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = 30; // La prioridad más alta toma el control definitivo
        }

        // 4. Devolver el control al jugador
        if (carController != null)
        {
            carController.enabled = true;
        }
    }

    private void ResetearPrioridades()
    {
        foreach (var cam in cinemachineCameras)
        {
            if (cam != null) cam.Priority = 0;
        }
    }
}