// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

public interface IConnectionTarget
{
    public bool AcceptsConnection
    {
        get;
    }

    public bool IsConnected
    {
        get;
    }

    public Transform TargetTransform
    {
        get;
    }

    public void OnConnectionInitiated(Connection c);
    public void OnConnectionEstablished(Connection c);
    public void OnConnectionSevered();
}
