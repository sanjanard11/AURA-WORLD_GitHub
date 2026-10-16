using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Patrol Path (On Sidewalk)")]
    [Tooltip("Sidewalk Z coordinate to keep pedestrians away from the road")]
    public float sidewalkZ = -2.6f;
    public float minX = -12.0f;
    public float maxX = 12.0f;
    public float walkSpeed = 1.35f;

    [Header("Procedural Humanoid Hierarchy")]
    public Transform torso;
    public Transform head;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;
    public GameObject umbrellaObject;

    [Header("Shelter In Thunderstorm")]
    public Vector3 shelterLocation = new Vector3(0f, 0.05f, -1.8f); // Porch of house
    public bool isSeekingShelter;

    private int direction = 1;
    private float walkTimer;
    private bool isUnderShelter;
    private Vector3 resumePatrolPosition;

    private void Awake()
    {
        // Auto-discover humanoid parts if unassigned
        if (torso == null) torso = transform.Find("Torso");
        if (head == null) head = transform.Find("Head");
        if (leftLeg == null) leftLeg = transform.Find("LeftLeg");
        if (rightLeg == null) rightLeg = transform.Find("RightLeg");
        if (leftArm == null) leftArm = transform.Find("LeftArm");
        if (rightArm == null) rightArm = transform.Find("RightArm");

        if (umbrellaObject == null)
        {
            Transform u = transform.Find("Umbrella");
            if (u != null) umbrellaObject = u.gameObject;
        }

        // Initialize position on designated sidewalk
        Vector3 p = transform.position;
        p.z = sidewalkZ;
        transform.position = p;
        resumePatrolPosition = p;

        // Random starting direction and phase
        direction = Random.value > 0.5f ? 1 : -1;
        walkTimer = Random.Range(0f, 10f);
    }

    private void Update()
    {
        bool isRaining = false;
        bool isStorm = false;

        if (WeatherManager.Instance != null)
        {
            isRaining = WeatherManager.Instance.IsRaining;
            isStorm = WeatherManager.Instance.IsThunderstorm;
        }

        // 1. Umbrella visibility responds to rain
        if (umbrellaObject != null)
        {
            // Show umbrella in rain or drizzle, fold/hide in clear weather
            bool shouldHoldUmbrella = isRaining || isStorm;
            if (umbrellaObject.activeSelf != shouldHoldUmbrella)
            {
                umbrellaObject.SetActive(shouldHoldUmbrella);
            }
        }

        // 2. Storm shelter behavior
        if (isStorm)
        {
            if (!isSeekingShelter)
            {
                isSeekingShelter = true;
                resumePatrolPosition = transform.position;
            }

            // Move towards shelter
            Vector3 toShelter = shelterLocation - transform.position;
            toShelter.y = 0;

            if (toShelter.magnitude > 0.4f)
            {
                isUnderShelter = false;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toShelter.normalized), Time.deltaTime * 6f);
                transform.position += transform.forward * (walkSpeed * 1.3f * Time.deltaTime);
                AnimateWalkCycle(walkSpeed * 1.3f);
            }
            else
            {
                // Reached shelter: pause and stay safe under the roof
                isUnderShelter = true;
                ResetPose();
            }
            return;
        }
        else if (isSeekingShelter)
        {
            // Storm ended: return to sidewalk
            Vector3 toSidewalk = resumePatrolPosition - transform.position;
            toSidewalk.y = 0;

            if (toSidewalk.magnitude > 0.4f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toSidewalk.normalized), Time.deltaTime * 6f);
                transform.position += transform.forward * (walkSpeed * Time.deltaTime);
                AnimateWalkCycle(walkSpeed);
                return;
            }
            else
            {
                isSeekingShelter = false;
                isUnderShelter = false;
            }
        }

        // 3. Normal sidewalk patrol
        Vector3 pos = transform.position;
        pos.z = sidewalkZ; // strictly lock to sidewalk

        // Reverse at sidewalk boundaries
        if (pos.x >= maxX)
        {
            direction = -1;
        }
        else if (pos.x <= minX)
        {
            direction = 1;
        }

        Vector3 targetMove = new Vector3(direction, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetMove), Time.deltaTime * 8f);
        transform.position = pos + targetMove * (walkSpeed * Time.deltaTime);

        AnimateWalkCycle(walkSpeed);
    }

    private void AnimateWalkCycle(float currentSpeed)
    {
        walkTimer += Time.deltaTime * currentSpeed * 4.5f;

        float legSwing = Mathf.Sin(walkTimer) * 26.0f;
        float armSwing = Mathf.Sin(walkTimer) * 20.0f;
        float torsoBob = Mathf.Abs(Mathf.Sin(walkTimer * 2f)) * 0.04f;

        // Swing legs back and forth
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

        // Swing arms in opposition to legs
        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-armSwing, 0f, 0f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(armSwing, 0f, 0f);

        // Subtle torso vertical stride bob
        if (torso != null)
        {
            Vector3 lp = torso.localPosition;
            lp.y = 0.85f + torsoBob;
            torso.localPosition = lp;
        }
    }

    private void ResetPose()
    {
        if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
        if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
        if (leftArm != null) leftArm.localRotation = Quaternion.identity;
        if (rightArm != null) rightArm.localRotation = Quaternion.identity;
    }
}
