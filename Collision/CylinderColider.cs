namespace Hype.GameServer.Collision
{
    using System;
    using System.Numerics;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Component.Detail;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;

    public interface ICollider
    {
        float Radius { get; }
        Vector3 CenterPos { get; }
        bool IsIntersectWithinRadius(in Vector2 target, float radius);
    }

    public sealed class CylinderColider : ICollider
    {
        private static readonly int[,] SquareVertexMatrix = new int[,] 
        {
            { -1, -1 },
            { 1, -1 },
            { -1, 1 },
            { 1, 1 },
        };

        private readonly ITopography _topography;
        private Actor _actor = null!;

        public CylinderColider(float radius, float height, ITopography topography)
        {
            this.Radius = radius;
            this.Height = height;
            this._topography = topography;
        }

        public Vector3 CenterPos => this._actor.Position;
        public float CenterVertexY => this._actor.Position.Y + this.Height;
        public Vector3 CenterForward => this._actor.Forward * this.Radius;

        /// <summary>
        /// 2D 원점으로부터 반지름.
        /// </summary>
        public float Radius { get; private set; }
        public float Diameter => this.Radius + this.Radius;

        /// <summary>
        /// 2D 원점으로부터 높이.
        /// </summary>
        public float Height { get; private set; }

        public void SetOwner(Actor owner)
        {
            this._actor = owner;
        }

        public bool IsWithinRadius(in Vector2 target)
        {
            float xDiff = this.CenterPos.X - target.X;
            float zDiff = this.CenterPos.Z - target.Y;

            return (xDiff * xDiff) + (zDiff * zDiff) < (this.Radius * this.Radius);
        }

        public bool IsIntersectByBarrier(Vector3 target)
        {
            var cellX = Topography.ToCellX(target.X);
            var cellZ = Topography.ToCellZ(target.Z);

            var worldX = Topography.ToWorldX(cellX);
            var worldZ = Topography.ToWorldZ(cellZ);

            target = new Vector3(x: worldX, y: target.Y, z: worldZ);
            //// 소수점으로 인해 발생하는 오차로 인해 cell 단위로 변환 후 다시 월드 좌표로 변환.

            var centerForwardPosition = target + this.CenterForward;
            if (this._topography.TryGetCell(centerForwardPosition, out var forwardCell) &&
                forwardCell.CellType is CellType.Barrier)
            {
                return true;
            } // 원의 중점이 바라보는 방향에 대한 충돌 체크.

            ////var squareVertexPostions = this.GetSquareVertexPositions(target);
            ////foreach (var vertexPosition in squareVertexPostions)
            ////{
            ////    if (this._topography.TryGetCell(in vertexPosition, out var vertexCell) &&
            ////        vertexCell.CellType is CellType.Barrier)
            ////    {
            ////        return true;
            ////    }
            ////} // 원의 사각형 경계선 충돌 체크.

            return false;
        }

        public bool IsIntersectWithinRadius(in Vector2 target, float radius)
        {
            var distance = Vector2.Distance(
                new Vector2(x: this.CenterPos.X, y: this.CenterPos.Z),
                new Vector2(x: target.X, y: target.Y));
            var totalRadius = this.Radius + radius;

            var overlapped = distance < totalRadius;
            return overlapped;
        }
        
        public bool IsIntersect(CylinderColider target)
        {
            var result = this.CenterPos.Y < target.CenterVertexY && target.CenterPos.Y < this.CenterVertexY;
            if (result == false)
            {
                return false;
            } // y 축에 대한 충돌 체크.

            var distance = Vector2.Distance(
                new Vector2(x: this.CenterPos.X, y: this.CenterPos.Z), 
                new Vector2(x: target.CenterPos.X, y: target.CenterPos.Z));
            var totalRadius = this.Radius + target.Radius;

            var overlapped = distance < totalRadius;
            return overlapped;
        }

        public bool IsIntersect(Cell cell)
        {
            ////bool result = this.CenterPos.Y < cell.Height && cell.Height < this.VertexY;
            ////if (result == false)
            ////{
            ////    return false;
            ////}

            float left = cell.WorldX - this.Radius;
            float right = cell.WorldXEnd + this.Radius;
            float bottom = cell.WorldZ - this.Radius;
            float top = cell.WorldZEnd + this.Radius;

            if (this.CenterPos.X > left && this.CenterPos.X < right && this.CenterPos.Z < top && this.CenterPos.Z > bottom)
            {
                return true;
            }

            ////if (this.IsWithinCircle(x: target.WorldX, z: target.WorldZ))
            ////{
            ////    return true;
            ////}
            ////else if (this.IsWithinCircle(x: target.WorldX, z: target.WorldZEdge))
            ////{
            ////    return true;
            ////}
            ////else if (this.IsWithinCircle(x: target.WorldXEdge, z: target.WorldZ))
            ////{
            ////    return true;
            ////}
            ////else if (this.IsWithinCircle(x: target.WorldXEdge, z: target.WorldZEdge))
            ////{
            ////    return true;
            ////}

            return false;
        }

        private Vector3[] GetSquareVertexPositions(in Vector3 position)
        {
            var rowLength = SquareVertexMatrix.GetLength(0);
            var edgePositions = new Vector3[rowLength];

            for (var row = 0; row < rowLength; ++row)
            {
                var xMatrix = SquareVertexMatrix[row, 0];
                var zMatrix = SquareVertexMatrix[row, 1];

                var xEdgePosition = position.X + (this.Radius * xMatrix);
                var zEdgePosition = position.Z + (this.Radius * zMatrix);

                edgePositions[row] = new Vector3(x: xEdgePosition, y: position.Y, z: zEdgePosition);
            }

            return edgePositions;
        }
    }
}
