using UnityEngine;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Borders (order: -X, +X, -Z, +Z)")]
    public Transform[] borders; // 4 elements

    [Header("Obstacles")]
    public GameObject[] obstacles; // Place all obstacles in scene, disable in Inspector

    [Header("Agents")]
    public EnemyShipAgent[] agents; // Assign all agent instances in scene

    private Vector3[] agentStartPositions;
    private Quaternion[] agentStartRotations;
    private bool[] agentWasActive; // Track which agents were active in previous reset

    private void Start()
    {
        // Store original positions and rotations as spawn points
        agentStartPositions = new Vector3[agents.Length];
        agentStartRotations = new Quaternion[agents.Length];
        agentWasActive = new bool[agents.Length]; // Initialize tracking array
        
        for (int i = 0; i < agents.Length; i++)
        {
            // Deactivate all agents initially
            agents[i].gameObject.SetActive(false);
            agentStartPositions[i] = agents[i].transform.position;
            agentStartRotations[i] = agents[i].transform.rotation;
            agentWasActive[i] = false; // Initially none were active
        }
        ResetEnvironment();
    }

    public void ResetEnvironment()
    {
        // Read curriculum parameters
        int numAgents = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("num_agents", 2);
        int numObstacles = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("num_obstacles", 0);
        float arenaSize = Academy.Instance.EnvironmentParameters.GetWithDefault("arena_size", 100f);

        // Move borders
        if (borders.Length == 4)
        {
            borders[0].localPosition = new Vector3(-arenaSize, borders[0].localPosition.y, 0); // -X
            borders[1].localPosition = new Vector3(arenaSize, borders[1].localPosition.y, 0);  // +X
            borders[2].localPosition = new Vector3(0, borders[2].localPosition.y, -arenaSize); // -Z
            borders[3].localPosition = new Vector3(0, borders[3].localPosition.y, arenaSize);  // +Z
        }

        // Activate obstacles
        for (int i = 0; i < obstacles.Length; i++)
            obstacles[i].SetActive(i < numObstacles);

        // Activate agents and set positions for newly activated ones only
        for (int i = 0; i < agents.Length; i++)
        {
            bool shouldBeActive = i < numAgents;
            bool wasActive = agentWasActive[i];
            
            // Only reset position if this agent is newly activated
            if (shouldBeActive && !wasActive)
            {
                agents[i].transform.position = agentStartPositions[i];
                agents[i].transform.rotation = agentStartRotations[i];
                Debug.Log($"Newly activated agent {agents[i].name} - reset to starting position");
            }
            
            // Update active state
            agents[i].gameObject.SetActive(shouldBeActive);
            
            // Update tracking
            agentWasActive[i] = shouldBeActive;
        }
    }
}