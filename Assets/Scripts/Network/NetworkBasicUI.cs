// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Touch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.Unity.Network
{
    public class NetworkBasicUI : MonoBehaviour
    {
        private void OnGUI()
        {
            if (InteractionHandler.DebugMode == false)
            {
                return;
            }

            // If we are already running a server or have connected to one, do not show this UI.
            if (!NetworkManager.Instance)
            {
                GUI.Label(new Rect(10, 75, 200, 30), "Network Manager not configured.");
                return;
            }

            if (NetworkManager.Instance.IsServer)
            {
                var ips = NetworkManager.Instance.Network.ServerIPs;
                switch (ips.Count)
                {
                    case 0:
                        GUI.Label(new Rect(10, 75, 200, 30), "Server configuration error.");
                        break;
                    case 1:
                        GUI.Label(new Rect(10, 75, 200, 30), "Running as server on " + ips[0] + ".");
                        break;
                    default:
                        GUI.Label(new Rect(10, 75, 200, 30), "Running as server on " + ips.Count + " IPs.");
                        break;
                }                
                return;
            }

            if (NetworkManager.Instance.IsConnected)
            {
                GUI.Label(new Rect(10, 75, 200, 30), "Running as client, connected to " + NetworkManager.Instance.Session?.SessionIp + ".");
                return;
            }

            GUI.Label(new Rect(10, 75, 200, 30), "Run as client or server?");

            // User's selection of client or server
            if (GUI.Button(new Rect(10, 110, 200, 60), "Client"))
            {
                if (NetworkManager.Instance)
                {
                    NetworkManager.Instance.AutomaticallyConnectToServer = true;
                }

                NetworkManager.Instance?.StartAsClient();
            }

            if (GUI.Button(new Rect(10, 180, 200, 60), "Server"))
            {
                NetworkManager.Instance?.StartAsServer();
                var touchEventDispatcher = FindObjectOfType<TouchEventDispatcher>();
                if (touchEventDispatcher)
                {
                    touchEventDispatcher.enabled = true;
                }
            }
        }
    }
}