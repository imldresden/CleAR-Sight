// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;

namespace IMLD.Unity.Network.Messages
{
    public class MessageVolumeRelease : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.VOLUME_RELEASE;

        public int id;
        public MessageVolumeRelease(int pickupId)
        {
            id = pickupId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageVolumeRelease Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageVolumeRelease>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}