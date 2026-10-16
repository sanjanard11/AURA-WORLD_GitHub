using UnityEngine;

public class WindResponsiveTree : MonoBehaviour
{
    [Header("Tree Canopy Elements")]
    public Transform foliageLower;
    public Transform foliageUpper;

    [Header("Sway Configuration")]
    [Tooltip("Base sway angle in degrees under gentle breeze")]
    public float baseSwayAngle = 1.2f;
    [Tooltip("Maximum sway angle in degrees during storm")]
    public float maxSwayAngle = 8.5f;
    [Tooltip("Base sway frequency (speed of oscillation)")]
    public float baseFrequency = 1.5f;

    [Header("Trunk Stability")]
    public bool keepTrunkStatic = true;

    private Quaternion initialLowerRot;
    private Quaternion initialUpperRot;
    private float phaseOffset;
    private float windGustTime;

    private void Awake()
    {
        // Auto-discover Foliage transforms if unassigned
        if (foliageLower == null)
        {
            Transform found = transform.Find("FoliageLower_1");
            if (found == null) found = transform.Find("FoliageLower_2");
            if (found == null) found = transform.Find("FoliageLower_3");
            if (found == null) found = transform.Find("FoliageLower_4");
            if (found == null) found = transform.Find("FoliageLower_5");
            if (found == null) found = transform.Find("FoliageLower_6");
            if (found == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("lower"))
                    {
                        found = child;
                        break;
                    }
                }
            }
            foliageLower = found;
        }

        if (foliageUpper == null)
        {
            Transform found = transform.Find("FoliageUpper_1");
            if (found == null) found = transform.Find("FoliageUpper_2");
            if (found == null) found = transform.Find("FoliageUpper_3");
            if (found == null) found = transform.Find("FoliageUpper_4");
            if (found == null) found = transform.Find("FoliageUpper_5");
            if (found == null) found = transform.Find("FoliageUpper_6");
            if (found == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("upper"))
                    {
                        found = child;
                        break;
                    }
                }
            }
            foliageUpper = found;
        }

        if (foliageLower != null) initialLowerRot = foliageLower.localRotation;
        if (foliageUpper != null) initialUpperRot = foliageUpper.localRotation;

        // Position-based pseudorandom phase offset so trees don't sway synchronously
        phaseOffset = (transform.position.x * 3.17f + transform.position.z * 5.41f) % (Mathf.PI * 2f);
    }

    private void Update()
    {
        float windSpeed = 1.0f;
        bool isStorm = false;

        if (WeatherManager.Instance != null)
        {
            windSpeed = WeatherManager.Instance.CurrentWindSpeed;
            isStorm = WeatherManager.Instance.IsThunderstorm;
        }

        // Calculate dynamic sway parameters
        // Normal wind: 0-10 m/s; Storms: 15-30 m/s
        float normWind = Mathf.Clamp(windSpeed / 12f, 0.15f, 2.0f);
        if (isStorm) normWind *= 1.4f;

        float currentSwayLimit = Mathf.Lerp(baseSwayAngle, maxSwayAngle, Mathf.Clamp01(normWind * 0.5f));
        float currentFreq = baseFrequency * Mathf.Lerp(0.8f, 1.8f, Mathf.Clamp01(normWind * 0.4f));

        windGustTime += Time.deltaTime * currentFreq;

        // Organic multi-harmonic sway formula
        float t = windGustTime + phaseOffset;
        float primaryWave = Mathf.Sin(t);
        float secondaryWave = Mathf.Sin(t * 1.73f + 1.2f) * 0.35f;
        float gustEnvelope = 0.85f + 0.15f * Mathf.Sin(t * 0.31f);

        float totalSway = (primaryWave + secondaryWave) * gustEnvelope * currentSwayLimit;

        // Apply sway to foliage while keeping trunk firmly stable
        if (foliageLower != null)
        {
            // Lower canopy sways moderately along Z and X
            Quaternion lowerSway = Quaternion.Euler(totalSway * 0.7f, 0f, totalSway * 0.85f);
            foliageLower.localRotation = initialLowerRot * lowerSway;
        }

        if (foliageUpper != null)
        {
            // Upper canopy sways with slightly higher amplitude and phase lag
            float upperWave = Mathf.Sin(t - 0.35f) * currentSwayLimit * 1.15f;
            Quaternion upperSway = Quaternion.Euler(upperWave * 0.8f, 0f, upperWave * 1.0f);
            foliageUpper.localRotation = initialUpperRot * upperSway;
        }
    }
}
