using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RaycastCar : MonoBehaviour
{
    [Header("Transformaciones de las Ruedas")]
    [SerializeField] private Transform[] wheelTransforms;

    [Header("Suspensión (Calibrada)")]
    [SerializeField] private float restDistance = 0.5f;
    [SerializeField] private float springStrength = 25000f;
    [SerializeField] private float springDamper = 1800f;

    [Header("Ajuste Visual de la Rueda")]
    [SerializeField] private float wheelRadius = 0.2f;

    [Header("Capas")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Agarre y Fricción Lateral")]
    [SerializeField][Range(0f, 1f)] private float tireGripFactor = 0.45f; // Valor reducido para derrape fluido
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Motor y Frenos")]
    [SerializeField] private float accelPower = 15000f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
    [SerializeField] private float brakePower = 20000f;

    [Header("Estabilidad")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.7f, 0f);
    [SerializeField] private float antiRollForce = 400f;

    [Header("Control en el Aire")]
    [Tooltip("Fuerza de giro cuando el carro está saltando o fuera del suelo")]
    [SerializeField] private float airControlPower = 1500f;

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
        rb.centerOfMass = centerOfMassOffset;
    }

    private void Update()
    {
        CheckIfFlipped();
    }

    private void FixedUpdate()
    {
        float accelInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        bool isGrounded = false;

        for (int i = 0; i < wheelTransforms.Length; i++)
        {
            Transform wheel = wheelTransforms[i];

            // Ángulo de dirección para ruedas delanteras (0 y 1)
            float currentSteerAngle = (i == 0 || i == 1) ? steerInput * maxSteerAngle : 0f;
            Quaternion steerRotation = Quaternion.AngleAxis(currentSteerAngle, wheel.up);

            Vector3 steeringDir = steerRotation * wheel.right;

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

                // 1. POSICIÓN VISUAL
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

        // 4. SISTEMA DE MOTOR CENTRALIZADO (Conserva impulso en curvas)
        if (isGrounded)
        {
            if (isBraking)
            {
                Vector3 forwardVel = Vector3.Project(rb.linearVelocity, transform.forward);
                rb.AddForce(-forwardVel * brakePower * Time.fixedDeltaTime, ForceMode.Force);
            }
            else if (accelInput != 0)
            {
                float currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);

                // Bonus leve en curvas para mantener velocidad fluida
                float turnBoost = (Mathf.Abs(steerInput) > 0.1f) ? 1.2f : 1.0f;
                float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelInput * turnBoost;

                // NUEVO: Calcula la dirección promedio de las ruedas delanteras
                float avgSteerAngle = steerInput * maxSteerAngle;
                Quaternion steerRot = Quaternion.AngleAxis(avgSteerAngle, transform.up);
                Vector3 forwardDriveDir = steerRot * transform.forward;

                rb.AddForce(transform.forward * (availableTorque * accelPower), ForceMode.Force);
            }

            // Anti-Roll para estabilidad constante
            Vector3 predictedUp = Quaternion.AngleAxis(rb.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f, rb.angularVelocity) * transform.up;
            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * antiRollForce * rb.mass);
        }
        else
        {
            // CONTROL EN EL AIRE
            if (steerInput != 0)
            {
                rb.AddTorque(transform.up * (steerInput * airControlPower));
            }
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