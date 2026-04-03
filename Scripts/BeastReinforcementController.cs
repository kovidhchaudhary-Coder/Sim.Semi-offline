using UnityEngine;

/// <summary>
/// Dynamic reinforcement layer for collider redundancy, hinge tendon strength,
/// and high-speed solver/inertia hardening.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BeastReinforcementController : MonoBehaviour
{
    [Header("Core Bodies")]
    public Rigidbody chassisBody;
    public Rigidbody hoodBody;
    public Rigidbody trunkBody;

    [Header("Panel Joints")]
    public HingeJoint hoodJoint;
    public HingeJoint trunkJoint;
    public float closedTargetAngle = 0f;

    [Header("Composite Collider Reinforcement")]
    [Tooltip("Minimum primitive collider count expected on this root/chassis object.")]
    public int minPrimitiveColliders = 3;
    public bool logColliderWarnings = true;

    [Header("Joint Reinforcement")]
    public float baseSpring = 5000f;
    public float baseDamper = 10000f;
    public float maxSpring = 30000f;
    public float maxDamper = 60000f;
    public float velocityToJointScale = 0.000002f;

    [Header("Inertia Tensor Reinforcement")]
    public Vector3 inertiaScale = new Vector3(1.35f, 1.2f, 1.35f);

    [Header("Dynamic Hardening")]
    public float hardenSpeedThreshold = 500000f;
    public int normalSolverIterations = 16;
    public int hardenedSolverIterations = 32;
    public int normalSolverVelocityIterations = 8;
    public int hardenedSolverVelocityIterations = 16;

    private Vector3 baseInertiaTensor;

    private void Reset()
    {
        chassisBody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (chassisBody == null)
        {
            chassisBody = GetComponent<Rigidbody>();
        }

        baseInertiaTensor = chassisBody.inertiaTensor;

        ApplyInertiaReinforcement();
        ApplySolverMode(false);

        if (logColliderWarnings)
        {
            ValidateCompositeColliderCoverage();
        }
    }

    private void FixedUpdate()
    {
        float speed = chassisBody.velocity.magnitude;
        bool hardened = speed > hardenSpeedThreshold;

        ApplySolverMode(hardened);
        ReinforceJoint(hoodJoint, speed, hardened);
        ReinforceJoint(trunkJoint, speed, hardened);
    }

    public void ApplyInertiaReinforcement()
    {
        chassisBody.inertiaTensor = Vector3.Scale(baseInertiaTensor, inertiaScale);
    }

    private void ValidateCompositeColliderCoverage()
    {
        Collider[] colliders = GetComponents<Collider>();
        int primitiveCount = 0;

        foreach (Collider c in colliders)
        {
            if (c is BoxCollider || c is CapsuleCollider || c is SphereCollider)
            {
                primitiveCount++;
            }
        }

        if (primitiveCount < minPrimitiveColliders)
        {
            Debug.LogWarning(
                "AUDIT: Composite collider reinforcement is weak. Found " + primitiveCount +
                " primitive colliders, expected >= " + minPrimitiveColliders + ".",
                this);
        }
    }

    private void ReinforceJoint(HingeJoint joint, float speed, bool hardened)
    {
        if (joint == null)
        {
            return;
        }

        float speedScale = 1f + speed * velocityToJointScale;
        if (hardened)
        {
            speedScale *= 2f;
        }

        JointSpring spring = joint.spring;
        spring.spring = Mathf.Min(maxSpring, baseSpring * speedScale);
        spring.damper = Mathf.Min(maxDamper, baseDamper * speedScale);
        spring.targetPosition = closedTargetAngle;

        joint.spring = spring;
        joint.useSpring = true;
    }

    private void ApplySolverMode(bool hardened)
    {
        int solverIterations = hardened ? hardenedSolverIterations : normalSolverIterations;
        int solverVelocityIterations = hardened ? hardenedSolverVelocityIterations : normalSolverVelocityIterations;

        SetBodySolverIterations(chassisBody, solverIterations, solverVelocityIterations);
        SetBodySolverIterations(hoodBody, solverIterations, solverVelocityIterations);
        SetBodySolverIterations(trunkBody, solverIterations, solverVelocityIterations);
    }

    private static void SetBodySolverIterations(Rigidbody body, int solverIterations, int solverVelocityIterations)
    {
        if (body == null)
        {
            return;
        }

        body.solverIterations = solverIterations;
        body.solverVelocityIterations = solverVelocityIterations;
    }
}
