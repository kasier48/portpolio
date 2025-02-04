namespace Hype.GameServer.World.Map
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Hype.GameServer.InGame.Actors;
    using Hype.GameServer.World.Map.Detail;

    public class TileGrid
    {
        private static int[,] matrix = new int[,]
        {
            { -1, 0 },
            { -1, 1 },
            { -1, -1 },
            { 0, 0 },
            { 0, 1 },
            { 0, -1 },
            { 1, 0 },
            { 1, 1 },
            { 1, -1 },
        };

        private readonly int _worldXStart;
        private readonly int _worldZStart;
        private readonly int _width;
        private readonly int _tileXSize;
        private readonly int _tileZSize;

        // z axis, x axis
        private readonly Tile[,] _grid;

        public TileGrid(int worldXStart, int worldZStart, int worldXEnd, int worldZEnd, int width)
        {
            this._worldXStart = worldXStart;
            this._worldZStart = worldZStart;
            this._width = width;

            this._tileXSize = ((worldXEnd - worldXStart) / width) + 1;
            this._tileZSize = ((worldZEnd - worldZStart) / width) + 1;
            this._grid = new Tile[this._tileZSize, this._tileXSize];

            for (int z = 0; z < this._tileZSize; ++z)
            {
                for (int x = 0; x < this._tileXSize; ++x)
                {
                    this._grid[z, x] = new Tile(x, z);
                }
            }
        }

        public Tile? GetTile(in Vector3 position)
        {
            int tileZ = (int)(Math.Abs(this._worldZStart) + position.Z) / this._width;
            int tileX = (int)(Math.Abs(this._worldXStart) + position.X) / this._width;

            return this.GetTile(tileX, tileZ);
        }

        public Tile? GetTile(int tileX, int tileZ)
        {
            if (this.IsOutOfEndSize(tileX, tileZ))
            {
                return null;
            }

            return this._grid[tileZ, tileX];
        }

        public bool AddActor(Actor actor)
        {
            var tileInfo = this.GetTile(actor.Position);
            if (tileInfo is null)
            {
                return false;
            }

            return tileInfo.AddActor(actor);
        }

        public bool AddActor(Actor actor, out Tile tile)
        {
            tile = null!;

            var curTile = this.GetTile(actor.Position);
            if (curTile is null)
            {
                return false;
            }

            if (curTile.AddActor(actor) is false)
            {
                return false;
            }

            tile = curTile;
            return true;
        }

        public bool RemoveActor(Actor actor)
        {
            var tile = this.GetTile(actor.Position);
            if (tile is null)
            {
                return false;
            }

            return tile.RemoveActor(actor);
        }
            
        public IEnumerable<Actor> GetNearActors(int tileX, int tileZ, long roomKey, long exceptActorKey)
        {
            int rowSize = matrix.GetLength(0);
            for (int row = 0; row < rowSize; ++row)
            {
                int offsetX = matrix[row, 0];
                int nearTileX = tileX + offsetX;

                int offsetZ = matrix[row, 1];
                int nearTileZ = tileZ + offsetZ;

                if (this.IsOutOfEndSize(nearTileX, nearTileZ))
                {
                    continue;
                }

                var nearTile = this._grid[nearTileZ, nearTileX];
                foreach (var other in nearTile.GetActors(roomKey))
                {
                    if (other.ActorId == exceptActorKey)
                    {
                        continue;
                    }

                    yield return other;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfEndSize(int tileX, int tileZ) 
        {
            if (tileX < 0 || tileX >= this._tileXSize || tileZ < 0 || tileZ >= this._tileZSize)
            {
                return true;
            }

            return false;
        }
    }
}
