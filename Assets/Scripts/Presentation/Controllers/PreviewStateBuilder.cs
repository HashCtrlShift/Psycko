using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;

namespace Psycko.Presentation.Controllers
{
    /// <summary>
    /// Construit l'état d'aperçu d'une pose : les cartes quittent leur couche d'origine
    /// ET arrivent sur la Pile dans le même GameState, AVANT tout effet (destruction,
    /// Valet, Jokers...). Aucune règle de jeu : réutilise uniquement GameState.PlayCards.
    /// Renvoie null si l'aperçu est impossible ; l'appelant affiche alors l'état résolu.
    /// </summary>
    internal static class PreviewStateBuilder
    {
        public static GameState Build(GameState lastResolved, GameLogEntry entry)
        {
            if (lastResolved == null || entry == null || entry.Cards.Count == 0)
                return null;

            Player player = lastResolved.Players.FirstOrDefault(p => p.Id == entry.PlayerId);
            if (player == null) return null;

            CardLayer? layer = FindLayer(player, entry.Cards);
            if (!layer.HasValue) return null;

            try
            {
                Play play = Play.Create(entry.PlayerId, entry.Cards, layer.Value);
                return lastResolved.PlayCards(play) as GameState;
            }
            catch (Exception)
            {
                // Pose refusée par le Domain : pas d'aperçu, jamais de crash UI.
                return null;
            }
        }

        /// <summary>Une pose vient d'UNE seule couche : celle qui contient toutes les cartes.</summary>
        private static CardLayer? FindLayer(Player player, IReadOnlyList<Card> cards)
        {
            if (ContainsAll(player.Hand, cards)) return CardLayer.Hand;
            if (ContainsAll(player.FaceUp, cards)) return CardLayer.FaceUp;
            if (ContainsAll(player.FaceDown, cards)) return CardLayer.FaceDown;
            return null;
        }

        /// <summary>Même sémantique que GameState.RemoveCards (Remove un par un).</summary>
        private static bool ContainsAll(IReadOnlyList<Card> source, IReadOnlyList<Card> cards)
        {
            var remaining = new List<Card>(source);
            foreach (Card card in cards)
                if (!remaining.Remove(card)) return false;
            return true;
        }
    }
}