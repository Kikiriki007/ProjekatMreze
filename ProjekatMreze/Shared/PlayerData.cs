using System;

namespace invaders.Shared
{
    [Serializable]
    public class PlayerData
    {
        public int PlayerNumber { get; set; }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Lives { get; set; }
        public int Points { get; set; }
        public PlayerType Type { get; set; }
        public bool IsAlive { get; set; }

        public PlayerData()
        {
            Name = "Unknown";
            Lives = Constants.STARTING_LIVES;
            IsAlive = true;
        }

        public PlayerData(int playerNumber, string name, int x, int y, PlayerType type)
        {
            PlayerNumber = playerNumber;
            Name = name;
            X = x;
            Y = y;
            Type = type;
            Lives = Constants.STARTING_LIVES;
            Points = 0;
            IsAlive = true;
        }

        public string GetDisplayChar()
        {
            return PlayerNumber == 1 ? " A " : " B ";
        }

        public override string ToString()
        {
            return $"P{PlayerNumber} {Name} ({X},{Y}) Lives:{Lives} Points:{Points}";
        }
    }
}