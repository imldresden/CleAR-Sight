// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using LifxNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LifxManager : MonoBehaviour
{
    public SmartBulb BulbPrefab;
    public Transform BulbAnchor;

    public IReadOnlyList<SmartBulb> SmartBulbs
    {
        get => LightBulbDict.Values.ToList();
    }

    LifxClient Client = null;

    private bool updateVis = false;

    private Queue<LightBulb> BulbsToInstantiate = new Queue<LightBulb>();
    private Queue<LightBulb> BulbsToDestroy = new Queue<LightBulb>();
    private Dictionary<string, SmartBulb> LightBulbDict = new Dictionary<string, SmartBulb>();

    void Awake()
    {
        DiscoverDevices();
    }


    // Update is called once per frame
    async void Update()
    {
        if (BulbsToInstantiate.Count > 0)
        {
            LightBulb Device = BulbsToInstantiate.Dequeue();
            GameObject GO = Instantiate(BulbPrefab.gameObject, BulbAnchor);
            var Bulb = GO.GetComponent<SmartBulb>();
            Bulb.Device = Device;
            LightBulbDict.Add(Bulb.Device.HostName, Bulb);
        }

        if (BulbsToDestroy.Count > 0)
        {
            LightBulb Device = BulbsToDestroy.Dequeue();
            SmartBulb Bulb;
            if (LightBulbDict.TryGetValue(Device.HostName, out Bulb))
                Destroy(Bulb.gameObject);
            LightBulbDict.Remove(Device.HostName);
        }

        foreach(var Bulb in LightBulbDict.Values)
        {
            if (Bulb.State.LastUpdate == 0f)
            {
                Bulb.State.LastUpdate = Time.realtimeSinceStartup;
                var LightState = await Client.GetLightStateAsync(Bulb.Device);
                Bulb.State.Color = LifxToUnity(LightState.Hue, LightState.Saturation, LightState.Brightness);
                Bulb.State.Power = LightState.IsOn;
                Bulb.State.Temperature = LightState.Kelvin;
                Bulb.State.IsChanged = false;                
                Debug.Log(Bulb.Device.HostName + ": Powered: " + Bulb.State.Power + ", Color: " + Bulb.State.Color + ", Temperature: " + Bulb.State.Temperature);
            }
            else if (Bulb.State.IsChanged == true && (Time.realtimeSinceStartup - Bulb.State.LastUpdate > 0.05f))
            {
                if (Bulb.State.HasChangedPower)
                {
                    SetPower(Bulb.Device, Bulb.State.Power, Bulb.State.TransitionTime);
                }

                if (Bulb.State.HasChangedColor)
                {
                    SetColor(Bulb.Device, Bulb.State.Color, Bulb.State.Temperature, Bulb.State.TransitionTime);
                    NetworkManager.Instance?.SendMessage(new MessageColorPickerChanged(Bulb.State.Color, Bulb.Device.HostName));
                }

                Bulb.State.IsChanged = false;
                Bulb.State.LastUpdate = Time.realtimeSinceStartup;
            }

            if (InteractionHandler.DebugMode)
            {
                Bulb.Visual.SetActive(true);
            }
            else
            {
                Bulb.Visual.SetActive(false);
            }
        }
    }

    private async void DiscoverDevices()
    {
        if (Client == null)
        {
            Debug.Log("Searching for Devices");
            Client = await LifxNet.LifxClient.CreateAsync();
            Client.DeviceDiscovered += Client_DeviceDiscovered;
            Client.DeviceLost += Client_DeviceLost;
        }
        Client.StartDeviceDiscovery();
    }

    private void Client_DeviceDiscovered(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
    {
        var bulb = e.Device as LifxNet.LightBulb;
        Debug.Log("Bulb " + LightBulbDict.Count + ": " + bulb.HostName);

        if (!LightBulbDict.ContainsKey(bulb.HostName))
            BulbsToInstantiate.Enqueue(bulb);
    }

    private void Client_DeviceLost(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
    {
        var bulb = e.Device as LifxNet.LightBulb;
        if (LightBulbDict.ContainsKey(bulb.HostName))
            BulbsToDestroy.Enqueue(bulb);
    }

    private void SetColor(LightBulb bulb, UnityEngine.Color color, int temperature, float transitionTime = 0.0f)
    {
        ushort[] hsb = UnityToLifx(color);
        Client.SetColorAsync(bulb, hsb[0], hsb[1], hsb[2], (ushort)temperature, TimeSpan.FromSeconds(transitionTime));
    }

    private void SetPower(LightBulb bulb, bool powerStatus, float transitionTime = 0.0f)
    {
        if (powerStatus)
        {
            Client.TurnBulbOnAsync(bulb, TimeSpan.FromSeconds(transitionTime));
        }
        else
        {
            Client.TurnBulbOffAsync(bulb, TimeSpan.FromSeconds(transitionTime));
        }
    }

    public static ushort[] UnityToLifx(UnityEngine.Color color)
    {
        ushort[] hsb = new ushort[3];
        UnityEngine.Color.RGBToHSV(color, out float h, out float s, out float v);
        hsb[0] = (ushort)(h * 65535f);
        hsb[1] = (ushort)(s * 65535f);
        hsb[2] = (ushort)(v * 65535f);
        return hsb;
    }

    public static UnityEngine.Color LifxToUnity(LifxNet.Color col)
    {
        return new UnityEngine.Color(col.R / 65535f, col.G / 65535f, col.B / 65535f);
    }

    public static UnityEngine.Color LifxToUnity(ushort hue, ushort saturation, ushort brightness)
    {
        return UnityEngine.Color.HSVToRGB(hue / 65535f, saturation / 65535f, brightness / 65535f);
    }
}
