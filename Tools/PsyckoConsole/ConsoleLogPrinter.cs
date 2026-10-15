#nullable enable

using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.Log.Format;

namespace Psycko.Console
{
    /// <summary>Affiche une partie en direct. Mains des bots masquées sauf reveal.</summary>
    public sealed class ConsoleLogPrinter : IGameObserver
    {
        private static readonly CardFormatter Fmt = new CardFormatter();
        private static string J(IEnumerable<Card> cards)
        {
            var list = cards.Select(Fmt.Format).ToList();
            return list.Count == 0 ? "(vide)" : string.Join(" ", list);
        }

        private readonly string[] _names;
        private readonly bool _reveal;
        private readonly int _humanSeat;

        public ConsoleLogPrinter(string[] names, bool reveal, int humanSeat = 0)
        {
            _names = names;
            _reveal = reveal;
            _humanSeat = humanSeat;
        }

        public void OnLogEntry(GameLogEntry e)
        {
            string who = e.PlayerId >= 0 && e.PlayerId < _names.Length ? _names[e.PlayerId] : "?";
            string cards = e.Cards.Count > 0 ? " " + J(e.Cards) : "";
            string target = e.TargetPlayerId.HasValue && e.TargetPlayerId.Value < _names.Length
                ? " -> " + _names[e.TargetPlayerId.Value] : "";
            string effects = e.Effects.Count > 0 ? " | effets : " + string.Join(", ", e.Effects) : "";
            string detail = string.IsNullOrEmpty(e.Detail) ? "" : " (" + e.Detail + ")";
            System.Console.WriteLine($"[{e.Kind}] {who}{cards}{target}{effects}{detail}");
        }

        public void OnStateChanged(GameState state)
        {
            System.Console.WriteLine($"  Pile ({state.Pile.Cards.Count}) : {J(state.Pile.Cards)}");

            if (!_reveal) return;
            foreach (var p in state.Players)
            {
                if (p.Id == _humanSeat) continue;
                System.Console.WriteLine(
                    $"  [reveal] {p.Name} [{p.CurrentPhase}] main : {J(p.Hand)}" +
                    $" | FaceUp : {J(p.FaceUp)} | FaceDown : {J(p.FaceDown)}");
            }
        }
    }
}