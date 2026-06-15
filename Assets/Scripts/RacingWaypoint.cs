using UnityEngine;

/// <summary>
/// Punto de control en la pista de carreras.
/// Provee velocidad objetivo para la IA y radio de detección para CarRaceProgress.
/// La detección se hace por distancia en CarRaceProgress (no requiere BoxCollider).
/// </summary>
public sealed class RacingWaypoint : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Velocidad objetivo para la IA en este punto (km/h). Baja en curvas, alta en rectas.")]
    private float targetSpeedKmh = 80f;

    [SerializeField]
    [Tooltip("Radio de detección (m). Debe cubrir el ancho completo de la pista.")]
    private float detectionRadius = 10f;

    [HideInInspector]
    [SerializeField]
    private int waypointIndex;

    public float TargetSpeedKmh => targetSpeedKmh;
    public float DetectionRadius => detectionRadius;
    public int WaypointIndex => waypointIndex;

    public void SetIndex(int index) => waypointIndex = index;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        bool isFinishLine = waypointIndex == 0;

        Gizmos.color = isFinishLine
            ? new Color(1f, 0.2f, 0.2f, 0.35f)
            : new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        Gizmos.color = isFinishLine ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (detectionRadius + 0.5f),
            $"WP {waypointIndex}  |  {targetSpeedKmh} km/h"
        );
    }
#endif
}
