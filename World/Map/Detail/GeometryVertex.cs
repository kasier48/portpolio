namespace Hype.GameServer.World.Map.Detail
{
    using System.Collections.Generic;
    using System.Numerics;

    public sealed class GeometryVertex
    {
        public GeometryVertex(string groupName, string vertexName, in Vector3 position)
        {
            this.GroupName = groupName;
            this.VertexName = vertexName;
            this.Position = position;
        }

        public string GroupName { get; private set; }
        public string VertexName { get; private set; }
        public string GeometryName => $"{this.GroupName}_{this.VertexName}";
        public Vector3 Position { get; private set; }
        public int CellX => Topography.ToCellX(this.Position.X);
        public int CellZ => Topography.ToCellZ(this.Position.Z);
    }
}
