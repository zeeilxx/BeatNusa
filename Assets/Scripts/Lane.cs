using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single lane in the rhythm game.
/// Now receives notes from JSON beatmap data instead of MIDI.
/// 
/// Each lane is identified by its index in SongManager.lanes[] (0, 1, 2, 3)
/// matching the "lane" field in the beatmap JSON.
/// </summary>
public class Lane : MonoBehaviour
{
    [Header("Miss Feedback")]
    public GameObject missTextPrefab;
    public float missTextYOffset = 0.6f;
    public KeyCode input;
    public GameObject notePrefab;
    List<Note> notes = new List<Note>();
    public List<double> timeStamps = new List<double>();

    int spawnIndex = 0;
    int inputIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"[Lane] Start() — Lane '{gameObject.name}' initializing...");
        Debug.Log($"[Lane]   Input key: {input}");
        Debug.Log($"[Lane]   Note prefab: {(notePrefab != null ? notePrefab.name : "NOT ASSIGNED")}");

        if (SongManager.Instance == null)
        {
            Debug.LogError($"[Lane] '{gameObject.name}': SongManager is not initialized!");
        }
        else
        {
            Debug.Log($"[Lane]   SongManager: OK");
        }

        if (notePrefab == null)
        {
            Debug.LogError($"[Lane] '{gameObject.name}': Note Prefab is NOT assigned!");
        }
    }

    /// <summary>
    /// Receive notes from the beatmap JSON for this lane.
    /// Converts time_ms (milliseconds) to seconds for the timestamp list.
    /// </summary>
    public void SetTimeStampsFromJson(List<BeatmapNote> laneNotes)
    {
        timeStamps.Clear();

        foreach (var note in laneNotes)
        {
            // Convert milliseconds to seconds
            double timeInSeconds = note.time_ms / 1000.0;
            timeStamps.Add(timeInSeconds);
        }

        Debug.Log($"[Lane] '{gameObject.name}': {timeStamps.Count} timestamps set from JSON.");
        if (timeStamps.Count > 0)
        {
            Debug.Log($"[Lane]   First note: {timeStamps[0]:F3}s, Last note: {timeStamps[timeStamps.Count - 1]:F3}s");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check SongManager and notePrefab before proceeding
        if (SongManager.Instance == null || notePrefab == null)
        {
            return;
        }

        // Don't process until the beatmap is loaded
        if (!SongManager.Instance.beatmapLoaded)
        {
            return;
        }

        // Spawning notes based on timestamps
        if (spawnIndex < timeStamps.Count)
        {
            if (SongManager.GetAudioSourceTime() >= timeStamps[spawnIndex] - SongManager.Instance.noteTime)
            {
                var note = Instantiate(notePrefab, transform);
                var noteComponent = note.GetComponent<Note>();

                if (noteComponent != null)
                {
                    notes.Add(noteComponent);
                    noteComponent.assignedTime = (float)timeStamps[spawnIndex];
                }
                else
                {
                    Debug.LogError("Note prefab is missing the Note component!");
                }

                spawnIndex++;
            }
        }

        // Input handling for hitting notes
        if (inputIndex < timeStamps.Count)
        {
            double timeStamp = timeStamps[inputIndex];
            double marginOfError = SongManager.Instance.marginOfError;
            double audioTime = SongManager.GetAudioSourceTime() - (SongManager.Instance.inputDelayInMilliseconds / 1000.0);

            if (Input.GetKeyDown(input))
            {
                double offset = Math.Abs(audioTime - timeStamp);

                if (offset < marginOfError)
                {
                    Hit(offset);
                    Debug.Log($"[Lane] '{gameObject.name}': HIT note #{inputIndex} (offset: {offset * 1000:F1}ms)");

                    // Safeguard in case the note has already been destroyed
                    if (inputIndex < notes.Count && notes[inputIndex] != null)
                    {
                        Destroy(notes[inputIndex].gameObject);
                    }

                    inputIndex++;
                }
                else
                {
                    Debug.Log($"[Lane] '{gameObject.name}': Input too early/late for note #{inputIndex} (offset: {offset * 1000:F1}ms, margin: {marginOfError * 1000:F1}ms)");
                }
            }

            // Handling missed notes
            if (timeStamp + marginOfError <= audioTime)
            {
                Miss();
                Debug.Log($"[Lane] '{gameObject.name}': MISSED note #{inputIndex} (expected: {timeStamp:F3}s, current: {audioTime:F3}s)");
                inputIndex++;
            }
        }
    }

    private void Hit(double offsetSeconds)
    {
        ScoreManager.Hit(offsetSeconds);
        AnimManagerPlayer.Hit();
        AnimManagerOrc.Hit();
    }

    private void Miss()
    {
    ScoreManager.Miss();

    SpawnMissText();

    AnimManagerPlayer.Miss();
    AnimManagerOrc.Miss();
    }

    private void SpawnMissText()
    {
        if (missTextPrefab == null)
        {
            Debug.LogWarning($"[Lane] '{gameObject.name}': Miss Text Prefab is not assigned!");
            return;
        }

        GameObject missText = Instantiate(missTextPrefab, transform);

        float targetY = SongManager.Instance != null
            ? SongManager.Instance.noteDespawnY + missTextYOffset
            : missTextYOffset;

        missText.transform.localPosition = new Vector3(0f, targetY, -1f);
        missText.transform.localRotation = Quaternion.identity;
    }
}
