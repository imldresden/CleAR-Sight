// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPicker : MonoBehaviour, ITouchable
{
    public class ColorChangedEventArgs : EventArgs
    {
        public ColorChangedEventArgs(Color c) => Color = c;
        public Color Color { get; set; }
    }

    protected Material colorPickerMaterial;

    public event EventHandler<ColorChangedEventArgs> ColorChanged;

    public Collider Collider
    {
        get => GetComponent<Collider>();
    }
    public virtual void OnDoubleTap(TouchEvent touch)
    {
        //Debug.Log("Colorpicker double tapped!");
    }

    public virtual void OnHold(TouchEvent touch)
    {
        //Debug.Log("Colorpicker hold!");
    }

    public virtual void OnTap(TouchEvent touch)
    {
        // check that this game object is correctly set up
        if (!colorPickerMaterial)
        {
            Debug.LogWarning("Color picker has no material assigned!");
            return;
        }

        // compute position of touch inside color picker
        float InnerRadius = colorPickerMaterial.GetFloat("_InnerRadius"); // inner radius of the ring
        float OuterRadius = colorPickerMaterial.GetFloat("_OuterRadius"); // outer radius of the ring
        Vector2 Uv = touch.CurrentTarget.textureCoord;
        Vector2 Center = new Vector2(0.5f, 0.5f) - Uv; // shift center of current fragment position for correct color space representation          
        float Radius = Center.magnitude * 2.0f; // compute the radial distance of the polar coordinates of the touch position

        // set color otherwise
        Color Color = GetColorFromUV(Uv);
        SelectedColor = Color;
        OnColorChanged(new ColorChangedEventArgs(Color));
    }

    public virtual void OnTouchDown(TouchEvent touch)
    {
        touch.Capture(this);
        touch.Consume();
    }

    public virtual void OnTouchMove(TouchEvent touch)
    {
        Color Color = GetColorFromUV(touch.CurrentTarget.textureCoord);
        SelectedColor = Color;
        OnColorChanged(new ColorChangedEventArgs(Color));
    }

    public virtual void OnTouchUp(TouchEvent touch)
    {
        //
    }

    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (!renderer)
        {
            Debug.LogError("No MeshRenderer component found!");
        }
        else
        {
            colorPickerMaterial = GetComponent<MeshRenderer>().material;
        }
    }

    public Color SelectedColor
    {
        set
        {
            if (!colorPickerMaterial)
            {
                Debug.LogWarning("Color picker has no material assigned!");
            }
            else
            {
                colorPickerMaterial.SetColor("_SelectedColor", value);
            }
        }

        get
        {
            if (!colorPickerMaterial)
            {
                Debug.LogWarning("Color picker has no material assigned!");
                return Color.white;
            }
            else
            {
                return colorPickerMaterial.GetColor("_SelectedColor");
            }
        }
    }

    protected virtual void OnColorChanged(ColorChangedEventArgs args)
    {
        ColorChanged?.Invoke(this, args);
    }

    /// <summary>
    /// This method computes a color on the color picker for given texture coordinates.
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    protected virtual Color GetColorFromUV(Vector2 uv)
    {
        if (!colorPickerMaterial)
        {
            Debug.LogWarning("Color picker has no material assigned!");
            return Color.white;
        }

        float InnerRadius = colorPickerMaterial.GetFloat("_InnerRadius"); // inner radius of the ring
        float OuterRadius = colorPickerMaterial.GetFloat("_OuterRadius"); // outer radius of the ring
        Color OutputColor; // output/result color

        Vector2 Center = new Vector2(0.5f, 0.5f) - uv; // shift center of current fragment position for correct color space representation          
        float Radius = Center.magnitude * 2.0f; // compute the radial distance of the polar coordinates of the current fragment 

        // leave early if the fragment is not in the ring
        //if (Radius < InnerRadius || Radius > OuterRadius)
        //{
        //    return Color.white;
        //}

        // convert the currently selected color to HSV color space
        Vector4 SelectedColor = colorPickerMaterial.GetColor("_SelectedColor");
        Color.RGBToHSV(SelectedColor, out float h, out float s, out float v);
        Vector4 SelectedColorHSV = new Vector4(h, s, v, SelectedColor.w);

        // compute position of currently selected color in ring
        float SelectedColorAngle = (SelectedColorHSV.x - 0.5f) * Mathf.PI*2f; // h = angle/2pi + 0.5 --> angle = (h-0.5)*2pi
        float SelectedColorRadius = 0.5f * (InnerRadius + ((OuterRadius - InnerRadius) / 2f));
        Vector2 SelectedColorPos = new Vector2(0.5f, 0.5f) - new Vector2(SelectedColorRadius * Mathf.Cos(SelectedColorAngle), SelectedColorRadius * Mathf.Sin(SelectedColorAngle));

        if (colorPickerMaterial.GetFloat("_ShowSaturation") == 0)
        {
            SelectedColorHSV.y = 1;
        }

        if (colorPickerMaterial.GetFloat("_ShowBrightness") == 0)
        {
            SelectedColorHSV.z = 1;
        }

        float angle = Mathf.Atan2(Center.y, Center.x); // compute angle of the polar coordinates of the current fragment
        Vector4 hsv = new Vector4((angle / (Mathf.PI*2f)) + 0.5f, SelectedColorHSV.y, SelectedColorHSV.z, SelectedColorHSV.w);
        OutputColor = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
        OutputColor.a = hsv.w;
        return OutputColor;
    }
}
