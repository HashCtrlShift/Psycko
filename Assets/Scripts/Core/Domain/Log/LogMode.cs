namespace Psycko.Core.Domain.Log
{
    /// <summary>Mode de logging pour la simulation.</summary>
    public enum LogMode
    {
        /// <summary>Aucun recorder créé. Coût nul en mémoire.</summary>
        Off = 0,

        /// <summary>Toutes les parties sont loggées (coût mémoire + export CSV).</summary>
        All = 1,

        /// <summary>Seulement les parties exceptionnelles sont loggées (voir T38d-4).</summary>
        ExceptionalOnly = 2
    }
}