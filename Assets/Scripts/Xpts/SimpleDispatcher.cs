using System.Collections.Generic;
using Entrepot.Stock;
using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>
    /// Répartiteur de test (provisoire, en attendant l'ordonnanceur) : chaque navette libre reçoit
    /// l'ordre de sortir un bac accessible, tiré au hasard sur son niveau. Sert à vérifier les animations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimpleDispatcher : MonoBehaviour
    {
        [SerializeField] private bool _active = true;
        [SerializeField] private int _seed = 7;
        [Tooltip("Pause entre deux ordres pour une même navette (s).")]
        [SerializeField] private float _pause = 1f;
        [Tooltip("Nombre maximal de navettes en mouvement en même temps (0 = toutes).")]
        [SerializeField] private int _maxBusy = 0;

        private StockManager _stock;
        private ShuttleAgent[] _shuttles;
        private readonly Dictionary<ShuttleAgent, float> _nextOrder = new Dictionary<ShuttleAgent, float>();
        private readonly List<SlotAddress> _candidates = new List<SlotAddress>();
        private System.Random _rng;

        private void Start()
        {
            _stock = GetComponent<StockManager>();
            if (_stock == null) _stock = FindAnyObjectByType<StockManager>(); // pas de ?? sur un objet Unity
            _shuttles = FindObjectsByType<ShuttleAgent>();
            _rng = new System.Random(_seed);
        }

        private void Update()
        {
            if (!_active || _stock == null || _stock.Model == null) return;
            int busy = 0;
            foreach (var s in _shuttles) if (!s.IsIdle) busy++;

            foreach (var s in _shuttles)
            {
                if (!s.IsIdle || s.Aisle < 0 || s.Level < 0) continue;
                if (_maxBusy > 0 && busy >= _maxBusy) return;
                if (_nextOrder.TryGetValue(s, out float t) && Time.time < t) continue;
                if (TryPick(s, out var slot) && s.Retrieve(slot)) busy++;
                _nextOrder[s] = Time.time + _pause;
            }
        }

        /// <summary>Un bac accessible au hasard sur le territoire de la navette (bloc continu du tableau).</summary>
        private bool TryPick(ShuttleAgent s, out SlotAddress slot)
        {
            var m = _stock.Model;
            _candidates.Clear();
            if (s.Level >= m.Levels || s.Aisle >= m.Aisles) { slot = default; return false; }
            int start = m.LevelStart(s.Aisle, s.Level);
            for (int i = start; i < start + m.SlotsPerLevel; i++)
            {
                if (m.GetBinIdAt(i) == 0) continue;
                var a = m.Address(i);
                if (!m.IsBlocked(a)) _candidates.Add(a);
            }
            if (_candidates.Count == 0) { slot = default; return false; }
            slot = _candidates[_rng.Next(_candidates.Count)];
            return true;
        }
    }
}
