using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("HUD")]
    public Slider healthSlider;
    public Slider xpSlider; // THIS is the new slot you were missing
    public TextMeshProUGUI levelText;
    public GameObject gameOverPanel;

    [Header("Upgrade Menu")]
    public GameObject upgradePanel; // THIS is the new slot
    public Button[] choiceButtons; // Assign your 3 buttons here
    public TextMeshProUGUI[] buttonTexts; // Assign the text inside those buttons

    [Header("Visual Feedback")]
    public Color normalColor = Color.green;
    public Color dangerColor = Color.red;
    public Image healthFillImage;
    [Header("Win Screen References")]
    public GameObject winPanel;
    public TextMeshProUGUI winTitle;
    public TextMeshProUGUI winDescription;
    public Image winBackgroundPanel;



    void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void UpdateXPUI(float current, float max)
    {
        if (xpSlider) xpSlider.value = current / max;
    }

        public void ShowLevelUpOptions(List<string> options, System.Action<int> onChoose)
    {
        // (Keep your existing code here)
        if (upgradePanel)
        {
            upgradePanel.SetActive(true);
            Time.timeScale = 0f;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < options.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    buttonTexts[i].text = options[i];

                    int index = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() =>
                    {
                        onChoose(index);
                        CloseUpgradeMenu();
                    });
                }
                else choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

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

    // --- NEW WIN FUNCTION ---
    public void ShowWinScreen()
    {
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            
            // THEME: "NOTHING IS WHAT IT SEEMS"
            // Change the visual style to look like a disaster report
            
            // 1. Scary Title
            if(winTitle) 
            {
                winTitle.text = "CONTAINMENT FAILED";
                winTitle.color = Color.red;
            }

            // 2. Lore Description (The Twist)
            if(winDescription)
            {
                winDescription.text = "Subject has escaped the facility.\n" +
                                      "Atmospheric contamination detected.\n" +
                                      "Projected Human Casualties: 100%.\n\n" +
                                      "THE MONSTER WINS.";
            }

            // 3. Dark Red Background
            if(winBackgroundPanel)
            {
                winBackgroundPanel.color = new Color(0.3f, 0f, 0f, 1f); // Blood Red
            }
        }

        // Stop the game time
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}