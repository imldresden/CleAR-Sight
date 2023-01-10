// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageWorldAnchor
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.WORLD_ANCHOR;

        public byte[] AnchorData;

        public MessageWorldAnchor(byte[] anchorData)
        {
            AnchorData = anchorData;
        }

        public MessageContainer Pack()
        {
            return new MessageContainer(Type, AnchorData);
        }

        public static MessageWorldAnchor Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            return new MessageWorldAnchor(container.Payload);
        }
    }
}