using UnityEngine;

/// <summary>
/// Muestra el panel de Victoria o Derrota al terminar la carrera.
/// Asignar en un GameObject del Canvas. 
/// </summary>
public sealed class RaceResultUI : MonoBehaviour
{
    [Header("Referencia al jugador")]
    [SerializeField]
    [Tooltip("Componente CarRaceProgress del auto del jugador.")]
    private CarRaceProgress playerProgress;

    [Header("Paneles de resultado")]
    [SerializeField]
    [Tooltip("Panel que se activa cuando el jugador termina en 1.ª posición.")]
    private GameObject panelVictoria;

    [SerializeField]
    [Tooltip("Panel que se activa cuando el jugador NO termina en 1.ª posición.")]
    private GameObject panelDerrota;

    private void Start()
    {
        if (playerProgress == null)
            Debug.LogError("[RaceResultUI] No se asignó el CarRaceProgress del jugador.", this);

        if (panelVictoria == null || panelDerrota == null)
            Debug.LogError("[RaceResultUI] Faltan referencias a los paneles de resultado.", this);

        // Ocultar ambos paneles al inicio
        panelVictoria?.SetActive(false);
        panelDerrota?.SetActive(false);

        if (RaceManager.Instance != null)
            RaceManager.Instance.OnRaceFinished += HandleRaceFinished;
        else
            Debug.LogError("[RaceResultUI] RaceManager no encontrado en escena.", this);
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (RaceManager.Instance != null)
            RaceManager.Instance.OnRaceFinished -= HandleRaceFinished;
    }

    private void HandleRaceFinished(CarRaceProgress winner)
    {
        bool playerWon = winner == playerProgress;

        panelVictoria.SetActive(playerWon);
        panelDerrota.SetActive(!playerWon);

        // Pausar el juego al mostrar el resultado
        Time.timeScale = 0f;

        Debug.Log($"[RaceResultUI] Resultado: {(playerWon ? "VICTORIA" : "DERROTA")} | Ganador: {winner.gameObject.name}");
    }
}