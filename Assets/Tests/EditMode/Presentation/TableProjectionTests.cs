using System;
using System.Collections.Generic;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Presentation.UI;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class TableProjectionTests
    {
        private const int SelfId = 10;

        // ---------- Stubs alignés sur IPlayerVisibleState.cs / IOpponentVisibleInfo ----------
        private sealed class OpponentStub : IOpponentVisibleInfo
        {
            public int PlayerId { get; set; }
            public string Name { get; set; }
            public IReadOnlyList<Card> FaceUp { get; set; } = new List<Card>();
            public int HandCount { get; set; }
            public int FaceDownCount { get; set; }
            public DefPhase CurrentPhase { get; set; } = DefPhase.Work;
        }

        private sealed class StateStub : IPlayerVisibleState
        {
            public int SelfPlayerId { get; set; } = SelfId;
            public IReadOnlyList<Card> SelfHand { get; set; } = new List<Card>();
            public IReadOnlyList<Card> SelfFaceUp { get; set; } = new List<Card>();
            public IReadOnlyList<Card> SelfFaceDown { get; set; } = new List<Card>();
            public IReadOnlyList<IOpponentVisibleInfo> Opponents { get; set; } = new List<IOpponentVisibleInfo>();
            public Pile Pile { get; set; } = Pile.Empty;
            public int DrawPileCount { get; set; }
            public int ActivePlayerIndex { get; set; }
            public int ActivePlayerId { get; set; } = SelfId;
            public int GetActivePlayerId() { return ActivePlayerId; }
            public PlayDirection Direction { get; set; } = PlayDirection.Clockwise;
            public HeightConstraint Constraint { get; set; } = HeightConstraint.Normal;
            public DefRank RefRank { get; set; } = DefRank.Three;
        }

        // ---------- Fabriques ----------
        private static Card C(DefSuit suit, DefRank rank) { return Card.CreateStandard(suit, rank); }

        private static OpponentStub Opp(int id, string name, int hand, int faceDown, params Card[] faceUp)
        {
            return new OpponentStub
            {
                PlayerId = id,
                Name = name,
                HandCount = hand,
                FaceDownCount = faceDown,
                FaceUp = new List<Card>(faceUp)
            };
        }

        private static StateStub ThreeOpponentsState()
        {
            var s = new StateStub { DrawPileCount = 24 };
            s.Opponents = new List<IOpponentVisibleInfo>
            {
                Opp(11, "Bot1", 5, 3, C(DefSuit.Hearts, DefRank.Ace)),
                Opp(12, "Bot2", 4, 3),
                Opp(13, "Discard", 0, 1, C(DefSuit.Clubs, DefRank.King), C(DefSuit.Spades, DefRank.Two))
            };
            return s;
        }

        // ---------- Garde ----------
        [Test]
        public void Build_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TableProjection.Build(null));
        }

        // ---------- Compteurs adverses ----------
        [Test]
        public void Build_Opponents_CountersAreCorrect()
        {
            TableViewModel vm = TableProjection.Build(ThreeOpponentsState());

            Assert.AreEqual(3, vm.Opponents.Count);
            Assert.AreEqual(5, vm.Opponents[0].HandCount);
            Assert.AreEqual(1, vm.Opponents[0].FaceUpCount);
            Assert.AreEqual(3, vm.Opponents[0].FaceDownCount);
            Assert.AreEqual(4, vm.Opponents[1].HandCount);
            Assert.AreEqual(0, vm.Opponents[1].FaceUpCount);
            Assert.AreEqual(0, vm.Opponents[2].HandCount);
            Assert.AreEqual(2, vm.Opponents[2].FaceUpCount);
            Assert.AreEqual(1, vm.Opponents[2].FaceDownCount);
        }

        [Test]
        public void Build_Opponents_PlacedLeftTopRightInSeatOrder()
        {
            TableViewModel vm = TableProjection.Build(ThreeOpponentsState());

            Assert.AreEqual(SeatPosition.Left, vm.Opponents[0].Position);
            Assert.AreEqual(SeatPosition.Top, vm.Opponents[1].Position);
            Assert.AreEqual(SeatPosition.Right, vm.Opponents[2].Position);
            Assert.AreEqual(11, vm.Opponents[0].PlayerId);
            Assert.AreEqual(12, vm.Opponents[1].PlayerId);
            Assert.AreEqual(13, vm.Opponents[2].PlayerId);
        }

        [Test]
        public void Build_Opponents_NamesCopied_NullNameBecomesEmpty()
        {
            StateStub s = ThreeOpponentsState();
            ((OpponentStub)s.Opponents[1]).Name = null;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual("Bot1", vm.Opponents[0].Name);
            Assert.AreEqual(string.Empty, vm.Opponents[1].Name);
            Assert.AreEqual("Discard", vm.Opponents[2].Name);
        }

        [Test]
        public void Build_Opponent_NullFaceUp_CountsAsZero()
        {
            StateStub s = ThreeOpponentsState();
            ((OpponentStub)s.Opponents[0]).FaceUp = null;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(0, vm.Opponents[0].FaceUpCount);
        }

        [Test]
        public void Build_NullOpponentsList_YieldsNoOpponents()
        {
            StateStub s = ThreeOpponentsState();
            s.Opponents = null;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(0, vm.Opponents.Count);
        }

        [Test]
        public void Build_FewerThanThreeOpponents_PlacesFromLeft()
        {
            StateStub s = ThreeOpponentsState();
            s.Opponents = new List<IOpponentVisibleInfo> { Opp(11, "Bot1", 2, 1) };

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(1, vm.Opponents.Count);
            Assert.AreEqual(SeatPosition.Left, vm.Opponents[0].Position);
        }

        [Test]
        public void Build_MoreThanThreeOpponents_ExtrasIgnored()
        {
            StateStub s = ThreeOpponentsState();
            var list = new List<IOpponentVisibleInfo>(s.Opponents) { Opp(14, "Extra", 1, 1) };
            s.Opponents = list;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(3, vm.Opponents.Count);
        }

        // ---------- Aucun contenu caché ----------
        [Test]
        public void OpponentViewModel_ExposesNoCardMembers()
        {
            foreach (System.Reflection.PropertyInfo prop in typeof(OpponentSeatViewModel).GetProperties())
            {
                Assert.IsFalse(typeof(Card).IsAssignableFrom(prop.PropertyType),
                    "Carte exposée : " + prop.Name);
                Assert.IsFalse(typeof(IEnumerable<Card>).IsAssignableFrom(prop.PropertyType),
                    "Collection de cartes exposée : " + prop.Name);
            }
        }

        [Test]
        public void TableViewModel_OpponentsDoNotLeakAnyCard_ForEveryProjectedSeat()
        {
            TableViewModel vm = TableProjection.Build(ThreeOpponentsState());

            foreach (OpponentSeatViewModel o in vm.Opponents)
                foreach (System.Reflection.PropertyInfo prop in o.GetType().GetProperties())
                    Assert.IsNotInstanceOf<Card>(prop.GetValue(o, null));
        }

        // ---------- Joueur humain ----------
        [Test]
        public void Build_Self_LayersMatchVisibleState()
        {
            StateStub s = ThreeOpponentsState();
            s.SelfHand = new List<Card> { C(DefSuit.Clubs, DefRank.Three), C(DefSuit.Hearts, DefRank.Queen) };
            s.SelfFaceUp = new List<Card> { C(DefSuit.Spades, DefRank.Ten) };
            s.SelfFaceDown = new List<Card>
            {
                C(DefSuit.Diamonds, DefRank.Seven), C(DefSuit.Clubs, DefRank.Jack), Card.CreateJoker(DefJokerType.Glass)
            };

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(SelfId, vm.Self.PlayerId);
            Assert.AreEqual(SeatPosition.Bottom, vm.Self.Position);
            Assert.AreSame(s.SelfHand, vm.Self.Hand);
            Assert.AreSame(s.SelfFaceUp, vm.Self.FaceUp);
            Assert.AreSame(s.SelfFaceDown, vm.Self.FaceDown);
            Assert.AreEqual(2, vm.Self.Hand.Count);
            Assert.AreEqual(1, vm.Self.FaceUp.Count);
            Assert.AreEqual(3, vm.Self.FaceDown.Count);
        }

        [Test]
        public void SelfViewModel_NullLayers_BecomeEmpty()
        {
            var self = new SelfSeatViewModel(1, null, null, null, false);

            Assert.AreEqual(0, self.Hand.Count);
            Assert.AreEqual(0, self.FaceUp.Count);
            Assert.AreEqual(0, self.FaceDown.Count);
        }

        // ---------- Joueur actif ----------
        [Test]
        public void Build_ActiveIsSelf_OnlySelfFlagged()
        {
            StateStub s = ThreeOpponentsState();
            s.ActivePlayerId = SelfId;

            TableViewModel vm = TableProjection.Build(s);

            Assert.IsTrue(vm.Self.IsActive);
            foreach (OpponentSeatViewModel o in vm.Opponents) Assert.IsFalse(o.IsActive);
        }

        [Test]
        public void Build_ActiveIsOpponent_OnlyThatSeatFlagged()
        {
            StateStub s = ThreeOpponentsState();
            s.ActivePlayerId = 12;

            TableViewModel vm = TableProjection.Build(s);

            Assert.IsFalse(vm.Self.IsActive);
            Assert.IsFalse(vm.Opponents[0].IsActive);
            Assert.IsTrue(vm.Opponents[1].IsActive);
            Assert.IsFalse(vm.Opponents[2].IsActive);
        }

        // ---------- Zones ----------
        [Test]
        public void Build_EmptyPile_NoException_FlagsEmpty()
        {
            StateStub s = ThreeOpponentsState();
            s.Pile = Pile.Empty;
            s.DrawPileCount = 24;

            TableViewModel vm = null;
            Assert.DoesNotThrow(() => vm = TableProjection.Build(s));

            Assert.AreEqual(24, vm.Zone.DrawPileCount);
            Assert.AreEqual(0, vm.Zone.PileCardCount);
            Assert.IsTrue(vm.Zone.IsPileEmpty);
        }

        [Test]
        public void Build_NullPile_TreatedAsEmpty()
        {
            StateStub s = ThreeOpponentsState();
            s.Pile = null;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(0, vm.Zone.PileCardCount);
            Assert.IsTrue(vm.Zone.IsPileEmpty);
        }

        [Test]
        public void Build_NonEmptyPile_CountMatchesPileCards()
        {
            StateStub s = ThreeOpponentsState();
            Play pair = Play.Create(11, new[]
            {
                C(DefSuit.Clubs, DefRank.Nine), C(DefSuit.Hearts, DefRank.Nine)
            });
            Play single = Play.CreateSingle(12, C(DefSuit.Spades, DefRank.King));
            s.Pile = Pile.Empty.Add(pair).Add(single);

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(3, s.Pile.Count);
            Assert.AreEqual(3, vm.Zone.PileCardCount);
            Assert.IsFalse(vm.Zone.IsPileEmpty);
        }

        [Test]
        public void Build_PileWithJoker_CountsTheJokerCard()
        {
            StateStub s = ThreeOpponentsState();
            s.Pile = Pile.Empty.Add(Play.CreateSingle(11, Card.CreateJoker(DefJokerType.Glass)));

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(1, vm.Zone.PileCardCount);
            Assert.IsFalse(vm.Zone.IsPileEmpty);
        }

        [Test]
        public void Build_EmptyDrawPile_Zero()
        {
            StateStub s = ThreeOpponentsState();
            s.DrawPileCount = 0;

            TableViewModel vm = TableProjection.Build(s);

            Assert.AreEqual(0, vm.Zone.DrawPileCount);
        }

        [Test]
        public void ZoneViewModel_NegativeCounts_ClampedToZero()
        {
            var zone = new ZoneViewModel(-3, -1);

            Assert.AreEqual(0, zone.DrawPileCount);
            Assert.AreEqual(0, zone.PileCardCount);
            Assert.IsTrue(zone.IsPileEmpty);
        }

        [Test]
        public void ZoneViewModel_PositiveCounts_NotEmpty()
        {
            var zone = new ZoneViewModel(10, 4);

            Assert.AreEqual(10, zone.DrawPileCount);
            Assert.AreEqual(4, zone.PileCardCount);
            Assert.IsFalse(zone.IsPileEmpty);
        }

        // ---------- Constructeurs ----------
        [Test]
        public void OpponentSeatViewModel_NullName_BecomesEmpty()
        {
            var o = new OpponentSeatViewModel(1, null, SeatPosition.Top, 1, 2, 3, true);

            Assert.AreEqual(string.Empty, o.Name);
            Assert.AreEqual(SeatPosition.Top, o.Position);
            Assert.AreEqual(1, o.HandCount);
            Assert.AreEqual(2, o.FaceUpCount);
            Assert.AreEqual(3, o.FaceDownCount);
            Assert.IsTrue(o.IsActive);
        }
    }
}