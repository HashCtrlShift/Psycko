namespace Psycko.Core.Domain
{
    /// <summary>
    /// Phase de progression d'un joueur. Chaque joueur avance
    /// indépendamment : un joueur peut être en Phase 3 quand un autre
    /// est encore en Phase 1.
    /// </summary>
    public enum DefPhase
    {
        /// <summary>Phase 1 — Le Travail : cartes jouées depuis la main.</summary>
        Work = 0,

        /// <summary>Phase 2 — Le Talent : cartes Face Découverte.</summary>
        Talent = 1,

        /// <summary>Phase 3 — La Chance : cartes Face Cachée révélées une à une.</summary>
        Luck = 2,

        /// <summary>Le joueur n'a plus aucune carte : il a terminé la partie.</summary>
        Finished = 3
    }
}