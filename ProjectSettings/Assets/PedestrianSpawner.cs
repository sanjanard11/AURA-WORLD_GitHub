using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    [Header("Pedestrian Settings")]
    public int pedestrianCount = 4;
    public Material skinMaterial;
    public Material[] clothesMaterials;
    public Material[] umbrellaMaterials;

    private void Start()
    {
        // Check if pedestrians already exist in scene
        PedestrianController[] existing = Object.FindObjectsByType<PedestrianController>(FindObjectsSortMode.None);
        if (existing != null && existing.Length >= pedestrianCount)
        {
            return;
        }

        SpawnPedestrians();
    }

    public void SpawnPedestrians()
    {
        // Near sidewalk is at Z = -2.6f
        // Far sidewalk is at Z = -7.4f
        float[] sidewalkZs = new float[] { -2.6f, -7.4f };
        float[] startXs = new float[] { -8f, -2f, 3f, 8f };

        Color[] defaultCoatColors = new Color[]
        {
            new Color(0.2f, 0.45f, 0.75f), // Blue coat
            new Color(0.8f, 0.3f, 0.25f),  // Red coat
            new Color(0.3f, 0.65f, 0.4f),  // Green coat
            new Color(0.85f, 0.7f, 0.2f)   // Yellow raincoat
        };

        Color[] defaultUmbrellaColors = new Color[]
        {
            new Color(0.15f, 0.15f, 0.18f), // Classic charcoal
            new Color(0.75f, 0.2f, 0.2f),   // Crimson
            new Color(0.2f, 0.5f, 0.8f),    // Royal blue
            new Color(0.95f, 0.85f, 0.1f)   // Bright yellow
        };

        for (int i = 0; i < pedestrianCount; i++)
        {
            float z = sidewalkZs[i % sidewalkZs.Length];
            float x = startXs[i % startXs.Length];
            Vector3 spawnPos = new Vector3(x, 0.05f, z);

            Color coatCol = defaultCoatColors[i % defaultCoatColors.Length];
            Color umbrellaCol = defaultUmbrellaColors[i % defaultUmbrellaColors.Length];

            CreatePedestrian("Pedestrian_" + (i + 1), spawnPos, z, coatCol, umbrellaCol);
        }
    }

    private GameObject CreatePedestrian(string name, Vector3 pos, float sidewalkZ, Color coatColor, Color umbrellaColor)
    {
        GameObject pedObj = new GameObject(name);
        pedObj.transform.SetParent(transform, false);
        pedObj.transform.position = pos;

        // Add PedestrianController
        PedestrianController controller = pedObj.AddComponent<PedestrianController>();
        controller.sidewalkZ = sidewalkZ;
        controller.minX = -13f;
        controller.maxX = 13f;
        controller.walkSpeed = Random.Range(1.1f, 1.5f);

        // Assign shelter location based on nearest house
        if (pos.x < -4f) controller.shelterLocation = new Vector3(-10.5f, 0.05f, 0.5f); // House_02 porch
        else if (pos.x > 4f) controller.shelterLocation = new Vector3(10.5f, 0.05f, 0.5f); // House_03 porch
        else controller.shelterLocation = new Vector3(0f, 0.05f, -1.0f); // House_01 porch

        // Materials
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Material coatMat = new Material(litShader);
        coatMat.color = coatColor;
        if (coatMat.HasProperty("_BaseColor")) coatMat.SetColor("_BaseColor", coatColor);

        Material pantsMat = new Material(litShader);
        Color pantsCol = new Color(0.18f, 0.2f, 0.25f);
        pantsMat.color = pantsCol;
        if (pantsMat.HasProperty("_BaseColor")) pantsMat.SetColor("_BaseColor", pantsCol);

        Material skinMat = new Material(litShader);
        Color skinCol = new Color(0.92f, 0.78f, 0.68f);
        skinMat.color = skinCol;
        if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", skinCol);

        Material umbMat = new Material(litShader);
        umbMat.color = umbrellaColor;
        if (umbMat.HasProperty("_BaseColor")) umbMat.SetColor("_BaseColor", umbrellaColor);

        // 1. Torso
        GameObject torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torsoObj.name = "Torso";
        torsoObj.transform.SetParent(pedObj.transform, false);
        torsoObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        torsoObj.transform.localScale = new Vector3(0.42f, 0.52f, 0.26f);
        torsoObj.GetComponent<MeshRenderer>().material = coatMat;
        Object.Destroy(torsoObj.GetComponent<Collider>());
        controller.torso = torsoObj.transform;

        // 2. Head
        GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headObj.name = "Head";
        headObj.transform.SetParent(pedObj.transform, false);
        headObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        headObj.transform.localScale = new Vector3(0.28f, 0.30f, 0.28f);
        headObj.GetComponent<MeshRenderer>().material = skinMat;
        Object.Destroy(headObj.GetComponent<Collider>());
        controller.head = headObj.transform;

        // 3. Legs
        GameObject leftLegObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLegObj.name = "LeftLeg";
        leftLegObj.transform.SetParent(pedObj.transform, false);
        leftLegObj.transform.localPosition = new Vector3(-0.12f, 0.32f, 0f);
        leftLegObj.transform.localScale = new Vector3(0.15f, 0.60f, 0.16f);
        leftLegObj.GetComponent<MeshRenderer>().material = pantsMat;
        Object.Destroy(leftLegObj.GetComponent<Collider>());
        controller.leftLeg = leftLegObj.transform;

        GameObject rightLegObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLegObj.name = "RightLeg";
        rightLegObj.transform.SetParent(pedObj.transform, false);
        rightLegObj.transform.localPosition = new Vector3(0.12f, 0.32f, 0f);
        rightLegObj.transform.localScale = new Vector3(0.15f, 0.60f, 0.16f);
        rightLegObj.GetComponent<MeshRenderer>().material = pantsMat;
        Object.Destroy(rightLegObj.GetComponent<Collider>());
        controller.rightLeg = rightLegObj.transform;

        // 4. Arms
        GameObject leftArmObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftArmObj.name = "LeftArm";
        leftArmObj.transform.SetParent(pedObj.transform, false);
        leftArmObj.transform.localPosition = new Vector3(-0.28f, 0.82f, 0f);
        leftArmObj.transform.localScale = new Vector3(0.12f, 0.46f, 0.14f);
        leftArmObj.GetComponent<MeshRenderer>().material = coatMat;
        Object.Destroy(leftArmObj.GetComponent<Collider>());
        controller.leftArm = leftArmObj.transform;

        GameObject rightArmObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightArmObj.name = "RightArm";
        rightArmObj.transform.SetParent(pedObj.transform, false);
        rightArmObj.transform.localPosition = new Vector3(0.28f, 0.82f, 0f);
        rightArmObj.transform.localScale = new Vector3(0.12f, 0.46f, 0.14f);
        rightArmObj.GetComponent<MeshRenderer>().material = coatMat;
        Object.Destroy(rightArmObj.GetComponent<Collider>());
        controller.rightArm = rightArmObj.transform;

        // 5. 3D Umbrella (held in right hand, dome extends overhead)
        GameObject umbObj = new GameObject("Umbrella");
        umbObj.transform.SetParent(pedObj.transform, false);
        umbObj.transform.localPosition = new Vector3(0.25f, 1.05f, 0.15f);

        // Shaft / handle
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Umbrella_Shaft";
        shaft.transform.SetParent(umbObj.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        shaft.transform.localScale = new Vector3(0.03f, 0.35f, 0.03f);
        Material metalMat = new Material(litShader);
        metalMat.color = Color.gray;
        shaft.GetComponent<MeshRenderer>().material = metalMat;
        Object.Destroy(shaft.GetComponent<Collider>());

        // Canopy dome (flattened sphere)
        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Umbrella_Canopy";
        dome.transform.SetParent(umbObj.transform, false);
        dome.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        dome.transform.localScale = new Vector3(1.15f, 0.38f, 1.15f);
        dome.GetComponent<MeshRenderer>().material = umbMat;
        Object.Destroy(dome.GetComponent<Collider>());

        controller.umbrellaObject = umbObj;
        umbObj.SetActive(false); // starts hidden unless raining

        return pedObj;
    }
}
