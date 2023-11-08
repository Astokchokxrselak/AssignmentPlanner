using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using Common.Extensions;

using InputField = TMPro.TMP_InputField;
public class InputSManager : InputValueManager
{
    [Header("String")]
    public InputField String;
    public override void Fill(object value)
    {
        if (value == null)
        {
            String.SetTextIfNotFocused(default);
            return;
        }
        String.SetTextIfNotFocused(value.ToString());
    }
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            SetScreenKey(String.text);
        });
    }
}
