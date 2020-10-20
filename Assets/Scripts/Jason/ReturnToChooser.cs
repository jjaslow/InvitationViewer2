using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToChooser : MonoBehaviour
{
    public void PressReturnToChooserButton()
    {
        Debug.Log("returning back to chooser");

        SceneManager.LoadScene(0);
    }
}
