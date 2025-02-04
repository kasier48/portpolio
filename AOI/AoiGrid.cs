namespace Hype.GameServer.InGame.Aoi
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Linq;
    using System.Numerics;
    using GameProto;
    using Google.Protobuf;
    using Hype.Elfang.Core.Util;
    using Hype.GameServer.Extension;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;
    using Microsoft.VisualBasic;
    using MySqlX.XDevAPI.Common;

    public sealed class AoiGrid
    {
        private readonly TileGrid _tileGrid;

        public AoiGrid(int worldXStart, int worldZStart, int worldXEnd, int worldZEnd, int width) 
        {
            this._tileGrid = new TileGrid(worldXStart, worldZStart, worldXEnd, worldZEnd, width);
        }

        public void Add(Actor actor)
        {
            var aoiTile = this._tileGrid.GetTile(actor.Position);
            Debug.Assert(aoiTile is not null, "tile is not null");

            var nearActors = this._tileGrid.GetNearActors(aoiTile.X, aoiTile.Z, actor.GameRoom!.RoomId, exceptActorKey: 0);
            this.EnterAoi(actor, nearActors);

            var result = aoiTile.AddActor(actor);
            Debug.Assert(result, "AddActor is true");

            actor.SetAoiTile(aoiTile);
        }

        public void Remove(Actor actor)
        {
            Debug.Assert(actor.AoiTile is not null, "AoiTile is not null");

            var nearActors = this._tileGrid.GetNearActors(actor.AoiTile.X, actor.AoiTile.Z, actor.GameRoom!.RoomId, exceptActorKey: actor.ActorId);
            this.LeaveAoi(actor, nearActors);

            var result = actor.AoiTile.RemoveActor(actor);
            Debug.Assert(result, "RemoveActor is true");

            actor.SetAoiTile(null);
        }

        public void Update(Actor actor, in Vector3 newPosition)
        {
            var newTile = this._tileGrid.GetTile(newPosition);
            if (newTile is null)
            {
                return;
            }

            var prevTile = actor.AoiTile;
            Debug.Assert(prevTile is not null, "prevTile is not null");

            if (prevTile is null)
            {
                return;
            }

            if (newTile == prevTile)
            {
                return;
            }

            var result = prevTile.RemoveActor(actor);
            Debug.Assert(result, "RemoveActor is true");

            var actorsByPrevTile = this._tileGrid.GetNearActors(prevTile.X, prevTile.Z, actor.GameRoom!.RoomId, exceptActorKey: 0);
            var actorsByNewTile = this._tileGrid.GetNearActors(newTile.X, newTile.Z, actor.GameRoom!.RoomId, exceptActorKey: 0);

            var actorsOutOfAoi = actorsByPrevTile.Except(actorsByNewTile);
            this.LeaveAoi(actor, actorsOutOfAoi);

            var actorsEnteredAoi = actorsByNewTile.Except(actorsByPrevTile);
            this.EnterAoi(actor, actorsEnteredAoi);

            result = newTile.AddActor(actor);
            Debug.Assert(result, "AddActor is true");

            actor.SetAoiTile(newTile);
        }

        private void LeaveAoi(Actor actor, IEnumerable<Actor> actorsOutOfAoi)
        {
            if (actorsOutOfAoi.Any() == false)
            {
                return;
            }

            var protoLeaveAoiToOthers = new Proto_LeaveAoi();
            protoLeaveAoiToOthers.DespawnInfos.Add(actor.ToSpawnInfo());

            foreach (var removeActor in actorsOutOfAoi)
            {
                actor.Aoi.Leave(removeActor);
                removeActor.Aoi.Leave(actor);

                if (removeActor is ActorUser removeActorUser)
                {
                    removeActorUser.Account.Send(protoLeaveAoiToOthers);
                }
            }

            if (actor is ActorUser actorUser)
            {
                var protoLeaveAoiToMe = new Proto_LeaveAoi();
                protoLeaveAoiToMe.DespawnInfos.AddRange(actorsOutOfAoi.Select(e => e.ToSpawnInfo()));

                actorUser.Account.Send(protoLeaveAoiToMe);
            }
        }

        private void EnterAoi(Actor actor, IEnumerable<Actor> actorsEnteredAoi)
        {
            if (actorsEnteredAoi.Any() == false)
            {
                return;
            }

            var protoEnterAoiToOthers = new Proto_EnterAoi();
            protoEnterAoiToOthers.SpawnInfos.Add(actor.ToSpawnInfo());

            foreach (var enteredActor in actorsEnteredAoi)
            {
                actor.Aoi.Enter(enteredActor);
                enteredActor.Aoi.Enter(actor);

                if (enteredActor is ActorUser enteredActorUser)
                {
                    enteredActorUser.Account.Send(protoEnterAoiToOthers);
                }
            }

            if (actor is ActorUser actorUser)
            {
                var protoEnterAoi = new Proto_EnterAoi();
                protoEnterAoi.SpawnInfos.AddRange(actorsEnteredAoi.Select(e => e.ToSpawnInfo()));

                actorUser.Account.Send(protoEnterAoi);
            }
        }
    }
}
