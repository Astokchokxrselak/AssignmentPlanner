using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common.Helpers;
using Common.Extensions;

using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;
public class InputDTManager : InputValueManager
{
    [Header("DateTime")]
    public InputField MM;
    public InputField DD;
    public InputField YY;
    public InputField HHTime;
    public InputField MMTime;
    public Toggle _AM;

    public const string _Century = "20";
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            // Inputs.RecordInput<DateTime>(ParseInput());
            SetScreenKey(ParseInput());
        });
    }
    public override void Fill(object value)
    {
        if (value == null)
        {
            OnEnable();
            return;
        }

        print(value.GetType());
        var dt = (DateTime)value;
        MM.SetTextIfNotFocused(dt.Month.ToString());
        DD.SetTextIfNotFocused(dt.Day.ToString());
        YY.SetTextIfNotFocused(dt.Year.ToString()[2..]); // get last two digits
        _AM.isOn = dt.Hour < 12 && dt.Hour != 12;
        HHTime.SetTextIfNotFocused((dt.Hour % 12).ToString());
        MMTime.SetTextIfNotFocused(dt.Minute.ToString());
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
    public DateTime ParseInput()
    {
        int year = TryParse(_Century + YY.text, 0), month = TryParse(MM.text, 0),
               day = TryParse(DD.text, 0);
        int hour = TryParse(HHTime.text, 0), minute = TryParse(MMTime.text, 0);
        if (_AM.isOn) {
            if (hour == 12) {
                hour = 0;
            }
        } else { //  PM
            if (hour > 12) {
                hour -= 12;
            } else if (hour != 12) {
                hour += 12;
            }
        }
        print(hour + ", " + minute);
        return new System.DateTime(year, month, day, hour, minute, 0);
    }
    private void OnEnable()
    {
        DateTime placeholder = !Inputs.AnyInput<DateTime>() ? DateTime.Now : Inputs.GetLastInput<DateTime>();
        YY.text = placeholder.Year.ToString()[^2..]; // last two chars
        MM.text = placeholder.Month.ToString();
        DD.text = placeholder.Day.ToString();
        HHTime.text = placeholder.Hour.ToString();
        MMTime.text = placeholder.Minute.ToString();
    }
}
