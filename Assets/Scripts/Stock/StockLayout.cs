using System;
using UnityEngine;

namespace Entrepot.Stock
{
    /// <summary>
    /// Géométrie du X-PTS : convertit une adresse de place en position dans la scène.
    /// Toutes les valeurs sont en mètres et viennent des racks posés dans la scène Main.
    /// </summary>
    [Serializable]
    public sealed class StockLayout
    {
        [Header("Allées")]
        [Tooltip("Axe des rails de la première allée (z).")]
        public float FirstAisleZ = 0.200f;
        [Tooltip("Distance entre deux axes d'allées (racks dos à dos).")]
        public float AislePitch = 3.932f;
        [Tooltip("Distance entre l'axe des rails et le centre d'un rack.")]
        public float RackOffset = 1.25f;

        [Header("Le long de l'allée (x)")]
        [Tooltip("Centre du premier rack.")]
        public float FirstRackX = 0.836f;
        public float RackPitch = 9.90f;
        public int BaysPerRack = 6;
        public float BayPitch = 1.65f;
        public int ColumnsPerBay = 3;
        public float ColumnPitch = 0.50f;

        [Header("Hauteur (y) et profondeur")]
        [Tooltip("Dessus du premier plateau.")]
        public float FirstLevelY = 0.548f;
        public float LevelPitch = 0.59f;
        [Tooltip("Distance entre le centre du rack et le centre d'un bac (avant ou arrière).")]
        public float DepthOffset = 0.30f;

        public int PositionsPerRack => BaysPerRack * ColumnsPerBay;

        public float AisleZ(int aisle) => FirstAisleZ + aisle * AislePitch;

        /// <summary>Position du pivot d'un bac (centre du fond) posé à cette adresse.</summary>
        public Vector3 SlotPosition(SlotAddress a)
        {
            int rack = a.Position / PositionsPerRack;
            int bay = a.Position % PositionsPerRack / ColumnsPerBay;
            int column = a.Position % ColumnsPerBay;

            float x = FirstRackX + rack * RackPitch
                      + (bay - (BaysPerRack - 1) * 0.5f) * BayPitch
                      + (column - (ColumnsPerBay - 1) * 0.5f) * ColumnPitch;
            float y = FirstLevelY + a.Level * LevelPitch;

            float aisleZ = AisleZ(a.Aisle);
            float towardAisle = a.Side == SlotAddress.SideSouth ? +1f : -1f;
            float rackZ = aisleZ - towardAisle * RackOffset;
            float z = rackZ + towardAisle * (a.Depth == SlotAddress.DepthFront ? DepthOffset : -DepthOffset);

            return new Vector3(x, y, z);
        }
    }
}
