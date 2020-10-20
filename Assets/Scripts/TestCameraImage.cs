using System;
using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// This component tests getting the latest camera image
/// and converting it to RGBA format. If successful,
/// it displays the image on the screen as a RawImage
/// and also displays information about the image.
///
/// This is useful for computer vision applications where
/// you need to access the raw pixels from camera image
/// on the CPU.
///
/// This is different from the ARCameraBackground component, which
/// efficiently displays the camera image on the screen. If you
/// just want to blit the camera texture to the screen, use
/// the ARCameraBackground, or use Graphics.Blit to create
/// a GPU-friendly RenderTexture.
///
/// In this example, we get the camera image data on the CPU,
/// convert it to an RGBA format, then display it on the screen
/// as a RawImage texture to demonstrate it is working.
/// This is done as an example; do not use this technique simply
/// to render the camera image on screen.
/// </summary>
public class TestCameraImage : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;

     /// <summary>
    /// Get or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get { return m_CameraManager; }
        set { m_CameraManager = value; }
    }

    //[SerializeField]
    //GameObject renderTexturePlane;
    //[SerializeField]
    //Texture2D sampleTexture;


    int screenWidth, screenHeight;
    int textureWidth, textureHeight;
    float screenRatio;


    void OnEnable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void Start()
    {
        setScreenDimensions();
        //renderTexturePlane.transform.localScale = new Vector3(.64f, .48f, 1);
        //renderTexturePlane.GetComponent<Renderer>().material.SetTexture("_MainTex", sampleTexture); 
    }

    void setScreenDimensions()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        screenRatio = (float)screenHeight / (float)screenWidth;
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        setScreenDimensions();

        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        XRCameraImage image;
        if (!cameraManager.TryGetLatestImage(out image))
        {
            Debug.Log("error getting camera image");
            image.Dispose();
            return;
        }


        // Once we have a valid XRCameraImage, we can access the individual image "planes"
        // (the separate channels in the image). XRCameraImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See XRCameraImage.FormatSupported for a complete list of supported formats.
        var format = TextureFormat.RGBA32;

        //calculate the ratio of the camera image that is displayed after being cropped to the screen dimensions. (flipped by 90 degrees)...
        textureWidth = image.width;
        textureHeight = (int)((float)textureWidth / screenRatio);
        //...and then get the pixel dimansions of that crop
        int heightDifference = Math.Abs(textureHeight - image.height);
        int yStart = heightDifference / 2;

        //create Texture2D for the first time, or if the rotation (image width (ie full height of screen)) changes
        if (m_Texture == null || m_Texture.width != image.width)
        {
            Debug.Log("creating new Texture2D in camera Image");
            m_Texture = new Texture2D(textureWidth, textureHeight, format, false);
        }

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorY);
        //We get the sub rectangle part of the camera image that is displayed after being cropped to the screen dimensions.
        conversionParams.inputRect = new RectInt(0, yStart, textureWidth, textureHeight);
        conversionParams.outputDimensions = new Vector2Int(textureWidth, textureHeight);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_Texture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCameraImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_Texture.Apply();

        // Set the RawImage's texture so we can visualize it.
        //m_RawImage.texture = m_Texture;
        //renderTexturePlane.transform.localScale = new Vector3(textureWidth/100, textureHeight/100, 1);
        //renderTexturePlane.GetComponent<Renderer>().material.SetTexture("_MainTex", m_Texture);
    }

    Texture2D m_Texture;


    public Color[] GetTextureColor(int x, int y)
    {
        int size = 12;
        Color[] colors = new Color[size*size];
        int index = 0;

        for(int a = -size / 2; a< size / 2; a++)
        {
            for (int b = -size / 2; b< size / 2; b++)
            {
                colors[index] = m_Texture.GetPixel(x+b, y+a);
                //Debug.Log("Color " + index + ": " + colors[index]);
                index++;
            }
        }
        //return m_Texture.GetPixel(x, y);
        return colors;
    }

    public int GetTextureWidth()
    {
        return m_Texture.width;
    }

    public int GetTextureHeight()
    {
        return m_Texture.height;
    }

    public bool isTextureReady()
    {
        return m_Texture != null;
    }

}
