// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;

namespace IMLD.Unity.Network.Messages
{
    public class MessageBookmarkDelete : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.BOOKMARK_DELETE;

        public int index;

        public MessageBookmarkDelete(int bookmarkIndex)
        {
            index = bookmarkIndex;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageBookmarkDelete Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageBookmarkDelete>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
