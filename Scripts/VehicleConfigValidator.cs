using UnityEngine;

/// <summary>
/// Validates and optionally enforces core vehicle physics setup at boot.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class VehicleConfigValidator : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Expected fixed timestep in seconds for the Beast rig.")]
    public float targetFixedDeltaTime = 0.001f;

    [Tooltip("Maximum allowed fixed timestep (boot fails above this value).")]
    public float maxFixedDeltaTime = 0.002f;

    [Tooltip("Hard-coded center of mass used by the architecture.")]
    public Vector3 requiredCenterOfMass = new Vector3(0f, -0.6f, 0f);

    [Tooltip("Required collision mode for high-speed stability.")]
    public CollisionDetectionMode requiredCollisionMode = CollisionDetectionMode.ContinuousSpeculative;

    [Header("Behavior")]
    [Tooltip("When true, apply requiredCenterOfMass and collision mode automatically before validation.")]
    public bool autoApplyRequiredSettings = true;

    [Tooltip("If true, configuration violations stop play mode in editor and quit in builds.")]
    public bool hardFail = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (autoApplyRequiredSettings)
        {
            rb.centerOfMass = requiredCenterOfMass;
            rb.collisionDetectionMode = requiredCollisionMode;
        }

        ValidateFixedTimeStep();
        ValidateCenterOfMass();
        ValidateCollisionMode();
    }

    private void ValidateFixedTimeStep()
    {
        if (Time.fixedDeltaTime > maxFixedDeltaTime)
        {
            Fail($"FATAL: Time.fixedDeltaTime={Time.fixedDeltaTime:F4}s exceeds {maxFixedDeltaTime:F4}s.");
            return;
        }

        if (!Mathf.Approximately(Time.fixedDeltaTime, targetFixedDeltaTime))
        {
            Debug.LogWarning(
                $"AUDIT: Time.fixedDeltaTime={Time.fixedDeltaTime:F4}s, target is {targetFixedDeltaTime:F4}s.",
                this);
        }
    }

    private void ValidateCenterOfMass()
    {
        if ((rb.centerOfMass - requiredCenterOfMass).sqrMagnitude > 1e-6f)
        {
            Fail($"FATAL: centerOfMass={rb.centerOfMass} expected {requiredCenterOfMass}.");
        }
    }

    private void ValidateCollisionMode()
    {
        if (rb.collisionDetectionMode != requiredCollisionMode)
        {
            Fail($"FATAL: Collision mode {rb.collisionDetectionMode} does not match required {requiredCollisionMode}.");
        }
    }

    private void Fail(string message)
    {
        Debug.LogError(message, this);

        if (!hardFail)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
