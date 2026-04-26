using UnityEngine;

public class pipe : MonoBehaviour
{
    public float moveSpeed = 5;
    private float deadZone = -15;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty", 1);
        
        if (difficulty == 1) // Easy Mode
        {
            moveSpeed = 4f; // Slower speed
            AdjustGap(1.5f); // Increase gap
        }
        else if (difficulty == 2) // Hard Mode
        {
            moveSpeed = 8f; // Faster speed
            AdjustGap(-1.0f); // Decrease gap
        }
    }

    void AdjustGap(float offsetAmount)
    {
        // Find top and bottom pipe children to adjust the space between them
        if (transform.childCount >= 2)
        {
            Transform child1 = transform.GetChild(0);
            Transform child2 = transform.GetChild(1);
            
            // Determine which is the top pipe based on local Y position
            Transform topPipe = child1.localPosition.y > child2.localPosition.y ? child1 : child2;
            Transform bottomPipe = child1.localPosition.y > child2.localPosition.y ? child2 : child1;
            
            // Move them to increase or decrease the gap
            topPipe.localPosition += new Vector3(0, offsetAmount, 0);
            bottomPipe.localPosition -= new Vector3(0, offsetAmount, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + (Vector3.left * moveSpeed) * Time.deltaTime;

        if(transform.position.x < deadZone)
        {
            Destroy(gameObject);
        }
    }
}
