using UnityEngine;

/// <summary>
/// Rastrea el progreso de un auto en la carrera mediante detección por distancia.
/// Se coloca en TODOS los autos (player + rivales). No depende de colliders ni triggers.
///
/// Lógica de vueltas:
/// - Los autos arrancan EN WP_00, por lo que el primer waypoint a cruzar es WP_01.
/// - Cruzar WP_00 después de recorrer toda la pista = vuelta completada.
/// - Anti-trampa: solo el siguiente waypoint esperado en orden es válido.
/// </summary>
public sealed class CarRaceProgress : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    [Tooltip("Activa logs de diagnóstico. Desactivar en producción.")]
    private bool debugMode = true;

    private int _nextExpectedWaypoint = 1;
    private int _lastValidatedWaypoint = 0;
    private int _lapsCompleted = 0;
    private int _racePosition = 0;
    private int _totalWaypoints = 0;

    // Temporizador para el log periódico de distancia
    private float _debugTimer = 0f;

    public int LapsCompleted => _lapsCompleted;
    public int RacePosition => _racePosition;
    public int LastValidatedWaypoint => _lastValidatedWaypoint;

    /// <summary>
    /// Mayor valor = más adelante en la carrera.
    /// Fórmula: (vueltas × totalWaypoints) + últimoWaypointValidado
    /// </summary>
    public float RaceScore => _lapsCompleted * _totalWaypoints + _lastValidatedWaypoint;

    private void Start()
    {
        if (RaceManager.Instance == null)
        {
            Debug.LogError("[CarRaceProgress] RaceManager no encontrado en la escena.", this);
            return;
        }

        RaceManager.Instance.RegisterCar(this);
        _totalWaypoints = RaceManager.Instance.WaypointCount;

        if (debugMode)
            Debug.Log($"[CarRaceProgress] {gameObject.name} registrado — {_totalWaypoints} waypoints | Esperando: WP_01");
    }

    private void Update()
    {
        if (_totalWaypoints == 0 || RaceManager.Instance == null)
            return;

        RacingWaypoint nextWp = RaceManager.Instance.GetWaypoint(_nextExpectedWaypoint);
        if (nextWp == null)
            return;

        float dist = Vector3.Distance(transform.position, nextWp.transform.position);

        // Log periódico cada segundo: muestra distancia actual vs radio necesario
        if (debugMode)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= 1f)
            {
                _debugTimer = 0f;
                Debug.Log($"[CarRaceProgress] {gameObject.name} → WP_{_nextExpectedWaypoint:00} | " +
                          $"Distancia: {dist:F1}m | Radio: {nextWp.DetectionRadius}m | " +
                          $"Score: {RaceScore} | Pos: {_racePosition}");
            }
        }

        if (dist <= nextWp.DetectionRadius)
            ReportWaypoint(_nextExpectedWaypoint);
    }

    private void ReportWaypoint(int index)
    {
        _lastValidatedWaypoint = index;
        _nextExpectedWaypoint = (index + 1) % _totalWaypoints;

        if (debugMode)
            Debug.Log($"[CarRaceProgress] ✓ {gameObject.name} pasó WP_{index:00} | Siguiente: WP_{_nextExpectedWaypoint:00} | Score: {RaceScore}");

        // Cruzar WP_00 después de completar el circuito = vuelta
        if (index == 0)
        {
            _lapsCompleted++;
            RaceManager.Instance.OnCarCompletedLap(this);
        }
    }

    public void SetRacePosition(int position) => _racePosition = position;
}
