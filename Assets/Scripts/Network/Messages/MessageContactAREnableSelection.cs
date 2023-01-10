// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageContactAREnableSelection : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.CONTACT_SELECTION_ENABLE;

        public bool selection;
        public int modelID;

        public MessageContactAREnableSelection(bool select, int ID = 5)
        {
            selection = select;
            modelID = ID;
        }
        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageContactAREnableSelection Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageContactAREnableSelection>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
