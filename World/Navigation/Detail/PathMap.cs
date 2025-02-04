namespace Hype.GameServer.World.Navigation.Detail
{
    using Hype.Elfang.Core.Map;

    public sealed class PathMap : Generic2DMap<IPath>
    {
        public bool Contains(IPath path)
        {
            return this.Contains(path.X, path.Y);
        }

        public IPath? GetPath(int x, int y)
        {
            return this.GetValue(x, y);
        }

        public bool TryRemove(IPath path)
        {
            return this.TryRemove(path.X, path.Y);
        }

        public void Add(IPath path)
        {
            this.Add(x: path.X, y: path.Y, path);
        }
    }
}
