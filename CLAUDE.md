# CLAUDE.md — Psycko Locked Rules Reference

**Purpose:** Capture all finalized, ambiguity-free game rules for consistency across implementation sessions.  
**Last Updated:** August 30, 2026  
**Status:** Locked — do not modify without explicit approval from Ekinox.

---

## Deck Composition

- **60 Cards:** 4 colors × 15 ranks (3, 4, 5, 6, 7, 8, 9, 10, Priest, Jack, Knight, Queen, King, Ace, 2)
- **3 Jokers (distinct):**
  - Glass Joker (Verre)
  - Black Joker / Pass (Noir/Passe)
  - Color Joker / Bomb (Couleur/Bombe)
- **Total:** 63 cards per deck

---

## Player Hand Distribution (Initial Setup)

Each player receives **9 cards** in **3 layers:**
1. **3 face-down cards** (revealed in Phase 3)
2. **3 face-up cards** (visible to all)
3. **3 hand cards** (private, in player's hand)

### Pre-Game Exchange Phase (~1 minute)
- Players may swap cards between **hand ↔ face-up cards** only.
- Face-down cards remain untouchable.

---

## Three Phases (Turn Order)

### Phase 1: Le Travail (The Work)
- Play from **hand only** until hand is empty.
- Main draw pile is active.

### Phase 2: Le Talent (The Talent)
- Triggered when **both hand and draw pile are empty**.
- **Face-up cards merge into hand** (become playable).
- Continue playing from this merged hand.

### Phase 3: La Chance (The Luck)
- Triggered when **face-up cards and hand are both empty**.
- **Reveal face-down cards one by one** on each turn.
- Play revealed cards immediately (if no immediate loss condition).

---

## Core Card Effects & Interactions

### Glass Joker (Joker de Verre)
- **Transparent** for:
  - Height reference (Pair/Triple/Quad matching)
  - Pair chain extension
  - Quad detection
- **Does NOT break chains** (unlike Black Joker).
- Example: `[5, Glass Joker, 5]` = valid Pair.

### Black Joker (Joker Noir / Passe)
- **Breaks all chains** (height, Pair, Quad).
- Acts as a reset point.
- Example: `[5, Black Joker, 5]` ≠ Pair (chain broken).

### Color Joker (Joker Couleur / Bombe)
- **Never grants a replay** (rejeu), even if cards remain in hand/pile.
- **No special chain behavior** — plays as a standalone bomb.

### Pair (Doublon)
- **Definition:** Same height on **two consecutive turns** (not two consecutive cards in sequence).
- **Transparent to Glass Joker:** `[5, Glass Joker]` then next turn `[5]` = Pair.
- **Disabled at 2-player table:** Pair logic does not trigger.
- **Quad (Carré) remains active at 2 players.**

### Quad (Carré)
- **Definition:** Four cards of same height in sequence.
- **Traverses Glass Jokers:** `[5, Glass Joker, 5, Glass Joker, 5, 5]` = valid Quad.
- **Does NOT traverse Black Jokers:** `[5, Black Joker, 5, 5, 5]` ≠ Quad.
- **Active at all player counts** (including 2 players).

### 7 Card (Don / Gift)
- In Phases 1–2: Grants **one free replay** per 7 played.
- In Phase 3 (La Chance): **Silent effect** — no replay or gift triggered (card is still face-down; no hand to gift from).

### 2 Card (Deux)
- **Cannot end a phase on a 2** — applies to all three phases (Travail, Talent, Chance).
- If a 2 is the last playable card, the player **must continue playing** if possible (draw, or trigger phase transition).

### Priest (Prêtre) & Knight (Cavalier)
- **Full interaction rules** with Pair/Quad/Jokers to be implemented as part of Core reconstruction.
- Placeholder status: rules confirmed in GDD, awaiting code implementation.

---

## Loss Condition

- **Last player with remaining cards = "Psycko"** (loser).
- Other three players rank by elimination order (1st to drop cards, 2nd, 3rd).

---

## Pre-Implementation Validation Checklist

Before coding any Core feature:
1. ✓ Rule is listed in this document (or explicitly added and approved).
2. ✓ Ambiguities are resolved (e.g., "consecutive turns" vs. "consecutive cards" for Pair).
3. ✓ Interactions with Jokers, edge cases documented.
4. ✓ Edge case: behavior at 2-player table confirmed.

---

## Stack & Architecture

- **Core Logic:** C# (no Unity dependencies) — testable in isolation via NUnit EditMode.
- **Testing:** NUnit EditMode syntax required for all new tests.
- **Simulation:** Mass bot simulations (500K+ games) validate structural correctness before UI integration.
- **Networking:** Photon Fusion (multplayer) — to be integrated post-Core validation.
- **Backend:** PlayFab (accounts, ELO, cosmetics).
---

## Phase 1 Implementation Status

**PHASE 1 — COMPLETE & LOCKED** ✅

All foundational data models implemented and tested:
- **T1:** Card, CardRank (15 ranks), CardSuit (4 colors), JokerType (3 types), Deck (63 cards) — 27/27 tests ✅
- **T2:** PowerCard (id, playerId, effectType, isUsed) — ✅
- **T3:** Player (hand, faceUpCards, faceDownCards, assignedPowerCard) — ✅
- **T4:** Pile (played cards) and base management — ✅

**Total Phase 1:** 62/62 NUnit EditMode tests passing - ✅

---

## Notes for Future Sessions

- **v0-legacy branch:** Contains all previous code (55-card deck, 125 tests, 500K simulations). Archive only — never merge or delete.
- **Reset rationale:** Deck expanded to 63 cards (added 2 Jokers, standardized ranks). Full rebuild ensures clean architecture.
- **Notion GDD:** Source of truth for rule clarifications. Sync with CLAUDE.md and code as rules evolve.

---

**End of CLAUDE.md**
