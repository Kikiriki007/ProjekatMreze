
using System;
using System.Threading;
using invaders.Shared;

namespace invaders.Client
{
    class Program
    {
        static GameClient client;
        static GameRenderer renderer;
        static bool running = true;

        static void Main(string[] args)
        {
            Console.Title = "Space Invaders - CLIENT";

            try
            {
                Console.WindowWidth = 70;
                Console.WindowHeight = 55;
            }
            catch { }

            Console.CursorVisible = false;
            Console.Clear();

            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("                    SPACE INVADERS CLIENT                      ");
            Console.WriteLine("---------------------------------------------------------------\n");

            Console.Write("Enter server IP (press Enter for localhost): ");
            string serverIp = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(serverIp))
            {
                serverIp = Constants.SERVER_IP;
            }

            Console.Write("Enter your name: ");
            string playerName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            client = new GameClient();
            renderer = new GameRenderer();

            client.OnLog += (msg) =>
            {
                if (!client.GameStarted)
                {
                    Console.WriteLine(msg);
                }
            };

            client.OnStateReceived += OnGameStateReceived;
            client.OnDisconnected += () =>
            {
                running = false;
            };

            Console.WriteLine($"\nConnecting to {serverIp}...\n");

            if (!client.Connect(serverIp, playerName))
            {
                Console.WriteLine("\nFailed to connect. Press any key to exit.");
                Console.ReadKey(true);
                return;
            }

            renderer.RenderWaiting(client.PlayerNumber, client.PlayerName, client.PlayerType);

            Console.CursorVisible = false;

            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);

                    if (key.Key == ConsoleKey.Escape)
                    {
                        running = false;
                        break;
                    }

                    PlayerInput input = GetInputFromKey(key, client.PlayerNumber);
                    if (input != PlayerInput.NONE)
                    {
                        client.SendInput(input);
                    }
                }

                var state = client.CurrentState;

                if (state != null)
                {
                    if (state.GameOver)
                    {
                        renderer.RenderGameOver(state, client.PlayerNumber);
                        var key = Console.ReadKey(true);

                        if (key.Key == ConsoleKey.R)
                        {
                            client.SendResetRequest();
                            renderer.RenderWaiting(client.PlayerNumber, client.PlayerName, client.PlayerType);
                        }
                        else
                        {
                            running = false;
                        }
                    }
                    else if (state.GameStarted)
                    {
                        renderer.Render(state, client.PlayerNumber);
                    }
                }

                Thread.Sleep(16);
            }

            client.Disconnect();
            Console.Clear();
            Console.WriteLine("Thanks for playing!");
            Console.CursorVisible = true;
        }

        static PlayerInput GetInputFromKey(ConsoleKeyInfo key, int playerNumber)
        {
            PlayerInput input = PlayerInput.NONE;

            if (playerNumber == 1)
            {
                switch (key.Key)
                {
                    case ConsoleKey.W: input = PlayerInput.MOVE_UP; break;
                    case ConsoleKey.S: input = PlayerInput.MOVE_DOWN; break;
                    case ConsoleKey.A: input = PlayerInput.MOVE_LEFT; break;
                    case ConsoleKey.D: input = PlayerInput.MOVE_RIGHT; break;
                    case ConsoleKey.Spacebar: input = PlayerInput.SHOOT; break;
                }
            }
            else
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: input = PlayerInput.MOVE_UP; break;
                    case ConsoleKey.DownArrow: input = PlayerInput.MOVE_DOWN; break;
                    case ConsoleKey.LeftArrow: input = PlayerInput.MOVE_LEFT; break;
                    case ConsoleKey.RightArrow: input = PlayerInput.MOVE_RIGHT; break;
                    case ConsoleKey.Enter: input = PlayerInput.SHOOT; break;
                }
            }

            return input;
        }

        static void OnGameStateReceived(GameState state)
        {

        }
    }
}