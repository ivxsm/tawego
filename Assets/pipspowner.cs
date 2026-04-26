using UnityEngine;

public class pipspowner : MonoBehaviour
{
    public GameObject pipe;
    public float spawnRate = 2;
    private float timer = 0;
    private float heightOffset = 10; 
    void Start()
    {
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
