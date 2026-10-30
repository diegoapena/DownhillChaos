using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCartController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform modeloVisual;     // Asigna aqu� el hijo 'Cube' o carrocer�a
    public LayerMask capaSuelo;        // Selecciona la capa 'Suelo'

    [Header("Velocidad y Aceleraci�n")]
    public float aceleracion = 120f;
    public float velocidadMaxima = 40f;
    public float reversa = 20f;
    public float fuerzaFreno = 5f;
    public float fuerzaGravedadExtra = 40f; // Mantiene el carrito pegado al suelo en rampas

    [Header("Direcci�n y Derrape")]
    public float fuerzaGiro = 90f;
    public float inclinacionCarroceri = 15f;
    [Range(0f, 1f)]
    public float derrapeFriccion = 0.90f;

    [Header("Alineaci�n con Rampas")]
    public float distanciaRayoSuelo = 2.5f;
    public float velocidadAlineacion = 10f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool enSuelo;
    private Vector3 normalSuelo = Vector3.up;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Bloqueamos inclinaciones f�sicas impredecibles para evitar que vuele o se ponga inestable
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (modeloVisual == null && transform.childCount > 0)
        {
            modeloVisual = transform.GetChild(0);
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;

        moveInput = new Vector2(x, y);

        InclinacionVisual();
    }

    void FixedUpdate()
    {
        ComprobarSuelo();
        AplicarMovimiento();
        AplicarGiro();
        SimularDerrapeArcade();
    }

    void ComprobarSuelo()
    {
        RaycastHit hit;
        // Raycast apuntando directamente hacia abajo en el mundo global
        enSuelo = Physics.Raycast(transform.position, Vector3.down, out hit, distanciaRayoSuelo, capaSuelo);

        if (enSuelo)
        {
            normalSuelo = hit.normal;
            // Fuerza hacia abajo para no despegarse en la cresta de la rampa
            rb.AddForce(-normalSuelo * fuerzaGravedadExtra, ForceMode.Acceleration);
        }
        else
        {
            normalSuelo = Vector3.up;
            rb.AddForce(Vector3.down * fuerzaGravedadExtra, ForceMode.Acceleration);
        }
    }

    void AplicarMovimiento()
    {
        float velAdelante = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (moveInput.y > 0)
        {
            if (velAdelante < velocidadMaxima)
            {
                rb.AddForce(transform.forward * aceleracion, ForceMode.Acceleration);
            }
        }
        else if (moveInput.y < 0)
        {
            if (velAdelante > -reversa)
            {
                rb.AddForce(-transform.forward * aceleracion * 0.6f, ForceMode.Acceleration);
            }
        }
        else
        {
            Vector3 velHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(-velHorizontal * fuerzaFreno, ForceMode.Acceleration);
        }
    }

    void AplicarGiro()
    {
        float velocidadActual = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        if (velocidadActual > 0.2f && moveInput.x != 0)
        {
            float sentidoGiro = Vector3.Dot(rb.linearVelocity, transform.forward) >= -0.1f ? 1f : -1f;
            float rotacionY = moveInput.x * fuerzaGiro * sentidoGiro * Time.fixedDeltaTime;

            transform.Rotate(0f, rotacionY, 0f, Space.World);
        }
    }

    void SimularDerrapeArcade()
    {
        Vector3 velForward = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 velRight = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        rb.linearVelocity = velForward + (velRight * derrapeFriccion) + new Vector3(0, rb.linearVelocity.y, 0);
    }

    void InclinacionVisual()
    {
        if (modeloVisual == null) return;

        // Inclinaci�n de rampa adaptada a la normal del suelo en el modelo est�tico
        Quaternion rotacionRampa = Quaternion.FromToRotation(Vector3.up, normalSuelo);

        // Inclinaci�n est�tica lateral en giros
        float anguloZ = -moveInput.x * inclinacionCarroceri;
        Quaternion rotacionGiro = Quaternion.Euler(0, 0, anguloZ);

        Quaternion rotacionTotalLocal = rotacionRampa * rotacionGiro;

        modeloVisual.localRotation = Quaternion.Slerp(
            modeloVisual.localRotation,
            rotacionTotalLocal,
            Time.deltaTime * velocidadAlineacion
        );
    }
}