using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a four-wheel vehicle using Unity Wheel Colliders.
/// Pressing the reverse input while moving forward applies brakes.
/// Pressing the reverse input while stopped or reversing applies reverse torque.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class WheelVehicleController : MonoBehaviour
{
    [Serializable]
    private sealed class WheelConfiguration
    {
        [SerializeField]
        [Tooltip("Wheel collider used for physics.")]
        private WheelCollider wheelCollider;

        [SerializeField]
        [Tooltip("Visual wheel transform updated from the wheel collider pose.")]
        private Transform wheelVisual;

        [SerializeField]
        [Tooltip("Whether this wheel receives steering input.")]
        private bool canSteer;

        [SerializeField]
        [Tooltip("Whether this wheel receives motor torque.")]
        private bool canDrive;

        public WheelCollider WheelCollider => wheelCollider;
        public Transform WheelVisual => wheelVisual;
        public bool CanSteer => canSteer;
        public bool CanDrive => canDrive;
    }

    [Header("Wheels")]
    [SerializeField]
    [Tooltip("Configured wheels for physics and visuals.")]
    private WheelConfiguration[] wheelConfigurations;

    [Header("Vehicle")]
    [SerializeField]
    [Tooltip("Optional center of mass transform.")]
    private Transform centerOfMass;

    [SerializeField]
    [Tooltip("Maximum motor torque applied to drive wheels.")]
    private float motorTorque = 1500f;

    [SerializeField]
    [Tooltip("Maximum steering angle applied to steer wheels.")]
    private float maximumSteeringAngle = 30f;

    [SerializeField]
    [Tooltip("Brake torque applied while braking.")]
    private float brakeTorque = 2500f;

    [SerializeField]
    [Tooltip("Forward speed (m/s) below which the reverse input switches from braking to reversing.")]
    private float brakeSpeedThreshold = 0.5f;

    private Rigidbody _rigidbody;
    private Vector2 _movementInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (centerOfMass != null)
        {
            _rigidbody.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
        }
    }

    private void FixedUpdate()
    {
        ApplyWheelForces();
        UpdateWheelVisuals();
    }

    public void RespondToMoveInput(InputAction.CallbackContext context)
    {
        _movementInput = context.ReadValue<Vector2>();
    }

    private void ApplyWheelForces()
    {
        float steeringAngle = _movementInput.x * maximumSteeringAngle;

        // Speed along the vehicle's local forward axis (positive = forward, negative = reversing)
        float forwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);

        float currentMotorTorque;
        float currentBrakeTorque;

        if (_movementInput.y > 0f)
        {
            // Accelerating forward
            currentMotorTorque = _movementInput.y * motorTorque;
            currentBrakeTorque = 0f;
        }
        else if (_movementInput.y < 0f)
        {
            if (forwardSpeed > brakeSpeedThreshold)
            {
                // Still moving forward: apply brakes
                currentMotorTorque = 0f;
                currentBrakeTorque = brakeTorque;
            }
            else
            {
                // Stopped or already reversing: apply reverse torque
                currentMotorTorque = _movementInput.y * motorTorque;
                currentBrakeTorque = 0f;
            }
        }
        else
        {
            // No vertical input: coast freely
            currentMotorTorque = 0f;
            currentBrakeTorque = 0f;
        }

        foreach (WheelConfiguration wheelConfiguration in wheelConfigurations)
        {
            if (wheelConfiguration.WheelCollider == null)
            {
                continue;
            }

            if (wheelConfiguration.CanSteer)
            {
                wheelConfiguration.WheelCollider.steerAngle = steeringAngle;
            }

            if (wheelConfiguration.CanDrive)
            {
                wheelConfiguration.WheelCollider.motorTorque = currentMotorTorque;
            }

            wheelConfiguration.WheelCollider.brakeTorque = currentBrakeTorque;
        }
    }

    private void UpdateWheelVisuals()
    {
        foreach (WheelConfiguration wheelConfiguration in wheelConfigurations)
        {
            if (wheelConfiguration.WheelCollider == null || wheelConfiguration.WheelVisual == null)
            {
                continue;
            }

            wheelConfiguration.WheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheelConfiguration.WheelVisual.SetPositionAndRotation(position, rotation);
        }
    }
}

