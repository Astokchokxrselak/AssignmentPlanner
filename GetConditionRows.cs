using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GetConditionRows : MonoBehaviour
{
    private List<RectTransform> Conditions => LoadGroupCondition.PlaceablesTypes;
    private HorizontalLayoutGroup[] _rows;
    private static string GetType(string name) 
    {
        for (int i = 0; i < PlaceableTypes.Length; i++) {
            if (name.EndsWith(PlaceableTypes[i])) {
                return PlaceableTypes[i];
            }
        } throw new ArgumentException("Type not found");
    }
    private static readonly string[] PlaceableTypes = new[] { "Connective", "Condition" }; 
    private class ConditionNameComparer : IComparer<RectTransform>
    {
        int IComparer<RectTransform>.Compare(RectTransform x, RectTransform y)
        {
            string nameX = x.name, nameY = y.name;
            string typeX = GetConditionRows.GetType(nameX), typeY = GetConditionRows.GetType(nameY);
            var res = typeX.CompareTo(typeY);
            if (res != 0) {
                return res;
            } return nameX.CompareTo(nameY);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        var comparer = new ConditionNameComparer();
        Conditions.Sort(comparer);
        _rows = GetComponentsInChildren<HorizontalLayoutGroup>();
        Generate();
    }
    public float maxWidth = 450f;
    private int currentIndex, count = 0;
    void InstantiateCondition(GameObject obj, Transform group) 
    {
        GameObject condition = Instantiate(obj.gameObject, group.transform);
        Destroy(condition.GetComponent<DraggableElement>());
        condition.AddComponent<DraggableSource>();
    }
    void Generate() 
    {
        for (int g = 0, i = 0; g < _rows.Length; g++) {
            var group = _rows[g];
            for (int j = 0; j < Conditions.Count / _rows.Length; j++, i++)
            {
                InstantiateCondition(Conditions[i].gameObject, group.transform);
            }
        }
        if (Conditions.Count % 2 == 1) {
            InstantiateCondition(Conditions[Conditions.Count - 1].gameObject, _rows[_rows.Length - 1].transform);
        }
    }
}
