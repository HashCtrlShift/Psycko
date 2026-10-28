using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class GameplayControllerTests
    {
        private static GameplayController Make(out LocalGameSession session, out GameObject go)
        {
            var human = new UnityHumanAgent(TimeSpan.FromSeconds(1));
            var layout = new SeatLayout(new System.Random(1)).Create(human, new CardPlayabilityChecker());
            session = new LocalGameSession(human, layout);
            go = new GameObject("gc");
            var gc = go.AddComponent<GameplayController>();
            gc.Attach(human, session);
            return gc;
        }

        [Test]
        public void GetSortedHandNulNeLevePas()
        {
            Assert.DoesNotThrow(() => GameplayController.GetSortedHand(null));
        }

        [Test]
        public void OnDestroySansSessionNeLevePas()
        {
            var go = new GameObject("gc");
            go.AddComponent<GameplayController>();
            Assert.DoesNotThrow(() => UnityEngine.Object.DestroyImmediate(go));
        }

        [Test]
        public void HumanEstNulAvantStart()
        {
            var go = new GameObject("gc");
            var gc = go.AddComponent<GameplayController>();
            Assert.IsNull(gc.Human);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickSansSessionNeLevePas()
        {
            var go = new GameObject("gc");
            var gc = go.AddComponent<GameplayController>();
            Assert.DoesNotThrow(() => gc.Tick());
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickRelaieLogRequeteEtFin()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            int log = 0, req = 0, fin = 0; var kind = HumanRequestKind.None;
            gc.LogEntryReceived += e => log++;
            gc.HumanRequestPending += k => { req++; kind = k; };
            gc.GameFinished += o => fin++;
            s.Enqueue(SessionEvent.Log(null));
            s.Enqueue(SessionEvent.Request(HumanRequestKind.Pickup));
            s.Enqueue(SessionEvent.Complete(null));
            gc.Tick();
            Assert.AreEqual(1, log);
            Assert.AreEqual(1, req);
            Assert.AreEqual(1, fin);
            Assert.AreEqual(HumanRequestKind.Pickup, kind);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickSansAbonnesNeLevePas()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            s.Enqueue(SessionEvent.Log(null));
            s.Enqueue(SessionEvent.Request(HumanRequestKind.None));
            s.Enqueue(SessionEvent.Complete(null));
            s.Enqueue(SessionEvent.Changed(null));
            Assert.DoesNotThrow(() => gc.Tick());
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickStateChangedNulEstRelaye()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            int changed = 0;
            gc.StateChanged += st => changed++;
            s.Enqueue(SessionEvent.Changed(null));
            gc.Tick();
            Assert.AreEqual(1, changed);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDestroyAvecSessionNeLevePas()
        {
            LocalGameSession s; GameObject go; Make(out s, out go);
            Assert.DoesNotThrow(() => UnityEngine.Object.DestroyImmediate(go));
        }

        [Test]
        public void TickStateChangedAvecEtatMetAJourLaMain()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            var deck = Deck.CreateFull(); deck.Shuffle(new System.Random(2));
            var pre = CardDealer.Deal(deck, new List<(int Id, string Name)> { (0,"H"),(1,"A"),(2,"B"),(3,"C") });
            var state = GameState.CreateInitial(pre.Players, pre.DrawPile);
            int changed = 0;
            gc.StateChanged += st => changed++;
            s.Enqueue(SessionEvent.Changed(state));
            gc.Tick();
            Assert.AreEqual(1, changed);
            Assert.AreEqual(state.Players[0].Hand.Count, gc.HumanHandSorted.Count);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test] public void BeginCreeSessionEtAgent()
        {
            var go = new GameObject("gc"); var gc = go.AddComponent<GameplayController>();
            gc.Begin();
            Assert.IsNotNull(gc.Human);
            gc.Tick();
            UnityEngine.Object.DestroyImmediate(go);   // OnDestroy annule la session
        }

        [Test] public void GetSortedHandAvecEtat()
        {
            var deck = Deck.CreateFull(); deck.Shuffle(new System.Random(2));
            var st = CardDealer.Deal(deck, new System.Collections.Generic.List<(int Id, string Name)> { (0,"H"),(1,"A"),(2,"B"),(3,"C") });
            Assert.Greater(GameplayController.GetSortedHand(PlayerVisibleState.FromExchange(st, 0)).Count, 0);
        }
        [Test]
        public void TickLogEntryEtHumanRequestEtFinished()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            int log = 0, req = 0, fin = 0;
            gc.LogEntryReceived += e => log++;
            gc.HumanRequestPending += k => req++;
            gc.GameFinished += o => fin++;
            s.Enqueue(SessionEvent.Log(null));
            s.Enqueue(SessionEvent.Request(HumanRequestKind.Pickup));
            s.Enqueue(SessionEvent.Complete(default(GameRunOutcome)));
            gc.Tick();
            Assert.AreEqual(1, log);
            Assert.AreEqual(1, req);
            Assert.AreEqual(1, fin);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickSansAbonnesNePlantePas()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            s.Enqueue(SessionEvent.Log(null));
            s.Enqueue(SessionEvent.Request(HumanRequestKind.Pickup));
            s.Enqueue(SessionEvent.Complete(default(GameRunOutcome)));
            s.Enqueue(SessionEvent.Changed(null));
            Assert.DoesNotThrow(() => gc.Tick());
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TickSansSessionNeFaitRien()
        {
            var go = new GameObject();
            var gc = go.AddComponent<GameplayController>();
            Assert.DoesNotThrow(() => gc.Tick());
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void GetSortedHandNullRenvoieListeVide()
        {
            Assert.IsNotNull(GameplayController.GetSortedHand(null));
        }

        [Test]
        public void HumanExposeLAgentAttache()
        {
            LocalGameSession s; GameObject go; var gc = Make(out s, out go);
            Assert.IsNotNull(gc.Human);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void BeginCreeSessionEtOnDestroyAnnule()
        {
            var go = new GameObject();
            var gc = go.AddComponent<GameplayController>();
            gc.Begin();
            Assert.IsNotNull(gc.Human);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}