using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class representing the button for privacy in drawing and sticky notes
// It changes the text between private and public when the privacy is changed
public class PrivacyMenuButton : MonoBehaviour
{
    [SerializeField]
    private MenuManager menuManager;

    [SerializeField]
    private GameObject privateText;

    [SerializeField]
    private GameObject publicText;

    void OnDisable()
    {
        // privacy button is represented by the number 7
        // if this option is picked the text switches
        if (menuManager.GetCurrentOnResetingMenu() == 7)
        {
            privateText.SetActive(!privateText.activeSelf);
            publicText.SetActive(!publicText.activeSelf);
        }
    }
}
