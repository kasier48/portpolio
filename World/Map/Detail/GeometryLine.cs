namespace Hype.GameServer.World.Map.Detail
{
    using System.Collections.Generic;
    using System.Numerics;

    public sealed class GeometryLine
    {
        private readonly List<GeometryVertex> _vertexList = new (capacity: 2);

        public GeometryLine(string groupName, string fromName, string toName)
        {
            this.GroupName = groupName;
            this.FromName = fromName;
            this.ToName = toName;
            this.NameKey = fromName.GetHashCode() ^ toName.GetHashCode();
        }

        public IReadOnlyList<GeometryVertex> Vertexs => this._vertexList;
        public string GroupName { get; private set; }
        public string FromName { get; private set; }
        public string ToName { get; private set; }
        public long NameKey { get; private set; }
        public void AddVertex(GeometryVertex vertex)
        {
            this._vertexList.Add(vertex);
        }
    }
}
