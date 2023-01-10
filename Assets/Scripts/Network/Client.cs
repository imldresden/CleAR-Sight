// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace IMLD.Unity.Network
{
    /// <summary>
    /// Generic class which establishes a socket connection to send data over the network.
    /// </summary>
    public abstract class Client : IDisposable
    {
        protected string _ipAddress;
        protected int _port;

        #region Public Properties
        /// <summary>
        /// The remote ip address to which the data should be send.
        /// </summary>
        public string IpAddress
        {
            get { return _ipAddress; }
        }
        /// <summary>
        /// The remote port to which the data should be send.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }
        /// <summary>
        /// Indicates if the sender is ready to send data to the remote connection.
        /// </summary>
        public abstract bool IsOpen { get; }
        #endregion

        /// <summary>
        /// Create a new instance of this client.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public Client(string ipAdress, int port)
        {
            _ipAddress = ipAdress;
            _port = port;
        }
        /// <summary>
        /// Tries to open the socket connection and, depending on the used protocol, tries to establish a connection with the remote host.
        /// </summary>
        /// <returns><value>true</value> if the socket was opened successfully, otherwise <value>false</value>.</returns>
        public abstract bool Open();
        /// <summary>
        /// Tries to send the specified data to the remote host.
        /// </summary>
        /// <param name="data">The data which should be send.</param>
        /// <returns><value>true</value> if the sending operation was successful, otherwise <value>false</value>.</returns>
        public abstract bool Send(byte[] data);
        /// <summary>
        /// Closes the connection.
        /// </summary>
        public abstract void Close();
        /// <summary>
        /// Free all resources used by this Client.
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }
    }
}
