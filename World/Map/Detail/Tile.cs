namespace Hype.GameServer.World.Map.Detail
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using Common.Logging;
    using Hype.Elfang.Core.Util;
    using Hype.GameServer.InGame.Actors;
    using Org.BouncyCastle.Asn1.Cms;
    using UnityEngine;

    /// <summary>
    /// cell 보다 더큰 단위.
    /// </summary>
    public class Tile
    {
        private readonly TileId _tileId;
        private Dictionary<RoomId, Dictionary<ActorId, Actor>> _actorRoomMap = new ();

        public Tile(int tileX, int tileZ)
        {
            this._tileId = new TileId(tileX, tileZ);
        }

        public int X => this._tileId.X;
        public int Z => this._tileId.Z;

        public static bool operator ==(Tile lhs, Tile rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Tile lhs, Tile rhs)
        {
            return !lhs.Equals(rhs);
        }

        public bool RemoveActor(Actor actor)
        {
            if (this._actorRoomMap.ContainsKey(actor.GameRoom!.RoomId) == false)
            {
                return false;
            }

            return this._actorRoomMap[actor.GameRoom.RoomId].Remove(actor.ActorId, out _);
        }

        public bool AddActor(Actor actor)
        {
            if (this._actorRoomMap.ContainsKey(actor.GameRoom!.RoomId) == false)
            {
                this._actorRoomMap.Add(actor.GameRoom.RoomId, new Dictionary<long, Actor>(capacity: 50));
            }

            return this._actorRoomMap[actor.GameRoom.RoomId].TryAdd(actor.ActorId, actor);
        }

        public IEnumerable<Actor> GetActors(long roomKey)
        {
            if (this._actorRoomMap.ContainsKey(roomKey) == false)
            {
                return Enumerable.Empty<Actor>();
            }

            return this._actorRoomMap[roomKey].Values;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is not Tile other)
            {
                return false;
            }

            return this._tileId == other._tileId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
