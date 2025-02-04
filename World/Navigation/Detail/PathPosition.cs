namespace Hype.GameServer.World.Navigation.Detail
{
    public class PathPosition 
    {
        public PathPosition(int x, int y, int distanceUnit)
        {
            this.X = x;
            this.Y = y;
            this.DistanceUnit = distanceUnit;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int DistanceUnit { get; private set; }
    }
}
