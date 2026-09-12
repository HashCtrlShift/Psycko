using Psycko.Core.Domain;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Contrat de mutation de l'état d'une partie.
    /// Chaque méthode retourne un NOUVEL état (immuabilité) — l'instance appelante
    /// n'est jamais modifiée.
    /// Principe directeur : ces transitions sont VOLONTAIREMENT "bêtes".
    /// Elles ne portent aucune règle de jeu (ReverseDirection ignore l'existence du Valet,
    /// DestroyPile ignore celle du Carré). Le "quand" appartient à Rules/Services.
    /// </summary>
    public interface IGameStateCommand
    {
        /// <summary>Pose un coup : les cartes quittent la main du joueur et rejoignent la Pile.</summary>
        IGameState PlayCards(Play play);

        /// <summary>Le joueur ramasse l'intégralité de la Pile de jeu dans sa main.</summary>
        IGameState PickUpPile(int playerIndex);

        /// <summary>Vide la Pile de jeu (Carré / Bombe / Joker Noir). Cartes hors-jeu définitivement.</summary>
        IGameState DestroyPile();

        /// <summary>Affecte le joueur actif. Le choix de l'index relève de TurnManager.</summary>
        IGameState SetActivePlayer(int playerIndex);

        /// <summary>Inverse le sens de jeu courant.</summary>
        IGameState ReverseDirection();

        /// <summary>Définit la contrainte de hauteur active et son rang de référence.</summary>
        IGameState SetConstraint(HeightConstraint constraint, DefRank refRank);

        /// <summary>Transfère les <paramref name="count"/> cartes du dessus de la Pioche vers la main du joueur.</summary>
        IGameState DrawCards(int playerIndex, int count);

        /// <summary>Fait progresser le joueur à la phase suivante (Travail → Talent → Chance).</summary>
        IGameState AdvancePlayerPhase(int playerIndex);

        /// <summary>Marque le joueur comme sorti de la partie (plus aucune carte).</summary>
        IGameState EliminatePlayer(int playerIndex);
    }
}