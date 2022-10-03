using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UVRN.Player;
using UnityEngine.InputSystem;
using System.Linq;
using UnityRecordingToolkit;
using System.IO;

// Class responsible for creating the drawings on the sticky notes
// Drawing are created by calculating the points that need to be added to the texture
// Points are added to the texture, with a simple interpolation process making the
// drawing smoother.
// The class also takes care of saving the texture, for revision of the notes. 
public class StickyNotePlane : MonoBehaviour
{
    private Texture2D texture;
    private Vector2 textureSize = new Vector2(750, 750);

    private bool firstDrawPoint = true;
    private Vector2 prevPoint;

    Color[] colors = Enumerable.Repeat(Color.black, 20 * 20).ToArray();
    private Color color;
    private int width;

    private Renderer planeRenderer = null;

    // texture color represents the base color of the drawing plane
    // this is different between the private and public note
    private Color textureColor;

    // Erasing on the note = drawing with the same color as the base of the note
    // an erasor also has a larger width, using the old width to remember the pevious setting
    private bool erasor = false;

    private int oldWidth;


    void Start()
    {
        // if the renderer is set the texture was already loaded another way
        if (planeRenderer)
        {
            textureColor = planeRenderer.material.color;
            return;
        }
        planeRenderer = gameObject.GetComponent<Renderer>();
        texture = new Texture2D((int)textureSize.x, (int)textureSize.y);
        textureColor = planeRenderer.material.color;
        planeRenderer.material.mainTexture = texture;

        //filling the texture with the base color
        var texturePixelArray = texture.GetPixels();
        for (var i = 0; i < texturePixelArray.Length; ++i)
        {
            texturePixelArray[i] = textureColor;
        }
        texture.SetPixels(texturePixelArray);
        texture.Apply();
    }

    public void SetColor(Color newColor)
    {
        // choosing a drawing color ends the erasing process
        if (erasor)
        {
            width = oldWidth;
            erasor = false;
        }
        color = newColor;
        colors = Enumerable.Repeat(color, width * width).ToArray();
    }

    public void SetWidth(int newWidth)
    {
        if (erasor)
        {
            oldWidth = newWidth;
            width = newWidth * 10;
        }
        else
        {
            width = newWidth;
        }
        colors = Enumerable.Repeat(color, width * width).ToArray();
    }

    // Erasing changes the color and increases the width
    public void Erase()
    {
        erasor = true;
        color = textureColor;
        oldWidth = width;
        width = width * 10;
        colors = Enumerable.Repeat(color, width * width).ToArray();
    }

    // Placing a new point into the texture
    public void AddPoint(Vector2 drawPoint)
    {
        // coordinates of the texture for the new point
        var texX = (int)(drawPoint.x * textureSize.x - (width / 2));
        var texY = (int)(drawPoint.y * textureSize.y - (width / 2));

        // check whether point is in bounds of the texture
        if (texX < 0 || texX >= textureSize.x) return;
        if (texY < 0 || texY >= textureSize.y) return;

        // set pixels of plane texture
        texture.SetPixels(texX, texY, width, width, colors);

        // interpolatation between the previous point and the added points
        // this process creates new points in between, creating a smoother drawing
        if (!firstDrawPoint)
        {
            for (float f = 0.01f; f < 1.00f; f += 0.02f)
            {
                // linear interpolation, between the previously added point and the new point
                var interpolatedX = (int)Mathf.Lerp(prevPoint.x, texX, f);
                var interpolatedY = (int)Mathf.Lerp(prevPoint.y, texY, f);
                if (interpolatedX < 0 || interpolatedX >= textureSize.x) continue;
                if (interpolatedY < 0 || interpolatedY >= textureSize.y) continue;
                texture.SetPixels(interpolatedX, interpolatedY, width, width, colors);
            }
        }
        else
        {
            firstDrawPoint = false;
        }

        prevPoint.x = texX;
        prevPoint.y = texY;

        texture.Apply();
    }

    // Makes sure that the new point is not connected to the previous one by the interpolation process
    public void StartNewDrawing()
    {
        firstDrawPoint = true;
    }

    public void ResetDrawingOptions(Color defaultColor, int defaultWidth)
    {
        StartNewDrawing();
        color = defaultColor;
        width = defaultWidth;
        colors = Enumerable.Repeat(Color.black, width * width).ToArray();
    }

    public byte[] GetTexture()
    {
        return (texture.EncodeToPNG());
    }

    public void SetTexture(byte[] newTexture)
    {
        if (!planeRenderer)
            planeRenderer = gameObject.GetComponent<Renderer>();

        texture = new Texture2D(1, 1);
        texture.LoadImage(newTexture);
        planeRenderer.material.mainTexture = texture;
    }


    // Saving the texture into the headsets memory 
    // texture naming convention - StickyNote_date_time of creation, so every saved
    // sticky note has a unique name
    public void SaveTexture()
    {
        byte[] byteTexture = texture.EncodeToPNG();

        string name = "/StickyNote_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss") + ".png";

        // Default path for the entire edive project
        string folder = PathConfig.GeneralSaveFolder + "/EdiveImages";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string filename = folder + name;

        System.IO.File.WriteAllBytes(filename, byteTexture);
    }

    public Color GetTextureColor()
    {
        return textureColor;
    }

    public Color GetColor()
    {
        return color;
    }
    public int GetWidth()
    {
        return width;
    }
}
