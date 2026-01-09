using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("HUD")]
    public Slider healthSlider;
    public Slider xpSlider; 
    public TextMeshProUGUI levelText;
    public GameObject gameOverPanel;

    [Header("Upgrade Menu")]
    public GameObject upgradePanel; 
    public Button[] choiceButtons; 
    public TextMeshProUGUI[] buttonTexts; 

    [Header("Visual Feedback")]
    public Color normalColor = Color.green;
    public Color dangerColor = Color.red;
    public Image healthFillImage;
    
    [Header("Win Screen References")]
    public GameObject winPanel;
    public TextMeshProUGUI winTitle;
    public TextMeshProUGUI winDescription;
    public Image winBackgroundPanel;
    
    [Header("Card System")]
    public GameObject cardPrefab;      
    public Transform cardContainer;
    [Header("Audio")]
    public AudioSource uiAudioSource; // Drag the AudioSource from your Game Manager here
    public AudioClip winMusic;        // Drag your Win Music here
    public AudioClip loseMusic;       // Drag your Defeat Music here

    void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        
        // Ensure game starts in a playing state
        ResumeGame();
    }

    public void UpdateXPUI(float current, float max)
    {
        if (xpSlider) xpSlider.value = current / max;
    }

    public void UpdateLevelUI(int level)
    {
        if (levelText != null) levelText.text = "Growth-" + level;
    }

    // --- HELPER FUNCTIONS FOR PAUSING ---
    void PauseGame()
    {
        Time.timeScale = 0f;          // Freezes Game Logic, Physics, and Particles
        AudioListener.pause = true;   // Pauses ALL Audio Sources globally
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;          // Unfreezes Game Logic
        AudioListener.pause = false;  // Unpauses Audio
    }
    // ------------------------------------

    public void ShowUpgradeCards(List<PlayerController.UpgradeOption> options, System.Action<int> onCardSelected)
    {
        // 1. FREEZE EVERYTHING
        PauseGame();

        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        if (upgradePanel) upgradePanel.SetActive(true);
        else gameObject.SetActive(true); 

        for (int i = 0; i < options.Count; i++)
        {
            int index = i; 
            GameObject card = Instantiate(cardPrefab, cardContainer);
            
            Transform iconTransform = card.transform.Find("CardIcon");
            if (iconTransform)
            {
                Image iconImg = iconTransform.GetComponent<Image>();
                if(iconImg) iconImg.sprite = options[i].cardSprite;
            }

            Button btn = card.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.AddListener(() => 
                {
                    // 2. UNFREEZE WHEN CLICKED
                    ResumeGame();
                    
                    onCardSelected(index);
                    if (upgradePanel) upgradePanel.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
        }
    }

   public void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        
        // 1. Play the Music
        if (uiAudioSource && loseMusic)
        {
            uiAudioSource.ignoreListenerPause = true; // IMPORTANT: Allows this to play while game is paused
            uiAudioSource.Stop(); // Stop any previous music
            uiAudioSource.clip = loseMusic;
            uiAudioSource.loop = false; // Set true if you want it to loop
            uiAudioSource.Play();
        }

        PauseGame(); // Freezes everything else
    }

    public void ShowWinScreen()
    {
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            
            // ... (Keep your existing text/color logic here) ...
            if(winTitle) { winTitle.text = "CONTAINMENT FAILED"; winTitle.color = Color.red; }
            if(winDescription) { winDescription.text = "Subject has escaped...\n\nTHE MONSTER WINS."; }
            if(winBackgroundPanel) { winBackgroundPanel.color = new Color(0.3f, 0f, 0f, 1f); }
        }

        // 1. Play the Music
        if (uiAudioSource && winMusic)
        {
            uiAudioSource.ignoreListenerPause = true; // IMPORTANT: Allows this to play while game is paused
            uiAudioSource.Stop();
            uiAudioSource.clip = winMusic;
            uiAudioSource.loop = false; 
            uiAudioSource.Play();
        }

        PauseGame(); // Freezes everything else
    } 
    public void RestartGame()
    {
        ResumeGame(); // IMPORTANT: Unfreeze before reloading or the next scene starts frozen!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void QuitToMainMenu()
    {
        // 1. CRITICAL: Unpause audio and time before leaving
        Time.timeScale = 1f;
        AudioListener.pause = false; 

        // 2. Load your menu scene
        // Make sure "MainMenu" matches the exact name of your scene file!
        SceneManager.LoadScene("MainMenu"); 
    }
}