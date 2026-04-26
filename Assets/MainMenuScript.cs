using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    // 1 for Easy, 2 for Hard
    // Default to Easy if not set
    private void Start()
    {
        if (!PlayerPrefs.HasKey("Difficulty"))
        {
            PlayerPrefs.SetInt("Difficulty", 1);
        }
    }

    public void SetEasyLevel()
    {
        PlayerPrefs.SetInt("Difficulty", 1);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to Easy");
    }

    public void SetHardLevel()
    {
        PlayerPrefs.SetInt("Difficulty", 2);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to Hard");
    }

    public void PlayGame()
    {
        // Assuming your game scene is at index 1 or has a specific name.
        // Change "SampleScene" if your game scene is named differently!
        SceneManager.LoadScene("SampleScene"); 
    }
}
