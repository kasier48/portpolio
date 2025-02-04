namespace Hype.GameServer.World.Map.Detail
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Cell
    {
        public const int Width = Topography.WorldWidth / 6; // x, z 너비

        private readonly List<GeometryLine> _lines = new List<GeometryLine>(capacity: 2);
             
        public Cell(float worldX, float worldZ, int cellX, int cellZ, int height, CellType cellType, bool isBoundary, string vertexName)
        {
            this.WorldX = worldX;
            this.WorldZ = worldZ;
            this.CellX = cellX;
            this.CellZ = cellZ;
            this.Height = height;
            this.CellType = cellType;
            this.IsBoundary = isBoundary;
            this.VertexName = vertexName;
        }

        public string VertexName { get; private set; }
        public bool IsBoundary { get; private set; }

        // 월드 좌표 시작점 (x, y, z)
        public float WorldX { get; private set; } 
        public float WorldZ { get; private set; }

        public float WorldXEnd => this.WorldX + Width;
        public float WorldZEnd => this.WorldZ + Width;

        // 배열의 인덱스.
        public int CellX { get; private set; }
        public int CellZ { get; private set; }
        public int Height { get; private set; }
        public CellType CellType { get; set; } = CellType.Open;
        public IReadOnlyList<GeometryLine> Lines => this._lines;

        public bool AddLine(GeometryLine line)
        {
            if (this._lines.Any(e => e.NameKey == line.NameKey))
            {
                return false;
            }

            this._lines.Add(line);
            return true;
        }

        /////// <summary>
        /////// 해당 셀이 어느 타일에 속하는지.
        /////// </summary>
        ////public Tile TileInfo { get; private set; }

        ////public void SetTileInfo(Tile info)
        ////{
        ////    this.TileInfo = info;
        ////}
    }
}
