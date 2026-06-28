using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    public AudioSource audioSource; // Reference to your AudioSource
    public Slider slider;           // Reference to the Slider
    public GameObject winPrefab;    // Reference to the prefab to spawn

    private bool deathTriggered = false; // To ensure the Death method is called only once
    public KeyCode skipKey = KeyCode.Tab; // First skip key (Tab)
    public KeyCode skipKey2 = KeyCode.Return; // Second skip key (Enter)

    private bool audioStopped = false; // Flag to check if audio has been stopped manually
    private bool sliderInitialized = false; // Flag: slider has been set up with clip duration
    private bool audioHasPlayed = false; // Flag: audio was confirmed playing at least once

    void Start()
    {
        // Don't initialize slider here — in WebGL the clip may not be loaded yet.
        // Initialization is deferred to Update() once the clip is ready.
        if (slider != null)
        {
            slider.value = 1f; // Prevent immediate zero trigger
        }
    }

    void Update()
    {
        if (audioSource == null || slider == null) return;

        // ── Deferred initialization: wait until clip is actually loaded ──
        if (!sliderInitialized)
        {
            if (audioSource.clip != null && audioSource.clip.length > 0)
            {
                slider.maxValue = audioSource.clip.length;
                slider.value = audioSource.clip.length;
                sliderInitialized = true;
                Debug.Log($"[SliderController] Slider initialized — clip length: {audioSource.clip.length:F2}s");
            }
            else
            {
                return; // Clip not ready yet, skip everything
            }
        }

        // ── Track that audio has actually been playing ──
        if (!audioHasPlayed && audioSource.isPlaying)
        {
            audioHasPlayed = true;
            Debug.Log("[SliderController] Audio confirmed playing.");
        }

        // ── Update slider ──
        if (!audioStopped)
        {
            slider.value = audioSource.clip.length - audioSource.time;

            // Only check for song end if audio was confirmed playing first
            if (audioHasPlayed && slider.value <= 0 && !deathTriggered)
            {
                deathTriggered = true;
                TriggerDeath();
            }
        }
        else
        {
            slider.value = 0;
            if (!deathTriggered)
            {
                deathTriggered = true;
                TriggerDeath();
            }
        }

        // Check if both keys are being pressed (Tab and Enter)
        if (Input.GetKey(skipKey) && Input.GetKey(skipKey2))
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                audioStopped = true;

                if (!deathTriggered)
                {
                    deathTriggered = true;
                    TriggerDeath();
                }
            }
        }
    }

    // Method to trigger the death animation and spawn the prefab
    private void TriggerDeath()
    {
        AnimManagerOrc.Death(); // Call the Death animation
        Debug.Log("Death animation triggered.");

        // Submit results when song finishes via slider
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SubmitResults();
        }

        // Instantiate the win prefab at the center of the screen with z = -8f
        if (winPrefab != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 3.2f, Camera.main.nearClipPlane);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenCenter);
            worldPosition.z = -8f; // Set z position to -8f

            Instantiate(winPrefab, worldPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Win prefab is not assigned.");
        }
    }
}
