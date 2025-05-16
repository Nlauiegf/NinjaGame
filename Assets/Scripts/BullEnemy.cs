using UnityEngine;

public class BullEnemy : MonoBehaviour
{
    public int health = 3; // Set this in the Inspector or via damage logic
    public float detectionRadius = 6f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 3f;
    private bool isDead = false;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 dashDirection;
    private int groundLayer;
    private int bullLayer;
    private float liftAmount = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Prevent any rotation
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        groundLayer = LayerMask.NameToLayer("Ground");
        bullLayer = gameObject.layer;
    }

    void Update()
    {
        if (isDead) return;
        if (health <= 0)
        {
            OnDeath();
            return;
        }
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = Vector2.zero;
                cooldownTimer = dashCooldown;
                // Move bull back down to original height
                transform.position -= Vector3.up * liftAmount;
            }
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= detectionRadius)
                {
                    Vector2 toPlayer = player.position - transform.position;
                    dashDirection = new Vector2(toPlayer.x, 0f).normalized;
                    isDashing = true;
                    dashTimer = dashDuration;
                    // Hop up a bit for visual effect
                    transform.position += Vector3.up * liftAmount;
                }
            }
        }
    }

    // Call this method to deal damage to the bull
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        health -= amount;
    }

    public void OnDeath()
    {
        isDead = true;
        PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>();
        if (player != null)
        {
            player.CanDash = true;
            Debug.Log("Player can now dash! (Bull killed)");
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            var playerSM = collision.gameObject.GetComponent<PlayerStateMachine>();
            if (playerSM != null)
            {
                playerSM.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
            }
        }
        else if (isDashing)
        {
            isDashing = false;
            rb.linearVelocity = Vector2.zero;
            cooldownTimer = dashCooldown;
            // Move bull back down to original height if dash interrupted
            transform.position -= Vector3.up * liftAmount;
        }
    }
} 