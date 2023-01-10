// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace IMLD.Unity.Network
{
    public static class SocketExtensions
    {
        public static void Kill(this Socket socket)
        {
#if NETFX_CORE
            socket.Dispose();
#else
            socket.Close();
#endif
        }
    }
}
