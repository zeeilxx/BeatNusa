using System.Collections;
using UnityEngine;

public class MapMovement : MonoBehaviour
{
    public Transform[] waypoints;       // Array of main waypoints
    public Transform[] branchWaypoints; // Array of branch waypoints (null for no branches)
    public float moveSpeed = 5f;        // Movement speed
    public KeyCode downKey = KeyCode.D; // Key for moving forward
    public KeyCode rightKey = KeyCode.W; // Key for moving to the branch
    public KeyCode spawnKey = KeyCode.Return; // Enter key
    public GameObject[] prefabToSpawn;  // Prefabs to spawn

    private int currentWaypointIndex = 0; // Index of the current waypoint
    private bool isMoving = false;        // Is the player currently moving?

    void Awake()
    {
        // Optionally reset PlayerPrefs here for testing
        // PlayerPrefs.SetInt("Level1Completed", 0); 
        // PlayerPrefs.SetInt("Level2Completed", 0);
        // PlayerPrefs.Save();
    }

    void Start()
    {
        int waypointIndex = PlayerPrefs.GetInt("WaypointIndex", 0);
        Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 3f, 10f));  // Ensure Z is positive and properly in front of the camera

        if (waypointIndex == 0)
        {
            SpawnPrefabAt(screenCenter, 4);
        }

        if (waypointIndex < 0 || waypointIndex >= waypoints.Length)
        {
            Debug.LogWarning($"Invalid waypoint index ({waypointIndex}). Defaulting to 0.");
            waypointIndex = 0;
            PlayerPrefs.SetInt("WaypointIndex", waypointIndex); // Reset the index
            PlayerPrefs.Save();
        }

        Transform targetWaypoint = waypoints[waypointIndex];
        transform.position = new Vector3(
            targetWaypoint.position.x,
            targetWaypoint.position.y,
            transform.position.z // Keep the current z position
        );

        currentWaypointIndex = waypointIndex; // Update the current waypoint index
        Debug.Log($"Starting at waypoint index: {waypointIndex}");
    }

    void Update()
    {
        if (isMoving) return;

        // Move forward to the next waypoint
        if (Input.GetKeyDown(downKey) && currentWaypointIndex < waypoints.Length - 1)
        {
            // Check if the player has completed the required level to move forward
            if (currentWaypointIndex == 2 && PlayerPrefs.GetInt("Level1Completed", 0) == 0)
            {
                Debug.Log("You must complete level 1 before moving to waypoint 3.");
                return; // Prevent moving to next level if Level 1 is not completed
            }

            if (currentWaypointIndex == 3 && PlayerPrefs.GetInt("Level2Completed", 0) == 0)
            {
                Debug.Log("You must complete level 2 before moving to waypoint 4.");
                return; // Prevent moving to next level if Level 2 is not completed
            }

            Debug.Log($"Current Waypoint Index: {currentWaypointIndex} -> Moving to {currentWaypointIndex + 1}");
            MoveToWaypoint(currentWaypointIndex + 1); // Move forward
        }

        // Move to a branch waypoint if available
        if (Input.GetKeyDown(rightKey) && branchWaypoints[currentWaypointIndex] != null)
        {
            Debug.Log($"Branching at Waypoint Index: {currentWaypointIndex}");
            MoveToBranchWaypoint(branchWaypoints[currentWaypointIndex]); // Move to branch
        }

        // Spawn prefab when Enter key is pressed
        if (Input.GetKeyDown(spawnKey))
        {
            Debug.Log("Spawn key pressed. Checking for prefab to spawn.");
            HandlePrefabSpawn(currentWaypointIndex);

            // **Check if we're at a branch waypoint and handle spawning for that**
            if (branchWaypoints[currentWaypointIndex] != null)  // Ensure you're at a branch waypoint
            {
                HandlePrefabSpawnForBranch(branchWaypoints[currentWaypointIndex]);
            }
        }
    }

    void MoveToWaypoint(int newWaypointIndex)
    {
        StartCoroutine(Move(waypoints[newWaypointIndex], newWaypointIndex));
    }

    void MoveToBranchWaypoint(Transform branchWaypoint)
    {
        if (branchWaypoint == null)
        {
            Debug.LogWarning("Branch waypoint is null. Cannot move.");
            return;
        }

        StartCoroutine(Move(branchWaypoint, -1)); // Use -1 as it's not part of the main sequence
    }

    IEnumerator Move(Transform targetWaypoint, int newWaypointIndex)
    {
        isMoving = true;

        Vector3 targetPosition = new Vector3(
            targetWaypoint.position.x,
            targetWaypoint.position.y,
            transform.position.z
        );

        while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;

        if (newWaypointIndex != -1)
        {
            Debug.Log($"Arrived at Waypoint Index: {newWaypointIndex}");
            currentWaypointIndex = newWaypointIndex; // Update current waypoint index for main sequence
        }

        isMoving = false;
    }

    void HandlePrefabSpawn(int waypointIndex)
    {
        if (prefabToSpawn == null || prefabToSpawn.Length == 0)
        {
            Debug.LogWarning("Prefab array is empty or not assigned.");
            return;
        }
        Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 3f, 10f));  // Ensure Z is positive and properly in front of the camera

        switch (waypointIndex)
        {
            case 2:
                SpawnPrefabAt(screenCenter, 0);  // Spawn the first prefab
                break;

            case 3:
                if (prefabToSpawn.Length > 1)
                {
                    SpawnPrefabAt(screenCenter, 1);  // Spawn the second prefab
                }
                break;

            case 4:
                if (prefabToSpawn.Length > 2)
                {
                    SpawnPrefabAt(screenCenter, 3);  // Spawn the fourth prefab
                }
                break;
        }
    }

    void HandlePrefabSpawnForBranch(Transform branchWaypoint)
    {
        if (prefabToSpawn == null || prefabToSpawn.Length <= 2)
        {
            Debug.LogWarning("Not enough prefabs in the array to spawn at branch waypoint.");
            return;
        }

        Debug.Log($"Attempting to spawn prefab at branch waypoint: {branchWaypoint.name}");
        Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 3f, 10f)); // Ensure Z is positive
        GameObject spawnedPrefab = Instantiate(prefabToSpawn[2], screenCenter, Quaternion.identity);
        spawnedPrefab.transform.position = new Vector3(spawnedPrefab.transform.position.x, spawnedPrefab.transform.position.y, -8f); // Set Z properly

        Debug.Log($"Prefab spawned at position {screenCenter}");
    }

    void SpawnPrefabAt(Vector3 position, int prefabIndex)
    {
        position.z = -8f; // Ensure visibility in front of the camera
        GameObject spawnedPrefab = Instantiate(prefabToSpawn[prefabIndex], position, Quaternion.identity);
        Debug.Log($"Prefab spawned at index {prefabIndex} at position {position}");
    }

    void OnDrawGizmos()
    {
        // Draw main waypoints
        if (waypoints != null && waypoints.Length > 1)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 0.2f);
        }

        // Draw branch waypoints
        if (branchWaypoints != null)
        {
            for (int i = 0; i < branchWaypoints.Length; i++)
            {
                if (branchWaypoints[i] != null && waypoints[i] != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(waypoints[i].position, branchWaypoints[i].position);
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(branchWaypoints[i].position, 0.2f);
                }
            }
        }
    }
}
