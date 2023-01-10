// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IMLD.Unity.Network
{
    public interface IMessage
    {
        public MessageContainer Pack();
    }
}
