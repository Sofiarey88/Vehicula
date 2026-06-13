using UnityEngine;

public class RivalCarAI : MonoBehaviour
{
    [Header("Ruta")]
    public Transform[] waypoints;

    [Header("Movimiento")]
    public float speed = 20f;
    public float turnSpeed = 5f;
    public float waypointDistance = 5f;

    private int currentWaypoint = 0;

    private void Update()
    {
        if (waypoints.Length == 0)
            return;

        Transform target = waypoints[currentWaypoint];

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        transform.position += transform.forward * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < waypointDistance)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                currentWaypoint = 0;
            }
        }
    }
}