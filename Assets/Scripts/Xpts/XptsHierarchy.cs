using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>Lecture de la hiérarchie de la scène : Entrepot/XPTS/Allee_NN/…</summary>
    public static class XptsHierarchy
    {
        public const string AislePrefix = "Allee_";

        /// <summary>Index d'allée (0 pour Allee_01) du premier parent nommé Allee_NN, ou −1.</summary>
        public static int AisleIndexOf(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.StartsWith(AislePrefix) && int.TryParse(p.name.Substring(AislePrefix.Length), out int n))
                    return n - 1;
            return -1;
        }

        /// <summary>Premier descendant dont le nom commence par <paramref name="prefix"/>, ou null.</summary>
        public static Transform FindDescendant(Transform root, string prefix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name.StartsWith(prefix)) return t;
            return null;
        }
    }
}
