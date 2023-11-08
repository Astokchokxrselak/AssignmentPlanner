using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common.Helpers;
using Common.Extensions;

using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;

public class InputDManager : InputValueManager
{
    Action action;
    Button button;
    public override void Init(params object[] @params)
    { 
        button = GetComponentInChildren<Button>();
        button.onClick.AddListener(() => {
            action();
        });
    } // Does nothing because decision is just a button

    public override void Fill(object value)
    { 
        if (value != null) 
        {
            Decision decision = value as Decision;
            action = decision.action;
        }
        else {
            action = () => { int _; };
        }
    } // Does nothing because decision is just a button

    // Caption appears in the hierarchy, do not need to redefine
}
