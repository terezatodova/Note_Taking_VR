using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsible for controlling the pen for 3D drawing.
// When the pen is drawing, it sends the positions of its tip to the line objects, which 
// reconstruct the drawing. 
// When the drawing stops, the Pen makes sure that another object for drawing is prepared
// spawning it ahead of time. 
// The communication between a private and public line drawings needs to be done separately. 
// Ideally LineObjectPrivate and LineObjectPublic would inherit from the same
// class. Unfortunatelly it is not possible due to the different base classes, Mono and Network Behaviour
public class DrawingPen : MonoBehaviour
{

    [SerializeField]
    private LineObjectManager lineObjectManager;


    [SerializeField]
    private Material penMaterial;


    [SerializeField]
    private GameObject publicText;


    [SerializeField]
    private GameObject privateText;

    // threshold controls how densly the points are sent into the line objects
    private float threshold = 0.0001f;

    private bool drawing = false;

    private bool privateDraw = false;
    private bool firstDraw = true;
    private Vector3 prevPosition, currPosition;

    private LineObjectPrivate lineObjectPrivate;
    private LineObjectPublic lineObjectPublic;

    // The drawing is happening from the tip of the pen
    [SerializeField]
    private GameObject penTip;

    // the penTipSphere changes size based on the drawing width
    [SerializeField]
    private GameObject penTipSphere;

    private Transform currControllerTransform;

    private Color color;
    private float width;


    // SetUp happends only once in a game right before the pens first use.
    // This cant be done on Start(), since we have no way to know is the lineObjectManager is in the scene when
    // the pen starts and wants to communicate with him
    public void SetUp(Color defaultColor, float defaultWidth)
    {
        color = defaultColor;
        width = defaultWidth;

        penMaterial.color = color;

        var penTipWidth = GetPenTipWidth(width);
        penTipSphere.transform.localScale = new Vector3(penTipWidth, penTipWidth, penTipWidth);

        publicText.SetActive(true);
        privateText.SetActive(false);

        lineObjectManager.SetPen(gameObject);

        CreateNewDrawing(false);
        CreateNewDrawing(true);
    }

    void Update()
    {
        if (currControllerTransform)
        {
            gameObject.transform.position = currControllerTransform.position;
            gameObject.transform.rotation = currControllerTransform.rotation;
        }

        if (drawing)
        {
            if (privateDraw && !lineObjectPrivate) return;
            if (!privateDraw && !lineObjectPublic) return;

            currPosition = penTip.transform.position;
            var distance = Vector3.Distance(currPosition, prevPosition);
            if (distance > threshold)
            {
                if (privateDraw)
                    lineObjectPrivate.UpdateTrajectory(currPosition);
                else
                {
                    lineObjectPublic.UpdateTrajectory(currPosition);
                    lineObjectPublic.Cmd_UpdateTrajectory(currPosition);
                }
                prevPosition = currPosition;
            }
        }
    }

    // When the pen gets activated, it immediately starts drawing
    public void Activate()
    {
        currPosition = penTip.transform.position;
        prevPosition = currPosition;
        drawing = true;

        if (privateDraw)
            StartPrivateDrawing();
        else
            StartPublicDrawing();
    }

    // Creating a new Line drawing is done through LineObjectManager
    // The Line Drawings are prepared prior to being used
    public void CreateNewDrawing(bool spawnPrivateDrawing)
    {
        if (!lineObjectManager) Debug.Log("Non existing trajectory");

        if (spawnPrivateDrawing)
        {
            lineObjectManager.SpawnPrivateDrawing();
        }
        else
        {
            lineObjectManager.Cmd_SpawnPublicDrawing();
        }
    }

    // called from the LineObjectManager
    // this sets the Private Drawing object when ready for use
    public void SetPrivateDrawing(GameObject lineObject)
    {
        lineObjectPrivate = lineObject.GetComponent<LineObjectPrivate>();
    }

    // called from the LineObjectManager    
    // sets the Public Drawing object when ready
    public void SetPublicDrawing(GameObject lineObject)
    {
        lineObjectPublic = lineObject.GetComponent<LineObjectPublic>();
    }

    public void StartPrivateDrawing()
    {
        // drawing with lighter color on private objects
        var newColor = TransformColorToPrivate(color);

        lineObjectPrivate.StartDrawing(newColor, width);
        lineObjectPrivate.UpdateTrajectory(currPosition);
    }

    // Color of the private objects is muted, to differentiate from the public
    private Color TransformColorToPrivate(Color originalColor)
    {
        float h, s, v;
        Color.RGBToHSV(originalColor, out h, out s, out v);
        if (s > 0.5f)
            s = 0.4f;
        else
            v = 0.6f;
        return Color.HSVToRGB(h, s, v);
    }

    public void StartPublicDrawing()
    {
        // this is done separately, since the synchronization with the server can 
        // take some time
        // we draw on the local user, creating the drawing, individually, to make
        // sure that they see the drawing as soon as possible 
        lineObjectPublic.SetCreator();
        lineObjectPublic.StartDrawing(color, width);
        lineObjectPublic.UpdateTrajectory(currPosition);

        // inform the server, and other users that the drawing started
        lineObjectPublic.Cmd_StartDrawing(color, width);
        lineObjectPublic.Cmd_UpdateTrajectory(currPosition);
    }

    // Deactivating the pen ends the currently running drawing
    public void Deactivate()
    {
        if (drawing)
        {
            if (privateDraw)
                EndPrivateDrawing();
            else
                EndPublicDrawing();
        }
        drawing = false;
    }

    private void EndPrivateDrawing()
    {
        lineObjectPrivate.EndDrawing();
        lineObjectPrivate = null;
        CreateNewDrawing(true);
    }

    private void EndPublicDrawing()
    {
        lineObjectPublic.Cmd_EndDrawing();
        lineObjectPublic.EndDrawing();
        lineObjectPublic = null;
        CreateNewDrawing(false);
    }

    // if the pen is hidden, the drawing needs to end first
    void OnDisable()
    {
        Deactivate();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;

        if (privateDraw)
            penMaterial.color = TransformColorToPrivate(color);
        else
            penMaterial.color = color;
    }

    public void SetWidth(float val)
    {
        width = val;
        var penTipWidth = GetPenTipWidth(width);
        penTipSphere.transform.localScale = new Vector3(penTipWidth, penTipWidth, penTipWidth);
    }

    // separate function, since this functionality is called on 2 different spots
    private float GetPenTipWidth(float drawingWidth)
    {
        return width * 0.7f;
    }

    // Changing the privacy of the drawing
    public void PrivateDrawing(bool val)
    {
        privateDraw = val;
        privateText.SetActive(privateDraw);
        publicText.SetActive(!privateDraw);
        if (privateDraw)
            penMaterial.color = TransformColorToPrivate(color);
        else
            penMaterial.color = color;
    }

    // Setting the controller, which "is holding" the pen 
    public void SetController(Transform newCurrController)
    {
        currControllerTransform = newCurrController;
    }

}
