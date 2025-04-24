using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform;
    Vector3 offset;
    private bool hasFoundPlayer = false;

    private void Update() {
        if (!hasFoundPlayer) {
            GameObject player = GameObject.Find("Player Ninja(Clone)");
            if (player != null) {
                playerTransform = player.GetComponent<Transform>();
                if (playerTransform != null) {
                    offset = transform.position - playerTransform.position;
                    hasFoundPlayer = true;
                    Debug.Log("Found player and set up camera following");
                }
            }
            return;
        }
        
        if (playerTransform != null) {
            Vector3 targetPosition = playerTransform.position + offset;
            transform.position = targetPosition;
            Debug.Log($"Camera position: {transform.position}, Player position: {playerTransform.position}");
        }
    }
}
