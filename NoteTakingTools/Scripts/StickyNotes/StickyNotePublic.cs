using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.XR.Interaction.Toolkit;
using UVRN.Player;
using UnityEngine.InputSystem;
using System.IO;
using System;

// Class representing a Public Sticky Note
// Gets created by publishing a private note through the sticky note manager
// Responsible for opening, closing the note and reacting to individual buttons 
// Opening and closing the note needs to be done through the server, ensuring
// that all clients have a note of the same size

// Responsible for editing the note = drawing on the note after it was created
// This means synchronizing the necessary points on all of the connected clients, so they 
// all could see the drawing happening in real time
// The drawn points are placed in the drawPoints sync list. 
// This list stores one continuous drawing (i.e. from the moment the trigger was pressed
// on the pen to the moment it was released)
// when the pen stops drawing = the trigger is released, the texture is updated on server and
// the list is cleared
public class StickyNotePublic : NetworkBehaviour
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
    private GameObject editingText;



    [SerializeField]
    private Material drawingPlaneMat;

    private StickyNotesManager2 stickyNotesManager;

    // will ONLY be set on server, sent to clients when they connect
    private byte[] byteTexture;

    private Vector3 defaultSize = new Vector3(0.1f, 0.1f, 0.01f);

    private bool highlighted = false;
    private bool saved = false;

    // if the user is drawing on their plane, the points are sent straight to the drawing plane,
    // ignoring the callback for faster drawing process
    private bool editingPlayer = false;

    private bool editingInProgress = false;

    private float lastChange;

    private bool destroyed = false;

    private float SHAKE_SPEED_TRESHOLD = 1.5f;
    private Rigidbody rigidBody;
    private Vector3 shakeStart;
    private bool grabbed = false;
    private int updatesWithSpeed = 0;

    private int MAX_EDITING_TIME = 60;

    [SyncVar(hook = "OnChangeActivated")]
    private bool activatedNote = false;


    [SyncVar(hook = "OnChangeEnabledEditing")]
    private bool enabledEditing = true;


    [SerializeField]
    private InputActionReference leftSelectAction;

    [SerializeField]
    private InputActionReference rightSelectAction;


    void Update()
    {
        // When the user edits the note for a long time without making any changes to it, the note
        // is automatically closed and the editing stopped
        // This is done especially for the moment, where a player disconnects (for example due to network failure)
        // during the editing process and cannot send the message to stop editing 
        if (isServer && !enabledEditing)
        {
            if (Time.time - lastChange > MAX_EDITING_TIME)
            {
                ForceQuitEditing();
            }
        }


        // Delete the note by shaking
        // Shaking is tracked the same way as with line object private or public
        if (grabbed && rigidBody.velocity.magnitude == 0) return;
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

    // SYNC HOOKS AND CALLBACK
    void OnChangeEnabledEditing(bool _, bool newValue)
    {
        enabledEditing = newValue;

        if (enabledEditing)
        {
            if (activatedNote) editButton.SetActive(true);
            editingText.SetActive(false);
        }
        else
        {
            if (!editingInProgress)
            {
                editingText.SetActive(true);
                editButton.SetActive(false);
            }
        }
    }

    void OnChangeActivated(bool _, bool newActivated)
    {
        activatedNote = newActivated;
        editButton.SetActive(activatedNote);
        saveButton.SetActive(activatedNote);
        if (activatedNote)
        {
            if (editingInProgress) editButton.SetActive(false);
            transform.localScale = 8 * defaultSize;
        }
        else
            transform.localScale = defaultSize;
    }


    // INPUT ACTIONS - opening and closing of the note
    void OnEnable()
    {
        leftSelectAction.action.performed += SelectObjectLeft_performed;
        rightSelectAction.action.performed += SelectObjectRight_performed;
    }

    void OnDisable()
    {
        leftSelectAction.action.performed -= SelectObjectLeft_performed;
        rightSelectAction.action.performed -= SelectObjectRight_performed;
    }


    private void SelectObjectLeft_performed(InputAction.CallbackContext ctx)
    {
        // we check trigger enter left so we know that the hand entering the
        // trigger is the same as the one pressing the buttons
        if (highlighted && !editingInProgress && !destroyed && enabledEditing)
        {
            Cmd_Activate();
        }
    }

    private void SelectObjectRight_performed(InputAction.CallbackContext ctx)
    {
        if (highlighted && !editingInProgress && !destroyed && enabledEditing)
        {
            Cmd_Activate();
        }
    }


    void Start()
    {
        GameObject XRManager = GameObject.Find("Edive_XRManager");
        if (XRManager)
        {
            var interactionManager = XRManager.GetComponent<UVRN_XRManager>().InteractionManager;
            grabInteractableScript.interactionManager = interactionManager;
        }
    }


    public void Highlight()
    {
        highlighted = true;
    }

    public void StopHighlight()
    {
        highlighted = false;
    }



    [Command(requiresAuthority = false)]
    public void Cmd_Activate()
    {
        //if start size is 0 = it was not loaded yet
        Server_Activate();
    }

    [Server]
    private void Server_Activate()
    {
        if (defaultSize.x <= 0.001f)
            defaultSize = gameObject.transform.localScale;
        activatedNote = !activatedNote;

        if (activatedNote)
            transform.localScale = 8 * defaultSize;
        else
            transform.localScale = defaultSize;
    }


    // DRAWING PROCESS

    // change the color of the texture - because the private and public notes have different colors
    // the server goes through all pixels of the private texture and changes the "not drawn parts"
    // to the color of the public note

    [Server]
    public void SetChangedTexture(byte[] newTexture, Color baseColor)
    {
        // starting editing of the texture
        Color newBaseColor = drawingPlaneMat.color;
        //Color newBaseColor = planeBaseColor;

        var editingTexture = new Texture2D(1, 1);
        editingTexture.LoadImage(newTexture);

        var texturePixelArray = editingTexture.GetPixels();
        for (var i = 0; i < texturePixelArray.Length; ++i)
        {
            if (CompareColors(texturePixelArray[i], baseColor))
            {
                texturePixelArray[i] = newBaseColor;
            }
        }
        editingTexture.SetPixels(texturePixelArray);
        editingTexture.Apply();

        byteTexture = editingTexture.EncodeToPNG();
        drawingPlane.SetTexture(byteTexture);

        Rpc_SetTexture(byteTexture);
    }

    // since colors are made of floats the comparison can be less precise
    private bool CompareColors(Color newColor, Color oldColor)
    {
        if (oldColor.r - 0.002f > newColor.r || oldColor.r + 0.002f < newColor.r)
            return false;
        if (oldColor.g - 0.002f > newColor.g || oldColor.g + 0.002f < newColor.g)
            return false;
        if (oldColor.b - 0.002f > newColor.b || oldColor.b + 0.002f < newColor.b)
            return false;
        return true;
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
            // publish button was pressed, simply end the editing, saving the texture
            EndEditing();
        }
    }

    public void StartEditing()
    {
        editingPlayer = true;
        if (!enabledEditing) return;

        stickyNotesManager.StartEditing(gameObject, false);
        Cmd_StartEditing();
    }


    // Makes sure that no other player can edit the note
    [Command(requiresAuthority = false)]
    private void Cmd_StartEditing()
    {
        lastChange = Time.time;
        enabledEditing = false;
    }

    public void DrawPoint(Vector2 newPoint)
    {
        Cmd_DrawPoint(newPoint);
    }

    [Command(requiresAuthority = false)]
    private void Cmd_DrawPoint(Vector2 newPoint)
    {
        lastChange = Time.time;
    }

    public void SyncTexture()
    {
        byteTexture = drawingPlane.GetTexture();
        Cmd_SyncTexture(byteTexture);
    }

    [Command(requiresAuthority = false)]
    private void Cmd_SyncTexture(byte[] newTexture)
    {
        // server takes the texture from the drawing plane and saves it in bytes
        byteTexture = newTexture;
        Rpc_SetTexture(byteTexture);
    }

    // When the user has not modified the note in the last 60 seconds, the editing
    // closes (for example if a user got disconnected during editing)
    [Server]
    private void ForceQuitEditing()
    {
        RPC_ForceQuitEditing();
        enabledEditing = true;
        // send texture to all clients in case the synchronization failed
        Rpc_SetTexture(byteTexture);

        //finally deactivate the note
        Server_Activate();
    }

    [ClientRpc]
    private void RPC_ForceQuitEditing()
    {
        if (editingInProgress)
            EndEditing();
    }

    // Editing ends via pressing the button on the note
    public void EndEditing()
    {
        editingPlayer = false;
        editingInProgress = false;

        // inform other clients that editing is possible
        Cmd_EndEditing();

        editButton.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        editButton.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        EnableExport();

        stickyNotesManager.EndPen();
    }

    [Command(requiresAuthority = false)]
    private void Cmd_EndEditing()
    {
        enabledEditing = true;
        // send texture to all clients in case the synchronization failed
        Rpc_SetTexture(byteTexture);
    }

    [ClientRpc]
    private void Rpc_SetTexture(byte[] newTexture)
    {
        drawingPlane.SetTexture(newTexture);
    }


    private void EnableExport()
    {
        saved = false;
        saveButton.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
        saveButton.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
    }


    public void SaveNote()
    {
        if (saved) return;
        saved = true;
        drawingPlane.SaveTexture();
    }

    public GameObject GetDrawingPlane()
    {
        return drawingPlane.gameObject;
    }


    // SECTION - Destroying the note

    public void ObjectGrabbed()
    {
        grabbed = true;
    }

    public void ObjectDropped()
    {
        grabbed = false;
        updatesWithSpeed = 0;
    }


    private void DestroyNote()
    {
        // in case someone is editing while the object was getting destroyed
        if (editingInProgress) return;
        destroyed = true;
        // Destroying the note needs to happen on the server
        Cmd_DestroyNote();
    }


    [Command(requiresAuthority = false)]
    private void Cmd_DestroyNote()
    {
        NetworkServer.Destroy(gameObject);
    }



    // SECTION - handling the newly connected client


    // When the client starts he first loads the texture othat is on the server
    // Then he synchronizes the last drawing (the continuous line drawing) if  
    // there is any
    public override void OnStartClient()
    {
        rigidBody = gameObject.GetComponent<Rigidbody>();
        stickyNotesManager = GameObject.Find("StickyNotesManager").GetComponent<StickyNotesManager2>();
        var cameraObj = stickyNotesManager.GetCamera();
        SetCamera(cameraObj);

        editButton.SetActive(activatedNote);
        saveButton.SetActive(activatedNote);

        if (enabledEditing)
            editingText.SetActive(false);
        else
        {
            editingText.SetActive(true);
            editButton.SetActive(false);
        }


        Cmd_AskForTexture();


    }


    [Command(requiresAuthority = false)]
    public void Cmd_AskForTexture(NetworkConnectionToClient sender = null)
    {
        Target_LoadTexture(sender, byteTexture);
    }


    [TargetRpc]
    public void Target_LoadTexture(NetworkConnection target, byte[] newTexture)
    {
        drawingPlane.SetTexture(newTexture);
    }



    // SetUp of the buttons
    // The buttons need to have the proper camera to register UI events
    public void SetCamera(GameObject cameraObj)
    {
        editButton.SetActive(true);
        saveButton.SetActive(true);
        editingText.SetActive(true);

        editButton.GetComponentInChildren<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();
        saveButton.GetComponentInChildren<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();
        editingText.GetComponent<Canvas>().worldCamera = cameraObj.GetComponent<Camera>();

        editButton.SetActive(false);
        saveButton.SetActive(false);
        editingText.SetActive(false);
    }

}
