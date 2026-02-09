namespace invaders.Shared
{
    public static class Constants
    {
        public const int FIELD_WIDTH = 21;
        public const int FIELD_HEIGHT = 40;

        public const int TCP_PORT = 5000;
        public const int UDP_PORT = 5001;
        public const string SERVER_IP = "127.0.0.1";

        public const int MAX_PLAYERS = 2;
        public const int STARTING_LIVES = 5;
        public const int WIN_POINTS = 1000;

        public const int FRAME_DELAY_MS = 16;//bilo 50
        public const int ENEMY_MOVE_INTERVAL = 20;
        public const int ENEMY_HORIZONTAL_MOVE_INTERVAL = 30;

        public const double ENEMY_SHOOT_PROBABILITY = 0.02;
        public const int ENEMY_DAMAGE = 1;

        public const int POINTS_BLOCK = 20;
        public const int POINTS_CIRCLE = 70;
        public const int POINTS_SHOOTER = 40;

        public const int LIVES_BLOCK = 21;
        public const int LIVES_CIRCLE = 9;
        public const int LIVES_SHOOTER = 3; 

        public const int BULLET_SPEED = 2;
        public const int PLAYER1_SHOOTING_SPEED = 5;
        public const int BULLET_DAMAGE = 2;


        public const int PLAYER2_SHOOTING_SPEED = 3;
        public const int BROADSIDE_DAMAGE = 1;

    }

    public enum PlayerType
    {
        BULLETPLAYER,
        BROADSIDEPLAYER
    }

    public enum ProjectileType
    {
        BULLET,
        BROADSIDEL,
        BROADSIDER,
        ENEMY
    }

    public enum EnemyType
    {
        BLOCK,
        CIRCLE,
        SHOOTER
    }

    public enum TcpMessageType : byte
    {
        LOGIN_REQUEST = 1,
        LOGIN_RESPONSE = 2,
        GAME_START = 3,
        GAME_END = 4,
        DISCONNECT = 5
    }

    [Flags]
    public enum PlayerInput : byte
    {
        NONE = 0,
        MOVE_UP = 1,
        MOVE_DOWN = 2,
        MOVE_LEFT = 4,
        MOVE_RIGHT = 8,
        SHOOT = 16
    }
}