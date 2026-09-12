# CLAUDE.md — Référence des Règles Verrouillées de Psycko

**Objectif :** Capturer toutes les règles de jeu finalisées et sans ambiguïté pour la cohérence entre les sessions d'implémentation.  
**Dernière mise à jour :** 07 Septembre 2026
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
- **Reste actif même à ≤2 joueurs.**

### Doublon (Pair / Skip)
- **Chaque coup joué sur la même hauteur que le dernier coup de la pile** (ou l'avant-dernier si un Joker de Verre s'intercale), **et qui ne complète pas un Carré**, redéclenche un Doublon.
- Se répète à **chaque** coup consécutif de même hauteur (cumul 2 ou 3), sautant à chaque fois le joueur suivant — jusqu'à ce que : le cumul atteigne 4 (→ Carré, priorité), qu'un Joker Noir casse la chaîne, ou qu'une autre hauteur soit jouée.
- **Joker de Verre transparent** : ne casse pas la chaîne, comparaison sautant par-dessus lui.
- **Joker Noir casse la chaîne** : remet tout à zéro immédiatement.
- **Désactivé à ≤2 joueurs** — **le Carré, lui, reste actif à ≤2 joueurs.**

### **Exemple**
J1 : Dame              → rien (première carte de la chaîne)
J2 : Dame (cumul=2)    → Doublon ! J3 sauté
J4 : Dame (cumul=3)    → Doublon ! J1 sauté
J2 : Dame (cumul=4)    → Carré ! (vérifié avant Doublon) Pile détruite, J2 rejoue, nouvelle pile ouverte

### **Exemple complémentaire (coup multi-cartes qui saute directement au Carré)**
J1 : Dame (cumul=1)         → rien
J2 : Dame (cumul=2)         → Doublon ! J3 sauté
J4 : 2x Dame (cumul=4)      → Carré ! (priorité sur Doublon) Pile détruite, J4 rejoue

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

#### Phase 3 (révélation face cachée)
- Si le joueur **retourne un 7 face cachée** à son tour en Phase 3 : **aucun Don** (effet silencieux) — pas de main disponible à ce stade.

#### Phase 3 (7 depuis la main)
- Si le joueur **pose un 7 depuis sa main en Phase 3** (après avoir ramassé d'autres cartes) : Don applicable si la main n'est pas vide après la pose.
- Si c'est sa dernière carte en main : Don non applicable.

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
│   │   │   │   ├── Card.cs                 Création des Cartes
│   │   │   │   ├── Deck.cs                 Création de la Pioche et Mélange
│   │   │   │   ├── DefCard.cs              Enums des Hauteurs, Couleurs et Jokers
│   │   │   │   ├── DefConstraint.cs        Enum des Contraintes
│   │   │   │   ├── DefDirection.cs         Enum des Directions
│   │   │   │   ├── DefPhase.cs             Enum des Phases de Jeu
│   │   │   │   ├── GameState.cs            Définit l'État d'une partie à un instant donné
│   │   │   │   ├── Pile.cs                 Définit la Pile
│   │   │   │   ├── Play.cs                 Définit un "Coup" joué
│   │   │   │   └── Player.cs               Définit un Joueur
│   │   │   ├── Rules/
│   │   │   │   ├── Comparison/
│   │   │   │   ├── Detection/
│   │   │   │   │   ├── PairDetection.cs
│   │   │   │   │   └── QuadDetection.cs
│   │   │   │   ├── Validation/
│   │   │   │   │   ├── CardPlayability.cs
│   │   │   │   │   └── LastCardValidator.cs
│   │   │   │   ├── Phase/
│   │   │   │   │   ├── PhaseResolver.cs         (abstrait)
│   │   │   │   │   ├── WorkPhaseResolver.cs     (Phase 1 - Le Travail)
│   │   │   │   │   ├── TalentPhaseResolver.cs   (Phase 2 - Le Talent)
│   │   │   │   │   └── LuckPhaseResolver.cs     (Phase 3 - La Chance)
│   │   │   │   ├── SpecialCards/
│   │   │   │   │   ├── SevenHandler.cs
│   │   │   │   │   ├── TwoHandler.cs
│   │   │   │   │   ├── JackHandler.cs
│   │   │   │   │   └── PriestHandler.cs
│   │   │   │   └── Jokers/
│   │   │   │       ├── GlassJokerResolver.cs
│   │   │   │       ├── BlackJokerResolver.cs
│   │   │   │       └── ColorJokerResolver.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICardPlayabilityChecker.cs
│   │   │   │   ├── IGameState.cs               Interface composite
│   │   │   │   ├── IGameStateCommand.cs        Contrat de transition
│   │   │   │   └── IGameStateQuery.cs          Lecture seule de l'état d'une partie
│   │   │   └── Services/
│   │   │       ├── GameOrchestrator.cs
│   │   │       ├── TurnManager.cs
│   │   │       ├── GameResultCalculator.cs
│   │   │       ├── GameSeed.cs          (génération + stockage de la seed RNG d'une partie)
│   │   │       └── GameLogRecorder.cs   (enregistre chaque action/coup avec horodatage/tour)
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
│   │       ├── Controllers/
│   │       │   ├── GameplayController.cs
│   │       │   ├── InputHandler.cs
│   │       │   └── AnimationController.cs
│   │       ├── Models/
│   │       │   ├── CardSkinDefinition.cs
│   │       │   └── HumanSelectionState.cs
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