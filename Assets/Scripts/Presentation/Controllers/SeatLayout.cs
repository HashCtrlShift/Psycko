using System;
using System.Collections.Generic;
using Psycko.Bots;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.Controllers
{
    public sealed class SeatLayoutResult
    {
        public IReadOnlyList<IPlayerAgent> Agents { get; private set; }
        public IReadOnlyList<string> Names { get; private set; }
        public int DiscardSeat { get; private set; }
        internal SeatLayoutResult(IPlayerAgent[] agents, string[] names, int discardSeat) { Agents = Array.AsReadOnly(agents); Names = Array.AsReadOnly(names); DiscardSeat = discardSeat; }
    }
    /// <summary>Composition locale des agents par siège.</summary>
    public sealed class SeatLayout
    {
        private readonly Random _random;
        public SeatLayout(Random random) { _random = random ?? throw new ArgumentNullException(nameof(random)); }
        public SeatLayoutResult Create(UnityHumanAgent human, ICardPlayabilityChecker checker)
        {
            if (human == null) throw new ArgumentNullException(nameof(human));
            if (checker == null) throw new ArgumentNullException(nameof(checker));
            int discard = _random.Next(1, 4);
            IPlayerAgent[] agents = new IPlayerAgent[4]; string[] names = { "Vous", "Bot1", "Bot2", "Bot3" };
            agents[0] = human; agents[discard] = new DiscardBot(checker); names[discard] = "Discard";
            for (int seat = 1; seat < 4; seat++) if (seat != discard) agents[seat] = new RandomBot(new Random(_random.Next()), checker);
            return new SeatLayoutResult(agents, names, discard);
        }
    }
}
