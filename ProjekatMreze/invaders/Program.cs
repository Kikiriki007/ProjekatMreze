using System;
using System.Threading;
using invaders.Shared;
namespace invaders.Server
{
    class Program
    {
        static GameServer server;
        static bool showLogs = true;
        static void Main(string[] args)
        {
            Console.Title = "Space Invaders - SERVER";
            try
            {
                Console.WindowWidth = 80;
                Console.WindowHeight = 30;
            }
            catch { }
            Console.Clear();
            PrintBanner();
            server = new GameServer();
            server.OnLog += HandleLog;
            server.Start();
            Console.WriteLine("\nServer Controls:");
            Console.WriteLine("  [S] - Start game (when players connected)");
            Console.WriteLine("  [L] - Toggle log display");
            Console.WriteLine("  [Q] - Quit server");
            Console.WriteLine();
            while (true)
            {
                Console.Write($"\rPlayers: {server.PlayerCount}/{Constants.MAX_PLAYERS} | Game: {(server.IsGameRunning ? "RUNNING" : "WAITING")} | Press key for command... ");
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    switch (char.ToUpper(key.KeyChar))
                    {
                        case 'S':
                            if (server.PlayerCount > 0 && !server.IsGameRunning)
                            {
                                Console.WriteLine("\n\n>>> STARTING GAME <<<\n");
                                server.StartGame();
                            }
                            else if (server.PlayerCount == 0)
                            {
                                Console.WriteLine("\n[!] No players connected yet!");
                            }
                            else
                            {
                                Console.WriteLine("\n[!] Game already running!");
                            }
                            break;
                        case 'L':
                            showLogs = !showLogs;
                            Console.WriteLine($"\n[i] Logs: {(showLogs ? "ON" : "OFF")}");
                            break;
                        case 'Q':
                            Console.WriteLine("\n\nShutting down server...");
                            server.Stop();
                            return;
                    }
                }
                Thread.Sleep(100);
            }
        }
        static void HandleLog(string message)
        {
            if (showLogs)
            {
                Console.WriteLine($"\n{DateTime.Now:HH:mm:ss} {message}");
            }
        }
        static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
-------------------------------------------------------------------
|                    SPACE INVADERS SERVER                        |
-------------------------------------------------------------------
|  TCP Port: " + Constants.TCP_PORT.ToString().PadRight(10) + @"  (Player Login)                           |
|  UDP Port: " + Constants.UDP_PORT.ToString().PadRight(10) + @"  (Game Data)                              |
-------------------------------------------------------------------
");
            Console.ResetColor();
        }
    }
}