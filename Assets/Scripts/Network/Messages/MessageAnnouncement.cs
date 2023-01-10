// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;

namespace IMLD.Unity.Network.Messages
{
    public class MessageAnnouncement
    {
        public int Port;
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOUNCEMENT;
        public string IP;
        public string Name;
        public string Message;

        public MessageAnnouncement(string broadcastmessage, string ip, string name, int port)
        {
            IP = ip;
            Port = port;
            Name = name;
            Message = broadcastmessage;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAnnouncement Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAnnouncement>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
