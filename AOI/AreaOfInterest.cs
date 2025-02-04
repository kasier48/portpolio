namespace Hype.GameServer.InGame.Aoi
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.Extension;
    using Common.Logging;
    using CommonProto;
    using GameProto;
    using Google.Protobuf;
    using Hype.GameServer.InGame.Actors;

    public sealed class AreaOfInterest
    {
        private readonly Actor _actor;
        private readonly HashSet<Actor> _vision = new HashSet<Actor>();

        public AreaOfInterest(Actor actor)
        {
            this._actor = actor;
        }

        public IEnumerable<Actor> Actors => this._vision;

        public void Enter(Actor actor)
        {
            Log.Debug($"Aoi enter, actorKey: {actor.ActorId}");
            this._vision.Add(actor);
            actor.Aoi.Add(this._actor);
        }

        public void Leave(Actor actor)
        {
            Log.Debug($"Aoi leave, actorKey: {actor.ActorId}");
            this._vision.Remove(actor);
            actor.Aoi.Remove(this._actor);
        }

        public void Add(Actor actor)
        {
            this._vision.Add(actor);
        }

        public void Remove(Actor actor)
        {
            this._vision.Remove(actor);
        }

        public void MulticastForceStopMove()
        {
            var protoForceStopMove = new Proto_ForceStopMove()
            {
                ActorKey = this._actor.ActorId,
                Forward = this._actor.Forward.ToCVector3(),
                Position = this._actor.Position.ToCVector3(),
            };

            this.Multicast(protoForceStopMove);
        }

        public void MulticastMoveUnit(Proto_MoveUnit.Types.MoveType moveType)
        {
            var proto = new Proto_MoveUnit();

            if (this._actor is ActorUser user)
            {
                proto.Uid = user.Account.Uid;
            }

            proto.RoomKey = this._actor.GameRoom!.RoomId;
            proto.ActorKey = this._actor.ActorId;
            proto.Position = this._actor.Position.ToCVector3();
            proto.Forward = this._actor.Forward.ToCVector3();
            proto.TargetPos = this._actor.TargetPos.ToCVector3();
            proto.TargetActorKey = this._actor.TargetActor?.ActorId ?? 0;
            proto.MoveType = moveType;

            this.Multicast(proto);
        }

        public void MulticastMoveUnitAlongPath(in System.Numerics.Vector2[] paths)
        {
            var proto = new Proto_MoveUnitAlongPath();
            proto.ActorId = this._actor.ActorId;
            proto.Position = this._actor.Position.ToCVector3();

            if (this._actor is ActorUser actorUser)
            {
                proto.Uid = actorUser.Account.Uid;
            }

            var pathsVec3 = paths.Select(e => e.ToCVector3());
            proto.Paths.AddRange(pathsVec3);

            this.Multicast(proto);
        }

        public void MulticastFollowActor(Actor targetActor)
        {
            var proto = new Proto_FollowTargetActor();
            proto.TargetActorKey = targetActor.ActorId;
            proto.TargetPosition = targetActor.Position.ToCVector3();
            proto.ActorKey = this._actor.ActorId;
            proto.Position = this._actor.Position.ToCVector3();
            proto.Forward = this._actor.Forward.ToCVector3();

            this.Multicast(proto);
        }

        public void MulticastEnterAoi()
        {
            var proto = new Proto_EnterAoi();

            var spawnInfo = new ActorSpawnInfo();
            spawnInfo.ActorType = this._actor.ActorType;

            if (this._actor is ActorUser actorUser)
            {
                spawnInfo.Uid = actorUser.Account.Uid;
            }
            
            spawnInfo.ActorKey = this._actor.ActorId;
            spawnInfo.FsmStateType = this._actor.Fsm.StateType;
            spawnInfo.Position = this._actor.Position.ToCVector3();
            spawnInfo.Forward = this._actor.Forward.ToCVector3();

            proto.SpawnInfos.Add(spawnInfo);

            this.Multicast(proto);
        }

        public void MulticastLeaveAoi()
        {
            var proto = new Proto_LeaveAoi();

            var spawnInfo = new ActorSpawnInfo();
            spawnInfo.ActorType = this._actor.ActorType;

            if (this._actor is ActorUser actorUser)
            {
                spawnInfo.Uid = actorUser.Account.Uid;
            }

            spawnInfo.ActorKey = this._actor.ActorId;
            spawnInfo.Position = this._actor.Position.ToCVector3();
            spawnInfo.Forward = this._actor.Forward.ToCVector3();

            proto.DespawnInfos.Add(spawnInfo);

            this.Multicast(proto);
        }

        public void Multicast(IMessage message)
        {
            foreach (var actor in this._vision)
            {
                if (actor is not ActorUser user)
                {
                    continue;
                }

                user.Account.Send(message);
            }
        }
    }
}
