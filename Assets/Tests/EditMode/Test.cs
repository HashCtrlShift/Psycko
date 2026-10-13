using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Core.Services;

namespace Psycko.Core.Tests.Services
{
    /// <summary>
    /// Règle CLAUDE.md : interdit de terminer une phase sur un 2.
    /// Si le 2 est la dernière carte du joueur : il pose, RAMASSE la pile
    /// (2 inclus) et passe son tour. Pas de destruction, pas de rejeu.
    /// Un 2 non terminal détruit la pile et accorde un rejeu.
    /// </summary>
    [TestFixture]
    public class GameOrchestratorLastTwoTests
    {
        // ---------- HELPERS ----------

        private static Card CreateCard(DefRank rank, DefSuit suit = DefSuit.Hearts)
            => Card.CreateStandard(suit, rank);

        /// <summary>
        /// Player.Create exige exactement 3 FaceUp et 3 FaceDown. On crée donc un joueur
        /// valide, puis on remplace chaque couche et la phase par l'état voulu par le test.
        /// </summary>
        private static Player CreatePlayer(
            int id,
            string name,
            DefPhase phase,
            IEnumerable<Card> hand,
            IEnumerable<Card> faceUp,
            IEnumerable<Card> faceDown)
        {
            var filler = new[]
            {
                CreateCard(DefRank.Three, DefSuit.Diamonds),
                CreateCard(DefRank.Four,  DefSuit.Diamonds),
                CreateCard(DefRank.Five,  DefSuit.Diamonds)
            };

            return Player.Create(id, name, new List<Card>(), filler, filler)
                .WithHand(hand)
                .WithFaceUp(faceUp)
                .WithFaceDown(faceDown)
                .WithPhase(phase);
        }
        private static GameState BuildState(Player active, IEnumerable<Card> pileCards = null)
        {
            var p1 = CreatePlayer(1, "Player1", DefPhase.Work,
                new[] { CreateCard(DefRank.Ace) }, Filler(), Filler());
            var p2 = CreatePlayer(2, "Player2", DefPhase.Work,
                new[] { CreateCard(DefRank.King) }, Filler(), Filler());

            var pile = Pile.Empty;
            foreach (var card in pileCards ?? Enumerable.Empty<Card>())
                pile = pile.Add(Play.CreateSingle(1, card, CardLayer.Hand));

            return GameState
                .CreateInitial(new[] { active, p1, p2 }, new List<Card>())
                .WithPile(pile)
                .WithActivePlayerIndex(0);
        }

private static IEnumerable<Card> Filler() =>
    Enumerable.Repeat(CreateCard(DefRank.Four), 3).ToList();
        private static Play PlaySingle(int playerId, Card card, CardLayer layer = CardLayer.Hand)
            => Play.CreateSingle(playerId, card, layer);

        private static Player FilledPlayer(DefPhase phase, IEnumerable<Card> hand)
            => CreatePlayer(0, "Active", phase,
                hand,
                new[] { CreateCard(DefRank.Ten), CreateCard(DefRank.Jack), CreateCard(DefRank.Queen) },
                new[] { CreateCard(DefRank.King), CreateCard(DefRank.Ace), CreateCard(DefRank.Three) });

        // ---------- CAS 1 : 2 = toute dernière carte (main), Work → ramasse ----------

        [Test]
        public void LastTwo_FromHand_InWork_PlayerPicksUpPile()
        {
            var two = CreateCard(DefRank.Two);
            var active = CreatePlayer(0, "Active", DefPhase.Work,
                new[] { two },
                new List<Card>(),
                new List<Card>());

            var state = BuildState(active, new[] { CreateCard(DefRank.Five) });
            var play = PlaySingle(active.Id, two, CardLayer.Hand);

            var result = new GameOrchestrator().ApplyPlay(state, play, 0);

            Assert.IsTrue(result.Success, $"Coup rejeté : {result.RejectionReason}");

            var me = result.NewState.Players[0];
            Assert.IsTrue(me.Hand.Contains(two),
                "Le 2 doit être dans la main après ramassage (pas destruction).");
            Assert.AreNotEqual(0, result.NewState.ActivePlayerIndex,
                "Le tour doit passer au joueur suivant (pas de rejeu).");
            Assert.AreEqual(0, result.NewState.Pile.Cards.Count,
                "La pile doit être ramassée (donc vide).");
        }

        // ---------- CAS 2 : 2 = dernière carte du FaceUp, Talent → ramasse ----------

        [Test]
        public void LastTwo_FromFaceUp_InTalent_PlayerPicksUpPile()
        {
            var two = CreateCard(DefRank.Two);
            var active = CreatePlayer(0, "Active", DefPhase.Talent,
                new List<Card>(),
                new[] { two },
                new List<Card>());

            var state = BuildState(active, new[] { CreateCard(DefRank.Five) });
            var play = PlaySingle(active.Id, two, CardLayer.FaceUp);

            var result = new GameOrchestrator().ApplyPlay(state, play, 0);

            Assert.IsTrue(result.Success, $"Coup rejeté : {result.RejectionReason}");

            var me = result.NewState.Players[0];
            Assert.AreNotEqual(DefPhase.Finished, me.CurrentPhase,
                "Le joueur ne doit pas finir après avoir ramassé.");
            Assert.AreEqual(0, result.NewState.Pile.Cards.Count,
                "La pile doit être ramassée (donc vide).");
            Assert.AreNotEqual(0, result.NewState.ActivePlayerIndex,
                "Le tour doit passer au joueur suivant.");
        }

        // ---------- CAS 3 : 2 = dernière FaceDown, Luck → ramasse ----------

        [Test]
        public void LastTwo_Blind_FromFaceDown_PlayerPicksUpPile()
        {
            var two = CreateCard(DefRank.Two);
            var active = CreatePlayer(0, "Active", DefPhase.Luck,
                new List<Card>(),
                new List<Card>(),
                new[] { two });

            var state = BuildState(active, new[] { CreateCard(DefRank.Five) });

            var resolution = new GameOrchestrator().ApplyBlindPlay(state, 0, 0);

            Assert.IsTrue(resolution.Result.Success,
                $"Coup rejeté : {resolution.Result.RejectionReason}");
            Assert.AreEqual(two, resolution.RevealedCard,
                "La carte révélée doit être le 2.");

            var me = resolution.Result.NewState.Players[0];
            Assert.AreNotEqual(DefPhase.Finished, me.CurrentPhase,
                "Le joueur ne doit pas finir après avoir ramassé.");
            Assert.IsTrue(me.Hand.Contains(two),
                "Le 2 doit être dans la main après ramassage.");
            Assert.AreNotEqual(0, resolution.Result.NewState.ActivePlayerIndex,
                "Le tour doit passer au joueur suivant.");
        }

        // ---------- CAS 4 : 2 NON terminal en main → destruction + rejeu ----------

        [Test]
        public void TwoNotLast_FromHand_DestroysPile_GrantsReplay()
        {
            var two = CreateCard(DefRank.Two);
            var active = FilledPlayer(DefPhase.Work,
                new[] { two, CreateCard(DefRank.Nine) });

            var state = BuildState(active, new[] { CreateCard(DefRank.Five) });
            var play = PlaySingle(active.Id, two, CardLayer.Hand);

            var result = new GameOrchestrator().ApplyPlay(state, play, 0);

            Assert.IsTrue(result.Success, $"Coup rejeté : {result.RejectionReason}");
            Assert.AreEqual(0, result.NewState.Pile.Cards.Count,
                "La pile doit être DÉTRUITE (2 est destructeur).");
            Assert.AreEqual(0, result.NewState.ActivePlayerIndex,
                "Le joueur doit REJOUER (2 accorde un rejeu).");
        }

        // ---------- CAS 5 : 2 NON terminal en FaceDown (Luck) → destruction + rejeu ----------

        [Test]
        public void Blind_Two_NotLastFaceDown_DestroysPile_GrantsReplay()
        {
            var two = CreateCard(DefRank.Two);
            var active = CreatePlayer(0, "Active", DefPhase.Luck,
                new List<Card>(),
                new List<Card>(),
                new[] { two, CreateCard(DefRank.Nine, DefSuit.Spades) });

            var state = BuildState(active, new[] { CreateCard(DefRank.Five) });

            var resolution = new GameOrchestrator().ApplyBlindPlay(state, 0, 0);

            Assert.IsTrue(resolution.Result.Success,
                $"Coup rejeté : {resolution.Result.RejectionReason}");
            Assert.AreEqual(0, resolution.Result.NewState.Pile.Cards.Count,
                "La pile doit être détruite.");
            Assert.AreEqual(0, resolution.Result.NewState.ActivePlayerIndex,
                "Le joueur doit rejouer (2 accorde un rejeu).");
        }

        // ---------- CAS 6 : carte normale non terminale → pile conservée, tour passe ----------

        [Test]
        public void NormalCard_FromHand_DoesNotDestroyPile()
        {
            var five = CreateCard(DefRank.Five);
            // 2 cartes en main : après la pose il en reste une, donc ni reconstruction
            // ni transition de phase ne perturbent l'avancement du tour.
            var active = FilledPlayer(DefPhase.Work,
                new[] { five, CreateCard(DefRank.Nine) });

            var state = BuildState(active, new[] { CreateCard(DefRank.Five, DefSuit.Clubs) });
            var play = PlaySingle(active.Id, five, CardLayer.Hand);

            var result = new GameOrchestrator().ApplyPlay(state, play, 0);

            Assert.IsTrue(result.Success, $"Coup rejeté : {result.RejectionReason}");
            Assert.AreEqual(2, result.NewState.Pile.Cards.Count,
                "La pile ne doit pas être détruite : 5♣ initial + 5♥ posé.");
            Assert.AreNotEqual(0, result.NewState.ActivePlayerIndex,
                "Le tour doit passer au joueur suivant.");
        }
    }
}