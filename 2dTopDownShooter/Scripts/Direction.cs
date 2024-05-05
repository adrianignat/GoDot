using Godot;

namespace dTopDownShooter.Scripts
{
    public enum Direction
    {
        N,
        S,
        W,
        E,
        NW,
        NE,
        SW,
        SE
    }

    public static class DirectionHelper
    {
        public static Direction GetMovingDirection(Vector2 vector)
        {
            if (vector.X > 0 && vector.Y == 0) // Right
                return Direction.E;
            else if (vector.X < 0 && vector.Y == 0) // Left
                return Direction.W;
            else if (vector.Y < 0 && vector.X == 0) // Up
                return Direction.N;
            else if (vector.Y > 0 && vector.X == 0) // Down
                return Direction.S;
            else if (vector.X > 0 && vector.Y > 0) // Down Right
                return Direction.SE;
            else if (vector.X > 0 && vector.Y < 0) // Up Right
                return Direction.NE;
            else if (vector.X < 0 && vector.Y > 0) // Down Left
                return Direction.SW;
            else if (vector.X < 0 && vector.Y < 0) // Up Left
                return Direction.NW;

            return Direction.E;
        }

        public static Direction GetFacingDirection(Vector2 vector)
        {
            if (vector.X < 0)
                return Direction.W;
            else if (vector.X > 0)
                return Direction.E;

            return Direction.E;
        }
    }
}
