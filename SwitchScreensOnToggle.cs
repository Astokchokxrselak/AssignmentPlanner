using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using Common;
using Common.Extensions;
using Common.Helpers;

public class SwitchScreensOnToggle : MonoBehaviour
{
    public Transform parent;
    public int targetScreen;
    public int defaultScreen; // the screen to choose if the target screen has already been switched to. -1 if it does nothing

    private Toggle _toggle;
    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                if (parent.GetChild(targetScreen).gameObject.activeInHierarchy)
                {
                    parent.IsolateChild(targetScreen);
                }
                else
                {
                    parent.IsolateChild(defaultScreen);
                }
            }
        });
    }
}
