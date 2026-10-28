using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Services;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class UnityHumanAgentTests
    {
        private sealed class Blind : IFaceDownBlindView { public int SelfFaceDownCount { get { return 2; } } }
        private static PlayerVisibleState View()
        {
            var deck = Deck.CreateFull(); deck.Shuffle(new Random(2));
            var state = CardDealer.Deal(deck, new List<(int Id, string Name)> { (0,"H"),(1,"A"),(2,"B"),(3,"C") });
            return PlayerVisibleState.FromExchange(state, 0);
        }
        [Test] public void EtatInitialEtRequetePubliee()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3)); Assert.AreEqual(HumanRequestKind.None, a.PendingRequest);
            var state = View(); var done = new ManualResetEventSlim(); a.RequestPending += k => { Assert.AreEqual(HumanRequestKind.NormalPlay,k); Assert.AreSame(state,a.PendingState); };
            var t = new Thread(() => { a.ProposeNormalPlay(state); done.Set(); }); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.NormalPlay, 1000)); a.SubmitNoPlay(); Assert.IsTrue(done.Wait(3000)); t.Join();
        }
        [Test] public void ReponsesEtTypesErrones()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(4)); var state = View(); var card = Card.CreateStandard(DefSuit.Clubs, DefRank.Three); var play = Play.CreateSingle(0, card);
            Assert.Throws<InvalidOperationException>(() => a.SubmitPickup(true));
            object result = null; var t = new Thread(() => result = a.ProposeNormalPlay(state)); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest != HumanRequestKind.None, 1000));
            Assert.Throws<InvalidOperationException>(() => a.SubmitPickup(true)); a.SubmitPlay(play); t.Join(3000); Assert.AreSame(play, result);
            bool picked = false; t = new Thread(() => picked = a.DecidePickup(state, false)); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.Pickup,1000)); a.SubmitPickup(true); t.Join(3000); Assert.IsTrue(picked);
            int? index = null; t = new Thread(() => index = a.ProposeFaceDownIndex(new Blind())); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.FaceDownIndex,1000)); a.SubmitFaceDownIndex(1); t.Join(3000); Assert.AreEqual(1,index);
            GiftResolutionChoice gift = default(GiftResolutionChoice); t = new Thread(() => gift = a.ResolveGift(state)); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.Gift,1000)); var recipient = state.Opponents[0].PlayerId; var g = new GiftResolutionChoice(card,recipient); a.SubmitGift(g); t.Join(3000); Assert.AreEqual(recipient,gift.RecipientPlayerId);
            IReadOnlyList<ExchangeChoice> exchanges = null; t = new Thread(() => exchanges = a.ProposeExchanges(state)); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.Exchanges,1000)); var choices = new List<ExchangeChoice>(); a.SubmitExchanges(choices); t.Join(3000); Assert.AreSame(choices,exchanges);
        }
        [Test] public void AnnulationDebloqueEtUtiliseReplis()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(3)); var state = View(); Play result = null;
            var t = new Thread(() => result = a.ProposeNormalPlay(state)); t.Start(); Assert.IsTrue(SpinWait.SpinUntil(() => a.PendingRequest == HumanRequestKind.NormalPlay,1000)); a.Cancel(); Assert.IsTrue(t.Join(2000)); Assert.IsNull(result);
            Assert.IsTrue(a.DecidePickup(state,true)); Assert.AreEqual(0,a.ProposeFaceDownIndex(new Blind()));
            var gift = a.ResolveGift(state); Assert.AreEqual(state.SelfHand[0],gift.CardToGive); Assert.AreEqual(state.Opponents[0].PlayerId,gift.RecipientPlayerId); Assert.AreEqual(0,a.ProposeExchanges(state).Count);
        }
        private static UnityHumanAgent Short() { return new UnityHumanAgent(TimeSpan.FromMilliseconds(20)); }

        [Test] public void TimeoutGiftSansMainRenvoieDefault()
        {
            var a = Short();
            var r = a.ResolveGift(View());
            Assert.IsNotNull(r);
        }

        [Test] public void TimeoutFaceDownRenvoieZero()
        {
            Assert.AreEqual(0, Short().ProposeFaceDownIndex(new Blind()));
        }

        [Test] public void TimeoutExchangesRenvoieListeVide()
        {
            Assert.AreEqual(0, Short().ProposeExchanges(View()).Count);
        }

        [Test] public void TimeoutPickupForceRenvoieTrue()
        {
            Assert.IsTrue(Short().DecidePickup(View(), true));
        }

        [Test] public void CancelPuisToutesRequetesUtiliseRepli()
        {
            var a = new UnityHumanAgent(); a.Cancel();
            var s = View();
            Assert.IsNull(a.ProposeNormalPlay(s));
            Assert.AreEqual(0, a.ProposeFaceDownIndex(new Blind()));
            Assert.AreEqual(0, a.ProposeExchanges(s).Count);
            Assert.IsNotNull(a.ResolveGift(s));
        }

        [Test] public void RequestPendingEstDeclenche()
        {
            var a = Short(); HumanRequestKind got = HumanRequestKind.None;
            a.RequestPending += k => got = k;
            a.ProposeNormalPlay(View());
            Assert.AreEqual(HumanRequestKind.NormalPlay, got);
        }
        [Test] public void SubmitDansLeHandlerEstCouvert()
        {
            var a = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            a.RequestPending += k => a.SubmitNoPlay();
            Assert.IsNull(a.ProposeNormalPlay(View()));

            var b = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            b.RequestPending += k => b.SubmitPickup(true);
            Assert.IsTrue(b.DecidePickup(View(), false));

            var c = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            c.RequestPending += k => c.SubmitFaceDownIndex(1);
            Assert.AreEqual(1, c.ProposeFaceDownIndex(new Blind()));

            var d = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            d.RequestPending += k => d.SubmitExchanges(new List<ExchangeChoice>());
            Assert.AreEqual(0, d.ProposeExchanges(View()).Count);
        }
        [Test] public void SubmitGiftDansLeHandler()
        {
            var s = View(); var a = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            var g = new GiftResolutionChoice(s.SelfHand[0], s.Opponents[0].PlayerId);
            a.RequestPending += k => a.SubmitGift(g);
            Assert.AreEqual(g.RecipientPlayerId, a.ResolveGift(s).RecipientPlayerId);
        }

        [Test] public void SubmitPlayDansLeHandler()
        {
            var s = View(); var a = new UnityHumanAgent(TimeSpan.FromSeconds(2));
            var p = Play.CreateSingle(0, s.SelfHand[0]);
            a.RequestPending += k => a.SubmitPlay(p);
            Assert.AreSame(p, a.ProposeNormalPlay(s));
        }

        [Test] public void ReplisParTimeoutNormalPlayEtPickupNonForce()
        {
            var a = Short(); var s = View();
            Assert.IsNull(a.ProposeNormalPlay(s));
            Assert.IsFalse(a.DecidePickup(s, false));
        }

        [Test] public void ReplisGiftSansMainOuSansAdversaire()
        {
            var a = new UnityHumanAgent(); a.Cancel();
            Assert.AreEqual(default(GiftResolutionChoice).RecipientPlayerId, a.ResolveGift(null).RecipientPlayerId);
        }
        private static T RunAsync<T>(UnityHumanAgent a, Func<T> call, HumanRequestKind kind, Action submit)
        {
            var seen = new ManualResetEventSlim(false);
            a.RequestPending += k => { if (k == kind) seen.Set(); };
            var task = Task.Run(call);
            Assert.IsTrue(seen.Wait(2000));
            submit();
            Assert.IsTrue(task.Wait(2000));
            return task.Result;
        }

        [Test]
        public void ProposeNormalPlayRenvoieLeCoupSoumis()
        {
            var a = new UnityHumanAgent();
            var st = View();
            var card = st.SelfHand[0];
            var play = Play.CreateSingle(0, st.SelfHand[0]);
            var r = RunAsync(a, () => a.ProposeNormalPlay(st), HumanRequestKind.NormalPlay, () => a.SubmitPlay(play));
            Assert.AreSame(play, r);
            Assert.AreEqual(HumanRequestKind.None, a.PendingRequest);
        }

        [Test]
        public void SubmitNoPlayRenvoieNull()
        {
            var a = new UnityHumanAgent(); var st = View();
            var r = RunAsync(a, () => a.ProposeNormalPlay(st), HumanRequestKind.NormalPlay, () => a.SubmitNoPlay());
            Assert.IsNull(r);
        }

        [Test]
        public void DecidePickupRenvoieLaReponse()
        {
            var a = new UnityHumanAgent(); var st = View();
            var r = RunAsync(a, () => a.DecidePickup(st, false), HumanRequestKind.Pickup, () => a.SubmitPickup(true));
            Assert.IsTrue(r);
        }

        [Test]
        public void ProposeFaceDownIndexRenvoieLIndex()
        {
            var a = new UnityHumanAgent();
            var r = RunAsync(a, () => a.ProposeFaceDownIndex(new Blind()), HumanRequestKind.FaceDownIndex, () => a.SubmitFaceDownIndex(1));
            Assert.AreEqual(1, r);
        }

        [Test]
        public void ProposeExchangesRenvoieLaListe()
        {
            var a = new UnityHumanAgent(); var st = View();
            var list = new List<ExchangeChoice>().AsReadOnly();
            var r = RunAsync(a, () => a.ProposeExchanges(st), HumanRequestKind.Exchanges, () => a.SubmitExchanges(list));
            Assert.AreSame(list, r);
        }

        [Test]
        public void PendingStateEtBlindViewExposesPendantLaRequete()
        {
            var a = new UnityHumanAgent(); var st = View();
            var seen = new ManualResetEventSlim(false);
            a.RequestPending += k => seen.Set();
            var t = Task.Run(() => a.ProposeNormalPlay(st));
            Assert.IsTrue(seen.Wait(2000));
            Assert.AreEqual(HumanRequestKind.NormalPlay, a.PendingRequest);
            Assert.AreSame(st, a.PendingState);
            Assert.IsNull(a.PendingBlindView);
            a.SubmitNoPlay(); t.Wait(2000);
        }

        [Test]
        public void RequeteDoubleLeveInvalidOperation()
        {
            var a = new UnityHumanAgent(); var st = View();
            var seen = new ManualResetEventSlim(false);
            a.RequestPending += k => seen.Set();
            var t = Task.Run(() => a.ProposeNormalPlay(st));
            Assert.IsTrue(seen.Wait(2000));
            Assert.Throws<InvalidOperationException>(() => a.DecidePickup(st, false));
            a.SubmitNoPlay(); t.Wait(2000);
        }

        [Test]
        public void SubmitSansRequeteOuMauvaisTypeLeve()
        {
            var a = new UnityHumanAgent();
            Assert.Throws<InvalidOperationException>(() => a.SubmitPickup(true));
        }

        [Test]
        public void SubmitPlayNullEtExchangesNullLevent()
        {
            var a = new UnityHumanAgent();
            Assert.Throws<ArgumentNullException>(() => a.SubmitPlay(null));
            Assert.Throws<ArgumentNullException>(() => a.SubmitExchanges(null));
        }

        [Test]
        public void TimeoutInvalideLeve()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnityHumanAgent(TimeSpan.Zero));
            Assert.AreEqual(TimeSpan.FromSeconds(1), new UnityHumanAgent(TimeSpan.FromSeconds(1)).Timeout);
        }

        [Test]
        public void FallbackTimeoutPourChaqueType()
        {
            var a = new UnityHumanAgent(TimeSpan.FromMilliseconds(30));
            var st = View();
            Assert.IsNull(a.ProposeNormalPlay(st));
            Assert.IsTrue(a.DecidePickup(st, true));
            Assert.IsFalse(a.DecidePickup(st, false));
            Assert.AreEqual(0, a.ProposeFaceDownIndex(new Blind()));
            Assert.AreEqual(0, a.ProposeExchanges(st).Count);
            var gift = a.ResolveGift(st);
        }

        [Test]
        public void CancelDeclencheLeFallbackImmediatEtPourLesRequetesSuivantes()
        {
            var a = new UnityHumanAgent(); var st = View();
            a.Cancel();
            Assert.IsNull(a.ProposeNormalPlay(st));
            Assert.IsTrue(a.DecidePickup(st, true));
        }

        [Test]
        public void CancelPendantAttenteLibereLeThread()
        {
            var a = new UnityHumanAgent(); var st = View();
            var seen = new ManualResetEventSlim(false);
            a.RequestPending += k => seen.Set();
            var t = Task.Run(() => a.ProposeNormalPlay(st));
            Assert.IsTrue(seen.Wait(2000));
            a.Cancel();
            Assert.IsTrue(t.Wait(2000));
            Assert.IsNull(t.Result);
        }
    }
}
