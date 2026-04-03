using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class AuditTestResult
{
    public string name;
    public bool pass;
    public float value;
    public string target;
}

[Serializable]
public class AuditReport
{
    public string build = "beast-0.9.3";
    public string gitCommit = "unknown";
    public string timestampUtc;
    public float fixedTimeStep;
    public int rigidbodyCount;
    public float calculatedMass;
    public bool overallPass;
    public List<AuditTestResult> tests = new List<AuditTestResult>();
}

/// <summary>
/// Runs deterministic, startup-safe physics audits and exports an inspection JSON report.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PhysicsAuditHarness : MonoBehaviour
{
    [Header("Targets")]
    public float targetMass = 1200f;
    [Range(0.001f, 0.2f)]
    public float massToleranceFraction = 0.01f;
    public float requiredCenterOfMassY = -0.6f;
    public float centerOfMassTolerance = 0.05f;
    public float requiredFixedDeltaTime = 0.001f;
    public float maxFixedDeltaTime = 0.002f;

    [Header("Component Checks")]
    public bool requireContinuousSpeculative = true;
    public TrunkLatchController trunkLatchController;
    public BeastReinforcementController reinforcementController;
    public float expectedOpenThreshold = 1200f;
    public float expectedCloseThreshold = 800f;
    public int expectedMinPrimitiveColliders = 3;

    [Header("Report")]
    public string reportFileName = "AuditReport.json";
    public string buildId = "beast-0.9.3";
    public string gitCommit = "unknown";

    [Header("Runtime")]
    public bool runOnStart = true;

    private Rigidbody[] bodies;
    private Rigidbody rootRb;

    private void Awake()
    {
        bodies = GetComponentsInChildren<Rigidbody>(true);
        rootRb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (runOnStart)
        {
            RunFullAudit();
        }
    }

    public void RunFullAudit()
    {
        AuditReport report = new AuditReport();
        report.build = buildId;
        report.gitCommit = gitCommit;
        report.timestampUtc = DateTime.UtcNow.ToString("o");
        report.fixedTimeStep = Time.fixedDeltaTime;
        report.rigidbodyCount = bodies.Length;

        float totalMass = bodies.Sum(r => r.mass);
        report.calculatedMass = totalMass;

        float massTolerance = targetMass * massToleranceFraction;
        AddTest(report, "CompoundMass", totalMass, targetMass + "±" + massTolerance, Mathf.Abs(totalMass - targetMass) <= massTolerance);

        float comY = rootRb.centerOfMass.y;
        bool comPass = Mathf.Abs(comY - requiredCenterOfMassY) <= centerOfMassTolerance;
        AddTest(report, "CenterOfMassY", comY, requiredCenterOfMassY + "±" + centerOfMassTolerance, comPass);

        bool fixedWithinMax = Time.fixedDeltaTime <= maxFixedDeltaTime;
        AddTest(report, "BootFixedTimestepLimit", Time.fixedDeltaTime, "<= " + maxFixedDeltaTime, fixedWithinMax);

        bool fixedOnTarget = Mathf.Abs(Time.fixedDeltaTime - requiredFixedDeltaTime) <= 1e-6f;
        AddTest(report, "SolverFrequencyTarget", Time.fixedDeltaTime, requiredFixedDeltaTime.ToString("F4"), fixedOnTarget);

        bool ccdPass = !requireContinuousSpeculative || rootRb.collisionDetectionMode == CollisionDetectionMode.ContinuousSpeculative;
        AddTest(report, "CCDMode", (float)rootRb.collisionDetectionMode, CollisionDetectionMode.ContinuousSpeculative.ToString(), ccdPass);

        if (trunkLatchController != null)
        {
            bool openThresholdPass = Mathf.Abs(trunkLatchController.openThreshold - expectedOpenThreshold) <= 0.001f;
            AddTest(report, "TrunkOpenThreshold", trunkLatchController.openThreshold, expectedOpenThreshold.ToString("F1"), openThresholdPass);

            bool closeThresholdPass = Mathf.Abs(trunkLatchController.closeThreshold - expectedCloseThreshold) <= 0.001f;
            AddTest(report, "TrunkCloseThreshold", trunkLatchController.closeThreshold, expectedCloseThreshold.ToString("F1"), closeThresholdPass);
        }

        int primitiveColliderCount = CountPrimitiveCollidersOnRoot();
        bool colliderPass = primitiveColliderCount >= expectedMinPrimitiveColliders;
        AddTest(report, "CompositeColliderLayers", primitiveColliderCount, ">= " + expectedMinPrimitiveColliders, colliderPass);

        if (reinforcementController != null)
        {
            float ix = rootRb.inertiaTensor.x;
            bool inertiaPass = ix > 0f;
            AddTest(report, "InertiaTensorX", ix, "> 0", inertiaPass);
        }

        report.overallPass = report.tests.All(t => t.pass);

        string reportPath = Path.Combine(Application.persistentDataPath, reportFileName);
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText(reportPath, json);

        Debug.Log("Physics audit exported: " + reportPath, this);
        Debug.Log("Physics audit overall pass: " + report.overallPass, this);
    }

    private int CountPrimitiveCollidersOnRoot()
    {
        Collider[] colliders = GetComponents<Collider>();
        int count = 0;

        foreach (Collider c in colliders)
        {
            if (c is BoxCollider || c is CapsuleCollider || c is SphereCollider)
            {
                count++;
            }
        }

        return count;
    }

    private static void AddTest(AuditReport report, string name, float value, string target, bool pass)
    {
        AuditTestResult result = new AuditTestResult();
        result.name = name;
        result.value = value;
        result.target = target;
        result.pass = pass;
        report.tests.Add(result);
    }
}
