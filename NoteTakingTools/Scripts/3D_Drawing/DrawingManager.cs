using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsible for managing the 3D drawing tool. 
// Takes care of turning the menu on and of, making the pen visible and invisible
// checking when the pen is drawing and when its deactivated.
// It makes sure that the drawing tools are set up well, with their default options.
public class DrawingManager : MonoBehaviour
{
    [SerializeField]
    private GameObject pen;

    private DrawingPen penScript;


    [SerializeField]
    private MenuManager drawMenuManager;


    private Transform currControllerTransform;


    [SerializeField]

    private ToolManager toolManager;


    private bool privateDraw = false;

    private bool penFirstUse = true;


    private static readonly float[] WIDTHS = new float[] { 0.001f, 0.005f, 0.01f };
    private static readonly float DEFAULT_WIDTH = WIDTHS[1];

    private static readonly Color[] COLORS = new Color[] { Color.black, Color.red, Color.green, Color.blue };
    private static readonly Color DEFAULT_COLOR = COLORS[0];


    void Start()
    {
        penScript = pen.GetComponent<DrawingPen>();
        if (pen.activeSelf) pen.SetActive(false);
    }

    // The option for the pen is picked
    // The pen appears in the users hand with the default setUp
    public void PenPicked(Transform controllerTransform)
    {
        currControllerTransform = controllerTransform;

        if (penFirstUse)
        {
            penFirstUse = false;
            penScript.SetUp(DEFAULT_COLOR, DEFAULT_WIDTH);
        }

        // the pen will be put into the hand that selected the menu
        ActivateRadialChange(-1);
    }

    // Starting the radial menu and hiding the pen
    public void StartRadialMenu(Transform controllerTransform)
    {
        currControllerTransform = controllerTransform;
        pen.SetActive(false);
        drawMenuManager.StartMenu(currControllerTransform);
    }

    // Closing the radial menu and checking the picked option
    public int EndRadialMenu()
    {
        int pickedChoice = drawMenuManager.GetCurrentChoice();
        drawMenuManager.ResetMenu();
        ActivateRadialChange(pickedChoice);
        return pickedChoice;
    }

    // Activating the option picked on the menu
    // The menu is closed and it gets replaced by the pen again
    public void ActivateRadialChange(int val)
    {
        pen.SetActive(true);

        //the Pen is set up with the correct hand holding it
        penScript.SetController(currControllerTransform);

        switch (val)
        {
            // options 0,1,2,3 represent colors
            case int n when (n >= 0 && n <= 3):
                penScript.SetColor(COLORS[n]);
                break;
            // options 4,5,6 represent widths
            case int n when (n > 3 && n <= 6):
                penScript.SetWidth(WIDTHS[n - 4]);
                break;
            // last option changes the privacy of the drawing
            case (7):
                privateDraw = !privateDraw;
                penScript.PrivateDrawing(privateDraw);
                break;
        }
    }

    // The button for drawing was pressed - the drawing process starts
    public void StartPenAction()
    {
        if (!pen.activeSelf) return;
        penScript.Activate();
    }

    // The button for drawing was released - the drawing process ends
    public void EndPenAction()
    {
        if (!pen.activeSelf) return;
        penScript.Deactivate();
    }

    // End the option to draw in 3D
    public void EndPen()
    {
        if (!pen.activeSelf) return;
        pen.SetActive(false);

        // Send a message that the pen option ended
        toolManager.OptionEnded();
    }
}
