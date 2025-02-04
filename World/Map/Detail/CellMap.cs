namespace Hype.GameServer.World.Map.Detail
{
    using System.Numerics;
    using Hype.Elfang.Core.Map;

    public class CellMap : Generic2DMap<Cell>
    {
        public Cell? GetCell(in Vector3 pos)
        {
            var cellX = Topography.ToCellX(pos.X);
            var cellZ = Topography.ToCellZ(pos.Z);

            return this.GetValue(x: cellX, y: cellZ);
        }

        public bool TryGetCell(in Vector3 pos, out Cell cell)
        {
            var cellX = Topography.ToCellX(pos.X);
            var cellZ = Topography.ToCellZ(pos.Z);

            return this.TryGetValue(x: cellX, y: cellZ, out cell);
        }

        public void Add(Cell cell)
        {
            this.Add(cell.CellX, cell.CellZ, cell);
        }

        public bool TryAdd(Cell cell)
        {
            return this.TryAdd(cell.CellX, cell.CellZ, cell);
        }
    }
}
