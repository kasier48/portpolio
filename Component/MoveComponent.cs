namespace Hype.GameServer.InGame.Component
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Common.Logging;
    using CommonProto;
    using GameProto;
    using Hype.GameServer.Collision;
    using Hype.GameServer.Extension;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Component.Detail;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;

    public interface IMoveComponent : IComponent
    {
        MoveResultType Move();
        void ReadjustTargetPosition(in Vector3 targetPos);
    }

    public class MoveComponent : IMoveComponent
    {
        private readonly Actor _actor;
        private readonly ITopography _topography;
        private readonly INavigatorComponent _navigatorComponent;
        
        private Vector3 _velocity;
        private float _velocityLength;
        private float _distance;
        private float _totalDistance;
        private Vector3 _targetPos;

        public MoveComponent(Actor actor, ITopography topography, INavigatorComponent navigatorComponent)
        {
            this._actor = actor;
            this._topography = topography;
            this._navigatorComponent = navigatorComponent;
        }

        public void ReadjustTargetPosition(in Vector3 targetPos)
        {
            this._targetPos = targetPos;
            this._actor.Look(targetPos);
            this._velocity = this._actor.Forward * this._actor.Speed;
            this._velocityLength = this._velocity.Length();
            this._distance = 0f;
            this._totalDistance = Vector3.Distance(this._actor.Position, targetPos);
        }
        
        public MoveResultType Move()
        {
            if (this._distance >= this._totalDistance)
            {
                this._actor.SetPosition(this._targetPos);
                
                if (this._navigatorComponent.IsNavigating == false)
                {
                    return MoveResultType.Reach;
                }

                this._navigatorComponent.MoveNextPath(out var isReachDestination);
                if (isReachDestination)
                {
                    return MoveResultType.Reach;
                }
            }

            var nextPosition = this._actor.Position + this._velocity;
            if (nextPosition.X < Topography.WorldXStart ||
                nextPosition.X > Topography.WorldXEnd ||
                nextPosition.Z < Topography.WorldZStart ||
                nextPosition.Z > Topography.WorldZEnd)
            {
                return MoveResultType.OutOfMap;
            }

            var isIntersectByBarrier = this._actor.Colider.IsIntersectByBarrier(nextPosition);
            if (isIntersectByBarrier)
            {
                return MoveResultType.Barrier;
            }

            this._topography.CollisionGrid.Update(this._actor, nextPosition);
            this._topography.AoiGrid.Update(this._actor, nextPosition);

            this._actor.SetPosition(nextPosition);

            this._distance += this._velocityLength;
            //// Log.Info($"actor position x: {this._position.x}, z: {this._position.z}");

            return MoveResultType.Success;
        }
    }
}
