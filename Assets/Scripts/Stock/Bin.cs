namespace Entrepot.Stock
{
    /// <summary>Un compartiment d'un bac : un seul produit (SKU) par compartiment.</summary>
    public sealed class Compartment
    {
        /// <summary>Référence produit. 0 = compartiment vide.</summary>
        public int Sku;
        public int Quantity;
        public int Capacity;

        public Compartment(int sku, int quantity, int capacity)
        {
            Sku = sku; Quantity = quantity; Capacity = capacity;
        }
    }

    /// <summary>
    /// Un bac physique. Il a 1 compartiment (bac simple) ou plusieurs (bac multiple).
    /// L'identifiant 0 est réservé : il signifie « pas de bac » dans la matrice du stock.
    /// </summary>
    public sealed class Bin
    {
        public int Id { get; internal set; }
        public Compartment[] Compartments { get; }

        public Bin(Compartment[] compartments) { Compartments = compartments; }

        /// <summary>Vrai si le bac a plus d'un compartiment.</summary>
        public bool IsMulti => Compartments.Length > 1;

        /// <summary>Taux de remplissage global du bac, de 0 à 1.</summary>
        public float FillRatio
        {
            get
            {
                int q = 0, c = 0;
                foreach (var k in Compartments) { q += k.Quantity; c += k.Capacity; }
                return c == 0 ? 0f : (float)q / c;
            }
        }
    }
}
