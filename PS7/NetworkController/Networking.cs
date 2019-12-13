using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace NetworkUtil
{

    public static class Networking
    {
        /////////////////////////////////////////////////////////////////////////////////////////
        // Server-Side Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
        /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
        /// AcceptNewClient will continue the event-loop.
        /// </summary>
        /// <param name="toCall">The method to call when a new connection is made</param>
        /// <param name="port">The the port to listen on</param>
        public static TcpListener StartServer(Action<SocketState> toCall, int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            listener.Start();

            Tuple<Action<SocketState>, TcpListener> tcpInfo = new Tuple<Action<SocketState>, TcpListener>(toCall, listener);

            listener.BeginAcceptSocket(AcceptNewClient, tcpInfo);

            return listener;
        }

        /// <summary>
        /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
        /// continues an event-loop to accept additional clients.
        ///
        /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
        /// OnNetworkAction should be set to the delegate that was passed to StartServer.
        /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
        /// 
        /// If anything goes wrong during the connection process (such as the server being stopped externally), 
        /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccured flag set to true 
        /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
        /// an error occurs.
        ///
        /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
        /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
        /// </summary>
        /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
        /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
        private static void AcceptNewClient(IAsyncResult ar)
        {
            Tuple<Action<SocketState>, TcpListener> tuple = (Tuple<Action<SocketState>, TcpListener>)ar.AsyncState;

            TcpListener listener = tuple.Item2;
            Action<SocketState> toCall = tuple.Item1;

            try
            {
                SocketState socketState = new SocketState(toCall, listener.EndAcceptSocket(ar));
                socketState.OnNetworkAction(socketState);

                Tuple<Action<SocketState>, TcpListener> tcpInfo = new Tuple<Action<SocketState>, TcpListener>(toCall, listener);
                listener.BeginAcceptSocket(AcceptNewClient, tcpInfo);
            }
            catch
            {
                SocketState errorSocketState = new SocketState(toCall, null);
                errorSocketState.ErrorOccured = true;
                errorSocketState.ErrorMessage = "Error occurred while accepting a new client";

                errorSocketState.OnNetworkAction(errorSocketState);
            }
        }

        /// <summary>
        /// Stops the given TcpListener.
        /// </summary>
        public static void StopServer(TcpListener listener)
        {
            listener.Stop();
        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Client-Side Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins the asynchronous process of connecting to a server via BeginConnect, 
        /// and using ConnectedCallback as the method to finalize the connection once it's made.
        /// 
        /// If anything goes wrong during the connection process, toCall should be invoked 
        /// with a new SocketState with its ErrorOccured flag set to true and an appropriate message 
        /// placed in its ErrorMessage field. Between this method and ConnectedCallback, toCall should 
        /// only be invoked once on error.
        ///
        /// This connection process should timeout and produce an error (as discussed above) 
        /// if a connection can't be established within 3 seconds of starting BeginConnect.
        /// 
        /// </summary>
        /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
        /// <param name="hostName">The server to connect to</param>
        /// <param name="port">The port on which the server is listening</param>
        public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
        {
            // Establish the remote endpoint for the socket.
            IPHostEntry ipHostInfo;
            IPAddress ipAddress = IPAddress.None;

            // Determine if the server address is a URL or an IP
            try
            {
                ipHostInfo = Dns.GetHostEntry(hostName);
                bool foundIPV4 = false;
                foreach (IPAddress addr in ipHostInfo.AddressList)
                    if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        foundIPV4 = true;
                        ipAddress = addr;
                        break;
                    }
                // Didn't find any IPV4 addresses
                if (!foundIPV4)
                {
                    SocketState errorSocketState = new SocketState(toCall, null);
                    errorSocketState.ErrorOccured = true;
                    errorSocketState.ErrorMessage = "Couldn't find any IPV4 addresses";

                    errorSocketState.OnNetworkAction(errorSocketState);
                    return;
                }
            }
            catch (Exception)
            {
                // See if host name is a valid ipaddress
                try
                {
                    ipAddress = IPAddress.Parse(hostName);
                }
                catch (Exception)
                {
                    SocketState errorSocketState = new SocketState(toCall, null);
                    errorSocketState.ErrorOccured = true;
                    errorSocketState.ErrorMessage = "Host name isn't a valid ipaddress";

                    errorSocketState.OnNetworkAction(errorSocketState);
                }
            }

            // Create a TCP/IP socket.
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // This disables Nagle's algorithm (google if curious!)
            // Nagle's algorithm can cause problems for a latency-sensitive 
            // game like ours will be 
            socket.NoDelay = true;

            try
            {
                SocketState TheServer = new SocketState(toCall, socket);
                IAsyncResult result = TheServer.TheSocket.BeginConnect(ipAddress, port, ConnectedCallback, TheServer);
                bool success = result.AsyncWaitHandle.WaitOne(3000, true);

                if (!TheServer.TheSocket.Connected)
                {
                    TheServer.TheSocket.Close();
                }
            }
            catch
            {
                SocketState errorSocketState = new SocketState(toCall, null);
                errorSocketState.ErrorOccured = true;
                errorSocketState.ErrorMessage = "An error occured while connecting to server";

                errorSocketState.OnNetworkAction(errorSocketState);
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
        ///
        /// Uses EndConnect to finalize the connection.
        /// 
        /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
        /// either this method or ConnectToServer (not both) should indicate the error appropriately.
        /// 
        /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
        /// with a new SocketState representing the new connection.
        /// 
        /// </summary>
        /// <param name="ar">The object asynchronously passed via BeginConnect</param>
        private static void ConnectedCallback(IAsyncResult ar)
        {
            SocketState theServer = (SocketState)ar.AsyncState;

            try
            {
                theServer.TheSocket.EndConnect(ar);

                SocketState socketState = new SocketState(theServer.OnNetworkAction, theServer.TheSocket);
                theServer.OnNetworkAction(socketState);
            }
            catch
            {
                theServer.ErrorOccured = true;
                theServer.ErrorMessage = "An error occured during the connection callback";

                theServer.OnNetworkAction(theServer);
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////
        // Server and Client Common Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
        /// as the callback to finalize the receive and store data once it has arrived.
        /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
        /// 
        /// If anything goes wrong during the receive process, the SocketState's ErrorOccured flag should 
        /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
        /// OnNetworkAction should be invoked. Between this method and ReceiveCallback, OnNetworkAction should only be 
        /// invoked once on error.
        /// 
        /// </summary>
        /// <param name="state">The SocketState to begin receiving</param>
        public static void GetData(SocketState state)
        {
            try
            {
                state.TheSocket.BeginReceive(state.buffer, 0, state.buffer.Length,
                  SocketFlags.None, ReceiveCallback, state);
            }

            catch
            {
                state.ErrorOccured = true;
                state.ErrorMessage = "Error occurred while starting the receive process";

                state.OnNetworkAction(state);
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
        /// 
        /// Uses EndReceive to finalize the receive.
        ///
        /// As stated in the GetData documentation, if an error occurs during the receive process,
        /// either this method or GetData (not both) should indicate the error appropriately.
        /// 
        /// If data is successfully received:
        ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
        ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
        ///      string builder.
        ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
        /// </summary>
        /// <param name="ar"> 
        /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
        /// </param>
        private static void ReceiveCallback(IAsyncResult ar)
        {
            SocketState theServer = (SocketState)ar.AsyncState;
            try
            {
                int numBytes = theServer.TheSocket.EndReceive(ar);

                if (numBytes == 0)
                {
                    theServer.ErrorOccured = true;
                    theServer.ErrorMessage = "Socket was closed";

                    theServer.OnNetworkAction(theServer);
                    return;
                }

                string message = Encoding.UTF8.GetString(theServer.buffer,
                    0, numBytes);

                lock (theServer.data)
                {
                    theServer.data.Append(message);
                }

                theServer.OnNetworkAction(theServer);
            }

            catch
            {
                theServer.ErrorOccured = true;
                theServer.ErrorMessage = "Error occured during the receive process";

                theServer.OnNetworkAction(theServer);
            }
        }

        /// <summary>
        /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
        /// 
        /// If the socket is closed, does not attempt to send.
        /// 
        /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
        /// </summary>
        /// <param name="socket">The socket on which to send the data</param>
        /// <param name="data">The string to send</param>
        /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
        public static bool Send(Socket socket, string data)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(data);

            if (!socket.Connected)
                return false;

            try
            {
                socket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, SendCallback, socket);
                return true;
            }
            catch
            {
                socket.Close();
                return false;
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a send operation that was initiated by Send.
        ///
        /// Uses EndSend to finalize the send.
        /// 
        /// This method must not throw, even if an error occured during the Send operation.
        /// </summary>
        /// <param name="ar">
        /// This is the Socket (not SocketState) that is stored with the callback when
        /// the initial BeginSend is called.
        /// </param>
        private static void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;

            try
            {
                socket.EndSend(ar);
            }
            catch
            {
            }
        }


        /// <summary>
        /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
        /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
        /// 
        /// If the socket is closed, does not attempt to send.
        /// 
        /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
        /// </summary>
        /// <param name="socket">The socket on which to send the data</param>
        /// <param name="data">The string to send</param>
        /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
        public static bool SendAndClose(Socket socket, string data)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(data);

            if (!socket.Connected)
                return false;

            try
            {
                socket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, SendAndCloseCallback, socket);
                return true;
            }
            catch
            {
                socket.Close();
                return false;
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
        ///
        /// Uses EndSend to finalize the send, then closes the socket.
        /// 
        /// This method must not throw, even if an error occured during the Send operation.
        /// 
        /// This method ensures that the socket is closed before returning.
        /// </summary>
        /// <param name="ar">
        /// This is the Socket (not SocketState) that is stored with the callback when
        /// the initial BeginSend is called.
        /// </param>
        private static void SendAndCloseCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;

            try
            {
                socket.EndSend(ar);
            }
            catch
            {
            }
            finally
            {
                socket.Close();
            }
        }
    }
}
