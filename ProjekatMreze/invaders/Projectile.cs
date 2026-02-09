using System;
using System.Collections.Generic;
using invaders.Shared;

namespace invaders.Server
{

    public class Projectile
    {
        private static int nextId = 1;

        public int Id { get; private set; }
        public ProjectileType Type { get; private set; }
        public int OwnerPlayerNumber { get; private set; } // 0 = enemy

        public int X { get; private set; }
        public int Y { get; private set; }

        public bool ShouldRemove { get; private set; }

        public Projectile(int x, int y, ProjectileType type, int owner)
        {
            Id = nextId++;
            X = x;
            Y = y;
            Type = type;
            OwnerPlayerNumber = owner;
            ShouldRemove = false;

            if (X < 0 || X >= Constants.FIELD_WIDTH || Y < 0 || Y >= Constants.FIELD_HEIGHT)
            {
                ShouldRemove = true;
            }
        }

        public void Move()
        {
            if (ShouldRemove) return;

            switch (Type)
            {
                case ProjectileType.BULLET:
                    Y--;
                    if (Y < 0) ShouldRemove = true;
                    break;

                case ProjectileType.BROADSIDEL:
                    X--;
                    if (X < 0) ShouldRemove = true;
                    break;

                case ProjectileType.BROADSIDER:
                    X++;
                    if (X >= Constants.FIELD_WIDTH) ShouldRemove = true;
                    break;

                case ProjectileType.ENEMY:
                    Y++;
                    if (Y >= Constants.FIELD_HEIGHT) ShouldRemove = true;
                    break;
            }
        }

        public bool CollidesAt(int x, int y)
        {
            return !ShouldRemove && X == x && Y == y;
        }


        public void MarkForRemoval()
        {
            ShouldRemove = true;
        }

        public ProjectileData ToProjectileData()
        {
            return new ProjectileData
            {
                Id = this.Id,
                X = this.X,
                Y = this.Y,
                Type = this.Type,
                OwnerPlayerNumber = this.OwnerPlayerNumber
            };
        }

        public static List<Projectile> CreateForPlayer(Player player)
        {
            var projectiles = new List<Projectile>();

            switch (player.Type)
            {
                case PlayerType.BULLETPLAYER:
                    if (player.Y > 0)
                    {
                        projectiles.Add(new Projectile(
                            player.X,
                            player.Y - 1,
                            ProjectileType.BULLET,
                            player.PlayerNumber
                        ));
                    }
                    break;

                case PlayerType.BROADSIDEPLAYER:

                    if (player.X > 0)
                    {
                        projectiles.Add(new Projectile(
                            player.X - 1,
                            player.Y,
                            ProjectileType.BROADSIDEL,
                            player.PlayerNumber
                        ));
                    }
                    if (player.X < Constants.FIELD_WIDTH - 1)
                    {
                        projectiles.Add(new Projectile(
                            player.X + 1,
                            player.Y,
                            ProjectileType.BROADSIDER,
                            player.PlayerNumber
                        ));
                    }
                    break;
            }

            return projectiles;
        }

        public override string ToString()
        {
            return $"Projectile[{Id}] {Type} at ({X},{Y}) from P{OwnerPlayerNumber}";
        }
    }
}