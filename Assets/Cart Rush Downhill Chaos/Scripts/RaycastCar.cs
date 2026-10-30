using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RaycastCar : MonoBehaviour
{
    [Header("Transformaciones de las Ruedas")]
    [SerializeField] private Transform[] wheelTransforms; // Los 4 puntos vacíos (Point_Rueda_...)

    [Header("Suspensión (Calibrada)")]
    [SerializeField] private float restDistance = 0.5f;     // Recorrido máximo de la suspensión
    [SerializeField] private float springStrength = 25000f; // Fuerza del resorte
    [SerializeField] private float springDamper = 1800f;   // Amortiguación anti-rebote brusco

    [Header("Ajuste Visual de la Rueda")]
    [SerializeField] private float wheelRadius = 0.2f;     // Ajuste del radio de la rueda

    [Header("Capas")]
    [SerializeField] private LayerMask groundLayer;        // Selecciona la capa Ground

    [Header("Agarre y Fricción Lateral")]
    [SerializeField][Range(0f, 1f)] private float tireGripFactor = 0.95f;
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Motor y Frenos")]
    [SerializeField] private float accelPower = 15000f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
    [SerializeField] private float brakePower = 20000f;

    [Header("Detección de Volcadura")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private float flipThreshold = -0.5f;
    [SerializeField] private float timeToTriggerFlip = 2.0f;

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
        float accelInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        for (int i = 0; i < wheelTransforms.Length; i++)
        {
            Transform wheel = wheelTransforms[i];

            // Ángulo de dirección para ruedas delanteras (0 y 1)
            float currentSteerAngle = (i == 0 || i == 1) ? steerInput * maxSteerAngle : 0f;
            Quaternion steerRotation = Quaternion.AngleAxis(currentSteerAngle, wheel.up);

            Vector3 steeringDir = steerRotation * wheel.right;
            Vector3 accelDir = steerRotation * wheel.forward;

            // Obtener la malla visual (debe ser el primer hijo de Point_Rueda_...)
            Transform wheelMesh = wheel.childCount > 0 ? wheel.GetChild(0) : null;

            // Girar rueda visualmente
            if (wheelMesh != null)
            {
                wheelMesh.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
            }

            // Raycast hacia abajo local de la rueda
            if (Physics.Raycast(wheel.position, -wheel.up, out RaycastHit hit, restDistance, groundLayer))
            {
                Vector3 wheelWorldVel = rb.GetPointVelocity(wheel.position);

                // 1. POSICIÓN VISUAL BLINDADA: Ajusta únicamente el eje Y local de la malla
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

                // 4. ACELERACIÓN / FRENO
                if (isBraking)
                {
                    float forwardVel = Vector3.Dot(accelDir, wheelWorldVel);
                    float brakeAccel = -forwardVel * brakePower;
                    rb.AddForceAtPosition(accelDir * brakeAccel, wheel.position);
                }
                else if (accelInput != 0)
                {
                    float currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
                    float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
                    float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelInput;

                    rb.AddForceAtPosition(accelDir * (availableTorque * accelPower), wheel.position);
                }
                else
                {
                    float forwardVel = Vector3.Dot(accelDir, wheelWorldVel);
                    float passiveBrake = -forwardVel * 1000f;
                    rb.AddForceAtPosition(accelDir * passiveBrake, wheel.position);
                }
            }
            else
            {
                // EN EL AIRE: Extensión máxima en reposo sin desintegrar la rueda
                if (wheelMesh != null)
                {
                    wheelMesh.localPosition = new Vector3(0f, -restDistance + wheelRadius, 0f);
                }
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
}