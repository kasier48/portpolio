namespace Hype.GameServer.World.Navigation.Detail
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using Hype.GameServer.Collision;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;

    public class PathFinder
    {
        public static readonly int DistanceUnit = 10;
        public static readonly int DistanceCrossUnit = 5;

        private static readonly int[,] SquareVertexMatrix = new int[,]
        {
            { -1, -1 },
            { 1, -1 },
            { -1, 1 },
            { 1, 1 },
            ////{ 1, 0 },
            ////{ -1, 0 },
            ////{ 0, 1 },
            ////{ 0, -1 },
        };

        private readonly CellMap _cellMap;
        private readonly PathPosition[] _nearPathDirection;
        private readonly Stack<OpenPath> _openPaths = new Stack<OpenPath>();
        private readonly PathMap _pathMap = new PathMap();
        private readonly List<ICollider> _colliderList = new ();
        private readonly int _endX;
        private readonly int _endY;
        private PathPosition? _destination;
        private float _colliderRadius;

        public PathFinder(CellMap cellMap, int endX, int endY)
        {
            this._cellMap = cellMap;
            this._endX = endX;
            this._endY = endY;

            this._nearPathDirection = new[]
            {
                new PathPosition(1, 0, DistanceUnit),
                new PathPosition(-1, 0, DistanceUnit),
                new PathPosition(0, 1, DistanceUnit),
                new PathPosition(0, -1, DistanceUnit),
                new PathPosition(1, 1, DistanceCrossUnit),
                new PathPosition(1, -1, DistanceCrossUnit),
                new PathPosition(-1, 1, DistanceCrossUnit),
                new PathPosition(-1, -1, DistanceCrossUnit),
            };
        }

        public void Clear()
        {
            this._openPaths.Clear();
            this._pathMap.Clear();
            this._colliderList.Clear();
            this._colliderRadius = 0f;
        }

        public OpenPath[] Search()
        {
            if (this._destination == null)
            {
                throw new InvalidOperationException($"Destination is not set");
            } // 목적지 설정필요

            if (this._openPaths.Count == 0)
            {
                throw new InvalidOperationException($"Start position is not set");
            } // 사직점 설정 필요

            while (true)
            {
                if (this._openPaths.Count == 0)
                {
                    break;
                }

                if (this._openPaths.TryPeek(out var headTile) is false)
                {
                    break;
                }

                var findPath = this.ChooseOpenPath(headTile, out var isReachEnd);
                if (findPath == null)
                {
                    this.RecalculateHeadTile();
                    continue;
                }

                findPath.IsTraced = true;
                this._openPaths.Push(findPath);

                if (isReachEnd)
                {
                    break;
                }
            }

            if (this._openPaths.Count == 0)
            {
                return new OpenPath[0];
            }

            var openPathLength = this._openPaths.Count;
            var reverseOpenPaths = new OpenPath[openPathLength];

            for (int index = openPathLength - 1; index >= 0; --index)
            {
                var openPath = this._openPaths.Pop();
                reverseOpenPaths[index] = openPath;
            }

            return reverseOpenPaths;
        }

        public void AddColliders(IEnumerable<ICollider> colliders)
        {
            this._colliderList.AddRange(colliders);
        }

        public void SetClosePath(int x, int y)
        {
            this._pathMap[x, y] = new ClosePath(x, y);
        }

        public void SetColliderRadius(float radius)
        {
            this._colliderRadius = radius;
        }

        public void SetDestination(int x, int y)
        {
            this._destination = new PathPosition(x, y, DistanceUnit);
        }

        public bool SetStart(int x, int y)
        {
            var startOpenPath = new OpenPath(x, y, distanceFromStart: 0, distanceFromEnd: 0);
            startOpenPath.IsStart = true;

            var result = this._pathMap.TryAdd(x, y, startOpenPath);
            if (result is false)
            {
                return false;
            }

            this._openPaths.Push(startOpenPath);

            return true;
        }

        private int CaculateDistanceToEnd(int x, int y)
        {
            Debug.Assert(this._destination != null, "endPosition must be not null");

            int xDistance = Math.Abs(this._destination.X - x);
            int yDistance = Math.Abs(this._destination.Y - y);

            return (xDistance * DistanceUnit) + (yDistance * DistanceUnit);
        }

        private void RecalculateHeadTile()
        {
            if (this._openPaths.TryPop(out var headTile) is false)
            {
                return;
            }

            this._pathMap[headTile.X, headTile.Y] = new ClosePath(headTile.X, headTile.Y);

            if (this._openPaths.TryPeek(out var prevHeadTile) is false)
            {
                return;
            }

            OpenPath? choicedPath = null;
            foreach (var nearPosition in this._nearPathDirection)
            {
                var x = prevHeadTile.X + nearPosition.X;
                if (x < 0 || x >= this._endX)
                {
                    continue;
                }

                var y = prevHeadTile.Y + nearPosition.Y;
                if (y < 0 || y >= this._endY)
                {
                    continue;
                }

                var path = this._pathMap.GetPath(x: x, y: y);
                var openPath = path as OpenPath;
                if (openPath is null)
                {
                    continue;
                }

                if (openPath.IsStart || openPath.IsTraced)
                {
                    continue;
                }

                if (choicedPath == null)
                {
                    choicedPath = openPath;
                }
                else if (openPath.DistanceFromEnd < choicedPath.DistanceFromEnd)
                {
                    choicedPath = openPath;
                }
            }

            if (choicedPath == null)
            {
                headTile = this._openPaths.Pop();
                this._pathMap[headTile.X, headTile.Y] = new ClosePath(headTile.X, headTile.Y);
                return;
            }

            choicedPath.IsTraced = true;
            this._openPaths.Push(choicedPath);
        }

        private OpenPath? ChooseOpenPath(OpenPath headTile, out bool isReachEnd)
        {
            isReachEnd = false;
            OpenPath? choicedPath = null;
            foreach (var nearTile in this._nearPathDirection)
            {
                var x = headTile.X + nearTile.X;
                if (x < 0 || x >= this._endX)
                {
                    continue;
                }

                var y = headTile.Y + nearTile.Y;
                if (y < 0 || y >= this._endY)
                {
                    continue;
                }

                if (this._pathMap.Contains(x, y))
                {
                    continue;
                }

                var isIntersectByBarrier = this.IsIntersectByBarrier(cellX: x, cellY: y);
                if (isIntersectByBarrier)
                {
                    this._pathMap[x, y] = new ClosePath(x, y);
                    continue;
                }

                var isIntersectByCollider = this.IsIntersectByCollider(cellX: x, cellY: y);
                if (isIntersectByCollider)
                {
                    this._pathMap[x, y] = new ClosePath(x, y);
                    continue;
                }

                var distanceFromStart = headTile.DistanceFromStart + nearTile.DistanceUnit;
                var distanceFromEnd = this.CaculateDistanceToEnd(x, y);
                var openPath = new OpenPath(
                    x: x,
                    y: y,
                    distanceFromStart: distanceFromStart,
                    distanceFromEnd: distanceFromEnd);

                this._pathMap.Add(openPath);

                if (openPath.DistanceFromEnd == 0)
                {
                    choicedPath = openPath;
                    isReachEnd = true;
                    break;
                } // 목적지 도달

                if (choicedPath == null)
                {
                    choicedPath = openPath;
                }
                else if (openPath.TotalDistance < choicedPath.TotalDistance)
                {
                    choicedPath = openPath;
                }
            }

            return choicedPath;
        }

        private bool IsIntersectByCollider(int cellX, int cellY)
        {
            if (this._colliderList.Count == 0)
            {
                return false;
            }

            var worldX = Topography.ToWorldX(cellX);
            var worldZ = Topography.ToWorldZ(cellY);

            foreach (var collider in this._colliderList)
            {
                var target = new Vector2(x: worldX, y: worldZ);
                var result = collider.IsIntersectWithinRadius(target: target, radius: this._colliderRadius);
                if (result)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIntersectByBarrier(int cellX, int cellY)
        {
            if (this._cellMap.TryGetValue(cellX, cellY, out var cell) &&
                cell.CellType == CellType.Barrier)
            {
                return true;
            }

            var worldX = Topography.ToWorldX(cellX);
            var worldZ = Topography.ToWorldZ(cellY);

            var rowLength = SquareVertexMatrix.GetLength(0);

            for (var row = 0; row < rowLength; ++row)
            {
                var xMatrix = SquareVertexMatrix[row, 0];
                var yMatrix = SquareVertexMatrix[row, 1];

                var vertexWorldX = worldX + (this._colliderRadius * xMatrix);
                var vertexWorldZ = worldZ + (this._colliderRadius * yMatrix);

                var vertextWorldPosition = new Vector3(x: vertexWorldX, y: 0, z: vertexWorldZ);
                if (this._cellMap.TryGetCell(in vertextWorldPosition, out var vertexCell) &&
                    vertexCell.CellType == CellType.Barrier)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
