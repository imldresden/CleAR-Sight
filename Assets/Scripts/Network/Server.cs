// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace IMLD.Unity.Network
{
    public delegate void ByteDataHandler(object sender, IPEndPoint remoteEndPoint, byte[] data);
    /// <summary>
    /// Generic class which establishes a socket connection to receive data over the network.
    /// </summary>
    public abstract class Server : IDisposable
    {
        protected int _port;

        public event ByteDataHandler DataReceived;
        /// <summary>
        /// The port the receiver listens for incoming data.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }
        /// <summary>
        /// Indicates if the receiver is currently listening for incomming data or not.
        /// </summary>
        public abstract bool IsListening { get; }

        public Server(int port)
        {
            _port = port;
        }

        protected virtual void OnDataReceived(IPEndPoint remoteEndPoint, byte[] data)
        {
            if (DataReceived != null)
                DataReceived(this, remoteEndPoint, data);
        }
        /// <summary>
        /// Start the receiver, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the receiver was successfully started, otherwise <value>false</value>.</returns>
        public abstract bool Start();
        /// <summary>
        /// Stops the receiver if it is currently running, freeing the specified port.
        /// </summary>
        public abstract void Stop();
        public virtual void Dispose()
        {
            Stop();
        }
    }
}
