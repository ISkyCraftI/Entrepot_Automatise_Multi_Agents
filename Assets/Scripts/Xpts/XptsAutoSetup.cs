using Entrepot.Stock;
using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>
    /// Au lancement du Play, ajoute les agents qui manquent dans la scène :
    /// ElevatorAgent sur chaque élévateur (parent des « Nacelle de transfert… »),
    /// ShuttleAgent sur chaque Navette_N*, SimpleDispatcher sur l'objet Stock.
    /// Pratique pour tester sans modifier la scène ; plus tard, ajoutez ces composants à la main
    /// et passez <see cref="Enabled"/> à false.
    /// </summary>
    public static class XptsAutoSetup
    {
        public static bool Enabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Setup()
        {
            if (!Enabled) return;
            var xpts = GameObject.Find("Entrepot/XPTS");
            if (xpts == null) return;

            int elevators = 0, shuttles = 0;
            foreach (Transform aisle in xpts.transform)
            {
                if (!aisle.name.StartsWith(XptsHierarchy.AislePrefix)) continue;

                var nacelle = XptsHierarchy.FindDescendant(aisle, "Nacelle");
                if (nacelle != null && nacelle.parent.GetComponent<ElevatorAgent>() == null)
                { nacelle.parent.gameObject.AddComponent<ElevatorAgent>(); elevators++; }

                var group = aisle.Find("Navettes");
                if (group == null) continue;
                foreach (Transform n in group)
                    if (n.name.StartsWith("Navette_N") && n.GetComponent<ShuttleAgent>() == null)
                    { n.gameObject.AddComponent<ShuttleAgent>(); shuttles++; }
            }

            var stock = Object.FindAnyObjectByType<StockManager>();
            if (stock != null && stock.GetComponent<SimpleDispatcher>() == null) stock.gameObject.AddComponent<SimpleDispatcher>();
            Debug.Log($"[XPTS] Ajout automatique : {elevators} élévateur(s), {shuttles} navette(s), répartiteur de test.");
        }
    }
}
