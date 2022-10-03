using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing a button on the Sticky Notes Radial menu for erasing
// This button functions as normal, but it changes its color
// based on the privacy settings. mWhen drawing on a private note, the button
// matches the color of private note. When drawing on a public note, it matches
// the public note color.
public class EraseMenuButton : MonoBehaviour
{

    [SerializeField]
    StickyNotesManager2 stickyNotesManager;

    [SerializeField]
    MenuButton eraseMenuButton;

    [SerializeField]
    Material privateMat;

    [SerializeField]
    Material highlightedPrivateMat;

    [SerializeField]
    Material publicMat;

    [SerializeField]
    Material highlightedPublicMat;


    void OnEnable()
    {
        if (stickyNotesManager.GetPrivacy())
        {
            eraseMenuButton.SetMaterials(privateMat, highlightedPrivateMat);
        }
        else
        {
            eraseMenuButton.SetMaterials(publicMat, highlightedPublicMat);
        }
    }
}
