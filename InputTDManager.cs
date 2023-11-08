using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using InputField = TMPro.TMP_InputField;

using Common.Extensions;
public class InputTDManager : InputValueManager
{
    [Header("Time")]
    public InputField Hour;
    public InputField Minute;
    public Toggle _AM;
    public override void Fill(object value)
    {
        TimeData td;
        if (value == null) td = default;
                      else td = (TimeData)value;

        DateTime dt = td.Time;
        _AM.isOn = dt.Hour < 12;
        Hour.SetTextIfNotFocused((dt.Hour % 12).ToString());
        Minute.SetTextIfNotFocused(dt.Minute.ToString());
    }
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            // Inputs.RecordInput<DateTime>(ParseInput());
            SetScreenKey(ParseInput());
        });
    }
    private int TryParse(string s, int @default)
    {
        try
        {
            return int.Parse(s);
        }
        catch (FormatException)
        {
            return @default;
        }
    }
    public TimeData ParseInput()
    {
        DateTime now = DateTime.Now;
        int hour = TryParse(Hour.text, 0), minute = TryParse(Minute.text, 0);
        if (_AM.isOn) {
            return new TimeData(hour, minute);
        } else {
            return new TimeData(hour == 12 ? hour : (hour % 12) + 12, minute);    
        }
    }
}
