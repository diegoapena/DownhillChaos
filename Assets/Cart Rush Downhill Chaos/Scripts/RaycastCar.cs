using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RaycastCar : MonoBehaviour
{
    [Header("Transformaciones de las Ruedas")]
    [SerializeField] private Transform[] wheelTransforms; // Los 4 puntos vacíos (Point_Rueda_...)

    [Header("Suspensión (Calibrada)")]
    [SerializeField] private float restDistance = 0.5f;
    [SerializeField] private float springStrength = 25000f;
    [SerializeField] private float springDamper = 1800f;

    [Header("Ajuste Visual de la Rueda")]
    [SerializeField] private float wheelRadius = 0.2f;

    [Header("Capas")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Agarre y Fricción Lateral")]
    [SerializeField][Range(0f, 1f)] private float tireGripFactor = 0.95f;
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Motor (Aceleración Automática)")]
    [SerializeField] private float accelPower = 15000f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);

    [Header("Control en el Aire")]
    [Tooltip("Fuerza de giro cuando el carro está saltando o fuera del suelo")]
    [SerializeField] private float airControlPower = 1200f;

    [Header("Detección de Volcadura")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private float flipThreshold = -0.5f;
    [SerializeField] private float timeToTriggerFlip = 2.0f;

    [Header("Elementos de UI (Opcional)")]
    [SerializeField] private GameObject miObjetoUI;

    private float flippedTimer = 0f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        CheckIfFlipped();
    }

    private void FixedUpdate()
    {
        // Solo leemos el control horizontal (Izquierda / Derecha)
        float steerInput = Input.GetAxis("Horizontal");

        bool isGrounded = false;

        for (int i = 0; i < wheelTransforms.Length; i++)
        {
            Transform wheel = wheelTransforms[i];

            // Ángulo de dirección para ruedas delanteras (0 y 1)
            float currentSteerAngle = (i == 0 || i == 1) ? steerInput * maxSteerAngle : 0f;
            Quaternion steerRotation = Quaternion.AngleAxis(currentSteerAngle, wheel.up);

            Vector3 steeringDir = steerRotation * wheel.right;
            Vector3 accelDir = steerRotation * wheel.forward;

            Transform wheelMesh = wheel.childCount > 0 ? wheel.GetChild(0) : null;

            // Girar rueda visualmente
            if (wheelMesh != null)
            {
                wheelMesh.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
            }

            // Raycast hacia abajo local de la rueda
            if (Physics.Raycast(wheel.position, -wheel.up, out RaycastHit hit, restDistance, groundLayer))
            {
                isGrounded = true;
                Vector3 wheelWorldVel = rb.GetPointVelocity(wheel.position);

                // 1. POSICIÓN VISUAL BLINDADA
                if (wheelMesh != null)
                {
                    float yPos = -hit.distance + wheelRadius;
                    wheelMesh.localPosition = new Vector3(0f, yPos, 0f);
                }

                // 2. FUERZA DE SUSPENSIÓN
                Vector3 springDir = wheel.up;
                float offset = restDistance - hit.distance;
                float vel = Vector3.Dot(springDir, wheelWorldVel);
                float force = (offset * springStrength) - (vel * springDamper);

                rb.AddForceAtPosition(springDir * force, wheel.position);

                // 3. FRICCIÓN LATERAL
                float steeringVel = Vector3.Dot(steeringDir, wheelWorldVel);
                float desiredVelChange = -steeringVel * tireGripFactor;
                float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                rb.AddForceAtPosition(steeringDir * (desiredAccel * (rb.mass / 4f)), wheel.position);

                // 4. ACELERACIÓN AUTOMÁTICA CONSTANTE (Siempre avanza)
                float currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);

                // Mantiene el impulso en curvas
                float turnBoost = (Mathf.Abs(steerInput) > 0.1f) ? 1.25f : 1.0f;
                float availableTorque = powerCurve.Evaluate(normalizedSpeed) * turnBoost;

                rb.AddForceAtPosition(accelDir * (availableTorque * accelPower), wheel.position);
            }
            else
            {
                // EN EL AIRE
                if (wheelMesh != null)
                {
                    wheelMesh.localPosition = new Vector3(0f, -restDistance + wheelRadius, 0f);
                }
            }
        }

        // CONTROL AÉREO: Girar en el aire con A/D o Flechas
        if (!isGrounded && steerInput != 0)
        {
            rb.AddTorque(transform.up * (steerInput * airControlPower));
        }
    }

    private void CheckIfFlipped()
    {
        float dotProduct = Vector3.Dot(transform.up, Vector3.down);

        if (dotProduct > flipThreshold)
        {
            flippedTimer += Time.deltaTime;

            if (flippedTimer >= timeToTriggerFlip)
            {
                SafeDestroyUI();

                if (levelManager != null)
                {
                    levelManager.TriggerRestart();
                }
            }
        }
        else
        {
            flippedTimer = 0f;
        }
    }

    public void SafeDestroyUI()
    {
        if (miObjetoUI != null)
        {
            Destroy(miObjetoUI);
        }
    }
}