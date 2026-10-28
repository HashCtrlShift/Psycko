using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Services;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class UnityHumanAgentExtraTests
    {
        private sealed class Blind : IFaceDownBlindView { public int SelfFaceDownCount { get { return 2; } } }

        private static PlayerVisibleState View()
        {
            var deck = Deck.CreateFull(); deck.Shuffle(new Random(2));
            var state = CardDealer.Deal(deck, new List<(int Id, string Name)> { (0, "H"), (1, "A"), (2, "B"), (3, "C") });
            return PlayerVisibleState.FromExchange(state, 0);
        }

        private static Thread Start(Action action)
        {
            var t = new Thread(() => action()); t.Start(); return t;
        }

        private static void WaitPending(UnityHumanAgent a, HumanRequestKind kind)
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == kind, 2000));
        }

        // ── Constructeurs ────────────────────────────────────────────────
        [Test]
        public void ConstructeurParDefautEstInfini()
        {
            var a = new UnityHumanAgent();
            Assert.AreEqual(Timeout.InfiniteTimeSpan, a.Timeout);
            Assert.AreEqual(HumanRequestKind.None, a.PendingRequest);
            Assert.IsNull(a.PendingState);
            Assert.IsNull(a.PendingBlindView);
        }

        [Test]
        public void TimeoutNulOuNegatifLeveArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnityHumanAgent(TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnityHumanAgent(TimeSpan.FromSeconds(-1)));
        }

        [Test]
        public void TimeoutPositifEstConserve()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(5));
            Assert.AreEqual(TimeSpan.FromSeconds(5), a.Timeout);
        }

        // ── Timeout écoulé → repli ───────────────────────────────────────
        [Test]
        public void TimeoutEcouleUtiliseLeRepliEtNettoieLEtat()
        {
            var a = new UnityHumanAgent(TimeSpan.FromMilliseconds(50));
            var state = View();
            Assert.IsNull(a.ProposeNormalPlay(state));
            Assert.AreEqual(HumanRequestKind.None, a.PendingRequest);
            Assert.IsNull(a.PendingState);
            Assert.IsFalse(a.DecidePickup(state, false));
            Assert.IsTrue(a.DecidePickup(state, true));
            Assert.AreEqual(0, a.ProposeFaceDownIndex(new Blind()));
            Assert.AreEqual(0, a.ProposeExchanges(state).Count);
        }

        // ── Arguments nuls ───────────────────────────────────────────────
        [Test]
        public void SubmitPlayNulLeveArgumentNull()
        {
            var a = new UnityHumanAgent();
            Assert.Throws<ArgumentNullException>(() => a.SubmitPlay(null));
        }

        [Test]
        public void SubmitExchangesNulLeveArgumentNull()
        {
            var a = new UnityHumanAgent();
            Assert.Throws<ArgumentNullException>(() => a.SubmitExchanges(null));
        }

        // ── Soumissions hors requête ─────────────────────────────────────
        [Test]
        public void SoumissionsSansRequeteLevent()
        {
            var a = new UnityHumanAgent();
            var card = Card.CreateStandard(DefSuit.Clubs, DefRank.Three);
            Assert.Throws<InvalidOperationException>(() => a.SubmitNoPlay());
            Assert.Throws<InvalidOperationException>(() => a.SubmitPlay(Play.CreateSingle(0, card)));
            Assert.Throws<InvalidOperationException>(() => a.SubmitFaceDownIndex(0));
            Assert.Throws<InvalidOperationException>(() => a.SubmitGift(default(GiftResolutionChoice)));
            Assert.Throws<InvalidOperationException>(() => a.SubmitExchanges(new List<ExchangeChoice>()));
        }

        // ── Pas de pose volontaire ───────────────────────────────────────
        [Test]
        public void SubmitNoPlayRenvoieNull()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3));
            var state = View(); Play result = Play.CreateSingle(0, Card.CreateStandard(DefSuit.Clubs, DefRank.Three));
            var t = Start(() => result = a.ProposeNormalPlay(state));
            WaitPending(a, HumanRequestKind.NormalPlay);
            a.SubmitNoPlay();
            Assert.IsTrue(t.Join(3000));
            Assert.IsNull(result);
            Assert.AreEqual(HumanRequestKind.None, a.PendingRequest);
        }

        // ── Double requête simultanée ────────────────────────────────────
        [Test]
        public void SecondeRequeteSimultaneeLeveInvalidOperation()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3));
            var state = View();
            var t = Start(() => a.ProposeNormalPlay(state));
            WaitPending(a, HumanRequestKind.NormalPlay);
            Assert.Throws<InvalidOperationException>(() => a.DecidePickup(state, false));
            a.SubmitNoPlay();
            Assert.IsTrue(t.Join(3000));
        }

        // ── Etat exposé pendant l'attente ────────────────────────────────
        [Test]
        public void PendingBlindViewExposeePendantLAttente()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3));
            var blind = new Blind(); int? index = null;
            var t = Start(() => index = a.ProposeFaceDownIndex(blind));
            WaitPending(a, HumanRequestKind.FaceDownIndex);
            Assert.AreSame(blind, a.PendingBlindView);
            Assert.IsNull(a.PendingState);
            a.SubmitFaceDownIndex(0);
            Assert.IsTrue(t.Join(3000));
            Assert.AreEqual(0, index);
            Assert.IsNull(a.PendingBlindView);
        }

        // ── Cancel ───────────────────────────────────────────────────────
        [Test]
        public void CancelAvantRequeteUtiliseLeRepliImmediatement()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3));
            a.Cancel();
            var state = View();
            Assert.IsNull(a.ProposeNormalPlay(state));
            Assert.IsFalse(a.DecidePickup(state, false));
            Assert.IsTrue(a.DecidePickup(state, true));
        }

        [Test]
        public void CancelPendantPickupRenvoieLeRepli()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3));
            var state = View(); bool result = false;
            var t = Start(() => result = a.DecidePickup(state, true));
            WaitPending(a, HumanRequestKind.Pickup);
            a.Cancel();
            Assert.IsTrue(t.Join(3000));
            Assert.IsTrue(result);
        }
    }
}