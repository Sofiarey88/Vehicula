using UnityEngine;

/// <summary>
/// Rastrea el progreso de un auto en la carrera mediante detección por distancia.
/// Se coloca en TODOS los autos (player + rivales). No depende de colliders ni triggers.
///
/// Lógica de vueltas:
/// - Los autos arrancan EN WP_00, por lo que el primer waypoint a cruzar es WP_01.
/// - Cruzar WP_00 después de recorrer toda la pista = vuelta completada.
/// - Anti-trampa: solo el siguiente waypoint esperado en orden es válido.
///
/// Cálculo de posición (RaceScore):
/// - Es un valor CONTINUO, no escalonado: además de (vueltas, último waypoint validado),
///   suma una fracción de progreso [0..1) hacia el próximo waypoint.
/// - Esto evita empates entre autos que están en el mismo tramo de pista
///   (ej. dos autos que todavía no pasaron ningún waypoint al arrancar),
///   que con un score puramente entero quedaban ordenados de forma inestable/aleatoria.
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

    // Distancia horizontal (XZ) actual hacia el próximo waypoint esperado.
    private float _distanceToNext = 0f;

    // Temporizador para el log periódico de distancia
    private float _debugTimer = 0f;

    public int LapsCompleted => _lapsCompleted;
    public int RacePosition => _racePosition;
    public int LastValidatedWaypoint => _lastValidatedWaypoint;

    /// <summary>
    /// Mayor valor = más adelante en la carrera.
    /// Fórmula: (vueltas × totalWaypoints) + últimoWaypointValidado + progresoFraccional
    /// El progreso fraccional [0..1) es 0 justo al validar un waypoint y se acerca a 1
    /// a medida que el auto se aproxima al próximo, evitando empates de score.
    /// </summary>
    public float RaceScore
    {
        get
        {
            float progress = 0f;

            if (RaceManager.Instance != null && _totalWaypoints > 0)
            {
                float segmentLength = RaceManager.Instance.GetSegmentLength(_lastValidatedWaypoint);

                if (segmentLength > 0.01f)
                    progress = Mathf.Clamp01(1f - (_distanceToNext / segmentLength));
            }

            return _lapsCompleted * _totalWaypoints + _lastValidatedWaypoint + progress;
        }
    }

    private void Start()
    {
        if (RaceManager.Instance == null)
        {
            Debug.LogError("[CarRaceProgress] RaceManager no encontrado en la escena.", this);
            return;
        }

        RaceManager.Instance.RegisterCar(this);
        _totalWaypoints = RaceManager.Instance.WaypointCount;

        // Inicializar la distancia hacia el primer waypoint esperado para que el
        // RaceScore sea correcto desde el primer frame (evita el desorden inicial).
        UpdateDistanceToNext();

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

        UpdateDistanceToNext();

        // Log periódico cada segundo: muestra distancia actual vs radio necesario
        if (debugMode)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= 1f)
            {
                _debugTimer = 0f;
                Debug.Log($"[CarRaceProgress] {gameObject.name} → WP_{_nextExpectedWaypoint:00} | " +
                          $"Distancia: {_distanceToNext:F1}m | Radio: {nextWp.DetectionRadius}m | " +
                          $"Score: {RaceScore:F2} | Pos: {_racePosition}");
            }
        }

        if (_distanceToNext <= nextWp.DetectionRadius)
            ReportWaypoint(_nextExpectedWaypoint);
    }

    /// <summary>
    /// Actualiza _distanceToNext usando distancia horizontal (XZ), ignorando la altura.
    /// Así el radio de detección representa el "ancho de pista" sin que los
    /// desniveles del terreno (puentes, rampas) impidan la detección.
    /// </summary>
    private void UpdateDistanceToNext()
    {
        if (RaceManager.Instance == null || _totalWaypoints == 0)
            return;

        RacingWaypoint nextWp = RaceManager.Instance.GetWaypoint(_nextExpectedWaypoint);
        if (nextWp == null)
            return;

        Vector3 a = transform.position;
        Vector3 b = nextWp.transform.position;
        a.y = 0f;
        b.y = 0f;

        _distanceToNext = Vector3.Distance(a, b);
    }

    private void ReportWaypoint(int index)
    {
        _lastValidatedWaypoint = index;
        _nextExpectedWaypoint = (index + 1) % _totalWaypoints;

        // Recalcular inmediatamente la distancia al nuevo waypoint esperado para
        // que el RaceScore no quede "viejo" durante un frame.
        UpdateDistanceToNext();

        if (debugMode)
            Debug.Log($"[CarRaceProgress] ✓ {gameObject.name} pasó WP_{index:00} | Siguiente: WP_{_nextExpectedWaypoint:00} | Score: {RaceScore:F2}");

        // Cruzar WP_00 después de completar el circuito = vuelta
        if (index == 0)
        {
            _lapsCompleted++;
            RaceManager.Instance.OnCarCompletedLap(this);
        }
    }

    public void SetRacePosition(int position) => _racePosition = position;
}