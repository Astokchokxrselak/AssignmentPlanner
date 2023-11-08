using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySizeFitter : MonoBehaviour
{
    RectTransform RectTransform => (RectTransform)transform;
    public float spacing;
    // Start is called before the first frame update
    void Awake()
    {
        FitToChildren();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Vector3 origin, sizeOrigin;
    public Transform list;
    public void FitToChildren()
    {
        float h = 0;
        foreach (RectTransform rect in list)
        {
            h += rect.sizeDelta.y + spacing;
            print(rect);
        }
        RectTransform.anchoredPosition = new(RectTransform.anchoredPosition.x, origin.y - h);
        RectTransform.sizeDelta = new(RectTransform.sizeDelta.x, sizeOrigin.y + h); // cut off the last spacing
    }
}
