namespace Hype.GameServer.World.Map
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using Common.Logging;
    using Common.Util;
    using Hype.Elfang.Core.Algorithm;
    using Hype.Elfang.Core.Util;
    using Hype.GameServer.Collision;
    using Hype.GameServer.Extension;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Aoi;
    using Hype.GameServer.Util;
    using Hype.GameServer.World.Map.Detail;
    using Hype.GameServer.World.Navigation;
    using Newtonsoft.Json.Linq;

    public interface ITopography
    {
        ICollisionGrid CollisionGrid { get; }
        AoiGrid AoiGrid { get; }
        INavigator Navigator { get; }
        Cell? GetCell(in Vector3 pos);
        bool TryGetCell(in Vector3 pos, out Cell cell);
    }

    public class Topography : ITopography
    {
        public const int WorldYSize = 20;
        public const int WorldXSize = 24;
        public const int WorldZSize = 24;
        public const int WorldWidth = 13;
        public const int WorldHeight = 13;
        public const int WorldXStart = -150;
        public const int WorldZStart = -150;

        public const int WorldXEnd = WorldXStart + (WorldXSize * WorldWidth);
        public const int WorldZEnd = WorldZStart + (WorldZSize * WorldWidth);
        public const int CollisionTileWidth = WorldWidth * 2;
        public const int AoiTileWidth = WorldWidth * 7;

        // z axis, x axis
        // 장애물 탐지용 셀맵, 셀 단위로 충돌 판정
        // private readonly Dictionary<int, Dictionary<int, Cell>> _cellMap = new Dictionary<int, Dictionary<int, Cell>>();
        private readonly CellMap _cellMap = new CellMap();

        // y axis, z axis, x axis
        //// private readonly Dictionary<int, Dictionary<int, Dictionary<int, Tile>>> _tileMap = new Dictionary<int, Dictionary<int, Dictionary<int, Tile>>>();
        private readonly CollisionGrid _collisionGrid;
        private readonly AoiGrid _aoiGrid;
        private readonly Navigator _navigator;

        public Topography()
        {
            this._collisionGrid = new CollisionGrid(WorldXStart, WorldZStart, WorldXEnd, WorldZEnd, CollisionTileWidth);
            this._aoiGrid = new AoiGrid(WorldXStart, WorldZStart, WorldXEnd, WorldZEnd, AoiTileWidth);
            this._navigator = new Navigator(this._cellMap, endWorldX: WorldXEnd, endWorldY: WorldZEnd);
        }

        public ICollisionGrid CollisionGrid => this._collisionGrid;
        public AoiGrid AoiGrid => this._aoiGrid;
        public INavigator Navigator => this._navigator;

        public static int ToWorldX(int cellX)
        {
            var worldX = WorldXStart + (cellX * Cell.Width);
            return worldX;
        }

        public static int ToWorldZ(int cellZ)
        {
            var worldZ = WorldZStart + (cellZ * Cell.Width);
            return worldZ;
        }

        public static int ToCellX(float worldX)
        {
            int cellX = (int)Math.Abs(WorldXStart - worldX) / Cell.Width;

            return cellX;
        }

        public static int ToCellZ(float worldZ)
        {
            int cellZ = (int)Math.Abs(WorldZStart - worldZ) / Cell.Width;

            return cellZ;
        }

        public Cell? GetCell(in Vector3 pos)
        {
            return this._cellMap.GetCell(in pos);
        }

        public bool TryGetCell(in Vector3 pos, out Cell cell)
        {
            return this._cellMap.TryGetCell(in pos, out cell);
        }

        public void Initialize()
        {
            this.LoadGeometryData();
        }

        private void LoadGeometryData()
        {
            Log.Warn("==================================================>");

            string jsonFullPath = Path.GetFullPath(@".\Config\server.config.json");
            string jsonFile = File.ReadAllText(jsonFullPath);
            var jsonObject = JObject.Parse(jsonFile);
            Debug.Assert(jsonObject is not null, "jsonObj is not nul");

            var objectTokens = jsonObject!["geometry"];
            Debug.Assert(objectTokens is not null, "objectToken is not null");

            Dictionary<string, GeometryVertex> geometryVertexMap = new ();

            foreach (JToken objectToken in objectTokens)
            {
                var groupName = objectToken.Value<string>("name");
                Debug.Assert(groupName is not null, "name is not null");

                var vectorTokens = objectToken["vectors"];
                if (vectorTokens is null)
                {
                    continue;
                }

                this.LoadVertexsInfo(groupName: groupName, vectorTokens: vectorTokens, geometryVertexMap, out var vertexMap);
                this.DrawBoundaryBarrier(groupName: groupName, vertexMap: vertexMap, geometryVertexMap: geometryVertexMap);
                this.DrawInsideBarrier(vertexMap: vertexMap);
            }
        }

        private void LoadVertexsInfo(
            string groupName,
            JToken vectorTokens,
            in Dictionary<string, GeometryVertex> geometryVertexMap,
            out Dictionary<string, (Vector3 pos, string[] links)> vertexMap)
        {
            vertexMap = new Dictionary<string, (Vector3 pos, string[] links)>();

            // Get Verticies
            foreach (var vectorToken in vectorTokens)
            {
                var name = vectorToken.Value<string>("name");
                Debug.Assert(name != null, "name is not null");

                var positionToken = vectorToken["position"];
                Debug.Assert(positionToken != null, "position is not null");

                Vector3 pos = new Vector3(positionToken!.Value<float>("x"), positionToken.Value<float>("y"), positionToken.Value<float>("z"));

                var linksToken = vectorToken["links"];
                Debug.Assert(linksToken != null, "links is null");

                var links = linksToken.ToObject<string[]>();
                Debug.Assert(links != null, "strArray is not null");

                vertexMap.Add(name, new (pos, links));

                var cell = new Cell(
                    worldX: pos.X,
                    worldZ: pos.Z,
                    cellX: Topography.ToCellX(pos.X),
                    cellZ: Topography.ToCellZ(pos.Z),
                    height: 0,
                    cellType: CellType.Barrier,
                    isBoundary: true,
                    vertexName: name);
                this._cellMap.Add(cell);

                var geometryVertex = new GeometryVertex(groupName: groupName, vertexName: name, position: pos);
                geometryVertexMap.Add(geometryVertex.GeometryName, geometryVertex);
            }
        }

        private void DrawBoundaryBarrier(string groupName, Dictionary<string, (Vector3 pos, string[] links)> vertexMap, Dictionary<string, GeometryVertex> geometryVertexMap)
        {
            // Draw Outer Wall
            foreach (var keyPair in vertexMap)
            {
                var vertexName = keyPair.Key;
                var links = keyPair.Value.links;
                var vertexPos = keyPair.Value.pos;
                var linkedVertexs = links.Select(vertexName =>
                {
                    return (vertexName, vertexMap[vertexName].pos);
                });

                var vertexCell = this.GetCell(vertexPos);
                Debug.Assert(vertexCell != null, "vertexCell != null");

                var geometryVertexName = $"{groupName}_{vertexName}";
                var geometryVertex = geometryVertexMap[geometryVertexName];

                foreach (var (linkedVertexName, linkedPos) in linkedVertexs)
                {
                    var start = vertexPos.ToCellVector2();
                    var end = linkedPos.ToCellVector2();
                    var paths = BresenhamLine.GetRaycastPathList(start, end);

                    var linkedGeometryVertexName = $"{groupName}_{linkedVertexName}";
                    var linkedGeometryVertex = geometryVertexMap[linkedGeometryVertexName];

                    var linkedVertexLine = new GeometryLine(groupName: groupName, fromName: vertexName, toName: linkedVertexName);
                    linkedVertexLine.AddVertex(geometryVertex);
                    linkedVertexLine.AddVertex(linkedGeometryVertex);

                    foreach (var path in paths)
                    {
                        var cellX = (int)path.X;
                        var cellZ = (int)path.Y;
                        if (this._cellMap.TryGetValue(x: cellX, y: cellZ, out var boundaryCell))
                        {
                            boundaryCell.AddLine(linkedVertexLine);
                            continue;
                        }

                        boundaryCell = new Cell(
                            worldX: ToWorldX(cellX),
                            worldZ: ToWorldZ(cellZ),
                            cellX: cellX,
                            cellZ: cellZ,
                            height: 0,
                            cellType: CellType.Barrier,
                            isBoundary: true,
                            vertexName: string.Empty);
                        boundaryCell.AddLine(linkedVertexLine);

                        this._cellMap.Add(boundaryCell);
                    }
                }
            }
        }

        private void DrawInsideBarrier(Dictionary<string, (Vector3 pos, string[] links)> vertexMap)
        {
            // Draw Inside
            var leftTopX = vertexMap.Select(x => x.Value.pos.X).Min();
            var leftTopZ = vertexMap.Select(x => x.Value.pos.Z).Min();
            var leftTopCellX = ToCellX(leftTopX);
            var leftTopCellZ = ToCellZ(leftTopZ);

            var rightBottomX = vertexMap.Select(x => x.Value.pos.X).Max();
            var rightBottomZ = vertexMap.Select(x => x.Value.pos.Z).Max();
            var rightBpttomCellX = ToCellX(rightBottomX);
            var rightBottomCellZ = ToCellZ(rightBottomZ);

            for (var cellZ = leftTopCellZ; cellZ <= rightBottomCellZ; ++cellZ)
            {
                var isPrevBarrier = false;
                var boundaryRangeList = new List<int>();
                for (var cellX = leftTopCellX; cellX <= rightBpttomCellX; ++cellX)
                {
                    if (this._cellMap.TryGetValue(x: cellX, y: cellZ, out var cell) is false)
                    {
                        isPrevBarrier = false;
                        continue;
                    }

                    ////if (cell.IsBoundary == false)
                    ////{
                    ////    continue;
                    ////}

                    if (isPrevBarrier == false)
                    {
                        boundaryRangeList.Add(cellX);
                        isPrevBarrier = true;
                    }
                }

                var boundaryRangeCount = boundaryRangeList.Count - (boundaryRangeList.Count % 2);
                for (var i = 0; i < boundaryRangeCount; i += 2)
                {
                    var startX = boundaryRangeList[i];
                    var endX = boundaryRangeList[i + 1];

                    for (var cellX = startX; cellX < endX; ++cellX)
                    {
                        if (this._cellMap.Contains(cellX, cellZ))
                        {
                            continue;
                        }

                        var cell = new Cell(
                            worldX: ToWorldX(cellX),
                            worldZ: ToWorldZ(cellZ),
                            cellX: cellX,
                            cellZ: cellZ,
                            height: 0,
                            CellType.Barrier,
                            isBoundary: false,
                            vertexName: string.Empty);
                        this._cellMap.Add(cell);
                    }
                }
            }
        }
    }
}