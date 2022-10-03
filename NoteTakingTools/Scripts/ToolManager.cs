using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing the Manager controlling the note-taking tools
// Takes care of picking, starting and ending the desired note-taking tool.
public class ToolManager : MonoBehaviour
{
    // References to the controllers and headset camera are only present in this class. 
    // This makes the bundle easier to import into a new project. 
    // Other classes, that need the references to the controllers or the headset get
    // them on their start.

    [SerializeField]
    private GameObject leftController;

    [SerializeField]
    private GameObject rightController;


    [SerializeField]
    private GameObject headsetCamera;


    [SerializeField]
    private DrawingManager drawingManager;


    [SerializeField]

    private StickyNotesManager2 stickyNoteManager;


    [SerializeField]

    private VoiceRecordingManager voiceRecordingManager;


    [SerializeField]
    private MenuManager toolMenuManager;


    [SerializeField]
    private ToolInputActions toolInputActions;

    [SerializeField]

    private GameObject textDescriptionsLeft;


    [SerializeField]

    private GameObject textDescriptionsRight;


    private Transform currControllerTransform = null;

    // Starts the Tool Menu.
    // The menu is spawned around the controller, on which the button for spawning was pressed.
    // This controllers transform is sent to the menu so it can be positioned correctly. 
    // The hand that started the menu then controlls the note taking tools, untill changed through input actions
    public void StartToolMenu(bool leftHandMenu)
    {
        textDescriptionsLeft.SetActive(false);
        textDescriptionsRight.SetActive(false);
        currControllerTransform = GetController(leftHandMenu);
        toolMenuManager.StartMenu(currControllerTransform);
    }

    public int EndToolMenu()
    {
        int pickedChoice = toolMenuManager.GetCurrentChoice();
        toolMenuManager.ResetMenu();
        ActivateMenuChange(pickedChoice);
        return pickedChoice;
    }

    public void ActivateMenuChange(int val)
    {
        textDescriptionsLeft.SetActive(false);
        textDescriptionsRight.SetActive(false);
        switch (val)
        {
            case (0):
                StartPen();
                break;
            case (1):
                StartNote();
                break;
            case (2):
                StartVoice();
                break;
            default:
                textDescriptionsLeft.SetActive(true);
                textDescriptionsRight.SetActive(true);
                break;
        }
    }

    public void StartPen()
    {
        drawingManager.PenPicked(currControllerTransform);
    }


    public void StartNote()
    {
        stickyNoteManager.StickyNotePicked(currControllerTransform, headsetCamera);
    }


    public void StartVoice()
    {
        voiceRecordingManager.VoiceRecordingPicked(currControllerTransform);
    }


    public void OptionEnded()
    {
        toolInputActions.ForceResetOptions();
        textDescriptionsLeft.SetActive(true);
        textDescriptionsRight.SetActive(true);
        currControllerTransform = null;
    }

    // Editing the created note is a special circumstance
    // Since the editing is called from the note, which only has the transform set up,
    // we need to find out which controller was used.
    public void EditNote(Transform currControllerTransform)
    {
        if (currControllerTransform.gameObject.name.ToLower().Contains("left"))
            toolInputActions.EditNote(true);
        else
            toolInputActions.EditNote(false);
    }

    public Transform GetController(bool isLeftHand)
    {
        if (isLeftHand)
            return leftController.transform;
        return rightController.transform;
    }

    public GameObject GetCameraObject()
    {
        return headsetCamera;
    }
}
