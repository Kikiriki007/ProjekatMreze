
using System;
using System.Collections.Generic;
using System.Linq;
using invaders.Shared;

namespace invaders.Server
{
    public class GameEngine
    {
        private List<Player> players;
        private List<Enemy> enemies;
        private List<Projectile> projectiles;

        public int FrameNumber { get; private set; }
        public bool GameStarted { get; private set; }
        public bool GameOver { get; private set; }
        public string GameOverReason { get; private set; }

        private int enemyMoveCounter = 0;
        private int enemyDirection = 1;

        public event Action<string> OnLog;
        public event Action<GameState> OnStateChanged;

        public GameEngine()
        {
            players = new List<Player>();
            enemies = new List<Enemy>();
            projectiles = new List<Projectile>();
            FrameNumber = 0;
            GameStarted = false;
            GameOver = false;
            GameOverReason = "";
        }

        public Player AddPlayer(string name)
        {
            if (players.Count >= Constants.MAX_PLAYERS)
            {
                Log($"Cannot add player {name}: max players reached");
                return null;
            }

            int playerNumber = players.Count + 1;
            int totalPlayers = players.Count + 1;

            if (playerNumber == 2 && players.Count == 1)
            {
                var p1 = players[0];
                players[0] = new Player(1, p1.Name, 2);
                players[0].UdpEndPoint = p1.UdpEndPoint;
            }

            var player = new Player(playerNumber, name, Math.Max(totalPlayers, players.Count + 1));
            players.Add(player);

            Log($"Player {playerNumber} '{name}' joined - Type: {player.Type}");
            return player;
        }

        public Player GetPlayer(int playerNumber)
        {
            return players.FirstOrDefault(p => p.PlayerNumber == playerNumber);
        }

        public void StartGame()
        {
            if (GameStarted) return;

            GameStarted = true;
            GameOver = false;
            FrameNumber = 0;

            enemies.Clear();
            projectiles.Clear();

            enemies.AddRange(Enemy.GenerateFormation());

            Log($"Game started with {players.Count} player(s) and {enemies.Count} enemies");
        }

        public void Update()
        {
            if (!GameStarted || GameOver) return;

            FrameNumber++;

            enemyMoveCounter++;

            if (enemyMoveCounter >= Constants.ENEMY_MOVE_INTERVAL)
            {
                enemyMoveCounter = 0;
                MoveEnemies();
            }

            EnemiesShoot();
            MoveProjectiles();
            CheckCollisions();
            CheckPlayerCollisions();
            CheckGameOver();
            CleanupEntities();

            OnStateChanged?.Invoke(GetGameState());
        }

        public void ProcessPlayerInput(int playerNumber, PlayerInput input)
        {
            var player = GetPlayer(playerNumber);
            if (player == null || !player.IsAlive) return;

            player.ProcessInput(input);

            if (input.HasFlag(PlayerInput.SHOOT) && player.CanShoot())
            {
                var newProjectiles = Projectile.CreateForPlayer(player);
                projectiles.AddRange(newProjectiles);
                player.DidShoot();
            }
        }

        private void MoveEnemies()
        {
            var activeEnemies = enemies.Where(e => e.IsActive).ToList();
            if (activeEnemies.Count == 0) return;

            int leftmost = activeEnemies.Min(e => e.X);
            int rightmost = activeEnemies.Max(e => e.X);

            bool hitEdge = (enemyDirection > 0 && rightmost >= Constants.FIELD_WIDTH - 1) ||
                           (enemyDirection < 0 && leftmost <= 0);

            if (hitEdge)
            {
                foreach (var enemy in activeEnemies)
                {
                    enemy.MoveDown(1);
                }
                enemyDirection *= -1;
            }
            else
            {
                foreach (var enemy in activeEnemies)
                {
                    enemy.MoveHorizontal(enemyDirection);
                }
            }
        }

        private void EnemiesShoot()
        {
            foreach (var enemy in enemies.Where(e => e.IsActive))
            {
                var projectile = enemy.TryShoot();
                if (projectile != null)
                {
                    projectiles.Add(projectile);
                }
            }
        }

        private void MoveProjectiles()
        {
            foreach (var projectile in projectiles)
            {
                projectile.Move();
            }
        }

        private void CheckCollisions()
        {
            foreach (var projectile in projectiles.ToList())
            {
                if (projectile.ShouldRemove) continue;
                if (projectile.Type == ProjectileType.ENEMY) continue;

                foreach (var enemy in enemies)
                {
                    if (!enemy.IsActive) continue;

                    if (enemy.CollidesAt(projectile.X, projectile.Y))
                    {
                        var shooter = GetPlayer(projectile.OwnerPlayerNumber);
                        if (shooter != null)
                        {
                            shooter.AddPoints(enemy.PointValue);
                        }

                        enemy.Destroy();
                        projectile.MarkForRemoval();
                        break;
                    }
                }
            }

            foreach (var projectile in projectiles.ToList())
            {
                if (projectile.ShouldRemove) continue;
                if (projectile.Type != ProjectileType.ENEMY) continue;

                foreach (var player in players)
                {
                    if (!player.IsAlive) continue;

                    if (projectile.CollidesAt(player.X, player.Y))
                    {
                        player.TakeDamage();
                        projectile.MarkForRemoval();
                        Log($"Player {player.PlayerNumber} hit! Lives: {player.Lives}");
                        break;
                    }
                }
            }

            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive) continue;

                foreach (var player in players)
                {
                    if (!player.IsAlive) continue;

                    if (enemy.CollidesAt(player.X, player.Y))
                    {
                        player.TakeDamage();
                        enemy.Destroy();
                        Log($"Player {player.PlayerNumber} collided with enemy! Lives: {player.Lives}");
                        break;
                    }
                }
            }
        }

        private void CheckPlayerCollisions()
        {
            if (players.Count < 2) return;

            var p1 = players[0];
            var p2 = players[1];

            if (p1.X == p2.X && p1.Y == p2.Y)
            {
                p1.ResolveCollision(p2);
                p2.ResolveCollision(p1);
            }
        }

        private void CheckGameOver()
        {
            bool allDead = players.All(p => !p.IsAlive);
            if (allDead)
            {
                GameOver = true;
                GameOverReason = "All players eliminated!";
                Log(GameOverReason);
                return;
            }

            var winner = players.FirstOrDefault(p => p.Points >= Constants.WIN_POINTS);
            if (winner != null)
            {
                GameOver = true;
                GameOverReason = $"{winner.Name} wins with {winner.Points} points!";
                Log(GameOverReason);
                return;
            }

            if (enemies.All(e => !e.IsActive))
            {
                GameOver = true;
                var topPlayer = players.OrderByDescending(p => p.Points).First();
                GameOverReason = $"Victory! {topPlayer.Name} wins with {topPlayer.Points} points!";
                Log(GameOverReason);
                return;
            }

            var lowestEnemy = enemies.Where(e => e.IsActive).OrderByDescending(e => e.Y).FirstOrDefault();
            if (lowestEnemy != null && lowestEnemy.Y >= Constants.FIELD_HEIGHT - 2)
            {
                GameOver = true;
                GameOverReason = "Enemies reached Earth! Game Over!";
                Log(GameOverReason);
                return;
            }
        }

        private void CleanupEntities()
        {
            enemies.RemoveAll(e => !e.IsActive);
            projectiles.RemoveAll(p => p.ShouldRemove);
        }

        public GameState GetGameState()
        {
            var state = new GameState
            {
                FrameNumber = this.FrameNumber,
                GameStarted = this.GameStarted,
                GameOver = this.GameOver,
                GameOverReason = this.GameOverReason
            };

            foreach (var player in players)
            {
                state.Players.Add(player.ToPlayerData());
            }

            foreach (var enemy in enemies)
            {
                if (enemy.IsActive)
                {
                    state.Enemies.Add(enemy.ToEnemyData());
                }
            }

            foreach (var projectile in projectiles)
            {
                if (!projectile.ShouldRemove)
                {
                    state.Projectiles.Add(projectile.ToProjectileData());
                }
            }

            if (GameOver)
            {
                state.Rankings = players
                    .OrderByDescending(p => p.Points)
                    .ThenByDescending(p => p.Lives)
                    .Select(p => p.ToPlayerData())
                    .ToList();
            }

            return state;
        }

        public void Reset()
        {
            enemies.Clear();
            projectiles.Clear();
            FrameNumber = 0;
            GameStarted = false;
            GameOver = false;
            GameOverReason = "";
            enemyMoveCounter = 0;
            enemyDirection = 1;

            foreach (var player in players.ToList())
            {
                var newPlayer = new Player(player.PlayerNumber, player.Name, players.Count);
                newPlayer.UdpEndPoint = player.UdpEndPoint;
                players[player.PlayerNumber - 1] = newPlayer;
            }

            Log("Game reset");
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[Engine] {message}");
        }

        public int PlayerCount => players.Count;
        public int EnemyCount => enemies.Count;
        public int ProjectileCount => projectiles.Count;
    }
}