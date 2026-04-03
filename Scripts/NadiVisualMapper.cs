using UnityEngine;

/// <summary>
/// Visual-only "Nadi" mapping layer. No forces are applied here.
/// Maps speed bands to color shift and optional jitter intensity.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class NadiVisualMapper : MonoBehaviour
{
    [Header("References")]
    public Rigidbody vehicleBody;
    public Renderer[] gridRenderers;
    public Light[] headLights;
    public Light[] tailLights;
    public Transform cameraRig;

    [Header("Speed Bands (m/s)")]
    public float redMaxSpeed = 100f;
    public float greenMaxSpeed = 10000f;

    [Header("Visual Targets")]
    public Color redShiftColor = new Color(1f, 0.35f, 0.35f);
    public Color greenShiftColor = new Color(0.35f, 1f, 0.35f);
    public Color violetShiftColor = new Color(0.55f, 0.3f, 1f);

    [Header("Jitter")]
    public float jitterFrequency = 35f;
    public float maxJitterAmplitude = 0.04f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Vector3 cameraLocalStart;

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

        if (cameraRig != null)
        {
            cameraLocalStart = cameraRig.localPosition;
        }
    }

    private void LateUpdate()
    {
        float speed = vehicleBody.velocity.magnitude;
        Color targetColor = SelectBandColor(speed);

        UpdateGridColor(targetColor);
        UpdateLightColor(targetColor, speed);
        UpdateCameraJitter(speed);
    }

    private Color SelectBandColor(float speed)
    {
        if (speed <= redMaxSpeed)
        {
            return redShiftColor;
        }

        if (speed <= greenMaxSpeed)
        {
            return greenShiftColor;
        }

        return violetShiftColor;
    }

    private void UpdateGridColor(Color targetColor)
    {
        foreach (Renderer rend in gridRenderers)
        {
            if (rend == null || rend.material == null)
            {
                continue;
            }

            if (rend.material.HasProperty(BaseColorId))
            {
                rend.material.SetColor(BaseColorId, targetColor);
            }
            else if (rend.material.HasProperty(ColorId))
            {
                rend.material.SetColor(ColorId, targetColor);
            }
        }
    }

    private void UpdateLightColor(Color targetColor, float speed)
    {
        foreach (Light head in headLights)
        {
            if (head == null)
            {
                continue;
            }

            head.color = Color.Lerp(Color.white, targetColor, 0.35f);
            head.intensity = 2f + speed * 0.0001f;
        }

        foreach (Light tail in tailLights)
        {
            if (tail == null)
            {
                continue;
            }

            tail.color = targetColor;
            tail.intensity = speed > greenMaxSpeed ? 7f : 2.5f;
        }
    }

    private void UpdateCameraJitter(float speed)
    {
        if (cameraRig == null)
        {
            return;
        }

        float jitterT = Mathf.Clamp01(speed / Mathf.Max(1f, greenMaxSpeed));
        float amplitude = maxJitterAmplitude * jitterT;
        float phase = Time.time * jitterFrequency;
        Vector3 offset = new Vector3(
            Mathf.Sin(phase) * amplitude,
            Mathf.Sin(phase * 1.37f) * amplitude,
            0f);

        cameraRig.localPosition = cameraLocalStart + offset;
    }
}
