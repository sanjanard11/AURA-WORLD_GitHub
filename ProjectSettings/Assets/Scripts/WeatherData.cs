[System.Serializable]
public class OfflineWeather {
    public string city, condition;
    public float temp, humidity, windSpeed, rain;
    public bool isNight;
}

[System.Serializable]
public class OfflineWeatherList { public OfflineWeather[] items; }