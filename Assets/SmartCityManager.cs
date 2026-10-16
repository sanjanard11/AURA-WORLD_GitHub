using UnityEngine;

[DefaultExecutionOrder(-50)]
public class SmartCityManager : MonoBehaviour
{
    public static SmartCityManager Instance { get; private set; }

    [Header("Settings")]
    public bool autoInitializeCity = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (autoInitializeCity)
        {
            InitializeSmartCity();
        }
    }

    public void InitializeSmartCity()
    {
        Debug.Log("[SmartCityManager] Initializing Smart City environment systems...");

        // 1. Fix Camera Quaternion Normalization if needed
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.localRotation = new Quaternion(0.10300949f, 0.16664531f, -0.01761502f, 0.98042456f);
        }

        // 2. Setup 3D Moon
        SetupMoon();

        // 3. Setup Wind-Responsive Trees
        SetupTrees();

        // 4. Setup Moving Vehicles
        SetupVehicles();

        // 5. Setup Animated Pedestrians
        SetupPedestrians();

        // 6. Setup Streetlights
        SetupStreetlights();

        // 7. Setup Lightning & Thunder
        SetupLightningSystem();

        Debug.Log("[SmartCityManager] Smart City systems fully operational.");
    }

    private void SetupMoon()
    {
        GameObject moonObj = GameObject.Find("Moon");
        if (moonObj == null)
        {
            moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moonObj.name = "Moon";
            Collider col = moonObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        moonObj.transform.position = new Vector3(-8f, 17f, 32f);
        moonObj.transform.localScale = Vector3.one * 3.2f;

        MoonController ctrl = moonObj.GetComponent<MoonController>();
        if (ctrl == null) ctrl = moonObj.AddComponent<MoonController>();

        // Link with WeatherManager
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.moonObject = moonObj;
            WeatherManager.Instance.moonController = ctrl;
        }
    }

    private void SetupTrees()
    {
        string[] treeNames = new string[] { "Tree_01", "Tree_02", "Tree_03", "Tree_04", "Tree_05", "Tree_06" };
        foreach (string tName in treeNames)
        {
            GameObject tObj = GameObject.Find(tName);
            if (tObj != null && tObj.GetComponent<WindResponsiveTree>() == null)
            {
                tObj.AddComponent<WindResponsiveTree>();
            }
        }
    }

    private void SetupVehicles()
    {
        // Setup Vehicle_01
        GameObject v1 = GameObject.Find("Vehicle_01");
        if (v1 != null)
        {
            VehicleController vc1 = v1.GetComponent<VehicleController>();
            if (vc1 == null) vc1 = v1.AddComponent<VehicleController>();
            vc1.cruiseSpeed = 8.5f;
            vc1.startWaypointIndex = 0;

            // Setup Vehicle_02 (Oncoming lane traffic)
            GameObject v2 = GameObject.Find("Vehicle_02");
            if (v2 == null)
            {
                v2 = Instantiate(v1);
                v2.name = "Vehicle_02";
                Transform cityEnv = GameObject.Find("CityEnvironment")?.transform;
                if (cityEnv != null) v2.transform.SetParent(cityEnv, true);

                v2.transform.position = new Vector3(14.0f, 0.01f, -4.2f);
                v2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                VehicleController vc2 = v2.GetComponent<VehicleController>();
                if (vc2 == null) vc2 = v2.AddComponent<VehicleController>();
                vc2.startWaypointIndex = 3; // Lane 2 heading west
                vc2.cruiseSpeed = 7.5f;

                // Colorize Vehicle_02 with distinct paint
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");
                Material bluePaint = new Material(litShader);
                Color c = new Color(0.12f, 0.35f, 0.78f, 1f);
                bluePaint.color = c;
                if (bluePaint.HasProperty("_BaseColor")) bluePaint.SetColor("_BaseColor", c);
                if (bluePaint.HasProperty("_Smoothness")) bluePaint.SetFloat("_Smoothness", 0.85f);
                if (bluePaint.HasProperty("_Metallic")) bluePaint.SetFloat("_Metallic", 0.6f);

                Transform chassis = v2.transform.Find("Car_Chassis");
                if (chassis != null) chassis.GetComponent<MeshRenderer>().material = bluePaint;
                Transform cabin = v2.transform.Find("Car_Cabin");
                if (cabin != null) cabin.GetComponent<MeshRenderer>().material = bluePaint;
            }
        }
    }

    private void SetupPedestrians()
    {
        GameObject pedManager = GameObject.Find("PedestrianManager");
        if (pedManager == null)
        {
            pedManager = new GameObject("PedestrianManager");
            Transform cityEnv = GameObject.Find("CityEnvironment")?.transform;
            if (cityEnv != null) pedManager.transform.SetParent(cityEnv, true);
        }

        PedestrianSpawner spawner = pedManager.GetComponent<PedestrianSpawner>();
        if (spawner == null) spawner = pedManager.AddComponent<PedestrianSpawner>();
        spawner.pedestrianCount = 4;
    }

    private void SetupStreetlights()
    {
        string[] streetLightNames = new string[] { "Streetlight_01", "Streetlight_02" };
        foreach (string slName in streetLightNames)
        {
            GameObject slObj = GameObject.Find(slName);
            if (slObj != null && slObj.GetComponent<StreetlightController>() == null)
            {
                slObj.AddComponent<StreetlightController>();
            }
        }
    }

    private void SetupLightningSystem()
    {
        GameObject weatherSystem = GameObject.Find("Weather System");
        if (weatherSystem != null && weatherSystem.GetComponent<LightningStormController>() == null)
        {
            weatherSystem.AddComponent<LightningStormController>();
        }
    }
}
