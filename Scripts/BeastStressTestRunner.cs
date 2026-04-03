using System.IO;
using UnityEngine;

/// <summary>
/// Runtime helper for repeatable stress scenarios and log capture.
/// Attach to the vehicle root and invoke StartStressRun from UI/debug menu.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BeastStressTestRunner : MonoBehaviour
{
    [Header("Scenario")]
    public float stressSpeed = 1000000f;
    public Vector3 travelDirection = Vector3.forward;
    public ForceMode launchForceMode = ForceMode.VelocityChange;

    [Header("References")]
    public Rigidbody vehicleBody;
    public TrunkLatchController trunkLatch;

    [Header("Logging")]
    public string logFileName = "StressRun_Log.txt";

    private void Reset()
    {
        vehicleBody = GetComponent<Rigidbody>();
    }

    public void StartStressRun()
    {
        if (vehicleBody == null)
        {
            vehicleBody = GetComponent<Rigidbody>();
        }

        Vector3 dir = travelDirection.sqrMagnitude < 1e-6f ? Vector3.forward : travelDirection.normalized;
        Vector3 targetVelocity = dir * stressSpeed;

        vehicleBody.velocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        vehicleBody.AddForce(targetVelocity, launchForceMode);

        float speed = vehicleBody.velocity.magnitude;
        float tau = trunkLatch != null ? trunkLatch.CalculateAeroTorque(speed) : -1f;

        string logLine = "UTC=" + System.DateTime.UtcNow.ToString("o") +
                         " | targetSpeed=" + stressSpeed.ToString("F1") +
                         " | achievedSpeed=" + speed.ToString("F1") +
                         " | tauAero=" + tau.ToString("F2") +
                         " | fixedDeltaTime=" + Time.fixedDeltaTime.ToString("F4");

        string path = Path.Combine(Application.persistentDataPath, logFileName);
        File.AppendAllText(path, logLine + "\n");

        Debug.Log("Beast stress run started. Log: " + path, this);
    }
}
