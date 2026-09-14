namespace Psycko.Core.Domain
{
    /// <summary>
    /// Couche d'origine d'une carte au moment où elle est jouée.
    /// Sert à distinguer la provenance réelle d'un Play, notamment pour les
    /// cartes révélées directement depuis la Couche 3 (Face Cachée) en Phase 3,
    /// qui n'ont jamais transité par la main du joueur.
    /// Un Play entier provient d'UNE SEULE couche — jamais un mélange.
    /// </summary>
    public enum CardLayer
    {
        /// <summary>Couche 1 — Main du joueur. Cas de la Phase 1 et de la majorité des coups.</summary>
        Hand,

        /// <summary>Couche 2 — Cartes Face Découverte, posées devant le joueur (Phase 2).</summary>
        FaceUp,

        /// <summary>Couche 3 — Cartes Face Cachée, révélées directement sur la pile (Phase 3).</summary>
        FaceDown
    }
}