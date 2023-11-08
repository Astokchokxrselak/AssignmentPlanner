using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using Common;
using Common.Helpers;
using Common.Extensions;

public class DraggableElement : MonoBehaviour, IOutlinable, IPointerDownHandler // , IPointerUpHandler // , IPointerMoveHandler
{
    public Dictionary<string, object> data;

    public bool placed = false, immortal;
    private RectTransform _rect;
    private bool touched;

    public Outline Outline { get; set; }
    private void Awake() 
    {
        _rect = transform as RectTransform;
        _rect.anchorMax = _rect.anchorMin = new(0, 1);
        Outline = GetComponent<Outline>();
        
        name.Trim();
        var index = name.LastIndexOf("(Clone)");
        if (index != -1) { 
            name = name.Substring(0, index);
        }
    }
    public void ToggleOutline(bool active) {
        Outline.enabled = active;
    }
    public void OnPointerDown(PointerEventData eventData) 
    {
        // TODO: behavior when source element is selected
        if (DraggableMain.source)
        {
            DraggableMain.AppendSource(this);
        }
        else
        {
            if (DraggableMain.element != this) {
                DraggableMain.SetElement(this);
            } else if (!immortal) {
                DraggableMain.DestroyElement();
            }
        }
    }

    private const float closestBlockRadius = 100f, exitRadius = 120f; // closestBlockRadius - radius to connect to a block, exitRadius - radius to unhighlight a block that was already a potential connection
    /*public void OnPointerMove(PointerEventData eventData) {
        if (touched) {
            _rect.SetParent(touchedParent, false);
            _rect.anchoredPosition = eventData.pressPosition;
        }
    }*/
}
