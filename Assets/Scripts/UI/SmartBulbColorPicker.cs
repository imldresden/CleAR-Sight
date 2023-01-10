// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartBulbColorPicker : ColorPicker, IConnectionTarget
{

    public SmartBulb Bulb;
    public class PowerChangedEventArgs : EventArgs
    {
        public PowerChangedEventArgs(bool state) => PowerOn = state;
        public bool PowerOn { get; set; }
    }

    public event EventHandler<PowerChangedEventArgs> PowerChanged;

    public override void OnTap(TouchEvent touch)
    {
        touch.Consume();

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

        if (Radius < InnerRadius)
        {
            // toggle power when inside the inner radius
            PowerOn = !PowerOn;
            PowerChanged?.Invoke(this, new PowerChangedEventArgs(PowerOn));
        }
        else
        {
            // set color otherwise
            Color Color = GetColorFromUV(Uv);
            SelectedColor = Color;
            OnColorChanged(new ColorChangedEventArgs(Color));
        }
    }

    public override void OnTouchDown(TouchEvent touch)
    {
        touch.Consume();
    }

    public override void OnTouchMove(TouchEvent touch)
    {
        Color Color = GetColorFromUV(touch.CurrentTarget.textureCoord);
        SelectedColor = Color;
        OnColorChanged(new ColorChangedEventArgs(Color));
        touch.Consume();
    }

    public override void OnTouchUp(TouchEvent touch)
    {
        touch.Consume();
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

    public bool PowerOn { set; get; }

    public bool AcceptsConnection => true;

    public bool IsConnected => throw new NotImplementedException();

    public Transform TargetTransform
    {
        get
        {
            if (Bulb)
            {
                return Bulb.transform;
            }
            else
            {
                return transform;
            }
        }
    }

    public void OnConnectionInitiated(Connection c)
    {
        //throw new NotImplementedException();
    }

    public void OnConnectionEstablished(Connection c)
    {
        //throw new NotImplementedException();
    }

    public void OnConnectionSevered()
    {
        //throw new NotImplementedException();
    }
}
