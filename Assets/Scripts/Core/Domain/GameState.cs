using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Rules.Comparison;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Direction de jeu (sens des joueurs), inversée par le Valet.
    /// </summary>
    public enum PlayDirection
    {
        Clockwise = 0,
        CounterClockwise = 1
    }

    /// <summary>
    /// État complet et IMMUABLE d'une partie à un instant T.
    /// Agrège Joueurs, Pile, Pioche, joueur actif, sens de jeu et contrainte de hauteur.
    /// Ne porte AUCUNE règle : Rules/Services produisent un nouveau GameState via les
    /// méthodes With*() à chaque transition (pose, pioche, changement de phase, etc.).
    /// La chaîne de Doublon/Carré N'EST PAS stockée ici : elle se déduit en lisant
    /// Pile.Cards (voir PairDetection/QuadDetection) — pas de duplication d'état.
    /// </summary>
    public sealed class GameState
    {
        private readonly List<Player> _players;
        private readonly List<Card> _drawPile;

        /// <summary>Joueurs de la partie, en ordre de siège (index fixe, ne change jamais).</summary>
        public IReadOnlyList<Player> Players => _players;

        /// <summary>Pioche commune restante. Vide = épuisée définitivement (jamais remélangée).</summary>
        public IReadOnlyList<Card> DrawPile => _drawPile;

        /// <summary>Pile de jeu centrale (coups posés + cartes à plat).</summary>
        public Pile Pile { get; }

        /// <summary>Index (dans Players) du joueur dont c'est le tour.</summary>
        public int ActivePlayerIndex { get; }

        /// <summary>Sens de jeu courant (inversé par le Valet).</summary>
        public PlayDirection Direction { get; }

        /// <summary>Contrainte de hauteur active pour le joueur actif.</summary>
        public HeightConstraint Constraint { get; }

        /// <summary>
        /// Rang de référence pour la contrainte de hauteur en cours : le rang du dernier
        /// coup effectivement posé sur la Pile (Joker de Verre transparent — voir
        /// GlassJokerResolver, qui recalculera ce champ en traversant le Joker).
        /// Combiné à Constraint :
        ///   - Normal          → le prochain coup doit être RefRank ou supérieur.
        ///   - PriestReversed  → le prochain coup doit être RefRank (le Prêtre) ou inférieur.
        /// Non pertinent tant que Pile.Cards est vide (valeur neutre Rank.Three au départ,
        /// jamais consultée avant le premier coup posé).
        /// </summary>
        public DefRank RefRank { get; }

        private GameState(
            List<Player> players,
            List<Card> drawPile,
            Pile pile,
            int activePlayerIndex,
            PlayDirection direction,
            HeightConstraint constraint,
            DefRank refRank)
        {
            _players = players;
            _drawPile = drawPile;
            Pile = pile;
            ActivePlayerIndex = activePlayerIndex;
            Direction = direction;
            Constraint = constraint;
            RefRank = refRank;
        }

        /// <summary>
        /// Construit l'état initial d'une partie : joueurs déjà distribués (Hand/FaceUp/FaceDown),
        /// pioche restante après distribution, pile vide, premier joueur actif, sens horaire,
        /// contrainte normale.
        /// </summary>
        public static GameState CreateInitial(
            IEnumerable<Player> players,
            IEnumerable<Card> drawPile,
            int firstPlayerIndex)
        {
            if (players is null) throw new ArgumentNullException(nameof(players));
            if (drawPile is null) throw new ArgumentNullException(nameof(drawPile));

            var playerList = players.ToList();
            if (playerList.Count < 2)
                throw new ArgumentException("Une partie nécessite au moins 2 joueurs.", nameof(players));

            if (firstPlayerIndex < 0 || firstPlayerIndex >= playerList.Count)
                throw new ArgumentOutOfRangeException(nameof(firstPlayerIndex));

            return new GameState(
                playerList,
                drawPile.ToList(),
                Pile.Empty,
                firstPlayerIndex,
                PlayDirection.Clockwise,
                HeightConstraint.Normal,
                DefRank.Three);
        }

        /// <summary>Retourne une copie de l'état avec la liste des joueurs remplacée.</summary>
        public GameState WithPlayers(IEnumerable<Player> players)
            => new GameState(players.ToList(), _drawPile, Pile, ActivePlayerIndex, Direction, Constraint, RefRank);

        /// <summary>Retourne une copie de l'état avec un seul joueur remplacé (par son index de siège).</summary>
        public GameState WithPlayer(int index, Player player)
        {
            if (index < 0 || index >= _players.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var updated = new List<Player>(_players);
            updated[index] = player;
            return new GameState(updated, _drawPile, Pile, ActivePlayerIndex, Direction, Constraint, RefRank);
        }

        /// <summary>Retourne une copie de l'état avec la pioche remplacée (ex: après un tirage).</summary>
        public GameState WithDrawPile(IEnumerable<Card> drawPile)
            => new GameState(_players, drawPile.ToList(), Pile, ActivePlayerIndex, Direction, Constraint, RefRank);

        /// <summary>Retourne une copie de l'état avec la Pile remplacée (nouveau coup ajouté, ou pile détruite via Pile.Empty).</summary>
        public GameState WithPile(Pile pile)
            => new GameState(_players, _drawPile, pile, ActivePlayerIndex, Direction, Constraint, RefRank);

        /// <summary>Retourne une copie de l'état avec un nouveau joueur actif désigné.</summary>
        public GameState WithActivePlayerIndex(int activePlayerIndex)
        {
            if (activePlayerIndex < 0 || activePlayerIndex >= _players.Count)
                throw new ArgumentOutOfRangeException(nameof(activePlayerIndex));

            return new GameState(_players, _drawPile, Pile, activePlayerIndex, Direction, Constraint, RefRank);
        }

        /// <summary>Retourne une copie de l'état avec le sens de jeu inversé (effet Valet).</summary>
        public GameState WithDirection(PlayDirection direction)
            => new GameState(_players, _drawPile, Pile, ActivePlayerIndex, direction, Constraint, RefRank);

        /// <summary>
        /// Retourne une copie de l'état avec une nouvelle contrainte de hauteur et son RefRank associé
        /// (ex: pose d'un Prêtre → constraint=PriestReversed, refRank=Priest ;
        /// retour au mode normal → constraint=Normal, refRank=rang du dernier coup posé).
        /// </summary>
        public GameState WithConstraint(HeightConstraint constraint, DefRank refRank)
            => new GameState(_players, _drawPile, Pile, ActivePlayerIndex, Direction, constraint, refRank);
    }
}