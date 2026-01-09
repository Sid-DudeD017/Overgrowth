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

    [Header("Visual Feedback")]
    public Color normalColor = Color.green;
    public Color dangerColor = Color.red;
    public Image healthFillImage;
    
    [Header("Win Screen References")]
    public GameObject winPanel;
    public TextMeshProUGUI winTitle;
    public TextMeshProUGUI winDescription;
    
    [Header("Card System")]
    public GameObject cardPrefab;      
    public Transform cardContainer;
    [Header("Audio")]
    public AudioSource uiAudioSource; 
    public AudioClip winMusic;        
    public AudioClip loseMusic;      

    void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        
        
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

    
    void PauseGame()
    {
        Time.timeScale = 0f;          
        AudioListener.pause = true;   
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;          
        AudioListener.pause = false;  
    }
    // ------------------------------------

    public void ShowUpgradeCards(List<PlayerController.UpgradeOption> options, System.Action<int> onCardSelected)
    {
       
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
        
        
        if (uiAudioSource && loseMusic)
        {
            uiAudioSource.ignoreListenerPause = true; 
            uiAudioSource.Stop(); 
            uiAudioSource.clip = loseMusic;
            uiAudioSource.loop = false; 
            uiAudioSource.Play();
        }

        PauseGame(); 
    }

    public void ShowWinScreen()
    {
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            

            if(winTitle) { winTitle.text = "CONTAINMENT FAILED"; winTitle.color = Color.red; }
            if(winDescription) { winDescription.text = "Subject has escaped...\n\nTHE MONSTER WINS."; }
        }


        if (uiAudioSource && winMusic)
        {
            uiAudioSource.ignoreListenerPause = true; 
            uiAudioSource.Stop();
            uiAudioSource.clip = winMusic;
            uiAudioSource.loop = false; 
            uiAudioSource.Play();
        }

        PauseGame(); 
    } 
    public void RestartGame()
    {
        ResumeGame(); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void QuitToMainMenu()
    {
        
        Time.timeScale = 1f;
        AudioListener.pause = false; 
        SceneManager.LoadScene("MainMenu"); 
    }
}