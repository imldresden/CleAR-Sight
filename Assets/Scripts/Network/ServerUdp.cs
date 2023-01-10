// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.Unity.Network
{
    public class ServerUdp : Server
    {
        private readonly int _bufferSize;
        private Socket _socket;
        private SocketAsyncEventArgs _socketEventArgs;

        public override bool IsListening
        {
            get { return _socket != null; }
        }

        public ServerUdp(int port, int bufferSize = 65536)
            : base(port)
        {
            _bufferSize = bufferSize;
            _socketEventArgs = new SocketAsyncEventArgs();
            _socketEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _socketEventArgs.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            _socketEventArgs.Completed += Receive_Completed;
            Debug.Log("Listener initialized");
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0)
                return;
            byte[] msg = new byte[e.BytesTransferred];
            Array.Copy(e.Buffer, 0, msg, 0, e.BytesTransferred);
            OnDataReceived((IPEndPoint)e.RemoteEndPoint, msg);

            _socket.ReceiveFromAsync(e);
        }

        public override bool Start()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
#if !NETFX_CORE
                _socket.EnableBroadcast = true;
#endif
                _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
                _socket.ReceiveFromAsync(_socketEventArgs);
                Debug.Log("Socket openend");
            }
            catch (Exception e)
            {
                Debug.LogError("ReceiverUdp - ERROR, could not open socket:\n" + e.Message);
                return false;
            }
            return true;
        }

        public override void Stop()
        {
            if (_socket == null)
                return;
            _socket.Kill();
            _socket = null;
        }

        public override void Dispose()
        {
            Stop();
        }
    }
}
