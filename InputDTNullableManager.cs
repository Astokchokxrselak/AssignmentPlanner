using System;

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common.Helpers;

using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;
public class InputDTNullableManager : InputDTManager
{
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            var textFields = new[] { MM, DD, YY, HHTime, MMTime };
            if (textFields.Any(i => !string.IsNullOrWhiteSpace(i.text))) // if any field is non-empty
            {
                SetScreenKey(ParseInput());
            }
            else
            {
                SetScreenKey(null);
            }
        });
    }
}
