#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

public static class SmartCitySceneSetup
{
    [MenuItem("AURA-WORLD/Setup Smart City Scene")]
    public static void SetupScene()
    {
        Debug.Log(">>> Starting Smart City Scene Setup for AURA-WORLD...");

        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("Failed to open scene: " + scenePath);
            return;
        }

        // 1. Fix Camera Quaternion Normalization to eliminate URP console assertions
        GameObject mainCam = GameObject.Find("Main Camera");
        if (mainCam != null)
        {
            mainCam.transform.localRotation = new Quaternion(0.10300949f, 0.16664531f, -0.01761502f, 0.98042456f);
            Debug.Log("[Setup] Main Camera rotation normalized successfully.");
        }

        // 2. Setup MoonMaterial
        string moonMatPath = "Assets/MoonMaterial.mat";
        Material moonMat = AssetDatabase.LoadAssetAtPath<Material>(moonMatPath);
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        if (moonMat == null)
        {
            moonMat = new Material(litShader);
            moonMat.name = "MoonMaterial";
            moonMat.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            if (moonMat.HasProperty("_BaseColor"))
                moonMat.SetColor("_BaseColor", new Color(0.92f, 0.94f, 0.98f, 1f));
            if (moonMat.HasProperty("_Smoothness"))
                moonMat.SetFloat("_Smoothness", 0.15f);

            moonMat.EnableKeyword("_EMISSION");
            if (moonMat.HasProperty("_EmissionColor"))
                moonMat.SetColor("_EmissionColor", new Color(0.70f, 0.78f, 0.95f, 1f) * 0.40f);

            AssetDatabase.CreateAsset(moonMat, moonMatPath);
            Debug.Log("[Setup] Created MoonMaterial at: " + moonMatPath);
        }

        // 3. Setup 3D Moon GameObject
        GameObject moonObj = GameObject.Find("Moon");
        if (moonObj == null)
        {
            moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moonObj.name = "Moon";
            Collider col = moonObj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        moonObj.transform.position = new Vector3(-8f, 17f, 32f);
        moonObj.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
        MeshRenderer moonRend = moonObj.GetComponent<MeshRenderer>();
        if (moonRend != null && moonMat != null)
        {
            moonRend.sharedMaterial = moonMat;
        }

        MoonController moonCtrl = moonObj.GetComponent<MoonController>();
        if (moonCtrl == null) moonCtrl = moonObj.AddComponent<MoonController>();
        moonCtrl.moonMaterial = moonMat;
        moonCtrl.skyPosition = new Vector3(-8f, 17f, 32f);
        moonCtrl.moonScale = 3.2f;

        // Child Moonlight source
        Transform mlTrans = moonObj.transform.Find("Moonlight_Source");
        GameObject mlObj = mlTrans != null ? mlTrans.gameObject : new GameObject("Moonlight_Source");
        mlObj.transform.SetParent(moonObj.transform, false);
        Light moonLight = mlObj.GetComponent<Light>();
        if (moonLight == null) moonLight = mlObj.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.68f, 0.78f, 0.98f);
        moonLight.intensity = 0.35f;
        moonLight.shadows = LightShadows.Soft;
        mlObj.transform.forward = (Vector3.zero - moonObj.transform.position).normalized;
        moonCtrl.moonlight = moonLight;
        Debug.Log("[Setup] 3D Moon configured with MoonController and Moonlight source.");

        // 4. Setup WindResponsiveTree on all trees
        string[] treeNames = new string[] { "Tree_01", "Tree_02", "Tree_03", "Tree_04", "Tree_05", "Tree_06" };
        foreach (string tName in treeNames)
        {
            GameObject tObj = GameObject.Find(tName);
            if (tObj != null)
            {
                WindResponsiveTree wrt = tObj.GetComponent<WindResponsiveTree>();
                if (wrt == null) wrt = tObj.AddComponent<WindResponsiveTree>();
                Debug.Log("[Setup] WindResponsiveTree attached to: " + tName);
            }
        }

        // 5. Setup Vehicle_01 with VehicleController and Headlights
        GameObject v1 = GameObject.Find("Vehicle_01");
        if (v1 != null)
        {
            VehicleController vc1 = v1.GetComponent<VehicleController>();
            if (vc1 == null) vc1 = v1.AddComponent<VehicleController>();
            vc1.cruiseSpeed = 8.5f;
            vc1.startWaypointIndex = 0;
            Debug.Log("[Setup] VehicleController configured on Vehicle_01.");
        }

        // 6. Setup Vehicle_02 (Second vehicle for 2-way traffic)
        GameObject v2 = GameObject.Find("Vehicle_02");
        if (v2 == null && v1 != null)
        {
            v2 = Object.Instantiate(v1);
            v2.name = "Vehicle_02";
            Transform cityEnv = GameObject.Find("CityEnvironment")?.transform;
            if (cityEnv != null) v2.transform.SetParent(cityEnv, true);

            // Create distinct cobalt blue car paint material
            string v2MatPath = "Assets/CarPaintMaterial_Blue.mat";
            Material v2Mat = AssetDatabase.LoadAssetAtPath<Material>(v2MatPath);
            if (v2Mat == null)
            {
                v2Mat = new Material(litShader);
                v2Mat.name = "CarPaintMaterial_Blue";
                Color blueCol = new Color(0.12f, 0.32f, 0.75f, 1f);
                v2Mat.color = blueCol;
                if (v2Mat.HasProperty("_BaseColor")) v2Mat.SetColor("_BaseColor", blueCol);
                if (v2Mat.HasProperty("_Smoothness")) v2Mat.SetFloat("_Smoothness", 0.85f);
                if (v2Mat.HasProperty("_Metallic")) v2Mat.SetFloat("_Metallic", 0.65f);
                AssetDatabase.CreateAsset(v2Mat, v2MatPath);
            }

            // Assign new material to chassis and cabin
            Transform chassis = v2.transform.Find("Car_Chassis");
            if (chassis != null) chassis.GetComponent<MeshRenderer>().sharedMaterial = v2Mat;
            Transform cabin = v2.transform.Find("Car_Cabin");
            if (cabin != null) cabin.GetComponent<MeshRenderer>().sharedMaterial = v2Mat;

            // Configure VehicleController for oncoming lane (starts at waypoint 3)
            VehicleController vc2 = v2.GetComponent<VehicleController>();
            if (vc2 == null) vc2 = v2.AddComponent<VehicleController>();
            vc2.startWaypointIndex = 3;
            vc2.cruiseSpeed = 7.5f;
            v2.transform.position = new Vector3(12.0f, 0.01f, -4.2f);
            v2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Debug.Log("[Setup] Vehicle_02 created and configured for oncoming traffic lane.");
        }

        // 7. Setup Streetlights with StreetlightController
        string[] streetLightNames = new string[] { "Streetlight_01", "Streetlight_02" };
        foreach (string slName in streetLightNames)
        {
            GameObject slObj = GameObject.Find(slName);
            if (slObj != null)
            {
                StreetlightController slc = slObj.GetComponent<StreetlightController>();
                if (slc == null) slc = slObj.AddComponent<StreetlightController>();
                Debug.Log("[Setup] StreetlightController attached to: " + slName);
            }
        }

        // 8. Setup Pedestrians & PedestrianSpawner
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
        Debug.Log("[Setup] PedestrianSpawner configured.");

        // 9. Setup LightningStormController on Weather System
        GameObject weatherSystem = GameObject.Find("Weather System");
        if (weatherSystem == null) weatherSystem = new GameObject("Weather System");

        LightningStormController lsc = weatherSystem.GetComponent<LightningStormController>();
        if (lsc == null) lsc = weatherSystem.AddComponent<LightningStormController>();

        Transform llTrans = weatherSystem.transform.Find("Lightning_Light");
        GameObject llObj = llTrans != null ? llTrans.gameObject : new GameObject("Lightning_Light");
        llObj.transform.SetParent(weatherSystem.transform, false);
        Light lLight = llObj.GetComponent<Light>();
        if (lLight == null) lLight = llObj.AddComponent<Light>();
        lLight.type = LightType.Directional;
        lLight.color = new Color(0.85f, 0.92f, 1.0f);
        lLight.intensity = 0f;
        lLight.enabled = false;
        lsc.lightningLight = lLight;
        Debug.Log("[Setup] LightningStormController configured on Weather System.");

        // 10. Link all Inspector references on WeatherManager
        WeatherManager wm = weatherSystem.GetComponent<WeatherManager>();
        if (wm == null) wm = weatherSystem.AddComponent<WeatherManager>();

        wm.sunObject = GameObject.Find("Sun");
        wm.moonObject = moonObj;
        wm.moonController = moonCtrl;
        wm.sunlight = GameObject.Find("Directional Light")?.GetComponent<Light>();
        wm.engineWindZone = Object.FindFirstObjectByType<WindZone>();
        wm.rainParticles = GameObject.Find("Rain")?.GetComponent<ParticleSystem>();
        wm.roadRenderer = GameObject.Find("Road")?.GetComponent<MeshRenderer>();
        wm.roadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/RoadMaterial.mat");

        GameObject wPanel = GameObject.Find("WeatherReportPanel");
        if (wPanel != null)
        {
            wm.weatherPanel = wPanel;
            wm.weatherText = wPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        wm.cityInput = Object.FindFirstObjectByType<TMP_InputField>();
        wm.searchButton = Object.FindFirstObjectByType<Button>();
        Debug.Log("[Setup] WeatherManager references wired up successfully.");

        // 11. Save scene and project assets
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(">>> Smart City Scene Setup Completed Successfully! Saved: " + saved);
    }
}
#endif
