// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WaterLevelIndicator : MonoBehaviour, ITouchable, IConnectionTarget
{
    public Material WaterFullMaterial;
    public Material WaterEmptyMaterial;

    public FlowerPot FlowerPot;

    public Connection ConnectionPrefab;
    public Connection Connection;

    private MeshRenderer Renderer;
    private bool waterFull;

    private Material waterFullMaterial, waterEmptyMaterial;

    public Collider Collider
    {
        get => GetComponent<Collider>();
    }

    void Start()
    {
        if (!WaterFullMaterial || !WaterEmptyMaterial)
        {
            Debug.LogError("Materials undefined for this component. Disabling component.");
            this.enabled = false;
            return;
        }

        waterFullMaterial = Instantiate<Material>(WaterFullMaterial);
        waterEmptyMaterial = Instantiate<Material>(WaterEmptyMaterial);

        Renderer = GetComponent<MeshRenderer>();
        if (!Renderer)
        {
            Debug.LogError("No MeshRenderer found. Disabling component.");
            this.enabled = false;
            return;
        }

        if (WaterFull == true)
        {
            Renderer.material = waterFullMaterial;
        }
        else
        {
            Renderer.material = waterEmptyMaterial;
        }

        NetworkManager.Instance.RegisterMessageHandler(MessageContainer.MessageType.SET_WATERLEVEL, OnRemoteSetWaterLevel);
    }

    private Task OnRemoteSetWaterLevel(MessageContainer container)
    {
        var message = MessageSetWaterLevel.Unpack(container);

        if (message != null)
        {
            bool waterFull = false;
            if (message.WaterLevel > 0.5f)
            {
                waterFull = true;
            }

            SetWaterLevel(waterFull, false);
        }

        return Task.CompletedTask;
    }

    public bool WaterFull
    {
        get { return waterFull; }
        set
        {
            SetWaterLevel(value);
        }
    }

    private void SetWaterLevel(bool water, bool networkSync = true)
    {
        if (networkSync)
        {
            if (water)
            {
                NetworkManager.Instance?.SendMessage(new MessageSetWaterLevel(1.0f));
            }
            else
            {
                NetworkManager.Instance?.SendMessage(new MessageSetWaterLevel(0.0f));
            }            
        }

        waterFull = water;

        if (!Renderer)
        {
            return;
        }

        if (waterFull == true)
        {
            Renderer.material = waterFullMaterial;
        }
        else
        {
            Renderer.material = waterEmptyMaterial;
        }
    }

    public float Transparency
    {
        set
        {
            if (!waterFullMaterial || !waterEmptyMaterial)
            {
                Debug.LogWarning("Water level indicator has no materials assigned!");
            }
            else
            {
                waterFullMaterial.SetFloat("_Transparency", value);
                waterEmptyMaterial.SetFloat("_Transparency", value);
            }
        }

        get
        {
            if (!waterFullMaterial || !waterEmptyMaterial)
            {
                Debug.LogWarning("Water level indicator has no materials assigned!");
                return 0f;
            }
            else
            {
                return waterFullMaterial.GetFloat("_Transparency");
            }
        }
    }

    public bool AcceptsConnection => true;

    public bool IsConnected => throw new System.NotImplementedException();

    public Transform TargetTransform
    {
        get
        {
            if (FlowerPot)
            {
                return FlowerPot.transform;
            }
            else
            {
                return transform;
            }
        }
    }

    public void OnDoubleTap(TouchEvent touch)
    {
        Debug.Log("Toggle Water Level.");
        WaterFull = !WaterFull;
        touch.Consume();
    }

    public void OnHold(TouchEvent touch)
    {
        //
    }

    public void OnTap(TouchEvent touch)
    {
        touch.Consume();
    }

    public void OnTouchDown(TouchEvent touch)
    {
        touch.Consume();

        if (!AcceptsConnection)
        {
            return;
        }

        touch.Capture(this);

        if (Connection)
        {
            Destroy(Connection.gameObject);
            Connection = null;
        }

        Connection = Instantiate<Connection>(ConnectionPrefab);
        Connection.Source = this;
        Connection.SetTemporaryEndpoint(touch.Position);
        OnConnectionInitiated(Connection);
    }

    public void OnTouchMove(TouchEvent touch)
    {
        if (Connection)
        {
            Connection.SetTemporaryEndpoint(touch.Position);
        }
    }

    public void OnTouchUp(TouchEvent touch)
    {
        if (Connection)
        {
            foreach (var target in touch.Targets)
            {
                var connectionTarget = target.transform.gameObject.GetComponent<IConnectionTarget>();
                if (connectionTarget != null && (IConnectionTarget)this != connectionTarget && connectionTarget.AcceptsConnection)
                {
                    Connection.Target = connectionTarget;
                    connectionTarget.OnConnectionEstablished(Connection);
                    OnConnectionEstablished(Connection);
                    return;
                }
            }
            Destroy(Connection.gameObject);
            Connection = null;
            OnConnectionSevered();
        }
    }

    public void OnConnectionInitiated(Connection c)
    {
        //throw new System.NotImplementedException();
    }

    public void OnConnectionEstablished(Connection c)
    {
        //throw new System.NotImplementedException();
    }

    public void OnConnectionSevered()
    {
        //throw new System.NotImplementedException();
    }

    void OnDestroy()
    {
        if(waterFullMaterial)
        {
            Destroy(waterFullMaterial);
            waterFullMaterial = null;
        }

        if (waterEmptyMaterial)
        {
            Destroy(waterEmptyMaterial);
            waterEmptyMaterial = null;
        }
    }
}
