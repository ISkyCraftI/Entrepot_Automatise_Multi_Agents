using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Entrepot.Stock
{
    /// <summary>
    /// Dessine tous les bacs rangés avec Graphics.RenderMeshInstanced, sans aucun GameObject par bac.
    /// Deux modèles : bac simple (07) et bac compartimenté (08). Le corps du bac prend la couleur de la vue ;
    /// les autres sous-maillages du bac compartimenté gardent un matériau fixe (graphite).
    /// Les matrices sont reconstruites seulement quand le stock change (Model.Version), puis envoyées
    /// par paquets de 1 023.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(StockManager))]
    public sealed class StockRenderer : MonoBehaviour
    {
        public enum ViewMode
        {
            /// <summary>Couleur selon le taux de remplissage.</summary>
            Fill,
            /// <summary>Vert = bac simple, rouge = bac multiple.</summary>
            BinType,
        }

        private const int BatchSize = 1023;
        private const int Single = 0, Multi = 1;
        private const int Red = 0, Orange = 1, Yellow = 2, Green = 3, Blue = 4, ColorCount = 5;

        [SerializeField] private ViewMode _view = ViewMode.Fill;

        [Header("Couleurs du corps des bacs (GPU Instancing activé)")]
        [Tooltip("Vide (< 1 %) — sert aussi pour « multiple » en vue BinType.")]
        [SerializeField] private Material _red;
        [Tooltip("Presque vide (< 25 %).")]
        [SerializeField] private Material _orange;
        [Tooltip("Moyen (< 50 %).")]
        [SerializeField] private Material _yellow;
        [Tooltip("Presque plein (< 80 %) — sert aussi pour « simple » en vue BinType.")]
        [SerializeField] private Material _green;
        [Tooltip("Plein (≥ 80 %).")]
        [SerializeField] private Material _blue;

        [Header("Pièces fixes du bac compartimenté")]
        [Tooltip("Matériau des sous-maillages autres que le corps (GPU Instancing activé).")]
        [SerializeField] private Material _fixedParts;

        [Header("Rendu")]
        [SerializeField] private ShadowCastingMode _shadows = ShadowCastingMode.On;

        /// <summary>Un modèle de bac prêt à être instancié.</summary>
        private sealed class BinVisual
        {
            public Mesh Mesh;
            public Matrix4x4 Offset;   // pivot du bac → maillage (rotation d'import du FBX comprise)
            public readonly List<Matrix4x4>[] Buckets = new List<Matrix4x4>[ColorCount];
            public readonly Matrix4x4[][] Arrays = new Matrix4x4[ColorCount][];
            public Matrix4x4[] All = new Matrix4x4[0];  // toutes les instances (pour les pièces fixes)

            public BinVisual() { for (int c = 0; c < ColorCount; c++) Buckets[c] = new List<Matrix4x4>(1024); }
        }

        private StockManager _manager;
        private readonly BinVisual[] _visuals = { new BinVisual(), new BinVisual() };
        private Material[] _colors;
        private Bounds _bounds;
        private StockModel _builtModel;
        private int _builtVersion = -1;
        private ViewMode _builtView;

        public ViewMode View
        {
            get => _view;
            set { _view = value; _builtVersion = -1; }
        }

        private void OnEnable()
        {
            _manager = GetComponent<StockManager>();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            _builtVersion = -1;
            _visuals[Single].Mesh = _visuals[Multi].Mesh = null;
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

        private void OnValidate()
        {
            _builtVersion = -1;
            _visuals[Single].Mesh = _visuals[Multi].Mesh = null; // relit les prefabs s'ils ont changé
        }

        private static bool PrepareVisual(BinVisual v, GameObject prefab)
        {
            if (v.Mesh != null) return true;
            if (prefab == null) return false;
            var mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null) return false;
            v.Mesh = mf.sharedMesh;
            v.Offset = Matrix4x4.Rotate(prefab.transform.localRotation)
                       * prefab.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            return true;
        }

        private int ColorOf(Bin bin)
        {
            if (_view == ViewMode.BinType) return bin.IsMulti ? Red : Green;
            float f = bin.FillRatio;
            if (f < 0.01f) return Red;
            if (f < 0.25f) return Orange;
            if (f < 0.50f) return Yellow;
            if (f < 0.80f) return Green;
            return Blue;
        }

        private void Rebuild()
        {
            var model = _manager.Model;
            var layout = _manager.Layout;
            foreach (var v in _visuals) foreach (var b in v.Buckets) b.Clear();
            bool first = true;
            for (int i = 0; i < model.SlotCount; i++)
            {
                int id = model.GetBinIdAt(i);
                if (id == 0) continue;
                var bin = model.GetBin(id);
                var v = _visuals[bin.IsMulti ? Multi : Single];
                var pos = layout.SlotPosition(model.Address(i));
                v.Buckets[ColorOf(bin)].Add(Matrix4x4.Translate(pos) * v.Offset);
                if (first) { _bounds = new Bounds(pos, Vector3.one); first = false; } else _bounds.Encapsulate(pos);
            }
            _bounds.Expand(2f);
            foreach (var v in _visuals)
            {
                var all = new List<Matrix4x4>();
                for (int c = 0; c < ColorCount; c++) { v.Arrays[c] = v.Buckets[c].ToArray(); all.AddRange(v.Buckets[c]); }
                v.All = all.ToArray();
            }
            _colors = new[] { _red, _orange, _yellow, _green, _blue };
            _builtModel = model;
            _builtVersion = model.Version;
            _builtView = _view;
        }

        private void Draw(Camera cam, Material mat, Mesh mesh, int submesh, Matrix4x4[] data)
        {
            if (mat == null || data == null || data.Length == 0) return;
            var rp = new RenderParams(mat)
            {
                camera = cam,
                worldBounds = _bounds,
                shadowCastingMode = _shadows,
                receiveShadows = true,
                layer = gameObject.layer,
            };
            for (int start = 0; start < data.Length; start += BatchSize)
                Graphics.RenderMeshInstanced(rp, mesh, submesh, data, Mathf.Min(BatchSize, data.Length - start), start);
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            if (_manager == null || _manager.Model == null) return;
            if (!PrepareVisual(_visuals[Single], _manager.SingleBinPrefab)) return;
            if (!PrepareVisual(_visuals[Multi], _manager.MultiBinPrefab)) return;
            var model = _manager.Model;
            if (model != _builtModel || model.Version != _builtVersion || _view != _builtView) Rebuild();

            foreach (var v in _visuals)
            {
                // Sous-maillage 0 = corps du bac, coloré selon la vue.
                for (int c = 0; c < ColorCount; c++) Draw(cam, _colors[c], v.Mesh, 0, v.Arrays[c]);
                // Autres sous-maillages (pièces graphite du bac compartimenté) : matériau fixe.
                for (int s = 1; s < v.Mesh.subMeshCount; s++) Draw(cam, _fixedParts, v.Mesh, s, v.All);
            }
        }
    }
}
