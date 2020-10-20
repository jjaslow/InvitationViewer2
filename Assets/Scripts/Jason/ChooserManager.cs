using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChooserManager : MonoBehaviour
{
    public static ChooserManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //PANELS MAIN
    GameObject enterPanel;
    GameObject selectPanel;

    //ENTER CODE ITEMS
    GameObject inputField;
    Text noticeText;
    GameObject submitButton;

    //SELECT DESIGN ITEMS
    [Header("SELECT DESIGN ITEMS")]
    [SerializeField]
    GameObject buttonPrefab;
    GameObject designFrame;
    GameObject nextButton;
    Text selectDesignHeaderText;

    [Space(10)]
    public List<DownloadedDesign> designs = new List<DownloadedDesign>();
    List<GameObject> designButtons = new List<GameObject>();

    int numberOfDesignsDownloaded;
    int designDisplayed=0;
    bool canUpdate = true;

    public void Reload()
    {
        Debug.Log("chooser Reload");
        enterPanel = GameObject.Find("EnterCode");
        selectPanel = GameObject.Find("SelectDesign");
        submitButton = GameObject.Find("SubmitButton");

        designFrame = GameObject.Find("Design Frame");
        nextButton = GameObject.Find("NextButton");
        nextButton.GetComponent<Button>().onClick.AddListener(() => PressNextDesignButton());


        enterPanel.SetActive(true);
        selectPanel.SetActive(true);

        inputField = GameObject.Find("InputField");
        noticeText = GameObject.Find("NoticeText").GetComponent<Text>();

        selectDesignHeaderText = selectPanel.transform.GetChild(0).GetComponent<Text>();

        //check for existing designs (ie return to select page)
        if (designs.Count > 0)
        {
            Debug.Log("existing designs found");

            enterPanel.SetActive(false);
            selectPanel.SetActive(true);

            selectDesignHeaderText.text = numberOfDesignsDownloaded + " of " + S3Manager.Instance.designsToDownload + "\n" + "Total Designs";

            for (int index = 0; index < designs.Count; index++)
            {
                DownloadedDesign dd = designs[index];
                AddToScreen(dd, index);
            }
            canUpdate = true;
        }
    }

    void AddToScreen(DownloadedDesign dd, int index)
    {
        Sprite sp = Sprite.Create(dd.designImage, new Rect(0, 0, dd.designImage.width, dd.designImage.height), new Vector2(0.5f, 0.5f));

        GameObject b = GameObject.Instantiate(buttonPrefab, designFrame.transform);
        b.name = "Button Design: " + index;
        designButtons.Add(b);

        b.transform.GetChild(0).GetComponent<Text>().text = "Tap this design to see your\n" + dd.designType;
        b.transform.GetChild(1).GetComponent<Image>().sprite = sp;
        b.GetComponent<Button>().onClick.AddListener(() => pressDesignButton(index));

        b.SetActive(false);
    }

    void Start()
    {
        Reload();

        Debug.Log("chooser Start");
        numberOfDesignsDownloaded = 0;

        noticeText.text = "";

        enterPanel.SetActive(true);
        selectPanel.SetActive(false);
    }


    public void ReceiveCode()
    {
        string code = inputField.GetComponent<InputField>().text;
        Debug.Log("entered code: " + code);
        noticeText.text = "Checking for code now.";
        submitButton.SetActive(false);
        S3Manager.Instance.ValidateBucketExists(code);
    }

    public void BucketFound()
    {
        noticeText.text = "Code found. Downloading Files now.";
    }

    public void SwapPanes()
    {
        enterPanel.SetActive(!enterPanel.activeSelf);
        selectPanel.SetActive(!enterPanel.activeSelf);
    }

    public IEnumerator BucketNotFound()
    {
        submitButton.SetActive(true);
        noticeText.text = "not found, please confirm your code";
        yield return new WaitForSeconds(3f);
        noticeText.text = "";
    }


    public IEnumerator DesignReceived(string name, string location)
    {
        float width = float.Parse(name.Substring(0, 2))/10;
        float height = float.Parse(name.Substring(2, 2)) / 10;
        string type = name.Substring(4, name.Length - 8);

        DownloadedDesign dd = new DownloadedDesign
        {
            width = width,
            height = height,
            designType = type
        };

        Texture2D tex = null;

        string path = Path.Combine(location, name);
        //TODO:: REMOVE THIS...   path = location + "/" + name;

        if (File.Exists(path))
        {
            Debug.Log("Choose found: " + path);
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + path))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.Log("uwr error: " + uwr.error);
                }
                else
                {
                    // Get downloaded asset bundle
                    yield return new WaitForEndOfFrame(); ;
                    tex = DownloadHandlerTexture.GetContent(uwr);

                    dd.designImage = tex;

                    designs.Add(dd);
                    AddToScreen(dd, numberOfDesignsDownloaded);

                    selectDesignHeaderText.text = ++numberOfDesignsDownloaded + " of " + S3Manager.Instance.designsToDownload + "\n" + "Total Designs";
                }
            }
        }
    }





    public void pressDesignButton(int x)
    {
        Debug.Log("you pressed the button for: " + designs[x].designType);
        canUpdate = false;


        for(int i = 0; i<designButtons.Count; i++)
        {
            designButtons[i].GetComponent<Button>().onClick.RemoveListener(() => pressDesignButton(i));
            designFrame.transform.GetChild(i).GetComponent<Button>().onClick.RemoveListener(() => pressDesignButton(i));
        }

        designButtons.Clear();

        SceneManager.LoadScene(1);
    }



    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Image Chooser")
            return;

        if (!canUpdate)
            return;

        if(numberOfDesignsDownloaded>0)
        {
            designButtons[designDisplayed].SetActive(true);
        }

        if (numberOfDesignsDownloaded > 1)
            nextButton.SetActive(true);
        else
            nextButton.SetActive(false);
    }

    public void PressNextDesignButton()
    {
        designButtons[designDisplayed].SetActive(false);
        designDisplayed += 1;
        if (designDisplayed >= designButtons.Count)
            designDisplayed = 0;

        designButtons[designDisplayed].SetActive(true);
    }

    public int GetDesignDisplayed()
    {
        return designDisplayed;
    }

    public Vector2 GetDesignDisplayedSize()
    {
        var result = new Vector2(designs[designDisplayed].width, designs[designDisplayed].height);
        return result;
    }


}
