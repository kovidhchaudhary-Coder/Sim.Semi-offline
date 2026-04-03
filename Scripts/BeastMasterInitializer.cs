using System.IO;
using UnityEngine;

/// <summary>
/// Central Beast initializer that applies baseline physics requirements and writes a startup audit log.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BeastMasterInitializer : MonoBehaviour
{
    [Header("Audit Specs")]
    public string build = "0.9.3";
    public float targetMass = 1200f;
    public float centerOfMassY = -0.6f;
    public bool forceFixedTimestep = true;
    public float fixedTimestep = 0.001f;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        if (forceFixedTimestep)
        {
            Time.fixedDeltaTime = fixedTimestep;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        string auditData = "BUILD: " + build +
                           " | MASS: " + targetMass.ToString("F1") +
                           " | CoM: " + centerOfMassY.ToString("F3") +
                           " | TS: " + Time.fixedDeltaTime.ToString("F4");

        string path = Path.Combine(Application.persistentDataPath, "Audit_Log.txt");
        File.WriteAllText(path, auditData);

        Debug.Log("BEAST INITIALIZED: 93% COMPUTER STATE REACHED.", this);
        Debug.Log("Startup audit log: " + path, this);
    }
}
