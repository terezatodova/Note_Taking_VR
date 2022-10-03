using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using UnityEngine.XR.Interaction.Toolkit;
using UVRN.Player;


// Class representing the private line object.
// One line object hosts one continuous line drawing
// The line object recieves points representing the drawing, and adds them into the line renderer
// It also calculates a bounding box, which encapsulates the entire drawing 
// Public line object is networked
// The line renderer does not automatically synchronize over the network, it needs to be done manually
public class LineObjectPublic : NetworkBehaviour
{

    [SerializeField]
    private XRGrabInteractable grabInteractableScript;


    [SerializeField]
    private LineRenderer lineRenderer;

    // Color and width are synchronized for newly connected users
    [SyncVar]
    private Color color;

    [SyncVar]
    private float width;

    private SyncList<Vector3> points = new SyncList<Vector3>();


    private Vector3 minPoint, maxPoint;

    // False by deafult, the creatore gets the points send directly to their
    // line renderer, not having to wait for the synchronization of the list to register
    private bool isCreator = false;
    private bool firstPoint = true;
    private bool drawingInProgress = true;

    private bool setUpLineRenderer = false;

    private bool highlighted = false;

    private float SHAKE_SPEED_TRESHOLD = 1.5f;
    private Vector3 shakeStart;

    private Rigidbody rigidBody;

    private bool grabbed = false;
    private int updatesWithSpeed = 0;


    // The object is deleted by shaking it. This is done by tracking the speed of the object
    // over multiple updates. It the object has a high speed for a longer period of time and
    // it has not travelled too far from the initial point, it is deleted.
    void Update()
    {
        if (drawingInProgress) return;

        if (grabbed && rigidBody.velocity.magnitude == 0) return;
        if (grabbed && rigidBody.velocity.magnitude > SHAKE_SPEED_TRESHOLD)
        {
            updatesWithSpeed += 1;
            if (updatesWithSpeed == 1) shakeStart = gameObject.transform.position;
            if (updatesWithSpeed > 10 && ComparePositions(shakeStart, gameObject.transform.position))
                Cmd_Destroy();
        }
        else
            updatesWithSpeed = 0;
    }

    [Command(requiresAuthority = false)]
    private void Cmd_SetPosition(Vector3 newPos, Quaternion newRot)
    {
        gameObject.transform.position = newPos;
        gameObject.transform.rotation = newRot;
        RPC_SetPosition(newPos, newRot);
    }

    [ClientRpc]
    private void RPC_SetPosition(Vector3 newPos, Quaternion newRot)
    {
        if (grabbed) return;
        gameObject.transform.position = newPos;
        gameObject.transform.rotation = newRot;
    }

    private bool ComparePositions(Vector3 fst, Vector3 snd)
    {
        if (Math.Abs(fst.x - snd.x) > 0.2f) return false;
        if (Math.Abs(fst.y - snd.y) > 0.2f) return false;
        if (Math.Abs(fst.z - snd.z) > 0.2f) return false;
        return true;
    }

    // Callback - gets triggered whenever the syncList is changed
    [ClientCallback]
    private void OnPointsUpdated(SyncList<Vector3>.Operation op, int index, Vector3 oldItem, Vector3 newItem)
    {
        if (isCreator) return;

        UpdateTrajectory(newItem);
    }

    // Starts the drawing on all connected clients, sending the color and width
    // If this information is not sent and only synced through sync vsr, sometimes the drawing 
    // starts before the set up width/color is synchronized
    [Command(requiresAuthority = false)]
    public void Cmd_StartDrawing(Color ncolor, float nwidth)
    {
        color = ncolor;
        width = nwidth;
        Rpc_StartDrawing(ncolor, nwidth);
    }

    // Client sets up his own line renderer
    [ClientRpc]
    private void Rpc_StartDrawing(Color ncolor, float nwidth)
    {
        if (isCreator) return;
        StartDrawing(ncolor, nwidth);
    }

    // Sending the color and width is safer than relying on syncVars, that
    // sometimes synchronized after the first sent point
    public void StartDrawing(Color ncolor, float nwidth)
    {
        if (!setUpLineRenderer)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            setUpLineRenderer = true;
        }

        lineRenderer.SetWidth(nwidth, nwidth);
        lineRenderer.material.color = ncolor;
        drawingInProgress = true;
    }


    // Called from the pen to add a neew point on all clients
    [Command(requiresAuthority = false)]
    public void Cmd_UpdateTrajectory(Vector3 point)
    {
        points.Add(point);
    }

    // Called when a new point is added to the syncList.
    // Each client adds it to their personal line renderer
    public void UpdateTrajectory(Vector3 point)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
        ResizeBoundingBox(point);
    }

    // With each incoming point the bounding box changes it's size, looking at min and max points
    private void ResizeBoundingBox(Vector3 point)
    {
        if (firstPoint)
        {
            minPoint = point;
            maxPoint = point;
            firstPoint = false;
        }
        else
        {
            if (point.x < minPoint.x) minPoint.x = point.x;
            if (point.y < minPoint.y) minPoint.y = point.y;
            if (point.z < minPoint.z) minPoint.z = point.z;
            if (point.x > maxPoint.x) maxPoint.x = point.x;
            if (point.y > maxPoint.y) maxPoint.y = point.y;
            if (point.z > maxPoint.z) maxPoint.z = point.z;
        }
    }

    [Command(requiresAuthority = false)]
    public void Cmd_EndDrawing()
    {
        Rpc_EndDrawing();
    }

    [ClientRpc]
    public void Rpc_EndDrawing()
    {
        if (isCreator) return;
        EndDrawing();
    }

    public void EndDrawing()
    {
        // Drawing ended  - we set the actual dimensions of the box
        // The box, represented by a collider is physically resized only after the drawing ends
        // This ensures that the object cannot be moved while it's being created
        Vector3 center = minPoint + (maxPoint - minPoint) / 2;
        float sizeX, sizeY, sizeZ;
        sizeX = maxPoint.x - minPoint.x + 0.1f;
        sizeY = maxPoint.y - minPoint.y + 0.1f;
        sizeZ = maxPoint.z - minPoint.z + 0.1f;

        BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
        boxCollider.enabled = true;
        Vector3 colliderSize = new Vector3(sizeX, sizeY, sizeZ);
        boxCollider.size = colliderSize;

        // Move the objects to ensure a correct center for grab
        gameObject.transform.position += center;
        lineRenderer.transform.localPosition -= center;

        drawingInProgress = false;
    }


    public void Highlight()
    {
        if (drawingInProgress) return;
        // when highlighting the object it gets bigger and lighter
        lineRenderer.SetWidth(width * 3, width * 3);

        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        // we change the saturation of the colorfull objects and the value of the black color
        if (s > 0.5f)
            s = s / 2;
        else
            v = 0.3f;
        var newColor = Color.HSVToRGB(h, s, v);
        lineRenderer.material.color = newColor;

        highlighted = true;
    }

    public void StopHighlight()
    {
        if (drawingInProgress) return;
        lineRenderer.SetWidth(width, width);
        lineRenderer.material.color = color;
        highlighted = false;
    }

    public void SetGrabInteractable()
    {
        GameObject XRManager = GameObject.Find("Edive_XRManager");
        if (XRManager)
        {
            var interactionManager = XRManager.GetComponent<UVRN_XRManager>().InteractionManager;
            grabInteractableScript.interactionManager = interactionManager;
        }
    }

    public override void OnStartClient()
    {
        rigidBody = gameObject.GetComponent<Rigidbody>();

        SetGrabInteractable();

        points.Callback += OnPointsUpdated;

        // if the line renderer is not ready we set it up
        if (!setUpLineRenderer)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineRenderer.material.color = color;
            lineRenderer.SetWidth(width, width);
            setUpLineRenderer = true;
        }

        // if there are any points already in the syncList we add them to
        // the line renderer in the propper order, resizing the bounding boc
        // in the process
        if (points.Count > 0)
        {
            for (int i = 0; i < points.Count; i++)
            {
                ResizeBoundingBox(points[i]);
                UpdateTrajectory(points[i]);
            }
            EndDrawing();
        }
    }

    public void ObjectGrabbed()
    {
        grabbed = true;
    }

    // The object is discarded when throwing it away with a big speed
    public void ObjectDropped()
    {
        grabbed = false;
        updatesWithSpeed = 0;
        Cmd_SetPosition(gameObject.transform.position, gameObject.transform.rotation);
    }

    // Destroying the object needs to be done on the server
    // An object with net identity is destroyed over the server, ensuring
    // that it's deleted over the entire network
    [Command(requiresAuthority = false)]
    private void Cmd_Destroy()
    {
        NetworkServer.Destroy(gameObject);
    }

    public void SetCreator()
    {
        isCreator = true;
    }
}
