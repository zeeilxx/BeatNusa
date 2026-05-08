using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Dynamic song list UI that fetches available songs from the backend API.
/// Each song entry shows title, artist, BPM, and a Play button.
/// 
/// Attach this to a panel in the SongSelect scene.
/// Requires:
///   - A content parent Transform to hold song entry items
///   - A song entry prefab with TextMeshPro fields and a play button
///   - A loading text indicator
/// </summary>
public class SongListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;     // ScrollView content container
    [SerializeField] private GameObject songEntryPrefab;  // Prefab for each song row
    [SerializeField] private TMP_Text loadingText;        // "Loading songs..." indicator
    [SerializeField] private TMP_Text errorText;          // Error display

    [Header("Gameplay Scene")]
    [SerializeField] private string gameplaySceneName = "LevelOne";

    private SongListItem[] songs;

    void Start()
    {
        Debug.Log("[SongListUI] Start() — Initializing song list UI...");
        Debug.Log($"[SongListUI]   Content parent: {(contentParent != null ? contentParent.name : "NOT ASSIGNED")}");
        Debug.Log($"[SongListUI]   Song entry prefab: {(songEntryPrefab != null ? songEntryPrefab.name : "NOT ASSIGNED")}");
        Debug.Log($"[SongListUI]   Gameplay scene: {gameplaySceneName}");
        RefreshSongList();
    }

    /// <summary>
    /// Fetch the song list from the backend and populate the UI.
    /// </summary>
    public void RefreshSongList()
    {
        Debug.Log("[SongListUI] RefreshSongList() — Fetching songs from backend...");

        // Clear existing entries
        int childCount = contentParent.childCount;
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log($"[SongListUI]   Cleared {childCount} existing entries.");

        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (errorText != null) errorText.gameObject.SetActive(false);

        StartCoroutine(ApiClient.GetSongList(
            onSuccess: (songList) =>
            {
                songs = songList;
                if (loadingText != null) loadingText.gameObject.SetActive(false);

                Debug.Log($"[SongListUI]   Received {songList.Length} songs from backend.");

                if (songList.Length == 0)
                {
                    Debug.LogWarning("[SongListUI]   No songs available.");
                    if (errorText != null)
                    {
                        errorText.text = "Belum ada lagu. Upload audio terlebih dahulu!";
                        errorText.gameObject.SetActive(true);
                    }
                    return;
                }

                PopulateSongList(songList);
            },
            onError: (error) =>
            {
                Debug.LogError($"[SongListUI]   GetSongList FAILED: {error}");
                if (loadingText != null) loadingText.gameObject.SetActive(false);
                if (errorText != null)
                {
                    errorText.text = "Gagal memuat daftar lagu:\n" + error;
                    errorText.gameObject.SetActive(true);
                }
            }
        ));
    }

    private void PopulateSongList(SongListItem[] songList)
    {
        Debug.Log($"[SongListUI] PopulateSongList() — Creating {songList.Length} entries...");

        foreach (var song in songList)
        {
            GameObject entry = Instantiate(songEntryPrefab, contentParent);
            Debug.Log($"[SongListUI]   Creating entry: {song.title} ({song.song_code}) — BPM: {song.bpm}, Status: {song.process_status}");

            TMP_Text titleText = FindChildText(entry, "TitleText");
            TMP_Text artistText = FindChildText(entry, "ArtistText");
            TMP_Text bpmText = FindChildText(entry, "BpmText");

            if (titleText != null)
                titleText.text = song.title;

            if (artistText != null)
                artistText.text = !string.IsNullOrEmpty(song.artist) ? song.artist : "Unknown Artist";

            if (bpmText != null)
                bpmText.text = song.bpm > 0 ? Mathf.RoundToInt(song.bpm) + " BPM" : "";

            // Setup play button
            var playButton = entry.GetComponentInChildren<UnityEngine.UI.Button>();
            if (playButton != null)
            {
                string songCode = song.song_code;
                string songTitle = song.title;
                string fileFormat = song.file_format;
                playButton.onClick.AddListener(() => OnSongSelected(songCode, songTitle, fileFormat));
            }
            else
            {
                Debug.LogWarning($"[SongListUI]     No PlayButton found in entry prefab for '{song.title}'!");
            }
        }
        Debug.Log($"[SongListUI]   ✓ Song list populated.");
    }

    private void OnSongSelected(string songCode, string title, string fileFormat)
    {
        Debug.Log($"[SongListUI] OnSongSelected() — '{title}' ({songCode}) — Format: {fileFormat}");
        Debug.Log($"[SongListUI]   Saving to PlayerPrefs...");

        // Store the selected song code for the gameplay scene to use
        PlayerPrefs.SetString("SelectedSongCode", songCode);
        PlayerPrefs.SetString("SelectedSongTitle", title);
        PlayerPrefs.SetString("SelectedAudioFormat", string.IsNullOrEmpty(fileFormat) ? "wav" : fileFormat);

        // Clear local audio path since we're selecting from the list (not a local file)
        PlayerPrefs.SetString("SelectedAudioPath", "");
        PlayerPrefs.Save();

        Debug.Log($"[SongListUI]   PlayerPrefs saved. Loading scene: {gameplaySceneName}");

        // Load the gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    private TMP_Text FindChildText(GameObject parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        if (child != null)
            return child.GetComponent<TMP_Text>();
        return null;
    }
}
