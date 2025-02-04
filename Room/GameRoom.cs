namespace Hype.GameServer.Room
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using Common.Logging;
    using Common.Util;
    using GameProto;
    using Google.Protobuf;
    using Hype.Elfang.Core.Timer;
    using Hype.Elfang.Messaging;
    using Hype.Elfang.Messaging.Detail;
    using Hype.GameServer.Collision;
    using Hype.GameServer.Extension;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.World.Map;

    public sealed class GameRoom : IWorkerJobActor<GameRoom>, IDisposable
    {
        private readonly RoomId _roomId = KeyGenerator.UniqueKey();
        private readonly WorkerJobDispatcher _jobDispacher = new WorkerJobDispatcher(useHistory: false);
        private readonly Dictionary<ActorId, ActorUser> _actorUserMap = new Dictionary<ActorId, ActorUser>();
        private readonly Dictionary<ActorId, ActorMonster> _actorMonsterMap = new Dictionary<ActorId, ActorMonster>();
        private readonly TimerWheel _timerWheel;
        private readonly ITopography _topography;

        private long _frameTimerKey;

        public GameRoom(TimerWheel timerWheel, ITopography topography)
        {
            this._timerWheel = timerWheel;
            this._topography = topography;

            ////var position1 = new Vector3(x: -30.0f, y: 0, z: 44.0f);
            ////this.SpawnMonster(position: position1, forward: forward, radius: 7.0f, height: 18.0f, speed: 1, viewingAngle: 120);
        }

        public RoomId RoomId => this._roomId;
        public int UserCount => this._actorUserMap.Count;
        public GameRoom Owner => this;

        public void Initialize()
        {
            var position = new Vector3(x: 102, y: 0, z: 30); //// new Vector3(x: -10, y: 0, z: 44.0f);
            var forward = new Vector3(x: 0, y: 0, z: 1.0f);
            this.SpawnMonster(position: position, forward: forward, radius: 7.0f, height: 18.0f, speed: 1, viewingAngle: 120);
        }

        public void ExecuteFrame()
        {
            this._frameTimerKey = this._timerWheel.AddTask(
                isRepeat: true,
                action: this.PostUpdateFrame);
        }

        public Future<GameRoom> PostEnter(ActorUser actorUser)
        {
            return this.PostFuture(self =>
            {
                actorUser.SetRoom(this);
                actorUser.BeginState();

                var result = this._topography.CollisionGrid.Add(actorUser);
                Debug.Assert(result, "CollisionGrid.Add is true");

                this._topography.AoiGrid.Add(actorUser);

                result = this._actorUserMap.TryAdd(actorUser.ActorId, actorUser);
                Debug.Assert(result, "_actorUserMap.TryAdd is true");

                return this;

                //// actorUser.Aoi.MulticastEnterAoi();
                //// AoiGrid.Add 타이밍에 다른 유저들에게 나의 스폰 정보를 알린다.
                // 다른 액터들에게 나의 입장을 알린다.
            });
            //// TODO: actorUser에 대한 상태를 스폰 대기 상태로 만들고 클라로부터 스폰이 완료되면 Active 상태로만들어야 함.
        }

        public void PostLeave(ActorUser actorUser)
        {
            this.Post(self =>
            {
                this._topography.CollisionGrid.Remove(actorUser);
                this._topography.AoiGrid.Remove(actorUser);

                var result = self._actorUserMap.Remove(actorUser.ActorId, out _);
                Debug.Assert(result, "_actorUserMap.Remove is true");

                actorUser.SetRoom(null);

                actorUser.Aoi.MulticastLeaveAoi();

                ////if (self._actorUserMap.Count == 0)
                ////{
                ////    GameRoomManager.Instance.PostRemoveRoom(this._roomKey);
                ////}
                //// GameRoom이 방을 제거할 권한이 없으므로, GameRoomManager에게 방을 제거하도록 추후 맡긴다.
            });
        }

        public void BroadcastToUser(IMessage message, long exceptActorKey = 0)
        {
            foreach (var actorUser in this._actorUserMap.Values)
            {
                if (actorUser.ActorId == exceptActorKey)
                {
                    continue;
                }

                actorUser.Account.Send(message);
            }
        }

        public void PostUpdateFrame(long delta)
        {
            this.Post(self =>
            {
                self.OnUpdate(delta);
            });
        }

        public WorkerJobDispatcher GetDispatcher()
        {
            return this._jobDispacher;
        }

        public void Dispose()
        {
            this._timerWheel.RemoveTask(this._frameTimerKey, onRemoveCompleted: null);
            this._jobDispacher.Dispose();
        }

        private void OnUpdate(long delta)
        {
            foreach (ActorUser user in this._actorUserMap.Values)
            {
                user.UpdateFrame(delta);
            }

            foreach (ActorMonster monster in this._actorMonsterMap.Values)
            {
                monster.UpdateFrame(delta);
            }
        }

        private void SpawnMonster(in Vector3 position, in Vector3 forward, float radius, float height, float speed, float viewingAngle)
        {
            var tempMonsterCollider = new CylinderColider(radius: radius, height: height, this._topography);

            var actorMonster = new ActorMonster(
                pos: position,
                collider: tempMonsterCollider,
                speed: speed,
                viewingAngle: viewingAngle,
                attackRange: 17,
                this._topography);
            actorMonster.SetForward(forward);
            actorMonster.SetRoom(this);

            var result = this._topography.CollisionGrid.Add(actorMonster);
            Debug.Assert(result, "CollisionGrid.Add is true");

            Log.Info($"Monster => Tile, x:{actorMonster.CollisionTile!.X}, z:{actorMonster.CollisionTile!.Z}");

            this._topography.AoiGrid.Add(actorMonster);
            this._actorMonsterMap.Add(actorMonster.ActorId, actorMonster);

            actorMonster.BeginState();

            var targetPos = new Vector3(x: 70, y: 0, z: 130);
            actorMonster.SetTargetPos(targetPos);
            actorMonster.Fsm.ChangeState(CommonProto.FsmStateType.Move);
        }
    }
}
