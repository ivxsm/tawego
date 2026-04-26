using UnityEngine;

public class bird_script : MonoBehaviour
{
    public Rigidbody2D myRigidbody;  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) == true)
        {
            myRigidbody.linearVelocity = Vector2.up * 10;
        }
        
    }
}
