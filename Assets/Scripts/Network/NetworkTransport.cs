// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using IMLD.Unity.Network.Messages;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

namespace IMLD.Unity.Network
{
    /// <summary>
    /// This Unity component serves as a layer between the high-level, application specific <see cref="NetworkManager"/> and the low-level network classes.
    /// </summary>
    public class NetworkTransport : MonoBehaviour
    {
        public ServerTcp Server;

        private const int MESSAGE_HEADER_LENGTH = MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH;
        private const int MESSAGE_SIZE_LENGTH = 4;
        private const int MESSAGE_TYPE_LENGTH = 1;

        private ClientTcp client;
        private ServerUdp listener;
        private bool justConnected = false;
        private bool isConnecting = false;
        private string announceMessage;
        private int port;
        private List<ClientUdp> announcers = new List<ClientUdp>();
        private string serverName = "Server";
        private readonly ConcurrentQueue<MessageContainer> messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly ConcurrentQueue<Socket> clientConnectionQueue = new ConcurrentQueue<Socket>();
        private readonly Dictionary<IPEndPoint, EndPointState> endPointStates = new Dictionary<IPEndPoint, EndPointState>();
        private readonly Dictionary<string, string> broadcastIPs = new Dictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (client != null && client.IsOpen)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating the port that the server is running on.
        /// </summary>
        public int Port { get { return port; } }

        /// <summary>
        /// Gets a value indicating the server name.
        /// </summary>
        public string ServerName { get { return serverName; } }

        /// <summary>
        /// Gets a value indicating the server IPs.
        /// </summary>
        public IReadOnlyList<string> ServerIPs { get { return broadcastIPs.Values.ToList().AsReadOnly(); } }

        /// <summary>
        /// Pauses the handling of network messages.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Restarts the handling of network messages.
        /// </summary>
        public void Unpause()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Starts listening for servers.
        /// </summary>
        /// <returns><see langword="true"/> if the client started listening for announcements, <see langword="false"/> otherwise.</returns>
        public bool StartListening()
        {
            // listen for server announcements on broadcast
            Debug.Log("searching for server...");
            return listener.Start();
        }

        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening()
        {
            listener?.Stop();
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public bool ConnectToServer(string ip, int port)
        {
            if (isConnecting)
            {
                return false;
            }

            if (IsConnected)
            {
                client.Close();
            }

            client = new ClientTcp(ip, port);
            Debug.Log("Connecting to server at " + ip);
            client.Connected += OnConnectedToServer;
            client.DataReceived += OnDataReceived;
            isConnecting = true;
            return client.Open();
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message)
        {
            client.Send(message.Serialize());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(int port, string message)
        {
            this.port = port;
            announceMessage = message;

            // setup server
            Server = new ServerTcp(this.port);
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            ////Server.DataReceived += OnDataReceived;
            Server.DataReceived += OnDataReceivedAtServer;

            // start server
            bool success = Server.Start();
            if (success == false)
            {
                Debug.Log("Failed to start server!");
                return false;
            }

            Debug.Log("Started server!");

            // announce server via broadcast
            success = false;
            foreach (var item in broadcastIPs)
            {
                var announcer = new ClientUdp(item.Key, 11338);
                if (!announcer.Open())
                {
                    Debug.Log("Failed to start announcing on " + item.Key + "!");
                }
                else
                {
                    announcers.Add(announcer);
                    Debug.Log("Started announcing on " + item.Key + "!");
                    success = true;
                }
            }

            if (success == false)
            {
                Debug.LogError("Failed to start announcing server!");
                return false;
            }

            InvokeRepeating(nameof(AnnounceServer), 1.0f, 2.0f);
            return true;
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToAll(MessageContainer message)
        {
            byte[] envelope = message.Serialize();
            foreach (var client in Server.Clients)
            {
                if (client.Connected)
                {
                    Server.SendToClient(client, envelope);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="client">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, Socket client)
        {
            byte[] envelope = message.Serialize();

            Server.SendToClient(client, envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (announcers != null && announcers.Count != 0)
            {
                CancelInvoke("AnnounceServer");
                foreach (var announcer in announcers)
                {
                    announcer?.Close();
                    announcer?.Dispose();
                }
                announcers.Clear();
            }

            Server?.Stop();
            Server?.Dispose();
            Server = null;
        }

        private void Awake()
        {
            // compute local & broadcast ip and look up server name
            CollectNetworkInfo(); // platform dependent, might not work in all configurations

            // create listen server for server announcements
            listener = new ServerUdp(11338);
            listener.DataReceived += OnBroadcastDataReceived;
        }

        private async void Update()
        {
            if (justConnected)
            {
                justConnected = false;
                NetworkManager.Instance?.OnConnectedToServer();
            }

            MessageContainer message;
            while (!IsPaused && messageQueue.TryDequeue(out message))
            {
                await NetworkManager.Instance?.HandleNetworkMessageAsync(message);
            }

            Socket client;
            while (clientConnectionQueue.TryDequeue(out client))
            {
                NetworkManager.Instance?.HandleNewClient(client);
            }
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            Debug.Log("Connected to server!");
            justConnected = true;
            isConnecting = false;
        }

        private void OnBroadcastDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            messageQueue.Enqueue(MessageContainer.Deserialize(remoteEndPoint, data));
        }

        // called by InvokeRepeating
        private void AnnounceServer()
        {
            foreach (var announcer in announcers)
            {
                if (announcer.IsOpen)
                {
                    var Message = new MessageAnnouncement(announceMessage, broadcastIPs[announcer.IpAddress], serverName, port);
                    announcer.Send(Message.Pack().Serialize());
                    //Debug.Log("Announcing server at " + announcer.IpAddress + " with message " + announceMessage);
                }
                else
                {
                    announcer.Open();
                }
            }
        }

        private void OnDataReceivedAtServer(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            // dispatch received data to all other clients (but not the original sender)
            if (Server != null)
            {
                // only if we have a server
                Dispatch(remoteEndPoint, data);
            }

            OnDataReceived(sender, remoteEndPoint, data);
        }

        private void OnDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            int currentByte = 0;
            int dataLength = data.Length;
            EndPointState state;
            try
            {
                if (endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    endPointStates[remoteEndPoint] = state;
                }

                state.CurrentSender = remoteEndPoint;
                while (currentByte < dataLength)
                {
                    int messageSize;

                    // currently still reading a (large) message?
                    if (state.IsMessageIncomplete)
                    {
                        // 1. get size of current message
                        messageSize = state.CurrentMessageBuffer.Length;

                        // 2. read data
                        // decide how much to read: not more than remaining message size, not more than remaining data size
                        int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, data.Length - currentByte);
                        Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                        currentByte += lengthToRead; // increase "current byte pointer"
                        state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                        // 3. decide how to proceed
                        if (state.CurrentMessageBytesRead == messageSize)
                        {
                            // Message is completed
                            state.IsMessageIncomplete = false;
                            messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                        }
                        else
                        {
                            // We did not read the whole message yet
                            state.IsMessageIncomplete = true;
                        }
                    }
                    else if (state.IsHeaderIncomplete)
                    {
                        // currently still reading a header
                        // decide how much to read: not more than remaining message size, not more than remaining header size
                        int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.CurrentHeaderBytesRead, dataLength - currentByte);
                        Array.Copy(data, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
                        currentByte += lengthToRead;
                        state.CurrentHeaderBytesRead += lengthToRead;
                        if (state.CurrentHeaderBytesRead == MESSAGE_HEADER_LENGTH)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.CurrentHeaderBuffer, 0);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;

                            // read type of next message
                            state.CurrentMessageType = state.CurrentHeaderBuffer[MESSAGE_SIZE_LENGTH];
                            state.IsHeaderIncomplete = false;
                            state.IsMessageIncomplete = true;
                        }
                        else
                        {
                            // We did not read the whole header yet
                            state.IsHeaderIncomplete = true;
                        }
                    }
                    else
                    {
                        // start reading a new message
                        // 1. check if remaining data sufficient to read message header
                        if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(data, currentByte);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;
                            currentByte += MESSAGE_SIZE_LENGTH;

                            // 3. read type of next message
                            state.CurrentMessageType = data[currentByte];
                            currentByte += MESSAGE_TYPE_LENGTH;

                            // 4. read data
                            // decide how much to read: not more than remaining message size, not more than remaining data size
                            int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, dataLength - currentByte);
                            Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                            currentByte += lengthToRead; // increase "current byte pointer"
                            state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                            // 4. decide how to proceed
                            if (state.CurrentMessageBytesRead == messageSize)
                            {
                                // Message is completed
                                state.IsMessageIncomplete = false;
                                messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                            }
                            else
                            {
                                // We did not read the whole message yet
                                state.IsMessageIncomplete = true;
                            }
                        }
                        else
                        {
                            // not enough data to read complete header for new message
                            state.CurrentHeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
                            int lengthToRead = dataLength - currentByte;
                            Array.Copy(data, currentByte, state.CurrentHeaderBuffer, 0, lengthToRead); // read header data into header buffer
                            currentByte += lengthToRead;
                            state.CurrentHeaderBytesRead = lengthToRead;
                            state.IsHeaderIncomplete = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing network data: " + e.Message);
            }
        }

        private void Dispatch(IPEndPoint sender, byte[] data)
        {
            var clients = new List<Socket>(Server.Clients);
            foreach (var client in clients)
            {
                if (sender.Address.ToString().Equals(((IPEndPoint)client.RemoteEndPoint).Address.ToString()))
                {
                    continue;
                }
                else
                {
                    Server.SendToClient(client, data);
                }
            }
        }

        private void OnClientDisconnected(object sender, Socket socket)
        {
            Debug.Log("Client disconnected");
        }

        private void OnClientConnected(object sender, Socket socket)
        {
            Debug.Log("Client connected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            clientConnectionQueue.Enqueue(socket);
        }

        private void OnDestroy()
        {
            StopListening();
            StopServer();
        }

#if UNITY_WSA && !UNITY_EDITOR
        private void CollectNetworkInfo()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();

        IEnumerable<HostName> hostnames =
            NetworkInformation.GetHostNames().Where(h =>
                h.IPInformation != null &&
                h.IPInformation.NetworkAdapter != null &&
                h.Type == HostNameType.Ipv4).ToList();

        var hostName = (from h in hostnames
                      where h.IPInformation.NetworkAdapter.NetworkAdapterId == profile.NetworkAdapter.NetworkAdapterId
                      select h).FirstOrDefault();
        byte? prefixLength = hostName.IPInformation.PrefixLength;
        IPAddress ip = IPAddress.Parse(hostName.RawName);
        byte[] ipBytes = ip.GetAddressBytes();
        uint mask = ~(uint.MaxValue >> prefixLength.Value);
        byte[] maskBytes = BitConverter.GetBytes(mask);

        byte[] broadcastIPBytes = new byte[ipBytes.Length];

        for (int i = 0; i < ipBytes.Length; i++)
        {
            broadcastIPBytes[i] = (byte)(ipBytes[i] | ~maskBytes[ipBytes.Length - (i+1)]);
        }

        // Convert the bytes to IP addresses.
        string broadcastIP = new IPAddress(broadcastIPBytes).ToString();
        string localIP = ip.ToString();
        foreach (HostName name in NetworkInformation.GetHostNames())
        {
            if (name.Type == HostNameType.DomainName)
            {
                serverName = name.DisplayName;
                break;
            }
        }
        broadcastIPs.Clear();
        broadcastIPs[broadcastIP] = localIP;
    }
#else

        private void CollectNetworkInfo()
        {
            serverName = Environment.ExpandEnvironmentVariables("%ComputerName%");
            broadcastIPs.Clear();

            // 1. get ipv4 addresses
            var IPs = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            // 2. get net mask for local ip
            // get valid interfaces
            var Interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up &&
                (intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

            // find interface with matching ipv4 and get the net mask
            IEnumerable<UnicastIPAddressInformation> NetMasks = null;
            foreach (var Interface in Interfaces)
            {
                NetMasks = from inf in Interface.GetIPProperties().UnicastAddresses
                           from IP in IPs
                           where inf.Address.Equals(IP)
                           select inf;
                if (NetMasks != null)
                {
                    IPAddress NetMask = NetMasks.FirstOrDefault().IPv4Mask;
                    IPAddress IP = NetMasks.FirstOrDefault().Address;
                    byte[] MaskBytes = NetMask.GetAddressBytes();
                    byte[] IPBytes = IP.GetAddressBytes();
                    for (int i = 0; i < IPBytes.Length; i++)
                    {
                        IPBytes[i] = (byte)(IPBytes[i] | ~MaskBytes[i]);
                    }

                    string localIP = IP.ToString();
                    string broadcastIP = new IPAddress(IPBytes).ToString();
                    broadcastIPs[broadcastIP] = localIP;
                }
            }
        }

#endif
    }

    /// <summary>
    /// Helper class used to store the current state of a network endpoint.
    /// </summary>
    internal class EndPointState
    {
        public byte[] CurrentMessageBuffer;
        public int CurrentMessageBytesRead;
        public byte CurrentMessageType;
        public bool IsMessageIncomplete = false;
        public IPEndPoint CurrentSender;
        public bool IsHeaderIncomplete = false;
        public byte[] CurrentHeaderBuffer;
        public int CurrentHeaderBytesRead;
    }
}