namespace Hype.GameServer.InGame.Fsm.State
{
    using System.Collections.Generic;
    using System.Numerics;
    using Common.Extension;
    using Common.Logging;
    using CommonProto;
    using GameProto;
    using Hype.GameServer.Collision;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Component;
    using Hype.GameServer.InGame.Component.Detail;
    using Hype.GameServer.InGame.Fsm.Detail;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;
    using Org.BouncyCastle.Asn1.X509;

    public abstract class ActorMoveState : IFsmBehaviour
    {
        protected readonly IMoveComponent _moveComponent;
        protected readonly INavigatorComponent _navigatorComponent;
        private const long IntervalTargetPathCheck = 500;
        private readonly Actor _actor;
        private readonly ICollisionGrid _collisionGrid;

        private long _accuTargetPathCheckDelta;

        public ActorMoveState(Actor actor, ICollisionGrid collisionGrid)
        {
            this._actor = actor;
            this._moveComponent = actor.GetComponent<IMoveComponent>();
            this._navigatorComponent = actor.GetComponent<INavigatorComponent>();
            this._collisionGrid = collisionGrid;
        }

        public virtual void Begin()
        {
            this._moveComponent.ReadjustTargetPosition(this._actor.TargetPos);

            this._actor.Aoi.MulticastMoveUnit(Proto_MoveUnit.Types.MoveType.Start);
        }

        public virtual void Update(long delta)
        {
            foreach (var nearActor in this.GetNearActorsWithinViewingAngle(this._actor))
            {
                if (nearActor.Colider.IsIntersect(this._actor.Colider))
                {
                    this.OnActorCollision(nearActor);
                    return;
                } // 나와 상대방의 충돌 처리.

                this._actor.FollowStrategy?.OnFollowCandiate(nearActor);
            }

            if (this._actor.TargetActor == null)
            {
                var followActor = this._actor.FollowStrategy?.GetCandidate();
                if (followActor != null)
                {
                    this.OnActorFollow(followActor);
                    return;
                }
            }
            else
            {
                this._accuTargetPathCheckDelta += delta;
                if (this._accuTargetPathCheckDelta >= IntervalTargetPathCheck)
                {
                    this._accuTargetPathCheckDelta = 0;

                    if (this._actor.TargetPos != this._actor.TargetActor.Position)
                    {
                        this._actor.SetTargetPos(this._actor.TargetActor.Position);
                        this._actor.Fsm.ChangeState(FsmStateType.Move);
                        return;
                    } // 타켓 액터의 위치가 변경될 경우 타켓 위치를 변경하여 다시 추적한다.
                }
            }

            var moveResult = this._moveComponent.Move();
            switch (moveResult)
            {
                case MoveResultType.Reach:
                    this.OnReached(this._actor.TargetActor);
                    return;

                case MoveResultType.Barrier:
                    this.OnBarrierCollision();
                    return;

                case MoveResultType.OutOfMap:
                    this._actor.Fsm.ChangeState(FsmStateType.Idle);
                    Log.Error($"actor: {this._actor.ActorId} can't move, out of map");
                    return;
            }
        }

        public virtual void End()
        {
        }

        protected abstract void OnReached(Actor? targetActor);
        protected abstract void OnActorFollow(Actor followActor);
        protected abstract void OnActorCollision(Actor collisionActor);
        protected abstract void OnBarrierCollision();
        protected void SendOnMoveUnit(ActorUser targetUser, ActorMoveResultType resultType)
        {
            var protoOnMoveUnit = new Proto_OnMoveUnit();

            protoOnMoveUnit.Uid = targetUser.Account.Uid;
            protoOnMoveUnit.MoveResultType = resultType;
            protoOnMoveUnit.Position = new CVector3();
            protoOnMoveUnit.Position.Set(targetUser.Position);
            protoOnMoveUnit.ActorKey = targetUser.ActorId;
            protoOnMoveUnit.TargetActorKey = targetUser.TargetActor?.ActorId ?? 0;

            targetUser.Account.Send(protoOnMoveUnit);
        }

        private IEnumerable<Actor> GetNearActorsWithinViewingAngle(Actor actor)
        {
            var nearActors = this._collisionGrid.GetNearActors(
                tileX: actor.CollisionTile!.X,
                tileZ: actor.CollisionTile!.Z,
                roomKey: actor.GameRoom!.RoomId,
                exceptActorKey: actor.ActorId);
            foreach (var nearAcotr in nearActors)
            {
                if (actor.IsWithinViewingAngle(nearAcotr.Position) == false)
                {
                    // Log.Error($"collision passing actor, not within viewing angle, actorKey: {otherActor.ActorKey}");
                    continue;
                }

                yield return nearAcotr;
            }
        }
    }
}
