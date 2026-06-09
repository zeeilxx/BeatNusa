using UnityEngine;

public class OpenPopupButton : MonoBehaviour
{
    [SerializeField] private GameObject popupToOpen;

    public void OpenPopup()
    {
        Debug.Log("Input Music button clicked");

        if (popupToOpen == null)
        {
            Debug.LogError("Popup to open is not assigned!");
            return;
        }

        popupToOpen.SetActive(true);
        popupToOpen.transform.SetAsLastSibling();

        Debug.Log("Popup opened: " + popupToOpen.name);
        Debug.Log("Active Self: " + popupToOpen.activeSelf);
        Debug.Log("Active In Hierarchy: " + popupToOpen.activeInHierarchy);
    }
}