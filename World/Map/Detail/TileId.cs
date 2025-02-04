namespace Hype.GameServer.World.Map.Detail
{
    public sealed class TileId
    {
        private readonly int _tileX;
        private readonly int _tileZ;
        private readonly long _id;
        
        public TileId(int tileX, int tileZ)
        {
            this._tileX = tileX;
            this._tileZ = tileZ;
            this._id = (long)tileX << 32 | (long)tileZ;
        }

        public int X => this._tileX;
        public int Z => this._tileZ;

        public long Id => this._id;

        public static bool operator ==(TileId lhs, TileId rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TileId lhs, TileId rhs)
        {
            return !lhs.Equals(rhs);
        }

        public void Deconstrutor(out int tileX, out int tileZ)
        {
            tileX = this._tileX;
            tileZ = this._tileZ;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is not TileId tileId)
            {
                return false;
            }

            return tileId._id == this._id;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
