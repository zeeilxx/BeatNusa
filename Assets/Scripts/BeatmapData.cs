using System;
using System.Collections.Generic;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Data models matching the FastAPI backend JSON responses.
// Used with Unity's JsonUtility for deserialization.
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// ── Beatmap ──────────────────────────────────────────

/// <summary>Single note in a beatmap.</summary>
[Serializable]
public class BeatmapNote
{
    public int time_ms;
    public int lane;
    public string type;      // "tap"
    public int length_ms;    // 0 for tap notes
}

/// <summary>The beatmap payload (lane_count, offset_ms, notes[]).</summary>
[Serializable]
public class BeatmapJSON
{
    public int lane_count;
    public int offset_ms;
    public BeatmapNote[] notes;
}

/// <summary>Full response from GET /api/beatmaps/{song_code}.</summary>
[Serializable]
public class BeatmapResponse
{
    public int id;
    public int song_id;
    public string song_code;
    public string model_name;
    public string model_version;
    public string difficulty_name;
    public int lane_count;
    public int offset_ms;
    public int note_count;
    public string generation_status;
    public string generated_at;
    public BeatmapJSON beatmap;
}

// ── Songs ────────────────────────────────────────────

/// <summary>Single song item from GET /api/songs.</summary>
[Serializable]
public class SongListItem
{
    public int id;
    public string song_code;
    public string title;
    public string artist;
    public string genre;
    public string file_format;
    public float duration_seconds;
    public float bpm;
    public string process_status;
    public string created_at;
}

/// <summary>
/// Wrapper for deserializing JSON arrays with JsonUtility.
/// JsonUtility cannot deserialize top-level arrays, so we wrap them.
/// Usage: JsonUtility.FromJson&lt;SongListWrapper&gt;("{\"items\":" + json + "}");
/// </summary>
[Serializable]
public class SongListWrapper
{
    public SongListItem[] items;
}

/// <summary>Response from POST /api/songs/upload.</summary>
[Serializable]
public class UploadResponse
{
    public string status;
    public string song_code;
    public string title;
    public string process_status;
    public string message;
}

/// <summary>Response from GET /api/songs/{song_code}/status.</summary>
[Serializable]
public class SongStatusResponse
{
    public string song_code;
    public string process_status;
    public string title;
}

// ── Game Results ─────────────────────────────────────

/// <summary>Payload sent to POST /api/game-results.</summary>
[Serializable]
public class GameResultPayload
{
    public string song_code;
    public int beatmap_id;
    public string player_name;
    public int score;
    public float accuracy;
    public int max_combo;
    public int hit_count;
    public int miss_count;
    public int good_count;
    public int perfect_count;
    public int bad_count;
    public float mean_offset_ms;
}

/// <summary>Response from POST /api/game-results.</summary>
[Serializable]
public class GameResultResponse
{
    public int id;
    public string song_code;
    public int beatmap_id;
    public string player_name;
    public int score;
    public float accuracy;
    public int max_combo;
    public string played_at;
}
