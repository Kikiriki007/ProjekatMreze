using System;
using System.Collections.Generic;
using invaders.Shared;

namespace invaders.Server
{
    public class Enemy
    {
        private static int nextId = 1;
        private static Random random = new Random();

        public int Id { get; private set; }
        public EnemyType Type { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsActive { get; private set; }
        public int PointValue { get; private set; }

        public Enemy(int x, int y, EnemyType type)
        {
            Id = nextId++;
            X = x;
            Y = y;
            Type = type;
            IsActive = true;

            PointValue = type switch
            {
                EnemyType.BLOCK => Constants.POINTS_BLOCK,
                EnemyType.CIRCLE => Constants.POINTS_CIRCLE,
                EnemyType.SHOOTER => Constants.POINTS_SHOOTER,
                _ => 10
            };
        }

        public void MoveHorizontal(int direction)
        {
            X += direction;
        }

        public void MoveDown(int amount = 1)
        {
            Y += amount;

            if (Y >= Constants.FIELD_HEIGHT)
            {
                IsActive = false;
            }
        }

        public bool CollidesAt(int x, int y)
        {
            return IsActive && X == x && Y == y;
        }

        public void Destroy()
        {
            IsActive = false;
        }

        public Projectile TryShoot()
        {
            if (Type != EnemyType.SHOOTER || !IsActive)
                return null;

            if (random.NextDouble() < Constants.ENEMY_SHOOT_PROBABILITY)
            {
                return new Projectile(X, Y + 1, ProjectileType.ENEMY, 0);
            }

            return null;
        }

        public EnemyData ToEnemyData()
        {
            return new EnemyData
            {
                Id = this.Id,
                X = this.X,
                Y = this.Y,
                Type = this.Type
            };
        }

        public static List<Enemy> GenerateFormation()
        {
            var enemies = new List<Enemy>();

            int rows = 5;
            int cols = 9;
            int startX = 6;
            int startY = 2;

            for (int row = 0; row < rows; row++)
            {
                EnemyType type;
                if (row == 0)
                {
                    type = EnemyType.SHOOTER;
                }
                else if (row == 1 || row == 2)
                {
                    type = EnemyType.CIRCLE;
                }
                else
                {
                    type = EnemyType.BLOCK;
                }

                for (int col = 0; col < cols; col++)
                {
                    int x = startX + col * 2;
                    int y = startY + row * 2;

                    if (x < Constants.FIELD_WIDTH && y < Constants.FIELD_HEIGHT)
                    {
                        enemies.Add(new Enemy(x, y, type));
                    }
                }
            }

            return enemies;
        }

        public override string ToString()
        {
            return $"Enemy[{Id}] {Type} at ({X},{Y}) - Active: {IsActive}";
        }
    }
}