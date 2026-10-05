namespace Psycko.Core.Domain.Log
{
    /// <summary>Détail immuable d'un effet enregistré dans une entrée de log.</summary>
    public readonly struct EffectLogDetail
    {
        /// <summary>Type d'effet détecté.</summary>
        public EffectKind Kind { get; }

        /// <summary>Précision facultative (ex. direction pour SensReverse). Null sinon.</summary>
        public string Detail { get; }

        public EffectLogDetail(EffectKind kind, string detail = null)
        {
            Kind = kind;
            Detail = detail;
        }

        public override string ToString()
            => Detail == null ? Kind.ToString() : $"{Kind} ({Detail})";
    }
}