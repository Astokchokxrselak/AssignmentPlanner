using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using TMPro;

public interface IPlannerField {
    public abstract KeyValuePair<string, object> GetFieldData();
}
public class PlannerDropdown : MonoBehaviour, IPlannerField
{
    public KeyValuePair<string, object> GetFieldData() => new(dropdownName, _dropdown.value);
    TMP_Dropdown _dropdown;
    public enum DropdownType {
        Group,
        Assignment,
    }
    public DropdownType dropdownType;
    public string dropdownName;
    void OnEnable() {
        TryGetComponent(out _dropdown);
        
        _dropdown.ClearOptions();
        switch (dropdownType) {
            case DropdownType.Assignment:
                IEnumerable<string> assignmentNames = from ass in Assignments.FocusedAssignments select ass.Title;
                _dropdown.AddOptions(assignmentNames.ToList());
                break;
            case DropdownType.Group:
                IEnumerable<string> groupNames = from grp in Groups.Instance.groups select grp.name;
                _dropdown.AddOptions(groupNames.ToList());
                break;
        }
        _dropdown.interactable = _dropdown.options.Count != 0;
    }
}
