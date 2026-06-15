using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Configuración de Carrera")]
    [SerializeField]
    [Tooltip("Número de vueltas para completar la carrera.")]
    private int totalLaps = 3;

    [SerializeField]
    [Tooltip("Todos los waypoints de la pista EN ORDEN de recorrido. WP_00 = línea de salida/meta.")]
    private RacingWaypoint[] waypoints;

    private readonly List<CarRaceProgress> _registeredCars = new();
    private readonly List<CarRaceProgress> _sortedPositions = new();
    private bool _raceFinished = false;

    public int TotalLaps => totalLaps;
    public int WaypointCount => waypoints != null ? waypoints.Length : 0;
    public int RegisteredCarCount => _registeredCars.Count;

    /// <summary>
    /// Se dispara UNA SOLA VEZ cuando el primer coche completa todas las vueltas.
    /// El parámetro es el coche que terminó primero.
    /// </summary>
    public event Action<CarRaceProgress> OnRaceFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                waypoints[i].SetIndex(i);
        }
    }

    private void Update()
    {
        UpdateRacePositions();
    }

    public void RegisterCar(CarRaceProgress car)
    {
        if (_registeredCars.Contains(car))
            return;

        _registeredCars.Add(car);
        _sortedPositions.Add(car);
    }

    public RacingWaypoint GetWaypoint(int index)
        => waypoints[index % waypoints.Length];

    public RacingWaypoint GetNextWaypoint(int currentIndex)
        => waypoints[(currentIndex + 1) % waypoints.Length];

    public void OnCarCompletedLap(CarRaceProgress car)
    {
        Debug.Log($"[Carrera] {car.gameObject.name} — Vuelta {car.LapsCompleted}/{totalLaps} | Posición: {car.RacePosition}");

        if (car.LapsCompleted >= totalLaps && !_raceFinished)
        {
            _raceFinished = true;
            Debug.Log($"[Carrera] ¡{car.gameObject.name} terminó primero la carrera!");
            OnRaceFinished?.Invoke(car);
        }
    }

    private void UpdateRacePositions()
    {
        if (_sortedPositions.Count == 0)
            return;

        _sortedPositions.Sort((a, b) => b.RaceScore.CompareTo(a.RaceScore));

        for (int i = 0; i < _sortedPositions.Count; i++)
            _sortedPositions[i].SetRacePosition(i + 1);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            int next = (i + 1) % waypoints.Length;
            if (waypoints[next] == null) continue;
            Gizmos.DrawLine(waypoints[i].transform.position, waypoints[next].transform.position);
        }
    }
#endif
}
