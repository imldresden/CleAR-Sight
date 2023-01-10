// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;

namespace IMLD.Unity.Network.Messages
{
    public class MessageVolumePickup : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.VOLUME_PICKUP;

        public int id;
        public MessageVolumePickup(int pickupId)
        {
            id = pickupId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageVolumePickup Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageVolumePickup>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
