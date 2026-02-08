
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using invaders.Shared;
using invaders.Client;

namespace invaders.Client.WPF
{
    public partial class MainWindow : Window
    {
        private BitmapSource[] healthBarFrames;
        private DateTime lastMoveTime = DateTime.MinValue;
        private DateTime lastShootTime = DateTime.MinValue;
        private const int MOVE_DELAY_MS = 80;
        private const int SHOOT_DELAY_MS = 10;
        private GameClient client;
        private DispatcherTimer gameTimer;
        private int lastFrameRendered = -1;

        private HashSet<Key> pressedKeys = new HashSet<Key>();

        private double cellWidth;
        private double cellHeight;

        private Dictionary<int, Image> enemyImages = new Dictionary<int, Image>();
        private Dictionary<int, Image> projectileImages = new Dictionary<int, Image>();
        private Dictionary<int, Image> playerImages = new Dictionary<int, Image>();

        private BitmapImage player1Sprite;
        private BitmapImage player2Sprite;
        private BitmapImage[] enemyBlockFrames;
        private BitmapImage[] enemyCircleFrames;
        private BitmapImage[] enemyShooterFrames;
        private BitmapImage bulletSprite;
        private BitmapImage broadsideSprite;
        private BitmapImage enemyProjectileSprite;
        private BitmapImage[] explosionFrames;

        private int currentEnemyFrame = 0;
        private int enemyFrameCounter = 0;
        private const int FRAMES_PER_ENEMY_ANIMATION = 15;

        private List<Explosion> activeExplosions = new List<Explosion>();
        private HashSet<int> previousEnemyIds = new HashSet<int>();

        public MainWindow()
        {
            InitializeComponent();
            LoadSprites();

            client = new GameClient();
            client.OnLog += OnClientLog;

            client.OnDisconnected += OnDisconnected;

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameTimer_Tick;


            this.Loaded += (s, e) =>
            {
                cellWidth = GameCanvas.ActualWidth / Constants.FIELD_WIDTH;
                cellHeight = GameCanvas.ActualHeight / Constants.FIELD_HEIGHT;
            };


            System.Diagnostics.Debug.WriteLine($"Canvas Size: {GameCanvas.ActualWidth}x{GameCanvas.ActualHeight}");
            System.Diagnostics.Debug.WriteLine($"Cell Size: {cellWidth}x{cellHeight}");
            System.Diagnostics.Debug.WriteLine($"FIELD: {Constants.FIELD_WIDTH}x{Constants.FIELD_HEIGHT}");
        }

        private void LoadSprites()
        {
            player1Sprite = LoadImage("pack://application:,,,/Assets/player1.png");
            player2Sprite = LoadImage("pack://application:,,,/Assets/player2.png");

            System.Diagnostics.Debug.WriteLine($"Loaded player1: {player1Sprite != null}");
            System.Diagnostics.Debug.WriteLine($"Loaded player2: {player2Sprite != null}");

            enemyBlockFrames = new BitmapImage[]
            {
        LoadImage("pack://application:,,,/Assets/enemy_block_1.png"),
        LoadImage("pack://application:,,,/Assets/enemy_block_2.png")
            };

            System.Diagnostics.Debug.WriteLine($"Loaded enemy_block: {enemyBlockFrames[0] != null}");

            enemyCircleFrames = new BitmapImage[]
            {
        LoadImage("pack://application:,,,/Assets/enemy_circle_1.png"),
        LoadImage("pack://application:,,,/Assets/enemy_circle_2.png")
            };

            enemyShooterFrames = new BitmapImage[]
            {
        LoadImage("pack://application:,,,/Assets/enemy_shooter_1.png"),
        LoadImage("pack://application:,,,/Assets/enemy_shooter_2.png")
            };

            bulletSprite = LoadImage("pack://application:,,,/Assets/projectile_bullet.png");
            broadsideSprite = LoadImage("pack://application:,,,/Assets/projectile_broadside.png");
            enemyProjectileSprite = LoadImage("pack://application:,,,/Assets/projectile_enemy.png");

            explosionFrames = new BitmapImage[]
            {
        LoadImage("pack://application:,,,/Assets/explosion_1.png"),
        LoadImage("pack://application:,,,/Assets/explosion_2.png"),
            };

            LoadHealthBar();
        }

        private BitmapImage LoadImage(string uri)
        {
            try
            {
                return new BitmapImage(new Uri(uri));
            }
            catch
            {
                return null;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = ServerIpTextBox.Text.Trim();
            string playerName = PlayerNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(serverIp)) serverIp = "127.0.0.1";
            if (string.IsNullOrEmpty(playerName)) playerName = "Player";

            ConnectionStatusText.Text = "Connecting...";
            ConnectButton.IsEnabled = false;

            System.Threading.Tasks.Task.Run(() =>
            {
                bool success = client.Connect(serverIp, playerName);

                Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        ConnectionOverlay.Visibility = Visibility.Collapsed;
                        WaitingOverlay.Visibility = Visibility.Visible;
                        WaitingInfoText.Text = $"Connected as Player {client.PlayerNumber} ({client.PlayerType})";

                        if (client.PlayerNumber == 1)
                        {
                            ControlsText.Text = "Controls: W/A/S/D = Move | SPACE = Shoot | ESC = Quit";
                        }
                        else
                        {
                            ControlsText.Text = "Controls: Arrow Keys = Move | ENTER = Shoot | ESC = Quit";
                        }

                        gameTimer.Start();
                    }
                    else
                    {
                        ConnectionStatusText.Text = "Failed to connect. Is the server running?";
                        ConnectButton.IsEnabled = true;
                    }
                });
            });
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            var state = client.CurrentState;
            if (state == null) return;

            if (cellWidth == 0 || cellHeight == 0)
            {
                cellWidth = GameCanvas.ActualWidth / Constants.FIELD_WIDTH;
                cellHeight = GameCanvas.ActualHeight / Constants.FIELD_HEIGHT;
            }

            enemyFrameCounter++;
            if (enemyFrameCounter >= FRAMES_PER_ENEMY_ANIMATION)
            {
                enemyFrameCounter = 0;
                currentEnemyFrame = (currentEnemyFrame + 1) % 2;
            }

            UpdateExplosions();
            SendInput();

            if (state.GameOver && GameOverOverlay.Visibility != Visibility.Visible)
            {
                ShowGameOver(state);
                return;
            }

            if (state.GameStarted && !state.GameOver)
            {
                WaitingOverlay.Visibility = Visibility.Collapsed;
                GameOverOverlay.Visibility = Visibility.Collapsed;
                RenderGame(state);
            }

            StatusText.Text = $"Frame: {state.FrameNumber} | Enemies: {state.Enemies.Count} | Projectiles: {state.Projectiles.Count}";
        }
        private void SendInput()
        {
            PlayerInput input = PlayerInput.NONE;
            DateTime now = DateTime.Now;

            bool canMove = (now - lastMoveTime).TotalMilliseconds >= MOVE_DELAY_MS;
            bool canShoot = (now - lastShootTime).TotalMilliseconds >= SHOOT_DELAY_MS;

            if (client.PlayerNumber == 1)
            {
                if (canMove)
                {
                    if (pressedKeys.Contains(Key.W)) input |= PlayerInput.MOVE_UP;
                    if (pressedKeys.Contains(Key.S)) input |= PlayerInput.MOVE_DOWN;
                    if (pressedKeys.Contains(Key.A)) input |= PlayerInput.MOVE_LEFT;
                    if (pressedKeys.Contains(Key.D)) input |= PlayerInput.MOVE_RIGHT;
                }
                if (canShoot && pressedKeys.Contains(Key.Space)) input |= PlayerInput.SHOOT;
            }
            else
            {
                if (canMove)
                {
                    if (pressedKeys.Contains(Key.Up)) input |= PlayerInput.MOVE_UP;
                    if (pressedKeys.Contains(Key.Down)) input |= PlayerInput.MOVE_DOWN;
                    if (pressedKeys.Contains(Key.Left)) input |= PlayerInput.MOVE_LEFT;
                    if (pressedKeys.Contains(Key.Right)) input |= PlayerInput.MOVE_RIGHT;
                }
                if (canShoot && pressedKeys.Contains(Key.Enter)) input |= PlayerInput.SHOOT;
            }

            if (input != PlayerInput.NONE)
            {
                if (input.HasFlag(PlayerInput.MOVE_UP) || input.HasFlag(PlayerInput.MOVE_DOWN) ||
                    input.HasFlag(PlayerInput.MOVE_LEFT) || input.HasFlag(PlayerInput.MOVE_RIGHT))
                {
                    lastMoveTime = now;
                }
                if (input.HasFlag(PlayerInput.SHOOT))
                {
                    lastShootTime = now;
                }

                client.SendInput(input);
            }
        }

        private void RenderGame(GameState state)
        {
            if (state.FrameNumber == lastFrameRendered) return;
            lastFrameRendered = state.FrameNumber;


            if (cellWidth == 0 || cellHeight == 0)
            {
                cellWidth = GameCanvas.ActualWidth / Constants.FIELD_WIDTH;
                cellHeight = GameCanvas.ActualHeight / Constants.FIELD_HEIGHT;
                System.Diagnostics.Debug.WriteLine($"Canvas Size: {GameCanvas.ActualWidth}x{GameCanvas.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"Cell Size: {cellWidth}x{cellHeight}");
                System.Diagnostics.Debug.WriteLine($"FIELD: {Constants.FIELD_WIDTH}x{Constants.FIELD_HEIGHT}");
            }

            HashSet<int> activeEnemies = new HashSet<int>();
            HashSet<int> activeProjectiles = new HashSet<int>();
            HashSet<int> activePlayers = new HashSet<int>();

            foreach (var enemy in state.Enemies)
            {
                activeEnemies.Add(enemy.Id);
            }
            
            foreach (int oldId in previousEnemyIds)
            {
                if (!activeEnemies.Contains(oldId))
                {
                    if (enemyImages.TryGetValue(oldId, out Image oldImage))
                    {
                        double x = Canvas.GetLeft(oldImage);
                        double y = Canvas.GetTop(oldImage);
                        CreateExplosion(x, y);
                    }
                }
            }

            previousEnemyIds.Clear();
            foreach (int id in activeEnemies)
            {
                previousEnemyIds.Add(id);
            }

            UpdatePlayerStats(state);

            foreach (var enemy in state.Enemies)
            {
                RenderEnemy(enemy);
            }

            foreach (var proj in state.Projectiles)
            {
                activeProjectiles.Add(proj.Id);
                RenderProjectile(proj);
            }

            foreach (var player in state.Players)
            {
                activePlayers.Add(player.PlayerNumber);
                RenderPlayer(player);
            }

            RemoveOldImages(enemyImages, activeEnemies);
            RemoveOldImages(projectileImages, activeProjectiles);
            RemoveOldImages(playerImages, activePlayers);
        }

        private void CreateExplosion(double x, double y)
        {
            Image explosionImage = new Image();
            explosionImage.Source = explosionFrames[0];
            explosionImage.Width = cellWidth;
            explosionImage.Height = cellHeight;
            Canvas.SetLeft(explosionImage, x);
            Canvas.SetTop(explosionImage, y);
            GameCanvas.Children.Add(explosionImage);

            activeExplosions.Add(new Explosion
            {
                Image = explosionImage,
                CurrentFrame = 0,
                FrameCounter = 0,
                FramesPerExplosionFrame = 4
            });
        }

        private void UpdateExplosions()
        {
            List<Explosion> toRemove = new List<Explosion>();

            foreach (var explosion in activeExplosions)
            {
                explosion.FrameCounter++;

                if (explosion.FrameCounter >= explosion.FramesPerExplosionFrame)
                {
                    explosion.FrameCounter = 0;
                    explosion.CurrentFrame++;

                    if (explosion.CurrentFrame >= explosionFrames.Length)
                    {
                        GameCanvas.Children.Remove(explosion.Image);
                        toRemove.Add(explosion);
                    }
                    else
                    {
                        explosion.Image.Source = explosionFrames[explosion.CurrentFrame];
                    }
                }
            }

            foreach (var explosion in toRemove)
            {
                activeExplosions.Remove(explosion);
            }
        }


        private void LoadHealthBar()
        {
            try
            {
                BitmapImage fullHealthBar = LoadImage("pack://application:,,,/Assets/health_bar.png");

                if (fullHealthBar == null)
                {
                    System.Diagnostics.Debug.WriteLine("Health bar not loaded!");
                    return;
                }

                

                healthBarFrames = new BitmapSource[6];
                int frameHeight = fullHealthBar.PixelHeight / 6;

                for (int i = 0; i < 6; i++)
                {
                    var croppedBitmap = new CroppedBitmap(
                        fullHealthBar,
                        new System.Windows.Int32Rect(0, i * frameHeight, fullHealthBar.PixelWidth, frameHeight)
                    );

                    healthBarFrames[i] = croppedBitmap;
                }

                System.Diagnostics.Debug.WriteLine($"Health bar loaded: {healthBarFrames.Length} frames");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading health bar: {ex.Message}");
            }
        }

        private BitmapSource GetHealthBarFrame(int lives)
        {
            if (healthBarFrames == null || lives < 0 || lives >= healthBarFrames.Length)
                return null;

            return healthBarFrames[lives];
        }
        private void UpdatePlayerStats(GameState state)
        {
            PlayerStatsPanel.Children.Clear();

            foreach (var player in state.Players)
            {
                bool isLocal = player.PlayerNumber == client.PlayerNumber;

                var border = new Border
                {
                    Background = isLocal
                        ? new SolidColorBrush(Color.FromRgb(0, 80, 40))
                        : new SolidColorBrush(Color.FromRgb(20, 60, 30)),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(15, 5, 15, 5),
                    Margin = new Thickness(10, 0, 10, 0),
                    BorderBrush = isLocal
                        ? new SolidColorBrush(Color.FromRgb(0, 255, 100))
                        : new SolidColorBrush(Color.FromRgb(100, 255, 150)),
                    BorderThickness = new Thickness(2)
                };
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 255, 100),
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.5
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                string typeSymbol = player.Type == PlayerType.BULLETPLAYER ? "^" : "<>";

                stack.Children.Add(new TextBlock
                {
                    Text = $"P{player.PlayerNumber} {player.Name}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100)),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                if (healthBarFrames != null)
                {
                    var healthBarImage = new Image
                    {
                        Width = 100,
                        Height = 24,
                        Margin = new Thickness(0, 0, 15, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    int healthFrame = Math.Clamp(5 - player.Lives, 0, 5);
                    healthBarImage.Source = GetHealthBarFrame(healthFrame);
                    RenderOptions.SetBitmapScalingMode(healthBarImage, BitmapScalingMode.NearestNeighbor);

                    stack.Children.Add(healthBarImage);
                }
                else
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"♥ {player.Lives}",
                        Foreground = Brushes.Red,
                        Margin = new Thickness(0, 0, 15, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                stack.Children.Add(new TextBlock
                {
                    Text = $"★ {player.Points}",
                    Foreground = Brushes.Yellow,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text = $"({typeSymbol})",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 255, 150)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                border.Child = stack;
                PlayerStatsPanel.Children.Add(border);
            }
        }

        private void RenderEnemy(EnemyData enemy)
        {
            if (!enemyImages.TryGetValue(enemy.Id, out Image image))
            {
                image = new Image();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                enemyImages[enemy.Id] = image;
                GameCanvas.Children.Add(image);
            }

        
            image.Width = cellWidth * 1.8;
            image.Height = cellHeight * 1.8;

            switch (enemy.Type)
            {
                case EnemyType.BLOCK:
                    if (enemyBlockFrames != null && enemyBlockFrames[currentEnemyFrame] != null)
                        image.Source = enemyBlockFrames[currentEnemyFrame];
                    break;
                case EnemyType.CIRCLE:
                    if (enemyCircleFrames != null && enemyCircleFrames[currentEnemyFrame] != null)
                        image.Source = enemyCircleFrames[currentEnemyFrame];
                    break;
                case EnemyType.SHOOTER:
                    if (enemyShooterFrames != null && enemyShooterFrames[currentEnemyFrame] != null)
                        image.Source = enemyShooterFrames[currentEnemyFrame];
                    break;
            }

   
            double offsetX = (1.8 - 1.0) / 2.0; 
            double offsetY = (1.8 - 1.0) / 2.0;

            double x = enemy.X * cellWidth - cellWidth * offsetX;
            double y = enemy.Y * cellHeight - cellHeight * offsetY;

            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, y);
        }
        private void RenderProjectile(ProjectileData proj)
        {
            if (!projectileImages.TryGetValue(proj.Id, out Image image))
            {
                image = new Image();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                switch (proj.Type)
                {
                    case ProjectileType.BULLET:
                        if (bulletSprite != null) image.Source = bulletSprite;
                        break;
                    case ProjectileType.BROADSIDEL:
                    case ProjectileType.BROADSIDER:
                        if (broadsideSprite != null) image.Source = broadsideSprite;
                        break;
                    case ProjectileType.ENEMY:
                        if (enemyProjectileSprite != null) image.Source = enemyProjectileSprite;
                        break;
                }

                projectileImages[proj.Id] = image;
                GameCanvas.Children.Add(image);
            }

    
            image.Width = cellWidth * 1.5;
            image.Height = cellHeight * 1.5;

            double offsetX = (1.5 - 1.0) / 2.0;
            double offsetY = (1.5 - 1.0) / 2.0;

            Canvas.SetLeft(image, proj.X * cellWidth - cellWidth * offsetX);
            Canvas.SetTop(image, proj.Y * cellHeight - cellHeight * offsetY);
        }

        private void RenderPlayer(PlayerData player)
        {
            if (!playerImages.TryGetValue(player.PlayerNumber, out Image image))
            {
                image = new Image();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                if (player.PlayerNumber == 1)
                {
                    if (player1Sprite != null) image.Source = player1Sprite;
                }
                else
                {
                    if (player2Sprite != null) image.Source = player2Sprite;
                }

                playerImages[player.PlayerNumber] = image;
                GameCanvas.Children.Add(image);
            }

          
            image.Width = cellWidth * 1.8;
            image.Height = cellHeight * 1.8;

            double offsetX = (1.8 - 1.0) / 2.0;
            double offsetY = (1.8 - 1.0) / 2.0;

            Canvas.SetLeft(image, player.X * cellWidth - cellWidth * offsetX);
            Canvas.SetTop(image, player.Y * cellHeight - cellHeight * offsetY);
        }

        private void RemoveOldImages(Dictionary<int, Image> images, HashSet<int> activeIds)
        {
            var toRemove = new List<int>();

            foreach (var kvp in images)
            {
                if (!activeIds.Contains(kvp.Key))
                {
                    GameCanvas.Children.Remove(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                images.Remove(id);
            }
        }

        private void ShowGameOver(GameState state)
        {
            GameOverOverlay.Visibility = Visibility.Visible;
            GameOverReasonText.Text = state.GameOverReason;

            RankingsPanel.Children.Clear();

            int rank = 1;
            foreach (var player in state.Rankings)
            {
                bool isLocal = player.PlayerNumber == client.PlayerNumber;

                var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

                var rankText = new TextBlock
                {
                    Text = $"#{rank}" + (isLocal ? " *" : ""),
                    Foreground = isLocal ? Brushes.Lime : Brushes.White,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(rankText, 0);
                grid.Children.Add(rankText);

                var nameText = new TextBlock
                {
                    Text = player.Name,
                    Foreground = isLocal ? Brushes.White : Brushes.White
                };
                Grid.SetColumn(nameText, 1);
                grid.Children.Add(nameText);

                var pointsText = new TextBlock
                {
                    Text = $"★ {player.Points}",
                    Foreground = Brushes.Yellow
                };
                Grid.SetColumn(pointsText, 2);
                grid.Children.Add(pointsText);

                var livesText = new TextBlock
                {
                    Text = $"♥ {player.Lives}",
                    Foreground = Brushes.Red
                };
                Grid.SetColumn(livesText, 3);
                grid.Children.Add(livesText);

                RankingsPanel.Children.Add(grid);
                rank++;
            }
        }

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            GameCanvas.Children.Clear();
            enemyImages.Clear();
            projectileImages.Clear();
            playerImages.Clear();
            activeExplosions.Clear();
            previousEnemyIds.Clear();
            lastFrameRendered = -1;

            GameOverOverlay.Visibility = Visibility.Collapsed;
            WaitingOverlay.Visibility = Visibility.Visible;
            WaitingInfoText.Text = "Waiting for game to start...";

            client.SendResetRequest();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.Key);

            if (e.Key == Key.Escape)
            {
                client.Disconnect();
                Close();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.Key);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gameTimer.Stop();
            client.Disconnect();
        }

        private void GameCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            cellWidth = GameCanvas.ActualWidth / Constants.FIELD_WIDTH;
            cellHeight = GameCanvas.ActualHeight / Constants.FIELD_HEIGHT;

            lastFrameRendered = -1;
        }

        private void OnClientLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine(message);
            });
        }

        private void OnDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                gameTimer.Stop();
                StatusText.Text = "Disconnected from server";
            });
        }
    }

    public class Explosion
    {
        public Image Image { get; set; }
        public int CurrentFrame { get; set; }
        public int FrameCounter { get; set; }
        public int FramesPerExplosionFrame { get; set; }
    }
}