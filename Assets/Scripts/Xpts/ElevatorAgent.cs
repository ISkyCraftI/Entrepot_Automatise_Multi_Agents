using System.Collections.Generic;
using Entrepot.Stock;
using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>
    /// Élévateur en tête d'allée. Il pilote ses nacelles (« Nacelle de transfert… ») :
    /// une nacelle est réservée par une navette, monte ou descend à son niveau, reçoit le bac,
    /// va au niveau du convoyeur de sortie puis pousse le bac dessus. Le bac roule et quitte l'entrepôt.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElevatorAgent : MonoBehaviour
    {
        public enum NacelleState
        {
            Free,             // libre, à l'arrêt
            MovingToShuttle,  // réservée, va au niveau de la navette
            WaitingLoad,      // au niveau de la navette, attend le bac
            MovingToExit,     // chargée, va au niveau du convoyeur de sortie
            Unloading,        // pousse le bac sur le convoyeur
        }

        /// <summary>Une nacelle et son axe vertical.</summary>
        public sealed class Nacelle
        {
            public Transform Transform;
            public LinearAxis Height;
            public NacelleState State = NacelleState.Free;
            public GameObject Bin;
            /// <summary>Côté de l'allée : −1 = vers −z (sud), +1 = vers +z (nord).</summary>
            public float Side;
            /// <summary>Distance entre l'axe de l'allée et le centre de la nacelle (course de coulisse pour le transfert).</summary>
            public float LateralOffset;
            internal float UnloadStartX;
        }

        [Header("Mouvement des nacelles (données Savoye)")]
        [SerializeField] private float _liftSpeed = 4.5f;
        [SerializeField] private float _liftAcceleration = 6f;

        [Header("Convoyeur de sortie")]
        [Tooltip("Hauteur du dessus des rouleaux du convoyeur de sortie (pivot à 4,1 + 0,533).")]
        [SerializeField] private float _exitTopY = 4.633f;
        [Tooltip("Abscisse où le bac quitte l'entrepôt (bout des 2 convoyeurs).")]
        [SerializeField] private float _exitEndX = -9.2f;
        [SerializeField] private float _conveyorSpeed = 0.5f;
        [Tooltip("Distance parcourue par le bac avant de libérer la nacelle (longueur d'une nacelle).")]
        [SerializeField] private float _clearDistance = 0.85f;

        public int Aisle { get; private set; } = -1;
        public IReadOnlyList<Nacelle> Nacelles => _nacelles;
        /// <summary>Abscisse de transfert : la navette s'arrête ici pour passer le bac.</summary>
        public float TransferX => _nacelles.Count > 0 ? _nacelles[0].Transform.position.x : transform.position.x;

        private readonly List<Nacelle> _nacelles = new List<Nacelle>();
        private readonly List<GameObject> _rolling = new List<GameObject>();
        private StockManager _stock;

        private void Awake()
        {
            _stock = FindAnyObjectByType<StockManager>();
            Aisle = XptsHierarchy.AisleIndexOf(transform);
            float axisZ = _stock != null && Aisle >= 0 ? _stock.Layout.AisleZ(Aisle) : transform.position.z;
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("Nacelle")) continue;
                float dz = t.position.z - axisZ;
                _nacelles.Add(new Nacelle
                {
                    Transform = t,
                    Height = new LinearAxis(t.position.y, _liftSpeed, _liftAcceleration),
                    Side = Mathf.Sign(dz),
                    LateralOffset = Mathf.Abs(dz),
                });
            }
            if (_nacelles.Count == 0) Debug.LogWarning($"{name} : aucune nacelle trouvée.", this);
        }

        // --- API pour les navettes --------------------------------------------------

        /// <summary>
        /// Règle de choix de la nacelle (à remplacer par une stratégie pour l'optimisation) :
        /// la nacelle libre la plus proche en hauteur. Retourne null si aucune n'est libre.
        /// </summary>
        public Nacelle Reserve(float levelY)
        {
            Nacelle best = null;
            foreach (var n in _nacelles)
                if (n.State == NacelleState.Free && (best == null || Mathf.Abs(n.Height.Position - levelY) < Mathf.Abs(best.Height.Position - levelY)))
                    best = n;
            if (best == null) return null;
            best.State = NacelleState.MovingToShuttle;
            best.Height.Target = levelY;
            return best;
        }

        public bool IsReadyToLoad(Nacelle n) => n.State == NacelleState.WaitingLoad;

        /// <summary>La navette a posé le bac sur la nacelle.</summary>
        public void Load(Nacelle n, GameObject bin)
        {
            n.Bin = bin;
            bin.transform.SetParent(n.Transform, true);
            n.State = NacelleState.MovingToExit;
            n.Height.Target = _exitTopY;
        }

        // --- Machine à états des nacelles -----------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var n in _nacelles)
            {
                n.Height.Step(dt);
                var p = n.Transform.position;
                n.Transform.position = new Vector3(p.x, n.Height.Position, p.z);

                switch (n.State)
                {
                    case NacelleState.MovingToShuttle:
                        if (n.Height.AtTarget) n.State = NacelleState.WaitingLoad;
                        break;
                    case NacelleState.MovingToExit:
                        if (n.Height.AtTarget)
                        {
                            n.Bin.transform.SetParent(null, true);
                            n.UnloadStartX = n.Bin.transform.position.x;
                            _rolling.Add(n.Bin);
                            n.State = NacelleState.Unloading;
                        }
                        break;
                    case NacelleState.Unloading:
                        // La nacelle est libre dès que le bac l'a quittée.
                        if (n.Bin == null || n.UnloadStartX - n.Bin.transform.position.x >= _clearDistance)
                        {
                            n.Bin = null;
                            n.State = NacelleState.Free;
                        }
                        break;
                }
            }

            // Bacs sur le convoyeur de sortie : ils roulent vers −x puis quittent l'entrepôt.
            for (int i = _rolling.Count - 1; i >= 0; i--)
            {
                var bin = _rolling[i];
                if (bin == null) { _rolling.RemoveAt(i); continue; }
                bin.transform.position += Vector3.left * (_conveyorSpeed * dt);
                if (bin.transform.position.x <= _exitEndX)
                {
                    _rolling.RemoveAt(i);
                    foreach (var n in _nacelles) if (n.Bin == bin) { n.Bin = null; n.State = NacelleState.Free; }
                    if (_stock != null) _stock.Ship(bin); else Destroy(bin);
                }
            }
        }
    }
}
