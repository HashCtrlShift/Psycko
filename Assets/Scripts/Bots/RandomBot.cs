#nullable enable

using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Bots
{
    /// <summary>
    /// Agent qui sélectionne uniformément une décision parmi les options
    /// autorisées par l'état visible fourni par le Core.
    /// </summary>
    /// <remarks>
    /// Ce bot ne contient aucune logique métier de validation.
    /// La jouabilité des cartes est déléguée à
    /// <see cref="ICardPlayabilityChecker"/>.
    /// </remarks>
    public sealed class RandomBot : IPlayerAgent
    {
        private readonly Random random;
        private readonly ICardPlayabilityChecker playabilityChecker;

        /// <summary>
        /// Initialise un bot aléatoire avec ses dépendances explicites.
        /// </summary>
        /// <param name="random">
        /// Source d'aléatoire injectée. Elle permet de reproduire les décisions
        /// lors des simulations déterministes.
        /// </param>
        /// <param name="playabilityChecker">
        /// Service Core chargé de déterminer les cartes jouables.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Levée si une dépendance est nulle.
        /// </exception>
        public RandomBot(
            Random random,
            ICardPlayabilityChecker playabilityChecker)
        {
            this.random = random
                ?? throw new ArgumentNullException(nameof(random));

            this.playabilityChecker = playabilityChecker
                ?? throw new ArgumentNullException(nameof(playabilityChecker));
        }

        /// <summary>
        /// Propose uniformément une carte jouable depuis la main.
        /// Si la main est vide, les cartes FaceUp disponibles sont examinées.
        /// </summary>
        /// <param name="state">Vue filtrée de l'état du joueur.</param>
        /// <returns>
        /// Un coup aléatoire parmi les cartes jouables, ou <c>null</c>
        /// lorsqu'aucun coup normal n'est disponible.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Levée si <paramref name="state"/> est nul.
        /// </exception>
        public Play? ProposeNormalPlay(IPlayerVisibleState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.SelfHand.Count > 0)
            {
                if (!playabilityChecker.HasAnyPlayableCard(state))
                {
                    return null;
                }

                return SelectPlayableCard(
                    state.SelfHand,
                    CardLayer.Hand,
                    state);
            }

            if (state.SelfFaceUp.Count > 0)
            {
                return SelectPlayableCard(
                    state.SelfFaceUp,
                    CardLayer.FaceUp,
                    state);
            }

            return null;
        }

        /// <summary>
        /// Décide si un pickup doit être accepté.
        /// </summary>
        /// <param name="state">Vue filtrée de l'état du joueur.</param>
        /// <param name="isForced">
        /// Indique que le pickup a déjà été décidé comme obligatoire par le Core
        /// ou le gestionnaire de tour.
        /// </param>
        /// <returns>
        /// <c>true</c> uniquement pour un pickup forcé ; <c>false</c> dans tous
        /// les cas volontaires.
        /// </returns>
        /// <remarks>
        /// RandomBot v1 ne demande jamais volontairement à ramasser la pile.
        /// La détection du pickup forcé reste une responsabilité du Core.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Levée si <paramref name="state"/> est nul.
        /// </exception>
        public bool DecidePickup(
            IPlayerVisibleState state,
            bool isForced)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return isForced;
        }

        /// <summary>
        /// Propose une carte FaceDown en phase Luck.
        /// </summary>
        /// <param name="state">Vue filtrée de l'état du joueur.</param>
        /// <returns>
        /// Un coup FaceDown construit à partir de l'index choisi, ou
        /// <c>null</c> si aucune carte FaceDown n'est disponible.
        /// </returns>
        /// <remarks>
        /// L'index est tiré aléatoirement avant toute lecture du contenu de la
        /// carte. Cette méthode respecte donc la discipline d'aveuglement
        /// imposée pour la couche FaceDown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Levée si <paramref name="state"/> est nul.
        /// </exception>
        public Play? ProposeFaceDownPlay(IPlayerVisibleState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.SelfHand.Count > 0
                || state.SelfFaceDown.Count == 0)
            {
                return null;
            }

            int selectedIndex = random.Next(state.SelfFaceDown.Count);

            Card selectedCard = state.SelfFaceDown[selectedIndex];

            return Play.CreateSingle(
                state.SelfPlayerId,
                selectedCard,
                CardLayer.FaceDown);
        }

        /// <summary>
        /// Résout aléatoirement un Don en choisissant une carte de la main
        /// et un adversaire destinataire.
        /// </summary>
        /// <param name="state">Vue filtrée de l'état du joueur.</param>
        /// <returns>
        /// Le choix immuable de la carte donnée et du joueur destinataire.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Levée si <paramref name="state"/> est nul.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Levée si le joueur n'a aucune carte en main ou si aucun adversaire
        /// n'est disponible.
        /// </exception>
        public GiftResolutionChoice ResolveGift(
            IPlayerVisibleState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.SelfHand.Count == 0)
            {
                throw new InvalidOperationException(
                    "Impossible de résoudre le Don : la main du joueur est vide.");
            }

            if (state.Opponents.Count == 0)
            {
                throw new InvalidOperationException(
                    "Impossible de résoudre le Don : aucun adversaire disponible.");
            }

            int cardIndex = random.Next(state.SelfHand.Count);
            Card cardToGive = state.SelfHand[cardIndex];

            int opponentIndex = random.Next(state.Opponents.Count);
            IOpponentVisibleInfo recipient = state.Opponents[opponentIndex];

            return new GiftResolutionChoice(
                cardToGive,
                recipient.PlayerId);
        }

        /// <summary>
        /// Sélectionne uniformément une carte jouable dans une couche donnée.
        /// </summary>
        /// <param name="cards">Cartes candidates de la couche.</param>
        /// <param name="sourceLayer">Couche d'origine des cartes.</param>
        /// <param name="state">Vue filtrée utilisée pour la validation.</param>
        /// <returns>
        /// Un coup construit à partir d'une carte jouable, ou <c>null</c>
        /// lorsqu'aucune carte de la couche n'est jouable.
        /// </returns>
        private Play? SelectPlayableCard(
            IReadOnlyList<Card> cards,
            CardLayer sourceLayer,
            IPlayerVisibleState state)
        {
            var playableCards = new List<Card>();

            for (int index = 0; index < cards.Count; index++)
            {
                Card card = cards[index];

                if (playabilityChecker.IsPlayable(card, state))
                {
                    playableCards.Add(card);
                }
            }

            if (playableCards.Count == 0)
            {
                return null;
            }

            int selectedIndex = random.Next(playableCards.Count);

            return Play.CreateSingle(
                state.SelfPlayerId,
                playableCards[selectedIndex],
                sourceLayer);
        }
    }
}