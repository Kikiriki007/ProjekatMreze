using System;
using System.Collections.Generic;
using invaders.Shared;
//using Microsoft.VisualBasic;

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

        public int Lives { get; private set; }

        public Enemy(int x, int y, EnemyType type)
        {
            Id = nextId++;
            X = x;
            Y = y;
            Type = type;
            IsActive = true;

            switch(type)
            {
                case EnemyType.BLOCK: 
                    PointValue = Constants.POINTS_BLOCK;
                    Lives = Constants.LIVES_BLOCK;
                    break;
                case EnemyType.CIRCLE:
                    PointValue = Constants.POINTS_CIRCLE;
                    Lives = Constants.LIVES_CIRCLE;
                    break;
                case EnemyType.SHOOTER:
                    PointValue = Constants.POINTS_SHOOTER;
                    Lives = Constants.LIVES_SHOOTER;
                    break;
            }
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

        public bool Destroy()
        {
            if (Lives <= 0)
            IsActive = false;
            return IsActive;
        }

        public void DamageEnemy(PlayerType pt)
        {
            switch (pt)
            {
                case PlayerType.BULLETPLAYER:
                    Lives -= Constants.BULLET_DAMAGE;
                    break;
                case PlayerType.BROADSIDEPLAYER:
                    Lives -= Constants.BROADSIDE_DAMAGE;
                    break;
            }
        
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
                Type = this.Type,
                Lives = this.Lives
            };
        }

        public static List<Enemy> GenerateFormation() //------------------------- pokusaj da napravis ucitavanje levela iz fajla 
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
            return $"Enemy[{Id}] {Type} at ({X},{Y}) - Active: {IsActive} (Lives: {Lives})";
        }
    }
}