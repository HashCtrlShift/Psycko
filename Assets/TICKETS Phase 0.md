# TICKETS Phase 0 — Fondations

**Objectif de la Phase 0 :** poser les fondations du Core (Psycko.Core, C# pur, zéro dépendance Unity) : le modèle de données des cartes et du deck, le service de comparaison de hauteur, la couche d'interfaces de contrats entre Core, Bots et Présentation, ainsi que la chaîne d'orchestration des transitions d'état. Aucune règle de jeu n'est implémentée dans cette phase : elle prépare le terrain pour les phases suivantes.

**Statut global :** compilation validée, jeu fonctionnel en jeu (Unity), règles du Core déjà posées, validées et testées in-game. La validation par tests unitaires sera formalisée en Phase 6.

---

## T0 — Domain de base : Card, Deck (63 cartes) et HeightComparison

**Branche** : `feature/T0-01-domain-base-deck-comparison`
**Phase** : 0 — Fondations
**Couches touchées** : Core uniquement (Domain + Rules/Comparison)
**Fichiers** : `Card.cs`, `Deck.cs`, `HeightComparison.cs`

### 1. Objectif

Poser la fondation du Core : un modèle `Card` immuable, un `Deck` qui génère les 63 cartes du jeu, et le service `HeightComparison` qui définit l'ordre de hauteur.

### 2. Contraintes

- Core = C# pur, zéro dépendance Unity.
- Modèles du Domain immuables.
- 1 responsabilité = 1 fichier.
- Cavalier et Prêtre existent dès le modèle de données.
- Les cartes spéciales et Jokers existent comme rangs/types, sans implémentation comportementale (Phase 2).
- Aucun push GitHub sans validation d'Ekinox.

### 3. Deck (63 cartes)

| Composition | Détail |
|---|---|
| 60 cartes | 4 couleurs × 15 rangs : 3, 4, 5, 6, 7, 8, 9, 10, Prêtre, Valet, Cavalier, Dame, Roi, As, 2 |
| 3 Jokers uniques | Verre, Noir/Passe, Couleur/Bombe |

Aucun doublon, aucun rang manquant. Les Jokers n'ont ni rang ni couleur ordinaires.

### 4. Ordre de hauteur

Croissant : 3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < Prêtre < Valet < Cavalier < Dame < Roi < As < 2.
Le 2 est la carte la plus haute.

`HeightComparison` ne gère que la comparaison des rangs ordinaires. Le comportement des Jokers et des cartes spéciales est hors périmètre.

### 5. Fichiers et changements

| Fichier | Changement |
|---|---|
| `Core/Domain/Card.cs` | Modèle immuable (rang, couleur, type Joker le cas échéant) |
| `Core/Domain/Deck.cs` | Génération des 63 cartes |
| `Core/Rules/Comparison/HeightComparison.cs` | Ordre de hauteur et comparaison |

### 6. Validation

- [x] Compilation propre.
- [x] Deck = 63 cartes, sans doublon.
- [x] Ordre de hauteur complet vérifié.
- [x] Aucune référence Unity dans Core.
- [x] Validé en jeu.

### 7. Hors périmètre

- Pile, Player, GameState.
- Detection (PairDetection, QuadDetection).
- Validation (CardPlayability, LastCardValidator).
- Phase, SpecialCards, Jokers (résolveurs).
- Bots et Présentation.

---

## T2 — Couche Interfaces du Core (contrats Query / Command / Playability)

**Branche** : `feature/T2-interfaces-core`
**Phase** : 0 — Fondations
**Couches touchées** : Core uniquement (Interfaces + ajustement mineur Domain)
**Fichiers** : `IGameStateQuery.cs`, `IGameStateCommand.cs`, `ICardPlayabilityChecker.cs`, déplacement de `HeightConstraint`

### 1. Objectif

Poser la couche `Interfaces/` : les contrats de lecture de l'état, de transition d'état et de validation d'un coup. Ces contrats sont la frontière entre Core, Bots et Présentation.

### 2. Contraintes

- Core = C# pur, zéro dépendance Unity.
- 1 responsabilité = 1 fichier.
- Aucune logique de jeu dans `Interfaces/`.
- Domain ne dépend jamais de Rules ; Rules dépend de Domain.
- Immuabilité préservée : toute mutation retourne un nouvel état.

### 3. Périmètre : trois contrats

| Fichier | Rôle |
|---|---|
| `IGameStateQuery.cs` | Lecture seule : joueurs, pile, pioche, joueur actif, contrainte active, rang de référence |
| `IGameStateCommand.cs` | Transitions retournant un nouvel état |
| `ICardPlayabilityChecker.cs` | Contrat de validation d'un coup |

### 4. Décision de conception

`GameState` (Domain) important `HeightConstraint` depuis Rules violait la règle « Domain ne dépend jamais de Rules ». Décision retenue : déplacer `HeightConstraint` dans `Domain/`, consignée dans CLAUDE.md et Notion.

### 5. Fichiers et changements

| Fichier | Changement |
|---|---|
| `Interfaces/IGameStateQuery.cs` | Nouveau |
| `Interfaces/IGameStateCommand.cs` | Nouveau |
| `Interfaces/ICardPlayabilityChecker.cs` | Nouveau |
| `Domain/GameState.cs` | Implémente les contrats ; retrait de l'import Rules |
| `Domain/DefCard.cs` ou `DefConstraint.cs` | Accueil de `HeightConstraint` |
| `Rules/Comparison/HeightComparison.cs` | Adaptation de l'import |

### 6. Validation

- [x] Compilation propre.
- [x] Décision `HeightConstraint` prise et consignée.
- [x] Les trois interfaces validées.
- [x] Validé en jeu.
- [ ] Mise à jour Notion et CLAUDE.md après merge.

### 7. Hors périmètre

- `Rules/Detection/` (PairDetection, QuadDetection).
- `Validation/`, `Phase/`, `SpecialCards/`, `Jokers/`, `Services/`.
- Bots, Présentation, réseau.

---

## T2 — Architecture de la chaîne Core : Query / Command / Orchestrator

**Branche** : `feature/T0-XX-chaine-core-architecture`
**Phase** : 0 — Fondations
**Couches touchées** : Domain, Interfaces, Services (documentation de contrat, aucune règle de jeu)
**Fichiers** : `GameState.cs`, `IGameStateQuery.cs`, `IGameStateCommand.cs`, `IGameState.cs`, `GameOrchestrator.cs`

### 1. Objectif

Figer la chaîne d'appel entre les couches Core avant de construire `TurnManager` et les règles. Chaque pièce a un rôle unique et un seul point d'entrée pour les transitions d'état.

### 2. Contraintes

- Aucune logique de règle dans `GameState` ni dans les interfaces.
- `GameState` est immuable : toute transition retourne un nouvel état.
- `Rules/` ne dépend que de `IGameStateQuery`, jamais de `GameState` concret.
- `TurnManager` n'appelle jamais `IGameStateCommand` directement.

### 3. Chaîne d'appel

Presentation / Bots → `GameOrchestrator` (seul point d'entrée des transitions) → lecture via `IGameStateQuery` (implémenté par `GameState`) → validation par `Rules/` → si valide, transition via `IGameStateCommand` (implémenté par `GameState`) → nouveau `GameState` immuable → `TurnManager` (ordre des tours, passage au suivant).

### 4. Rôle de chaque pièce

| Pièce | Couche | Rôle |
|---|---|---|
| `GameState` | Domain | Snapshot immuable (joueurs, pile, pioche, contrainte active, joueur courant). Implémente Query et Command. Aucune règle de jeu. |
| `IGameStateQuery` | Interfaces | Contrat de lecture seule, vu par `Rules/`. |
| `IGameStateCommand` | Interfaces | Transitions mécaniques brutes, retournent un nouvel état sans juger de la légalité. |
| `IGameState` | Interfaces | Union de Query et Command, utilisée uniquement par `GameOrchestrator`. |
| `GameOrchestrator` | Services | Seul appelant autorisé de `IGameStateCommand` : reçoit l'intention, la fait valider par `Rules/`, applique la transition, retourne le nouvel état. |
| `TurnManager` | Services | Mécanique de tour uniquement. Passe toujours par `GameOrchestrator`. |

### 5. Règles de dépendance

1. `Rules/` → `IGameStateQuery` uniquement.
2. `GameOrchestrator` → Query, Command, `Rules/`.
3. `TurnManager` → `GameOrchestrator` uniquement.
4. Presentation / Bots → `GameOrchestrator` uniquement.

### 6. Fichiers et changements

| Fichier | Changement |
|---|---|
| `GameState.cs` | Vérifier l'implémentation de Query et Command, aucune règle ajoutée |
| `IGameStateQuery.cs` | Lecture seule |
| `IGameStateCommand.cs` | Transitions mécaniques, retour d'un nouvel état |
| `IGameState.cs` | Union des deux |
| `GameOrchestrator.cs` | Stub, complété au ticket de l'orchestration |

### 7. Risques

1. `GameOrchestrator` vide pendant la construction de `TurnManager` : risque de dispersion des appels de transition. Mitigation : interdire tout appel direct à `IGameStateCommand` hors `GameOrchestrator`.
2. Dérive de `GameState` vers de la logique de règle. Mitigation : revue de code.

### 8. Validation

- [x] Compilation propre (`Psycko.Core` sans dépendance Unity).
- [x] Aucune référence à `GameState` concret dans `Rules/`.
- [x] Aucune référence à `IGameStateCommand` dans `TurnManager`.
- [x] Validé en jeu.

### 9. Hors périmètre

- Règles de jeu (Phase 1+).
- Implémentation complète de `GameOrchestrator` (ticket dédié).
- Implémentation complète de `TurnManager`.
- Présentation et Bots.

---

## T4 — Harmonisation de l'API des handlers Jokers et SpecialCards

**Branche** : `feature/T4-harmonisation-api-handlers`
**Phase** : 0 — Fondations
**Couches touchées** : Core uniquement (`Rules/Jokers/`, `Rules/SpecialCards/`)
**Fichiers** : `ColorJokerResolver.cs`, `BlackJokerResolver.cs`, `TwoHandler.cs`, `PriestHandler.cs`, `JackHandler.cs`

### 1. Objectif

Uniformiser la forme des propriétés de comportement des handlers de cartes spéciales et des Jokers, afin qu'un même concept s'exprime toujours avec le même nom et la même signature. Aucune règle de jeu n'est modifiée.

### 2. Contraintes

- Aucune logique de jeu modifiée : uniquement signatures et noms.
- `GlassJokerResolver` reste inchangé : il lit réellement la pile et conserve son paramètre de lecture.
- Pas de dépendance Unity.

### 3. Convention retenue

- Une propriété constante (valeur fixe, indépendante de l'état) est une propriété sans paramètre.
- Une méthode n'est gardée que si la réponse dépend réellement de l'état de lecture.
- Le concept « après destruction ou ramassage de la pile, le poseur enchaîne-t-il ? » est nommé `GrantsReplay` partout (remplace `PlayerPlaysAgain`).

### 4. Changements

| Fichier | Avant | Après |
|---|---|---|
| `ColorJokerResolver.cs` | `PlayerPlaysAgain` (méthode) | `GrantsReplay` (propriété) |
| `ColorJokerResolver.cs` | `IsAlwaysPlayable` (méthode) | `IsAlwaysPlayable` (propriété) |
| `ColorJokerResolver.cs` | `DestroysPile` (méthode) | `DestroysPile` (propriété) |
| `BlackJokerResolver.cs` | `IsAlwaysPlayable` (méthode) | `IsAlwaysPlayable` (propriété) |
| `BlackJokerResolver.cs` | `ResetsConstraint` (méthode) | `ResetsConstraint` (propriété) |

`TwoHandler`, `PriestHandler`, `JackHandler` : déjà conformes, aucun changement.

### 5. Validation

- [x] Compilation propre (Core).
- [x] Aucune occurrence restante de `PlayerPlaysAgain` dans le dépôt.
- [x] Validé en jeu.

### 6. Hors périmètre

- Modification de `GlassJokerResolver`.
- Implémentation de la logique de `Phase/` consommant ces propriétés.

---

## Synthèse Phase 0

| Ticket | Sujet | Statut |
|---|---|---|
| T0 | Domain de base, Deck 63 cartes, HeightComparison | ✅ Validé |
| T2 | Interfaces Core (Query / Command / Playability) | ✅ Validé |
| T2 (chaîne) | Architecture Query / Command / Orchestrator | ✅ Validé |
| T4 | Harmonisation API handlers Jokers et SpecialCards | ✅ Validé |