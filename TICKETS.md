# TICKETS.md — Historique chronologique de Psycko

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

### T25 — Correction SevenHandler.IsGiftTriggered (index de siège) — ✅ FAIT

**Bug corrigé :** `IsGiftTriggered` accédait à `state.Players[play.PlayerId]`, traitant
l'Id joueur comme un index de siège. Divergence Id/siège → mauvais joueur lu ou
`IndexOutOfRangeException`.

**Correction :**
- `IGameStateQuery` : ajout de `int GetSeatIndex(int playerId)` (lecture pure, lève
  `ArgumentException` si Id introuvable).
- `GameState` : aucun changement — `GetSeatIndex` existait déjà, simplement rattaché
  formellement au contrat `IGameStateQuery`.
- `SevenHandler.IsGiftTriggered` : résolution du siège via `state.GetSeatIndex(play.PlayerId)`
  avant tout accès à `state.Players`. Tous les autres membres de `SevenHandler`
  (`ResolveConstraint`, `DestroysPile`, `GrantsReplay`, `ResolveGiftCardCount`,
  `ResolveDirection`) restent inchangés — nécessaires à `Step3_CardEffectsResolver`.

**Comportement Id introuvable :** `ArgumentException` explicite (cohérent avec le reste de
`GameState`).

**Périmètre respecté :** SevenHandler reste en lecture seule. `JackHandler`,
`PriestHandler`, `TwoHandler`, `BlackJokerResolver`, `ColorJokerResolver`,
`GlassJokerResolver` inchangés (n'utilisent pas `GetSeatIndex`).

**Backlog tests NUnit (à écrire) :**
- `IsGiftTriggered` : Id ≠ siège → Don évalué sur le bon joueur.
- `IsGiftTriggered` : 7 en FaceDown → jamais de Don.
- `IsGiftTriggered` : Id inconnu → lève `ArgumentException` via `GetSeatIndex`.

**Hors périmètre (reporté) :** T26c (orchestrateur du Don), T26 à T32, Bots, Présentation, réseau.

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

### MOMENT D'APPEL — CRITIQUE

- **T18 — Fix** : Step3 reçoit `reconstruction.State` au lieu de `result.State`, afin que `SevenHandler.IsGiftTriggered` lise la main reconstituée.

