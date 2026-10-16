using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Dynamics")]
    [Tooltip("Target cruise speed in m/s")]
    public float cruiseSpeed = 9.0f;
    public float acceleration = 6.0f;
    public float braking = 12.0f;
    public float turnSpeed = 7.0f;

    [Header("Wheels")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;
    public float wheelRadius = 0.3f;

    [Header("Collision & Safety")]
    public float stopDistance = 4.2f;
    public float slowDistance = 8.0f;
    public LayerMask obstacleLayers = ~0;

    [Header("Headlights")]
    public Light headlightLeft;
    public Light headlightRight;
    public float nightHeadlightIntensity = 1.8f;

    [Header("Route Settings")]
    public int startWaypointIndex = 0;

    private Vector3[] waypoints;
    private int currentWaypointIndex;
    private float currentSpeed;
    private bool isBrakingForObstacle;

    private void Awake()
    {
        // Auto-discover wheels if unassigned
        if (wheelFL == null) wheelFL = transform.Find("Wheel_FL");
        if (wheelFR == null) wheelFR = transform.Find("Wheel_FR");
        if (wheelRL == null) wheelRL = transform.Find("Wheel_RL");
        if (wheelRR == null) wheelRR = transform.Find("Wheel_RR");

        // Define road looping waypoints
        // Road spans X = -30 to +30, centered at Z = -5.
        // Lane 1 (+X direction): Z = -5.8f
        // Lane 2 (-X direction): Z = -4.2f
        waypoints = new Vector3[]
        {
            new Vector3(-26.0f, 0.01f, -5.8f),
            new Vector3( 26.0f, 0.01f, -5.8f),
            new Vector3( 27.5f, 0.01f, -5.0f),
            new Vector3( 26.0f, 0.01f, -4.2f),
            new Vector3(-26.0f, 0.01f, -4.2f),
            new Vector3(-27.5f, 0.01f, -5.0f)
        };

        currentWaypointIndex = Mathf.Clamp(startWaypointIndex, 0, waypoints.Length - 1);

        SetupHeadlights();
    }

    private void SetupHeadlights()
    {
        if (headlightLeft == null)
        {
            Transform hl = transform.Find("Headlight_Left");
            if (hl == null)
            {
                GameObject obj = new GameObject("Headlight_Left");
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = new Vector3(1.7f, 0.45f, 0.5f);
                headlightLeft = obj.AddComponent<Light>();
                headlightLeft.type = LightType.Spot;
                headlightLeft.spotAngle = 48f;
                headlightLeft.range = 14f;
                headlightLeft.color = new Color(1.0f, 0.95f, 0.85f);
                headlightLeft.intensity = nightHeadlightIntensity;
                obj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            }
            else
            {
                headlightLeft = hl.GetComponent<Light>();
            }
        }

        if (headlightRight == null)
        {
            Transform hr = transform.Find("Headlight_Right");
            if (hr == null)
            {
                GameObject obj = new GameObject("Headlight_Right");
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = new Vector3(1.7f, 0.45f, -0.5f);
                headlightRight = obj.AddComponent<Light>();
                headlightRight.type = LightType.Spot;
                headlightRight.spotAngle = 48f;
                headlightRight.range = 14f;
                headlightRight.color = new Color(1.0f, 0.95f, 0.85f);
                headlightRight.intensity = nightHeadlightIntensity;
                obj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            }
            else
            {
                headlightRight = hr.GetComponent<Light>();
            }
        }
    }

    private void Update()
    {
        CheckObstaclesAhead();
        DriveAlongRoute();
        RotateWheels();
        UpdateHeadlights();
    }

    private void CheckObstaclesAhead()
    {
        isBrakingForObstacle = false;
        Vector3 origin = transform.position + Vector3.up * 0.4f + transform.forward * 1.5f;

        RaycastHit hit;
        // Cast forward ray to detect pedestrians or other cars
        if (Physics.SphereCast(origin, 0.6f, transform.forward, out hit, slowDistance, obstacleLayers))
        {
            // Don't stop for ground/road or self
            if (hit.transform != transform && !hit.transform.IsChildOf(transform) && hit.transform.name != "Road")
            {
                if (hit.distance < stopDistance)
                {
                    isBrakingForObstacle = true;
                }
            }
        }
    }

    private void DriveAlongRoute()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetPos = waypoints[currentWaypointIndex];
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0;

        float distance = toTarget.magnitude;

        // Waypoint arrival check
        if (distance < 1.8f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            targetPos = waypoints[currentWaypointIndex];
            toTarget = targetPos - transform.position;
            toTarget.y = 0;
        }

        // Speed adjustment
        float desiredSpeed = isBrakingForObstacle ? 0f : cruiseSpeed;

        if (currentSpeed < desiredSpeed)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, desiredSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, desiredSpeed, braking * Time.deltaTime);
        }

        // Smoothly steer toward the next waypoint
if (toTarget.sqrMagnitude > 0.01f)
{
    Quaternion targetRot =
        Quaternion.LookRotation(toTarget.normalized);

    // Adjust for the car model's local +X forward direction
    targetRot *= Quaternion.Euler(0f, -90f, 0f);

    transform.rotation = Quaternion.RotateTowards(
        transform.rotation,
        targetRot,
        turnSpeed * 30f * Time.deltaTime
    );
}
  

        // CHANGED: Move directly toward the waypoint
        if (toTarget.sqrMagnitude > 0.01f)
        {
            transform.position +=
                toTarget.normalized * (currentSpeed * Time.deltaTime);
        }
    }

    private void RotateWheels()
    {
        if (Mathf.Abs(currentSpeed) < 0.01f) return;

        float spinAngle =
            (currentSpeed * Time.deltaTime /
            (2f * Mathf.PI * wheelRadius)) * 360f;

        RotateWheel(wheelFL, spinAngle);
        RotateWheel(wheelFR, spinAngle);
        RotateWheel(wheelRL, spinAngle);
        RotateWheel(wheelRR, spinAngle);
    }

    private void RotateWheel(Transform wheel, float angle)
    {
        if (wheel == null) return;

        wheel.Rotate(0f, angle, 0f, Space.Self);
    }

    private void UpdateHeadlights()
    {
        bool isNight = false;
        bool isDarkStorm = false;

        if (WeatherManager.Instance != null)
        {
            isNight = WeatherManager.Instance.IsNight;
            isDarkStorm = WeatherManager.Instance.IsThunderstorm;
        }

        bool lightsOn = isNight || isDarkStorm;
        float targetIntensity = lightsOn ? nightHeadlightIntensity : 0f;

        if (headlightLeft != null)
        {
            headlightLeft.enabled = targetIntensity > 0.01f;
            headlightLeft.intensity = Mathf.MoveTowards(
                headlightLeft.intensity, targetIntensity, Time.deltaTime * 3f);
        }

        if (headlightRight != null)
        {
            headlightRight.enabled = targetIntensity > 0.01f;
            headlightRight.intensity = Mathf.MoveTowards(
                headlightRight.intensity, targetIntensity, Time.deltaTime * 3f);
        }
    }
}