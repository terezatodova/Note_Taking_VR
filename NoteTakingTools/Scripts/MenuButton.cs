using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class respresenting one button of the radial menu
// If a controller hovers over a button when the menu ends, the option is selected.
public class MenuButton : MonoBehaviour
{
    [SerializeField]
    private MenuManager menuManager;

    // Set on each button separately, through the unity editor
    // Represents which option will be picked
    [SerializeField]
    int option = -1;

    [SerializeField]
    Material normalMat;

    [SerializeField]
    Material highlightedMat;

    private Material material;

    private bool highlighted = false;

    void Awake()
    {
        if (!material) 
            material = GetComponent<Renderer>().material;
    }

    void OnDisable()
    {
        material.color = normalMat.color;
    }

    public void Highlight()
    {
        material.color = highlightedMat.color;
        menuManager.SetHover(option);
    }


    public void StopHighlight()
    {
        material.color = normalMat.color;
        menuManager.SetHover(-1);
    }

    public void SetMaterials(Material newNormalMat, Material newHighlightedMat)
    {
        normalMat = newNormalMat;
        highlightedMat = newHighlightedMat;
        if (!material) 
            material = GetComponent<Renderer>().material;
        material.color = normalMat.color;
    }

}
