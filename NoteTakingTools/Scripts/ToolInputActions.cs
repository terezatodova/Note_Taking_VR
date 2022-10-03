using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// This class looks at inputs from the users controllers surrounding the Note-taking process
public class ToolInputActions : MonoBehaviour
{
    // Activate and deactivate on private/public notes is done through their own script,
    // since every note listens separately

    [SerializeField]
    ToolManager toolManager;

    [SerializeField]
    DrawingManager drawingManager;


    [SerializeField]
    StickyNotesManager2 stickyNoteManager;


    [SerializeField]
    VoiceRecordingManager voiceRecordingManager;



    // action for manipulating with radial menus 
    // recommended setup - left and right secondary button presses
    [SerializeField]
    private InputActionReference menuOpenActionLeft;

    [SerializeField]
    private InputActionReference menuOpenActionRight;


    // action for activating object - trigger press
    // recommended setup - left and right trigger presses (activate)
    [SerializeField]
    private InputActionReference activateActionLeft;

    [SerializeField]
    private InputActionReference activateActionRight;

    private bool leftHand = false;

    private bool menuOpen = false;

    private bool actionStarted = false;

    private int pickedChoice = -1;

    // The note-taking process can be in 4 different modes
    // based on the note we open a specific menu or start a specific action
    // This depends on whether a tool is picked and if so, which one
    enum Mode
    {
        ToolMenu,
        Draw3D,
        StickyNote,
        Voice
    }


    // We always start the note-taking with no tool selected
    private Mode currMode = Mode.ToolMenu;



    void OnEnable()
    {
        menuOpenActionLeft.action.started += MenuActionLeft_performed;
        menuOpenActionLeft.action.canceled += MenuActionLeft_performed;

        menuOpenActionRight.action.started += MenuActionRight_performed;
        menuOpenActionRight.action.canceled += MenuActionRight_performed;

        activateActionLeft.action.started += ActivateActionLeft_performed;
        activateActionLeft.action.canceled += ActivateActionLeft_performed;

        activateActionRight.action.started += ActivateActionRight_performed;
        activateActionRight.action.canceled += ActivateActionRight_performed;
    }

    void OnDisable()
    {
        menuOpenActionLeft.action.started -= MenuActionLeft_performed;
        menuOpenActionLeft.action.canceled -= MenuActionLeft_performed;

        menuOpenActionRight.action.started -= MenuActionRight_performed;
        menuOpenActionRight.action.canceled -= MenuActionRight_performed;

        activateActionLeft.action.started -= ActivateActionLeft_performed;
        activateActionLeft.action.canceled -= ActivateActionLeft_performed;

        activateActionRight.action.started -= ActivateActionRight_performed;
        activateActionRight.action.canceled -= ActivateActionRight_performed;
    }

    private void MenuActionLeft_performed(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            StartMenu(true);
            leftHand = true;
        }
        else if (ctx.canceled)
        {
            EndMenu(true);
        }
    }


    private void MenuActionRight_performed(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            StartMenu(false);
            leftHand = false;
        }
        else if (ctx.canceled)
        {
            EndMenu(false);
        }
    }

    private void ActivateActionLeft_performed(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            ActivateActionStart(true);
        }
        else if (ctx.canceled)
        {
            ActivateActionEnd(true);
        }
    }

    private void ActivateActionRight_performed(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            ActivateActionStart(false);
        }
        else if (ctx.canceled)
        {
            ActivateActionEnd(false);
        }
    }

    // Starting the menu can happen in all modes - with the sticky notes and pen it spawns their menus,
    // With the note taking tool it spawns the menu for choosing a tool. 
    // With the voice recordings it ends the tool and moves to the Base Menu.
    private void StartMenu(bool isLeftController)
    {
        leftHand = isLeftController;
        menuOpen = true;

        switch (currMode)
        {
            case Mode.ToolMenu:
                toolManager.StartToolMenu(leftHand);
                break;
            case Mode.Draw3D:
                // open 3D draw radial menu
                drawingManager.StartRadialMenu(toolManager.GetController(leftHand));
                break;
            case Mode.StickyNote:
                // open radial menu of notes 
                stickyNoteManager.StartRadialMenu(toolManager.GetController(leftHand));
                break;
            case Mode.Voice:
                // ends the voice recording tool by opening the Tool Menu
                voiceRecordingManager.EndRecordingOption();
                currMode = Mode.ToolMenu;
                toolManager.OptionEnded();
                toolManager.StartToolMenu(isLeftController);
                break;
        }
    }

    // Depending on the option picked in the radial menus of pen and sticky notes,
    // ending the menu can lead to switching back to ToolMenu. 
    // Ending the menu in the Tool Menu can lead to a new mode, depending on the option picked.
    private void EndMenu(bool isLeftController)
    {
        if (leftHand != isLeftController) return;
        menuOpen = false;

        switch (currMode)
        {
            case Mode.ToolMenu:
                pickedChoice = toolManager.EndToolMenu();

                // possible switch from base menu to others
                switch (pickedChoice)
                {
                    case 0:
                        // pen 
                        currMode = Mode.Draw3D;
                        break;
                    case 1:
                        // notes
                        currMode = Mode.StickyNote;
                        break;
                    case 2:
                        // voice
                        currMode = Mode.Voice;
                        break;
                }
                break;

            case Mode.Draw3D:
                pickedChoice = drawingManager.EndRadialMenu();

                // picked choice -1 means no change has been done
                // picked choice -2 = end tool button was picked
                if (pickedChoice == -2)
                {
                    drawingManager.EndPen();
                    currMode = Mode.ToolMenu;
                }

                break;
            case Mode.StickyNote:
                pickedChoice = stickyNoteManager.EndRadialMenu();

                // possible switch to base menu 
                if (pickedChoice == -2)
                {
                    stickyNoteManager.EndPen();
                    currMode = Mode.ToolMenu;
                }
                break;

            case Mode.Voice:
                break;
        }
    }

    // Activate action start starts the drawing process during pen and sticky notes.
    // It does nothing during the Tool Menu and Voice Recordings
    private void ActivateActionStart(bool isLeftController)
    {
        if (leftHand != isLeftController) return;
        actionStarted = true;

        switch (currMode)
        {
            case Mode.ToolMenu:
                break;
            case Mode.Draw3D:
                // start drawing
                drawingManager.StartPenAction();
                break;
            case Mode.StickyNote:
                // start drawing on note
                stickyNoteManager.StartPenAction();
                break;
            case Mode.Voice:
                break;
        }
    }


    // Activate action end ends the drawing process during pen and sticky notes.
    // It does nothing during the Tool Menu.
    // During the Voice Recordings it starts or ends the recording
    private void ActivateActionEnd(bool isLeftController)
    {
        if (leftHand != isLeftController) return;
        actionStarted = false;

        switch (currMode)
        {
            case Mode.ToolMenu:
                break;
            case Mode.Draw3D:
                // stop drawing
                drawingManager.EndPenAction();
                break;
            case Mode.StickyNote:
                // stop drawing on notes
                stickyNoteManager.EndPenAction();
                break;
            case Mode.Voice:
                voiceRecordingManager.ActivateVoiceRecording();
                break;
        }
    }

    // Sticky Note mode is turned on when drawing on the note is enabled
    // This happends when the note is created (reachable when the tool menu ends) 
    // or when the note is being edited - an action starting away from the Tool Input Actions
    public void EditNote(bool newLeftHand)
    {
        if (menuOpen) EndMenu(leftHand);
        if (actionStarted) ActivateActionEnd(leftHand);

        currMode = Mode.StickyNote;
        leftHand = newLeftHand;
    }

    // Called when an option ends in a different way than through selecting an option when a menu is ending.
    public void ForceResetOptions()
    {
        if (menuOpen) EndMenu(leftHand);
        if (actionStarted) ActivateActionEnd(leftHand);

        currMode = Mode.ToolMenu;
    }

}

