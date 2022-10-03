using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsible for managing an arbitrary radial menu
// Takes care of spawning the menu in the correct position and getting the chosen option
public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject radialMenu;

    private Transform currControllerTransform = null;

    // Hover value = which option is currently picked
    // -1 means no option is picked
    private int hoverValue = -1;
    private int hoverValueOnResetingMenu = -1;

    public void StartMenu(Transform controllerTransform)
    {
        // the hover gets reset when the menu spawns 
        // This is done because of the "special" private button
        hoverValueOnResetingMenu = -1;
        currControllerTransform = controllerTransform;
        SetMenu();
    }


    // Deactivates the radial menu and resets the option
    public void ResetMenu()
    {
        hoverValueOnResetingMenu = hoverValue;
        hoverValue = -1;
        radialMenu.SetActive(false);
        currControllerTransform = null;
    }

    // Sets the proper position of the menu
    private void SetMenu()
    {
        radialMenu.SetActive(true);
        radialMenu.transform.position = currControllerTransform.position;
        radialMenu.transform.rotation = Quaternion.Euler(currControllerTransform.eulerAngles.x, currControllerTransform.eulerAngles.y, 0);
    }


    public void SetHover(int value)
    {
        hoverValue = value;
    }


    public int GetCurrentChoice()
    {
        return hoverValue;
    }

    public int GetCurrentOnResetingMenu()
    {
        return hoverValueOnResetingMenu;
    }

}
