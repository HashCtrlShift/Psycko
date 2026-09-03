## CLAUDE.md — Psycko Locked Rules Reference

**Purpose:** Capture all finalized, ambiguity-free game rules for consistency across implementation sessions.  
**Last Updated:** August 30, 2026  
**Status:** Locked — do not modify without explicit approval from Ekinox.

---

## Deck Composition

- **60 Cards:** 4 colors × 15 ranks (3, 4, 5, 6, 7, 8, 9, 10, Priest, Jack, Knight, Queen, King, Ace, 2)
- **3 Jokers (distinct):**
  - *Glass Joker* (Verre)
  - *Black Joker* / Pass (Noir/Passe)
  - *Color Joker* / Bomb (Couleur/Bombe)
- **Total:** 63 cards per deck

---

## Player Hand Distribution (Initial Setup)

Each player receives **9 cards** in **3 layers:**
1. **3 face-down cards** (revealed in Phase 3)
2. **3 face-up cards** (visible to all)
3. **3 hand cards** (private, in player's hand)

### Pre-Game Exchange Phase (~1 minute)
- Players may swap cards between **hand ↔ face-up cards** only.
- Face-down cards remain *untouchable*.

---

## Three Phases (Turn Order)

### Phase 1: Le Travail (The Work)
- Play from **hand only** until hand is empty.
- Main draw pile is *active*.

### Phase 2: Le Talent (The Talent)
- Triggered when **both hand and draw pile are empty**.
- **Face-up cards merge into hand** (become playable).
- Continue playing from this merged hand.

### Phase 3: La Chance (The Luck)
- Triggered when **face-up cards and hand are both empty**.
- **Reveal face-down cards one by one** on each turn.
- Play revealed cards **immediately** (if no immediate loss condition).

---

## Core Card Effects & Interactions

### Glass Joker (Joker de Verre)
- **Transparent** for:
  - *Height* : on regarde la carte EN-DESSOUS du Joker de Verre pour déterminer la contrainte applicable
  - *Applique la règle active* : ≥ (mode normal) ou ≤ (si Prêtre a été posé avant)
  - *Très restrictif possible* : ex. Joker de Verre sur As → on doit jouer ≥ As (Très Difficile : Jouables As / 2 / Autre Joker )
  - **Does NOT break chains** (unlike Black Joker).
  - Example: `[5, Glass Joker, 5]` = valid Pair : 
    - *Doublon* : compare à la carte EN-DESSOUS du Joker de Verre
   - *Carré* : s'insère au milieu/fin sans interrompre la chaîne
  - *Prêtre* : si Prêtre est avant, la contrainte ≤ traverse le Joker de Verre
  - *Jamais de rejeu, jamais de destruction de pile*

### Black Joker (Joker Noir / Passe)
- **Breaks all chains** (height, Pair, Quad).
- Acts as a reset point.
- Example: `[5, Black Joker, 5]` ≠ Pair (chain broken).

### **Joker Couleur (Color / Bombe)**
- **Détruit la pile** : cartes disparaissent du jeu (ne sont pas ramassées par le joueur suivant)
- **Jamais de rejeu** pour celui qui la pose
- **Joueur suivant ouvre une nouvelle pile** : hauteur libre (comme après Joker Noir)

---

## Clarifications Carré & Doublon — Verrouillées

### **Carré (Quad)**
-  **4 cartes de même hauteur** posées **consécutivement** dans la pile
-  **Joker de Verre transparent** : s'insère au milieu/fin sans interrompre la chaîne
  - Ex: 5♣, 5♦, Joker de Verre, 5♥, 5♠ = Carré valide
- **Joker Noir casse la chaîne** : arrête le compte, Carré non complété
-  **Reste actif même à 2 joueurs** : contrairement au Doublon
-  **Même joueur rejoue** : pile détruite, nouvelle pile ouverte

### **Doublon (Pair / Skip)**
-  **Deux tours consécutifs (pas deux cartes)** de même hauteur → tour du joueur suivant sauté
-  **Joker de Verre transparent** : Doublon compare à la carte EN-DESSOUS du Joker de Verre
-  **Joker Noir casse** : fin du Doublon immédiatement
-  **Désactivé à ≤2 joueurs** : évite les boucles infinies (ex: J1 joue 5, J2 joue 5, J1 serait sauté, J2 rejoue, boucle)
-  **Carré reste actif à ≤2 joueurs** : seul le Doublon se désactive

---

### 7 Card (Don / Gift)

  ### Effet de base
  - Quand un joueur pose un **7**, il doit faire un **Don** : donner une carte de son choix à un adversaire de son choix.
  - Le 7 reste en jeu sur la pile après avoir été posé.
  - Le joueur suivant doit ensuite jouer **≥ 7** (règle normale de hauteur).

  ### Condition d'applicabilité du Don

    #### Phase 1 → Phase 2 (transition critique)
    - Si le joueur pose un **7 comme dernière carte de Phase 1**, il ramasse d'abord ses cartes **Phase 2** (face découverte), qui deviennent sa nouvelle main.
    - **Après** avoir ramassé ses cartes Phase 2, il a alors une main remplie → le **Don est applicable**.
    - Ordre d'exécution exact : pose 7 → ramasse Phase 2 → fait le Don → attend son prochain tour en Phase 2.

    #### Phase 2 → Phase 3 (transition)
    - Si le joueur pose un **7 comme dernière carte de Phase 2**, il **passe à Phase 3**.
    - À ce moment il n'a plus de main (ses cartes Phase 2 sont épuisées, ses cartes Phase 3 face cachée ne sont pas encore en main).
    - **Le Don n'est pas applicable** (pas de cartes à donner).
    - Le 7 reste simplement sur la pile sans effet.

    #### Phase 3 (révélation face cachée)
    - Si le joueur **retourne un 7 face cachée** à son tour en Phase 3 : **aucun Don** (effet silencieux).
    - Raison : à ce stade le joueur n'a plus de main disponible (les cartes face cachée sont jouées sans main).

    #### Phase 3 (7 depuis la main — cas rare)
    - Si le joueur **pose un 7 depuis sa main en Phase 3** (après avoir ramassé d'autres cartes) : le Don est applicable si la main n'est pas vide après la pose.
    - Si c'est sa dernière carte en main : Don n'est pas applicable.

    #### Cas général (pas de transition de phase)
    - Le Don n'est possible **que si le joueur a encore des cartes en main** au moment de poser le 7.
    - Si le joueur n'a plus de cartes après avoir posé le 7, le Don **n'est pas applicable** — le 7 reste simplement sur la pile sans effet.

    ### Applicabilité à 2 joueurs
    - Le Don du 7 reste **applicable même à 2 joueurs restants** (contrairement au Doublon qui se désactive à 2 joueurs).

    ### Pas de rejeu
    - Le 7 n'entraîne pas de rejeu pour celui qui l'a posé (contrairement au Carré ou au "2").

  ### Destinataire du Don
  - Lors du Don d'un "7" : le joueur qui pose le 7 choisit le destinataire parmi tous les adversaires restants (y compris ceux en Phase 3), **sauf ceux qui ont déjà gagné**.
  - Le joueur en Phase 3 qui reçoit une carte reste en Phase 3 mais doit se débarrasser de sa main avant de pouvoir retourner une nouvelle carte face cachée.

### **2 (La Fermeture)**

  - **Détruit la pile** : cartes disparaissent (comme Bombe)
  - **Rejeu obligatoire** : celui qui pose le 2 ouvre une nouvelle pile

  - **Interdit de terminer une phase sur un 2**
    - **S'applique dans les 3 phases** : Travail, Talent, La Chance
    - **Cas limite** : si c'est la dernière carte du joueur, le joueur **ramasse** au lieu de rejouer

### Priest (Prêtre) 

1. **Effet de base** : le Prêtre inverse temporairement la règle de hauteur pour le joueur
   suivant uniquement, un seul tour. Mode normal : jouer ≥ sommet de pile. Prêtre posé :
   le joueur suivant doit jouer ≤ Prêtre (ComparisonMode.LessOrEqual). Retour automatique
   à GreaterOrEqual après ce tour.

2. **Position hiérarchique** : 10 < Prêtre < Valet. Sous contrainte ≤ Prêtre, un 10 est
   jouable, un Valet ne l'est pas.

3. **Enchaînement de Prêtres (Doublon)** : si le joueur sous contrainte rejoue un Prêtre,
   c'est un Doublon (deux Prêtres consécutifs) → le joueur suivant est sauté. La contrainte
   ≤ Prêtre reste active pour celui qui joue après le skip. La chaîne de Doublon persiste
   à travers les skips : chaque nouveau Prêtre posé sur un Prêtre précédent redéclenche un
   Doublon et un nouveau skip, indépendamment de qui a joué entre-temps.

   Exemple validé (T36) :
   P1 [Prêtre] → P2 [Prêtre] Doublon (P3 sauté) → P4 [Prêtre] Doublon (P1 sauté)
   → P2 [Prêtre] 4ᵉ consécutif → Carré détecté.

4. **Carré de Prêtres** : 4 Prêtres enchaînés consécutivement (via la chaîne de Doublons,
   en traversant les skips) déclenchent un Carré → pile détruite, le joueur qui complète
   le Carré rejoue/ouvre. La contrainte Prêtre disparaît avec la pile détruite (retour à
   GreaterOrEqual, pas de contrainte héritée pour la nouvelle pile).

5. **Transparence du Joker de Verre sous contrainte Prêtre** : un Joker de Verre posé
   pendant la contrainte ≤ Prêtre est transparent — il ne rompt pas la contrainte, la
   référence de hauteur reste le Prêtre pour le joueur suivant.
   Exemple validé : P1 [Prêtre] → P2 [Joker de Verre] → P3 doit jouer ≤ Prêtre.

6. **Cartes non-Prêtre sous contrainte** : toute carte ≤ Prêtre reste jouable normalement
   pendant la contrainte, sous réserve des autres règles (Doublon/Carré/2/Bombe).

7. **Durée de vie de la contrainte** : ≤ Prêtre ne s'applique qu'au tour immédiatement
   suivant la pose du Prêtre (ou sa relance via un nouveau Prêtre/Doublon). Réinitialisée
   dès que le tour sous contrainte passe sans nouveau Prêtre, ou qu'un Carré détruit la pile.

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

## Phases Implementations Status

**PHASE 1 — COMPLETE & LOCKED** ✅

All foundational data models implemented and tested:
- **T1:** Card, CardRank (15 ranks), CardSuit (4 colors), JokerType (3 types), Deck (63 cards) — 27/27 tests ✅
- **T2:** PowerCard (id, playerId, effectType, isUsed) — ✅
- **T3:** Player (hand, faceUpCards, faceDownCards, assignedPowerCard) — ✅
- **T4:** Pile (played cards) and base management — ✅

---
**PHASE 2 — COMPLETE & LOCKED** ✅ 

  - T5–T7qar : Logique de jeu (phases, tours, Carré, Doublon, 2, Bombe) - 151/151 tests✅

---
  
**PHASE 3 — COMPLETE & LOCKED** ✅
  - T7qui : Les 3 Jokers (vérification correcte)✅
  - T7hex : 7 (Le Don)✅
  - T7hep : Valet (L'Inverseur)✅
  - T7oct : Prêtre (Le Prêtre)✅


## Notes for Future Sessions

- **v0-legacy branch:** Contains all previous code (55-card deck, 125 tests, 500K simulations). Archive only — never merge or delete.
- **Reset rationale:** Deck expanded to 63 cards (added 2 Jokers, standardized ranks). Full rebuild ensures clean architecture.
- **Notion GDD:** Source of truth for rule clarifications. Sync with CLAUDE.md and code as rules evolve.

---

**End of CLAUDE.md**
