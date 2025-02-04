namespace Hype.GameServer.InGame.Component
{
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
    using Hype.GameServer.World.Navigation;

    public interface INavigatorComponent : IComponent
    {
        bool IsNavigating { get; }
        bool Navigate(params ICollider[] colliders);
        void MoveNextPath(out bool isReachDestination);
    }

    public sealed class NavigatorComponent : INavigatorComponent
    {
        /// <summary>
        /// Navi에서 길 찾기 도중 회피할 경로들.
        /// </summary>
        private readonly List<Vector3> _closePaths = new ();

        /// <summary>
        /// Navi에서 길 찾기 도중 회피할 Collider 목록들.
        /// </summary>
        private readonly HashSet<ICollider> _colliderSet = new ();
        private readonly INavigator _navigaotr;
        private readonly Actor _actor;

        private Vector2[] _paths = new Vector2[0];
        private int _pathIndex = 0;
        private Vector3 _destination;

        public NavigatorComponent(Actor actor, INavigator navigator)
        {
            this._actor = actor;
            this._navigaotr = navigator;
        }

        public bool IsNavigating => this._pathIndex < this._paths.Length;

        /// <summary>
        /// 목표 지점을 Navigation 기반으로 이동한다.
        /// </summary>
        /// <param name="colliders">장애물로 인식하는 collider 객체들.</param>
        /// <returns>True: 이동 가능, False: 이동 불가능.</returns>
        public bool Navigate(params ICollider[] colliders)
        {
            var start = this._actor.Position;
            if (this.IsNavigating == false)
            {
                this.Clear();
                this._destination = this._actor.TargetPos;
            }
            else
            {
                var result = this.TryGetPrevNaviPath(out var prevPosition);
                if (result == false)
                {
                    this._actor.Fsm.ChangeState(FsmStateType.Idle);
                    return false;
                }

                Log.Error($"rollback position, actorId: {this._actor.ActorId}, position: {prevPosition}");
                this._closePaths.Add(this._actor.TargetPos);
                //// 목표했던 목적지점은 장애물로 등록.

                start = prevPosition.ToVector3(y: this._actor.Position.Y);
                // 이전 위치로 롤백.
            } // 정해진 경로를 따라가다 장애물을 만남.

            this._actor.Fsm.ChangeState(FsmStateType.Idle);

            foreach (var collider in colliders)
            {
                if (this._colliderSet.Contains(collider))
                {
                    continue;
                }

                this._colliderSet.Add(collider);
            }

            var searchResult = this.SearchPath(
                start: start,
                destination: this._destination,
                closePaths: this._closePaths,
                colliders: this._colliderSet,
                out var paths);
            if (searchResult == false)
            {
                this.Clear();
                return false;
            }

            this._paths = paths;
            this._pathIndex = 0;

            var targetPos = paths.First();
            var targetPosVec3 = targetPos.ToVector3(y: this._actor.Position.Y);
            this._actor.SetTargetPos(targetPosVec3);
            this._actor.Fsm.ChangeState(FsmStateType.Move);

            return true;
        }

        /// <summary>
        /// Navigation의 다음 경로로 이동한다.
        /// </summary>
        /// <param name="isReadDestination">목적지 도착 여부.</param>
        public void MoveNextPath(out bool isReadDestination)
        {
            isReadDestination = false;
            
            ++this._pathIndex;
            if (this._pathIndex >= this._paths.Length)
            {
                isReadDestination = true;
                return;
            }

            var targetPos = this._paths[this._pathIndex];
            var targetPosVec3 = targetPos.ToVector3(y: this._actor.Position.Y);
            this._actor.SetTargetPos(targetPosVec3);
            this._actor.Fsm.ChangeState(FsmStateType.Move);

            return;
        }
        
        private bool TryGetPrevNaviPath(out Vector2 prevPath)
        {
            prevPath = Vector2.Zero;
            var prevIndex = this._pathIndex - 1;
            if (prevIndex < 0)
            {
                return false;
            }

            prevPath = this._paths[prevIndex];
            return true;
        }

        private bool SearchPath(in Vector3 start, in Vector3 destination, IEnumerable<Vector3> closePaths, IEnumerable<ICollider> colliders, out Vector2[] paths)
        {
            var startVec2 = start.ToVector2();
            var destinationVec2 = destination.ToVector2();
            var closePathsVec2 = closePaths.Select(e => e.ToVector2());

            paths = this._navigaotr.FindPath(
                start: startVec2,
                destination: destinationVec2,
                colliderRadius: this._actor.Colider.Radius,
                closePathsVec2,
                colliders);
            if (paths.Length == 0)
            {
                return false;
            }

            return true;
        }

        private void Clear()
        {
            this._closePaths.Clear();
            this._colliderSet.Clear();
            this._pathIndex = 0;
            this._paths = new Vector2[0];
            this._destination = Vector3.Zero;
        }
    }
}
