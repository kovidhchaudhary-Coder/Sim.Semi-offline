using UnityEngine;

/// <summary>
/// Aero-torque trunk latch controller with hysteresis and anti-chatter relatch rules.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TrunkLatchController : MonoBehaviour
{
    public enum LatchState
    {
        Closed,
        Open
    }

    [Header("References")]
    public HingeJoint trunkJoint;
    public Rigidbody vehicleBody;

    [Header("Aero Torque Model")]
    public float airDensity = 1.225f;
    public float trunkArea = 0.9f;
    public float dragCoefficient = 1.1f;
    public float leverArm = 0.45f;

    [Header("Hysteresis")]
    public float openThreshold = 1200f;
    public float closeThreshold = 800f;
    public float relatchAngleDeg = 5f;
    public float relatchAngularVelocityDeg = 10f;

    [Header("Joint Targets")]
    public float openTargetDeg = 45f;
    public float closedTargetDeg = 0f;

    [SerializeField]
    private LatchState state = LatchState.Closed;

    private Rigidbody trunkBody;
    private JointLimits defaultLimits;

    public LatchState State => state;

    private void Reset()
    {
        vehicleBody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (vehicleBody == null)
        {
            vehicleBody = GetComponent<Rigidbody>();
        }

        if (trunkJoint == null)
        {
            Debug.LogError("TrunkLatchController requires a HingeJoint reference.", this);
            enabled = false;
            return;
        }

        trunkBody = trunkJoint.GetComponent<Rigidbody>();
        defaultLimits = trunkJoint.limits;
    }

    private void FixedUpdate()
    {
        float speed = vehicleBody.velocity.magnitude;
        float tauAero = CalculateAeroTorque(speed);

        switch (state)
        {
            case LatchState.Closed:
                if (tauAero > openThreshold)
                {
                    UnlockTrunk();
                }
                break;

            case LatchState.Open:
                bool lowTorque = tauAero < closeThreshold;
                bool nearClosedAngle = Mathf.Abs(trunkJoint.angle) <= relatchAngleDeg;
                bool lowAngVel = GetTrunkAngularVelocityDeg() <= relatchAngularVelocityDeg;

                if (lowTorque && nearClosedAngle && lowAngVel)
                {
                    LockTrunk();
                }
                break;
        }
    }

    public float CalculateAeroTorque(float speed)
    {
        return 0.5f * airDensity * speed * speed * trunkArea * dragCoefficient * leverArm;
    }

    private float GetTrunkAngularVelocityDeg()
    {
        if (trunkBody == null)
        {
            return Mathf.Abs(vehicleBody.angularVelocity.magnitude * Mathf.Rad2Deg);
        }

        return Mathf.Abs(trunkBody.angularVelocity.magnitude * Mathf.Rad2Deg);
    }

    private void UnlockTrunk()
    {
        state = LatchState.Open;
        trunkJoint.useLimits = false;
        SetJointTarget(openTargetDeg);
    }

    private void LockTrunk()
    {
        state = LatchState.Closed;
        trunkJoint.limits = defaultLimits;
        trunkJoint.useLimits = true;
        SetJointTarget(closedTargetDeg);
    }

    private void SetJointTarget(float targetDeg)
    {
        JointSpring spring = trunkJoint.spring;
        spring.targetPosition = targetDeg;
        trunkJoint.spring = spring;
        trunkJoint.useSpring = true;
    }
}
