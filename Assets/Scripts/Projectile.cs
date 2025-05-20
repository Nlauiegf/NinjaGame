using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3f; // Projectile will be destroyed after this many seconds

    private void Start()
    {
        // Destroy the projectile after lifetime seconds
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collision with the player
        if (other.GetComponent<PlayerStateMachine>() != null)
        {
            return;
        }

        // Ignore collision with portals
        if (other.GetComponent<Portal>() != null)
        {
            return;
        }

        // Ignore collision with coins
        if (other.GetComponent<Coin>() != null)
        {
            return;
        }

        // Check if we hit an enemy by looking for the BullEnemy component
        var enemy = other.GetComponent<BullEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Hit enemy! Dealt {damage} damage.");
        }

        // Destroy the projectile on any collision (except with player, portals, and coins)
        Destroy(gameObject);
    }
} 