using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UVRN.Player;
using UnityEngine.InputSystem;

// Class representing the private line object.
// One line object hosts one continuous line drawing
// The line object recieves points representing the drawing, and adds them into the line renderer
// It also calculates a bounding box, which encapsulates the entire drawing 
// Private line object is non networked, visible only to the creator
public class LineObjectPrivate : MonoBehaviour
{

    [SerializeField]
    private XRGrabInteractable grabInteractableScript;


    [SerializeField]
    private LineRenderer lineRenderer;


    // points representing the bounding box
    private Vector3 minPoint, maxPoint;

    private bool firstPoint = true;

    private Color color;
    private float width;

    private bool drawingInProgress = true;
    private bool highlighted = false;

    private Rigidbody rigidBody;


    private float SHAKE_SPEED_TRESHOLD = 1.5f;
    private Vector3 shakeStart;
    private bool grabbed = false;
    private int updatesWithSpeed = 0;

    // Set up of the drawing object called from the pen initiating the drawing process
    public void StartDrawing(Color ncolor, float nwidth)
    {
        rigidBody = gameObject.GetComponent<Rigidbody>();

        color = ncolor;
        width = nwidth;

        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineRenderer.SetWidth(width, width);
        lineRenderer.material.color = color;

        GameObject XRManager = GameObject.Find("Edive_XRManager");
        if (XRManager)
        {
            var interactionManager = XRManager.GetComponent<UVRN_XRManager>().InteractionManager;
            grabInteractableScript.interactionManager = interactionManager;
        }

        drawingInProgress = true;
    }

    // The object is deleted by shaking it. This is done by tracking the speed of the object
    // over multiple updates. It the object has a high speed for a longer period of time and
    // it has not travelled too far from the initial point, it is deleted.
    void Update()
    {
        if (drawingInProgress) return;

        // Sometimes the transform registers 0 velocity even when moving an object
        // This is considered a mistake and terefore ignored
        if (grabbed && rigidBody.velocity.magnitude == 0) return;
        if (grabbed && rigidBody.velocity.magnitude > SHAKE_SPEED_TRESHOLD)
        {
            updatesWithSpeed += 1;
            if (updatesWithSpeed == 1) shakeStart = gameObject.transform.position;
            if (updatesWithSpeed > 8 && ComparePositions(shakeStart, gameObject.transform.position))
                Destroy(gameObject);
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

    // Adding a point to the drawing.
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

        // the color on the private object is already "muted" in comparison to the public objects 
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        // we change the saturation of the colorfull objects and the value of the black color
        if (s > 0.3f)
            s = s / 2;
        else
            v = 0.9f;
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

    public void ObjectGrabbed()
    {
        grabbed = true;
    }

    public void ObjectDropped()
    {
        grabbed = false;
        updatesWithSpeed = 0;
    }
}
