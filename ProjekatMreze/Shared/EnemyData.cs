using System;

namespace invaders.Shared
{

    [Serializable]
    public class EnemyData
    {
        public int Id { get; set; }          
        public int X { get; set; }
        public int Y { get; set; }
        public EnemyType Type { get; set; }

        public int Lives { get; set; } // adding lives

        public EnemyData() { }

        public EnemyData(int id, int x, int y, EnemyType type,int lives)
        {
            Id = id;
            X = x;
            Y = y;
            Type = type;
            Lives = lives;
        }

        public string GetDisplayChar()
        {
            return Type switch
            {
                EnemyType.BLOCK => "[ ]",
                EnemyType.CIRCLE => " O ",
                EnemyType.SHOOTER => "{X}",
                _ => "[?]"
            };
        }

        public int GetPointValue()
        {
            return Type switch
            {
                EnemyType.BLOCK => Constants.POINTS_BLOCK,
                EnemyType.CIRCLE => Constants.POINTS_CIRCLE,
                EnemyType.SHOOTER => Constants.POINTS_SHOOTER,
                _ => 10
            };
        }

        public override string ToString()
        {
            return $"Enemy[{Id}] {Type} at ({X},{Y})";
        }
    }
}