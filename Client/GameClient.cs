using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using invaders.Shared;

namespace invaders.Client
{
    public class GameClient
    {
        private Socket tcpSocket;
        private Socket udpSocket;
        private IPEndPoint serverUdpEndpoint;

        public int PlayerNumber { get; private set; }
        public string PlayerName { get; private set; }
        public PlayerType PlayerType { get; private set; }

        public GameState CurrentState { get; private set; }
        public bool IsConnected { get; private set; }
        public bool GameStarted => CurrentState?.GameStarted ?? false;
        public bool GameOver => CurrentState?.GameOver ?? false;

        private Thread receiveThread;
        private bool isRunning;

        public event Action<string> OnLog;
        public event Action<GameState> OnStateReceived;
        public event Action OnDisconnected;

        public GameClient()
        {
            CurrentState = new GameState();
            IsConnected = false;
        }

        public bool Connect(string serverIp, string playerName)
        {
            byte[] buffer = new byte[8192];

            try
            {
                PlayerName = playerName;

                Log($"Connecting to {serverIp}:{Constants.TCP_PORT}...");


                tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverTcpEP = new IPEndPoint(IPAddress.Parse(serverIp), Constants.TCP_PORT);


                tcpSocket.Connect(serverTcpEP);
                Log("TCP connected");


                LoginRequest request = new LoginRequest(playerName);
                byte[] requestData = NetworkProtocol.Serialize(request);

                int bytesSent = tcpSocket.Send(requestData);
                Log($"Login request sent ({bytesSent} bytes)");


                tcpSocket.ReceiveTimeout = 10000;
                int bytesReceived = tcpSocket.Receive(buffer);

                if (bytesReceived == 0)
                {
                    Log("No response from server");
                    Disconnect();
                    return false;
                }

                byte[] responseData = new byte[bytesReceived];
                Array.Copy(buffer, responseData, bytesReceived);

                LoginResponse response = NetworkProtocol.Deserialize<LoginResponse>(responseData);

                if (!response.Success)
                {
                    Log($"Login failed: {response.Message}");
                    Disconnect();
                    return false;
                }

                PlayerNumber = response.AssignedPlayerNumber;
                PlayerType = response.AssignedType;
                Log($"Login successful! Player {PlayerNumber}, Type: {PlayerType}");
                Log(response.Message);


                serverUdpEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), response.UdpPort);
                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


                IPEndPoint localUdpEP = new IPEndPoint(IPAddress.Any, 0);
                udpSocket.Bind(localUdpEP);
                udpSocket.ReceiveTimeout = 100;

                isRunning = true;
                IsConnected = true;

                receiveThread = new Thread(() => ReceiveLoop(buffer)) { IsBackground = true };
                receiveThread.Start();


                SendInput(PlayerInput.NONE);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        public void SendResetRequest()
        {
            if (!IsConnected || udpSocket == null) return;

            try
            {
                ResetRequest request = new ResetRequest(PlayerNumber);
                byte[] data = NetworkProtocol.Serialize(request);


                udpSocket.SendTo(data, serverUdpEndpoint);
            }
            catch (Exception ex)
            {
                Log($"Send reset error: {ex.Message}");
            }
        }

        public void SendInput(PlayerInput input)
        {
            if (!IsConnected || udpSocket == null) return;

            try
            {
                InputPacket packet = new InputPacket(
                    PlayerNumber,
                    input,
                    CurrentState?.FrameNumber ?? 0
                );

                byte[] data = NetworkProtocol.Serialize(packet);


                udpSocket.SendTo(data, serverUdpEndpoint);
            }
            catch (Exception ex)
            {
                Log($"Send error: {ex.Message}");
            }
        }

        private void ReceiveLoop(byte[] buffer)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                try
                {
                    if (udpSocket.Available > 0)
                    {

                        int bytesReceived = udpSocket.ReceiveFrom(buffer, ref remoteEP);

                        byte[] data = new byte[bytesReceived];
                        Array.Copy(buffer, data, bytesReceived);

                        if (NetworkProtocol.IsValidPacket(data))
                        {
                            GameState state = NetworkProtocol.Deserialize<GameState>(data);
                            CurrentState = state;
                            OnStateReceived?.Invoke(state);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode != SocketError.TimedOut && se.SocketErrorCode != SocketError.WouldBlock)
                    {
                        if (isRunning)
                            Log($"Receive error: {se.Message}");
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Log($"Receive error: {ex.Message}");
                }
            }
        }

        public void Disconnect()
        {
            isRunning = false;
            IsConnected = false;


            Thread.Sleep(50);

            try { tcpSocket?.Shutdown(SocketShutdown.Both); } catch { }
            try { tcpSocket?.Close(); } catch { }
            try { udpSocket?.Close(); } catch { }

            OnDisconnected?.Invoke();
            Log("Disconnected from server");
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[Client] {message}");
        }
    }
}