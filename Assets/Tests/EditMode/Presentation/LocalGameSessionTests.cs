using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Validation;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class LocalGameSessionTests
    {
        private sealed class Pilot : IDisposable
        {
            private readonly UnityHumanAgent _human; private readonly CardPlayabilityChecker _checker; private readonly Thread _thread; private volatile bool _stop;
            internal Exception Error;
            internal Pilot(UnityHumanAgent human, CardPlayabilityChecker checker)
            {
                _human=human; _checker=checker; _human.RequestPending += OnRequest;
                _thread=new Thread(() => { while (!_stop) Thread.Sleep(1); }); _thread.IsBackground=true; _thread.Start();
            }
            private void OnRequest(HumanRequestKind kind)
            {
                try
                {
                    var state = _human.PendingState;
                    switch (kind)
                    {
                        case HumanRequestKind.NormalPlay:
                            Card card = state.SelfHand.OrderBy(c => c.IsJoker ? 1 : 0).ThenBy(c => c.Rank).FirstOrDefault(c => _checker.IsPlayable(c,state));
                            if (card == null) _human.SubmitNoPlay(); else _human.SubmitPlay(Play.CreateSingle(state.SelfPlayerId,card)); break;
                        case HumanRequestKind.Pickup: _human.SubmitPickup(true); break;
                        case HumanRequestKind.FaceDownIndex: _human.SubmitFaceDownIndex(0); break;
                        case HumanRequestKind.Gift:
                            var opponent=state.Opponents.FirstOrDefault(o => o.CurrentPhase != DefPhase.Finished);
                            if (state.SelfHand.Count > 0 && opponent != null) _human.SubmitGift(new GiftResolutionChoice(state.SelfHand[0],opponent.PlayerId)); else _human.Cancel(); break;
                        case HumanRequestKind.Exchanges: _human.SubmitExchanges(new List<ExchangeChoice>()); break;
                    }
                }
                catch(Exception ex) { Error=ex; _human.Cancel(); }
            }
            public void Dispose() { _stop=true; _thread.Join(1000); _human.RequestPending -= OnRequest; }
        }
        [Test, Timeout(60000)] public void VingtPartiesCompletesAvecPiloteHumain()
        {
            var watch=Stopwatch.StartNew(); var checker=new CardPlayabilityChecker();
            for (int seed=0;seed<20;seed++)
            {
                var human=new UnityHumanAgent(TimeSpan.FromSeconds(10)); using(var pilot=new Pilot(human,checker))
                {
                    var layout=new SeatLayout(new Random(seed)).Create(human,checker); var session=new LocalGameSession(human,layout); session.Start(seed);
                    Assert.IsTrue(SpinWait.SpinUntil(() => session.IsFinished,2500),"La partie a dépassé le délai pour la seed "+seed);
                    Assert.IsNull(pilot.Error); Assert.IsNull(session.Outcome.Error); Assert.IsFalse(session.Outcome.Result.IsError); Assert.AreEqual(4,session.Outcome.Result.RankingBySeat.Count);
                }
                Assert.Less(watch.Elapsed,TimeSpan.FromSeconds(60));
            }
        }
        [Test, Timeout(10000)] public void AnnulationNeLaissePasLaSessionBloquee()
        {
            var checker=new CardPlayabilityChecker(); var human=new UnityHumanAgent(); var session=new LocalGameSession(human,new SeatLayout(new Random(1)).Create(human,checker)); session.Start(42);
            Assert.IsTrue(SpinWait.SpinUntil(() => human.PendingRequest != HumanRequestKind.None || session.IsFinished,3000)); session.Cancel();
            Assert.IsTrue(SpinWait.SpinUntil(() => session.IsFinished,3000)); Assert.IsNotNull(session.Outcome); Assert.IsNull(session.Outcome.Error);
        }
        [Test] public void ConstructeurArgumentsNuls()
        {
            var h = new UnityHumanAgent();
            var layout = new SeatLayout(new Random(1)).Create(h, new CardPlayabilityChecker());
            Assert.Throws<ArgumentNullException>(() => new LocalGameSession(null, layout));
            Assert.Throws<ArgumentNullException>(() => new LocalGameSession(h, null));
        }

        [Test, Timeout(10000)] public void DoubleStartLeveEtIsRunning()
        {
            var h = new UnityHumanAgent();
            var s = new LocalGameSession(h, new SeatLayout(new Random(1)).Create(h, new CardPlayabilityChecker()));
            Assert.IsFalse(s.IsRunning);
            s.Start(7);
            Assert.Throws<InvalidOperationException>(() => s.Start(8));
            Assert.IsTrue(SpinWait.SpinUntil(() => h.PendingRequest != HumanRequestKind.None || s.IsFinished, 3000));
            s.Cancel();
            Assert.IsTrue(SpinWait.SpinUntil(() => s.IsFinished, 3000));
            Assert.IsFalse(s.IsRunning);
        }
        private static LocalGameSession NewSession(out UnityHumanAgent human)
        {
            human = new UnityHumanAgent(TimeSpan.FromMilliseconds(20));
            var layout = new SeatLayout(new System.Random(1)).Create(human, new CardPlayabilityChecker());
            return new LocalGameSession(human, layout);
        }

        [Test]
        public void ConstructeurRefuseNull()
        {
            var h = new UnityHumanAgent();
            Assert.Throws<ArgumentNullException>(() => new LocalGameSession(null, null));
            Assert.Throws<ArgumentNullException>(() => new LocalGameSession(h, null));
        }

        [Test]
        public void StartLanceLaPartieEtTermine()
        {
            UnityHumanAgent h; var s = NewSession(out h);
            Assert.IsFalse(s.IsRunning);
            Assert.IsFalse(s.IsFinished);
            s.Start(42);
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (!s.IsFinished && DateTime.UtcNow < deadline) Thread.Sleep(20);
            Assert.IsTrue(s.IsFinished);
            Assert.IsFalse(s.IsRunning);
            Assert.IsNotNull(s.Outcome);
        }

        [Test]
        public void StartDeuxFoisLeve()
        {
            UnityHumanAgent h; var s = NewSession(out h);
            s.Start(1);
            Assert.Throws<InvalidOperationException>(() => s.Start(2));
            s.Cancel();
        }

        [Test]
        public void LesEvenementsLogEtEtatEtFinSontProduits()
        {
            UnityHumanAgent h; var s = NewSession(out h);
            s.Start(7);
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (!s.IsFinished && DateTime.UtcNow < deadline) Thread.Sleep(20);
            var kinds = new HashSet<SessionEventKind>();
            SessionEvent e;
            while (s.TryDequeue(out e)) kinds.Add(e.Kind);
            Assert.IsTrue(kinds.Contains(SessionEventKind.LogEntry));
            Assert.IsTrue(kinds.Contains(SessionEventKind.StateChanged));
            Assert.IsTrue(kinds.Contains(SessionEventKind.Finished));
        }
    }
}
