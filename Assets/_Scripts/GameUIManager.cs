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

    void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void UpdateXPUI(float current, float max)
    {
        if (xpSlider) xpSlider.value = current / max;
    }

    // --- UPDATED FUNCTION: REMOVED TEXT LOGIC, KEPT IMAGE LOGIC ---
   public void ShowUpgradeCards(List<PlayerController.UpgradeOption> options, System.Action<int> onCardSelected)
    {
        // --- FIX 1: STOP TIME ---
        Time.timeScale = 0f; 
        // ------------------------

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
                    // --- FIX 2: RESUME TIME ---
                    Time.timeScale = 1f; 
                    // --------------------------
                    
                    onCardSelected(index);
                    if (upgradePanel) upgradePanel.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
        }
    }
    // -------------------------------------------------------------

    void CloseUpgradeMenu()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ShowWinScreen()
    {
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            
            // THEME: "NOTHING IS WHAT IT SEEMS"
            if(winTitle) 
            {
                winTitle.text = "CONTAINMENT FAILED";
                winTitle.color = Color.red;
            }

            if(winDescription)
            {
                winDescription.text = "Subject has escaped the facility.\n" +
                                      "Atmospheric contamination detected.\n" +
                                      "Projected Human Casualties: 100%.\n\n" +
                                      "THE MONSTER WINS.";
            }

            if(winBackgroundPanel)
            {
                winBackgroundPanel.color = new Color(0.3f, 0f, 0f, 1f); 
            }
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}