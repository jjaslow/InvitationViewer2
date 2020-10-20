using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// This component listens for images detected by the <c>XRImageTrackingSubsystem</c>
/// and overlays some information as well as the source Texture2D on top of the
/// detected image.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class TrackedImageInfoManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The camera to set on the world space UI canvas for each instantiated image info.")]
    Camera m_WorldSpaceCanvasCamera;

    /// <summary>
    /// The prefab has a world space UI canvas,
    /// which requires a camera to function properly.
    /// </summary>
    public Camera worldSpaceCanvasCamera
    {
        get { return m_WorldSpaceCanvasCamera; }
        set { m_WorldSpaceCanvasCamera = value; }
    }

    Camera cam;

    [SerializeField]
    TestCameraImage testCameraImage;


    const float INCHES_PER_METER = 39.37f;
    bool setPaperColor;


    void Start()
    {
        setPaperColor = false;
        cam = Camera.main;
    }

    ARTrackedImageManager m_TrackedImageManager;

    void Awake()
    {
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void UpdateInfo(ARTrackedImage trackedImage)
    {
        var plane1ParentGo = trackedImage.transform.GetChild(0).gameObject;
        var plane1GO = plane1ParentGo.transform.GetChild(0).gameObject;
        var plane2GO = plane1GO.transform.GetChild(0).gameObject;

        if (!testCameraImage.isTextureReady())
        {
            setPaperColor = false;

            plane1GO.SetActive(false);
            plane2GO.SetActive(false);
            return;
        }

        // Disable the visual plane if it is not being tracked
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 0.1f, trackedImage.size.y);

            plane1GO.SetActive(true);

            if(!setPaperColor)
            {
                //locate empty center of QR code
                Vector3 trackedScreenPos = cam.WorldToScreenPoint(trackedImage.transform.position);

                //calculate QR center location on scaled camera texture. Then grab the colors at and around that location
                float textureScaleFactor = (float)Screen.height / (float)testCameraImage.GetTextureWidth();
                int xPos = (int)((trackedScreenPos.x) / textureScaleFactor);
                int yPos = (int)((trackedScreenPos.y) / textureScaleFactor);

                Color[] colors = testCameraImage.GetTextureColor(yPos, (testCameraImage.GetTextureHeight() - xPos));
                int colorArrayDimensions = (int)Mathf.Sqrt(colors.Length);


                Texture2D colorPatch = trackedImage.referenceImage.texture;

                for (int a = 0; a < colorPatch.width; a++)
                {
                    for (int b = 0; b < colorPatch.height; b++)
                    {
                        int c = UnityEngine.Random.Range(0, colors.Length);
                        Color finalColor = colors[c];
                        finalColor.a = colorPatch.GetPixel(b, a).a;
                        colorPatch.SetPixel(b, a, finalColor);
                    }
                }

                colorPatch.Apply();

                // Set the cover texture and material intensity
                var material1 = plane1GO.GetComponent<MeshRenderer>().material;
                material1.mainTexture = colorPatch;

                //Color lightTesterColor = colors[colors.Length / 2];
                //float lightness = (lightTesterColor.r + lightTesterColor.g + lightTesterColor.b)/3f;
                //material1.color = new Color(lightness, lightness, lightness, 1);

                //stop changing the color as long as tracking persists
                setPaperColor = true;

                Debug.Log("APPLY");
            }


            ///////////////////////////////////////
            //Start to generate the design Texture2D
            plane2GO.SetActive(true);

            //size the design to the invitation size
            float setDesignX = (ChooserManager.Instance.GetDesignDisplayedSize().x / INCHES_PER_METER) / trackedImage.size.x;
            float setDesignY = (ChooserManager.Instance.GetDesignDisplayedSize().y / INCHES_PER_METER) / trackedImage.size.y;
            plane2GO.transform.localScale = new Vector3(setDesignX, .01f, setDesignY);

            // Set the design texture
            var material2 = plane2GO.GetComponent<MeshRenderer>().material;
            material2.mainTexture =  ChooserManager.Instance.designs[ChooserManager.Instance.GetDesignDisplayed()].designImage;
        }
        else
        {
            setPaperColor = false;

            plane1GO.SetActive(false);
            plane2GO.SetActive(false);
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            // Give the initial image a reasonable default scale
            trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);

            UpdateInfo(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
            UpdateInfo(trackedImage);
    }


    void GenerateTestSphere(ARTrackedImage trackedImage, Vector3 trackedScreenPos, Color color)
    {
        GameObject testPos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //Destroy(testPos, .125f);
        testPos.transform.localScale = new Vector3(.005f, .005f, .005f);
        //testPos.transform.position = trackedImage.transform.position;
        testPos.transform.position = cam.ScreenToWorldPoint(trackedScreenPos);
        testPos.GetComponent<Renderer>().material.color = color;
    }
}
