# CLAUDE.md — Référence des Règles Verrouillées de Psycko

**Objectif :** Capturer toutes les règles de jeu finalisées et sans ambiguïté pour la cohérence entre les sessions d'implémentation.  
**Statut :** Verrouillé — ne modifier que avec l'approbation explicite d'Ekinox.

---

## Stack Technique & Implémentation

- **Engine :** Unity 6000.3.19f1 LTS (2D Universel / URP)
- **Langage :** C#
- **IDE :** VS Code (C# Dev Kit + extension Unity)
- **Réseau :** Photon Fusion (multijoueur en ligne)
- **Backend :** PlayFab (comptes, ELO, inventaire cosmétique)
- **Monétisation :** Unity IAP (cosmétiques uniquement)
- **Tests :** NUnit (EditMode)
- **Versionning :** Git + GitHub Desktop
- **Design :** Figma (UI/UX), Inkscape, GIMP
- **Palette de marque :** Nuit #100d1f, Violet profond #26215C, Violet clair #7F77DD, Or vif #EF9F27, Or pâle #FAC775. Wordmark : Rajdhani Bold 700.

---

## Composition du Paquet

- **60 Cartes :** 4 couleurs × 15 rangs (3, 4, 5, 6, 7, 8, 9, 10, Prêtre, Valet, Cavalier, Dame, Roi, As, 2)
- **Hauteurs des Cartes (ordre croissant) :** 3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < Prêtre < Valet < Cavalier < Dame < Roi < As < 2
  - **Le 2 est la carte la plus élevée.**
- **Couleurs (4) :** Cosmétiques uniquement, sauf pour déterminer le premier joueur de la partie.
- **3 Jokers (exemplaires uniques) :**
  - *Joker de Verre* — 1 exemplaire unique
  - *Joker Noir / Passe* — 1 exemplaire unique
  - *Joker Couleur / Bombe* — 1 exemplaire unique
- **Total :** 63 cartes par paquet

---

## Phase d'Échange Pré-Jeu (~1 minute)

### Objectif
Permettre aux joueurs d'ajuster leur stratégie initiale en échangeant des cartes entre leur **main** et leurs **cartes face découverte** avant le démarrage officiel du jeu.

### Mécanique d'Échange

- **Échanges simultanés** : Tous les joueurs échangent en même temps
- **Ratio** : À chaque échange, **1 carte Main ↔ 1 carte Face Découverte**
- **Répétitions** : Un joueur peut effectuer **autant d'échanges qu'il souhaite** dans le délai imparti (limité par le serveur)
- **Isolation** : Chaque joueur a son propre groupe de **3 cartes Face Découverte** — impossible de se battre pour la même carte

### Visibilité

- **Cartes Face Découverte** : Les changements sont **visibles en temps réel** à tous les joueurs (car elles sont face visible)
- **Cartes en Main** : Restent **privées** (chaque joueur ne voit que sa propre main)

### Fin de Phase d'Échange

- Les joueurs cliquent sur un bouton **"Prêt !"** pour signaler leur fin d'échange
- **Automatique après 1 minute** : Si un joueur n'a pas cliqué "Prêt !", la phase se termine quand même
- **Démarrage du jeu** : Une fois tous les joueurs prêts (ou délai écoulé), la Phase 1 (Le Travail) commence

---

## Phase 1 : Le Travail (The Work)

### Objectif
Les joueurs jouent **à tour de rôle** en posant des cartes de leur **main** jusqu'à ce qu'elle soit vide.

### Conditions de Jeu

#### Cartes Jouables
- **Source unique** : Les cartes proviennent **exclusivement de la main du joueur**
- Les cartes Face Découverte (Couche 2) **ne sont pas jouables** en Phase 1
- Les cartes Face Cachée (Couche 3) **ne sont pas accessibles** en Phase 1

#### Règle de Pose
- Le joueur pose une ou plusieurs cartes de la même hauteur par tour sur la **Pile de Jeu** (par exemple : 3 x10 )
- La carte (ou le groupe de cartes) doit respecter les **règles de validité** (voir section "Validité d'une Pose")
- On applique une seule fois l'effet d'un groupe de cartes.

### Effets Spéciaux en Phase 1
- **Joker de Verre** : Voir section "Effets Spéciaux"
- **Joker Noir (Passe)** : Voir section "Effets Spéciaux"
- **Joker Couleur (Bombe)** : Voir section "Effets Spéciaux"
- **Carte 7** : Déclenche un "Don" si applicable — **Attention particulière si c'est la dernière carte de Phase 1** (voir section "Condition d'Applicabilité du Don")
- **Carte 2** : Voir section "Effets Spéciaux"— **Interdit de Terminer la Phase 1 sur un (ou plusieurs) 2**
- **Doublon** : Actif (compare deux tours consécutifs) tant qu'il y a **plus de 2 joueurs**
- **Carré** : Voir section "Effets Spéciaux"
- **Prêtre** : Voir section "Effets Spéciaux"
- **Valet** : Voir section "Effets Spéciaux"

### Pioche
- Après chaque pose valide ou Don de Carte, le joueur **pioche 1 carte** du dessus de la **Pioche commune**
- La carte piochée **rejoint la main du joueur**
- **Si la Pioche est vide** et ne reviendra plus.

### Incapacité à Jouer
- Si un joueur n'a **aucune carte valide** à jouer sur la Pile, il **passe son tour** sans poser ni piocher
- Il **ramasse immédiatement la Pile de Jeu**
- Le jeu continue avec le joueur suivant

### Ramassage Volontaire
- Un joueur, à son tour de jeu, peut décider **stratégiquement** de **ramasser volontairement la Pile de Jeu**
- Il **passe son tour**, sans poser ni piocher
- Le jeu continue avec le joueur suivant

### Rejeu (Bonus Tour)
- Certains effets accordent un **rejeu** (tour supplémentaire immédiat)
- Après un rejeu, le joueur joue à nouveau selon les mêmes règles
- Les rejeux s'enchaînent jusqu'à épuisement des effets générateurs

### Passage de Tour
- Après que le joueur ait **posé sa carte, exécuté tous les effets et pioché**, son tour se termine
- Le **joueur suivant** (dans le sens des aiguilles d'une montre, ou inversé selon un effet) commence son tour
- **Cas particulier** : Si un joueur **ne peut pas jouer** ou **décide de passer volontairement** (voir "Ramassage Volontaire"), il **ramasse la Pile de Jeu** et le jeu continue avec le joueur suivant

### Fin de Phase 1 : Phase Travail - Chaque Phase est unique a chaque joueur
- Phase 1 se termine quand **deux conditions sont simultanément vraies** :
  1. La **main du joueur actif est vide** (même en fin de tour par exemple il lui reste 2 cartes en main : il pose un 7 -> Don de sa dernière carte)
  2. La **Pioche n'existe plus** (Pioche vide)
- **Transition vers Phase 2** (Le Talent) — voir section "Transitions Entre Phases"

---

## Phase 2 : Le Talent (The Talent)

### Déclenchement
- La Phase 2 se déclenche quand **la main du joueur ET la Pioche sont vides**

### Transition
- Les **cartes Face Découverte (Couche 2) deviennent la main du joueur**, récupérées **en un seul bloc** (les 3 cartes d'un coup)
- Le joueur continue de jouer à partir de cette main

### Conditions de Jeu
- Mêmes règles qu'en Phase 1 : **Règle de Pose**, **Pas de Pioche** (déjà épuisée à ce stade), **Incapacité à Jouer**, **Ramassage Volontaire**, **Rejeu**, **Passage de Tour**

### Effets Spéciaux en Phase 2
- **Mêmes effets spéciaux qu'en Phase 1** :
  - Joker de Verre
  - Joker Noir (Passe)
  - Joker Couleur (Bombe)
  - Carte 7 (Don) — **Attention particulière si c'est la dernière carte de Phase 2** (voir section "Condition d'Applicabilité du Don")
  - Carte 2 — **Interdit de Terminer la Phase 2 sur un (ou plusieurs) 2**
  - Doublon (actif tant qu'il y a plus de 2 joueurs)
  - Carré
  - Prêtre
  - Valet

### Fin de Phase 2 : Le Talent
- Phase 2 se termine quand **la main du joueur est vide** (la Pioche étant déjà épuisée depuis la fin de Phase 1)
- **Transition vers Phase 3** (La Chance) — voir section "Transitions Entre Phases"

---

## Phase 3 : La Chance (The Luck)

### Déclenchement
- La Phase 3 se déclenche quand **la main du joueur ET les cartes Face Découverte (Couche 2) sont vides** (Phase 2 entièrement terminée).

### Mécanique de révélation
- Le joueur possède **3 cartes Face Cachée** (Couche 3).
- À **son tour uniquement**, il retourne **une seule carte face cachée** (jamais en avance, jamais plusieurs à la fois).
- La carte retournée est **immédiatement révélée à tous les joueurs**.

### Résolution de la carte révélée 
- Elle est **posée sur la Pile** (visible de tous, pour permettre l'ajustement stratégique des autres joueurs).
- **Si la carte respecte la Règle de Pose** (hauteur ≥ carte du dessus, ou règle spéciale applicable) :
  - Les **effets spéciaux de la carte s'appliquent normalement** (voir ci-dessous pour les exceptions).
  - Le jeu continue : passage au joueur suivant, ou **Rejeu** si l'effet de la carte le permet.
- **Si la carte ne respecte pas la Règle de Pose** :
  - Le joueur **ramasse toute la Pile** et l'ajoute à sa main (comme en Phase 1/2).
  - Il devra **vider entièrement cette main** (selon les règles habituelles de pose) **avant de pouvoir retourner une nouvelle carte Face Cachée**.

### Effets Spéciaux en Phase 3
- **Tous les effets spéciaux s'appliquent normalement** aux cartes révélées (Joker de Verre, Joker Noir/Passe, Joker Couleur/Bombe, Carte 2, Doublon, Carré, Prêtre, Valet), **sauf exception explicite ci-dessous**.
- **Exception — Carte 7 (Don) révélée Face Cachée** :
  - **Aucun effet de Don n'est déclenché** si le 7 est révélé directement depuis la Couche 3 (Face Cachée).
  - *Raison* : le Don nécessite de donner une carte de sa main, or à ce stade le joueur n'a pas de main constituée pour ce 7 (effet silencieux).
  - **Rappel important ** : si un 7 est joué **depuis la main**, il **conserve son effet de Don**, **sauf si c'est la dernière carte de la main du joueur** (aucune carte disponible à donner).
- Carte 2 — **Interdit de Terminer la Phase 3 sur un (ou plusieurs) 2 que ce soit issue de la main ou de la carte révélée**

### Fin de Phase 3 (pour un joueur)
- Un joueur **termine la partie** (n'est plus en jeu) dès qu'il n'a **plus aucune carte** : ni en main, ni Face Découverte (déjà toutes jouées normalement), ni Face Cachée.
- Le jeu continue avec les joueurs restants.

### Fin de Partie
- Le jeu se termine quand il ne reste **qu'un seul joueur avec des cartes**.
- Ce dernier joueur est désigné **"Psycko"** (perdant).
- Détection hors périmètre de Step6_AdvanceTurnResolver : Step6 avance
  seulement le tour parmi les joueurs actifs restants. C'est GameOrchestrator,
  après appel à TurnManager, qui constate qu'un seul joueur a
  CurrentPhase != DefPhase.Finished et déclenche la fin de partie.

---

## Core Card Effects & Interactions

### Glass Joker (Joker de Verre)
- **Transparent** pour :
  - **Hauteur** : on regarde la carte EN-DESSOUS du Joker de Verre pour déterminer la contrainte applicable.
  - **Applique la règle active** : ≥ (mode normal) ou ≤ (si un Prêtre a été posé avant dans la chaîne).
    - *Prêtre* : si un Prêtre précède, la contrainte ≤ traverse le Joker de Verre — la référence de hauteur reste le Prêtre pour le joueur suivant.
  - **Très restrictif possible** : ex. Joker de Verre posé sur un As → la contrainte devient ≥ As (Très Difficile : jouables = As / 2 / autre Joker).
  - **Doublon** : compare la hauteur sur deux tours consécutifs via la carte EN-DESSOUS du Joker de Verre — celui-ci est transparent, pas de rupture de chaîne.
  - **Carré** : s'insère au milieu ou à la fin de la séquence sans interrompre la chaîne (ex. `5♣, 5♦, Joker de Verre, 5♥, 5♠` = Carré valide).
- **Ne casse jamais les chaînes** (contrairement au Joker Noir).
- **Aucun effet propre** : jamais de rejeu, jamais de destruction de pile — il se contente de "transmettre" la référence de la carte/contrainte en dessous.
- **Accepte n'importe quelle hauteur précédente** : peut être posé sur n'importe quelle carte, sans contrainte de ≥/≤.

### Black Joker (Joker Noir / Passe)
- **Casse toutes les chaînes** : hauteur, Doublon, Carré — aucune référence ne traverse le Joker Noir.
- Exemple : `[5, Joker Noir, 5]` ≠ Doublon (chaîne cassée).
- **Agit comme un point de reset** : la carte suivante ouvre une nouvelle contrainte de hauteur libre (comme un nouveau début de pile).
- Exemple : `[As, Joker Noir, 3]` = reset de la hauteur (le 3 est jouable librement après le Joker Noir, l'As ne compte plus comme référence).
- **Aucun effet de destruction de pile** — la pile reste en place, seule la continuité des chaînes est interrompue.
- **Accepte n'importe quelle hauteur précédente** : peut être posé sur n'importe quelle carte, sans contrainte de ≥/≤.

### Joker Couleur (Color / Bombe)
- **Détruit la pile** : les cartes disparaissent définitivement du jeu (ne sont pas ramassées par le joueur suivant).
- **Jamais de rejeu** pour celui qui la pose, même s'il lui reste des cartes jouables.
- **Joueur suivant ouvre une nouvelle pile** : hauteur libre (comme après un Joker Noir).
- **Accepte n'importe quelle hauteur précédente** : peut être posé sur n'importe quelle carte, sans contrainte de ≥/≤.
---

## Carré & Doublon 

### Principe général
- Un **coup** = une action de jeu posant 1 à 4 cartes de **même hauteur** simultanément.
- Carré et Doublon se détectent en analysant la **pile entière** (cumul de coups consécutifs de même hauteur), pas uniquement le dernier coup isolé.
- **Ordre de priorité de vérification à chaque coup : Carré d'abord, Doublon ensuite.**
  - Si le cumul de cartes de même hauteur atteint 4 (ou plus, cas futur PowerCards) → **Carré**, le Doublon ne s'applique pas sur ce coup.
  - Sinon, si le cumul est ≥ 2 (et < 4) → **Doublon**.
- **Joker de Verre transparent** : ignoré dans le comptage cumulatif — la comparaison de hauteur se fait toujours avec la dernière carte/coup **avant** le Joker de Verre.
- **Joker Noir casse tout** : remet le cumul à zéro, aucune chaîne (Carré ou Doublon) ne traverse un Joker Noir.

### Carré (Quad)
- Se construit par **cumul de cartes de même hauteur sur plusieurs coups consécutifs** (jusqu'à 5 coups max dans le deck actuel : 1+1+1+1, 2+1+1, 2+2, 3+1, 2+1+1(Joker Verre transparent), etc.).
- **Validé dès que le cumul atteint 4**, peu importe la répartition des coups — y compris quand un coup de 2 ou 3 cartes fait franchir directement le seuil de 4 (ex. cumul=2 puis un coup de 2 cartes → cumul=4 → Carré direct, pas de Doublon sur ce coup).
- **Un coup de 4 cartes d'un seul coup valide immédiatement un Carré.**
- **Futur (PowerCards)** : validé dès que le seuil de 4 est atteint/franchi, peu importe le total final.
- **Effet** : pile détruite, le joueur qui complète le Carré rejoue, ouvre une nouvelle pile (hauteur libre).
- **Cas limite — Si le joueur complète le Carré avec sa dernière carte** : il ne rejoue pas, contrairement au cas normal — il gagne sa partie (transition vers Finished) et c'est le joueur suivant qui joue sur la nouvelle pile. Pas de rejeu pour un joueur qui n'a plus de cartes.
- **Reste actif même à ≤2 joueurs.**

### Doublon (Pair / Skip)
- **Chaque coup joué sur la même hauteur que le dernier coup de la pile** (ou l'avant-dernier si un Joker de Verre s'intercale), **et qui ne complète pas un Carré**, redéclenche un Doublon.
- Se répète à **chaque** coup consécutif de même hauteur (cumul 2 ou 3), sautant à chaque fois le joueur suivant — jusqu'à ce que : le cumul atteigne 4 (→ Carré, priorité), qu'un Joker Noir casse la chaîne, ou qu'une autre hauteur soit jouée.
- **Joker de Verre transparent** : ne casse pas la chaîne, comparaison sautant par-dessus lui.
- **Joker Noir casse la chaîne** : remet tout à zéro immédiatement.
- **Désactivé à ≤2 joueurs** — **le Carré, lui, reste actif à ≤2 joueurs.**
### Source d'autorité — décompte des joueurs actifs
- Le seuil « ≤2 joueurs » (Doublon désactivé, Carré/Don toujours actifs) se
calcule exclusivement sur le nombre de joueurs dont CurrentPhase != DefPhase.Finished.
- Ne jamais utiliser Players.Count brut : ce compte inclut les joueurs déjà
Finished (ayant gagné leur partie), ce qui fausserait le seuil une fois des
joueurs éliminés en cours de partie.
- **Granularité du Doublon** : Le Doublon compare le Play courant au Play précédent (niveau coup, pas niveau carte). Un Play multi-cartes (ex. 2×5) ne peut jamais être un Doublon avec lui-même. Il n'y a Doublon que si sa hauteur effective égale celle du Play précédent. Sur pile vide, aucun Doublon n'est possible (pas de Play précédent).

### **Exemple**
J1 : Dame              → rien (première carte de la chaîne)
J2 : Dame (cumul=2)    → Doublon ! J3 sauté
J4 : Dame (cumul=3)    → Doublon ! J1 sauté
J2 : Dame (cumul=4)    → Carré ! (vérifié avant Doublon) Pile détruite, J2 rejoue, nouvelle pile ouverte

### **Exemple complémentaire (coup multi-cartes qui saute directement au Carré)**
J1 : Dame (cumul=1)         → rien
J2 : Dame (cumul=2)         → Doublon ! J3 sauté
J4 : 2x Dame (cumul=4)      → Carré ! (priorité sur Doublon) Pile détruite, J4 rejoue

- **Traversée Doublon/Carré par type de Joker** :
  - Joker de Verre : transparent — traverse sans rompre la chaîne Doublon/Carré, la comparaison continue avec la carte/le Play en-dessous.
  - Joker Noir : réinitialise hauteur et contrainte — rompt la chaîne. Une carte posée juste après ne peut pas former de Doublon avec ce qui précède le Joker Noir.
  - Joker Couleur/Bombe : détruit la Pile — rupture totale, équivalent à repartir de pile vide.

---

## Jack (Valet)

### Effet de base
- Le Valet **change le sens de jeu**.
- Après la pose d'un Valet, l'ordre des joueurs s'inverse pour tous les tours suivants.
- Un deuxième Valet posé ultérieurement **réinverse** le sens (retour au sens initial).

### Position hiérarchique
- **Prêtre < Valet < Cavalier**

### Interaction avec Doublon (Valet sur Valet)
- Si un joueur pose un Valet alors que le dernier Valet posé était sur la pile précédente (deux Valets consécutifs dans la chaîne), c'est un **Doublon** → **le joueur suivant est sauté**.
- **Le changement de sens a lieu AVANT le Doublon.**
- Le sens reste inversé après le skip.

### Interaction avec Carré (4 Valets consécutifs)
- **4 Valets enchaînés consécutivement** (via la chaîne de Doublons, en traversant les skips) déclenchent un **Carré**.
- **Pile détruite** → le joueur qui complète le Carré rejoue/ouvre une nouvelle pile.
- Le sens de jeu **reste celui en vigueur** après la destruction (pas de réinitialisation automatique).

### Transparence du Joker de Verre avec Valet
- Un Joker de Verre posé après un Valet ne rompt pas le changement de sens en cours.
- Le Joker de Verre est transparent pour la chaîne des Valets.

### Durée de vie du changement de sens
- Le changement de sens persiste **jusqu'à la pose d'un nouveau Valet** (qui le réinverse).

---

## 7 Card (Don / Gift) — Clarifications Verrouillées

### Effet de base
- Quand un joueur pose un **7**, il doit faire un **Don** : donner une carte de son choix à un adversaire de son choix.
- Le 7 reste en jeu sur la pile après avoir été posé.
- Le joueur suivant doit ensuite jouer **≥ 7** (règle normale de hauteur).
- On ne peut pas faire de don à un joueur qui a fini sa partie.

### Condition d'applicabilité du Don

#### Phase 1 — Si un joueur n'a que 3 cartes en main
- Le joueur pose un **7** → doit **piocher avant de faire son don** → fait le Don → repiocher à la fin de son tour.

#### Phase 1 → Phase 2 (transition critique)
- Si le joueur pose un **7 comme dernière carte de Phase 1**, il ramasse d'abord ses cartes **Phase 2** (face découverte), qui deviennent sa nouvelle main.
- **Après** avoir ramassé ses cartes Phase 2, il a alors une main remplie → le **Don est applicable**.
- Ordre d'exécution exact : pose 7 → ramasse Phase 2 → fait le Don → attend son prochain tour en Phase 2.

#### Phase 2 → Phase 3 (transition)
- Si le joueur pose un **7 comme dernière carte de Phase 2**, il **passe à Phase 3**.
- À ce moment il n'a plus de main (cartes Phase 2 épuisées, cartes Phase 3 face cachée pas encore en main).
- **Le Don n'est pas applicable** (pas de cartes à donner).
- Le 7 reste simplement sur la pile sans effet.

### Exception — Carte 7 en Phase 3 (Face Cachée vs. Main)

En Phase 3, un joueur dispose de deux sources de cartes :
  • Sa main (Couche 1) — cartes en main, donables
  • Ses cartes Face Cachée (Couche 3) — révélées directement sur la pile, NON donables

Lorsqu'un joueur pose un 7 :

  **Cas A — 7 joué depuis la Main (CardLayer.Hand) en Phase 3** :
    • Le Don s'applique NORMALEMENT.
    • Le joueur donne une carte de sa main reconstituée (après éventuelle pioche).

  **Cas B — 7 révélé depuis Face Cachée (CardLayer.FaceDown) en Phase 3** :
    • AUCUN DON n'est déclenché (effet silencieux).
    • Raison : Une carte Face Cachée n'a jamais transité par la main du joueur 
      — elle est révélée directement sur la pile. Il n'existe donc aucune main 
      « constituée pour ce 7 » à partir de laquelle on pourrait donner.

Cette distinction s'exprime via :
  • play.SourceLayer == CardLayer.Hand   → Don possible (si main non vide)
  • play.SourceLayer == CardLayer.FaceDown → Don impossible (effet silencieux)

**Corollaire critique** : Un joueur en Phase 3 ne peut JAMAIS donner une carte 
Face Cachée, même s'il en a en Couche 3. Il ne peut donner que des cartes de sa 
main reconstituée.

#### Cas général (pas de transition de phase)
- Le Don n'est possible **que si le joueur a encore des cartes en main** au moment de poser le 7.
- Si le joueur n'a plus de cartes en main après avoir posé le 7 et ne peut pas en récupérer (pioche ou cartes Phase 2), le Don **n'est pas applicable** — le 7 reste sur la pile sans effet.

### Applicabilité à 2 joueurs
- Le Don du 7 reste **applicable même à 2 joueurs restants** (contrairement au Doublon qui se désactive à 2 joueurs).

### Pas de rejeu
- Le 7 n'entraîne pas de rejeu pour celui qui l'a posé (contrairement au Carré ou au "2").

### Destinataire du Don
- Le joueur qui pose le 7 choisit le destinataire parmi tous les adversaires restants (y compris ceux en Phase 3), **sauf ceux qui ont déjà gagné**.
- Le joueur en Phase 3 qui reçoit une carte reste en Phase 3 mais doit se débarrasser de sa main avant de pouvoir retourner une nouvelle carte face cachée.

---

## 2 Card (La Fermeture / The Closure) — Clarifications Verrouillées

### Effet de base
- **Détruit la pile** : toutes les cartes sur la pile disparaissent (comme Bombe).
- **Rejeu obligatoire** : celui qui pose le 2 ouvre une nouvelle pile.

### Interdit de terminer une phase sur un 2
- **S'applique dans les 3 phases** : Le Travail (Phase 1), Le Talent (Phase 2), La Chance (Phase 3).
- **Cas limite — Si c'est la dernière carte du joueur** : le joueur pose son 2 ,**ramasse la pile** et **passe son tour** (y compris le 2 qui vient d'être posé). Pas de destruction, pas de rejeu.

#### Phase 3 spécifiquement
- Le joueur ne peut pas terminer sur le 2, **que ce soit** :
  - Sa dernière carte retournée (main vide, cartes cachées épuisées), ou
  - Sa derndernière carte en main (cartes cachées vides).

  ---

## Priest (Prêtre) — Clarifications Verrouillées

### Effet de base
- Le Prêtre **inverse temporairement la règle de hauteur** pour le joueur suivant uniquement, un seul tour.
- Mode normal : jouer ≥ sommet de pile.
- Prêtre posé : le joueur suivant doit jouer ≤ Prêtre.
- Retour automatique au Mode normal après ce tour.

### Position hiérarchique
- **10 < Prêtre < Valet**
- Sous contrainte ≤ Prêtre, un 10 est jouable, un Valet ne l'est pas.

### Enchaînement de Prêtres (Doublon)
- Si le joueur sous contrainte rejoue un Prêtre, c'est un **Doublon** (deux Prêtres consécutifs dans la chaîne) → **le joueur suivant est sauté**.
- La contrainte ≤ Prêtre **reste active** pour celui qui joue après le skip.
- La chaîne de Doublon **persiste à travers les skips** : chaque nouveau Prêtre posé sur un Prêtre précédent redéclenche un Doublon et un nouveau skip, indépendamment de qui a joué entre-temps.

**Exemple 1** : J1 [Prêtre] → J2 [Prêtre] [Doublon, J3 sauté] → J4 [5, retour Mode normal]

**Exemple 2** : J1 [Prêtre] → J2 [Prêtre] [Doublon, J3 sauté] → J4 [Joker de Verre] → J1 [Prêtre] [Doublon, J2 sauté] → J3 [9, retour Mode normal]

### Carré de Prêtres
- **4 Prêtres enchaînés consécutivement** (via la chaîne de Doublons, en traversant les skips) déclenchent un **Carré**.
- **Pile détruite** → le joueur qui complète le Carré rejoue/ouvre une nouvelle pile.
- **La contrainte Prêtre disparaît** avec la pile détruite (retour au Mode normal, pas de contrainte héritée pour la nouvelle pile).

**Exemple validé** : P1 [Prêtre] → P2 [Prêtre] [Doublon, P3 sauté] → P4 [Prêtre] [Doublon, P1 sauté] → P2 [Prêtre] [4ᵉ consécutif → Carré détecté, pile détruite, P2 rejoue]

### Transparence du Joker de Verre sous contrainte Prêtre
- Un Joker de Verre posé pendant la contrainte ≤ Prêtre est **transparent** — il ne rompt pas la contrainte.
- La référence de hauteur **reste le Prêtre** pour le joueur suivant.
- Si un Joker de Verre est joué sur une **Pile Vide** alors le joueur suivant n'a *pas de contrainte*. Il joue ce qu'il veut ou presque (voir autres execptions).

**Exemple validé** : P1 [Prêtre] → P2 [Joker de Verre] → P3 doit jouer ≤ Prêtre.

### Cartes non-Prêtre sous contrainte
- Toute carte ≤ Prêtre reste jouable normalement pendant la contrainte, sous réserve des autres règles (Doublon/Carré/2/Bombe/Joker Noir/etc.).

### Durée de vie de la contrainte
- ≤ Prêtre s'applique au tour immédiatement suivant la pose du Prêtre (ou sa relance via un nouveau Prêtre ou Joker de Verre).
- **Réinitialisée** dès que :
  - Le tour sous contrainte passe sans nouveau Prêtre, ou
  - Un Carré / Bombe / Joker Noir détruit la pile.

---
## PowerCards — Système Méta (Hors-Scope V1, Réservé Extension Future)

### Statut
- **Hors scope des Phases 0→5 actuelles.** Aucune implémentation avant validation complète
  du Core de base (63 cartes) + Bots + Présentation V1.
- Réflexion et idées à consigner au fur et à mesure dans **Notion** (liste vivante, non figée).

### Principe général
- Avant le lancement de la recherche de partie, chaque joueur sélectionne **une PowerCard
  unique** parmi celles débloquées sur son compte (progression/conditions de déblocage à définir).
- La PowerCard est **liée au joueur**, pas au deck de la partie — elle n'est **jamais** intégrée
  à sa main (60+3 cartes du paquet reste inchangé).
- **Jouable à tout moment durant son tour**, peu importe la phase (1, 2 ou 3).
- **Non-obligatoire** : un joueur peut terminer la partie (plus de cartes en main/devant lui)
  sans avoir joué sa PowerCard — elle n'entre pas dans la condition de victoire/défaite.

### Pistes d'effets (non figées, à trancher plus tard)
- **Piste A — Duplication d'effet existant** : la PowerCard reproduit un effet déjà présent
  dans le deck de base (Joker de Verre / Noir / Couleur, Prêtre, 2, 7, ou une hauteur forte
  type Cavalier/Dame/Roi/As). Implémentation plus simple (réutilise les Rules existantes).
- **Piste B — Effet inédit** : mécanique propre à la PowerCard, sans équivalent dans le deck
  de base. Exemples en réflexion :
  - Forcer le joueur suivant à ramasser la pile.
  - Forçage de coup (ex. le joueur suivant doit jouer 2 cartes d'un coup ou passe son tour).
- Les deux pistes sont envisagées comme **progression de compte** : effets de plus en plus
  puissants débloqués à mesure que le joueur monte de niveau.

### Impact anticipé sur l'architecture
- Le Carré (`Rules/Detection/QuadDetection.cs`) a déjà une note prévoyant un seuil "≥4 peu
  importe le total final" — anticipant qu'une PowerCard pourrait un jour dupliquer une hauteur
  et pousser le cumul au-delà de 4 cartes simultanées.
- Quand implémentées, les PowerCards devront s'intégrer via la couche **Interfaces**
  (ex. `IPowerCardEffect`) sans casser l'isolation Domain/Rules — à concevoir en session dédiée.

### Prochaine étape
- Ekinox consigne les idées de PowerCards dans Notion (liste ouverte, brainstorming).
- Reprise du sujet en session dédiée une fois le Core V1 (63 cartes) stabilisé et testé.

---

### CardLayer — Provenance d'une Carte jouée

Un Play provient toujours d'UNE SEULE couche du joueur, jamais d'un mélange.
Enum déclaré dans : Assets/Scripts/Core/Domain/DefLayer.cs (namespace Psycko.Core.Domain)

  • CardLayer.Hand     — Couche 1. Main du joueur. Couche jouable en Phase 1 (Work),
                         en Phase 2 (Talent, car les FaceUp y sont déjà ramassées en main),
                         et en Phase 3 (Luck) tant que Hand.Count > 0.
  • CardLayer.FaceUp   — Couche 2. Cartes Face Découverte posées devant le joueur.
                         JAMAIS jouable directement : elles sont ramassées EN MAIN
                         à l'entrée de la Phase 2, puis jouées via CardLayer.Hand.
  • CardLayer.FaceDown — Couche 3. Cartes Face Cachée, révélées directement sur la pile.
                         Jouable en Phase 3 (Luck) uniquement, et uniquement si Hand.Count == 0.

Cette information est portée par Play.SourceLayer (Assets/Scripts/Core/Domain/Play.cs,
`public sealed record Play`), de type CardLayer, valeur par défaut Hand dans les
factories Create(...) et CreateSingle(...).

Utilité : Certains effets spéciaux (notamment le 7 / Don) dépendent de la provenance
réelle de la carte, indépendamment de la phase. Une carte révélée depuis FaceDown
(Phase 3) ne peut pas déclencher un Don, car le joueur n'a pas de main constituée pour ce 7.

#### Qualification effective des couches — qui décide

Aucun Resolver de TurnManager ne décide seul de la couche jouable. La qualification
relève EXCLUSIVEMENT de Rules/Phase/, via PhaseResolver.IsLayerPlayable(Player, CardLayer)
(lecture seule, ne modifie jamais Player) :

  Work  (Assets/Scripts/Core/Rules/Phase/WorkPhaseResolver.cs)
    return layer == CardLayer.Hand && player.Hand.Count > 0;
    → Hand uniquement. FaceUp et FaceDown ne sont JAMAIS jouables en Phase 1.

  Talent (Assets/Scripts/Core/Rules/Phase/TalentPhaseResolver.cs)
    return layer == CardLayer.Hand && player.Hand.Count > 0;
    → Hand uniquement. Les FaceUp ayant été ramassées en main à l'entrée de phase,
      CardLayer.FaceUp n'est jamais une SourceLayer légale.

  Luck  (Assets/Scripts/Core/Rules/Phase/LuckPhaseResolver.cs)
    if (player.Hand.Count > 0) return layer == CardLayer.Hand;
    return layer == CardLayer.FaceDown && player.FaceDown.Count > 0;
    → EXCLUSIVITÉ MUTUELLE STRICTE : jamais deux couches jouables simultanément.
      Hand prime toujours (cas d'un Don via 7, ou d'un ramassage de pile en Phase 3).

  Toute autre phase (DefPhase.Finished, ou phase sans resolver enregistré)
    → aucune couche jouable. Voir CardPlayabilityChecker (Rules/Validation/), qui
      retourne false si la phase du joueur n'a pas de resolver dans son dictionnaire.

CONSÉQUENCE POUR LES RESOLVERS (Step1→Step6) :
Valider une SourceLayer signifie TOUJOURS interroger le PhaseResolver de la phase
courante du joueur — jamais réécrire la condition en dur dans un Step.

#### Invariants de couches (Domain)

Garantis par Player.Create (Assets/Scripts/Core/Domain/Player.cs, classe sealed) :
  • FaceUp.Count == 3 exactement à la création — sinon ArgumentException.
  • FaceDown.Count == 3 exactement à la création — sinon ArgumentException.
  • Hand : aucune contrainte de taille.
  • CurrentPhase forcée à DefPhase.Work à la création.

Player est immuable : toute évolution passe par WithHand / WithFaceUp / WithFaceDown /
WithPhase, dont l'appel relève de Services/, jamais de Rules/.
Propriétés dérivées : HasCards (Hand OU FaceUp OU FaceDown non vide), TotalCardCount.

#### Pose effective — séparation décision / exécution

Step1_PlaceCardsResolver VALIDE une pose, il ne l'EXÉCUTE jamais.
Il reçoit un état, vérifie (joueur actif, count ≥ 1, rang identique, count ≤ 4,
SourceLayer qualifiée par le PhaseResolver, hauteur via CardPlayability),
et retourne un TurnResult dont l'État est IDENTIQUE à l'état d'entrée.
Anomalie → exception explicite, jamais un état dégradé.

Le retrait réel des cartes de la couche source et leur ajout à la Pile relèvent
d'un unique appel IGameStateCommand.PlayCards(play), émis par GameOrchestrator
APRÈS validation. C'est GameOrchestrator qui lit play.SourceLayer pour savoir
de quelle couche retirer les cartes — Hand en Phases 1 et 2, Hand ou FaceDown
en Phase 3 selon l'exclusivité mutuelle ci-dessus.

Corollaire : aucun Step ne doit jamais retirer, déplacer ou ajouter une carte.
Si un Step a besoin d'exprimer une pose, il la DÉCRIT dans son TurnResult ;
GameOrchestrator l'APPLIQUE.

---

### Ordre Strict d'Application des Effets

Lorsqu'un joueur commence son tour, la séquence orchestrée par TurnManager respecte 
cet ordre IMMUABLE (chaque étape reçoit l'état du précédent, jamais de mutation locale).

    0. RAMASSAGE WORK/TALENT (branche alternative, pas une étape de la séquence de pose) :
     - Cette règle s'applique exclusivement aux phases **Work (Phase 1)** et
       **Talent (Phase 2)**. Elle ne s'applique pas à la phase **Luck (Phase 3)**.
     - En **début de tour** (`BeginTurn`), avant que le joueur actif ne propose un coup,
       `TurnManager` vérifie `HasAnyPlayableCard`.
     - Si `HasAnyPlayableCard == false`, `TurnManager.BeginTurn(state)` décide un
       ramassage forcé. `GameOrchestrator` exécute ensuite la décision en appelant
       `ResolvePickup(state, playerId)` puis `PickUpPile(playerIndex)`.
     - Si `HasAnyPlayableCard == true`, aucun ramassage forcé n'est décidé à ce stade.
       Le joueur peut toutefois demander explicitement un ramassage volontaire via le
       bouton **"Ramasser"** côté Présentation pendant qu'il choisit ses cartes.
     - Dans les deux cas, `TurnManager.ResolvePickup(state, playerId)` décrit la
       décision de ramassage volontaire ou forcé ; `GameOrchestrator` est le seul à
       exécuter la mutation. La séquence POSE→JOUEUR SUIVANT (étapes 1 à 6 ci-dessous)
       n'est PAS exécutée : le tour se termine directement sur JOUEUR SUIVANT.

  Si le joueur pose une ou plusieurs cartes, l'ordre suivant DOIT être respecté
  (c'est le rôle de TurnManager) :

  **Chaîne Step1-6 (exécutée uniquement si pas de ramassage) :**

  1. POSE (Step1_PlaceCardsResolver) :
     - Valide le coup via Rules/ (IGameStateQuery en lecture seule)
     - Aucune mutation (state retourné = state reçu)
     - Lève exception si coup invalide
     - GameOrchestrator exécute IGameStateCommand.PlayCards(play) 
       avant Step2, produisant un nouvel état avec cartes en Pile
  
  2. RECONSTRUCTION (Step2_ReconstructionResolver) :
     - Reçoit state avec cartes déjà en Pile
     - DrawCards (pioche si main < 3 et pioche non épuisée)
     - AdvancePlayerPhase + ramassage des FaceUp (transition Phase 1→2 uniquement)
     - Ré-pioche si main < 3 et pioche non épuisée après ramassage
  
  3. EFFETS SPÉCIAUX (Step3_CardEffectsResolver) :
     - Résout les effets via Step3_CardEffectsResolver.Resolve(state, play).
     - Retourne toujours des valeurs concrètes non-null pour NextConstraint,
     NextRefRank et NextDirection (switch/default exhaustif).
     - TurnManager chaîne WithState, WithNextConstraint, WithNextDirection,
     WithDestroysPile, WithGrantsReplay, WithRequiresGiftResolution.
     - Si RequiresGiftResolution == true → retour immédiat (pas de ResolveRemainder).
     GameOrchestrator résout alors le Don du 7 via IGameStateCommand, puis
     rappelle TurnManager.ResolveRemainder(result, play) pour reprendre à Step4.
     - Sinon → appel direct de ResolveRemainder(result, play).
  
  4. RE-PIOCHE FINALE (Step4_FinalDrawResolver) :
     - Ré-pioche si main < 3 et pioche non épuisée (après Don le cas échéant)
  
  5. EFFETS DE PILE (Step5_PileEffectsResolver) :
     - Évaluation Doublon/Carré en relisant Pile.Cards
     - Doublon → skip du joueur suivant
     - Carré → destruction de la pile + rejeu du poseur
  
  6. AVANCEMENT DU TOUR (Step6_AdvanceTurnResolver) :
     - Recherche ancrée sur ActivePlayerIndex courant, avance selon Direction
       (PlayDirection.Clockwise / CounterClockwise).
     - Ne compte et ne s'arrête que sur un joueur actif
       (CurrentPhase != DefPhase.Finished) ; les joueurs Finished sont
       traversés sans jamais être ciblés comme "prochain joueur".
     - Replay : le joueur actif rejoue (aucun avancement).
     - SkipNext : saute exactement UN joueur actif supplémentaire dans le
       sens courant (jamais un joueur Finished, qui ne compte pas comme le
       joueur sauté).
     - Ne détecte JAMAIS la fin de partie : cette responsabilité revient
       exclusivement à GameOrchestrator (voir section Fin de Partie).
 
⚠️ CRITÈRE : Interroger un handler d'effet de main (ex. SevenHandler) à l'étape 1 
(avant reconstitution) produirait des Dons silencieux à tort. Un 7 en dernière carte 
doit permettre au joueur de donner la carte qu'il vient de piocher à l'étape 2.

⚠️ CRITÈRE RAMASSAGE : Le ramassage (forcé ou volontaire) est une branche exclusive 
qui court-circuite entièrement les étapes 1 à 6. TurnManager doit trancher 
"le joueur ramasse-t-il ?" AVANT d'entrer dans la séquence POSE, jamais après.
### Règle verrouillée — Ramassage en phases Work/Talent

Cette règle clôt T23. Elle concerne uniquement les phases **Work** et **Talent**. La phase **Luck** possède
une mécanique distincte et relève de T24.

- **Décision du ramassage forcé** :
  `TurnManager.BeginTurn(state)` est appelé au début du tour, avant toute proposition
  de coup. Il retourne un `PickupResolution` en lecture seule et vérifie
  `HasAnyPlayableCard`. Le ramassage forcé est décidé uniquement en phases Work/Talent
  lorsque cette propriété vaut `false`. L'exécution appartient à `GameOrchestrator`.

- **Ramassage volontaire** :
  si le joueur possède au moins une carte jouable, un ramassage volontaire reste
  possible via une action explicite de la Présentation, par exemple le bouton
  **"Ramasser"**. Cette action est résolue par `TurnManager.ResolvePickup(state, playerId)`,
  qui rejette `NotYourTurn`, `PlayerFinished` ou `GameAlreadyOver` avec le
  `PlayRejectionReason` existant, puis exécutée par `GameOrchestrator`.

- **Découpage de `TurnManager`** :
  `BeginTurn(state)` et `ResolvePickup(state, playerId)` retournent un
  `PickupResolution` en lecture seule ; `ApplyPlay(state, play)` applique la chaîne
  de pose sans paramètre `voluntaryPickup` ; `ResolveRemainder` reprend la chaîne
  après la résolution du Don. `Step0_PickupResolver` est supprimé : la chaîne est
  désormais **Step1→Step6**. `PickupResolution.cs` vit dans `Services/TurnManager/`.

- **Orchestration d'un ramassage** :
  `GameOrchestrator.ApplyPlay(state, play, playerIndex, bool voluntaryPickupRequested)`
  dispose d'un overload sans le booléen. Sur un ramassage, il exécute
  `PickUpPile(seatIndex)`, puis `SetConstraint(Normal, Three)`, puis l'avancement
  de Step6, sans modifier la direction. Aucun enchaînement de ramassages forcés
  n'est possible : après un ramassage, le joueur suivant a une pile vide et peut
  toujours jouer.

- **Résultat exposé à la Présentation** :
  `PlayResult` expose `ForcedPickupPlayerIds` en `IReadOnlyList<int>` ; la collection
  n'est jamais `null`. `Accepted` accepte une liste optionnelle.

- **Exclusion explicite de Luck / T24** :
  cette règle ne couvre pas la phase Luck. En Luck, le joueur choisit une carte
  FaceDown à l'aveugle ; la carte est révélée et rendue visible à tous les joueurs,
  puis, si elle n'est pas jouable, la pile est ramassée et la carte révélée est
  fusionnée dans la main. Cette mécanique sera traitée séparément dans T24,
  `ApplyBlindPlay`. Elle ne doit pas être mélangée avec `BeginTurn`, qui s'applique
  uniquement aux phases Work/Talent.

✅  [RÉSOLU – Step3] Fusion des drapeaux Step2/Step3
Le TurnManager.ApplyPlay n'écrase plus silencieusement les intentions de Step3.
Chaque intention (NextConstraint, NextRefRank, NextDirection, DestroysPile,
GrantsReplay, RequiresGiftResolution) est portée par un With... dédié.
Aucune fusion OR n'était nécessaire ici car Step2 et Step3 n'exposent pas
les mêmes drapeaux en conflit.

✅ [RÉSOLU – Step5 implémenté] Fusion Step3/Step5
Step5_PileEffectsResolver est implémenté (Doublon/Carré). Signature :
Resolve(GameState state, Play play, TurnResult incomingResult) — particularité
volontaire par rapport aux autres Steps, car Step5 doit fusionner son propre
Replay (cas Carré) avec le GrantsReplay déjà porté par incomingResult (issu de
Step3, ex. rejeu du Valet). Le TurnResult retourné par Step5 repart de
incomingResult et applique la fusion par OR logique explicite :
.WithReplay(incomingResult.GrantsReplay || step5Replay) — JAMAIS par
écrasement séquentiel via WithState. Même classe de risque que celle déjà
corrigée pour Step2/Step3 : perte silencieuse d'un drapeau de rejeu si les
deux Steps l'activent indépendamment.
---
---

### État d'implémentation et feuille de route

- **T21 — FAIT** : `GameState` implémente `IGameStateCommand` ; les 9 méthodes
  délèguent aux `With*` existants et ne portent aucune règle de jeu.
- **T23 — FAIT** : le câblage `GameOrchestrator`/`TurnManager` du ramassage
  Work/Talent est terminé. `TurnManager` expose `BeginTurn(state)`,
  `ResolvePickup(state, playerId)`, `ApplyPlay(state, play)` et
  `ResolveRemainder`. `GameOrchestrator` est le seul appelant de
  `IGameStateCommand`. `Step0_PickupResolver` a été supprimé ; la chaîne est
  `Step1→Step6`.
- **T24 — FAIT** : la spécification de
  `GameOrchestrator.ApplyBlindPlay(GameState state, int playerIndex, int faceDownIndex)`
  est arrêtée ci-dessous. Le code livré doit encore être compilé et fusionné.

### T24 — Contrat verrouillé de `ApplyBlindPlay` (Luck)

- Gardes : état et index valides, siège courant, phase `DefPhase.Luck`, main vide,
  et `faceDownIndex` dans les limites de `Player.FaceDown`. Le joueur choisit
  l'index sans voir la carte ; la carte est révélée avec `CardLayer.FaceDown`.
- Une carte FaceDown ne peut pas être accompagnée de cartes de la main.
- Si la carte est valide : elle est posée, puis les effets sont appliqués dans
  l'ordre **spécial → Carré → Doublon**, avec rejeu/skip selon les règles ; si le
  joueur n'a plus de cartes, sa phase devient `Finished`, puis le tour avance.
  Une transition `Talent → Luck` peut survenir au milieu d'un tour pendant un
  rejeu (par exemple après un Carré) et ne doit pas être perdue.
- Si la carte est invalide : elle va sur la pile, puis la séquence T23 est exécutée
  (**PickUpPile → SetConstraint(Normal, Three) → Step6**). Le joueur reste en
  `Luck` et doit vider la main par un `ApplyPlay` normal ; ce chemin commence par
  `BeginTurn` et peut donc déclencher le ramassage forcé avant une nouvelle
  révélation aveugle.
- Le 7 révélé avec `CardLayer.FaceDown` ne déclenche aucun Don (exception déjà
  codée dans `SevenHandler`). Le retour reste `PlayResult` jusqu'à la décision T31.

## Tickets à venir (backlog GameOrchestrator)

Les chemins ci-dessous sont ceux du dépôt décrit par cette documentation. Les fichiers
présents dans la passe de vérification sont référencés par leur chemin projet attendu ;
quand l'implémentation n'était pas fournie, le ticket est explicitement marqué à
confirmer. Les règles verrouillées de ce document priment sur toute simplification.

### T25 — Corriger `SevenHandler.IsGiftTriggered`

- **Contexte :** le Don est obligatoire quand une carte 7 issue de la main laisse une
  main non vide ; aucun Don pour `FaceDown`.
- **Problème :** utiliser `state.Players[play.PlayerId]` confond identifiant et index
  de siège si les IDs ne sont pas contigus.
- **Fichiers concernés :** `Assets/Scripts/Core/Rules/SpecialCards/SevenHandler.cs`
  (fourni sous `SevenHandler.cs`), `GameState.cs`/`IGameStateQuery.cs`.
- **Règles CLAUDE.md applicables :** couche d'origine, exception du 7 FaceDown,
  reconstruction avant appel du handler, Don avant Doublon/Carré.
- **Travail attendu :** résoudre le siège via `GetSeatIndex(play.PlayerId)` ou un
  équivalent explicite, puis lire la main du joueur correspondant ; conserver le test
  `SourceLayer == FaceDown`.
- **Hors périmètre :** implémenter le transfert du Don ou modifier son timing.
- **Dépendances :** T23 ; contrat d'identifiants de `GameState`.
- **Critères d'acceptation :** IDs non contigus couverts ; 7 FaceDown=false ; 7 de
  main avec main restante=true ; main vide=false.
- **Questions à trancher :** faut-il exposer `GetSeatIndex` sur `IGameStateQuery` ?
- **Pistes de tests NUnit EditMode futurs :** IDs 10/42 ; 7 posé par chaque siège ;
  7 FaceDown ; 7 dernière carte.
- **Squelette de prompt :** « Corrige T25 sans changer le contrat du Don ; utilise le
  siège résolu depuis PlayerId et ajoute les tests d'IDs non contigus. »

> **Vérification :** le fichier fourni contient déjà `state.Players[play.PlayerId]` et
> `GameState.GetSeatIndex(int)`. La correction demandée n'est donc pas encore présente ;
> le claim est confirmé et le ticket reste à faire.

### T26a — Pioche / Work

- **Contexte :** la phase Work doit reconstruire la main selon la pioche commune, sans
  remélange, avant les étapes dépendantes.
- **Problème :** les intentions de pioche (`DrawCards`) ne sont pas exécutées par
  `GameOrchestrator` dans le code fourni.
- **Fichiers concernés :** `Assets/Scripts/Core/Services/GameOrchestrator.cs`,
  `Assets/Scripts/Core/Domain/GameState.cs`, `IGameStateCommand.cs`,
  `Step2_ReconstructionResolver.cs` (non fourni).
- **Règles CLAUDE.md applicables :** reconstruction avant Don et avant pile ; pioche
  épuisée définitivement ; orchestrateur seul appelant des commandes.
- **Travail attendu :** appliquer les intentions de Step dans l'ordre, respecter la
  limite de `DrawPile`, et conserver l'immuabilité.
- **Hors périmètre :** règles Luck/T24 et transfert du Don.
- **Dépendances :** T23 ; contrat de `TurnResult`.
- **Critères d'acceptation :** cartes tirées une seule fois, ordre de pioche conservé,
  pioche vide gérée sans exception métier, aucun effet observé avant reconstruction.
- **Questions à trancher :** tirage partiel autorisé quand la pioche contient moins de
  trois cartes ?
- **Pistes de tests NUnit EditMode futurs :** pioche 0/1/3 cartes ; main sous le seuil ;
  conservation de Pile et des autres joueurs.
- **Squelette de prompt :** « Implémente T26a dans l'orchestrateur en consommant les
  intentions de reconstruction, sans déplacer les règles hors des resolvers. »

### T26b — Transitions de phase, y compris pendant un rejeu

- **Contexte :** chaque joueur progresse Work → Talent → Luck → Finished ; un rejeu
  peut traverser une transition.
- **Problème :** le chemin réel des transitions pendant replay n'est pas vérifiable :
  `TurnManager` et les resolvers Step ne sont pas présents dans les uploads.
- **Fichiers concernés :** `GameOrchestrator.cs`, `GameState.cs`,
  `IGameStateCommand.cs`, `Rules/Phase/*PhaseResolver.cs` (non fournis),
  `Step6_AdvanceTurnResolver.cs` (non fourni).
- **Règles CLAUDE.md applicables :** transition individuelle ; main vide comme seuil ;
  phase suivante avant résolution de la suite ; `Finished` si toutes les couches sont vides.
- **Travail attendu :** rendre les transitions explicites et idempotentes dans la
  chaîne normale et dans replay, notamment Talent→Luck après Carré.
- **Hors périmètre :** redéfinir les conditions de victoire ou la direction.
- **Dépendances :** T26a, T27, T24.
- **Critères d'acceptation :** aucun joueur est rejoué dans une phase obsolète ;
  `FaceUp`/`FaceDown` sont disponibles au bon moment ; aucun saut de transition.
- **Questions à trancher :** une transition est-elle évaluée avant ou après chaque effet
  de pile lorsqu'un replay est produit ?
- **Pistes de tests NUnit EditMode futurs :** Carré en fin de Work/Talent ; replay avec
  passage Talent→Luck ; dernier FaceDown vers Finished.
- **Squelette de prompt :** « Trace et corrige T26b dans la chaîne Step, y compris un
  replay qui franchit Talent→Luck ; ajoute des tests de phase individuelle. »

### T26c — Don du 7

- **Contexte :** `SevenHandler` déclare le Don ; l'orchestrateur doit appliquer sa
  transition après reconstruction et avant Doublon/Carré.
- **Problème :** le code fourni n'exécute pas encore les intentions `Don` ; aucune
  commande de transfert n'existe dans `IGameStateCommand`.
- **Fichiers concernés :** `SevenHandler.cs`, `GameOrchestrator.cs`,
  `IGameStateCommand.cs`, `GameState.cs`, interfaces de sélection (non fournies).
- **Règles CLAUDE.md applicables :** Don obligatoire si possible, une seule carte par
  coup, choix de la carte et du destinataire, aucun Don FaceDown, avant pile.
- **Travail attendu :** exposer une résolution de choix puis appliquer le transfert
  immuable ; ne pas laisser l'orchestrateur deviner un choix utilisateur.
- **Hors périmètre :** T25 (résolution du siège), UI, bots.
- **Dépendances :** T25, T26a/b, T31 pour le type final éventuellement.
- **Critères d'acceptation :** exactement une carte ; Don avant Doublon/Carré ; refus
  impossible quand le Don est dû ; FaceDown silencieux.
- **Questions à trancher :** API de sélection synchrone ou résolution en deux temps ?
- **Pistes de tests NUnit EditMode futurs :** 7 avec main restante ; dernier 7 ; carré
  de 7 ; 7 FaceDown ; deux joueurs.
- **Squelette de prompt :** « Implémente T26c après reconstruction, avec un choix de
  carte/destinataire explicite et sans Don automatique inventé. »

### T27 — Step6, `ActivePlayerIndex` et source unique de vérité

- **Contexte :** Step6 doit avancer selon `Direction`, ignorer les joueurs Finished,
  appliquer replay et skip.
- **Problème :** la mutation de `ActivePlayerIndex` hors de `IGameStateCommand` est
  signalée par la dette existante ; l'implémentation Step6 n'est pas fournie.
- **Fichiers concernés :** `Step6_AdvanceTurnResolver.cs` (non fourni), `GameState.cs`,
  `IGameStateCommand.cs`, `GameOrchestrator.cs`.
- **Règles CLAUDE.md applicables :** siège fixe ; direction ; skip d'un actif ; replay
  sans avancement ; Finished jamais ciblé ; orchestrateur seul mutateur.
- **Travail attendu :** choisir une source unique de vérité et faire passer toute
  mutation par elle, sans double avancement entre Step6 et orchestrateur.
- **Hors périmètre :** effets de cartes et calcul de fin de partie.
- **Dépendances :** T26b.
- **Critères d'acceptation :** sens horaire/anti-horaire, un skip exact, replay, tours
  à 2 joueurs, Finished traversés sans être ciblés.
- **Questions à trancher :** Step6 retourne-t-il l'état avec `SetActivePlayer`, ou
  produit-il une intention consommée par l'orchestrateur ?
- **Pistes de tests NUnit EditMode futurs :** 2/3/4 joueurs, chaque direction, suites
  de Finished, skip+replay.
- **Squelette de prompt :** « Fais de T27 une source unique de vérité pour Step6 et
  ActivePlayerIndex ; prouve l'absence de double avance par des tests. »

### T28 — Supprimer le doublon `IsGameOver`

- **Contexte :** la fin de partie dépend du nombre de joueurs non Finished ; Step6 ne
  doit pas la détecter.
- **Problème :** la responsabilité est annoncée comme dupliquée, mais le calculateur
  fourni est vide.
- **Fichiers concernés :** `GameOrchestrator.cs`, `GameResultCalculator.cs`,
  `TurnManager.cs` (non fourni).
- **Règles CLAUDE.md applicables :** un seul Psycko restant ; Step6 hors périmètre ;
  état immuable et résultat explicite.
- **Travail attendu :** implémenter/valider `GameResultCalculator`, déplacer le calcul
  unique, puis supprimer l'autre implémentation et ses appels redondants.
- **Hors périmètre :** règles de phase et élimination.
- **Dépendances :** décision d'API de T27.
- **Critères d'acceptation :** 0/1/2 joueurs actifs calculés correctement ; aucune
  détection dans Step6 ; résultat cohérent après replay et pickup.
- **Questions à trancher :** `IsGameOver` booléen ou résultat de partie nommé ?
- **Pistes de tests NUnit EditMode futurs :** deux joueurs, plusieurs Finished, état
  initial, transition du pénultième.
- **Squelette de prompt :** « Implémente T28 autour de GameResultCalculator (actuellement
  vide), puis retire toute copie de IsGameOver. »

> **Vérification :** `GameOrchestrator.cs` contient `IsGameOver`;
> `GameResultCalculator.cs` est vide. Le doublon avec `TurnManager` n'est pas vérifiable
> dans les fichiers fournis.

### T29 — Remplacer les `!.Value` par des gardes explicites

- **Contexte :** un rejet métier doit retourner `PlayResult.Rejected`, pas lever une
  exception de nullabilité.
- **Problème :** des `!.Value` subsistent dans les chemins de validation/résolution.
- **Fichiers concernés :** `GameOrchestrator.cs` (`pickup.RejectionReason!.Value`),
  `Play.cs` (`Rank!.Value`), et resolvers Step non fournis.
- **Règles CLAUDE.md applicables :** exceptions pour erreurs de programmation ; rejets
  typés pour règles métier ; ne jamais muter l'état sur rejet.
- **Travail attendu :** vérifier explicitement chaque nullable ; retourner le motif de
  rejet approprié, ou traiter l'invariant impossible comme erreur de programmation
  documentée.
- **Hors périmètre :** changer la forme de `PlayResult` (T31).
- **Dépendances :** inventaire complet des Steps.
- **Critères d'acceptation :** zéro `!.Value` dans les flux métier ciblés ; état inchangé
  et raison renseignée sur rejet.
- **Questions à trancher :** quel `PlayRejectionReason` pour une incohérence interne ?
- **Pistes de tests NUnit EditMode futurs :** pickup rejeté sans motif ; rang absent ;
  play mal formé ; état strictement identique.
- **Squelette de prompt :** « Sécurise T29 sans masquer les bugs : garde explicite,
  PlayResult rejeté pour le métier, exception seulement pour invariant impossible. »

### T30 — Rendre public `RequestPickup`

- **Contexte :** le ramassage volontaire est une action distincte de la pose.
- **Problème :** l'API actuelle passe par `ApplyPlay(..., voluntaryPickupRequested)` ;
  le debt item demande une API dédiée réutilisant T23.
- **Fichiers concernés :** `GameOrchestrator.cs`, `PlayResult.cs`,
  `PickupResolution.cs` et `TurnManager.cs` (non fournis).
- **Règles CLAUDE.md applicables :** `PickUpPile → SetConstraint(Normal, Three) → Step6`,
  direction inchangée, état immuable, BeginTurn seulement Work/Talent.
- **Travail attendu :** exposer `public RequestPickup(GameState, int playerIndex)` et
  mutualiser exactement la séquence T23 ; conserver un overload de compatibilité si besoin.
- **Hors périmètre :** pickup invalide de Luck/T24.
- **Dépendances :** T27, T29.
- **Critères d'acceptation :** volontaire/forcé cohérents ; même résultat d'état ; aucun
  appel à ApplyPlay nécessaire pour demander le pickup.
- **Questions à trancher :** paramètre d'ID joueur ou index de siège ?
- **Pistes de tests NUnit EditMode futurs :** pickup autorisé/interdit, contrainte reset,
  direction, Step6, Finished et mauvais siège.
- **Squelette de prompt :** « Ajoute T30 comme API publique mince qui réutilise la
  séquence T23, sans dupliquer la logique de ramassage. »

### T31 — Décider le type de retour avant les Bots

- **Contexte :** T24 doit révéler une carte tout en conservant le résultat d'orchestration.
- **Problème :** `PlayResult` ne porte pas actuellement `RevealedCard`; le besoin
  d'un résultat `BlindPlayResolution` n'est pas tranché.
- **Fichiers concernés :** `PlayResult.cs`, `GameOrchestrator.cs`, `ApplyBlindPlay`,
  contrats Bots/Presentation (non fournis).
- **Règles CLAUDE.md applicables :** visibilité publique de la révélation ; couche
  FaceDown conservée ; rejet typé ; aucune information cachée au mauvais moment.
- **Travail attendu :** comparer `PlayResult + RevealedCard` à un type dédié, documenter
  le choix, puis adapter Bots/Présentation avant de figer l'API.
- **Hors périmètre :** logique des Bots.
- **Dépendances :** T24, T30.
- **Critères d'acceptation :** carte révélée disponible en succès et non divulguée en
  rejet ; compatibilité claire avec ApplyPlay.
- **Questions à trancher :** révélation dans `PlayResult`, sous-type, ou événement ?
- **Pistes de tests NUnit EditMode futurs :** succès valide/invalide, rejet de garde,
  carte et couche exposées, compatibilité ApplyPlay.
- **Squelette de prompt :** « Décide T31 avant toute implémentation Bot : compare les
  deux modèles et migre les appels avec un contrat de révélation explicite. »

### T32 — Renommer le namespace TurnManager

- **Contexte :** le type `TurnManager` et le namespace
  `Psycko.Core.Services.TurnManager` portent le même nom.
- **Problème :** cette collision peut provoquer `CS0234` et impose aujourd'hui un alias.
- **Fichiers concernés :** tous les fichiers `Assets/Scripts/Core/Services/TurnManager/*.cs`,
  `GameOrchestrator.cs`, tests EditMode et références dans `CLAUDE.md`. Aucun fichier
  TurnManager n'était fourni pour migration directe.
- **Règles CLAUDE.md applicables :** dépendance TurnManager→Phase, orchestrateur seul
  appelant les commandes, aucune règle perdue lors du renommage.
- **Travail attendu :** renommer en `Psycko.Core.Services.Turn`, supprimer l'alias,
  mettre à jour namespaces/usings, asmdefs et tests, puis vérifier les références.
- **Hors périmètre :** refactor fonctionnel de la chaîne Step.
- **Dépendances :** T27–T31 stabilisés, compilation complète disponible.
- **Critères d'acceptation :** aucune référence à l'ancien namespace hors historique ;
  plus d'alias `TurnManagerService` ; compilation et tests EditMode verts.
- **Questions à trancher :** nom final de l'assembly ou seuls namespaces ?
- **Pistes de tests NUnit EditMode futurs :** compilation de tous les Steps, contrats
  BeginTurn/ApplyPlay/ResolveRemainder, tests d'intégration GameOrchestrator.
- **Squelette de prompt :** « Effectue T32 en renommage mécanique vérifié par recherche
  globale et compilation ; ne change aucun comportement. »

### Contrat des Handlers Rules/SpecialCards/

Tous les handlers SpecialCards (SevenHandler, TwoHandler, JackHandler, PriestHandler) 
respectent l'interface uniforme suivante :

  • (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
    Retourne la contrainte imposée par ce coup. Jamais null : une pile vide 
    s'exprime par (Normal, Three).

  • PlayDirection ResolveDirection(IGameStateQuery state)
    Retourne le sens de jeu après ce coup (identique à state.Direction sauf Valet).

  • bool DestroysPile
    Propriété statique : true si ce coup détruit la pile courante.

  • bool GrantsReplay
    Propriété statique : true si le poseur rejoue après ce coup.

  • [Membres spécifiques au handler]
    Ajoutés uniquement si l'effet l'exige (ex. TwoHandler.ResolveReplayingPlayer(), 
    SevenHandler.IsGiftTriggered()).

Chaque handler est une classe statique, jamais instancié. Il reçoit toujours 
IGameStateQuery (lecture seule), jamais le type concret GameState.

Les handlers DÉCLARENT les effets (« un Don est dû »), ils ne les APPLIQUENT jamais.
L'application relève de Rules/Phase (PhaseResolver) ou de Services/.
### MOMENT D'APPEL — CRITIQUE

- **T18 — Fix** : Step3 reçoit `reconstruction.State` au lieu de `result.State`, afin que `SevenHandler.IsGiftTriggered` lise la main reconstituée.

---

### Priorité et Imbrication des Effets

Lorsque plusieurs effets sont possibles sur le même coup (ex. Carré + effet spécial 
d'une carte), l'ordre de priorité est :

  1. Effets spéciaux (Prêtre, 2, 7, Joker de Verre, etc.)
  2. Détection Carré (≥ 4 cartes de même hauteur) → Destruction pile + rejeu
  3. Détection Doublon (≥ 2 cartes de même hauteur) → Skip joueur suivant

Note : Ces trois évaluations se font sur la MÊME pile avant de passer au tour suivant.
Seule la Carré interrompt la chaîne Doublon et réinitialise la pile.

---

### Contrat des PhaseResolver (Rules/Phase/)

Chaque phase (Work, Talent, Luck) est représentée par une classe scellée (`sealed`)
héritant de `PhaseResolver` (abstrait). Contrat commun :

  • DefPhase Phase { get; }
    Phase représentée par ce resolver.

  • bool IsLayerPlayable(Player player, CardLayer layer)
    Répond : cette couche est-elle jouable pour ce joueur, à l'instant T ?
    Ne modifie jamais Player. Lecture seule.

  • bool ShouldTransitionToNextPhase(Player player)
    Répond : faut-il transitionner vers la phase suivante, à l'instant T ?
    Ne réalise jamais la transition (pas de WithPhase/WithHand/etc.).

  • DefPhase NextPhase { get; }
    Phase suivante si ShouldTransitionToNextPhase retourne true.

RÈGLES VERROUILLÉES PAR PHASE :

  Work (Phase 1) :
    - Seule Hand est jouable.
    - Transition → Talent quand Hand.Count == 0.
    - INVARIANT (garanti par TurnManager, pas testé dans le resolver) :
      cette méthode est appelée APRÈS re-pioche (étape 2 de l'Ordre Strict).
      Hand.Count == 0 implique donc pioche épuisée par construction.

  Talent (Phase 2) :
    - Seule Hand est jouable (FaceUp déjà ramassées en main à l'entrée de phase).
    - Transition → Luck quand Hand.Count == 0.

  Luck (Phase 3) :
    - Exclusivité mutuelle stricte : si Hand.Count > 0 → seule Hand jouable ;
      sinon → seule FaceDown jouable (jamais les deux simultanément).
    - Transition → Finished quand !player.HasCards (Hand, FaceUp, FaceDown tous vides).

Aucun resolver n'appelle TurnManager, GameOrchestrator, ni aucun handler
SpecialCards — dépendance strictement unidirectionnelle :
TurnManager → PhaseResolver (jamais l'inverse).

Aucun effet spécial n'est câblé ici (SevenHandler, TwoHandler, etc.) —
ces resolvers sont purement déclaratifs.

## Structure du Code

Psycko/
├── CLAUDE.md
├── .editorconfig
├── .gitignore
├── Psycko.slnx
│
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                          (C# pur, zéro dépendance Unity — noEngineReferences: true)
│   │   │   ├── Psycko.Core.asmdef
│   │   │   ├── Domain/
│   │   │   │   ├── Card.cs                       Création des Cartes
│   │   │   │   ├── Deck.cs                       Création de la Pioche et Mélange
│   │   │   │   ├── DefCard.cs                    Enums des Hauteurs, Couleurs et Jokers
│   │   │   │   ├── DefConstraint.cs              Enum des Contraintes
│   │   │   │   ├── DefDirection.cs               Enum des Directions
│   │   │   │   ├── DefLayer.cs                   Enum de l'Origine d'une carte lorsqu'elle est jouée
│   │   │   │   ├── DefPhase.cs                   Enum des Phases de Jeu
│   │   │   │   ├── GameState.cs                  Définit l'État d'une partie à un instant donné
│   │   │   │   ├── Pile.cs                       Définit la Pile
│   │   │   │   ├── Play.cs                       Définit un "Coup" joué
│   │   │   │   ├── Player.cs                     Définit un Joueur
│   │   │   │   └── PlayRejectionReason.cs        Définit le Rejet d'un Play
│   │   │   │ 
│   │   │   ├── Rules/
│   │   │   │   ├── Comparison/
│   │   │   │   │   └── HeightComparison.cs       Compare la Hauteur de 2 DefRank
│   │   │   │   │ 
│   │   │   │   ├── Detection/
│   │   │   │   │   ├── PairDetection.cs          Définit un "Doublon"
│   │   │   │   │   └── QuadDetection.cs          Définit un "Carré"
│   │   │   │   │ 
│   │   │   │   ├── Validation/
│   │   │   │   │   ├── CardPlayability.cs        Détermine si une carte est jouable d'après l'état actuel de      │   │   │   │   │   │                             la Partie
│   │   │   │   │   ├── CardPlayabilityChecker.cs Détecte les cartes jouables d'un joueur
│   │   │   │   │   ├── HandReconstructionPolicy.cs Contrat de Pioche commun à Step2 et Step4
│   │   │   │   │   └── LastCardValidator.cs      Valide la règle : interdiction de terminer une phase sur un 2.
│   │   │   │   │ 
│   │   │   │   ├── Phase/
│   │   │   │   │   ├── PhaseResolver.cs          Contrat abstrait commun aux phases de jeu 
│   │   │   │   │   ├── WorkPhaseResolver.cs      Phase 1 - Le Travail
│   │   │   │   │   ├── TalentPhaseResolver.cs    Phase 2 - Le Talent
│   │   │   │   │   └── LuckPhaseResolver.cs      Phase 3 - La Chance
│   │   │   │   │ 
│   │   │   │   ├── SpecialCards/
│   │   │   │   │   ├── JackHandler.cs            Définit le Valet
│   │   │   │   │   ├── PriestHandler.cs          Définit le Prêtre
│   │   │   │   │   ├── SevenHandler.cs           Définit le 7
│   │   │   │   │   └── TwoHandler.cs             Définit le 2
│   │   │   │   │ 
│   │   │   │   └── Jokers/
│   │   │   │       ├── GlassJokerResolver.cs     Définit le Joker de Verre
│   │   │   │       ├── BlackJokerResolver.cs     Définit le Joker Noir
│   │   │   │       └── ColorJokerResolver.cs     Définit le Joker Couleur
│   │   │   │ 
│   │   │   ├── Interfaces/ 
│   │   │   │   ├── ICardPlayabilityChecker.cs    Contrat de jouabilité des cartes d'un joueur
│   │   │   │   ├── IGameState.cs                 Interface composite
│   │   │   │   ├── IGameStateCommand.cs          Contrat de transition
│   │   │   │   └── IGameStateQuery.cs            Lecture seule de l'état d'une partie
│   │   │   │ 
│   │   │   └── Services/
│   │   │       ├── TurnManager/
│   │   │       │   ├── PickupResolution.cs               Résultat immutable de BeginTurn/ResolvePickup
│   │   │       │   ├── Step1_PlaceCardsResolver.cs       Étape 1 — Pose des cartes sur la pile
│   │   │       │   ├── Step2_ReconstructionResolver.cs   Étape 2 — Reconstruction de main
│   │   │       │   ├── Step3_CardEffectsResolver.cs      Étape 3 — Effets des cartes spéciales
│   │   │       │   ├── Step4_FinalDrawResolver.cs        Étape 4 — Repioche finale après Don éventuel
│   │   │       │   ├── Step5_PileEffectsResolver.cs      Étape 5 — Effets de pile (Doublon/Carré)
│   │   │       │   ├── Step6_AdvanceTurnResolver.cs      Étape 6 — Avancement de tour (joueur suivant, skip, rejeu)
│   │   │       │   ├── TurnManager.cs                    Définit le déroulement d'un tour pour un joueur
│   │   │       │   └── TurnResult.cs                     Résultat immutable porté entre les Steps (état+intentions)
│   │   │       │
│   │   │       ├── GameOrchestrator.cs           Définit le déroulement d'une partie
│   │   │       ├── GameResultCalculator.cs       Définit la Fin d'un partie.
│   │   │       ├── GameSeed.cs                   Génération + stockage de la seed RNG d'une partie
│   │   │       ├── GameLogRecorder.cs            Enregistre chaque action/coup avec horodatage/tour
│   │   │       └── PlayResult.cs                 Définit un appel à GameOrchestrator
│   │   │
│   │   ├── Bots/                          (C# pur, dépend Core — noEngineReferences: true)
│   │   │   ├── Psycko.Bots.asmdef
│   │   │   ├── IPlayerAgent.cs
│   │   │   └── RandomBot.cs
│   │   │
│   │   └── Presentation/                  (Unity 2D, dépend Core + Bots, zéro logique de jeu)
│   │       ├── UI/
│   │       │   ├── CardView.cs
│   │       │   ├── ZoneView.cs
│   │       │   ├── PlayerSeatView.cs
│   │       │   └── GameTableView.cs
│   │       │ 
│   │       ├── Controllers/
│   │       │   ├── GameplayController.cs
│   │       │   ├── InputHandler.cs
│   │       │   └── AnimationController.cs
│   │       │ 
│   │       ├── Models/
│   │       │   ├── CardSkinDefinition.cs
│   │       │   └── HumanSelectionState.cs
│   │       │ 
│   │       └── Scenes/
│   │           └── GameplayLocal.unity
│   │
│   └── Tests/
│       └── EditMode/                      (NUnit EditMode, miroir de la structure Core, 1 fichier de test par fichier de règle)
└── Tools/
    ├──  PsyckoConsole/                     (app console dotnet, simulation de parties + parties humain contre bots)
    │    └── PsyckoConsole.csproj           (Compile Include relatif vers Assets/Scripts/Core et Assets/Scripts/Bots)
    │    Tools/PsyckoConsole/
    └── Formatting/
          ├── CardFormatter.cs         (ex: "2♥", "Valet♠", "Joker de Verre")
          ├── CardSymbols.cs           (♥ ♦ ♣ ♠ — universel, aucune langue)
          └── ICardFormatter.cs        (contrat commun : Format(Card) → string) 
## Notes 
- **Notion** : Source d'un grand nombre d'informations sur le projet 
- **GitHub** :  Repo : https://github.com/HashCtrlShift/Psycko
- **Chemin local** :  C:\Users\raphs\Psycko 

---

**End of CLAUDE.md**