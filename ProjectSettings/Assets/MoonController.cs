using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MoonController : MonoBehaviour
{
    [Header("Moon Surface & Visuals")]
    [Tooltip("Optional custom lunar surface texture; if null, a procedural cratered texture is generated.")]
    public Texture2D customMoonTexture;
    public Material moonMaterial;
    public Color moonBaseColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    public Color moonGlowColor = new Color(0.70f, 0.78f, 0.95f, 1f);
    public float baseGlowIntensity = 0.35f;

    [Header("Moonlight")]
    public Light moonlight;
    public Color moonlightColor = new Color(0.68f, 0.78f, 0.98f, 1f);
    public float maxMoonlightIntensity = 0.45f;

    [Header("Day & Night Blending")]
    [Range(0f, 1f)]
    public float nightVisibilityFactor = 1f; // 1 = full night, 0 = bright day
    public float dayAlpha = 0.08f;           // Faint trace in daytime sky
    public float nightAlpha = 1.0f;
    public float transitionSpeed = 2.0f;

    [Header("Placement & Size")]
    public Vector3 skyPosition = new Vector3(-8f, 17f, 32f);
    public float moonScale = 3.2f;

    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    private Texture2D generatedTexture;
    private float currentVisibility = 1f;
    private float targetVisibility = 1f;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        transform.position = skyPosition;
        transform.localScale = Vector3.one * moonScale;

        // Create runtime material instance so we don't modify the asset file on disk
        if (moonMaterial != null)
        {
            runtimeMaterial = new Material(moonMaterial);
        }
        else if (meshRenderer.sharedMaterial != null)
        {
            runtimeMaterial = new Material(meshRenderer.sharedMaterial);
        }
        else
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            runtimeMaterial = new Material(litShader);
        }

        meshRenderer.material = runtimeMaterial;

        // Setup texture: use custom if provided, otherwise procedural lunar surface
        if (customMoonTexture != null)
        {
            SetTexture(customMoonTexture);
        }
        else
        {
            // Try loading texture from Resources or Assets if present
            Texture2D loadedTex = Resources.Load<Texture2D>("MoonSurface");
            if (loadedTex != null)
            {
                SetTexture(loadedTex);
            }
            else
            {
                generatedTexture = GenerateProceduralLunarTexture(512, 256);
                SetTexture(generatedTexture);
            }
        }

        // Setup subtle moonlight
        if (moonlight == null)
        {
            GameObject lightObj = new GameObject("Moonlight_Source");
            lightObj.transform.SetParent(transform, false);
            moonlight = lightObj.AddComponent<Light>();
            moonlight.type = LightType.Directional;
            moonlight.color = moonlightColor;
            moonlight.intensity = maxMoonlightIntensity;
            moonlight.shadows = LightShadows.Soft;
            moonlight.transform.forward = (Vector3.zero - transform.position).normalized;
        }

        UpdateAppearance(true);
    }

    private void Start()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.OnWeatherUpdated += HandleWeatherUpdated;
            UpdateFromWeather();
        }
    }

    private void OnDestroy()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.OnWeatherUpdated -= HandleWeatherUpdated;
        }

        if (generatedTexture != null)
        {
            Destroy(generatedTexture);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void Update()
    {
        // Smoothly blend visibility factor
        if (Mathf.Abs(currentVisibility - targetVisibility) > 0.005f)
        {
            currentVisibility = Mathf.MoveTowards(currentVisibility, targetVisibility, Time.deltaTime * transitionSpeed);
            UpdateAppearance(false);
        }

        // Keep Moon gently facing the camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform.position);
            transform.Rotate(0f, 180f, 0f); // face outward toward camera
        }
    }

    private void HandleWeatherUpdated(WeatherData data)
    {
        UpdateFromWeather();
    }

    public void UpdateFromWeather()
    {
        bool isNight = true;
        string condition = "Clear";

        if (WeatherManager.Instance != null)
        {
            isNight = WeatherManager.Instance.IsNight;
            condition = WeatherManager.Instance.CurrentCondition;
        }

        // Calculate target visibility based on Day/Night and Weather
        if (!isNight)
        {
            // Daytime: hide or reduce to faint trace
            targetVisibility = dayAlpha;
        }
        else
        {
            // Nighttime: modulated by cloudiness and rain
            if (condition == "Clear")
            {
                targetVisibility = 1.0f;
            }
            else if (condition == "Clouds")
            {
                targetVisibility = 0.55f; // softened through clouds
            }
            else if (condition == "Rain" || condition == "Drizzle")
            {
                targetVisibility = 0.35f; // dimmer in rain
            }
            else if (condition == "Thunderstorm")
            {
                targetVisibility = 0.25f; // stormy dimming
            }
            else
            {
                targetVisibility = 0.50f;
            }
        }
    }

    public void SetNightFactor(float factor)
    {
        targetVisibility = Mathf.Lerp(dayAlpha, nightAlpha, Mathf.Clamp01(factor));
    }

    private void UpdateAppearance(bool instant)
    {
        if (instant)
            currentVisibility = targetVisibility;

        // Update Material properties
        if (runtimeMaterial != null)
        {
            Color baseCol = moonBaseColor;
            baseCol.a = currentVisibility;
            runtimeMaterial.color = baseCol;

            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", baseCol);

            // Emission for lunar glow
            Color glow = moonGlowColor * (baseGlowIntensity * currentVisibility);
            if (runtimeMaterial.HasProperty("_EmissionColor"))
            {
                runtimeMaterial.SetColor("_EmissionColor", glow);
                if (currentVisibility > 0.1f)
                    runtimeMaterial.EnableKeyword("_EMISSION");
                else
                    runtimeMaterial.DisableKeyword("_EMISSION");
            }
        }

        // Update Moonlight intensity
        if (moonlight != null)
        {
            moonlight.intensity = maxMoonlightIntensity * currentVisibility;
            moonlight.enabled = currentVisibility > 0.05f;
        }

        // Enable or disable renderer when completely invisible
        if (meshRenderer != null)
        {
            meshRenderer.enabled = currentVisibility > 0.02f;
        }
    }

    private void SetTexture(Texture2D tex)
    {
        if (runtimeMaterial != null && tex != null)
        {
            runtimeMaterial.mainTexture = tex;
            if (runtimeMaterial.HasProperty("_BaseMap"))
                runtimeMaterial.SetTexture("_BaseMap", tex);
        }
    }

    /// <summary>
    /// Procedurally creates a realistic 3D lunar surface texture with maria, impact craters, and ejecta rays.
    /// </summary>
    public static Texture2D GenerateProceduralLunarTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.name = "ProceduralMoonSurface";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] cols = new Color32[width * height];
        System.Random rnd = new System.Random(1337);

        // Maria centers (dark basalt plains)
        float[,] maria = new float[,] {
            { 0.35f, 0.45f, 0.18f, 0.22f, 0.42f },
            { 0.42f, 0.32f, 0.10f, 0.10f, 0.48f },
            { 0.58f, 0.38f, 0.08f, 0.08f, 0.50f },
            { 0.68f, 0.45f, 0.09f, 0.08f, 0.46f },
            { 0.72f, 0.35f, 0.06f, 0.06f, 0.52f },
            { 0.65f, 0.60f, 0.08f, 0.07f, 0.48f }
        };

        // Impact craters (Tycho, Copernicus, Kepler)
        float[,] craters = new float[,] {
            { 0.48f, 0.75f, 0.035f, 12f, 0.35f },
            { 0.40f, 0.40f, 0.025f, 8f,  0.22f },
            { 0.30f, 0.42f, 0.018f, 6f,  0.15f }
        };

        for (int y = 0; y < height; y++)
        {
            float ny = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;

                // Base highlands regolith (light grey with multi-octave noise)
                float n1 = Mathf.PerlinNoise(nx * 8f, ny * 8f);
                float n2 = Mathf.PerlinNoise(nx * 24f, ny * 24f) * 0.4f;
                float microNoise = ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                float val = 0.68f + (n1 * 0.18f - 0.09f) + n2 + microNoise;

                // Dark Maria basins
                for (int m = 0; m < 6; m++)
                {
                    float dx = (nx - maria[m, 0]) / maria[m, 2];
                    float dy = (ny - maria[m, 1]) / maria[m, 3];
                    float dSq = dx * dx + dy * dy;
                    if (dSq < 1.3f)
                    {
                        float jitter = Mathf.PerlinNoise(nx * 15f, ny * 15f) * 0.35f;
                        float effD = Mathf.Sqrt(dSq) + jitter;
                        if (effD < 1f)
                        {
                            float blend = 1f - Mathf.Pow(effD, 1.8f);
                            float mariaDarkness = 0.32f * maria[m, 4];
                            val = Mathf.Lerp(val, mariaDarkness, blend * 0.75f);
                        }
                    }
                }

                // Craters & bright ray systems
                for (int c = 0; c < 3; c++)
                {
                    float cdx = nx - craters[c, 0];
                    float cdy = ny - craters[c, 1];
                    float cDist = Mathf.Sqrt(cdx * cdx + cdy * cdy);
                    float rMax = craters[c, 4];
                    float cRadius = craters[c, 2];

                    if (cDist < rMax && cDist > cRadius * 0.5f)
                    {
                        float angle = Mathf.Atan2(cdy, cdx);
                        float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * craters[c, 3] * 0.5f)), 4f);
                        float falloff = 1f - (cDist / rMax);
                        val += ray * falloff * 0.25f;
                    }

                    if (cDist < cRadius * 1.6f)
                    {
                        float normR = cDist / cRadius;
                        if (normR > 0.75f && normR < 1.25f)
                            val += 0.28f * (1f - Mathf.Abs(normR - 1f) * 4f); // Rim
                        else if (normR <= 0.75f)
                            val += (normR < 0.2f) ? 0.20f : -0.15f;          // Floor / Peak
                    }
                }

                byte lum = (byte)Mathf.Clamp((int)(val * 255f), 45, 255);
                byte r = lum;
                byte g = (byte)(lum * 0.98f);
                byte b = (byte)(lum * 0.96f);

                cols[y * width + x] = new Color32(r, g, b, 255);
            }
        }

        tex.SetPixels32(cols);
        tex.Apply(true);
        return tex;
    }
}
