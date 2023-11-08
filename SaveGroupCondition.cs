using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using Text = TMPro.TextMeshProUGUI;
public class SaveGroupCondition : MonoBehaviour
{
    private static SaveGroupCondition _Self;
    private void Start() {
        _Self = this;
    }
    private static string TrueName(Transform obj) {
        int index = obj.name.LastIndexOf("Condition");
        if (index == -1){ 
            index = obj.name.LastIndexOf("Connective");
        }
        if (index == -1) {
            index = obj.name.LastIndexOf("Group");
        }
        return obj.name.Substring(0, index);
    }
    public TMP_InputField groupName;
    public RectTransform _chain;
    public static List<GroupTriad> Parse() {
        var conditions = new List<GroupTriad>();
        GCOperator connective = default;
        bool negate = default;
        string condition = default;
        Dictionary<string, object> data = new();
        void ExtractData(Transform current) {
            var fields = current.GetComponentsInChildren<IPlannerField>();
            foreach (var field in fields) {
                var pair = field.GetFieldData();
                data[pair.Key] = pair.Value;
            }
        }
        void AddCondition() {
            GroupTriad triad = new(condition, connective, negate) {
                Data = data
            };
            conditions.Add(triad);
            connective = default;
            negate = default;
            condition = default;
            data = new();
            print(conditions[conditions.Count - 1]);
        }
        for (int i = 0; i < _Self._chain.childCount; i++) {
            var current = _Self._chain.GetChild(i);
            string name = current.name, trueName = TrueName(current);
            if (name.EndsWith("Condition")) {
                if (connective != default) {
                    if (condition == null) {
                        condition = trueName;
                    } 
                } else {
                    // TODO: check if in precise mode (implicit AND not permitted)
                    connective = GCOperator.AND;
                    condition = trueName;
                    ExtractData(current);
                    AddCondition();
                }
            } else if (name.EndsWith("Connective")) {
                // TODO: Support custom connectives
                switch (trueName) {
                    case "OR":
                        connective = GCOperator.OR;
                        break;
                    case "AND":
                        connective = GCOperator.AND;
                        break;
                    default:
                        // TODO: Support custom connectives
                        if (trueName == "NOT") {
                            print("n");
                            negate = !negate;
                        }
                        break;
                }
            } else if (name.EndsWith("Group")) {
                connective = GCOperator.BUF;
            }
            if (connective != GCOperator.NONE && condition != default) {
                AddCondition();
            }
        }
        return conditions;
        // Groups.Instance.groups.Add(new Group(groupName.text, new(), conditions));
    }
}
