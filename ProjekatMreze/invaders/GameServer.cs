using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using invaders.Shared;

namespace invaders.Server
{
    public class GameServer
    {
        private Socket tcpListenSocket;
        private Socket udpSocket;
        private List<Socket> connectedClientSockets;
        private Dictionary<int, IPEndPoint> playerEndpoints;

        private GameEngine engine;
        private bool isRunning;
        private Thread gameLoopThread;
        private Thread tcpListenerThread;
        private Thread udpReceiverThread;

        private object lockObject = new object();

        public event Action<string> OnLog;

        public GameServer()
        {
            connectedClientSockets = new List<Socket>();
            playerEndpoints = new Dictionary<int, IPEndPoint>();
            engine = new GameEngine();

            engine.OnLog += (msg) => Log(msg);
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;


            tcpListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// internetwork = ipv4
            IPEndPoint tcpEP = new IPEndPoint(IPAddress.Any, Constants.TCP_PORT);
            tcpListenSocket.Bind(tcpEP);
            tcpListenSocket.Listen(10);
            Log($"TCP socket listening on port {Constants.TCP_PORT}");


            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, Constants.UDP_PORT);
            udpSocket.Bind(udpEP);
            udpSocket.ReceiveTimeout = 100;
            Log($"UDP socket bound to port {Constants.UDP_PORT}");

            tcpListenerThread = new Thread(TcpListenerLoop) { IsBackground = true };
            tcpListenerThread.Start();

            udpReceiverThread = new Thread(UdpReceiverLoop) { IsBackground = true };
            udpReceiverThread.Start();

            Log("Server started. Waiting for players...");
        }

        public void StartGame()
        {
            engine.StartGame();

            gameLoopThread = new Thread(GameLoop) { IsBackground = true };
            gameLoopThread.Start();

            BroadcastGameState();
        }

        public void ResetGame()
        {
            lock (lockObject)//lock thread
            {
                engine.Reset();
            }
            Log("Game reset by admin");
        }

        public void Stop()
        {
            isRunning = false;

            try { tcpListenSocket?.Close(); } catch { }
            try { udpSocket?.Close(); } catch { }

            lock (connectedClientSockets)
            {
                foreach (var socket in connectedClientSockets)
                {
                    try { socket.Close(); } catch { }
                }
                connectedClientSockets.Clear();
            }

            Log("Server stopped");
        }

        private void TcpListenerLoop()
        {
            byte[] buffer = new byte[8192];

            while (isRunning)
            {
                try
                {

                    if (tcpListenSocket.Poll(100000, SelectMode.SelectRead))
                    {
                        Socket clientSocket = tcpListenSocket.Accept();
                        Log($"New TCP connection from {clientSocket.RemoteEndPoint}");

                        Thread loginThread = new Thread(() => HandleLogin(clientSocket, buffer)) { IsBackground = true };
                        loginThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Log($"TCP Error: {ex.Message}");
                }

                Thread.Sleep(10);
            }
        }

        private void HandleLogin(Socket clientSocket, byte[] buffer)
        {
            try
            {
                clientSocket.ReceiveTimeout = 5000;


                int bytesReceived = clientSocket.Receive(buffer);
                if (bytesReceived == 0)
                {
                    Log("Failed to read login request");
                    clientSocket.Close();
                    return;
                }

                byte[] data = new byte[bytesReceived];
                Array.Copy(buffer, data, bytesReceived);

                LoginRequest request = NetworkProtocol.Deserialize<LoginRequest>(data);
                Log($"Login request from: {request.PlayerName}");

                LoginResponse response = new LoginResponse();

                lock (lockObject)
                {
                    Player player = engine.AddPlayer(request.PlayerName);

                    if (player != null)
                    {
                        response.Success = true;
                        response.AssignedPlayerNumber = player.PlayerNumber;
                        response.AssignedType = player.Type;
                        response.StartX = player.X;
                        response.StartY = player.Y;
                        response.UdpPort = Constants.UDP_PORT;
                        response.Message = $"Welcome {request.PlayerName}! You are Player {player.PlayerNumber}";

                        lock (connectedClientSockets)//lock da bi se dodalo sta gde treba, tako svi lokovi
                        {
                            connectedClientSockets.Add(clientSocket);
                        }

                        Log($"Player {player.PlayerNumber} '{request.PlayerName}' logged in successfully");
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "Server full or game in progress";
                        Log($"Login rejected for {request.PlayerName}: server full");
                    }
                }


                byte[] responseData = NetworkProtocol.Serialize(response);
                int bytesSent = clientSocket.Send(responseData);

                if (bytesSent > 0)
                {
                    Log($"Login response sent ({bytesSent} bytes)");
                }

                if (!response.Success)
                {
                    clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Log($"Login error: {ex.Message}");
                try { clientSocket.Close(); } catch { }
            }
        }

        private void UdpReceiverLoop()
        {
            byte[] buffer = new byte[8192];
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
                            byte typeId = data[3];

                            if (typeId == 2)
                            {
                                InputPacket input = NetworkProtocol.Deserialize<InputPacket>(data);

                                lock (lockObject)
                                {
                                    playerEndpoints[input.PlayerNumber] = (IPEndPoint)remoteEP;

                                    var player = engine.GetPlayer(input.PlayerNumber);
                                    if (player != null)
                                    {
                                        player.UdpEndPoint = (IPEndPoint)remoteEP;
                                    }

                                    engine.ProcessPlayerInput(input.PlayerNumber, input.Input);
                                }
                            }
                            else if (typeId == 8)
                            {
                                ResetRequest reset = NetworkProtocol.Deserialize<ResetRequest>(data);
                                Log($"Reset request from Player {reset.PlayerNumber}");

                                lock (lockObject)
                                {
                                    engine.Reset();
                                    engine.StartGame();
                                }
                                Log("Game reset and restarted");
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (SocketException se)
                {

                    if (se.SocketErrorCode != SocketError.TimedOut &&
                        se.SocketErrorCode != SocketError.WouldBlock &&
                        se.SocketErrorCode != SocketError.ConnectionReset)
                    {
                        if (isRunning)
                            Log($"UDP Socket Error: {se.SocketErrorCode} - {se.Message}");
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Log($"UDP Error: {ex.Message}");
                }
            }
        }

        private void GameLoop()
        {
            Log("Game loop started");

            while (isRunning)
            {
                lock (lockObject)
                {
                    if (engine.GameStarted && !engine.GameOver)
                    {
                        engine.Update();
                    }
                }

                BroadcastGameState();

                Thread.Sleep(Constants.FRAME_DELAY_MS);
            }
        }

        private void BroadcastGameState()
        {
            try
            {
                GameState state;
                lock (lockObject)
                {
                    state = engine.GetGameState();
                }

                byte[] data = NetworkProtocol.Serialize(state);

                lock (lockObject)
                {

                    List<int> disconnectedPlayers = new List<int>();

                    foreach (var kvp in playerEndpoints)
                    {
                        try
                        {

                            udpSocket.SendTo(data, kvp.Value);
                        }
                        catch (SocketException se)
                        {

                            if (se.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                disconnectedPlayers.Add(kvp.Key);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Broadcast error: {ex.Message}");

                        }
                    }


                    foreach (int playerNum in disconnectedPlayers)
                    {
                        playerEndpoints.Remove(playerNum);
                        Log($"Player {playerNum} endpoint removed (disconnected)");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Broadcast error: {ex.Message}");
            }
        }

        public int PlayerCount => engine.PlayerCount;

        public bool IsGameRunning => engine.GameStarted && !engine.GameOver;

        private void Log(string message)
        {
            OnLog?.Invoke($"[Server] {message}");
        }
    }
}