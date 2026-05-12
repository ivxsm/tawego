using UnityEngine;

public class pipspowner : MonoBehaviour
{
    public GameObject pipe;
    public float spawnRate = 2;
    private float timer = 0;
    private float heightOffset = 10; 

    void Start()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty", 1);
        
        if (difficulty == 1) // Easy
        {
            spawnRate = 3.0f; // More horizontal space between pipes
        }
        else if (difficulty == 2) // Hard
        {
            spawnRate = 1.65f; // Still hard, but gives a bit more horizontal space
        }

        spawnPipe();       
    }

    // Update is called once per frame
    void Update()
    {
        if(timer < spawnRate){
            timer = timer + Time.deltaTime;
        }
        else{
            timer = 0;
            spawnPipe();
        }
    }

    void spawnPipe(){
        float highestPoint = transform.position.y + heightOffset;
        float lowestPoint = transform.position.y - heightOffset;
        float randomY = Random.Range(lowestPoint, highestPoint); 

        Instantiate(pipe, new Vector3(transform.position.x, randomY, 0), transform.rotation);  
    }
}
