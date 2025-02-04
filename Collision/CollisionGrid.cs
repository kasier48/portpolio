namespace Hype.GameServer.Collision
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using Hype.Elfang.Core.Util;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.World.Map;
    using Hype.GameServer.World.Map.Detail;

    public interface ICollisionGrid
    {
        bool Add(Actor actor);
        Tile? GetTile(in Vector3 position);
        void Update(Actor actor, in Vector3 newPosition);
        void Remove(Actor actor);
        IEnumerable<Actor> GetNearActors(int tileX, int tileZ, long roomKey, long exceptActorKey);
    }

    public sealed class CollisionGrid : ICollisionGrid
    {
        private readonly TileGrid _tileGrid;

        public CollisionGrid(int worldXStart, int worldZStart, int worldXEnd, int worldZEnd, int width)
        {
            this._tileGrid = new TileGrid(worldXStart, worldZStart, worldXEnd, worldZEnd, width);
        }

        public bool Add(Actor actor)
        {
            var result = this._tileGrid.AddActor(actor, out var tile);
            if (result == false)
            {
                return false;
            }

            actor.SetCollisionTile(tile);

            return true;
        }

        public Tile? GetTile(in Vector3 position)
        {
            return this._tileGrid.GetTile(position);
        }

        public void Update(Actor actor, in Vector3 newPosition)
        {
            var newTile = this.GetTile(newPosition);
            if (newTile is null)
            {
                return;
            }

            var prevTile = actor.CollisionTile;
            Debug.Assert(prevTile is not null, "prevTile is not null");

            if (newTile == prevTile!)
            {
                return;
            }

            var result = prevTile!.RemoveActor(actor);
            Debug.Assert(result, "RemoveActor is true");

            result = newTile.AddActor(actor);
            Debug.Assert(result, "AddActor is true");

            actor.SetCollisionTile(newTile);
        }

        public IEnumerable<Actor> GetNearActors(int tileX, int tileZ, long roomKey, long exceptActorKey)
        {
            return this._tileGrid.GetNearActors(tileX, tileZ, roomKey, exceptActorKey);
        }

        public void Remove(Actor actor)
        {
            Debug.Assert(actor.CollisionTile is not null, "CollisionTile  is not null");

            if (actor.CollisionTile is null)
            {
                return;
            }

            var result = actor.CollisionTile.RemoveActor(actor);
            Debug.Assert(result, "result is true");

            actor.SetCollisionTile(null);
        }
    }
}
