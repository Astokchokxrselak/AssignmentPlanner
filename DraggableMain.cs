using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.Extensions;

public interface IOutlinable
{
    public abstract Outline Outline { get; }
    public abstract void ToggleOutline(bool enabled);
}
public class DraggableMain : MonoBehaviour
{
    private void _Disable() {
        gameObject.SetActive(false);
    }
    public void Disable() {
        _Disable();
        quitDisabled = false;
    }
    public void DisableQuit() {
        _Disable();
        quitDisabled = true;
    }
    public static bool quitDisabled = false;
    public RectTransform _outline;
    public RectTransform _touchedParent, _bucket, _viewport, _chain;
    public ScrollRect _scroll;
    private GameObject _helper;
    public static RectTransform touchedParent;
    public static RectTransform bucket, viewport, chain;
    public static RectTransform outline;
    public static ScrollRect scroll;
    private static GameObject helper;

    public static DraggableSource source;
    public static DraggableElement element;

    public static void SetSource(DraggableSource s) 
    {
        if (source != s) 
        {
            if (source) {
                source.ToggleOutline(false);
            }
            if (element) {
                element.ToggleOutline(false);
                element = null;
            }
            source = s;
            s.ToggleOutline(true);
        }
    }
    public static void AppendSource() {
        AppendSource(chain.GetChild(chain.childCount - 1).GetComponent<DraggableElement>());
    }
    public static void AppendSource(DraggableElement element) 
    {
        if (!source) {
            throw new System.ArgumentException("Source not set for element to be placed!");
        }

        source.ToggleOutline(false);
        var newElement = Instantiate(source.gameObject);
        Destroy(newElement.GetComponent<DraggableSource>());
        newElement.AddComponent<DraggableElement>();
        
        var newRect = newElement.transform as RectTransform;
        var rect = element.transform as RectTransform; // chain
        
        newRect.SetParent(rect.parent, false);
        newRect.SetSiblingIndex(rect.GetSiblingIndex() + 1);

        source = null;
        RebuildLayout(rect);
    }
    private static void RebuildLayout(RectTransform rect) {
        IEnumerator _IEnum() {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect.parent as RectTransform);
        }
        CommonGameManager.Coroutine(_IEnum());
    }
    
    public float _scrollSensitivity;
    public static float scrollSensitivity;
    public void Awake() 
    {
        touchedParent = _touchedParent;
        bucket = _bucket;
        viewport = _viewport;
        outline = _outline;
        scroll = _scroll;
        scrollSensitivity = _scrollSensitivity;
        chain = _chain;

        helper = new GameObject("HELPER");
        helper.transform.SetParent(scroll.transform, true);
    }
    /// <summary>
    /// Blocks have an anchor on the top left; adjustments are made so that the anchor is on the top right.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static RectTransform ClosestBlock(Vector3 point, float radius) 
    {
        RectTransform closest = null;
        float distance = -1;
        foreach (RectTransform t in bucket)
        {
            if (t.name != "Chain" && !t.AncestorOfName("Chain")) {
                continue;
            }
            var ndist = Vector2.Distance(t.anchoredPosition + new Vector2(t.sizeDelta.x, 0), point);
            helper.transform.position = t.anchoredPosition + new Vector2(t.sizeDelta.x, 0);
            Debug.Break();
            if (ndist == 0) {
                continue;
            }
            if ((distance == -1 || ndist < distance) && ndist < radius) {
                distance = ndist;
                closest = t;
            }
        }
        return closest;
    }
    public static void ScrollBasedOnMousePosition() 
    {
        var viewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        var signedViewport = viewport.x * 2 - 1;
        if (Mathf.Abs(signedViewport) > 0.75f) 
        {
            scroll.horizontalScrollbar.value += viewport.x * Time.deltaTime * scrollSensitivity * Mathf.Sign(signedViewport);
        }
    }
    public static RectTransform HighlightClosestBlock(Vector3 point, float radius, float highlightLength) 
    {
        var block = ClosestBlock(point, radius);
        if (!block) {
            Unhighlight();
            return null;
        }
        outline.gameObject.SetActive(true);
        outline.SetParent(block, false);
        outline.sizeDelta = new(highlightLength, outline.sizeDelta.y);
        return block;
    }
    public static void Highlight(RectTransform block, float highlightLength) 
    {    
        outline.gameObject.SetActive(true);
        outline.SetParent(block, false);
        outline.sizeDelta = new(highlightLength, outline.sizeDelta.y);
    }
    public static void Unhighlight() 
    {
        outline.SetParent(null, false);
        outline.gameObject.SetActive(false);
    }
    public static void SetElement(DraggableElement e) 
    {
        if (element) {
            element.ToggleOutline(false);
        }
        element = e;
        element.ToggleOutline(true);
    }
    public static void DestroyElement() 
    {
        element.ToggleOutline(false);
        Destroy(element.gameObject);
        element = null;
    }
    public void Update() {
        if (element) {
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) {
                DestroyElement();
            }
        }
    }
}
