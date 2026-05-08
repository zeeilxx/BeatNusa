using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenuButton : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.gray;
    public Color pressedColor = Color.black;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    private void OnMouseEnter()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = hoverColor;
    }

    private void OnMouseExit()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    private void OnMouseDown()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = pressedColor;

        Invoke(nameof(LoadMainMenu), 0.1f);
    }

    private void OnMouseUp()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = hoverColor;
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}