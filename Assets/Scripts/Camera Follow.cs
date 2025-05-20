using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform;
    Vector3 offset;
    private bool hasFoundPlayer = false;

    public void ResetPlayerTarget()
    {
        hasFoundPlayer = false;
    }

    private void Update() {
        if (!hasFoundPlayer) {
            GameObject player = GameObject.Find("Player Ninja(Clone)");
            if (player != null) {
                playerTransform = player.GetComponent<Transform>();
                if (playerTransform != null) {
                    // Set camera position relative to player
                    Vector3 desiredPosition = playerTransform.position + new Vector3(5f, 2.5f, -15f);
                    transform.position = desiredPosition;
                    offset = transform.position - playerTransform.position;
                    hasFoundPlayer = true;
                    Debug.Log("Found player and set up camera following (relocated camera with custom offset)");
                }
            }
            return;
        }
        
        if (playerTransform != null) {
            Vector3 targetPosition = playerTransform.position + offset;
            transform.position = targetPosition;
        }
    }
}
