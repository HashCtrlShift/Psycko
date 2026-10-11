#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Distribue un paquet mélangé : 3 FaceDown, 3 FaceUp et 3 Hand par joueur, le reste
    /// forme la pioche. Produit un PreGameExchangeState : le GameState (et donc le premier
    /// joueur) n'est créé qu'après l'échange, via PreGameExchange.CreateGameState.
    /// Ne mélange pas : l'appelant fournit un Deck déjà mélangé (seed gérée hors Core).
    /// </summary>
    public static class CardDealer
    {
        public const int CardsPerLayer = 3;

        public static PreGameExchangeState Deal(Deck shuffledDeck, IReadOnlyList<(int Id, string Name)> seats)
        {
            if (shuffledDeck is null) throw new ArgumentNullException(nameof(shuffledDeck));
            if (seats is null) throw new ArgumentNullException(nameof(seats));
            if (seats.Count < 2)
                throw new ArgumentException("Une partie nécessite au moins 2 joueurs.", nameof(seats));

            int needed = seats.Count * CardsPerLayer * 3;
            if (shuffledDeck.Cards.Count < needed)
                throw new ArgumentException("Paquet insuffisant pour la distribution.", nameof(shuffledDeck));

            int cursor = 0;
            var players = new List<Player>(seats.Count);

            foreach (var seat in seats)
            {
                var faceDown = Take(shuffledDeck.Cards, ref cursor, CardsPerLayer);
                var faceUp = Take(shuffledDeck.Cards, ref cursor, CardsPerLayer);
                var hand = Take(shuffledDeck.Cards, ref cursor, CardsPerLayer);

                players.Add(Player.Create(seat.Id, seat.Name, hand, faceUp, faceDown));
            }

            var drawPile = shuffledDeck.Cards.Skip(cursor).ToList();
            return new PreGameExchangeState(players, drawPile);
        }

        private static List<Card> Take(IReadOnlyList<Card> cards, ref int cursor, int count)
        {
            var taken = new List<Card>(count);
            for (int i = 0; i < count; i++)
                taken.Add(cards[cursor++]);
            return taken;
        }
    }
}