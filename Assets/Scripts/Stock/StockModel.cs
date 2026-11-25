using System;
using System.Collections.Generic;

namespace Entrepot.Stock
{
    /// <summary>
    /// Modèle de données du stock, sans dépendance à Unity (testable seul).
    /// La matrice est un tableau à plat : place → identifiant de bac (0 = place vide).
    /// La table <see cref="_bins"/> fait la correspondance identifiant → bac.
    /// </summary>
    public sealed class StockModel
    {
        public const int Sides = 2;
        public const int Depths = 2;

        public int Aisles { get; }
        public int Positions { get; }
        public int Levels { get; }
        public int SlotCount => _slots.Length;
        public int BinCount { get; private set; }

        /// <summary>Augmente à chaque modification. Le rendu compare ce numéro pour savoir s'il doit se reconstruire.</summary>
        public int Version { get; private set; }

        private readonly int[] _slots;
        private readonly List<Bin> _bins = new List<Bin> { null }; // index 0 réservé

        public StockModel(int aisles, int positions, int levels)
        {
            Aisles = aisles; Positions = positions; Levels = levels;
            _slots = new int[aisles * Sides * positions * levels * Depths];
        }

        // --- Adressage -----------------------------------------------------------
        // Ordre : allée → niveau → côté → position → profondeur.
        // - Une allée + un niveau = le territoire d'une navette = un bloc continu du tableau.
        // - Profondeur en dernier : l'avant (index pair) et l'arrière (index impair) d'une case sont voisins.

        /// <summary>Nombre de places servies par une navette (une allée, un niveau).</summary>
        public int SlotsPerLevel => Sides * Positions * Depths;

        public int Index(SlotAddress a) =>
            (((a.Aisle * Levels + a.Level) * Sides + a.Side) * Positions + a.Position) * Depths + a.Depth;

        public SlotAddress Address(int index)
        {
            int depth = index % Depths; index /= Depths;
            int position = index % Positions; index /= Positions;
            int side = index % Sides; index /= Sides;
            int level = index % Levels; index /= Levels;
            return new SlotAddress(index, side, position, level, depth);
        }

        /// <summary>Premier index du territoire d'une navette ; il s'étend sur <see cref="SlotsPerLevel"/> places.</summary>
        public int LevelStart(int aisle, int level) => (aisle * Levels + level) * SlotsPerLevel;

        // --- Lecture -------------------------------------------------------------

        public int GetBinIdAt(int index) => _slots[index];
        public int GetBinId(SlotAddress a) => _slots[Index(a)];
        public Bin GetBin(int id) => id > 0 && id < _bins.Count ? _bins[id] : null;
        public Bin GetBin(SlotAddress a) => GetBin(GetBinId(a));
        public bool IsEmpty(SlotAddress a) => GetBinId(a) == 0;

        /// <summary>Une place arrière est bloquée si un bac occupe la place avant.</summary>
        public bool IsBlocked(SlotAddress a) => a.Depth == SlotAddress.DepthBack && !IsEmpty(a.Front);

        // --- Écriture ------------------------------------------------------------

        /// <summary>Enregistre un nouveau bac dans la table et retourne son identifiant.</summary>
        public int Register(Bin bin)
        {
            bin.Id = _bins.Count;
            _bins.Add(bin);
            return bin.Id;
        }

        /// <summary>Pose un bac sur une place vide.</summary>
        public void Place(SlotAddress a, int binId, bool notify = true)
        {
            int i = Index(a);
            if (_slots[i] != 0) throw new InvalidOperationException($"Place {a} déjà occupée par le bac {_slots[i]}.");
            if (GetBin(binId) == null) throw new ArgumentException($"Bac {binId} inconnu.");
            _slots[i] = binId;
            BinCount++;
            if (notify) NotifyChanged();
        }

        /// <summary>Retire le bac d'une place et retourne son identifiant. Refuse une place arrière bloquée.</summary>
        public int Remove(SlotAddress a)
        {
            if (IsBlocked(a)) throw new InvalidOperationException($"Place {a} bloquée par le bac avant.");
            int i = Index(a);
            int id = _slots[i];
            if (id == 0) throw new InvalidOperationException($"Place {a} vide.");
            _slots[i] = 0;
            BinCount--;
            NotifyChanged();
            return id;
        }

        /// <summary>À appeler après un changement de contenu (quantités) pour mettre à jour le rendu.</summary>
        public void NotifyChanged() => Version++;
    }
}
