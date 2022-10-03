using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing the pen used for sticky notes
// When drawing, casts a ray in the correct direction.
// If the ray hits the sticky note plane a point is drawn in the spot of the hit.
public class StickyNotePen : MonoBehaviour
{
    [SerializeField]
    private Material penMaterial;


    [SerializeField]
    private GameObject penRaycastPoint;

    [SerializeField]
    private GameObject penTip;

    [SerializeField]
    private GameObject penTipSphere;


    private Color color;

    private float width;

    private bool drawing;

    private Vector3 prevPosition;

    private bool privateDraw = false;

    private Transform currControllerTransform = null;

    private GameObject currDrawingPlane = null;
    private float threshold = 0.0001f;

    private bool erase = false;

    private StickyNotePlane currDrawingPlaneScript;

    private StickyNotePublic currPublicNote;
    private Vector3 rayCastPoint;

    private LineRenderer drawingRay;


    private const float RAY_DISTANCE = 1.0f;

    public void SetUp(Color defaultColor, float defaultWidth)
    {
        penMaterial.color = defaultColor;
        color = defaultColor;
        width = defaultWidth;

        // Setting up the UI for the drawing ray
        drawingRay = GetComponent<LineRenderer>();
        drawingRay.material = new Material(Shader.Find("Hidden/Internal-Colored"));
        drawingRay.SetColors(color, color);
        drawingRay.SetWidth(0.008f, 0.008f);
        drawingRay.SetPosition(1, RAY_DISTANCE * Vector3.forward * 1.4f);


        drawing = false;
        penTipSphere.transform.localScale = new Vector3(width, width, width);
        rayCastPoint = penRaycastPoint.transform.position;
        erase = false;
        privateDraw = false;
    }

    void Update()
    {
        if (!currControllerTransform) return;

        gameObject.transform.position = currControllerTransform.transform.position;
        gameObject.transform.rotation = currControllerTransform.transform.rotation;

        if (drawing)
        {
            Draw();
        }
    }


    public void ActivatePen()
    {
        drawing = true;
        prevPosition = new Vector3(0, 0, 0);
    }

    void OnDisable()
    {
        DeactivatePen();
    }

    public void DeactivatePen()
    {
        drawing = false;
    }

    // Drawing by tracking the poisiton of the pen
    // The pen casts a ray. If the ray hits the drawing plane it adds points.
    private void Draw()
    {
        var currPosition = penTip.transform.position;
        var offset = Vector3.Distance(currPosition, prevPosition);

        if (offset > threshold)
        {
            rayCastPoint = penRaycastPoint.transform.position;
            RaycastHit touchPoint;

            Collider coll = currDrawingPlane.GetComponent<Collider>();
            Ray ray = new Ray(rayCastPoint, penTip.transform.forward);

            if (coll.Raycast(ray, out touchPoint, RAY_DISTANCE))
            {
                var collisionObject = touchPoint.collider.gameObject;
                if (collisionObject == currDrawingPlane)
                {
                    if (currPublicNote) currPublicNote.DrawPoint(touchPoint.textureCoord);
                    currDrawingPlaneScript.AddPoint(touchPoint.textureCoord);
                }

            }
            prevPosition = currPosition;
        }
    }


    public void SetColor(Color newColor)
    {
        // If a color is selected it ends the erasing 
        if (erase)
        {
            erase = false;
            penTipSphere.transform.localScale = new Vector3(width, width, width);
        }

        color = newColor;
        penMaterial.color = color;
        drawingRay.startColor = color;
        drawingRay.endColor = color;
    }

    public void SetWidth(float val)
    {
        width = val;

        if (erase)
            penTipSphere.transform.localScale = new Vector3(3 * width, 3 * width, 3 * width);
        else
            penTipSphere.transform.localScale = new Vector3(width, width, width);


        drawingRay.startWidth = width + 0.002f;
        drawingRay.endWidth = width + 0.002f;
    }

    // Erasing changes the color and width of the drawing
    public void Erase(Color erasorColor)
    {
        // we are starting erasing
        color = erasorColor;
        penMaterial.color = color;
        drawingRay.startColor = color;
        drawingRay.endColor = color;

        erase = true;

        penTipSphere.transform.localScale = new Vector3(3 * width, 3 * width, 3 * width);
    }

    public void SetNullValues()
    {
        currControllerTransform = null;
        currDrawingPlane = null;
        currPublicNote = null;
    }

    public void SetController(Transform controllerTransform)
    {
        currControllerTransform = controllerTransform;
    }

    public void SetDrawingPlane(GameObject plane, bool isPrivate)
    {
        currDrawingPlane = plane;
        currDrawingPlaneScript = currDrawingPlane.GetComponent<StickyNotePlane>();
    }


    // Editing of a public notes = sending the points to the plane as well as the actual
    // public sticky note
    public void PublicEditing()
    {
        currPublicNote = currDrawingPlane.GetComponentInParent<StickyNotePublic>();
    }
}
