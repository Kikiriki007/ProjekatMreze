using System;

namespace invaders.Shared
{

    [Serializable]
    public class ProjectileData
    {
        public int Id { get; set; }              
        public int X { get; set; }
        public int Y { get; set; }
        public ProjectileType Type { get; set; }
        public int OwnerPlayerNumber { get; set; } // 0 = enemy 1 = player 1 2 = player 2

        public ProjectileData() { }

        public ProjectileData(int id, int x, int y, ProjectileType type, int owner)
        {
            Id = id;
            X = x;
            Y = y;
            Type = type;
            OwnerPlayerNumber = owner;
        }

        public string GetDisplayChar()
        {
            return Type switch
            {
                ProjectileType.BULLET => " ^ ",
                ProjectileType.BROADSIDEL => "<< ",
                ProjectileType.BROADSIDER => " >>",
                ProjectileType.ENEMY => " o ",
                _ => " ? "
            };
        }

        public override string ToString()
        {
            return $"Projectile[{Id}] {Type} at ({X},{Y}) from P{OwnerPlayerNumber}";
        }
    }
}