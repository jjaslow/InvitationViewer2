using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reload : MonoBehaviour
{
    private void OnEnable()
    {
        if(ChooserManager.Instance != null)
            ChooserManager.Instance.Reload();
    }
}
