namespace Hype.GameServer.World.Navigation.Detail
{
    public class ClosePath : IPath
    {
        public ClosePath(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }
}
