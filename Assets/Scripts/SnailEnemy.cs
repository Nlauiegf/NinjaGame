using UnityEngine;

public class SnailEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float patrolRadius = 3f;
    public float rotationSpeed = 180f; // Degrees per second
    public float pauseTime = 1f; // Time to pause at each end

    private Vector3 startPosition;
    private float currentPauseTime;
    private bool isPaused;
    private bool movingRight = true;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Ensure Rigidbody2D is set up correctly
        if (rb != null)
        {
            rb.gravityScale = 0f; // Snails don't need gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation by physics
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
        }
    }

    private void Update()
    {
        if (isPaused)
        {
            currentPauseTime -= Time.deltaTime;
            if (currentPauseTime <= 0)
            {
                isPaused = false;
                movingRight = !movingRight;
                
                // Rotate the snail
                StartCoroutine(RotateSnail());
            }
            return;
        }

        // Calculate target position
        float targetX = startPosition.x + (movingRight ? patrolRadius : -patrolRadius);
        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Update sprite direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !movingRight;
        }

        // Check if we've reached the target
        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
        {
            isPaused = true;
            currentPauseTime = pauseTime;
        }

        // Update animation
        if (animator != null)
        {
            animator.SetBool("IsMoving", !isPaused);
        }
    }

    private System.Collections.IEnumerator RotateSnail()
    {
        float targetRotation = movingRight ? 0f : 180f;
        float currentRotation = transform.eulerAngles.y;
        
        while (Mathf.Abs(currentRotation - targetRotation) > 0.1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0, currentRotation, 0);
            yield return null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit the player
        if (collision.gameObject.CompareTag("Player"))
        {
            var playerSM = collision.gameObject.GetComponent<PlayerStateMachine>();
            if (playerSM != null)
            {
                playerSM.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
            }
        }
        // If we hit anything else, change direction
        else
        {
            movingRight = !movingRight;
            StartCoroutine(RotateSnail());
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the patrol radius in the editor
        Gizmos.color = Color.yellow;
        Vector3 leftPoint = transform.position + Vector3.left * patrolRadius;
        Vector3 rightPoint = transform.position + Vector3.right * patrolRadius;
        Gizmos.DrawLine(leftPoint, rightPoint);
        Gizmos.DrawWireSphere(leftPoint, 0.2f);
        Gizmos.DrawWireSphere(rightPoint, 0.2f);
    }
} 