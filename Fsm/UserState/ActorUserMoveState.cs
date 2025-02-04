namespace Hype.GameServer.InGame.Fsm.UserState
{
    using CommonProto;
    using Hype.GameServer.Collision;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.InGame.Fsm.State;

    public class ActorUserMoveState : ActorMoveState
    {
        private readonly ActorUser _actor;

        public ActorUserMoveState(ActorUser actorUser, ICollisionGrid collisionGrid)
            : base(actorUser, collisionGrid)
        {
            this._actor = actorUser;
        }

        public override void Begin()
        {
            base.Begin();
        }

        public override void Update(long delta)
        {
            //// Log.Debug($"actorUser({this._actor.ActorKey}), pos({this._actor.Position})");

            base.Update(delta);
        }

        public override void End()
        {
            base.End();
        }

        protected override void OnReached(Actor? targetActor)
        {
            this._actor.Fsm.ChangeState(FsmStateType.Idle);
        }

        protected override void OnActorCollision(Actor collisionActor)
        {
            //// this._actor.Fsm.ChangeState(FsmStateType.Idle);
            this._actor.Fsm.ChangeState(FsmStateType.Idle);
            this.SendOnMoveUnit(this._actor, ActorMoveResultType.ActorCollision);
        }

        protected override void OnBarrierCollision()
        {
            this._actor.Fsm.ChangeState(FsmStateType.Idle);
            this.SendOnMoveUnit(this._actor, ActorMoveResultType.BarrierCollision);
        }

        protected override void OnActorFollow(Actor followActor)
        {
        }
    }
}
