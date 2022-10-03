using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.IO;



// Class responsible for creating as sticky note.
// When the sticky note method is picked, this class spawns a private version of the note and a pen
// The pen communicated with the drawing plane, drawing on it 
// When the drawing process stops through the radial menu, the sticky note gets deactivated
// If a user deciedes to publish a note and make it private, they can do so by pressing a button and sending
// a message to the Sticky Note Manager
// The name StickyNotesManager is already used to the second version of sticky notes in edive, 
// leading to the need for StickyNotesManager2

// When creating and editing a public note the drawing happens directly on the drawing plane
// The drawing happening on a public object needs to be synchronized, changing the communication
// With the networked object as well as the plane
public class StickyNotesManager2 : NetworkBehaviour
{
    [SerializeField]
    private GameObject privateStickyNotePrefab;

    [SerializeField]
    private GameObject publicStickyNotePrefab;

    [SerializeField]
    private ToolManager toolManager;


    [SerializeField]
    private MenuManager menuManager;

    [SerializeField]
    private GameObject stickyNotePenObj;
    private StickyNotePen stickyNotePen;

    // The camera is necessary for setting up the UI of the spawned notes
    private GameObject cameraObj;

    private Transform currControllerTransform;

    private StickyNotePrivate currStickyNotePrivate = null;
    private StickyNotePublic currStickyNotePublic = null;

    private GameObject currStickyNoteObj = null;

    private GameObject currDrawingPlaneObj = null;
    private StickyNotePlane currDrawingPlane = null;

    private bool editing = false;

    // Represents the drawing that is happening on a private note when it spawns
    private bool privateDrawing = false;

    private bool privateNote = false;

    private bool penSetUp = false;

    private static readonly float[] WIDTHS = new float[] { 0.001f, 0.005f, 0.01f };
    private static readonly float DEFAULT_WIDTH = WIDTHS[1];

    private static readonly Color[] COLORS = new Color[] { Color.black, Color.red, Color.green, Color.blue };
    private static readonly Color DEFAULT_COLOR = COLORS[0];

    void Start()
    {
        stickyNotePen = stickyNotePenObj.GetComponent<StickyNotePen>();
    }


    // The Sticky Note tool is picked and set up
    public void StickyNotePicked(Transform controllerTransform, GameObject newCameraObj)
    {
        currControllerTransform = controllerTransform;
        privateNote = true;
        cameraObj = newCameraObj;
        SpawnPrivateNote();
        privateDrawing = true;

        // best way to start the pen with default settings
        ActivateMenuChange(-1);
    }

    // Private Sticky note is spawned.  It stays this way untill a user decides to publish it
    public void SpawnPrivateNote()
    {
        // get position for a new sticky note relevant to the camera
        var spawnPosition = cameraObj.transform.position;
        spawnPosition = spawnPosition + (cameraObj.transform.forward * 0.5f);

        currStickyNoteObj = Instantiate(privateStickyNotePrefab, spawnPosition, cameraObj.transform.rotation);
        currStickyNotePrivate = currStickyNoteObj.GetComponent<StickyNotePrivate>();

        // Enabling two way communication from note to manager
        currStickyNotePrivate.SetStickyNoteManager(gameObject);

        currDrawingPlaneObj = currStickyNotePrivate.GetDrawingPlane();
    }


    // Opening the radial menu for sticky notes, hiding the pen in the process
    public void StartRadialMenu(Transform controllerTransform)
    {
        currControllerTransform = controllerTransform;

        // if trigger is pressed while we are pressing the menu button
        EndPenAction();
        stickyNotePenObj.SetActive(false);
        menuManager.StartMenu(currControllerTransform);
    }

    // Closing the radiial menu for sticky notes and showing the pen if the drawing is continuing
    public int EndRadialMenu()
    {
        int pickedChoice = menuManager.GetCurrentChoice();
        menuManager.ResetMenu();
        ActivateMenuChange(pickedChoice);
        return pickedChoice;
    }

    // Activating the change that happened on the radial menu
    public void ActivateMenuChange(int val)
    {
        stickyNotePenObj.SetActive(true);

        // Setting the pen and drawing plane with the default values
        if (!penSetUp || !currDrawingPlane)
        {
            stickyNotePen.SetUp(DEFAULT_COLOR, DEFAULT_WIDTH);
            stickyNotePen.SetDrawingPlane(currDrawingPlaneObj, privateNote);
            penSetUp = true;
            currDrawingPlane = currDrawingPlaneObj.GetComponent<StickyNotePlane>();
            // sets up the drawing width
            currDrawingPlane.ResetDrawingOptions(DEFAULT_COLOR, GetTextureWidth(DEFAULT_WIDTH));
        }

        stickyNotePen.SetController(currControllerTransform);


        // inform the pen to send points to sticky note public instead of the drawing plane
        if (editing && !privateNote) stickyNotePen.PublicEditing();

        int textureWidth = 0;

        switch (val)
        {
            // options 0,1,2,3 represent colors
            case int n when (n >= 0 && n <= 3):
                stickyNotePen.SetColor(COLORS[n]);
                currDrawingPlane.SetColor(COLORS[n]);
                break;
            // options 4,5,6 represent widths
            case int n when (n > 3 && n <= 6):
                stickyNotePen.SetWidth(WIDTHS[n - 4]);
                textureWidth = GetTextureWidth(WIDTHS[n - 4]);
                currDrawingPlane.SetWidth(textureWidth);
                break;
            // option 7 represents an erasor
            case (7):
                stickyNotePen.Erase(currDrawingPlane.GetTextureColor());
                currDrawingPlane.Erase();
                break;
            default:
                break;
        }
    }

    // transform the width pof the pen to the width placed on the texture
    private int GetTextureWidth(float drawingWidth)
    {
        return ((int)(drawingWidth * 1000 + 2));
    }

    // Starting the drawing
    public void StartPenAction()
    {
        if (!stickyNotePenObj.activeSelf) return;
        stickyNotePen.ActivatePen();
    }

    // Ending the drawing
    public void EndPenAction()
    {
        if (!stickyNotePenObj.activeSelf) return;
        stickyNotePen.DeactivatePen();

        // new continuous drawing will begin
        currDrawingPlane.StartNewDrawing();



        // Message to sync the continuous line
        if (editing && !privateNote) currStickyNotePublic.SyncTexture();
    }

    // End the pen entirely
    // If a private note is used, the drawing ends and the note is closed
    // If a public note is used, it means that it's being edited - the changes are saved and the drawing ends
    public void EndPen()
    {
        penSetUp = false;
        if (!stickyNotePenObj.activeSelf) return;
        stickyNotePen.SetNullValues();
        stickyNotePenObj.SetActive(false);

        if (!currDrawingPlane) Debug.Log("No current drawing plane");

        currDrawingPlane.ResetDrawingOptions(DEFAULT_COLOR, GetTextureWidth(DEFAULT_WIDTH));

        if (privateNote)
            StopPrivateNoteDrawing();
        else
            StopPublicNoteEditing();

        ResetNoteManager();
    }

    private void StopPrivateNoteDrawing()
    {
        if (!currStickyNotePrivate) Debug.Log("No current sitcky note private");
        
        currStickyNotePrivate.EndEditing();
        privateDrawing = false;

        //deactivate note and make it smaller
        currStickyNotePrivate.Activate();
    }


    private void StopPublicNoteEditing()
    {
        if (!editing) Debug.Log("Error - Drawing ended on a public note without editing on");
        currStickyNotePublic.EndEditing();
        currStickyNotePublic.Cmd_Activate();

    }


    // The Publish button was pressed on a private note
    // The private note is destroyed and replaced by a public note
    public void PublishPrivateNote(GameObject publishingNoteObj)
    {
        // we need to save the basic color of the current texture to work with later
        var baseColor = publishingNoteObj.GetComponentInChildren<StickyNotePlane>().GetTextureColor();

        StickyNotePrivate publishingNote = publishingNoteObj.GetComponent<StickyNotePrivate>();
        var privateTexture = publishingNote.GetTexture();

        var privateNotePosition = publishingNoteObj.transform.position;
        var privateNoteRotation = publishingNoteObj.transform.rotation;
        Cmd_SpawnPublicNote(privateTexture, privateNotePosition, privateNoteRotation, baseColor);
        publishingNote.DestroyNote();
    }


    // Public objects need to be spawned on server
    [Command(requiresAuthority = false)]
    public void Cmd_SpawnPublicNote(byte[] byteTexture, Vector3 position, Quaternion rotation, Color baseColor, NetworkConnectionToClient sender = null)
    {
        GameObject go = Instantiate(publicStickyNotePrefab, position, rotation);
        NetworkServer.Spawn(go);

        // set the byteTexture on server side of public note
        go.GetComponent<StickyNotePublic>().SetChangedTexture(byteTexture, baseColor);
    }


    private void ResetNoteManager()
    {
        currStickyNoteObj = null;
        currDrawingPlane = null;
        currStickyNotePrivate = null;
        currStickyNotePublic = null;
        currDrawingPlaneObj = null;
        editing = false;
        toolManager.OptionEnded();
    }


    //Editing can be done on a private, unpublished note as well as onm a public note    
    public void StartEditing(GameObject editStickyNote, bool isPrivate)
    {
        // End the previusly started editing or creation of the note and replace it by a new one
        if (editing || privateDrawing)
        {
            if (privateNote)
                StopPrivateNoteDrawing();
            else
                StopPublicNoteEditing();
        }


        // if there is no controller setup previously we edit with the right hand
        if (!currControllerTransform)
            currControllerTransform = toolManager.GetController(false);

        toolManager.EditNote(currControllerTransform);

        editing = true;
        penSetUp = false;
        currDrawingPlane = null;
        privateNote = isPrivate;
        currStickyNoteObj = editStickyNote;

        if (privateNote)
        {
            currStickyNotePrivate = currStickyNoteObj.GetComponent<StickyNotePrivate>();
            currStickyNotePublic = null;
            currDrawingPlaneObj = currStickyNotePrivate.GetDrawingPlane();
        }
        else
        {
            currStickyNotePublic = currStickyNoteObj.GetComponent<StickyNotePublic>();
            currStickyNotePrivate = null;
            currDrawingPlaneObj = currStickyNotePublic.GetDrawingPlane();
        }
        ActivateMenuChange(-1);
    }


    public GameObject GetCamera()
    {
        if (!cameraObj)
            cameraObj = toolManager.GetCameraObject();
        return cameraObj;
    }

    public bool GetPrivacy()
    {
        return privateNote;
    }
}
