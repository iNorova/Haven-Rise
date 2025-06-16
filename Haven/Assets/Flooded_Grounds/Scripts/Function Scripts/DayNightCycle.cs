using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Duration of a full day cycle in real-world minutes.")]
    public float dayDurationInMinutes = 10f;
    [Range(0, 24)]
    [Tooltip("Current time of day in hours (0 = midnight, 12 = noon, 24 = next midnight).")]
    public float timeOfDay = 12f; // Start at noon

    [Header("Lighting References")]
    [Tooltip("Assign your main Directional Light that acts as the sun/moon.")]
    public Light sunLight;

    [Header("Sky & Light Properties")]
    [Tooltip("Color of the sun light over the 24-hour cycle.")]
    public Gradient sunColor;
    [Tooltip("Intensity of the sun light over the 24-hour cycle.")]
    public AnimationCurve sunIntensity;
    [Tooltip("Color of the ambient light over the 24-hour cycle.")]
    public Gradient ambientColor;
    [Tooltip("Color of the fog over the 24-hour cycle.")]
    public Gradient fogColor;

    private float _timeScale;

    void Start()
    {
        // Calculate time scale: (24 hours / dayDurationInMinutes) * minutes_in_hour (60) for a full day
        _timeScale = 24f / dayDurationInMinutes; // Now represents hours per minute
        UpdateEnvironment(); // Initial update
    }

    void Update()
    {
        // Advance time of day
        timeOfDay += Time.deltaTime * _timeScale; // timeOfDay is in hours
        if (timeOfDay >= 24f) // If we pass midnight, loop back
        {
            timeOfDay -= 24f;
        }

        UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        // Normalize time to a 0-1 range for gradients and curves
        float normalizedTime = timeOfDay / 24f;

        // Update Sun Light (Directional Light)
        if (sunLight != null)
        {
            // Rotate the sun light. 0 degrees for noon, -90 for sunrise, 90 for sunset.
            // A full 24-hour cycle is 360 degrees rotation around the X-axis for the light.
            // 0 (midnight) -> 0 degrees
            // 6 (sunrise)   -> -90 degrees
            // 12 (noon)     -> 0 degrees
            // 18 (sunset)   -> 90 degrees
            // This logic assumes the sunLight starts facing straight down at noon (x=0, y=0, z=0 rotation).
            // We need it to rotate from -90 (morning) through 0 (noon) to 90 (evening) relative to its initial x-axis.
            // A simpler way is to rotate it from 0 to 360 over the day.
            // At 6 AM (0.25 normalized) it should be rising (e.g., -90 degrees X rotation).
            // At 12 PM (0.5 normalized) it should be at its peak (e.g., 0 degrees X rotation).
            // At 18 PM (0.75 normalized) it should be setting (e.g., 90 degrees X rotation).
            // At 0 AM (0 or 1 normalized) it should be at its lowest point (e.g., 180 degrees X rotation for moon/night).
            
            // Let's make it simpler: rotate 360 degrees over 24 hours.
            // At 0 hours, let's say sun is at -90 degrees (just before sunrise).
            // At 12 hours, sun is at 90 degrees (just after sunset).
            // A 360-degree rotation maps 0-1 normalized time to 0-360 degrees.
            // For sun, typical approach is: start from sun below horizon, rise, set, then go back below horizon.
            // If 0-1 normalized time maps to 0-360 degrees, 0.25 (6am) is 90 deg, 0.5 (12pm) is 180 deg, 0.75 (6pm) is 270 deg.
            // To get a natural looking sun/moon cycle, the directional light should rotate around the X-axis.
            // 0 degrees for light means pointing straight down. So at noon, it should be pointing down.
            // At 6 AM, it should be at -90 degrees (coming from right side if light is global x-rot).
            // At 18 PM, it should be at 90 degrees (going to left side).

            // Let's consider 0 degrees X rotation as midday sun.
            // So, -90 for 6am, +90 for 6pm. Full rotation 0-360 means:
            // Normalized time 0: (midnight, sun lowest) = 0 degrees (pointing up)
            // Normalized time 0.25 (6am): = 90 degrees (horizontal from right)
            // Normalized time 0.5 (12pm): = 180 degrees (pointing down, sun at peak)
            // Normalized time 0.75 (6pm): = 270 degrees (horizontal from left)

            // This is simplest: sun rotation from -90 to 270 degrees. This will move it from morning to night.
            float xRotation = Mathf.Lerp(-90f, 270f, normalizedTime); // Rotates 360 degrees. -90 is rising, 270 is after setting.
            sunLight.transform.localRotation = Quaternion.Euler(xRotation, sunLight.transform.localEulerAngles.y, sunLight.transform.localEulerAngles.z);

            sunLight.color = sunColor.Evaluate(normalizedTime);
            sunLight.intensity = sunIntensity.Evaluate(normalizedTime);
        }

        // Update Ambient Light and Fog
        RenderSettings.ambientLight = ambientColor.Evaluate(normalizedTime);
        RenderSettings.fogColor = fogColor.Evaluate(normalizedTime);
    }
} 