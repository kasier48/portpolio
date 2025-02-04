namespace Hype.GameServer.InGame.Fsm.MonsterState
{
    using Common.Logging;
    using CommonProto;
    using Hype.GameServer.Collision;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Fsm.State;

    public class ActorMonsterMoveState : ActorMoveState
    {
        private ActorMonster _actor;

        public ActorMonsterMoveState(ActorMonster actorMonster, ICollisionGrid collisionGrid)
            : base(actorMonster, collisionGrid)
        {
            this._actor = actorMonster;
        }

        public override void Begin()
        {
            base.Begin();
        }

        public override void Update(long delta)
        {
            base.Update(delta);
        }

        public override void End()
        {
            base.End();
        }

        protected override void OnReached(Actor? targetActor)
        {
            this._actor.Fsm.ChangeState(FsmStateType.Idle);
            ////if (targetActor != null)
            ////{
            ////    // this._actor.Fsm.ChangeState(FsmStateType.Attack);
            ////    return;
            ////} // 목표 followActor에 도달.

            //// this._actor.Fsm.ChangeState(FsmStateType.Idle);
        }

        protected override void OnActorCollision(Actor collisionActor)
        {
            Log.Error($"monster actor collision, actorId: {this._actor.ActorId}");

            if (collisionActor.IsUser)
            {
                var result = this._navigatorComponent.Navigate(colliders: collisionActor.Colider);
                if (result == false)
                {
                    Log.Error($"MoveAlongNavi is failed, actorId: {this._actor.ActorId}");
                    return;
                }

                return;
            }

            this._actor.Fsm.ChangeState(FsmStateType.Idle);
            ////if (collisionActor.IsUser)
            ////{
            ////    this._actor.Fsm.ChangeState(FsmStateType.Attack);
            ////    return;
            ////}

            //// this._actor.Fsm.ChangeState(FsmStateType.Idle);
        }

        protected override void OnBarrierCollision()
        {
            Log.Error($"monster barrier collision, actorId: {this._actor.ActorId}");

            var result = this._navigatorComponent.Navigate();
            if (result == false)
            {
                Log.Error($"MoveAlongNavi is failed, actorId: {this._actor.ActorId}");
                return;
            }
        }

        protected override void OnActorFollow(Actor followActor)
        {
            Log.Info($"monster follow actorKey: {followActor.ActorId}");

            //// this._actor.SetTargetActor(followActor);
            
            if (followActor.IsMonster)
            {
                return;
            }

            this._actor.SetTargetActor(followActor);
            //// var targetPos = this._actor.GetBoundaryTargetPosition(followActor.Position);
            // followActor.Position
            this._actor.SetTargetPos(followActor.Position);
            this._actor.Fsm.ChangeState(FsmStateType.Move);
            // this._actor.Aoi.MulticastFollowActor(followActor);
        }
    }
}
