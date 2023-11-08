using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using Common;
using Common.Extensions;
public class DraggableSource : MonoBehaviour, IOutlinable, IPointerDownHandler
{
    public Outline Outline { get; set; }
    private void Start() {
        Outline = GetComponent<Outline>();
    }
    public void ToggleOutline(bool active) {
        Outline.enabled = active;
    }
    public void OnPointerDown(PointerEventData eventData) 
    {
        if (DraggableMain.source != this) {
            DraggableMain.SetSource(this);
        } else {
            DraggableMain.AppendSource();
        }
    }
}
