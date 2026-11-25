using System;

namespace Entrepot.Stock
{
    /// <summary>
    /// Adresse d'une place de stockage dans le X-PTS.
    /// Une place peut recevoir un seul bac.
    /// </summary>
    [Serializable]
    public struct SlotAddress : IEquatable<SlotAddress>
    {
        /// <summary>Allée (0 = Allee_01).</summary>
        public int Aisle;
        /// <summary>Côté de l'allée : 0 = sud, 1 = nord.</summary>
        public int Side;
        /// <summary>Position le long de l'allée : rack × 18 + travée × 3 + colonne.</summary>
        public int Position;
        /// <summary>Niveau (0 = plateau le plus bas).</summary>
        public int Level;
        /// <summary>Profondeur : 0 = avant (côté allée), 1 = arrière.</summary>
        public int Depth;

        public const int SideSouth = 0, SideNorth = 1;
        public const int DepthFront = 0, DepthBack = 1;

        public SlotAddress(int aisle, int side, int position, int level, int depth)
        {
            Aisle = aisle; Side = side; Position = position; Level = level; Depth = depth;
        }

        /// <summary>La place avant de la même case (celle qui bloque une place arrière).</summary>
        public SlotAddress Front => new SlotAddress(Aisle, Side, Position, Level, DepthFront);

        public bool Equals(SlotAddress o) =>
            Aisle == o.Aisle && Side == o.Side && Position == o.Position && Level == o.Level && Depth == o.Depth;
        public override bool Equals(object obj) => obj is SlotAddress o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Aisle, Side, Position, Level, Depth);

        public override string ToString() =>
            $"A{Aisle + 1}-N{Level + 1:00}-{(Side == SideSouth ? "Sud" : "Nord")}-P{Position:00}-{(Depth == DepthFront ? "Av" : "Ar")}";
    }
}
