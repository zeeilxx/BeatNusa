using UnityEngine;

public class OpenPopupButton : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.gray;
    public Color pressedColor = Color.black;

    [SerializeField] private GameObject popupToOpen;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    private void OnMouseEnter()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    private void OnMouseDown()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pressedColor;
        }

        if (popupToOpen != null)
        {
            Invoke(nameof(OpenPopup), 0.1f);
        }
        else
        {
            Debug.LogError("Popup to open is not assigned!");
        }
    }

    private void OnMouseUp()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    private void OpenPopup()
    {
        popupToOpen.SetActive(true);
    }
}