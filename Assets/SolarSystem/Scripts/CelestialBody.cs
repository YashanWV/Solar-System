using System;
using UnityEngine;

/// <summary>Deterministic educational Kepler orbit, with independently exaggerated visual size.</summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public sealed class CelestialBody : MonoBehaviour
{
    public static double SimulationDays;
    // Preserve relative spin periods while slowing visual rotation for readable surfaces.
    public const double VisualSpinScale = 0.025;

    public string displayName;
    [TextArea(2, 5)] public string description;
    [Min(0.001f)] public float radius = 1f;
    public double trueRadiusKm;
    [Min(0)] public float distanceAU = 1f;
    [Min(0)] public float orbitRadius = 10f;
    [Min(0)] public float orbitalPeriodDays = 365.256f;
    [Range(0, 0.95f)] public float eccentricity = 0.0167f;
    public float inclination;
    public float axialTilt = 23.44f;
    [Tooltip("Sidereal rotation in hours. Negative values represent retrograde rotation.")]
    public float rotationHours = 23.9345f;
    public float phaseDegrees;
    public float ascendingNode;
    public Transform orbitCenter;
    [Tooltip("A child containing the visible sphere and any equatorial rings.")]
    public Transform surface;
    public Color accent = Color.white;

    void OnEnable() => Evaluate(Application.isPlaying ? SimulationDays : 0.0);
    void Update() => Evaluate(Application.isPlaying ? SimulationDays : 0.0);

    public void Evaluate(double days)
    {
        if (double.IsNaN(days) || double.IsInfinity(days)) days = 0.0;
        if (orbitRadius > 0f)
        {
            double fraction = orbitalPeriodDays > 0f ? (days / orbitalPeriodDays) % 1.0 : 0.0;
            transform.position = OrbitPoint((float)(phaseDegrees + fraction * 360.0));
        }

        if (surface != null)
        {
            double spinScale = orbitCenter != null && orbitCenter.GetComponent<CelestialBody>() != null && orbitCenter.GetComponent<CelestialBody>().displayName != "Sun" ? 1.0 : VisualSpinScale;
            double turns = Math.Abs(rotationHours) > 0.0001f ? (days * 24.0 * spinScale / rotationHours) % 1.0 : 0.0;
            // A signed period already encodes retrograde rotation. Using a >90 degree
            // pole as well would reverse it twice; the opposite pole has the same equator.
            float tilt = rotationHours < 0f && axialTilt > 90f ? axialTilt - 180f : axialTilt;
            // Negative Unity yaw follows the +X to +Z orbital direction above.
            surface.localRotation = Quaternion.AngleAxis(tilt, Vector3.forward)
                * Quaternion.AngleAxis((float)(-turns * 360.0), Vector3.up);
        }
    }

    /// <summary>World position at a mean anomaly, in degrees. The attractor occupies an ellipse focus.</summary>
    public Vector3 OrbitPoint(float meanAnomalyDegrees)
    {
        double e = Mathf.Clamp(eccentricity, 0f, 0.95f);
        double mean = (meanAnomalyDegrees % 360.0) * Math.PI / 180.0;
        if (mean > Math.PI) mean -= 2.0 * Math.PI;
        if (mean < -Math.PI) mean += 2.0 * Math.PI;
        double anomaly = e < 0.8 ? mean : (mean < 0 ? -Math.PI : Math.PI);
        for (int i = 0; i < 12; i++)
        {
            double correction = (anomaly - e * Math.Sin(anomaly) - mean)
                / (1.0 - e * Math.Cos(anomaly));
            anomaly -= correction;
            if (Math.Abs(correction) < 1e-10) break;
        }
        float a = Mathf.Max(0f, orbitRadius);
        Vector3 point = new Vector3((float)(a * (Math.Cos(anomaly) - e)), 0f,
            (float)(a * Math.Sqrt(1.0 - e * e) * Math.Sin(anomaly)));
        Quaternion plane = Quaternion.AngleAxis(ascendingNode, Vector3.up)
            * Quaternion.AngleAxis(inclination, Vector3.right);
        return (orbitCenter != null ? orbitCenter.position : Vector3.zero) + plane * point;
    }
}
