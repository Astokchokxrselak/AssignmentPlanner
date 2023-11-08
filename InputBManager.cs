using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputBManager : InputValueManager
{
    [Header("Boolean")]
    public Toggle toggle;
    public override void Fill(object value)
    {
        if (value == null) 
        { 
            toggle.isOn = false;
        }
        toggle.isOn = (bool)value;
    }
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            SetScreenKey(toggle.isOn);
        });
    }
}
