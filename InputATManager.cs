using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using System;

public class InputATManager : InputValueManager
{
    [Header("Assignment Type")]
    private TMP_Dropdown _type;
    public bool allOption;
    public override void Fill(object value)
    {
        if (value == null)
        {
            _type.value = default;
        }
        else
        {
            _type.value = (int)value; // int
        }
    }
    public override void Init(params object[] @params)
    {
        TryGetComponent(out _type);
        if (@params?.Length > 0) {
            allOption = (bool)@params[0];
        }

        _type.options = new List<TMP_Dropdown.OptionData>();
        for (AssignmentType at = AssignmentType.Normal; at < AssignmentType.Count; at++)
        {
            int lastSpace = 0;
            String name = at.ToString(), @out = "";
            for (int i = 0; i < name.Length - 1; i++) 
            {
                if (char.IsLower(name[i]) && char.IsUpper(name[i + 1])) 
                { 
                    var next = name.Substring(lastSpace, i + 1);
                    @out += next + " ";
                    lastSpace = i + 1;
                }
            } name = @out += name.Substring(lastSpace);
            _type.options.Add(new(name));
        }
        if (allOption) {
            _type.options.Add(new("All"));
        }

        submit.onClick.AddListener(() =>
        {
            SetScreenKey((AssignmentType)_type.value);
        });
    }
}
