using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UVRN.Player;
using UnityEngine.InputSystem;
using System.Linq;
using UnityRecordingToolkit;
using System.IO;
using System;

// Class representing a Private Sticky Note
// Responsible for opening, closing the note and reacting to individual buttons 
// such as exporting the texture of the note, editing the note and publishing it 
// turning it into a public note
public class StickyNotePrivate : MonoBehaviour
{
    [SerializeField]
    private XRGrabInteractable grabInteractableScript;


    [SerializeField]

    private StickyNotePlane drawingPlane;


    [SerializeField]
    private GameObject editButton;

    [SerializeField]
    private GameObject saveButton;

    [SerializeField]
    private GameObject publishButton;
    private StickyNotesManager2 stickyNotesManager;


    private Vector3 defaultSize;

    private bool editingInProgress = false;

    private bool activated = false;

    private bool firstNoteDrawing = true;

    private bool highlighted = false;

    private bool destroyed = false;

    private bool saved = false;

    private Rigidbody rigidBody;

    private float SHAKE_SPEED_TRESHOLD = 1.5f;
    private Vector3 shakeStart;

    private bool grabbed = false;
    private int updatesWithSpeed = 0;

    [SerializeField]
    private InputActionReference leftSelectAction;

    [SerializeField]
    private InputActionReference rightSelectAction;

    void Update()
    {
        if (grabbed && rigidBody.velocity.magnitude == 0) return;

        // Delete the note by shaking
        // Shaking is tracked the same way as with line object private or public
        if (grabbed && rigidBody.velocity.magnitude > SHAKE_SPEED_TRESHOLD)
        {
            updatesWithSpeed += 1;
            if (updatesWithSpeed == 1) shakeStart = gameObject.transform.position;
            if (updatesWithSpeed > 8 && ComparePositions(shakeStart, gameObject.transform.position))
                DestroyNote();
        }
        else
            updatesWithSpeed = 0;
    }

    private bool ComparePositions(Vector3 fst, Vector3 snd)
    {
        if (Math.Abs(fst.x - snd.x) > 0.2f) return false;
        if (Math.Abs(fst.y - snd.y) > 0.2f) return false;
        if (Math.Abs(fst.z - snd.z) > 0.2f) return false;
        return true;
    }


    void OnEnable()
    {
        leftSelectAction.action.performed += SelectObject_performed;
        rightSelectAction.action.performed += SelectObject_performed;
    }

    void OnDisable()
    {
        leftSelectAction.action.performed -= SelectObject_performed;
        rightSelectAction.action.performed -= SelectObject_performed;
    }

    // Pressing the trigger button while hovering over the object activates/deactivates it
    private void SelectObject_performed(InputAction.CallbackContext ctx)
    {
        // Check trigger enter left so we know that the hand entering the
        // trigger is the same as the one pressing the buttons
        if (highlighted && !editingInProgress && !destroyed)
        {
            Activate();
        }
    }

    void Start()
    {
        rigidBody = gameObject.GetComponent<Rigidbody>();
        defaultSize = transform.localScale;

        GameObject XRManager = GameObject.Find("Edive_XRManager");
        if (XRManager)
        {
            var interactionManager = XRManager.GetComponent<UVRN_XRManager>().InteractionManager;
            grabInteractableScript.interactionManager = interactionManager;
        }

        SetCamera(stickyNotesManager.GetCamera());


        // When the note starts - is spawned it's immediately activated to enable drawing
        Activate();
        editingInProgress = true;
    }


    // Set-up of the buttons
    // The buttons need to have the proper camera to register UI events
    public void SetCamera(GameObject cameraObj)
    {
        editButton.SetActive(true);
        saveButton.SetActive(true);
        publishButton.SetActive(true);

        editButton.GetComponentInChildren<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();
        saveButton.GetComponentInChildren<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();
        publishButton.GetComponentInChildren<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();


        editButton.SetActive(false);
        saveButton.SetActive(false);
        publishButton.SetActive(false);
    }



    public void Activate()
    {
        activated = !activated;

        // Activating the note makes it bigger and shows all available buttons
        if (activated)
        {
            transform.localScale = 8 * defaultSize;


            if (!stickyNotesManager) Debug.Log("Problem - there is no notes manager");


            editButton.SetActive(true);
            saveButton.SetActive(true);
            publishButton.SetActive(true);
        }
        // Deactivating the note makes sure that no pen is being used
        // it also makes the note smaller and hides all buttons
        else
        {
            if (editingInProgress)
            {
                TurnOffPen();
                stickyNotesManager.EndPen();
            }

            transform.localScale = defaultSize;

            editButton.SetActive(false);
            saveButton.SetActive(false);
            publishButton.SetActive(false);
        }
    }

    public void EditNote()
    {
        editingInProgress = !editingInProgress;
        if (editingInProgress)
        {
            // start editing note
            StartEditing();
        }
        else
        {
            // discard changes, swap texture to the previous one
            EndEditing();
        }
    }

    public void StartEditing()
    {
        editingInProgress = true;
        stickyNotesManager.StartEditing(gameObject, true);
    }

    public void EndEditing()
    {
        //edit text on the button
        editButton.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

        //save text on the button
        editButton.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        EnableExport();

        stickyNotesManager.EndPen();
    }

    // If the note was already exported it cannot be exported again untill it's changed
    private void EnableExport()
    {
        saved = false;

        // Changing the text of the save button
        saveButton.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
        saveButton.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
    }

    public void SaveNote()
    {
        if (saved) return;
        saved = true;
        drawingPlane.SaveTexture();
    }

    // On the press of the publish button, the private note gets replaced with a public note, visible to all
    public void PublishNote()
    {
        stickyNotesManager.PublishPrivateNote(gameObject);
    }


    public void Highlight()
    {
        highlighted = true;
    }
    public void StopHighlight()
    {
        highlighted = false;
    }


    public void ObjectGrabbed()
    {
        grabbed = true;
    }

    public void ObjectDropped()
    {
        grabbed = false;
        updatesWithSpeed = 0;
    }

    public void DestroyNote()
    {
        if (editingInProgress)
        {
            stickyNotesManager.EndPen();
        }

        // sometimes the destroy has a small delay
        // setting the bool informs the note that no input actions should be registered anymore
        destroyed = true;
        Destroy(gameObject);
    }


    public byte[] GetTexture()
    {
        return drawingPlane.GetTexture();
    }
    public void SetStickyNoteManager(GameObject snManager)
    {
        stickyNotesManager = snManager.GetComponent<StickyNotesManager2>();
    }

    public GameObject GetDrawingPlane()
    {
        return drawingPlane.gameObject;
    }

    public void TurnOffPen()
    {
        editingInProgress = false;
    }


    public bool IsActivated()
    {
        return activated;
    }

}
