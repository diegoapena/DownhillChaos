using UnityEngine;

public class ArcadeCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;           // Objeto Carrito
    [SerializeField] private Rigidbody targetRigidbody;  // Rigidbody del Carrito

    [Header("Distancias y Offsets")]
    [Tooltip("Ajusta X si el carro aparece descentrado a la izquierda o derecha")]
    [SerializeField] private Vector3 offsetForward = new Vector3(0f, 3.5f, -7f);
    [SerializeField] private Vector3 offsetReverse = new Vector3(0f, 3.5f, 7f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f); // Punto al que apunta el centro de la cámara

    [Header("Suavizado (Lerp/Slerp)")]
    [SerializeField] private float moveSmoothSpeed = 10f;   // Recomendado: 8 a 12
    [SerializeField] private float rotateSmoothSpeed = 8f;  // Recomendado: 6 a 10

    [Header("Detección de Reversa")]
    [SerializeField] private float reverseSpeedThreshold = -0.5f;

    private Vector3 currentOffset;

    private void Start()
    {
        currentOffset = offsetForward;

        if (target != null && targetRigidbody == null)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Detectar marcha hacia adelante o reversa
        float forwardVelocity = targetRigidbody != null ? Vector3.Dot(target.forward, targetRigidbody.linearVelocity) : 0f;
        Vector3 targetOffset = (forwardVelocity < reverseSpeedThreshold) ? offsetReverse : offsetForward;

        // Suavizar la transición entre la posición frontal y reversa
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * 3f);

        // 2. Posición objetivo centrada respecto a la rotación del carro
        Vector3 desiredPosition = target.position + (target.rotation * currentOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * moveSmoothSpeed);

        // 3. Rotación centrada apuntando hacia el objetivo con el offset focal
        Vector3 targetLookPosition = target.position + (target.rotation * lookAtOffset);
        Quaternion desiredRotation = Quaternion.LookRotation(targetLookPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotateSmoothSpeed);
    }
}