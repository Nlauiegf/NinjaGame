using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
    public float rotationSpeed = 100f;
    public float bobSpeed = 1f;
    public float bobHeight = 0.2f;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Rotate the coin
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        
        // Bob up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player collected the coin
        if (other.GetComponent<PlayerStateMachine>() != null)
        {
            // Find the PlayerStateMachine and increment coin count
            PlayerStateMachine player = other.GetComponent<PlayerStateMachine>();
            player.AddCoins(value);
            
            // Play collection effect (you can add particle effects or sounds here)
            
            // Destroy the coin
            Destroy(gameObject);
        }
    }
} 