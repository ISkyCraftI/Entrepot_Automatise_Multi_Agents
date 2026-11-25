using System;
using System.Collections.Generic;
using Entrepot.Stock;
using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>
    /// Navette X-PTS : un agent par niveau et par allée. Elle roule le long de l'allée (axe x),
    /// sort sa coulisse télescopique (3 étages) sur le côté (axe z) et accroche le bac avec 4 doigts
    /// qui pivotent de 90° autour de l'axe vertical.
    ///
    /// Machine à états d'un ordre « sortir un bac » :
    /// Idle → MovingToSlot → ExtendingToSlot → Gripping → RetractingWithBin → MovingToLift
    ///      → WaitingNacelle → ExtendingToNacelle → Releasing → RetractingEmpty → Returning → Idle
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShuttleAgent : MonoBehaviour
    {
        public enum State
        {
            Idle,
            MovingToSlot,
            ExtendingToSlot,
            Gripping,
            RetractingWithBin,
            MovingToLift,
            WaitingNacelle,
            ExtendingToNacelle,
            Releasing,
            RetractingEmpty,
            Returning,
        }

        [Header("Translation (données Savoye)")]
        [SerializeField] private float _travelSpeed = 3f;
        [SerializeField] private float _travelAcceleration = 1.5f;

        [Header("Coulisse télescopique")]
        [SerializeField] private float _armSpeed = 1.0f;
        [SerializeField] private float _armAcceleration = 2.0f;
        [Tooltip("Durée de pivotement des doigts (s).")]
        [SerializeField] private float _fingerTime = 0.3f;

        [Header("Repérage du niveau")]
        [Tooltip("Hauteur du pivot de la navette au niveau 1 ; le niveau n est à cette valeur + n × pas.")]
        [SerializeField] private float _firstLevelPivotY = 0.378f;

        public State CurrentState { get; private set; } = State.Idle;
        public int Aisle { get; private set; } = -1;
        public int Level { get; private set; } = -1;
        public bool IsIdle => CurrentState == State.Idle;
        public SlotAddress CurrentOrder { get; private set; }

        /// <summary>Appelé à chaque changement d'état (journal, KPI, interface).</summary>
        public event Action<ShuttleAgent, State> StateChanged;

        private StockManager _stock;
        private ElevatorAgent _elevator;
        private LinearAxis _x, _arm;
        private float _restX, _armSide, _fingerT, _timer, _levelY;
        private GameObject _bin;
        private ElevatorAgent.Nacelle _nacelle;

        private readonly List<Transform> _stages = new List<Transform>();
        private readonly List<Vector3> _stageRest = new List<Vector3>();
        private readonly List<Transform> _fingers = new List<Transform>();
        private readonly List<Quaternion> _fingerRest = new List<Quaternion>();
        private readonly List<float> _fingerSign = new List<float>();

        // --- Initialisation -------------------------------------------------------------

        private void Start()
        {
            _stock = FindAnyObjectByType<StockManager>();
            Aisle = XptsHierarchy.AisleIndexOf(transform);
            var aisleRoot = FindAisleRoot();
            _elevator = aisleRoot != null ? aisleRoot.GetComponentInChildren<ElevatorAgent>() : null;

            float pitch = _stock != null ? _stock.Layout.LevelPitch : 0.59f;
            Level = Mathf.RoundToInt((transform.position.y - _firstLevelPivotY) / pitch);
            _levelY = _stock != null ? _stock.Layout.FirstLevelY + Level * pitch : 0f;

            _restX = transform.position.x;
            _x = new LinearAxis(_restX, _travelSpeed, _travelAcceleration);
            _arm = new LinearAxis(0f, _armSpeed, _armAcceleration);

            foreach (var prefix in new[] { "Coulisse 1", "Coulisse 2", "Coulisse 3" })
            {
                var s = XptsHierarchy.FindDescendant(transform, prefix);
                if (s == null) { Debug.LogError($"{name} : « {prefix} » introuvable.", this); enabled = false; return; }
                _stages.Add(s); _stageRest.Add(s.localPosition);
            }
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("Doigt")) { _fingers.Add(t); _fingerRest.Add(t.localRotation); }
            DetectFingerDirections();

            if (_stock == null) Debug.LogError($"{name} : aucun StockManager dans la scène.", this);
            if (_elevator == null) Debug.LogWarning($"{name} : aucun ElevatorAgent dans l'allée {Aisle + 1}.", this);
        }

        private Transform FindAisleRoot()
        {
            for (var p = transform; p != null; p = p.parent)
                if (p.name.StartsWith(XptsHierarchy.AislePrefix)) return p;
            return null;
        }

        /// <summary>
        /// Les doigts sont rangés le long du bras. Pour accrocher, chacun pivote de 90° vers l'intérieur
        /// (vers l'axe du bras). On teste les deux sens et on garde celui qui rapproche le doigt de l'axe.
        /// </summary>
        private void DetectFingerDirections()
        {
            var center = _stages[2].position;
            for (int i = 0; i < _fingers.Count; i++)
            {
                var r = _fingers[i].GetComponentInChildren<Renderer>();
                float best = float.MaxValue, sign = 1f;
                foreach (var s in new[] { 1f, -1f })
                {
                    _fingers[i].localRotation = FingerRotation(i, s, 1f);
                    float d = r != null ? Mathf.Abs(r.bounds.center.x - center.x) : 0f;
                    if (d < best) { best = d; sign = s; }
                }
                _fingerSign.Add(sign);
                _fingers[i].localRotation = _fingerRest[i];
            }
        }

        private Quaternion FingerRotation(int i, float sign, float t)
        {
            var axis = _fingers[i].parent.InverseTransformDirection(Vector3.up);
            return Quaternion.AngleAxis(sign * 90f * t, axis) * _fingerRest[i];
        }

        // --- API ------------------------------------------------------------------------

        /// <summary>Donne l'ordre de sortir le bac de cette place. Refusé si la navette est occupée ou si la place n'est pas accessible.</summary>
        public bool Retrieve(SlotAddress slot)
        {
            if (!IsIdle || _stock == null || _elevator == null) return false;
            if (slot.Aisle != Aisle || slot.Level != Level) return false;
            if (_stock.Model.IsEmpty(slot) || _stock.Model.IsBlocked(slot)) return false;
            CurrentOrder = slot;
            _x.Target = _stock.Layout.SlotPosition(slot).x;
            SetState(State.MovingToSlot);
            return true;
        }

        // --- Machine à états --------------------------------------------------------------

        private void SetState(State s)
        {
            CurrentState = s;
            _timer = 0f;
            StateChanged?.Invoke(this, s);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _timer += dt;
            _x.Step(dt);
            _arm.Step(dt);

            switch (CurrentState)
            {
                case State.MovingToSlot:
                    if (_x.AtTarget)
                    {
                        var slotZ = _stock.Layout.SlotPosition(CurrentOrder).z;
                        _armSide = Mathf.Sign(slotZ - transform.position.z);
                        _arm.Target = Mathf.Abs(slotZ - transform.position.z);
                        SetState(State.ExtendingToSlot);
                    }
                    break;

                case State.ExtendingToSlot:
                    if (_arm.AtTarget) SetState(State.Gripping);
                    break;

                case State.Gripping:
                    _fingerT = Mathf.Clamp01(_timer / _fingerTime);
                    if (_fingerT >= 1f)
                    {
                        _bin = _stock.TakeOut(CurrentOrder);
                        if (_bin == null) { Debug.LogWarning($"{name} : bac {CurrentOrder} indisponible.", this); _arm.Target = 0f; SetState(State.RetractingEmpty); break; }
                        _bin.transform.SetParent(_stages[2], true);
                        _arm.Target = 0f;
                        SetState(State.RetractingWithBin);
                    }
                    break;

                case State.RetractingWithBin:
                    if (_arm.AtTarget) { _x.Target = _elevator.TransferX; SetState(State.MovingToLift); }
                    break;

                case State.MovingToLift:
                    if (_x.AtTarget) SetState(State.WaitingNacelle);
                    break;

                case State.WaitingNacelle:
                    if (_nacelle == null) _nacelle = _elevator.Reserve(_levelY);
                    if (_nacelle != null && _elevator.IsReadyToLoad(_nacelle))
                    {
                        _armSide = _nacelle.Side;
                        _arm.Target = _nacelle.LateralOffset;
                        SetState(State.ExtendingToNacelle);
                    }
                    break;

                case State.ExtendingToNacelle:
                    if (_arm.AtTarget) SetState(State.Releasing);
                    break;

                case State.Releasing:
                    _fingerT = 1f - Mathf.Clamp01(_timer / _fingerTime);
                    if (_fingerT <= 0f)
                    {
                        _elevator.Load(_nacelle, _bin);
                        _bin = null; _nacelle = null;
                        _arm.Target = 0f;
                        SetState(State.RetractingEmpty);
                    }
                    break;

                case State.RetractingEmpty:
                    _fingerT = Mathf.MoveTowards(_fingerT, 0f, dt / _fingerTime);
                    if (_arm.AtTarget && _fingerT <= 0f) { _x.Target = _restX; SetState(State.Returning); }
                    break;

                case State.Returning:
                    if (_x.AtTarget) SetState(State.Idle);
                    break;
            }

            ApplyPose();
        }

        /// <summary>Recopie l'état des axes sur les objets 3D.</summary>
        private void ApplyPose()
        {
            var p = transform.position;
            transform.position = new Vector3(_x.Position, p.y, p.z);

            // Chaque étage sort d'un tiers de la course, dans le repère de son parent.
            var worldDir = new Vector3(0f, 0f, _armSide);
            for (int i = 0; i < _stages.Count; i++)
                _stages[i].localPosition = _stageRest[i] + _stages[i].parent.InverseTransformDirection(worldDir) * (_arm.Position / _stages.Count);

            for (int i = 0; i < _fingers.Count; i++)
                _fingers[i].localRotation = FingerRotation(i, _fingerSign[i], _fingerT);
        }
    }
}
