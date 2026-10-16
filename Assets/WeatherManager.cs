using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class WeatherData
{
    public string name;
    public MainData main;
    public WindData wind;
    public WeatherInfo[] weather;
    public SysData sys;
    public CloudsData clouds;
    public long dt;
    public int timezone;
}

[System.Serializable]
public class MainData
{
    public float temp;
    public float humidity;
    public float feels_like;
    public float temp_min;
    public float temp_max;
    public float pressure;
}

[System.Serializable]
public class WindData
{
    public float speed;
    public float deg;
    public float gust;
}

[System.Serializable]
public class WeatherInfo
{
    public int id;
    public string main;
    public string description;
    public string icon;
}

[System.Serializable]
public class SysData
{
    public long sunrise;
    public long sunset;
    public string country;
}

[System.Serializable]
public class CloudsData
{
    public int all;
}

public class WeatherManager : MonoBehaviour
{
    [Header("Weather API")]
    [Tooltip("OpenWeatherMap API Key")]
    public string apiKey = "83df4a5aa9321e9d9195217241115f21";
    public float refreshInterval = 60f;

    [Header("Online / Offline Mode")]
    [Tooltip("OFF = Online mode. ON = Offline mode.")]
    public bool offlineMode = false;

    [Tooltip("Apply the selected preview when starting in offline mode.")]
    public bool startWithPreviewCondition = true;

    [Header("City Search UI")]
    public TMP_InputField cityInput;
    public Button searchButton;
    public TextMeshProUGUI weatherText;
    public GameObject weatherPanel;

    [Header("3D Celestial Objects")]
    public GameObject sunObject;
    public GameObject moonObject;
    public MoonController moonController;
    public Light sunlight;

    [Header("Environment & Effects")]
    public WindZone engineWindZone;
    public ParticleSystem rainParticles;
    public AudioSource rainAudioSource;
    public MeshRenderer roadRenderer;
    public Material roadMaterial;

    [Header("Testing & Inspector Previews")]
    public bool testInEditor = false;

    [Tooltip("Choose condition to preview immediately in Play mode")]
    public TestWeatherCondition previewCondition = TestWeatherCondition.Clear;

    public bool previewIsNight = false;

    [Range(0f, 25f)]
    public float previewWindSpeed = 3.5f;

    public enum TestWeatherCondition
    {
        Clear,
        Clouds,
        Rain,
        Thunderstorm,
        Mist
    }

    public static WeatherManager Instance { get; private set; }

    [Header("Live Telemetry State")]
    public string CurrentCondition { get; private set; } = "Clear";
    public string CurrentDescription { get; private set; } = "Clear Sky";
    public float CurrentWindSpeed { get; private set; } = 1.0f;
    public float CurrentWindDirection { get; private set; } = 0.0f;
    public float CurrentTemperature { get; private set; } = 20.0f;
    public float CurrentHumidity { get; private set; } = 50.0f;
    public float Cloudiness { get; private set; } = 0.0f;
    public bool IsRaining { get; private set; } = false;
    public bool IsThunderstorm { get; private set; } = false;
    public bool IsNight { get; private set; } = false;
    public string SunriseTime { get; private set; } = "--:--";
    public string SunsetTime { get; private set; } = "--:--";

    public event Action<WeatherData> OnWeatherUpdated;
    public event Action<bool> OnDayNightChanged;

    private string selectedCity = "";
    private Coroutine requestCoroutine;
    private Coroutine refreshCoroutine;
    private bool isSearching;
    private Material runtimeRoadMat;
    private float targetRoadSmoothness = 0.25f;
    private Color targetRoadColor = new Color(0.22f, 0.22f, 0.24f, 1f);

    // Source of the weather data shown on screen.
    private string dataSource = "Live";

    private void Awake()
    {
        // Disable duplicate WeatherManager component on Rain GameObject.
        if (gameObject.name == "Rain")
        {
            WeatherManager[] managers =
                UnityEngine.Object.FindObjectsByType<WeatherManager>(
                    FindObjectsSortMode.None);

            foreach (var m in managers)
            {
                if (m != this && m.gameObject.name == "Weather System")
                {
                    Debug.LogWarning(
                        "Disabling duplicate WeatherManager component on Rain GameObject.");

                    enabled = false;
                    return;
                }
            }
        }

        // Auto-link references if unassigned in scene.
        if (cityInput == null)
            cityInput = UnityEngine.Object.FindFirstObjectByType<TMP_InputField>();

        if (searchButton == null)
            searchButton = UnityEngine.Object.FindFirstObjectByType<Button>();

        if (weatherPanel == null)
        {
            GameObject wp = GameObject.Find("WeatherReportPanel");
            if (wp != null)
                weatherPanel = wp;
        }

        if (weatherText == null && weatherPanel != null)
            weatherText = weatherPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        if (sunlight == null)
        {
            GameObject dLight = GameObject.Find("Directional Light");
            if (dLight != null)
                sunlight = dLight.GetComponent<Light>();
        }

        if (sunObject == null)
            sunObject = GameObject.Find("Sun");

        if (moonObject == null)
            moonObject = GameObject.Find("Moon");

        if (moonController == null && moonObject != null)
            moonController = moonObject.GetComponent<MoonController>();

        if (rainParticles == null)
        {
            GameObject r = GameObject.Find("Rain");
            if (r != null)
                rainParticles = r.GetComponent<ParticleSystem>();
        }

        // Auto-link rain audio.
        if (rainAudioSource == null && rainParticles != null)
            rainAudioSource = rainParticles.GetComponent<AudioSource>();

        if (engineWindZone == null)
            engineWindZone = UnityEngine.Object.FindFirstObjectByType<WindZone>();

        if (roadRenderer == null)
        {
            GameObject roadObj = GameObject.Find("Road");
            if (roadObj != null)
                roadRenderer = roadObj.GetComponent<MeshRenderer>();
        }

        if (roadRenderer != null && roadRenderer.sharedMaterial != null)
        {
            runtimeRoadMat = new Material(roadRenderer.sharedMaterial);
            roadRenderer.material = runtimeRoadMat;
        }

        if (Instance == null || !Instance.enabled)
            Instance = this;

        // Rain audio must not start automatically.
        if (rainAudioSource != null)
        {
            rainAudioSource.playOnAwake = false;
            rainAudioSource.loop = true;
            rainAudioSource.Stop();
        }
    }

    private void Start()
    {
        // Read API key from environment if supplied.
        string envKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");

        if (!string.IsNullOrEmpty(envKey))
            apiKey = envKey.Trim();

        selectedCity = "";

        if (cityInput != null)
        {
            cityInput.interactable = !offlineMode;
            cityInput.text = "";

            cityInput.onSubmit.RemoveListener(OnInputSubmit);
            cityInput.onSubmit.AddListener(OnInputSubmit);

            cityInput.onEndEdit.RemoveListener(OnInputEndEdit);
            cityInput.onEndEdit.AddListener(OnInputEndEdit);
        }

        if (weatherPanel != null)
            weatherPanel.SetActive(false);

        if (weatherText != null)
        {
            weatherText.text = "";
            weatherText.gameObject.SetActive(true);
        }

        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.enabled = false;

            rainParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            rainParticles.Clear(true);
        }

        if (rainAudioSource != null)
            rainAudioSource.Stop();

        if (searchButton != null)
        {
            searchButton.interactable = !offlineMode;

            searchButton.onClick.RemoveListener(SearchCity);
            searchButton.onClick.AddListener(SearchCity);
        }

        // Start in whichever mode is selected in the Inspector.
        if (offlineMode)
        {
            if (startWithPreviewCondition)
            {
                testInEditor = true;
                dataSource = "Offline preview";

                ApplyTestWeather();
                ShowOfflineTelemetry();
            }
            else
            {
                testInEditor = false;
                ApplyClearEnvironment();

                ShowMessage("Offline Mode: Choose a weather button.");
            }
        }
        else if (testInEditor)
        {
            dataSource = "Offline preview";

            ApplyTestWeather();
            ShowOfflineTelemetry();
        }
        else
        {
            ApplyClearEnvironment();
        }
    }

    private void Update()
    {
        // Smoothly update road wetness.
        if (runtimeRoadMat != null)
        {
            if (runtimeRoadMat.HasProperty("_Smoothness"))
            {
                float curSmoothness = runtimeRoadMat.GetFloat("_Smoothness");

                if (Mathf.Abs(curSmoothness - targetRoadSmoothness) > 0.005f)
                {
                    float newS = Mathf.MoveTowards(
                        curSmoothness,
                        targetRoadSmoothness,
                        Time.deltaTime * 0.4f);

                    runtimeRoadMat.SetFloat("_Smoothness", newS);
                }
            }

            if (runtimeRoadMat.HasProperty("_BaseColor"))
            {
                Color curCol = runtimeRoadMat.GetColor("_BaseColor");

                Color newCol = Color.Lerp(
                    curCol,
                    targetRoadColor,
                    Time.deltaTime * 0.5f);

                runtimeRoadMat.SetColor("_BaseColor", newCol);
            }
        }

        // Keep the selected preview active while in offline preview mode.
        if (offlineMode && testInEditor && !isSearching)
            ApplyTestWeather();

        UpdateRainAudio();
    }

    private void OnInputSubmit(string text)
    {
        SearchCity();
    }

    private void OnInputEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SearchCity();
        }
    }

    // ===== SWITCH BETWEEN ONLINE AND OFFLINE =====

    public void SetOnlineMode()
    {
        offlineMode = false;
        testInEditor = false;
        dataSource = "Live";

        if (cityInput != null)
            cityInput.interactable = true;

        if (searchButton != null)
            searchButton.interactable = true;

        ShowMessage(
            "Online Mode enabled. Enter a city and press Search to get live weather.");

        Debug.Log("Weather World switched to Online Mode.");
    }

    public void SetOfflineMode()
    {
        // Stop any request or automatic refresh before switching offline.
        offlineMode = true;

        if (requestCoroutine != null)
        {
            StopCoroutine(requestCoroutine);
            requestCoroutine = null;
        }

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        isSearching = false;
        dataSource = "Offline preview";
        testInEditor = true;

        if (cityInput != null)
            cityInput.interactable = false;

        if (searchButton != null)
            searchButton.interactable = false;

        ApplyTestWeather();
        ShowOfflineTelemetry();

        Debug.Log("Weather World switched to Offline Mode.");
    }

    public void SearchCity()
    {
        // Never send a request while offline.
        if (offlineMode)
        {
            ShowMessage(
                "Offline Mode: Use Sunny, Cloudy, Rain, Thunderstorm, or Mist buttons.");

            return;
        }

        if (isSearching)
            return;

        if (cityInput == null)
        {
            ShowMessage("City input is not connected.");
            return;
        }

        string city = cityInput.text.Trim();

        if (string.IsNullOrWhiteSpace(city))
        {
            ShowMessage("Please enter a city name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowMessage(
                "API key is missing. Assign your OpenWeatherMap API key in the Inspector.");

            return;
        }

        selectedCity = city;

        if (requestCoroutine != null)
            StopCoroutine(requestCoroutine);

        requestCoroutine = StartCoroutine(GetWeather(city, false));

        if (refreshCoroutine == null)
            refreshCoroutine = StartCoroutine(RefreshWeatherLoop());
    }

    private IEnumerator RefreshWeatherLoop()
    {
        while (!offlineMode)
        {
            yield return new WaitForSeconds(Mathf.Max(10f, refreshInterval));

            if (!offlineMode &&
                !string.IsNullOrWhiteSpace(selectedCity) &&
                !isSearching)
            {
                if (requestCoroutine != null)
                    StopCoroutine(requestCoroutine);

                requestCoroutine = StartCoroutine(
                    GetWeather(selectedCity, true));
            }
        }

        refreshCoroutine = null;
    }

    // ===== OFFLINE CACHE SUPPORT =====

    private string GetCachePath(string city)
    {
        string safe = city.Trim().ToLowerInvariant();

        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');

        return System.IO.Path.Combine(
            Application.persistentDataPath,
            "weather_" + safe + ".json");
    }

    private void SaveCache(string path, string json)
    {
        try
        {
            System.IO.File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not save weather cache: " + ex.Message);
        }
    }

    private string LoadOfflineJson(string cachePath)
    {
        try
        {
            if (System.IO.File.Exists(cachePath))
            {
                dataSource = "Saved data (offline)";
                return System.IO.File.ReadAllText(cachePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not read weather cache: " + ex.Message);
        }

        TextAsset sample = Resources.Load<TextAsset>("sample_weather");

        if (sample != null)
        {
            dataSource = "Sample data (offline)";
            return sample.text;
        }

        return null;
    }

    private IEnumerator GetWeather(string city, bool isAutoRefresh)
    {
        if (offlineMode)
            yield break;

        isSearching = true;

        if (!isAutoRefresh)
            ShowMessage("Fetching weather for " + city + "...");

        string url =
            "https://api.openweathermap.org/data/2.5/weather?q=" +
            UnityWebRequest.EscapeURL(city) +
            "&appid=" + UnityWebRequest.EscapeURL(apiKey) +
            "&units=metric";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 15;

            yield return request.SendWebRequest();

            // If mode changed while waiting for the request, ignore its result.
            if (offlineMode)
            {
                isSearching = false;
                requestCoroutine = null;
                yield break;
            }

            string json = null;
            dataSource = "Live";

            string cachePath = GetCachePath(city);

            if (request.result == UnityWebRequest.Result.Success)
            {
                json = request.downloadHandler.text;
                SaveCache(cachePath, json);
            }
            else
            {
                Debug.LogError(
                    "Weather API error code: " + request.responseCode +
                    " | Status: " + request.error);

                // On a failed manual request, try saved/sample data.
                // Do not use fallback for an invalid city (404).
                if (!isAutoRefresh && request.responseCode != 404)
                    json = LoadOfflineJson(cachePath);

                if (json == null)
                {
                    if (!isAutoRefresh)
                    {
                        ShowMessage(
                            "Could not get weather for '" + city +
                            "'.\nCheck the city name or internet connection.");
                    }

                    isSearching = false;
                    requestCoroutine = null;
                    yield break;
                }
            }

            WeatherData data = null;

            try
            {
                data = JsonUtility.FromJson<WeatherData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse weather data: " + ex.Message);
            }

            if (data == null ||
                data.main == null ||
                data.wind == null ||
                data.weather == null ||
                data.weather.Length == 0)
            {
                if (!isAutoRefresh)
                    ShowMessage("Invalid weather data received for '" + city + "'.");

                isSearching = false;
                requestCoroutine = null;
                yield break;
            }

            ApplyWeather(data);
        }

        isSearching = false;
        requestCoroutine = null;
    }

    public void ApplyClearEnvironment()
    {
        CurrentCondition = "Clear";
        CurrentDescription = "Clear Sky";
        IsRaining = false;
        IsThunderstorm = false;
        IsNight = false;

        CurrentWindSpeed = 1.2f;
        CurrentWindDirection = 0f;
        CurrentTemperature = 22.0f;
        CurrentHumidity = 45.0f;
        Cloudiness = 0.05f;

        targetRoadSmoothness = 0.25f;
        targetRoadColor = new Color(0.22f, 0.22f, 0.24f, 1f);

        UpdateCelestialAndLighting();
        UpdateRainAndWind();
        UpdateRainAudio();
    }

    public void ApplyTestWeather()
    {
        CurrentCondition = previewCondition.ToString();
        IsNight = previewIsNight;
        CurrentWindSpeed = previewWindSpeed;

        switch (previewCondition)
        {
            case TestWeatherCondition.Clear:
                CurrentDescription = IsNight ? "Clear Night" : "Clear Sky";
                IsRaining = false;
                IsThunderstorm = false;
                Cloudiness = 0.05f;

                targetRoadSmoothness = 0.25f;
                targetRoadColor = new Color(0.22f, 0.22f, 0.24f, 1f);
                break;

            case TestWeatherCondition.Clouds:
                CurrentDescription = "Overcast Clouds";
                IsRaining = false;
                IsThunderstorm = false;
                Cloudiness = 0.85f;

                targetRoadSmoothness = 0.25f;
                targetRoadColor = new Color(0.22f, 0.22f, 0.24f, 1f);
                break;

            case TestWeatherCondition.Rain:
                CurrentDescription = "Moderate Rain";
                IsRaining = true;
                IsThunderstorm = false;
                Cloudiness = 0.95f;

                targetRoadSmoothness = 0.88f;
                targetRoadColor = new Color(0.12f, 0.12f, 0.14f, 1f);
                break;

            case TestWeatherCondition.Thunderstorm:
                CurrentDescription = "Thunderstorm with Heavy Rain";
                IsRaining = true;
                IsThunderstorm = true;
                Cloudiness = 1.0f;

                targetRoadSmoothness = 0.92f;
                targetRoadColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                break;

            case TestWeatherCondition.Mist:
                CurrentDescription = "Hazy Mist";
                IsRaining = false;
                IsThunderstorm = false;
                Cloudiness = 0.60f;

                targetRoadSmoothness = 0.35f;
                targetRoadColor = new Color(0.20f, 0.20f, 0.22f, 1f);
                break;
        }

        UpdateCelestialAndLighting();
        UpdateRainAndWind();
        UpdateRainAudio();
    }

    public void ApplyWeather(WeatherData data)
    {
        if (data == null ||
            data.weather == null ||
            data.weather.Length == 0)
        {
            ShowMessage("Weather data is incomplete.");
            return;
        }

        string condition = data.weather[0].main;
        string description = data.weather[0].description;

        string actualCity =
            string.IsNullOrEmpty(data.name) ? selectedCity : data.name;

        string country =
            (data.sys != null && !string.IsNullOrEmpty(data.sys.country))
                ? ", " + data.sys.country
                : "";

        CurrentCondition = condition;
        CurrentWindSpeed = data.wind != null ? data.wind.speed : 1f;
        CurrentWindDirection = data.wind != null ? data.wind.deg : 0f;
        CurrentTemperature = data.main != null ? data.main.temp : 20f;
        CurrentHumidity = data.main != null ? data.main.humidity : 50f;
        Cloudiness = data.clouds != null ? data.clouds.all / 100f : 0f;

        bool calculatedNight = false;

        if (data.sys != null &&
            data.sys.sunrise > 0 &&
            data.sys.sunset > 0)
        {
            long currentTime =
                data.dt > 0
                    ? data.dt
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            calculatedNight =
                currentTime < data.sys.sunrise ||
                currentTime >= data.sys.sunset;

            try
            {
                DateTimeOffset sr =
                    DateTimeOffset.FromUnixTimeSeconds(
                        data.sys.sunrise + data.timezone);

                DateTimeOffset ss =
                    DateTimeOffset.FromUnixTimeSeconds(
                        data.sys.sunset + data.timezone);

                SunriseTime = sr.ToString("hh:mm tt");
                SunsetTime = ss.ToString("hh:mm tt");
            }
            catch
            {
                SunriseTime = "06:00 AM";
                SunsetTime = "06:30 PM";
            }
        }
        else if (data.weather[0].icon != null &&
                 data.weather[0].icon.EndsWith("n"))
        {
            calculatedNight = true;
        }

        bool previousNight = IsNight;
        IsNight = calculatedNight;

        if (previousNight != IsNight)
            OnDayNightChanged?.Invoke(IsNight);

        if (!string.IsNullOrEmpty(description) && description.Length > 1)
        {
            description =
                char.ToUpper(description[0]) + description.Substring(1);
        }

        CurrentDescription = description;

        bool raining =
            condition == "Rain" ||
            condition == "Drizzle" ||
            condition == "Thunderstorm";

        bool storm = condition == "Thunderstorm";

        IsRaining = raining;
        IsThunderstorm = storm;

        if (raining)
        {
            targetRoadSmoothness = storm ? 0.92f : 0.88f;
            targetRoadColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        }
        else
        {
            targetRoadSmoothness = 0.25f;
            targetRoadColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        }

        string dayNightIcon = IsNight ? "Night 🌙" : "Day ☀️";

        if (weatherPanel != null)
            weatherPanel.SetActive(true);

        if (weatherText != null)
        {
            weatherText.gameObject.SetActive(true);

            weatherText.text =
                "<b>WEATHER TELEMETRY</b>\n" +
                "<b>City:</b> " + actualCity + country +
                " (" + dayNightIcon + ")\n" +
                "<b>Condition:</b> " + description + "\n" +
                "<b>Temperature:</b> " +
                CurrentTemperature.ToString("F1") + " °C\n" +
                "<b>Wind:</b> " +
                CurrentWindSpeed.ToString("F1") + " m/s (" +
                CurrentWindDirection.ToString("F0") + "°)\n" +
                "<b>Humidity:</b> " +
                CurrentHumidity.ToString("F0") + "% | " +
                "<b>Clouds:</b> " +
                (Cloudiness * 100f).ToString("F0") + "%\n" +
                "<b>Sunrise:</b> " + SunriseTime +
                " | <b>Sunset:</b> " + SunsetTime +
                "\n<b>Source:</b> " + dataSource;
        }

        UpdateCelestialAndLighting();
        UpdateRainAndWind();
        UpdateRainAudio();

        OnWeatherUpdated?.Invoke(data);

        Debug.Log(
            "Weather telemetry updated for " + actualCity +
            ": " + condition + " (" + dayNightIcon + "), " +
            CurrentTemperature.ToString("F1") + " °C [" + dataSource + "]");
    }

    private void UpdateCelestialAndLighting()
    {
        string condition = CurrentCondition;

        bool cloudy = condition == "Clouds";
        bool raining = IsRaining;
        bool storm = IsThunderstorm;

        bool mistOrFog =
            condition == "Mist" ||
            condition == "Fog" ||
            condition == "Haze";

        // Sun and moon visibility.
        if (sunObject != null)
        {
            sunObject.SetActive(
                !IsNight &&
                (condition == "Clear" || condition == "Clouds"));
        }

        if (moonController != null)
        {
            moonController.UpdateFromWeather();
        }
        else if (moonObject != null)
        {
            moonObject.SetActive(IsNight);
        }

        // Directional light.
        if (sunlight != null)
        {
            if (!IsNight)
            {
                sunlight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                if (condition == "Clear")
                {
                    sunlight.intensity = 1.6f;
                    sunlight.color = new Color(1.0f, 0.96f, 0.88f);

                    RenderSettings.ambientLight =
                        new Color(0.80f, 0.85f, 0.92f);

                    RenderSettings.fog = false;
                }
                else if (cloudy)
                {
                    sunlight.intensity = 0.70f;
                    sunlight.color = new Color(0.85f, 0.88f, 0.95f);

                    RenderSettings.ambientLight =
                        new Color(0.62f, 0.65f, 0.72f);

                    SetFog(0.005f, new Color(0.72f, 0.75f, 0.82f));
                }
                else if (storm)
                {
                    sunlight.intensity = 0.25f;
                    sunlight.color = new Color(0.48f, 0.52f, 0.64f);

                    RenderSettings.ambientLight =
                        new Color(0.35f, 0.38f, 0.46f);

                    SetFog(0.016f, new Color(0.40f, 0.44f, 0.52f));
                }
                else if (raining)
                {
                    sunlight.intensity = 0.40f;
                    sunlight.color = new Color(0.65f, 0.70f, 0.82f);

                    RenderSettings.ambientLight =
                        new Color(0.45f, 0.48f, 0.55f);

                    SetFog(0.012f, new Color(0.50f, 0.55f, 0.62f));
                }
                else if (mistOrFog)
                {
                    sunlight.intensity = 0.55f;
                    sunlight.color = new Color(0.80f, 0.82f, 0.88f);

                    RenderSettings.ambientLight =
                        new Color(0.55f, 0.58f, 0.65f);

                    SetFog(0.022f, new Color(0.75f, 0.78f, 0.82f));
                }
            }
            else
            {
                sunlight.transform.rotation = Quaternion.Euler(55f, 150f, 0f);

                if (condition == "Clear")
                {
                    sunlight.intensity = 0.35f;
                    sunlight.color = new Color(0.65f, 0.76f, 0.96f);

                    RenderSettings.ambientLight =
                        new Color(0.12f, 0.16f, 0.28f);

                    RenderSettings.fog = false;
                }
                else if (cloudy)
                {
                    sunlight.intensity = 0.20f;
                    sunlight.color = new Color(0.52f, 0.62f, 0.80f);

                    RenderSettings.ambientLight =
                        new Color(0.09f, 0.12f, 0.20f);

                    SetFog(0.006f, new Color(0.15f, 0.18f, 0.26f));
                }
                else if (storm)
                {
                    sunlight.intensity = 0.08f;
                    sunlight.color = new Color(0.35f, 0.42f, 0.58f);

                    RenderSettings.ambientLight =
                        new Color(0.04f, 0.06f, 0.10f);

                    SetFog(0.018f, new Color(0.08f, 0.10f, 0.16f));
                }
                else if (raining)
                {
                    sunlight.intensity = 0.14f;
                    sunlight.color = new Color(0.45f, 0.54f, 0.70f);

                    RenderSettings.ambientLight =
                        new Color(0.06f, 0.08f, 0.14f);

                    SetFog(0.014f, new Color(0.10f, 0.12f, 0.18f));
                }
                else
                {
                    sunlight.intensity = 0.22f;
                    sunlight.color = new Color(0.58f, 0.68f, 0.88f);

                    RenderSettings.ambientLight =
                        new Color(0.08f, 0.11f, 0.18f);

                    SetFog(0.008f, new Color(0.12f, 0.15f, 0.22f));
                }
            }
        }
    }

    private void SetFog(float density, Color color)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = density;
        RenderSettings.fogColor = color;
    }

    private void UpdateRainAndWind()
    {
        // Rain particles.
        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.enabled = IsRaining;

            if (IsRaining)
            {
                var vel = rainParticles.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;

                float windAngleRad =
                    CurrentWindDirection * Mathf.Deg2Rad;

                float windFactor =
                    Mathf.Clamp(CurrentWindSpeed * 0.4f, 0.5f, 6.0f);

                vel.x = new ParticleSystem.MinMaxCurve(
                    Mathf.Sin(windAngleRad) * windFactor);

                vel.z = new ParticleSystem.MinMaxCurve(
                    Mathf.Cos(windAngleRad) * windFactor);

                if (!rainParticles.isPlaying)
                    rainParticles.Play();
            }
            else
            {
                rainParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);

                rainParticles.Clear(true);
            }
        }

        // Wind zone.
        if (engineWindZone != null)
        {
            engineWindZone.windMain =
                Mathf.Clamp(CurrentWindSpeed * 0.25f, 0.2f, 5.0f);
        }
    }

    // Play rain audio only during rain or thunderstorms.
    private void UpdateRainAudio()
    {
        if (rainAudioSource == null)
            return;

        if (IsRaining)
        {
            if (!rainAudioSource.isPlaying)
                rainAudioSource.Play();
        }
        else
        {
            if (rainAudioSource.isPlaying)
                rainAudioSource.Stop();
        }
    }

    // ===== OFFLINE WEATHER BUTTONS =====

    public void SetSunnyOffline()
    {
        SetOfflineCondition(TestWeatherCondition.Clear);
    }

    public void SetCloudyOffline()
    {
        SetOfflineCondition(TestWeatherCondition.Clouds);
    }

    public void SetRainOffline()
    {
        SetOfflineCondition(TestWeatherCondition.Rain);
    }

    public void SetThunderstormOffline()
    {
        SetOfflineCondition(TestWeatherCondition.Thunderstorm);
    }

    public void SetMistOffline()
    {
        SetOfflineCondition(TestWeatherCondition.Mist);
    }

    private void SetOfflineCondition(TestWeatherCondition condition)
    {
        // Switching to an offline condition stops online requests.
        if (!offlineMode)
            SetOfflineMode();

        previewCondition = condition;
        previewIsNight = false;
        testInEditor = true;
        dataSource = "Offline preview";

        ApplyTestWeather();
        ShowOfflineTelemetry();
    }

    public void SetOfflineNight(bool night)
    {
        if (!offlineMode)
            SetOfflineMode();

        previewIsNight = night;
        testInEditor = true;
        dataSource = "Offline preview";

        ApplyTestWeather();
        ShowOfflineTelemetry();
    }

    private void ShowOfflineTelemetry()
    {
        if (weatherPanel != null)
            weatherPanel.SetActive(true);

        if (weatherText == null)
            return;

        weatherText.gameObject.SetActive(true);

        weatherText.text =
            "<b>OFFLINE WEATHER TELEMETRY</b>\n" +
            "<b>Condition:</b> " + CurrentDescription + "\n" +
            "<b>Temperature:</b> " +
            CurrentTemperature.ToString("F1") + " °C\n" +
            "<b>Wind:</b> " +
            CurrentWindSpeed.ToString("F1") + " m/s\n" +
            "<b>Humidity:</b> " +
            CurrentHumidity.ToString("F0") + "%\n" +
            "<b>Clouds:</b> " +
            (Cloudiness * 100f).ToString("F0") + "%\n" +
            "<b>Time:</b> " + (IsNight ? "Night" : "Day") + "\n" +
            "<b>Source:</b> Offline preview";
    }

    private void ShowMessage(string message)
    {
        if (weatherPanel != null)
            weatherPanel.SetActive(true);

        if (weatherText != null)
        {
            weatherText.gameObject.SetActive(true);
            weatherText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (cityInput != null)
        {
            cityInput.onSubmit.RemoveListener(OnInputSubmit);
            cityInput.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        if (searchButton != null)
            searchButton.onClick.RemoveListener(SearchCity);

        if (requestCoroutine != null)
            StopCoroutine(requestCoroutine);

        if (refreshCoroutine != null)
            StopCoroutine(refreshCoroutine);

        if (runtimeRoadMat != null)
            Destroy(runtimeRoadMat);

        if (Instance == this)
            Instance = null;
    }
}