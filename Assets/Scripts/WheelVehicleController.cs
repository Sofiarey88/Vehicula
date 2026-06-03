using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a four-wheel vehicle using Unity Wheel Colliders.
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

    private Rigidbody _rigidbody;
    private Vector2 _movementInput;
    private bool _isBraking;

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

    public void RespondToBrakeInput(InputAction.CallbackContext context)
    {
        _isBraking = context.ReadValueAsButton();
    }

    private void ApplyWheelForces()
    {
        float steeringAngle = _movementInput.x * maximumSteeringAngle;
        float currentMotorTorque = _movementInput.y * motorTorque;
        float currentBrakeTorque = _isBraking ? brakeTorque : 0f;

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

