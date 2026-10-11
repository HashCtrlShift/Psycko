#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;

namespace Psycko.Console
{
    /// <summary>Agent humain en console. 'q' = abandon (partie marquée en erreur, log conservé).</summary>
    public sealed class ConsoleHumanAgent : IPlayerAgent
    {
        private readonly ICardPlayabilityChecker _checker;

        public ConsoleHumanAgent(ICardPlayabilityChecker checker) => _checker = checker;

        public IReadOnlyList<ExchangeChoice> ProposeExchanges(IPlayerVisibleState s)
        {
            var hand = s.SelfHand.ToList();
            var faceUp = s.SelfFaceUp.ToList();
            var choices = new List<ExchangeChoice>();

            while (true)
            {
                System.Console.WriteLine("\n=== Échange pré-partie ===");
                Show("Main", hand);
                Show("FaceUp", faceUp);
                int h = Ask("Index Main à échanger (Entrée = prêt)", hand.Count, allowEmpty: true);
                if (h < 0) return choices;
                int f = Ask("Index FaceUp", faceUp.Count);

                choices.Add(new ExchangeChoice(hand[h], faceUp[f])); // ⚠️ ordre des paramètres à vérifier
                (hand[h], faceUp[f]) = (faceUp[f], hand[h]);
            }
        }
        public Play? ProposeNormalPlay(IPlayerVisibleState s)
        {
            ShowTable(s);
            bool fromHand = s.SelfHand.Count > 0;
            var cards = fromHand ? s.SelfHand : s.SelfFaceUp;
            var layer = fromHand ? CardLayer.Hand : CardLayer.FaceUp;

            Show(fromHand ? "Ta main" : "Tes FaceUp", cards, c => _checker.IsPlayable(c, s));

            while (true)
            {
                System.Console.Write("Index à jouer, séparés par des espaces (ex: 0 2 3), Entrée = ramasser > ");
                var input = System.Console.ReadLine()?.Trim() ?? "q";
                if (input == "q") throw new OperationCanceledException("Partie abandonnée par le joueur.");
                if (input.Length == 0) return null;

                var indexes = new List<int>();
                bool valid = true;
                foreach (var token in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(token, out int i) && i >= 0 && i < cards.Count && !indexes.Contains(i))
                        indexes.Add(i);
                    else { valid = false; break; }
                }

                if (!valid || indexes.Count == 0 || indexes.Count > 4)
                {
                    System.Console.WriteLine("Saisie invalide (1 à 4 index distincts).");
                    continue;
                }

                try
                {
                    return Play.Create(s.SelfPlayerId, indexes.Select(i => cards[i]), layer);
                }
                catch (ArgumentException ex)
                {
                    System.Console.WriteLine(ex.Message); // hauteurs différentes, Joker mêlé…
                }
            }
        }

        public bool DecidePickup(IPlayerVisibleState s, bool isForced) => true;

        public Play? ProposeFaceDownPlay(IPlayerVisibleState s)
        {
            ShowTable(s);
            int i = Ask($"FaceDown à retourner (0..{s.SelfFaceDown.Count - 1})", s.SelfFaceDown.Count);
            return Play.CreateSingle(s.SelfPlayerId, s.SelfFaceDown[i], CardLayer.FaceDown);
        }

        public GiftResolutionChoice ResolveGift(IPlayerVisibleState s)
        {
            System.Console.WriteLine("\n=== Don du 7 ===");
            Show("Ta main", s.SelfHand);
            int c = Ask("Carte à donner", s.SelfHand.Count);
            var targets = s.Opponents.Where(o => o.CurrentPhase != DefPhase.Finished).ToList();
            for (int i = 0; i < targets.Count; i++)
                System.Console.WriteLine($"  [{i}] {targets[i].Name}");
            int t = Ask("Destinataire", targets.Count);
            return new GiftResolutionChoice(s.SelfHand[c], targets[t].PlayerId);
        }

        private static void ShowTable(IPlayerVisibleState s)
        {
            System.Console.WriteLine($"\n--- Pile : {string.Join(" ", s.Pile.Cards)} | contrainte {s.Constraint} {s.RefRank} | pioche {s.DrawPileCount}");
            foreach (var o in s.Opponents)
                System.Console.WriteLine($"  {o.Name} [{o.CurrentPhase}] main {o.HandCount} | FaceUp {string.Join(" ", o.FaceUp)} | FaceDown {o.FaceDownCount}");
            System.Console.WriteLine($"  Toi : FaceUp {string.Join(" ", s.SelfFaceUp)} | FaceDown {s.SelfFaceDown.Count}");
        }

        private static void Show(string label, IReadOnlyList<Card> cards, Func<Card, bool>? playable = null)
        {
            System.Console.WriteLine(label + " :");
            for (int i = 0; i < cards.Count; i++)
                System.Console.WriteLine($"  [{i}] {cards[i]}{(playable != null && playable(cards[i]) ? "  ✓" : "")}");
        }

        private static int Ask(string prompt, int count, bool allowEmpty = false)
        {
            while (true)
            {
                System.Console.Write(prompt + " > ");
                var input = System.Console.ReadLine()?.Trim() ?? "q";
                if (input == "q") throw new OperationCanceledException("Partie abandonnée par le joueur.");
                if (allowEmpty && input.Length == 0) return -1;
                if (int.TryParse(input, out int i) && i >= 0 && i < count) return i;
                System.Console.WriteLine("Index invalide.");
            }
        }
    }
}