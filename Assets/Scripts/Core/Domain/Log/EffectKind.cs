namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Effet enregistré dans la colonne "Effets" du GameLog.
    /// Une entrée peut en combiner plusieurs (ex. DetruiteCarre + SensReverse).
    /// Convention : on logge le RÉSULTAT observé, pas la carte causale.
    /// Le Joker Couleur n'a donc pas d'entrée propre : il apparaît comme DetruiteBombe.
    /// Don est ajouté en fin d'enum (compteurs d'effets T42).
    /// </summary>
    public enum EffectKind
    {
        Doublon,        // 2 cartes de même rang jouées ensemble
        Detruite2,
        DetruiteCarre,
        DetruiteBombe,  // Bombe et Joker Couleur
        SensReverse,    // Detail = "Horaire" / "Antihoraire"
        PriestEffect,   // Contrainte PriestReversed posée
        JokerVerre,
        JokerNoir,
        Don             // Don du 7 (carte transférée)
    }
}