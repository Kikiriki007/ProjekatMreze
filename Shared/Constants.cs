
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

        public const int FRAME_DELAY_MS = 50;
        public const int ENEMY_MOVE_INTERVAL = 10;
        public const int ENEMY_HORIZONTAL_MOVE_INTERVAL = 25;

        public const double ENEMY_SHOOT_PROBABILITY = 0.015;

        public const int POINTS_BLOCK = 10;
        public const int POINTS_CIRCLE = 20;
        public const int POINTS_SHOOTER = 50;
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