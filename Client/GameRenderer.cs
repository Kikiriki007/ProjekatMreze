using System;
using System.Text;
using invaders.Shared;

namespace invaders.Client
{
    public class GameRenderer
    {
        private string[,] screen;
        private StringBuilder outputBuffer;
        private int lastFrameRendered = -1;

        public GameRenderer()
        {
            screen = new string[Constants.FIELD_HEIGHT, Constants.FIELD_WIDTH];
            outputBuffer = new StringBuilder();
        }

        public void Render(GameState state, int localPlayerNumber)
        {
            if (state == null) return;

            if (state.FrameNumber == lastFrameRendered) return;
            lastFrameRendered = state.FrameNumber;

            ClearScreen();

            foreach (var enemy in state.Enemies)
            {
                if (IsInBounds(enemy.X, enemy.Y))
                {
                    screen[enemy.Y, enemy.X] = enemy.GetDisplayChar();
                }
            }

            foreach (var proj in state.Projectiles)
            {
                if (IsInBounds(proj.X, proj.Y))
                {
                    screen[proj.Y, proj.X] = proj.GetDisplayChar();
                }
            }

            foreach (var player in state.Players)
            {
                if (IsInBounds(player.X, player.Y))
                {
                    screen[player.Y, player.X] = player.GetDisplayChar();
                }
            }

            outputBuffer.Clear();

            outputBuffer.AppendLine("---------------------------------------------------------------");
            outputBuffer.AppendLine("|                    SPACE INVADERS                           |");
            outputBuffer.AppendLine("---------------------------------------------------------------");

            foreach (var player in state.Players)
            {
                string marker = player.PlayerNumber == localPlayerNumber ? ">>>" : "   ";
                string typeStr = player.Type == PlayerType.BULLETPLAYER ? "^" : "<>";
                outputBuffer.AppendLine($"| {marker} P{player.PlayerNumber} {player.Name,-12} Lives: {player.Lives}  Points: {player.Points,-6} ({typeStr})  |");
            }

            outputBuffer.AppendLine("---------------------------------------------------------------");

            for (int y = 0; y < Constants.FIELD_HEIGHT; y++)
            {
                outputBuffer.Append("|");
                for (int x = 0; x < Constants.FIELD_WIDTH; x++)
                {
                    outputBuffer.Append(screen[y, x]);
                }
                outputBuffer.AppendLine("|");
            }

            outputBuffer.AppendLine("---------------------------------------------------------------");

            if (localPlayerNumber == 1)
            {
                outputBuffer.AppendLine("| Controls: W/A/S/D = Move  |  SPACE = Shoot  |  ESC = Quit   |");
            }
            else
            {
                outputBuffer.AppendLine("| Controls: Arrows = Move  |  ENTER = Shoot  |  ESC = Quit    |");
            }

            outputBuffer.AppendLine($"| Frame: {state.FrameNumber,-8}                                          |");
            outputBuffer.AppendLine("---------------------------------------------------------------");

            Console.SetCursorPosition(0, 0);
            Console.Write(outputBuffer.ToString());
        }

        public void RenderWaiting(int playerNumber, string playerName, PlayerType type)
        {
            Console.Clear();
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("|                    SPACE INVADERS                           |");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"|  Connected as: Player {playerNumber} - {playerName,-20}              |");
            Console.WriteLine($"|  Type: {type,-30}                       |");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("|                                                             |");
            Console.WriteLine("|              Waiting for game to start...                   |");
            Console.WriteLine("|                                                             |");
            Console.WriteLine("|      Server admin needs to press [S] to start game          |");
            Console.WriteLine("|                                                             |");
            Console.WriteLine("---------------------------------------------------------------");
        }

        public void RenderGameOver(GameState state, int localPlayerNumber)
        {
            Console.Clear();
            Console.WriteLine("\n");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("|                       GAME OVER                             |");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"|  {state.GameOverReason,-50}         |");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("|                      RANKINGS                               |");
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("| RANK  |      PLAYER       |   POINTS   |   LIVES   |  TYPE  |");
            Console.WriteLine("---------------------------------------------------------------");

            int rank = 1;
            foreach (var player in state.Rankings)
            {
                string marker = player.PlayerNumber == localPlayerNumber ? " *" : "  ";
                string typeStr = player.Type == PlayerType.BULLETPLAYER ? "  ^  " : " <> ";
                Console.WriteLine($"|  {rank}{marker}  | {player.Name,-17} | {player.Points,10} | {player.Lives,9} | {typeStr}  |");
                rank++;
            }

            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("\n  * = You");
            Console.WriteLine("\n  Press R to restart or any other key to exit...");
        }

        private void ClearScreen()
        {
            for (int y = 0; y < Constants.FIELD_HEIGHT; y++)
            {
                for (int x = 0; x < Constants.FIELD_WIDTH; x++)
                {
                    screen[y, x] = " . ";
                }
            }
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Constants.FIELD_WIDTH && y >= 0 && y < Constants.FIELD_HEIGHT;
        }
    }
}