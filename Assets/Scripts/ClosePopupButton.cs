using UnityEngine;

public class ClosePopupButton : MonoBehaviour
{
    [SerializeField] private GameObject popupToClose;

    public void ClosePopup()
    {
        if (popupToClose != null)
            popupToClose.SetActive(false);
        else
            Debug.LogError("Popup to close is not assigned!");
    }
}