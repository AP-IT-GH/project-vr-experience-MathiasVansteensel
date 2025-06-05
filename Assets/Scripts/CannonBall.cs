using UnityEngine;

public class Cannonball : MonoBehaviour
{
    public EnemyShipAgent shooter; // Set this when firing
    public Transform target; 

    private void OnCollisionEnter(Collision collision)
    {
        var hitAgent = collision.gameObject.GetComponent<EnemyShipAgent>();
        if (hitAgent != null && shooter != null && hitAgent.name == target.name)
        {
            Debug.Log($"Cannonball hit {hitAgent.name} fired by {shooter.name}");
            shooter.OnHitOpponent();
            hitAgent.OnHitByOpponent();
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Invoke(nameof(DestroyAfterTimeout), 20f);
    }

    private void Update()
    {
        if (transform.position.y < -1f)
        {
            if (shooter != null)
            {
                shooter.OnMissedShot();
            }
            Destroy(gameObject);
        }
    }

    private void DestroyAfterTimeout()
    {
        if (shooter != null)
        {
            shooter.OnMissedShot();
        }
        Destroy(gameObject);
    }
}