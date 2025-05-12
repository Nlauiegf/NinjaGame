using UnityEngine;

public class Portal : MonoBehaviour
{
    public string targetLevelFileName = "Bonus Level.png"; // Set this in the Inspector to the desired level file

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player Ninja"))
        {
            // Find the LevelLoader in the scene
            LevelLoader loader = FindObjectOfType<LevelLoader>();
            if (loader != null)
            {
                loader.levelFileName = targetLevelFileName;
                loader.SendMessage("LoadMap"); // Call LoadMap to load the new level
            }
            else
            {
                Debug.LogError("No LevelLoader found in the scene!");
            }
        }
    }
}
//asdasd