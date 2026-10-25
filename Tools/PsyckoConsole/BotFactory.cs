using System;
using System.Collections.Generic;
using Psycko.Bots;
using Psycko.Core.Interfaces;

namespace Psycko.Console
{
    /// <summary>Construit les agents de la partie selon le type de chaque siège.</summary>
    public static class BotFactory
    {
        public static bool TryCreateAgents(
            IReadOnlyList<SeatKind> seats,
            ICardPlayabilityChecker checker,
            out List<IPlayerAgent> agents,
            out string error)
        {
            agents = null;
            error = null;
            var random = new Random(); // décisions non reproductibles (choix assumé)
            var result = new List<IPlayerAgent>();

            foreach (var seat in seats)
            {
                switch (seat)
                {
                    case SeatKind.Random:
                        result.Add(new RandomBot(random, checker));
                        break;
                    case SeatKind.Human:
                        result.Add(new ConsoleHumanAgent(checker));
                        break;
                    case SeatKind.Discard:
                        // Déterministe et sans état : l'instance peut être partagée entre les parties.
                        result.Add(new DiscardBot(checker));
                        break;
                    default:
                        error = $"Type de siège inconnu : {seat}.";
                        return false;
                }
            }

            agents = result;
            return true;
        }
    }
}