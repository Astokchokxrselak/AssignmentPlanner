using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class InputAssignmentManager : InputValueManager
{
    [Header("Assignment")]
    private TMP_Dropdown _assignment;
    public static InputAssignmentManager Instance;

    public override void Init(params object[] @params)
    {
        Instance = this;
        TryGetComponent(out _assignment);
        InitializeInput();
        _assignment.onValueChanged.AddListener(OnValueChanged);
        submit.onClick.AddListener(() =>
        {
            SetScreenKey(_assignmentList[_assignment.value]);
        });
    }
    void OnValueChanged(int v) {
        if (v == _assignmentList.Count) {
            Assignments.RequestAssignment();
        }
    }
    public override void Fill(object value)
    {
        if (value == null)
        {
            _assignment.value = default;
        }
        else if (value is int)
        {
            _assignment.value = (int)value;
        }
        else
        {
            _assignment.value = _assignmentList.IndexOf(value as Assignment);
        }
    }

    public bool undoneOnly;
    private List<Assignment> _assignmentList;
    private void InitializeInput()
    {
        _assignmentList = new List<Assignment>();
        _assignment.options = new List<TMP_Dropdown.OptionData>();
        for (int asIndex = 0; asIndex < Assignments.FocusedAssignments.Count; asIndex++)
        {
            var assignment = Assignments.GetFocusedAssignment(asIndex);
            if (assignment.IsDone && undoneOnly)
            {
                continue;
            }
            _assignment.options.Add(new(assignment.Title));
            _assignmentList.Add(assignment);
        }
        _assignment.options.Add(new("New Assignment"));
        OnValueChanged(0);
    }
}
