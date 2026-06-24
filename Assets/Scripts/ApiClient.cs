using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Centralized HTTP client for all backend API communication.
/// Uses UnityWebRequest (WebGL-compatible). All methods are coroutines.
/// 
/// Usage example:
///   StartCoroutine(ApiClient.GetBeatmap("SONG-ABC123", (response) => { ... }));
/// </summary>
public static class ApiClient
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UPLOAD AUDIO → POST /api/songs/upload
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator UploadAudio(
        string filePath,
        string title,
        Action<UploadResponse> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] UploadAudio (File) START");
        Debug.Log($"[ApiClient]   File: {filePath}");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ApiClient] UploadAudio FAILED — File not found: {filePath}");
            onError?.Invoke("File not found: " + filePath);
            yield break;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        yield return UploadAudio(fileData, fileName, title, onSuccess, onError);
    }

    public static IEnumerator UploadAudio(
        byte[] fileData,
        string fileName,
        string title,
        Action<UploadResponse> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] UploadAudio (Bytes) START");
        Debug.Log($"[ApiClient]   URL: {ApiConfig.SongsUpload}");
        Debug.Log($"[ApiClient]   FileName: {fileName}");
        Debug.Log($"[ApiClient]   Title: {title}");

        Debug.Log($"[ApiClient]   File size: {fileData.Length} bytes ({fileData.Length / 1024f:F1} KB)");
        Debug.Log($"[ApiClient]   MIME type: {GetMimeType(fileName)}");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, GetMimeType(fileName));

        if (!string.IsNullOrEmpty(title))
            form.AddField("title", title);

        using (UnityWebRequest www = UnityWebRequest.Post(ApiConfig.SongsUpload, form))
        {
            Debug.Log($"[ApiClient]   Sending POST request...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode}");
            Debug.Log($"[ApiClient]   Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] UploadAudio FAILED — {www.error}");
                Debug.LogError($"[ApiClient]   Response body: {www.downloadHandler?.text}");
                onError?.Invoke(www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log($"[ApiClient] UploadAudio SUCCESS");
                Debug.Log($"[ApiClient]   Response: {json}");
                UploadResponse response = JsonUtility.FromJson<UploadResponse>(json);
                Debug.Log($"[ApiClient]   Song code: {response.song_code}");
                Debug.Log($"[ApiClient]   Process status: {response.process_status}");
                onSuccess?.Invoke(response);
            }
        }
        Debug.Log($"[ApiClient] UploadAudio END");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GET SONG LIST → GET /api/songs
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator GetSongList(
        Action<SongListItem[]> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] GetSongList START");
        Debug.Log($"[ApiClient]   URL: {ApiConfig.SongsList}");

        using (UnityWebRequest www = UnityWebRequest.Get(ApiConfig.SongsList))
        {
            Debug.Log($"[ApiClient]   Sending GET request...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode}");
            Debug.Log($"[ApiClient]   Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] GetSongList FAILED — {www.error}");
                Debug.LogError($"[ApiClient]   Response body: {www.downloadHandler?.text}");
                onError?.Invoke(www.error);
            }
            else
            {
                // JsonUtility cannot deserialize top-level arrays,
                // so we wrap the response in a JSON object.
                string json = www.downloadHandler.text;
                Debug.Log($"[ApiClient]   Response length: {json.Length} chars");
                string wrapped = "{\"items\":" + json + "}";
                SongListWrapper wrapper = JsonUtility.FromJson<SongListWrapper>(wrapped);
                Debug.Log($"[ApiClient] GetSongList SUCCESS — {wrapper.items.Length} songs loaded");
                foreach (var song in wrapper.items)
                {
                    Debug.Log($"[ApiClient]   • {song.title} ({song.song_code}) — {song.process_status}");
                }
                onSuccess?.Invoke(wrapper.items);
            }
        }
        Debug.Log($"[ApiClient] GetSongList END");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GET BEATMAP → GET /api/beatmaps/{song_code}
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator GetBeatmap(
        string songCode,
        Action<BeatmapResponse> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] GetBeatmap START");
        Debug.Log($"[ApiClient]   URL: {ApiConfig.Beatmap(songCode)}");
        Debug.Log($"[ApiClient]   Song code: {songCode}");

        using (UnityWebRequest www = UnityWebRequest.Get(ApiConfig.Beatmap(songCode)))
        {
            Debug.Log($"[ApiClient]   Sending GET request...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode}");
            Debug.Log($"[ApiClient]   Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] GetBeatmap FAILED — {www.error}");
                Debug.LogError($"[ApiClient]   Response body: {www.downloadHandler?.text}");
                onError?.Invoke(www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log($"[ApiClient]   Response length: {json.Length} chars");
                BeatmapResponse response = JsonUtility.FromJson<BeatmapResponse>(json);
                Debug.Log($"[ApiClient] GetBeatmap SUCCESS");
                Debug.Log($"[ApiClient]   Beatmap ID: {response.id}");
                Debug.Log($"[ApiClient]   Note count: {response.note_count}");
                Debug.Log($"[ApiClient]   Lane count: {response.lane_count}");
                Debug.Log($"[ApiClient]   Difficulty: {response.difficulty_name}");
                Debug.Log($"[ApiClient]   Model: {response.model_name} v{response.model_version}");
                Debug.Log($"[ApiClient]   Status: {response.generation_status}");
                onSuccess?.Invoke(response);
            }
        }
        Debug.Log($"[ApiClient] GetBeatmap END");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // CHECK SONG STATUS → GET /api/songs/{song_code}/status
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator CheckSongStatus(
        string songCode,
        Action<SongStatusResponse> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] CheckSongStatus START — code: {songCode}");
        Debug.Log($"[ApiClient]   URL: {ApiConfig.SongStatus(songCode)}");

        using (UnityWebRequest www = UnityWebRequest.Get(ApiConfig.SongStatus(songCode)))
        {
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode} | Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] CheckSongStatus FAILED — {www.error}");
                onError?.Invoke(www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log($"[ApiClient]   Response: {json}");
                SongStatusResponse response = JsonUtility.FromJson<SongStatusResponse>(json);
                Debug.Log($"[ApiClient] CheckSongStatus SUCCESS — status: {response.process_status}");
                onSuccess?.Invoke(response);
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // SUBMIT GAME RESULT → POST /api/game-results
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator SubmitGameResult(
        GameResultPayload payload,
        Action<GameResultResponse> onSuccess,
        Action<string> onError)
    {
        string jsonPayload = JsonUtility.ToJson(payload);

        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] SubmitGameResult START");
        Debug.Log($"[ApiClient]   URL: {ApiConfig.GameResults}");
        Debug.Log($"[ApiClient]   Payload: {jsonPayload}");
        Debug.Log($"[ApiClient]   Song: {payload.song_code} | Beatmap ID: {payload.beatmap_id}");
        Debug.Log($"[ApiClient]   Player: {payload.player_name} | Score: {payload.score}");

        using (UnityWebRequest www = new UnityWebRequest(ApiConfig.GameResults, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[ApiClient]   Sending POST request...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode}");
            Debug.Log($"[ApiClient]   Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] SubmitGameResult FAILED — {www.error}");
                Debug.LogError($"[ApiClient]   Response body: {www.downloadHandler?.text}");
                onError?.Invoke(www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                GameResultResponse response = JsonUtility.FromJson<GameResultResponse>(json);
                Debug.Log($"[ApiClient] SubmitGameResult SUCCESS");
                Debug.Log($"[ApiClient]   Result ID: {response.id}");
                Debug.Log($"[ApiClient]   Played at: {response.played_at}");
                Debug.Log($"[ApiClient]   Response: {json}");
                onSuccess?.Invoke(response);
            }
        }
        Debug.Log($"[ApiClient] SubmitGameResult END");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DOWNLOAD AUDIO → GET /api/songs/{song_code}/audio
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator DownloadAudio(
        string songCode,
        Action<AudioClip> onSuccess,
        Action<string> onError)
    {
        string fileFormat = PlayerPrefs.GetString("SelectedAudioFormat", "wav");
        string url = ApiConfig.SongAudio(songCode) + "." + fileFormat;

        AudioType audioType = AudioType.UNKNOWN;
        if (fileFormat == "mp3") audioType = AudioType.MPEG;
        else if (fileFormat == "wav") audioType = AudioType.WAV;
        else if (fileFormat == "ogg") audioType = AudioType.OGGVORBIS;

        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] DownloadAudio START");
        Debug.Log($"[ApiClient]   URL: {url}");
        Debug.Log($"[ApiClient]   Song code: {songCode}");
        Debug.Log($"[ApiClient]   Audio type: {audioType}");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            Debug.Log($"[ApiClient]   Sending GET request (audio)...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   HTTP Status: {www.responseCode}");
            Debug.Log($"[ApiClient]   Result: {www.result}");
            Debug.Log($"[ApiClient]   Downloaded bytes: {www.downloadedBytes}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] DownloadAudio FAILED — {www.error}");
                onError?.Invoke(www.error);
            }
            else
            {
                try
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                    {
                        Debug.LogError($"[ApiClient] DownloadAudio FAILED — GetContent returned null or failed. Ensure the audio format ({fileFormat}) is supported in WebGL.");
                        onError?.Invoke("Format audio tidak didukung atau corrupt.");
                    }
                    else
                    {
                        Debug.Log($"[ApiClient] DownloadAudio SUCCESS");
                        Debug.Log($"[ApiClient]   Duration: {clip.length:F2}s");
                        Debug.Log($"[ApiClient]   Frequency: {clip.frequency}Hz");
                        Debug.Log($"[ApiClient]   Channels: {clip.channels}");
                        onSuccess?.Invoke(clip);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ApiClient] Exception during DownloadAudio GetContent: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
        }
        Debug.Log($"[ApiClient] DownloadAudio END");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // LOAD LOCAL AUDIO FILE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static IEnumerator LoadLocalAudio(
        string filePath,
        Action<AudioClip> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[ApiClient] ════════════════════════════════════════");
        Debug.Log($"[ApiClient] LoadLocalAudio START");
        Debug.Log($"[ApiClient]   File: {filePath}");

        AudioType audioType = GetAudioTypeFromPath(filePath);
        string fileUrl = "file:///" + filePath.Replace("\\", "/");
        Debug.Log($"[ApiClient]   URL: {fileUrl}");
        Debug.Log($"[ApiClient]   Audio type: {audioType}");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, audioType))
        {
            Debug.Log($"[ApiClient]   Loading local file...");
            yield return www.SendWebRequest();

            Debug.Log($"[ApiClient]   Result: {www.result}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] LoadLocalAudio FAILED — {www.error}");
                onError?.Invoke(www.error);
            }
            else
            {
                try
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                    {
                        Debug.LogError($"[ApiClient] LoadLocalAudio FAILED — GetContent returned null or failed.");
                        onError?.Invoke("GetContent returned null or failed load state.");
                    }
                    else
                    {
                        Debug.Log($"[ApiClient] LoadLocalAudio SUCCESS");
                        Debug.Log($"[ApiClient]   Duration: {clip.length:F2}s");
                        Debug.Log($"[ApiClient]   Frequency: {clip.frequency}Hz");
                        Debug.Log($"[ApiClient]   Channels: {clip.channels}");
                        onSuccess?.Invoke(clip);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ApiClient] Exception during LoadLocalAudio GetContent: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
        }
        Debug.Log($"[ApiClient] LoadLocalAudio END");
    }

    private static AudioType GetAudioTypeFromPath(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        switch (ext)
        {
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".mp3": return AudioType.MPEG;
            default:     return AudioType.UNKNOWN;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // HELPERS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private static string GetMimeType(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLower();
        switch (ext)
        {
            case ".mp3": return "audio/mpeg";
            case ".wav": return "audio/wav";
            case ".ogg": return "audio/ogg";
            default:     return "application/octet-stream";
        }
    }
}
