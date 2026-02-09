
using System;
using System.Net;
using invaders.Shared;
using Microsoft.VisualBasic;
using Constants = invaders.Shared.Constants;

namespace invaders.Server
{
    public class Player
    {
        public int PlayerNumber { get; private set; }
        public string Name { get; set; }
        public PlayerType Type { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int PreviousX { get; private set; }
        public int PreviousY { get; private set; }

        public int Lives { get; private set; }
        public int Points { get; private set; }
        public bool IsAlive => Lives > 0;

        public IPEndPoint UdpEndPoint { get; set; }
        public bool IsConnected { get; set; }

        private int shootCooldown = 0;
        private const int SHOOT_COOLDOWN_FRAMES = 1;

        public Player(int playerNumber, string name, int totalPlayers)
        {
            PlayerNumber = playerNumber;
            Name = name;
            Lives = Constants.STARTING_LIVES;
            Points = 0;
            IsConnected = true;
            Type = playerNumber == 1 ? PlayerType.BULLETPLAYER : PlayerType.BROADSIDEPLAYER;

            if (totalPlayers == 1)
            {
                X = Constants.FIELD_WIDTH / 2;
                Y = Constants.FIELD_HEIGHT - 1;
            }
            else if (playerNumber == 1)
            {
                X = (Constants.FIELD_WIDTH / 2) - 2;
                Y = Constants.FIELD_HEIGHT - 1;
            }
            else
            {
                X = (Constants.FIELD_WIDTH / 2) + 2;
                Y = Constants.FIELD_HEIGHT - 1;
            }

            PreviousX = X;
            PreviousY = Y;
        }

        public void ProcessInput(PlayerInput input)
        {
            PreviousX = X;
            PreviousY = Y;

            if (input.HasFlag(PlayerInput.MOVE_UP))
                MoveBy(0, -1);
            if (input.HasFlag(PlayerInput.MOVE_DOWN))
                MoveBy(0, 1);
            if (input.HasFlag(PlayerInput.MOVE_LEFT))
                MoveBy(-1, 0);
            if (input.HasFlag(PlayerInput.MOVE_RIGHT))
                MoveBy(1, 0);

            if (shootCooldown > 0) shootCooldown--;
        }

        private void MoveBy(int dx, int dy)
        {
            X = Math.Clamp(X + dx, 0, Constants.FIELD_WIDTH - 1);
            Y = Math.Clamp(Y + dy, 0, Constants.FIELD_HEIGHT - 1);
        }

        public bool CanShoot()
        {
            return shootCooldown == 0;
        }

        public void DidShoot()
        {
            shootCooldown = SHOOT_COOLDOWN_FRAMES;
        }

        public void TakeDamage(int amount = 1)
        {
            Lives = Math.Max(0, Lives - amount);
        }

        public void AddPoints(int amount)
        {
            Points += amount;
        }

        public void ResolveCollision(Player other)
        {
            if (other == null || (X != other.X || Y != other.Y)) return;

            int moveDirectionX = X - PreviousX;

            if (moveDirectionX != 0)
            {
                X = Math.Clamp(PreviousX - (moveDirectionX * 2), 0, Constants.FIELD_WIDTH - 1);
            }
            else
            {
                int pushDir = PlayerNumber == 1 ? -2 : 2;
                X = Math.Clamp(X + pushDir, 0, Constants.FIELD_WIDTH - 1);
            }
        }

        public PlayerData ToPlayerData()
        {
            return new PlayerData
            {
                PlayerNumber = this.PlayerNumber,
                Name = this.Name,
                X = this.X,
                Y = this.Y,
                Lives = this.Lives,
                Points = this.Points,
                Type = this.Type,
                IsAlive = this.IsAlive
            };
        }

        public override string ToString()
        {
            return $"Player {PlayerNumber} ({Name}) at ({X},{Y}) - Lives: {Lives}, Points: {Points}";
        }
    }
}