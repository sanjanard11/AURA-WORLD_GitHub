using System.Collections;
using UnityEngine;

public class LightningStormController : MonoBehaviour
{
    [Header("Lightning Light")]
    public Light lightningLight;
    public Color lightningColor = new Color(0.85f, 0.92f, 1.0f);
    public float maxFlashIntensity = 3.5f;

    [Header("Timing (Irregular intervals)")]
    public float minInterval = 5.0f;
    public float maxInterval = 13.0f;
    public float minThunderDelay = 1.0f;
    public float maxThunderDelay = 2.4f;

    [Header("Thunder Audio")]
    public AudioSource audioSource;
    public AudioClip customThunderClip;
    [Range(0f, 1f)]
    public float thunderVolume = 0.65f;

    private Coroutine stormCoroutine;
    private AudioClip proceduralThunderClip;
    private bool isStormActive;

    private void Awake()
    {
        // Setup lightning light
        if (lightningLight == null)
        {
            GameObject lObj = new GameObject("Lightning_Light");
            lObj.transform.SetParent(transform, false);
            lObj.transform.position = new Vector3(0f, 25f, 0f);
            lightningLight = lObj.AddComponent<Light>();
            lightningLight.type = LightType.Directional;
            lightningLight.color = lightningColor;
            lightningLight.intensity = 0f;
            lightningLight.enabled = false;
        }

        // Setup AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D ambient thunder
            audioSource.volume = thunderVolume;
        }

        // Generate procedural thunder sound as built-in fallback
        if (customThunderClip == null)
        {
            proceduralThunderClip = GenerateProceduralThunderClip();
        }
    }

    private void Start()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.OnWeatherUpdated += HandleWeatherUpdated;
            CheckStormState();
        }
    }

    private void OnDestroy()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.OnWeatherUpdated -= HandleWeatherUpdated;
        }

        if (proceduralThunderClip != null)
        {
            Destroy(proceduralThunderClip);
        }
    }

    private void HandleWeatherUpdated(WeatherData data)
    {
        CheckStormState();
    }

    private void Update()
    {
        // In case WeatherManager is updated without event
        if (WeatherManager.Instance != null)
        {
            bool shouldBeActive = WeatherManager.Instance.IsThunderstorm;
            if (shouldBeActive != isStormActive)
            {
                CheckStormState();
            }
        }
    }

    private void CheckStormState()
    {
        bool isStorm = WeatherManager.Instance != null && WeatherManager.Instance.IsThunderstorm;

        if (isStorm && !isStormActive)
        {
            isStormActive = true;
            if (stormCoroutine != null) StopCoroutine(stormCoroutine);
            stormCoroutine = StartCoroutine(StormRoutine());
        }
        else if (!isStorm && isStormActive)
        {
            isStormActive = false;
            if (stormCoroutine != null)
            {
                StopCoroutine(stormCoroutine);
                stormCoroutine = null;
            }
            if (lightningLight != null)
            {
                lightningLight.intensity = 0f;
                lightningLight.enabled = false;
            }
        }
    }

    private IEnumerator StormRoutine()
    {
        while (isStormActive)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!isStormActive) yield break;

            // Trigger multi-strobe lightning flash
            yield return StartCoroutine(DoMultiStrobeFlash());

            // Realistic thunder audio delay (speed of sound over distance)
            float thunderDelay = Random.Range(minThunderDelay, maxThunderDelay);
            yield return new WaitForSeconds(thunderDelay);

            if (!isStormActive) yield break;

            PlayThunderSound();
        }
    }

    private IEnumerator DoMultiStrobeFlash()
    {
        if (lightningLight == null) yield break;

        lightningLight.enabled = true;

        // 2-3 quick strobe bursts
        int bursts = Random.Range(2, 4);
        for (int i = 0; i < bursts; i++)
        {
            float peak = Random.Range(maxFlashIntensity * 0.6f, maxFlashIntensity);
            lightningLight.intensity = peak;
            yield return new WaitForSeconds(Random.Range(0.04f, 0.08f));

            lightningLight.intensity = peak * 0.15f;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
        }

        // Final flash fadeout
        float elapsed = 0f;
        float duration = Random.Range(0.12f, 0.22f);
        float startInt = lightningLight.intensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lightningLight.intensity = Mathf.Lerp(startInt, 0f, elapsed / duration);
            yield return null;
        }

        lightningLight.intensity = 0f;
        lightningLight.enabled = false;
    }

    private void PlayThunderSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = customThunderClip != null ? customThunderClip : proceduralThunderClip;
        if (clipToPlay != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.volume = thunderVolume * Random.Range(0.8f, 1.0f);
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    /// <summary>
    /// Procedurally synthesizes a low-frequency, deep rolling thunder audio clip.
    /// </summary>
    private AudioClip GenerateProceduralThunderClip()
    {
        int sampleRate = 22050;
        float duration = 3.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        System.Random rnd = new System.Random(77);
        float brownNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Low frequency rumble generator using filtered brown noise
            float white = ((float)rnd.NextDouble() * 2f - 1f);
            brownNoise = (brownNoise + (0.02f * white)) / 1.02f;

            // Secondary resonant low rumble hum (35Hz - 55Hz)
            float hum = Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.15f;

            // Amplitude envelope: sharp attack at 0.15s, followed by long rolling decay with rumbles
            float envelope;
            if (t < 0.15f)
            {
                envelope = t / 0.15f;
            }
            else
            {
                float decay = Mathf.Exp(-(t - 0.15f) * 0.9f);
                float rumbleMod = 0.7f + 0.3f * Mathf.Sin(t * 7.5f) * Mathf.Sin(t * 13.2f);
                envelope = decay * rumbleMod;
            }

            samples[i] = Mathf.Clamp((brownNoise * 3.5f + hum) * envelope, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("ProceduralThunder", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
