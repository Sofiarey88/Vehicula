using System;
using UnityEngine;

/// <summary>
/// IA de conducción para autos rivales usando Rigidbody directo.
/// No usa WheelColliders: la velocidad se controla con linearVelocity
/// y la rotación con RotateTowards. Simple, predecible y fácil de tunear.
///
/// SETUP del prefab RivalCar:
/// - Eliminar WheelVehicleController y todos los WheelCollider del prefab.
/// - Agregar BoxCollider al cuerpo (NO trigger) para colisiones con el player.
/// - En Rigidbody: Freeze Rotation X y Z (evita que vuelque).
/// - Asignar los transforms visuales de ruedas en el Inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CarRaceProgress))]
public sealed class RivalCarAI : MonoBehaviour
{
    [Serializable]
    private sealed class WheelVisualSetup
    {
        [SerializeField]
        [Tooltip("Transform visual de la rueda.")]
        private Transform wheelTransform;

        [SerializeField]
        [Tooltip("Si está activo, la rueda recibe el ángulo de dirección visualmente.")]
        private bool isSteerable;

        public Transform WheelTransform => wheelTransform;
        public bool IsSteerable => isSteerable;
    }

    [Header("Visuales de Ruedas")]
    [SerializeField]
    [Tooltip("Asignar los 4 transforms de rueda desde Wheels > Meshes.")]
    private WheelVisualSetup[] wheelVisuals;

    [SerializeField]
    [Tooltip("Radio aproximado de la rueda para calcular la rotación visual (m).")]
    private float wheelRadius = 0.35f;

    [Header("Física")]
    [SerializeField]
    [Tooltip("Aceleración máxima (m/s²).")]
    private float acceleration = 8f;

    [SerializeField]
    [Tooltip("Deceleración al frenar (m/s²).")]
    private float braking = 14f;

    [SerializeField]
    [Tooltip("Velocidad de giro máxima (grados/segundo). Aumentar si la IA no toma bien las curvas.")]
    private float maxTurnDegreesPerSecond = 120f;

    [Header("Navegación")]
    [SerializeField]
    [Tooltip("Distancia al waypoint para avanzar al siguiente (m).")]
    private float waypointArrivalDistance = 8f;

    [SerializeField]
    [Tooltip("Distancia desde la que se anticipa la velocidad del siguiente waypoint para frenar antes de curvas (m).")]
    private float brakeLookAheadDistance = 25f;

    [SerializeField]
    [Tooltip("Margen de tolerancia de velocidad (km/h) para evitar aceleraciones/frenadas nerviosas.")]
    private float speedToleranceKmh = 4f;

    private Rigidbody _rigidbody;
    private int _targetWaypointIndex = 1;
    private float _wheelSpinAngle = 0f;
    private float _currentSteerAngle = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        // Impedir que el auto vuelque; la IA controla la rotación Y manualmente
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX
                               | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        if (RaceManager.Instance == null)
        {
            Debug.LogError("[RivalCarAI] RaceManager NO encontrado en escena. El auto no se moverá.", this);
            return;
        }

        if (RaceManager.Instance.WaypointCount == 0)
        {
            Debug.LogError("[RivalCarAI] RaceManager existe pero no tiene waypoints asignados.", this);
            return;
        }

        Debug.Log($"[RivalCarAI] {gameObject.name} listo — {RaceManager.Instance.WaypointCount} waypoints detectados.");
    }

    private void FixedUpdate()
    {
        if (RaceManager.Instance == null || RaceManager.Instance.WaypointCount == 0)
            return;

        DriveTowardsWaypoint();
        UpdateWheelVisuals();
    }

    private void DriveTowardsWaypoint()
    {
        RacingWaypoint target = RaceManager.Instance.GetWaypoint(_targetWaypointIndex);
        RacingWaypoint lookAhead = RaceManager.Instance.GetNextWaypoint(_targetWaypointIndex);

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float distanceToTarget = toTarget.magnitude;

        // ?? Dirección ????????????????????????????????????????????????????
        if (toTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                maxTurnDegreesPerSecond * Time.fixedDeltaTime
            );

            _currentSteerAngle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        }

        // ?? Velocidad objetivo ????????????????????????????????????????????
        // Look-ahead: si está cerca del waypoint actual, anticipar la velocidad del siguiente
        float effectiveTargetSpeedKmh = distanceToTarget < brakeLookAheadDistance
            ? Mathf.Min(target.TargetSpeedKmh, lookAhead.TargetSpeedKmh)
            : target.TargetSpeedKmh;

        float targetSpeedMs = effectiveTargetSpeedKmh / 3.6f;
        float currentSpeedMs = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
        float toleranceMs = speedToleranceKmh / 3.6f;

        // ?? Aplicar aceleración o freno ???????????????????????????????????
        float deltaSpeed;
        if (currentSpeedMs < targetSpeedMs - toleranceMs)
            deltaSpeed = acceleration * Time.fixedDeltaTime;
        else if (currentSpeedMs > targetSpeedMs + toleranceMs)
            deltaSpeed = -braking * Time.fixedDeltaTime;
        else
            deltaSpeed = 0f;

        float newSpeedMs = Mathf.Max(0f, currentSpeedMs + deltaSpeed);

        // Preservar velocidad vertical para que la gravedad siga actuando
        _rigidbody.linearVelocity = new Vector3(
            transform.forward.x * newSpeedMs,
            _rigidbody.linearVelocity.y,
            transform.forward.z * newSpeedMs
        );

        // ?? Avanzar al siguiente waypoint ?????????????????????????????????
        if (distanceToTarget < waypointArrivalDistance)
            _targetWaypointIndex = (_targetWaypointIndex + 1) % RaceManager.Instance.WaypointCount;
    }

    private void UpdateWheelVisuals()
    {
        float speedMs = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
        float safeRadius = Mathf.Max(wheelRadius, 0.01f);

        // Acumular ángulo de giro de la rueda según velocidad
        _wheelSpinAngle += (speedMs / (2f * Mathf.PI * safeRadius)) * Mathf.Rad2Deg * Time.fixedDeltaTime;

        foreach (WheelVisualSetup wheel in wheelVisuals)
        {
            if (wheel.WheelTransform == null)
                continue;

            float steer = wheel.IsSteerable ? _currentSteerAngle : 0f;

            // X = giro de rodadura, Y = ángulo de dirección
            wheel.WheelTransform.localRotation = Quaternion.Euler(_wheelSpinAngle, steer, 0f);
        }
    }
}