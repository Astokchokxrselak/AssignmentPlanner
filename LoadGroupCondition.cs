using System;

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.Helpers;
using Common.Extensions;

using Text = TMPro.TextMeshProUGUI;
public class LoadGroupCondition : MonoBehaviour
{
    // THIS SCRIPT IS ATTACHED TO A CHAIN, WHICH WILL STORE ALL CONTENTS
    // There are two kinds of objects: Conditions and Connectives
    public static LoadGroupCondition Instance;
    public string groupName;
    private Group targetGroup;
    public static void SetGroupName(string gname) => Instance.groupName = gname;
    public static void SetTargetGroup(Group g) => Instance.targetGroup = g;

    [SerializeField] private List<RectTransform> _placeablesTypes;
    public static List<RectTransform> PlaceablesTypes => Instance._placeablesTypes;
    public Dictionary<string, GameObject> Placeables;
    private void InitializePlaceables() {
        Placeables = new();
        for (int i = 0; i < PlaceablesTypes.Count; i++) {
            var placeable = PlaceablesTypes[i];
            var pname = placeable.name;
            var condition = pname.LastIndexOf("Condition");
            if (condition == -1) {
                condition = pname.LastIndexOf("Connective");
            }
            var trueName = pname.Substring(0, condition);
            Placeables[trueName] = placeable.gameObject;
        }
    }
    private void PlacePlaceable(string name) {
        var placeable = Placeables[name];
        Instantiate(placeable, transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
    private enum PlaceableType 
    {
        Group,
        Condition,
        Connective
    }
    [Header("Base Placeables for Building")]
    [SerializeField] private GameObject _baseGroupPlaceable;
    [SerializeField] private GameObject _baseConditionPlaceable;
    [SerializeField] private GameObject _baseConnectivePlaceable;
    private GameObject BuildPlaceable(string name, Color color, PlaceableType placeableType) {
        // TODO: Make it possible to build placeables on the fly
        if (Placeables.ContainsKey(name)) {
            throw new ArgumentException(name + " already exists in list of Placeables");
        }
        #region Build Methods for each placeable type
        GameObject BuildGroup() {
            var group = Instantiate(_baseGroupPlaceable);
            group.GetComponentInChildren<Text>().text = name;
            group.GetComponent<Image>().color = color;
            return group;
        }
        GameObject BuildCondition() {
            var condition = Instantiate(_baseConditionPlaceable);
            condition.GetComponentInChildren<Text>().text = name;
            condition.GetComponent<Image>().color = color;
            return condition;
        }
        GameObject BuildConnective() {
            var connective = Instantiate(_baseConnectivePlaceable);
            connective.GetComponentInChildren<Text>().text = name.Substring(0, 3).ToUpper();
            connective.GetComponent<Image>().color = color;
            return connective;
        }
        #endregion
        var placeable = placeableType switch {
            PlaceableType.Group => BuildGroup(),
            PlaceableType.Condition => BuildCondition(),
            _ => BuildConnective()
        };
        Placeables[name] = placeable;
        PlacePlaceable(name);
        return placeable;
    }
    public void OnEnable() 
    {
        Instance = this;
        foreach (Transform t in transform) {
            Destroy(t.gameObject); // clear children
        }

        InitializePlaceables();

        var seed = groupName.Sum(x => x);
        BuildPlaceable(groupName + "Group", RandomHelper.RandomColor(), PlaceableType.Group);
        if (targetGroup != null)
        {
            for (int i = 0; i < targetGroup.conditions.Count; i++) 
            {
                var condition = targetGroup.conditions[i];
                switch (condition.GCOperator) {
                    case GCOperator.AND:
                        PlacePlaceable("AND");
                        break;
                    case GCOperator.OR:
                        PlacePlaceable("OR");
                        break;
                }
                if (condition.Invert) {
                    PlacePlaceable("NOT");
                } 
                PlacePlaceable(condition.ConditionName);
            }
        }
    }
}
