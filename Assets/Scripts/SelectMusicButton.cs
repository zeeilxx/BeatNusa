using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System.Collections;
using SFB;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

/// <summary>
/// Lets the user select a local audio file, upload it to the backend for
/// beatmap generation, and automatically navigate to the gameplay scene.
/// 
/// Flow:
///   1. User clicks "Pilih Musik" → file browser opens
///   2. File selected → display filename, load audio clip locally
///   3. Auto-upload to backend → poll status until beatmap is generated
///   4. Beatmap completed → auto-navigate to gameplay scene
/// </summary>
public class SelectMusicButton : MonoBehaviour, IPointerDownHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text selectedMusicText;
    [SerializeField] private TMP_Text statusText;

    [Header("Loading Bar (Optional)")]
    [Tooltip("Image with Image Type = Filled. Set Fill Method to Horizontal.")]
    [SerializeField] private UnityEngine.UI.Image progressBarFill;
    [Tooltip("Panel that contains the progress bar + status. Activated during pipeline.")]
    [SerializeField] private GameObject loadingPanel;

    [Header("Audio")]
    [SerializeField] private AudioSource previewAudioSource; // Optional: for previewing

    [Header("Navigation")]
    [SerializeField] private string gameplaySceneName = "SongSelect";

    [Header("Polling Settings")]
    [SerializeField] private float pollIntervalSeconds = 3f;
    [SerializeField] private int maxPollAttempts = 60; // 60 × 3s = 3 minutes max

    private string selectedMusicPath;
    private string uploadedSongCode;
    private AudioClip loadedClip;
    private bool isUploading = false;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (progressBarFill != null)
            progressBarFill.fillAmount = 0f;

        #if UNITY_WEBGL && !UNITY_EDITOR
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
        #endif
    }

    /// <summary>
    /// Opens the file browser for audio selection.
    /// Called by a UI button.
    /// </summary>
    public void OpenMusicBrowser()
    {
        Debug.Log("[SelectMusicButton] OpenMusicBrowser() called.");

        if (isUploading)
        {
            SetStatus("Proses upload sedang berjalan.\nMohon tunggu...");
            Debug.LogWarning("[SelectMusicButton] Cannot open browser — upload in progress.");
            return;
        }

        var extensions = new[]
        {
            new ExtensionFilter("Audio Files", "mp3", "wav", "ogg")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Pilih File Musik",
            "",
            extensions,
            false
        );

        if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            selectedMusicPath = paths[0];
            string fileName = Path.GetFileName(paths[0]);

            if (selectedMusicText != null)
                selectedMusicText.text = fileName;

            // Save the local audio path and format for gameplay scene to use
            PlayerPrefs.SetString("SelectedAudioPath", selectedMusicPath);
            string format = Path.GetExtension(selectedMusicPath).ToLower().TrimStart('.');
            PlayerPrefs.SetString("SelectedAudioFormat", format);
            PlayerPrefs.Save();

            Debug.Log($"[SelectMusicButton] File selected: {selectedMusicPath}");
            SetStatus($"File dipilih: {fileName}\nMemuat audio...");

            // Start the full pipeline: load audio → upload → poll → navigate
            StartCoroutine(FullPipeline(selectedMusicPath));
        }
        else
        {
            Debug.Log("[SelectMusicButton] No file selected.");
        }
    }

    /// <summary>
    /// Full automated pipeline:
    ///   1. Load audio locally for preview/playback
    ///   2. Upload to backend
    ///   3. Poll until beatmap is generated
    ///   4. Auto-navigate to gameplay scene
    /// </summary>
    private IEnumerator FullPipeline(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string title = Path.GetFileNameWithoutExtension(filePath);

        Debug.Log($"[SelectMusicButton] ════════════════════════════════════════");
        Debug.Log($"[SelectMusicButton] FullPipeline START");
        Debug.Log($"[SelectMusicButton]   File: {filePath}");
        Debug.Log($"[SelectMusicButton]   Title: {title}");

        ShowLoading();
        SetProgress(0.1f);

        // ── Step 1: Load audio locally ──────────────────────
        Debug.Log($"[SelectMusicButton] ── Step 1: Loading audio locally...");
        SetStatus($"Memuat audio: {fileName}...");

        bool audioLoaded = false;
        yield return LoadLocalAudioClip(filePath, (success) => { audioLoaded = success; });

        if (!audioLoaded)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".mp3")
            {
                Debug.LogWarning("[SelectMusicButton] Local audio load failed (expected for MP3 on Windows due to FMOD local MP3 limitations). Continuing pipeline using backend download fallback...");
                audioLoaded = true; // Bypass local load failure for MP3 files
            }
            else
            {
                SetStatus($"Gagal memuat audio.\nCoba file lain.");
                Debug.LogError("[SelectMusicButton] Pipeline ABORTED — audio load failed.");
                HideLoading();
                yield break;
            }
        }

        SetProgress(0.3f);

        // ── Step 2: Upload to backend ───────────────────────
        Debug.Log($"[SelectMusicButton] ── Step 2: Uploading to backend...");
        SetStatus($"Mengupload: {fileName}...\n(Mohon tunggu)");

        isUploading = true;
        bool uploadDone = false;
        bool uploadSuccess = false;

        yield return ApiClient.UploadAudio(
            filePath,
            title,
            onSuccess: (response) =>
            {
                uploadedSongCode = response.song_code;

                // Save for gameplay scene
                PlayerPrefs.SetString("SelectedSongCode", response.song_code);
                string format = Path.GetExtension(filePath).ToLower().TrimStart('.');
                PlayerPrefs.SetString("SelectedAudioFormat", format);
                PlayerPrefs.Save();

                Debug.Log($"[SelectMusicButton]   Upload SUCCESS! Song code: {response.song_code}");
                SetStatus($"Upload berhasil!\nKode: {response.song_code}\nMemulai generate beatmap...");
                uploadSuccess = true;
                uploadDone = true;
            },
            onError: (error) =>
            {
                SetStatus($"Upload gagal: {error}\nCoba lagi.");
                Debug.LogError($"[SelectMusicButton]   Upload FAILED: {error}");
                uploadDone = true;
            }
        );

        while (!uploadDone) yield return null;

        if (!uploadSuccess)
        {
            isUploading = false;
            Debug.LogError("[SelectMusicButton] Pipeline ABORTED — upload failed.");
            HideLoading();
            yield break;
        }

        SetProgress(0.6f);

        // ── Step 3: Poll until beatmap is generated ─────────
        Debug.Log($"[SelectMusicButton] ── Step 3: Polling beatmap generation status...");
        bool beatmapReady = false;
        yield return PollSongStatus(uploadedSongCode, (success) => { beatmapReady = success; });

        isUploading = false;

        if (!beatmapReady)
        {
            Debug.LogError("[SelectMusicButton] Pipeline ABORTED — beatmap generation failed or timed out.");
            HideLoading();
            yield break;
        }

        SetProgress(1f);

        // ── Step 4: Navigate to gameplay ────────────────────
        Debug.Log($"[SelectMusicButton] ── Step 4: Navigating to gameplay scene...");
        SetStatus("Beatmap siap!\nMemuat game...");

        // Short delay so user can see the success message
        yield return new WaitForSeconds(1.5f);

        Debug.Log($"[SelectMusicButton]   Loading scene: {gameplaySceneName}");
        Debug.Log($"[SelectMusicButton]   PlayerPrefs 'SelectedSongCode': {PlayerPrefs.GetString("SelectedSongCode", "")}");
        Debug.Log($"[SelectMusicButton]   PlayerPrefs 'SelectedAudioPath': {PlayerPrefs.GetString("SelectedAudioPath", "")}");
        Debug.Log($"[SelectMusicButton] FullPipeline END — navigating now!");

        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Upload the selected audio to the backend for AI beatmap generation.
    /// Can also be called manually by a separate UI button if needed.
    /// </summary>
    public void UploadAndGenerate()
    {
        if (string.IsNullOrEmpty(selectedMusicPath))
        {
            SetStatus("Pilih file audio terlebih dahulu!");
            return;
        }

        if (isUploading)
        {
            SetStatus("Upload sedang berlangsung...");
            return;
        }

        // If called manually, run the pipeline from upload step
        StartCoroutine(ManualUploadPipeline());
    }

    private IEnumerator ManualUploadPipeline()
    {
        string title = Path.GetFileNameWithoutExtension(selectedMusicPath);
        isUploading = true;

        Debug.Log($"[SelectMusicButton] ManualUploadPipeline START");
        ShowLoading();
        SetProgress(0.3f);
        SetStatus("Mengupload audio...\n(Mohon tunggu)");

        bool uploadDone = false;
        bool uploadSuccess = false;

        yield return ApiClient.UploadAudio(
            selectedMusicPath,
            title,
            onSuccess: (response) =>
            {
                uploadedSongCode = response.song_code;
                PlayerPrefs.SetString("SelectedSongCode", response.song_code);
                string format = Path.GetExtension(selectedMusicPath).ToLower().TrimStart('.');
                PlayerPrefs.SetString("SelectedAudioFormat", format);
                PlayerPrefs.Save();

                Debug.Log($"[SelectMusicButton]   Upload SUCCESS! Song code: {response.song_code}");
                SetStatus($"Upload berhasil!\nKode: {response.song_code}\nMemulai generate beatmap...");
                uploadSuccess = true;
                uploadDone = true;
            },
            onError: (error) =>
            {
                SetStatus($"Upload gagal: {error}");
                Debug.LogError($"[SelectMusicButton]   Upload FAILED: {error}");
                uploadDone = true;
            }
        );

        while (!uploadDone) yield return null;

        if (uploadSuccess && !string.IsNullOrEmpty(uploadedSongCode))
        {
            bool beatmapReady = false;
            yield return PollSongStatus(uploadedSongCode, (success) => { beatmapReady = success; });

            if (beatmapReady)
            {
                SetProgress(1f);
                SetStatus("Beatmap siap!\nMemuat game...");
                yield return new WaitForSeconds(1.5f);

                Debug.Log($"[SelectMusicButton] Navigating to {gameplaySceneName}");
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                HideLoading();
            }
        }
        else
        {
            HideLoading();
        }

        isUploading = false;
    }

    /// <summary>
    /// Polls GET /api/songs/{song_code}/status every few seconds
    /// until the process_status is "completed" or "failed", or max attempts reached.
    /// </summary>
    private IEnumerator PollSongStatus(string songCode, System.Action<bool> onComplete)
    {
        int attempts = 0;

        while (attempts < maxPollAttempts)
        {
            attempts++;
            
            // Calculate progress between 0.6 and 0.9 based on attempts
            float currentProgress = 0.6f + (0.3f * ((float)attempts / maxPollAttempts));
            SetProgress(currentProgress);

            SetStatus($"Generating beatmap...\n({attempts}/{maxPollAttempts})\nMohon tunggu...");

            bool pollDone = false;
            string currentStatus = "";

            yield return ApiClient.CheckSongStatus(
                songCode,
                onSuccess: (response) =>
                {
                    currentStatus = response.process_status;
                    Debug.Log($"[SelectMusicButton] Poll #{attempts}: status = '{currentStatus}'");
                    pollDone = true;
                },
                onError: (error) =>
                {
                    Debug.LogWarning($"[SelectMusicButton] Poll #{attempts}: FAILED — {error}");
                    currentStatus = "error";
                    pollDone = true;
                }
            );

            while (!pollDone) yield return null;

            // Check result
            if (currentStatus == "done" || currentStatus == "completed")
            {
                SetStatus($"Beatmap berhasil di-generate!\nKode: {songCode}\n\n✓ Siap dimainkan!");
                Debug.Log("[SelectMusicButton] Beatmap generation COMPLETED!");
                onComplete?.Invoke(true);
                yield break;
            }
            else if (currentStatus == "failed" || currentStatus == "error")
            {
                SetStatus($"Generate beatmap gagal.\nKode: {songCode}\nSilakan coba upload ulang.");
                Debug.LogError("[SelectMusicButton] Beatmap generation FAILED.");
                onComplete?.Invoke(false);
                yield break;
            }

            // Still processing — wait before next poll
            yield return new WaitForSeconds(pollIntervalSeconds);
        }

        // Max attempts reached
        SetStatus($"Timeout: beatmap belum selesai\nsetelah {maxPollAttempts} percobaan.\nCoba refresh nanti.");
        Debug.LogWarning("[SelectMusicButton] Max poll attempts reached (timeout).");
        onComplete?.Invoke(false);
    }

    /// <summary>
    /// Load audio clip from local file path using UnityWebRequest.
    /// </summary>
    private IEnumerator LoadLocalAudioClip(string filePath, System.Action<bool> onComplete = null)
    {
        AudioType audioType = GetAudioType(filePath);
        string fileUrl = "file:///" + filePath.Replace("\\", "/");

        Debug.Log($"[SelectMusicButton] LoadLocalAudioClip: {filePath}");
        Debug.Log($"[SelectMusicButton]   AudioType: {audioType}");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SelectMusicButton] LoadLocalAudioClip FAILED: {www.error}");
                SetStatus("Gagal memuat audio: " + www.error);
                onComplete?.Invoke(false);
            }
            else
            {
                try
                {
                    loadedClip = DownloadHandlerAudioClip.GetContent(www);
                    if (loadedClip == null || loadedClip.loadState == AudioDataLoadState.Failed)
                    {
                        Debug.LogError("[SelectMusicButton] DownloadHandlerAudioClip returned null or failed load state.");
                        onComplete?.Invoke(false);
                    }
                    else
                    {
                        Debug.Log($"[SelectMusicButton] LoadLocalAudioClip SUCCESS: {loadedClip.length:F2}s, {loadedClip.frequency}Hz");
                        SetStatus($"Audio dimuat: {Path.GetFileName(filePath)} ({loadedClip.length:F1}s)");

                        // Store the clip for preview
                        if (previewAudioSource != null)
                        {
                            previewAudioSource.clip = loadedClip;
                        }

                        onComplete?.Invoke(true);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SelectMusicButton] Exception during DownloadHandlerAudioClip.GetContent: {ex.Message}");
                    onComplete?.Invoke(false);
                }
            }
        }
    }

    private AudioType GetAudioType(string filePath)
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

    private void SetStatus(string message)
    {
        Debug.Log($"[SelectMusicButton] STATUS: {message.Replace("\n", " | ")}");

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
        }
    }

    /// <summary>
    /// Set the progress bar fill (0.0 to 1.0).
    /// </summary>
    private void SetProgress(float value)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = Mathf.Clamp01(value);
    }

    private void ShowLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        SetProgress(0f);
    }

    private void HideLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public string GetSelectedMusicPath()
    {
        return selectedMusicPath;
    }

    public string GetUploadedSongCode()
    {
        return uploadedSongCode;
    }

    public AudioClip GetLoadedClip()
    {
        return loadedClip;
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string id);

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[SelectMusicButton] OnPointerDown() called (WebGL).");
        if (isUploading)
        {
            SetStatus("Proses upload sedang berjalan.\nMohon tunggu...");
            return;
        }
        UploadFile(gameObject.name);
    }

    // Called from browser in WebGL when a file is selected
    public void OnFileUploaded(string urlWithFilename)
    {
        Debug.Log($"[SelectMusicButton] OnFileUploaded WebGL: {urlWithFilename}");
        StartCoroutine(WebGLPipeline(urlWithFilename));
    }

    private IEnumerator WebGLPipeline(string urlWithFilename)
    {
        string[] parts = urlWithFilename.Split('>');
        string blobUrl = parts[0];
        string fileName = parts.Length > 1 ? parts[1] : "uploaded_song.mp3";
        string title = Path.GetFileNameWithoutExtension(fileName);

        Debug.Log($"[SelectMusicButton] WebGLPipeline START");
        Debug.Log($"[SelectMusicButton]   Blob URL: {blobUrl}");
        Debug.Log($"[SelectMusicButton]   FileName: {fileName}");

        ShowLoading();
        SetProgress(0.1f);

        // ── Step 1: Load/Download audio bytes from blob URL ──────────────────
        SetStatus("Membaca file audio...");
        byte[] fileData = null;

        using (UnityWebRequest www = UnityWebRequest.Get(blobUrl))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SelectMusicButton] Failed to read blob data: {www.error}");
                SetStatus("Gagal membaca audio.");
                HideLoading();
                yield break;
            }
            fileData = www.downloadHandler.data;
        }

        SetProgress(0.3f);

        // Save SelectedAudioPath and SelectedAudioFormat
        PlayerPrefs.SetString("SelectedAudioPath", blobUrl);
        string format = Path.GetExtension(fileName).ToLower().TrimStart('.');
        PlayerPrefs.SetString("SelectedAudioFormat", format);
        PlayerPrefs.Save();

        // ── Step 2: Upload to backend ───────────────────────
        SetStatus("Mengupload audio...\n(Mohon tunggu)");
        isUploading = true;

        bool uploadDone = false;
        bool uploadSuccess = false;

        yield return ApiClient.UploadAudio(
            fileData,
            fileName,
            title,
            onSuccess: (response) =>
            {
                uploadedSongCode = response.song_code;
                PlayerPrefs.SetString("SelectedSongCode", response.song_code);
                PlayerPrefs.Save();

                Debug.Log($"[SelectMusicButton]   Upload SUCCESS! Song code: {response.song_code}");
                SetStatus($"Upload berhasil!\nKode: {response.song_code}\nMemulai generate beatmap...");
                uploadSuccess = true;
                uploadDone = true;
            },
            onError: (error) =>
            {
                SetStatus($"Upload gagal: {error}\nCoba lagi.");
                Debug.LogError($"[SelectMusicButton]   Upload FAILED: {error}");
                uploadDone = true;
            }
        );

        while (!uploadDone) yield return null;

        if (!uploadSuccess)
        {
            isUploading = false;
            HideLoading();
            yield break;
        }

        SetProgress(0.6f);

        // ── Step 3: Poll until beatmap is generated ─────────
        bool beatmapReady = false;
        yield return PollSongStatus(uploadedSongCode, (success) => { beatmapReady = success; });

        isUploading = false;

        if (!beatmapReady)
        {
            HideLoading();
            yield break;
        }

        SetProgress(1f);

        // ── Step 4: Navigate to gameplay ────────────────────
        SetStatus("Beatmap siap!\nMemuat game...");
        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene(gameplaySceneName);
    }
    #else
    public void OnPointerDown(PointerEventData eventData) {}
    #endif
}