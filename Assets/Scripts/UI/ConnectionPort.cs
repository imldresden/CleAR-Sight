// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionPort : MonoBehaviour, ITouchable
{
    public GameObject Source;

    public Connection ConnectionPrefab;

    public Connection Connection;

    public Collider Collider
    {
        get => GetComponent<Collider>();
    }

    public void OnDoubleTap(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnHold(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTap(TouchEvent touch)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTouchDown(TouchEvent touch)
    {
        Debug.Log("Port DOWN");
        IConnectionTarget ConnectionSource = Source.GetComponent<IConnectionTarget>();
        if (!(MonoBehaviour)ConnectionSource || !ConnectionSource.AcceptsConnection)
        {
            return;
        }

        touch.Capture(this);

        if (Source)
        {
            if (Connection)
            {
                Destroy(Connection.gameObject);
                Connection = null;
            }

            Connection = Instantiate<Connection>(ConnectionPrefab);
            Connection.Source = ConnectionSource;
            Connection.SetTemporaryEndpoint(touch.Position);
            ConnectionSource.OnConnectionInitiated(Connection);
        }
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
        Debug.Log("Port UP");
        if (Connection)
        {
            Destroy(Connection.gameObject);
            Connection = null;
            IConnectionTarget ConnectionSource = Source.GetComponent<IConnectionTarget>();
            if ((MonoBehaviour)ConnectionSource)
            {
                ConnectionSource.OnConnectionSevered();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
