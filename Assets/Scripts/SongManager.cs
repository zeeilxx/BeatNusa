using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Core song/beatmap manager.
/// Now loads beatmaps from the backend API (JSON) instead of MIDI files.
/// 
/// Flow:
///   1. Reads "SelectedSongCode" from PlayerPrefs (set by SongListUI)
///   2. Fetches beatmap JSON from GET /api/beatmaps/{song_code}
///   3. Distributes notes to lanes by lane index
///   4. Starts playing the audio after a delay
/// </summary>
public class SongManager : MonoBehaviour
{
    public static SongManager Instance;
    public AudioSource audioSource;
    public Lane[] lanes;
    public float songDelayInSeconds;
    public double marginOfError; // in seconds
    public int inputDelayInMilliseconds;

    public float noteTime;
    public float noteSpawnY;
    public float noteTapY;
    public float noteDespawnY
    {
        get
        {
            return noteTapY - (noteSpawnY - noteTapY);
        }
    }

    // ── API-related fields ───────────────────────────────
    [HideInInspector] public string songCode;
    [HideInInspector] public int beatmapId;
    [HideInInspector] public int totalNotes;
    [HideInInspector] public bool beatmapLoaded = false;

    // Awake is called before the first frame update to handle Singleton logic
    void Awake()
    {
        Debug.Log("[SongManager] Awake() called.");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[SongManager] Singleton instance created.");
        }
        else
        {
            Debug.LogWarning("[SongManager] Duplicate instance detected. Destroying this object.");
            Destroy(gameObject);
            return;
        }
    }

    // Start is called on initialization
    void Start()
    {
        Debug.Log("[SongManager] ════════════════════════════════════════");
        Debug.Log("[SongManager] Start() initializing...");

        if (audioSource == null)
        {
            Debug.LogError("[SongManager] AudioSource is NOT assigned! Aborting.");
            return;
        }
        Debug.Log("[SongManager]   AudioSource: OK");

        if (lanes.Length == 0)
        {
            Debug.LogError("[SongManager] No lanes assigned! Aborting.");
            return;
        }
        Debug.Log($"[SongManager]   Lanes: {lanes.Length} lanes configured");
        for (int i = 0; i < lanes.Length; i++)
        {
            Debug.Log($"[SongManager]     Lane {i}: {lanes[i]?.gameObject.name ?? "NULL"}");
        }

        Debug.Log($"[SongManager]   Settings: delay={songDelayInSeconds}s, margin={marginOfError}s, inputDelay={inputDelayInMilliseconds}ms");
        Debug.Log($"[SongManager]   Note: time={noteTime}, spawnY={noteSpawnY}, tapY={noteTapY}, despawnY={noteDespawnY}");

        // Get the selected song code from PlayerPrefs (set by SongListUI or SelectMusicButton)
        songCode = PlayerPrefs.GetString("SelectedSongCode", "");
        Debug.Log($"[SongManager]   PlayerPrefs 'SelectedSongCode': '{songCode}'");

        string localAudioPath = PlayerPrefs.GetString("SelectedAudioPath", "");
        Debug.Log($"[SongManager]   PlayerPrefs 'SelectedAudioPath': '{localAudioPath}'");

        if (!string.IsNullOrEmpty(songCode))
        {
            Debug.Log($"[SongManager] Starting beatmap load for song: {songCode}");
            StartCoroutine(LoadBeatmapFromApi());
        }
        else
        {
            Debug.LogWarning("[SongManager] No song code found in PlayerPrefs! Please select a song first.");
        }
    }

    /// <summary>
    /// Fetch beatmap JSON from the backend and distribute notes to lanes.
    /// Also loads the audio clip automatically.
    /// </summary>
    private IEnumerator LoadBeatmapFromApi()
    {
        Debug.Log("[SongManager] ════════════════════════════════════════");
        Debug.Log("[SongManager] LoadBeatmapFromApi START");
        Debug.Log($"[SongManager]   Song code: {songCode}");

        // ── Step 1: Load beatmap data ────────────────────────
        Debug.Log("[SongManager] ── Step 1: Fetching beatmap data...");
        bool beatmapDone = false;

        yield return ApiClient.GetBeatmap(
            songCode,
            onSuccess: (response) =>
            {
                Debug.Log($"[SongManager]   Beatmap received!");
                Debug.Log($"[SongManager]     ID: {response.id}");
                Debug.Log($"[SongManager]     Notes: {response.note_count}");
                Debug.Log($"[SongManager]     Lanes: {response.lane_count}");

                beatmapId = response.id;
                totalNotes = response.note_count;

                // Distribute notes to lanes
                SetNotesFromJson(response.beatmap);
                beatmapLoaded = true;
                beatmapDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError($"[SongManager]   Beatmap fetch FAILED: {error}");
                beatmapDone = true;
            }
        );

        // Wait until the callback finishes
        while (!beatmapDone) yield return null;

        if (!beatmapLoaded)
        {
            Debug.LogError("[SongManager] Beatmap failed to load. Song will NOT start.");
            yield break;
        }
        Debug.Log("[SongManager]   ✓ Beatmap loaded successfully.");

        // ── Step 2: Load audio clip ──────────────────────────
        Debug.Log("[SongManager] ── Step 2: Loading audio clip...");
        string localAudioPath = PlayerPrefs.GetString("SelectedAudioPath", "");
        bool audioLoaded = false;

        if (!string.IsNullOrEmpty(localAudioPath) && System.IO.File.Exists(localAudioPath))
        {
            Debug.Log($"[SongManager]   Trying local file: {localAudioPath}");

            yield return ApiClient.LoadLocalAudio(
                localAudioPath,
                onSuccess: (clip) =>
                {
                    audioSource.clip = clip;
                    audioLoaded = true;
                    Debug.Log($"[SongManager]   ✓ Audio loaded from local file ({clip.length:F2}s)");
                },
                onError: (error) =>
                {
                    Debug.LogWarning($"[SongManager]   ✗ Local audio failed: {error}. Trying backend...");
                }
            );
        }
        else
        {
            Debug.Log($"[SongManager]   No local file available (path='{localAudioPath}', exists={(!string.IsNullOrEmpty(localAudioPath) && System.IO.File.Exists(localAudioPath))})");
        }

        // If local failed or not available, download from backend
        if (!audioLoaded)
        {
            Debug.Log($"[SongManager]   Trying backend download for: {songCode}");

            yield return ApiClient.DownloadAudio(
                songCode,
                onSuccess: (clip) =>
                {
                    audioSource.clip = clip;
                    audioLoaded = true;
                    Debug.Log($"[SongManager]   ✓ Audio downloaded from backend ({clip.length:F2}s)");
                },
                onError: (error) =>
                {
                    Debug.LogWarning($"[SongManager]   ✗ Backend audio download failed: {error}");
                }
            );
        }

        if (!audioLoaded)
        {
            // Last resort: check if AudioSource already has a clip assigned in Inspector
            if (audioSource.clip != null)
            {
                Debug.LogWarning($"[SongManager]   ⚠ Using pre-assigned AudioClip from Inspector as fallback: {audioSource.clip.name} ({audioSource.clip.length:F2}s)");
                audioLoaded = true;
            }
            else
            {
                Debug.LogError("[SongManager]   ✗ No audio available! Assign a clip in Inspector or ensure backend serves audio.");
                yield break;
            }
        }

        // ── Step 3: Start the song ───────────────────────────
        Debug.Log($"[SongManager] ── Step 3: Starting song (delay: {songDelayInSeconds}s)...");
        Debug.Log($"[SongManager]   Audio clip: {audioSource.clip.name}, {audioSource.clip.length:F2}s, {audioSource.clip.frequency}Hz");
        Invoke(nameof(StartSong), songDelayInSeconds);
        Debug.Log("[SongManager] LoadBeatmapFromApi END");
    }

    /// <summary>
    /// Distribute notes from the beatmap JSON to lanes by lane index.
    /// Lane index 0 → lanes[0], lane index 1 → lanes[1], etc.
    /// </summary>
    private void SetNotesFromJson(BeatmapJSON beatmap)
    {
        Debug.Log("[SongManager] SetNotesFromJson: Distributing notes to lanes...");

        if (beatmap == null || beatmap.notes == null)
        {
            Debug.LogError("[SongManager]   Beatmap JSON is null or has no notes!");
            return;
        }

        Debug.Log($"[SongManager]   Total notes in beatmap: {beatmap.notes.Length}");
        Debug.Log($"[SongManager]   Lane count: {beatmap.lane_count}, Offset: {beatmap.offset_ms}ms");

        // Group notes by lane index
        Dictionary<int, List<BeatmapNote>> notesByLane = new Dictionary<int, List<BeatmapNote>>();
        foreach (var note in beatmap.notes)
        {
            if (!notesByLane.ContainsKey(note.lane))
            {
                notesByLane[note.lane] = new List<BeatmapNote>();
            }
            notesByLane[note.lane].Add(note);
        }

        // Assign to lanes
        for (int i = 0; i < lanes.Length; i++)
        {
            if (notesByLane.ContainsKey(i))
            {
                lanes[i].SetTimeStampsFromJson(notesByLane[i]);
                Debug.Log($"[SongManager]   Lane {i} ({lanes[i].gameObject.name}): {notesByLane[i].Count} notes assigned");
            }
            else
            {
                lanes[i].SetTimeStampsFromJson(new List<BeatmapNote>());
                Debug.Log($"[SongManager]   Lane {i} ({lanes[i].gameObject.name}): 0 notes assigned");
            }
        }
        Debug.Log("[SongManager]   ✓ Note distribution complete.");
    }

    // Start playing the song
    public void StartSong()
    {
        Debug.Log("[SongManager] ════════════════════════════════════════");
        Debug.Log("[SongManager] StartSong() called");
        if (audioSource != null && audioSource.clip != null)
        {
            Debug.Log($"[SongManager]   Playing: {audioSource.clip.name}");
            Debug.Log($"[SongManager]   Duration: {audioSource.clip.length:F2}s");
            Debug.Log($"[SongManager]   Total notes: {totalNotes}");
            audioSource.Play();
            Debug.Log("[SongManager]   ✓ Song started!");
        }
        else
        {
            Debug.LogError("[SongManager]   ✗ AudioSource or clip is null! Cannot play.");
        }
    }

    public static void StopSong()
    {
        Debug.Log("[SongManager] StopSong() called");
        if (Instance != null && Instance.audioSource != null)
        {
            Instance.audioSource.Stop();
            Debug.Log($"[SongManager]   ✓ Song stopped at time: {GetAudioSourceTime():F2}s");
        }
        else
        {
            Debug.LogError("[SongManager]   ✗ Instance or AudioSource is null!");
        }
    }

    // Get the current time of the audio playing
    public static double GetAudioSourceTime()
    {
        if (Instance == null || Instance.audioSource == null)
        {
            Debug.LogError("SongManager Instance or AudioSource is null!");
            return 0;
        }

        return (double)Instance.audioSource.timeSamples / Instance.audioSource.clip.frequency;
    }

    void Update()
    {
        // Currently empty — add any runtime updates as needed.
    }
}
