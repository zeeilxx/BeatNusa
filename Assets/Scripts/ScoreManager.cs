using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks gameplay statistics (hits, misses, score, combo, accuracy)
/// and submits results to the backend API when the song ends.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public AudioSource hitSFX;
    public AudioSource missSFX;
    public TMPro.TextMeshPro scoreText;
    public GameObject deathPrefab;   // Prefab to instantiate on player death
    public GameObject winPrefab;     // Prefab to instantiate on song completion (optional)

    // ── Health (existing system) ─────────────────────────
    public static int healthScore;

    // ── Gameplay statistics ──────────────────────────────
    public static int hitCount = 0;
    public static int missCount = 0;
    public static int perfectCount = 0;
    public static int goodCount = 0;
    public static int badCount = 0;
    public static int score = 0;
    public static int currentCombo = 0;
    public static int maxCombo = 0;
    public static float totalOffsetMs = 0f;

    // ── Accuracy thresholds (in seconds) ─────────────────
    // These define how close the hit must be to be classified
    private const double PERFECT_THRESHOLD = 0.05;  // 50ms
    private const double GOOD_THRESHOLD = 0.10;     // 100ms
    // Anything beyond GOOD but within marginOfError = "bad" hit

    // ── Score values per hit type ────────────────────────
    private const int PERFECT_SCORE = 300;
    private const int GOOD_SCORE = 100;
    private const int BAD_SCORE = 50;

    private bool resultsSubmitted = false;
    private bool songFinished = false;

    void Start()
    {
        Instance = this;
        healthScore = PlayerPrefs.GetInt("HealthScore", 30);

        Debug.Log("[ScoreManager] ════════════════════════════════════════");
        Debug.Log("[ScoreManager] Start() initializing...");
        Debug.Log($"[ScoreManager]   Health score: {healthScore}");
        Debug.Log($"[ScoreManager]   Hit SFX: {(hitSFX != null ? "OK" : "NOT ASSIGNED")}");
        Debug.Log($"[ScoreManager]   Miss SFX: {(missSFX != null ? "OK" : "NOT ASSIGNED")}");
        Debug.Log($"[ScoreManager]   Score text: {(scoreText != null ? "OK" : "NOT ASSIGNED")}");
        Debug.Log($"[ScoreManager]   Death prefab: {(deathPrefab != null ? deathPrefab.name : "NOT ASSIGNED")}");
        Debug.Log($"[ScoreManager]   Win prefab: {(winPrefab != null ? winPrefab.name : "NOT ASSIGNED")}");
        Debug.Log($"[ScoreManager]   Thresholds: Perfect≤{PERFECT_THRESHOLD * 1000}ms, Good≤{GOOD_THRESHOLD * 1000}ms");
        Debug.Log($"[ScoreManager]   Scores: Perfect={PERFECT_SCORE}, Good={GOOD_SCORE}, Bad={BAD_SCORE}");

        // Reset stats
        hitCount = 0;
        missCount = 0;
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        score = 0;
        currentCombo = 0;
        maxCombo = 0;
        totalOffsetMs = 0f;
        resultsSubmitted = false;
        songFinished = false;
        Debug.Log("[ScoreManager]   ✓ Stats reset complete.");
    }

    /// <summary>
    /// Called when the player hits a note within the margin of error.
    /// offsetSeconds = how far off the hit was from the perfect timing.
    /// </summary>
    public static void Hit(double offsetSeconds)
    {
        hitCount++;
        currentCombo++;

        if (currentCombo > maxCombo)
            maxCombo = currentCombo;

        // Track offset for mean_offset_ms calculation
        totalOffsetMs += (float)(offsetSeconds * 1000.0);

        // Classify the hit
        string hitType;
        if (offsetSeconds <= PERFECT_THRESHOLD)
        {
            perfectCount++;
            score += PERFECT_SCORE;
            hitType = "PERFECT";
        }
        else if (offsetSeconds <= GOOD_THRESHOLD)
        {
            goodCount++;
            score += GOOD_SCORE;
            hitType = "GOOD";
        }
        else
        {
            badCount++;
            score += BAD_SCORE;
            hitType = "BAD";
        }

        Debug.Log($"[ScoreManager] HIT #{hitCount}: {hitType} (offset: {offsetSeconds * 1000:F1}ms) | Combo: {currentCombo} | Score: {score} | HP: {healthScore}");

        Instance.hitSFX.Play();
    }

    /// <summary>
    /// Called when the player misses a note (note passes without input).
    /// </summary>
    public static void Miss()
    {
        missCount++;
        currentCombo = 0; // Reset combo on miss

        // Reduce health
        healthScore = Mathf.Max(healthScore - 1, 0);

        Debug.Log($"[ScoreManager] MISS #{missCount}: Combo reset | HP: {healthScore} | Total misses: {missCount}");

        Instance.missSFX.Play();

        // Death check
        if (healthScore == 0)
        {
            Debug.LogWarning("[ScoreManager] ═══ PLAYER DEATH ═══");
            Debug.Log($"[ScoreManager]   Final Score: {score}");
            Debug.Log($"[ScoreManager]   Perfect: {perfectCount} | Good: {goodCount} | Bad: {badCount} | Miss: {missCount}");
            Debug.Log($"[ScoreManager]   Max combo: {maxCombo}");

            AnimManagerPlayer.Death();
            SongManager.StopSong();

            // Instantiate the death prefab at the center of the screen
            if (Instance.deathPrefab != null)
            {
                Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 3.2f, Camera.main.nearClipPlane);
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenCenter);
                worldPosition.z = -8f;

                Instantiate(Instance.deathPrefab, worldPosition, Quaternion.identity);
                Debug.Log("[ScoreManager]   Death prefab instantiated.");
            }
            else
            {
                Debug.LogWarning("[ScoreManager]   Death prefab is not assigned!");
            }

            // Submit results even on death
            Debug.Log("[ScoreManager]   Submitting results on death...");
            Instance.SubmitResults();
        }
    }

    private void Update()
    {
        // Update the displayed score
        scoreText.text = healthScore.ToString();

        // Check if the song has ended naturally (not by death)
        if (!songFinished && SongManager.Instance != null && SongManager.Instance.beatmapLoaded)
        {
            if (SongManager.Instance.audioSource != null &&
                !SongManager.Instance.audioSource.isPlaying &&
                SongManager.GetAudioSourceTime() > 0)
            {
                songFinished = true;
                OnSongFinished();
            }
        }
    }

    /// <summary>
    /// Called when the song ends naturally (player survived).
    /// </summary>
    private void OnSongFinished()
    {
        Debug.Log("[ScoreManager] ═══ SONG FINISHED ═══");
        Debug.Log($"[ScoreManager]   Survived! Submitting results...");
        SubmitResults();
    }

    /// <summary>
    /// Calculate accuracy and submit game results to the backend.
    /// </summary>
    public void SubmitResults()
    {
        if (resultsSubmitted)
        {
            Debug.Log("[ScoreManager] SubmitResults: Already submitted, skipping.");
            return;
        }
        resultsSubmitted = true;

        // Calculate accuracy (percentage of notes hit)
        int totalNotes = hitCount + missCount;
        float accuracy = totalNotes > 0 ? (float)hitCount / totalNotes * 100f : 0f;
        float meanOffsetMs = hitCount > 0 ? totalOffsetMs / hitCount : 0f;

        Debug.Log($"[ScoreManager] ════════════════════════════════════════");
        Debug.Log($"[ScoreManager] SubmitResults START");
        Debug.Log($"[ScoreManager]   ┌─────────────────────────────────┐");
        Debug.Log($"[ScoreManager]   │ GAME RESULTS SUMMARY            │");
        Debug.Log($"[ScoreManager]   ├─────────────────────────────────┤");
        Debug.Log($"[ScoreManager]   │ Score:     {score,-20} │");
        Debug.Log($"[ScoreManager]   │ Accuracy:  {accuracy:F1}%{new string(' ', 17 - accuracy.ToString("F1").Length)}│");
        Debug.Log($"[ScoreManager]   │ Max Combo: {maxCombo,-20} │");
        Debug.Log($"[ScoreManager]   │ Perfect:   {perfectCount,-20} │");
        Debug.Log($"[ScoreManager]   │ Good:      {goodCount,-20} │");
        Debug.Log($"[ScoreManager]   │ Bad:       {badCount,-20} │");
        Debug.Log($"[ScoreManager]   │ Miss:      {missCount,-20} │");
        Debug.Log($"[ScoreManager]   │ Mean Offset: {meanOffsetMs:F1}ms{new string(' ', 14 - meanOffsetMs.ToString("F1").Length)}│");
        Debug.Log($"[ScoreManager]   └─────────────────────────────────┘");

        // Build the payload
        string playerName = PlayerNameInput.GetPlayerName();
        Debug.Log($"[ScoreManager]   Player: {playerName}");
        Debug.Log($"[ScoreManager]   Song: {SongManager.Instance.songCode}");
        Debug.Log($"[ScoreManager]   Beatmap ID: {SongManager.Instance.beatmapId}");

        GameResultPayload payload = new GameResultPayload
        {
            song_code = SongManager.Instance.songCode,
            beatmap_id = SongManager.Instance.beatmapId,
            player_name = playerName,
            score = score,
            accuracy = accuracy,
            max_combo = maxCombo,
            hit_count = hitCount,
            miss_count = missCount,
            good_count = goodCount,
            perfect_count = perfectCount,
            bad_count = badCount,
            mean_offset_ms = meanOffsetMs,
        };

        Debug.Log("[ScoreManager]   Sending to backend...");

        // Send to backend
        StartCoroutine(ApiClient.SubmitGameResult(
            payload,
            onSuccess: (response) =>
            {
                Debug.Log($"[ScoreManager] SubmitResults SUCCESS! Result ID: {response.id}");
            },
            onError: (error) =>
            {
                Debug.LogError($"[ScoreManager] SubmitResults FAILED: {error}");
            }
        ));
    }
}
