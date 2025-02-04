namespace Hype.GameServer.World.Navigation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Hype.Elfang.Core.Algorithm;
    using Hype.GameServer.Collision;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;
    using Hype.GameServer.World.Navigation.Detail;

    public interface INavigator
    {
        Vector2[] FindPath(in Vector2 start, in Vector2 destination, float colliderRadius, IEnumerable<Vector2> closePaths, IEnumerable<ICollider> colliders);
        bool Raycast2D(in Vector2 start, in Vector2 end, out Vector2 hitPosition);
    }

    public class Navigator : INavigator
    {
        private readonly PathFinder _pathFinder;
        private readonly CellMap _cellMap;
        private readonly int _endCellX;
        private readonly int _endCellY;

        public Navigator(CellMap cellMap, int endWorldX, int endWorldY)
        {
            this._cellMap = cellMap;
            this._pathFinder = new PathFinder(cellMap, endX: endWorldX, endY: endWorldY);
            this._endCellX = Topography.ToCellX(endWorldX);
            this._endCellY = Topography.ToCellZ(endWorldY);
        }

        public Vector2[] FindPath(in Vector2 start, in Vector2 destination, float colliderRadius, IEnumerable<Vector2> closePaths, IEnumerable<ICollider> colliders)
        {
            this._pathFinder.Clear();
            this._pathFinder.SetColliderRadius(colliderRadius);
            this._pathFinder.AddColliders(colliders);

            foreach (var closePosition in closePaths)
            {
                var closeX = Topography.ToCellX(closePosition.X);
                var closeY = Topography.ToCellZ(closePosition.Y);
                this._pathFinder.SetClosePath(closeX, closeY);
            }

            var startX = Topography.ToCellX(start.X);
            var startZ = Topography.ToCellZ(start.Y);
            var result = this._pathFinder.SetStart(startX, startZ);
            if (result == false)
            {
                return new Vector2[0];
            }

            var destX = Topography.ToCellX(destination.X);
            var destZ = Topography.ToCellZ(destination.Y);
            this._pathFinder.SetDestination(destX, destZ);

            var openPaths = this._pathFinder.Search();
            var paths = openPaths.Select(e =>
            {
                var worldX = Topography.ToWorldX(e.X);
                var worldZ = Topography.ToWorldZ(e.Y);
                return new Vector2(worldX, worldZ);
            }).ToArray();

            return paths;
        }

        public bool Raycast2D(in Vector2 start, in Vector2 end, out Vector2 hitPosition)
        {
            hitPosition = Vector2.Zero;

            var startX = Topography.ToCellX(start.X);
            if (startX < 0 || this._endCellX < startX)
            {
                return false;
            }

            var startY = Topography.ToCellZ(start.Y);
            if (startY < 0 || this._endCellY < startY)
            {
                return false;
            }

            var cellStart = new Vector2(x: startX, y: startY);

            var endX = Topography.ToCellX(end.X);
            if (endX < 0 || this._endCellX < endX)
            {
                return false;
            }

            var endY = Topography.ToCellZ(end.Y);
            if (endY < 0 || this._endCellY < endY)
            {
                return false;
            }

            var cellEnd = new Vector2(x: endX, y: endY);

            foreach (var path in BresenhamLine.GetRaycastPaths(start: cellStart, end: cellEnd))
            {
                var cell = this._cellMap.GetValue((int)path.X, (int)path.Y);
                if (cell != null &&
                    cell.CellType == CellType.Barrier)
                {
                    var worldX = Topography.ToWorldX((int)path.X);
                    var worldY = Topography.ToWorldZ((int)path.Y);

                    hitPosition = new Vector2(x: worldX, y: worldY);
                    return true;
                }
            }

            return false;
        }
    }
}
