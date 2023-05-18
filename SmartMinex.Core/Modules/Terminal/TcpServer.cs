//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TcpServer –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System.Net;
    using System.Net.Sockets;
    #endregion Using

    class TcpServer : Socket
    {
        readonly IPEndPoint _endpoint;

        public TcpServer(int port)
            : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            _endpoint = new IPEndPoint(IPAddress.Any, port);
            Bind(_endpoint);
        }
    }
}
