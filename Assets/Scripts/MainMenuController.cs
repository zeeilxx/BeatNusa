using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void GoToSongSelect()
    {
        Debug.Log("GoToSongSelect button clicked");
        SceneManager.LoadScene("SongSelect");
    }
}