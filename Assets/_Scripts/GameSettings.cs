using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [Header("Performance Settings")]
    [Tooltip("0 = Unlimited (or VSync), 30, 60, 144 etc.")]
    public int targetFPS = 60;

    void Awake()
    {
        // VSync must be disabled for targetFrameRate to work!
        // 0 = VSync Off, 1 = VSync On (locked to monitor refresh)
        QualitySettings.vSyncCount = 0; 
        
        // Set the cap
        Application.targetFrameRate = targetFPS;
    }
    
    // Optional: Hotkey to toggle FPS in Editor for testing
    void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Application.targetFrameRate = 30;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Application.targetFrameRate = 60;
        }
        #endif
    }
}