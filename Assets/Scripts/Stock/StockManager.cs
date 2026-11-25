using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entrepot.Stock
{
    /// <summary>
    /// Possède le modèle de stock, le remplit au départ (aléatoire avec graine fixe)
    /// et gère les bacs « en transit » : un bac sorti de sa place devient un vrai GameObject
    /// le temps du trajet (navette, élévateur), puis redevient une simple donnée.
    /// </summary>
    [ExecuteAlways]
    public sealed class StockManager : MonoBehaviour
    {
        [SerializeField] private StockLayout _layout = new StockLayout();

        [Header("Dimensions")]
        [SerializeField, Min(1)] private int _aisles = 2;
        [SerializeField, Min(1)] private int _racksPerAisle = 4;
        [SerializeField, Min(1)] private int _levels = 12;

        [Header("Remplissage initial")]
        [SerializeField, Range(0f, 1f)] private float _initialOccupancy = 0.70f;
        [SerializeField] private int _seed = 42;
        [SerializeField, Min(1)] private int _skuCount = 500;
        [SerializeField, Min(1)] private int _binCapacity = 48;
        [Tooltip("Nombre de compartiments possibles pour un bac (1 = bac simple).")]
        [SerializeField] private int[] _compartmentOptions = { 1, 1, 2, 4 };

        [Header("Modèles 3D des bacs")]
        [Tooltip("Bac à 1 compartiment (07_bac_stockage).")]
        [SerializeField, FormerlySerializedAs("_binPrefab")] private GameObject _singleBinPrefab;
        [Tooltip("Bac à plusieurs compartiments (08_bac_compartimente).")]
        [SerializeField] private GameObject _multiBinPrefab;

        public StockModel Model { get; private set; }
        public StockLayout Layout => _layout;
        public GameObject SingleBinPrefab => _singleBinPrefab;
        public GameObject MultiBinPrefab => _multiBinPrefab;

        /// <summary>Le modèle 3D qui correspond au type du bac.</summary>
        public GameObject PrefabFor(Bin bin) => bin.IsMulti ? _multiBinPrefab : _singleBinPrefab;

        private readonly Dictionary<GameObject, int> _inTransit = new Dictionary<GameObject, int>();

        private void OnEnable() => Build();

        private void OnValidate()
        {
            // Rejoue la génération quand un réglage change dans l'Inspector.
            if (isActiveAndEnabled) Build();
        }

        /// <summary>Crée le stock initial. Même graine → même stock.</summary>
        public void Build()
        {
            Model = new StockModel(_aisles, _racksPerAisle * _layout.PositionsPerRack, _levels);
            var rng = new System.Random(_seed);
            for (int i = 0; i < Model.SlotCount; i++)
            {
                if (rng.NextDouble() >= _initialOccupancy) continue;
                int id = Model.Register(CreateRandomBin(rng));
                Model.Place(Model.Address(i), id, notify: false);
            }
            Model.NotifyChanged();
        }

        private Bin CreateRandomBin(System.Random rng)
        {
            int n = _compartmentOptions.Length > 0 ? Mathf.Max(1, _compartmentOptions[rng.Next(_compartmentOptions.Length)]) : 1;
            int capacity = Mathf.Max(1, _binCapacity / n);
            var compartments = new Compartment[n];
            for (int k = 0; k < n; k++)
                compartments[k] = new Compartment(rng.Next(1, _skuCount + 1), rng.Next(0, capacity + 1), capacity);
            return new Bin(compartments);
        }

        // --- Bacs en transit -------------------------------------------------------

        /// <summary>
        /// Sort le bac d'une place : il disparaît de la matrice (donc du rendu instancié)
        /// et réapparaît comme GameObject au même endroit. Retourne null si la place est vide ou bloquée.
        /// </summary>
        public GameObject TakeOut(SlotAddress a, Transform parent = null)
        {
            if (Model.IsEmpty(a) || Model.IsBlocked(a)) return null;
            var prefab = PrefabFor(Model.GetBin(a));
            int id = Model.Remove(a);
            var go = Instantiate(prefab, _layout.SlotPosition(a), prefab.transform.rotation, parent);
            go.name = $"Bac_{id}";
            _inTransit[go] = id;
            return go;
        }

        /// <summary>Range un bac en transit sur une place vide : le GameObject est détruit, la donnée reprend le relais.</summary>
        public void PutAway(GameObject go, SlotAddress a)
        {
            if (!_inTransit.TryGetValue(go, out int id)) throw new System.ArgumentException($"{go.name} n'est pas un bac en transit.");
            Model.Place(a, id);
            _inTransit.Remove(go);
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        /// <summary>Identifiant du bac porté par un GameObject en transit (0 si inconnu).</summary>
        public int GetTransitBinId(GameObject go) => _inTransit.TryGetValue(go, out int id) ? id : 0;
    }
}
