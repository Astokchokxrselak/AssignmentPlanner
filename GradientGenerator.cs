using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common.Helpers;
using Common.Extensions;

using Text = TMPro.TextMeshProUGUI;
public class GradientGenerator : MonoBehaviour
{
    private LineRenderer _line;
    [Range(0, 1)] public float t;
    [SerializeField] private Transform _pointer;
    private SpriteRenderer _pointerSprite;
    private float _py;
    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out _line);
        _pointer.TryGetComponent(out _pointerSprite);
        _py = _pointer.position.y;

        InitializeEvents();
    }

    private void InitializeEvents() {
        var whatColor = ComponentHelper.FindObjectOfName<Image>("WhatColor");
        OnChangeColor += ncol => whatColor.color = ncol;

        Transform r = GameObject.Find("R").transform,
                   g = GameObject.Find("G").transform,
                   b = GameObject.Find("B").transform;
        
        Slider rbar = r.FindComponent<Slider>("RBar"),
               gbar = g.FindComponent<Slider>("GBar"),
               bbar = b.FindComponent<Slider>("BBar");
        OnChangeColor += ncol => {
            rbar.value = ncol.r;
            gbar.value = ncol.g;
            bbar.value = ncol.b;
        };

        Text rtxt = r.FindComponent<Text>("Text"),
             gtxt = g.FindComponent<Text>("Text"),
             btxt = b.FindComponent<Text>("Text");
        OnChangeColor += ncol => {
            rtxt.text = ((int)(ncol.r * 255)).ToString();
            gtxt.text = ((int)(ncol.g * 255)).ToString();
            btxt.text = ((int)(ncol.b * 255)).ToString();
        };
    }

    void PlacePointer() {
        float NegX = _line.GetPosition(0).x, PosX = _line.GetPosition(1).x;
        var x = Mathf.Lerp(NegX, PosX, t);
        _pointer.position = new Vector3(x, _py);
    }

    Color _oldColor;
    private event System.Action<Color> OnChangeColor;
    void ColorPointer() {
        _pointerSprite.color = _line.colorGradient.Evaluate(t);
        if (_pointerSprite.color != _oldColor) {
            OnChangeColor?.Invoke(_pointerSprite.color);
            _oldColor = _pointerSprite.color;
        }
    }
    // Update is called once per frame
    void Update()
    {
        PlacePointer();
        ColorPointer();
    }
}
