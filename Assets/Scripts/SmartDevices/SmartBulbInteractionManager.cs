// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Core;
using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using IMLD.Unity.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SmartBulbInteractionManager : MonoBehaviour
{
    public LifxManager BulbManager;
    public BoxCollider TabletCollider;
    public SmartBulbColorPicker ColorPickerPrefab;
    public LineConnector LineConnectorPrefab;

    private Dictionary<SmartBulb, SmartBulbColorPicker> ColorPickersDict = new Dictionary<SmartBulb, SmartBulbColorPicker>();

    private Dictionary<SmartBulb, LineConnector> Lines = new Dictionary<SmartBulb, LineConnector>();
    private const float COLORPICKER_SIZE = 0.075f;

    // Start is called before the first frame update
    void Start()
    {
        if (!BulbManager)
        {
            Debug.LogError("No LifxManager defined!");
            enabled = false;
        }

        if (!TabletCollider)
        {
            Debug.LogError("No BoxColliders for Tablet defined!");
            enabled = false;
        }

        NetworkManager.Instance.RegisterMessageHandler(MessageContainer.MessageType.COLORPICKER_CHANGED, OnRemoteColorPickerChange);
    }

    private Task OnRemoteColorPickerChange(MessageContainer container)
    {
        var message = MessageColorPickerChanged.Unpack(container);

        if (message != null)
        {
            foreach (var picker in ColorPickersDict.Values)
            {
                if (picker.Bulb.Device.HostName == message.Id)
                {
                    //picker.SelectedColor = message.Color;
                    picker.Bulb.State.Color = message.Color;
                }
            }
        }

        return Task.CompletedTask;
    }

    // Update is called once per frame
    void Update()
    {
        CheckSmartBulbs();     
    }

    private void CheckSmartBulbs()
    {
        if (!BulbManager || !TabletCollider)
        {
            return;
        }

        // get camera/head position
        //Vector3 headPosition = CameraCache.Main.transform.position;
        Vector3 headPosition = UserPositionManager.Instance.GetClosestUser(transform.position).position;

        var bulbs = BulbManager.SmartBulbs;
        foreach (var bulb in bulbs)
        {
            if (TabletCollider.Raycast(new Ray(headPosition, bulb.transform.position - headPosition), out RaycastHit hit, 5.0f))
            {
                if (!ColorPickersDict.ContainsKey(bulb))
                {
                    GameObject go = Instantiate(ColorPickerPrefab.gameObject, TabletCollider.transform);
                    Vector3 ColorPickerScale = new Vector3(COLORPICKER_SIZE / go.transform.parent.localScale.x,
                                          COLORPICKER_SIZE / go.transform.parent.localScale.z,
                                          1);
                    go.transform.localScale = ColorPickerScale;
                    ColorPickersDict.Add(bulb, go.GetComponent<SmartBulbColorPicker>());
                }

                SmartBulbColorPicker picker = ColorPickersDict[bulb];
                picker.gameObject.SetActive(true);
                picker.SelectedColor = bulb.State.Color;
                picker.PowerOn = bulb.State.Power;
                picker.ColorChanged += PickerColorChanged;
                picker.PowerChanged += PickerPowerChanged;
                picker.transform.position = hit.point;
                picker.Bulb = bulb;

                Vector3 pointOnCollider = TabletCollider.transform.InverseTransformPoint(hit.point);

                // world-scale distance to x and z borders
                float distX = ((0.5f * TabletCollider.size.x) - Mathf.Abs(pointOnCollider.x)) * TabletCollider.gameObject.transform.localScale.x;
                float distZ = ((0.5f * TabletCollider.size.z) - Mathf.Abs(pointOnCollider.z)) * TabletCollider.gameObject.transform.localScale.z;

                // min world-scale distance to borders
                float dist = Mathf.Min(distX, distZ);
                float size = Mathf.Min(2f * dist, COLORPICKER_SIZE);
                //picker.transform.localScale = new Vector3(size / picker.transform.parent.localScale.x, size / picker.transform.parent.localScale.z, picker.transform.localScale.z);
                picker.transform.localScale = new Vector3(size / picker.transform.parent.localScale.x, size / picker.transform.parent.localScale.z, 1);
                picker.SelectedColor = new Color(picker.SelectedColor.r, picker.SelectedColor.g, picker.SelectedColor.b, size / 0.04f);
            }
            else
            {
                if (ColorPickersDict.ContainsKey(bulb))
                {
                    ColorPicker picker = ColorPickersDict[bulb];
                    picker.gameObject.SetActive(false);
                }
            }
        }
    }

    private void PickerPowerChanged(object sender, SmartBulbColorPicker.PowerChangedEventArgs e)
    {
        SmartBulbColorPicker picker = sender as SmartBulbColorPicker;
        if (picker)
        {
            foreach (var pair in ColorPickersDict)
            {
                if (pair.Value == picker)
                {
                    pair.Key.SetPower(e.PowerOn);
                }
            }
        }
    }

    private void PickerColorChanged(object sender, ColorPicker.ColorChangedEventArgs e)
    {
        ColorPicker picker = sender as ColorPicker;
        if (picker)
        {
            foreach (var pair in ColorPickersDict)
            {
                if (pair.Value == picker)
                {
                    pair.Key.SetColor(e.Color);
                }
            }
        }
    }
}
