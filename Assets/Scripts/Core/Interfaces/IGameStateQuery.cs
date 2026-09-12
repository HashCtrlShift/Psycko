using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Contrat de LECTURE SEULE sur l'état d'une partie (GameState).
    /// Aucune mutation possible via cette interface — voir IGameStateCommand
    /// pour les transitions produisant un nouvel état immuable.
    /// Miroir fidèle de GameState : ne porte aucune règle, aucun calcul métier
    /// (l'ordre de jeu, les skips et les détections se déduisent dans Rules/Services).
    /// </summary>
    public interface IGameStateQuery
    {
        /// <summary>Joueurs de la partie, en ordre de siège (index fixe, ne change jamais).</summary>
        IReadOnlyList<Player> Players { get; }

        /// <summary>Pioche commune restante. Vide = épuisée définitivement (jamais remélangée).</summary>
        IReadOnlyList<Card> DrawPile { get; }

        /// <summary>Pile de jeu centrale (coups posés + cartes à plat).</summary>
        Pile Pile { get; }

        /// <summary>Index (dans Players) du joueur dont c'est le tour.</summary>
        int ActivePlayerIndex { get; }

        /// <summary>Sens de jeu courant (inversé par le Valet).</summary>
        PlayDirection Direction { get; }

        /// <summary>Contrainte de hauteur active pour le joueur actif (Normal ou PriestReversed).</summary>
        HeightConstraint Constraint { get; }

        /// <summary>
        /// Rang de référence de la contrainte en cours (rang du dernier coup posé,
        /// ou le Prêtre si Constraint = PriestReversed).
        /// Non pertinent tant que Pile.Cards est vide.
        /// </summary>
        DefRank RefRank { get; }

        /// <summary>
        /// Raccourci de lecture : le joueur dont c'est le tour (équivaut à Players[ActivePlayerIndex]).
        /// Aucune logique de tour ici — la progression du tour relève de TurnManager.
        /// </summary>
        Player GetActivePlayer();
    }
}