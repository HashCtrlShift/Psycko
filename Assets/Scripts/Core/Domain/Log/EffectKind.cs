namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Effet enregistré dans la colonne "Effets" du GameLog.
    /// Une entrée peut en combiner plusieurs (ex. DetruiteCarre + SensReverse).
    /// </summary>
    public enum EffectKind
    {
        Doublon,        // 2 cartes de même rang jouées ensemble
        Detruite2,
        DetruiteCarre,
        DetruiteBombe,
        SensReverse,    // Detail = "Horaire" / "Antihoraire"
        PriestEffect,   // Contrainte PriestReversed posée
        JokerVerre,
        JokerNoir
    }
}