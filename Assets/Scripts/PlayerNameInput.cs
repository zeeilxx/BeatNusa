using UnityEngine;
using TMPro;

/// <summary>
/// Simple player name input prompt.
/// Stores the player name in PlayerPrefs for use across scenes.
/// Attach to a UI panel with a TMP_InputField and a confirm button.
/// </summary>
public class PlayerNameInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject namePromptPanel;

    private const string PLAYER_NAME_KEY = "PlayerName";
    private const string DEFAULT_NAME = "Player";

    void Start()
    {
        Debug.Log("[PlayerNameInput] Start() initializing...");
        Debug.Log($"[PlayerNameInput]   Input field: {(nameInputField != null ? "OK" : "NOT ASSIGNED")}");
        Debug.Log($"[PlayerNameInput]   Prompt panel: {(namePromptPanel != null ? namePromptPanel.name : "NOT ASSIGNED")}");

        // Load saved name if exists
        string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "");

        if (!string.IsNullOrEmpty(savedName))
        {
            nameInputField.text = savedName;
            Debug.Log($"[PlayerNameInput]   Loaded saved name: '{savedName}'");
        }
        else
        {
            Debug.Log($"[PlayerNameInput]   No saved name found. Default: '{DEFAULT_NAME}'");
        }
    }

    /// <summary>
    /// Called by the confirm button. Saves the player name and hides the prompt.
    /// </summary>
    public void ConfirmName()
    {
        string playerName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = DEFAULT_NAME;
            Debug.Log($"[PlayerNameInput] ConfirmName: Empty input, using default: '{DEFAULT_NAME}'");
        }

        PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerNameInput] ConfirmName: Player name saved as '{playerName}'");

        if (namePromptPanel != null)
        {
            namePromptPanel.SetActive(false);
            Debug.Log("[PlayerNameInput]   Prompt panel hidden.");
        }
    }

    /// <summary>
    /// Show the name prompt panel.
    /// </summary>
    public void ShowPrompt()
    {
        Debug.Log("[PlayerNameInput] ShowPrompt() called.");
        if (namePromptPanel != null)
        {
            namePromptPanel.SetActive(true);
            Debug.Log("[PlayerNameInput]   Prompt panel shown.");
        }
        else
        {
            Debug.LogWarning("[PlayerNameInput]   Prompt panel is not assigned!");
        }
    }

    /// <summary>
    /// Get the current player name from PlayerPrefs.
    /// </summary>
    public static string GetPlayerName()
    {
        string name = PlayerPrefs.GetString(PLAYER_NAME_KEY, DEFAULT_NAME);
        Debug.Log($"[PlayerNameInput] GetPlayerName() returning: '{name}'");
        return name;
    }
}
