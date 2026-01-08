using UnityEngine;
using UnityEngine.SceneManagement; // Needed if you add a 'Quit' or 'Restart' function

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI; // Reference to your Pause Menu Panel

    // State variable to track if the game is paused
    public static bool isPaused = false;

    void Update()
    {
        // Optional: Toggle pause with the Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Hide the menu
        Time.timeScale = 1f;          // Resume game time
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);  // Show the menu
        Time.timeScale = 0f;          // Freeze game time
        isPaused = true;
    }

    public void QuitGame()
    {
       Time.timeScale = 1f;
        
        // 2. Load the scene by its exact name
        // Make sure your homepage scene is actually named "MainMenu" (or change this string)
        SceneManager.LoadScene("MainMenu");
    }
}