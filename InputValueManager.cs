using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using Text = TMPro.TextMeshProUGUI;
public abstract class InputValueManager : MonoBehaviour
{
    public Button submit;
    public Text caption;
    private Screen screen;
    private string screenKey;
    public void AssignScreenKeyPair(Screen scr, string key)
    {
        screen = scr;
        screenKey = key;
    }
    public void SetScreenKey(object value)
    {
        System.Action action = null;
        Inputs.SaveVariables += action = () =>
        {
            screen[screenKey] = value;
            Inputs.SaveVariables -= action;
        };
    }
    public abstract void Fill(object value);
    public abstract void Init(params object[] @params);
}
