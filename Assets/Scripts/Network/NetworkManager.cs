// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network.Messages;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.Unity.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance = null;

        public string AnnounceMessage = "TTD";

        [Tooltip("When started as a client, should automatically connect to the first server found.")]
        public bool AutomaticallyConnectToServer = true;

        public NetworkTransport Network;

        public int Port = 11338;

        [HideInInspector]
        public Dictionary<string, SessionInfo> Sessions = new Dictionary<string, SessionInfo>();

        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> MessageHandlers;

        /// <summary>
        /// Event raised when connected or disconnected.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the list of sessions changes.
        /// </summary>
        public event EventHandler<EventArgs> SessionListChanged;

        public int ClientCounter { get; private set; } = 0;

        public bool IsConnected { get; private set; }

        public bool IsServer { get; set; }

        public SessionInfo Session { get; private set; }

        public async Task HandleNetworkMessageAsync(MessageContainer message)
        {
            if (MessageHandlers != null)
            {
                Func<MessageContainer, Task> callback;
                if (MessageHandlers.TryGetValue(message.Type, out callback) && callback != null)
                {
                    await callback(message);
                }
                else
                {
                    Debug.Log("Unknown message: " + message.Type.ToString() + " with content: " + message.Payload);
                }
            }
        }

        public void HandleNewClient(Socket client)
        {
            if (!IsServer || Network == null) return;

            // assign id to client
            var ClientMessage = new MessageAcceptClient(ClientCounter++);
            Network.SendToClient(ClientMessage.Pack(), client);

            // do your own session handling...
        }

        public void JoinSession(SessionInfo session)
        {
            SessionInfo sessionInfo;
            if (Sessions.TryGetValue(session.SessionIp, out sessionInfo) == true)
            {
                if (Network != null)
                {
                    IsServer = false;
                    Network.StopServer();
                    Network.StopListening();
                    Network.ConnectToServer(session.SessionIp, session.SessionPort);
                    Session = session;
                }
            }
        }

        public void OnConnectedToServer()
        {
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            Network.Pause();
        }

        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler)
        {
            try
            {
                MessageHandlers[messageType] = messageHandler;
            }
            catch (Exception exp)
            {
                Debug.LogError("Registering message handler failed! Original error message: " + exp.Message);
                return false;
            }
            return true;
        }

        public void SendMessage(MessageContainer command)
        {
            if (IsServer)
            {
                Network.SendToAll(command);
            }
            else if (IsConnected)
            {
                Network.SendToServer(command);
            }
        }

        public void SendMessage(IMessage message)
        {
            SendMessage(message.Pack());
        }

        public bool StartAsServer()
        {
            if (!enabled)
            {
                Debug.Log("Network Manager disabled, cannot start server!");
                return false;
            }

            if (!Network || Network.enabled == false)
            {
                Debug.Log("Network transport not ready, cannot start server!");
                return false;
            }

            Debug.Log("Starting as server");
            bool Success = Network.StartServer(Port, AnnounceMessage);
            if (Success)
            {
                Network.StopListening();
                IsServer = true;
                ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
            }
            return Success;
        }

        public void Unpause()
        {
            Network.Unpause();
        }

        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType)
        {
            return MessageHandlers.Remove(messageType);
        }

        private void Awake()
        {
            MessageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();

            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        private Task OnBroadcastData(MessageContainer obj)
        {
            Debug.Log("Received broadcast!");
            MessageAnnouncement Message = MessageAnnouncement.Unpack(obj); // deserialize message
            if (Message != null && Message.Message.Equals(AnnounceMessage)) // check if the announcement strings matches
            {
                SessionInfo sessionInfo;
                if (Sessions.TryGetValue(Message.IP, out sessionInfo) == false)
                {
                    // add to session list
                    sessionInfo = new SessionInfo() { SessionName = Message.Name, SessionIp = Message.IP, SessionPort = Message.Port };
                    Sessions.Add(Message.IP, sessionInfo);
                    // trigger event to notify about new session
                    SessionListChanged?.Invoke(this, EventArgs.Empty);
                    if (AutomaticallyConnectToServer == true)
                    {
                        JoinSession(sessionInfo);
                    }
                }
            }
            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            // registers callback for announcement handling
            RegisterMessageHandler(MessageContainer.MessageType.ANNOUNCEMENT, OnBroadcastData);

            if (AutomaticallyConnectToServer == true)
            {
                StartAsClient();
            }
        }

        public bool StartAsClient()
        {
            if (!enabled)
            {
                Debug.Log("Network Manager disabled, cannot start client!");
                return false;
            }

            if (!Network || Network.enabled == false)
            {
                Debug.Log("Network transport not ready, cannot start client!");
                return false;
            }

            Debug.Log("Starting as client");
            IsServer = false;
            bool Success = Network.StartListening();
            return Success;
        }

        public class SessionInfo
        {
            public string SessionIp;
            public string SessionName;
            public int SessionPort;
        }
    }
}