using UnityEngine;

public class BullEnemy : MonoBehaviour
{
    public int health = 3; // Set this in the Inspector or via damage logic
    private bool isDead = false;

    void Update()
    {
        if (!isDead && health <= 0)
        {
            OnDeath();
        }
    }

    // Call this method to deal damage to the bull
    public void TakeDamage(int amount)
    {
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
} 