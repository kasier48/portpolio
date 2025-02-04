namespace Hype.GameServer.World.Navigation.Detail
{
    using Hype.GameServer.World.Map;

    public class OpenPath : IPath
    {
        public OpenPath(int x, int y, int distanceFromStart, int distanceFromEnd)
        {
            this.X = x;
            this.Y = y;
            this.DistanceFromStart = distanceFromStart;
            this.DistanceFromEnd = distanceFromEnd;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int WorldX => Topography.ToWorldX(this.X);
        public int WorldY => Topography.ToWorldZ(this.Y);
        public int DistanceFromStart { get; private set; }
        public int DistanceFromEnd { get; private set; }
        public int TotalDistance => this.DistanceFromStart + this.DistanceFromEnd;
        public bool IsStart { get; set; }
        public bool IsTraced { get; set; }
    }
}
