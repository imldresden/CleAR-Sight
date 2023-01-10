// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.Unity.Network
{
    public class ClientTcp : Client
    {
        private Socket _socket;
        private bool _isOpen;
        private readonly int _bufferSize = 65536;

        /// <summary>
        /// Indicates if the client is connected to a server.
        /// </summary>
        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        #region Events
        /// <summary>
        /// Called, when the client successfully connected to a server.
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Called, when the client successfully received data from the server.
        /// </summary>
        public event ByteDataHandler DataReceived;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the ClientTcp class.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public ClientTcp(string ipAddress, int port)
            : base(ipAddress, port)
        {
            _isOpen = false;
            _socket = null;
        }
        #endregion

        #region Private Methods
        private void Connect_Completed(object sender, SocketAsyncEventArgs args)
        {
            args.Completed -= Connect_Completed;
            args.Dispose();
            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ClientTcp - ERROR, " + args.SocketError);
                return;
            }
            var receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            receiveArgs.Completed += Receive_Completed;
            _socket.ReceiveAsync(receiveArgs);
            _isOpen = true;
            if (Connected != null)
                Connected(this, EventArgs.Empty);
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ClientTcp - ERROR receiving data: " + args.SocketError);
                args.Dispose();
                Close();
                return;
            }
            if (args.BytesTransferred > 0)
            {
                byte[] msg = new byte[args.BytesTransferred];
                Array.Copy(args.Buffer, 0, msg, 0, args.BytesTransferred);
                OnDataReceived((IPEndPoint)_socket.RemoteEndPoint, msg);
            }
            // if the connection has since been terminated, don't start a new receive operation, but dispose the args to free the resources, etc.
            if (!_isOpen)
            {
                args.Completed -= Receive_Completed;
                args.Dispose();
                return;
            }
            try
            {
                _socket.ReceiveAsync(args);
            }
            catch (Exception ex)
            {
                Debug.Log("ClientTcp - ERROR receiving data:\n\t" + ex.Message);
                Close();
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                Debug.Log("ClientTcp - ERROR sending data: " + e.SocketError);
            e.Completed -= Send_Completed;
            e.Dispose();
        }

        protected virtual void OnDataReceived(IPEndPoint remoteEndPoint, byte[] data)
        {
            if (DataReceived != null)
                DataReceived(this, remoteEndPoint, data);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Tries to open the connection to the server.
        /// Note: If the method return true, this doesn't mean the connection itself was succesfull, only that the atempt has been started.
        /// Subscribe to the Connected event to be notified if the connection to the server has been successfully established.
        /// </summary>
        /// <returns>true if the connection attempt has been successfully started, otherwise false.</returns>
        public override bool Open()
        {
            if (_socket != null)
                return false;
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            args.Completed += Connect_Completed;
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.ConnectAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ClientTcp - ERROR, could not connect to device:\n" + e.Message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Send the specified data to the server. Only works then the client is connected.
        /// </summary>
        /// <param name="data">The data that should be send.</param>
        /// <returns>true if the data has been send, otherwise false.</returns>
        public override bool Send(byte[] data)
        {
            if (!_isOpen)
                return false;
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.Completed += Send_Completed;
            try
            {
                _socket.SendAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ClientTcp - ERROR while sending data:\n" + e.Message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Terminate the connection to the server and free all used resources.
        /// </summary>
        public override void Close()
        {
            _isOpen = false;
            if (_socket != null && _socket.Connected)
            {
                _socket.Kill();
                _socket = null;
            }
        }
        #endregion
    }
}
