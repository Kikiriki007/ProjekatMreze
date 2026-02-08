using System;
using System.Collections.Generic;

namespace invaders.Shared
{
    [Serializable]
    public class GameState
    {
        public int FrameNumber { get; set; }


        public List<PlayerData> Players { get; set; }

        public List<EnemyData> Enemies { get; set; }

        public List<ProjectileData> Projectiles { get; set; }


        public bool GameStarted { get; set; }
        public bool GameOver { get; set; }
        public string GameOverReason { get; set; }

        public List<PlayerData> Rankings { get; set; }

        public GameState()
        {
            Players = new List<PlayerData>();
            Enemies = new List<EnemyData>();
            Projectiles = new List<ProjectileData>();
            Rankings = new List<PlayerData>();
            GameStarted = false;
            GameOver = false;
            GameOverReason = "";
        }

        public GameState Clone()
        {
            var clone = new GameState
            {
                FrameNumber = this.FrameNumber,
                GameStarted = this.GameStarted,
                GameOver = this.GameOver,
                GameOverReason = this.GameOverReason
            };

            foreach (var p in Players)
            {
                clone.Players.Add(new PlayerData
                {
                    PlayerNumber = p.PlayerNumber,
                    Name = p.Name,
                    X = p.X,
                    Y = p.Y,
                    Lives = p.Lives,
                    Points = p.Points,
                    Type = p.Type,
                    IsAlive = p.IsAlive
                });
            }

            foreach (var e in Enemies)
            {
                clone.Enemies.Add(new EnemyData
                {
                    Id = e.Id,
                    X = e.X,
                    Y = e.Y,
                    Type = e.Type
                });
            }

            foreach (var proj in Projectiles)
            {
                clone.Projectiles.Add(new ProjectileData
                {
                    Id = proj.Id,
                    X = proj.X,
                    Y = proj.Y,
                    Type = proj.Type,
                    OwnerPlayerNumber = proj.OwnerPlayerNumber
                });
            }

            foreach (var r in Rankings)
            {
                clone.Rankings.Add(new PlayerData
                {
                    PlayerNumber = r.PlayerNumber,
                    Name = r.Name,
                    Points = r.Points,
                    Lives = r.Lives
                });
            }

            return clone;
        }

        public override string ToString()
        {
            return $"Frame {FrameNumber}: {Players.Count} players, {Enemies.Count} enemies, {Projectiles.Count} projectiles";
        }
    }

    [Serializable]
    public class InputPacket
    {
        public int PlayerNumber { get; set; }
        public PlayerInput Input { get; set; }
        public int FrameNumber { get; set; }

        public InputPacket()
        {
            Input = PlayerInput.NONE;
        }

        public InputPacket(int playerNumber, PlayerInput input, int frame)
        {
            PlayerNumber = playerNumber;
            Input = input;
            FrameNumber = frame;
        }

        public override string ToString()
        {
            return $"P{PlayerNumber} Input: {Input} (Frame {FrameNumber})";
        }
    }
    [Serializable]
    public class ResetRequest
    {
        public int PlayerNumber { get; set; }

        public ResetRequest() { }
        public ResetRequest(int playerNumber) { PlayerNumber = playerNumber; }
    }

    [Serializable]
    public class LoginRequest
    {
        public string PlayerName { get; set; }

        public LoginRequest() { PlayerName = "Unknown"; }
        public LoginRequest(string name) { PlayerName = name; }
    }

    [Serializable]
    public class LoginResponse
    {
        public bool Success { get; set; }
        public int AssignedPlayerNumber { get; set; }
        public PlayerType AssignedType { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public string Message { get; set; }
        public int UdpPort { get; set; }

        public LoginResponse()
        {
            Success = false;
            Message = "";
            UdpPort = Constants.UDP_PORT;
        }
    }
}