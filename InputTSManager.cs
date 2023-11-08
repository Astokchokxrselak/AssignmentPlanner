using System;
using Common.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common.Extensions;

using InputField = TMPro.TMP_InputField;

public class InputTSManager : InputValueManager
{
    [Header("TimeSpan")]
    public InputField Days;
    public InputField Hours;
    public InputField Minutes;
    public InputField Seconds;
    public override void Fill(object value)
    {
        TimeSpan ts;
        if (value == null) ts = default;
                      else ts = (TimeSpan)value;
        Days.SetTextIfNotFocused(ts.Days.ToString());
        Hours.SetTextIfNotFocused(ts.Hours.ToString());
        Minutes.SetTextIfNotFocused(ts.Minutes.ToString());
        Seconds.SetTextIfNotFocused(ts.Seconds.ToString());
    }
    private int ParseOr0(string s) => int.TryParse(s, out int i) ? i : 0;
    public override void Init(params object[] @params)
    {
        submit.onClick.AddListener(() =>
        {
            int days = ParseOr0(Days.text), hours = ParseOr0(Hours.text), 
                minutes = ParseOr0(Minutes.text), seconds = ParseOr0(Seconds.text);
            SetScreenKey(new TimeSpan(days, hours, minutes, seconds));
        });
    }
    private void OnEnable()
    {
        /*TimeSpan placeholder = !Inputs.AnyInput<TimeSpan>() ? TimeSpan.Zero : Inputs.GetLastInput<TimeSpan>();
        Days.text = placeholder.Days.ToString(); // last two chars
        Hours.text = placeholder.Hours.ToString();
        Minutes.text = placeholder.Minutes.ToString();
        Seconds.text = placeholder.Seconds.ToString();*/
    }
}
