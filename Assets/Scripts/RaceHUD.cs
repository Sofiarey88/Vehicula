using TMPro;
using UnityEngine;

/// <summary>
/// Actualiza los textos de UI de posición y vueltas con los datos del jugador.
/// Asignar este componente en un GameObject de la escena (ej. el Canvas del HUD).
/// </summary>
public sealed class RaceHUD : MonoBehaviour
{
    [Header("Referencia al jugador")]
    [SerializeField]
    [Tooltip("Componente CarRaceProgress del auto del jugador.")]
    private CarRaceProgress playerProgress;

    [Header("Textos de UI")]
    [SerializeField]
    [Tooltip("Texto que muestra la posición actual. Ej: '1 / 4'")]
    private TextMeshProUGUI textoPos;

    [SerializeField]
    [Tooltip("Texto que muestra las vueltas. Ej: 'Vuelta 2 / 3'")]
    private TextMeshProUGUI textoVueltas;

    private int _totalCars = 0;

    private void Start()
    {
        if (playerProgress == null)
            Debug.LogError("[RaceHUD] No se asignó el CarRaceProgress del jugador.", this);

        if (textoPos == null || textoVueltas == null)
            Debug.LogError("[RaceHUD] Faltan referencias a los TextMeshProUGUI.", this);
    }

    private void Update()
    {
        if (playerProgress == null || RaceManager.Instance == null)
            return;

        // Refrescar el total de coches registrados dinámicamente
        _totalCars = RaceManager.Instance.RegisteredCarCount;

        // Posición: "1 / 4"
        textoPos.text = $"{playerProgress.RacePosition} / {_totalCars}";

        // Vueltas: "Vuelta 2 / 3"
        int vueltaActual = Mathf.Min(playerProgress.LapsCompleted + 1, RaceManager.Instance.TotalLaps);
        textoVueltas.text = $"Vuelta {vueltaActual} / {RaceManager.Instance.TotalLaps}";
    }
}