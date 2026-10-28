using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Tri d'affichage déterministe des cartes de la main.</summary>
    public static class HandSorter
    {
        public static List<Card> Sort(IReadOnlyList<Card> cards)
        {
            if (cards == null) return new List<Card>();
            return cards.Select((card, index) => new { card, index })
                .OrderBy(x => x.card.IsJoker ? 1 : 0)
                .ThenBy(x => x.card.IsJoker ? int.MaxValue : (int)x.card.Rank.GetValueOrDefault())
                .ThenBy(x => x.card.IsJoker ? int.MaxValue : (int)x.card.Suit.GetValueOrDefault())
                .ThenBy(x => x.card.IsJoker ? (int)x.card.JokerType.GetValueOrDefault() : -1)
                .ThenBy(x => x.index).Select(x => x.card).ToList();
        }
    }
}
