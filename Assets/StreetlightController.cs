using UnityEngine;

public class StreetlightController : MonoBehaviour
{
    [Header("Lighting")]
    public Light lampLight;
    public MeshRenderer lampRenderer;
    public Color warmLightColor = new Color(1.0f, 0.86f, 0.62f);
    public float nightIntensity = 2.4f;
    public float lightRange = 8.5f;

    [Header("Glow Material")]
    public Material customGlowMaterial;

    private Material runtimeLampMat;
    private float targetIntensity = 0f;

    private void Awake()
    {
        // Auto-find lamp mesh
        if (lampRenderer == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("lamp"))
                {
                    lampRenderer = child.GetComponent<MeshRenderer>();
                    break;
                }
            }
        }

        // Setup downward point light
        if (lampLight == null)
        {
            Transform lightTrans = transform.Find("Streetlight_PointLight");
            if (lightTrans == null)
            {
                GameObject lObj = new GameObject("Streetlight_PointLight");
                lObj.transform.SetParent(transform, false);
                lObj.transform.localPosition = new Vector3(0f, 3.3f, -0.5f);
                lampLight = lObj.AddComponent<Light>();
                lampLight.type = LightType.Point;
                lampLight.range = lightRange;
                lampLight.color = warmLightColor;
                lampLight.intensity = 0f;
                lampLight.shadows = LightShadows.Soft;
            }
            else
            {
                lampLight = lightTrans.GetComponent<Light>();
            }
        }

        if (lampRenderer != null)
        {
            runtimeLampMat = new Material(lampRenderer.sharedMaterial);
            lampRenderer.material = runtimeLampMat;
        }
    }

    private void OnDestroy()
    {
        if (runtimeLampMat != null)
        {
            Destroy(runtimeLampMat);
        }
    }

    private void Update()
    {
        bool isNight = false;
        bool isDarkStorm = false;

        if (WeatherManager.Instance != null)
        {
            isNight = WeatherManager.Instance.IsNight;
            isDarkStorm = WeatherManager.Instance.IsThunderstorm;
        }

        targetIntensity = (isNight || isDarkStorm) ? nightIntensity : 0f;

        if (lampLight != null)
        {
            lampLight.intensity = Mathf.MoveTowards(lampLight.intensity, targetIntensity, Time.deltaTime * 3.5f);
            lampLight.enabled = lampLight.intensity > 0.02f;
        }

        if (runtimeLampMat != null)
        {
            float norm = Mathf.Clamp01(lampLight != null ? (lampLight.intensity / nightIntensity) : (targetIntensity / nightIntensity));
            Color glow = warmLightColor * (norm * 1.6f);

            if (runtimeLampMat.HasProperty("_EmissionColor"))
            {
                runtimeLampMat.SetColor("_EmissionColor", glow);
                if (norm > 0.05f)
                    runtimeLampMat.EnableKeyword("_EMISSION");
                else
                    runtimeLampMat.DisableKeyword("_EMISSION");
            }
        }
    }
}
