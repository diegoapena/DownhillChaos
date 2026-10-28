using UnityEngine;
using UnityEngine.InputSystem;

public class ControlCarrito : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float fuerzaAceleracion = 30f;
    public float fuerzaReversa = 15f;
    public float velocidadMaxima = 30f;
    public float friccionFrenado = 5f; // Fuerza para frenar cuando no presionas nada

    [Header("Configuración de Giro (Dirección)")]
    public float velocidadGiro = 90f; // Grados por segundo al girar
    public float anguloMaxInclinacion = 10f; // Inclina suavemente la carrocería en curvas

    [Header("Referencias Visuales")]
    public Transform modeloVisual; // Objeto hijo que se inclina en curvas

    private Rigidbody rb;
    private float entradaAvanzar;
    private float entradaGiro;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Si no asignas modelo visual, toma el primer hijo
        if (modeloVisual == null && transform.childCount > 0)
        {
            modeloVisual = transform.GetChild(0);
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Leer Entrada de Teclado
        entradaAvanzar = 0f;
        entradaGiro = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) entradaAvanzar += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) entradaAvanzar -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) entradaGiro += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) entradaGiro -= 1f;

        AplicarInclinacionVisual();
    }

    void FixedUpdate()
    {
        ProcesarMovimiento();
        ProcesarGiro();
    }

    void ProcesarMovimiento()
    {
        // Calcular la velocidad relativa local hacia adelante/atrás
        float velocidadAdelanteActual = Vector3.Dot(rb.linearVelocity, transform.forward);

        // 1. ACELERAR ADELANTE (W)
        if (entradaAvanzar > 0)
        {
            if (velocidadAdelanteActual < velocidadMaxima)
            {
                rb.AddForce(transform.forward * fuerzaAceleracion, ForceMode.Acceleration);
            }
        }
        // 2. RETROCEDER / FRENAR (S)
        else if (entradaAvanzar < 0)
        {
            if (velocidadAdelanteActual > -velocidadMaxima / 2f)
            {
                rb.AddForce(-transform.forward * fuerzaReversa, ForceMode.Acceleration);
            }
        }
        // 3. FRENADO AUTOMÁTICO (Sin presionar nada)
        else
        {
            // Reduce progresivamente la velocidad horizontal cuando no se presiona W ni S
            Vector3 velocidadHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(-velocidadHorizontal * friccionFrenado, ForceMode.Acceleration);
        }
    }

    void ProcesarGiro()
    {
        // Solo permite girar si el vehículo se está moviendo (para evitar giros estáticos de tanque)
        float factorVelocidad = Mathf.Clamp01(rb.linearVelocity.magnitude / 2f);

        if (entradaGiro != 0 && factorVelocidad > 0.05f)
        {
            // Invierte la dirección de giro si está en marcha atrás
            float sentidoGiro = Vector3.Dot(rb.linearVelocity, transform.forward) >= -0.1f ? 1f : -1f;

            float rotacionY = entradaGiro * velocidadGiro * sentidoGiro * factorVelocidad * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotacionY, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    void AplicarInclinacionVisual()
    {
        if (modeloVisual == null) return;

        // Inclinación lateral estética en curvas (eje Z local)
        float anguloObjetivo = -entradaGiro * anguloMaxInclinacion;
        Quaternion rotacionObjetivo = Quaternion.Euler(0f, 0f, anguloObjetivo);

        modeloVisual.localRotation = Quaternion.Slerp(
            modeloVisual.localRotation,
            rotacionObjetivo,
            Time.deltaTime * 6f
        );
    }
}