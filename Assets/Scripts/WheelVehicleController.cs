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
    [Tooltip("Maximum motor torque applied to drive wheels (N·m).")]
    private float motorTorque = 1500f;

    [SerializeField]
    [Tooltip("Maximum steering angle applied to steer wheels.")]
    private float maximumSteeringAngle = 30f;

    [SerializeField]
    [Tooltip("Base brake torque at low speed (N·m). Scales up with velocity for consistent braking feel.")]
    private float brakeTorque = 2500f;

    [SerializeField]
    [Tooltip("Speed (km/h) at which brake torque scaling begins to increase. Below this speed, brakeTorque is used as-is.")]
    private float brakeScaleReferenceSpeedKmh = 40f;

    [SerializeField]
    [Tooltip("Passive brake torque applied to all wheels when no input is given (engine braking).")]
    private float engineBrakeTorque = 100f;

    [SerializeField]
    [Tooltip("Forward speed (m/s) below which the reverse input switches from braking to reversing.")]
    private float brakeSpeedThreshold = 0.5f;

    [SerializeField]
    [Tooltip("Maximum forward speed in km/h. Motor torque is cut when this speed is reached.")]
    private float maxSpeedKmh = 120f;

    [SerializeField]
    [Tooltip("Maximum reverse speed in km/h.")]
    private float maxReverseSpeedKmh = 40f;

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

    /// <summary>
    /// Returns brake torque scaled by current speed so the braking feel
    /// remains consistent regardless of velocity.
    /// </summary>
    private float GetScaledBrakeTorque(float speedKmh)
    {
        // Avoid division by zero and ensure at least 1x below reference speed
        float referenceSpeed = Mathf.Max(brakeScaleReferenceSpeedKmh, 1f);
        float speedRatio = Mathf.Max(speedKmh / referenceSpeed, 1f);
        return brakeTorque * speedRatio;
    }

    private void ApplyWheelForces()
    {
        float steeringAngle = _movementInput.x * maximumSteeringAngle;

        // Speed along the vehicle's local forward axis (positive = forward, negative = reversing)
        float forwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
        float forwardSpeedKmh = forwardSpeed * 3.6f;

        float currentMotorTorque;
        float currentBrakeTorque;

        if (_movementInput.y > 0f)
        {
            // Cut motor torque when max forward speed is reached
            currentMotorTorque = forwardSpeedKmh >= maxSpeedKmh
                ? 0f
                : _movementInput.y * motorTorque;
            currentBrakeTorque = 0f;
        }
        else if (_movementInput.y < 0f)
        {
            if (forwardSpeed > brakeSpeedThreshold)
            {
                // Still moving forward: apply speed-scaled brakes
                currentMotorTorque = 0f;
                currentBrakeTorque = GetScaledBrakeTorque(forwardSpeedKmh);
            }
            else
            {
                // Stopped or already reversing: apply reverse torque
                currentMotorTorque = (-forwardSpeedKmh) >= maxReverseSpeedKmh
                    ? 0f
                    : _movementInput.y * motorTorque;
                currentBrakeTorque = 0f;
            }
        }
        else
        {
            // No input: engine braking — resists movement passively
            currentMotorTorque = 0f;
            currentBrakeTorque = engineBrakeTorque;
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

