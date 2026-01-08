using UnityEngine;

public class OneShotEffect : MonoBehaviour
{
    public float lifetime = 0.5f; // Duration in seconds

    void Start()
    {
        // This destroys the object automatically after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }
}