# TICKETS.md — Historique chronologique de Psycko

### État d'implémentation et feuille de route

- **T21 — FAIT** : `GameState` implémente `IGameStateCommand` ; les 9 méthodes
  délèguent aux `With*` existants et ne portent aucune règle de jeu.
- **T22 — FAIT** : la gestion fine du ramassage (forcé vs volontaire, remise à (Normal, Three), notification à la Présentation)
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

**Hors périmètre (reporté) :** T28 (orchestrateur du Don), T26 à T32, Bots, Présentation, réseau.

### T26 — Exécution de l'intention DrawCards (pioche en phase Work) — MERGÉ (à confirmer par Ekinox après compilation locale)

**Statut** : Code livré, en attente de validation locale (compilation + revue Ekinox) avant merge effectif.

**Contenu** :
- `GameOrchestrator.ApplyPlay` exécute désormais, dans l'ordre strict Step2→Step3→Step4 :
  1. `DrawCards` pour `turnResult.DrawCount` (reconstruction Step2, avant Don éventuel)
  2. `DestroyPile` si `turnResult.DestroysPile` (effet Step3, ex. 2/Bombe)
  3. `SetConstraint` si `NextConstraint`/`NextRefRank` renseignés (Step3)
  4. `WithDirection` si `NextDirection` renseigné (Step3, Valet)
  5. `DrawCards` pour `turnResult.FinalReconstruction.Value.DrawCount` (Step4, après Don éventuel)
- Aucune nouvelle commande créée : `IGameStateCommand.DrawCards(int, int)` existait déjà et sa décision de bornage (`Math.Min(needed, pile)`) est déjà portée en amont par `HandReconstructionPolicy` — pioche insuffisante/vide gérée nativement, sans exception.
- **Décision DestroysPile** : pas de ticket T26 séparé. Le mécanisme est identique à celui déjà en place dans `ApplyBlindPlay` (phase Luck) et est intégré directement dans T26 au même point d'application que les autres intentions Step3.

**Hors périmètre confirmé (reporté)** :
- `TriggersFaceUpPickup`, `TriggersPhaseTransition`, `TargetPhase` → transitions de phase Work→Talent→Luck : **T27**.
- `RequiresGiftResolution` → Don du 7 : **T28**.

## T26bis — Correctif Step5 replay projection

**Statut :** mergé

### Contexte

`Step5_PileEffectsResolver` calcule le rejeu accordé au poseur d'un Carré via
`poseur.HasCards`. Ce champ est lu sur `state`, un état dans lequel aucune
pioche n'a encore été réellement appliquée : Step2 (`DrawCount`) et Step4
(`FinalReconstruction.DrawCount`) ne portent que des intentions, jamais de
mutation — conformément à la doctrine TurnManager (Steps en lecture seule,
GameOrchestrator seul exécuteur de `IGameStateCommand`).

### Bug

Un poseur qui vide sa main en complétant un Carré, mais pour qui une pioche
est déjà décidée (Step2 ou Step4), se voyait refuser à tort le rejeu, car
`HasCards` était évalué avant l'application de cette pioche par
GameOrchestrator.

### Correctif

### Correctif

Projection des intentions de pioche déjà connues avant de statuer sur le rejeu :

var willDraw = incomingResult.DrawCount > 0
    || (incomingResult.FinalReconstruction?.DrawCount ?? 0) > 0;
step5Replay = poseur.HasCards || willDraw;

### T27 — Transitions de phase, y compris pendant un rejeu — MERGÉ / CLÔTURÉ

**Statut :** mergé et clôturé.

### Contexte

Chaque joueur progresse individuellement selon la séquence Work → Talent → Luck →
Finished. Un rejeu peut traverser une transition de phase, notamment Talent → Luck
après un Carré.

## Correctif

Dans `GameOrchestrator.ApplyPlay`, le bloc utilisant la méthode inexistante
`WithPlayerPhase` a été remplacé par :

if (turnResult.TriggersPhaseTransition)
{
    newState = (GameState)newState.AdvancePlayerPhase(state.ActivePlayerIndex);
}

### T28 — Don du 7 (résolution explicite)

**Statut : ✅ Mergé**

**Dépendances :** T25 (résolution du siège via GetSeatIndex), T26/T26bis
(fusion OR SkipNext/Replay), T27 (transitions de phase via AdvancePlayerPhase).

**Résumé :**
Le Don du 7 était déclaré par `SevenHandler` (`RequiresGiftResolution` sur
`TurnResult`) mais jamais exécuté : aucune commande de transfert n'existait
dans `IGameStateCommand`, et `GameOrchestrator.ApplyPlay` ignorait
silencieusement le flag en continuant à appliquer les intentions post-Step3
sur un état où le Don n'avait pas eu lieu.

**Livré :**
- `PlayResult.PendingGift` (champ `TurnResult?`, pas de nouveau statut enum) :
  signale un Don en attente sans être un rejet (`Success == true`).
- Early-return dans `ApplyPlay` : si `turnResult.RequiresGiftResolution`,
  retourne immédiatement `PlayResult.AwaitingGift(turnResult)` avant toute
  application d'intention.
- `GameOrchestrator.ResolveGiftAndContinue(state, play, pendingResult, choice)` :
  - Garde `InvalidOperationException` hors contexte de Don.
  - Rejette (`InvalidCards`) si la carte choisie n'est pas dans la main
    de l'actif.
  - Exécute le transfert réel, réinjecte l'état muté.
  - Reprend Step4 → Step5 → Step6 via `TurnManager.ResolveRemainder`.
  - Applique le reste des intentions comme `ApplyPlay` standard.
- Correction structurelle de T18-bis : `Step4_FinalDrawResolver` ne peut
  plus recevoir un état obsolète d'avant-Don.

### T29 — Step6 ActivePlayerIndex + Correction erreurs de compilation nullable (CS8625/CS8632/CS8604)

**Statut : ✅ Mergé**

### Partie 1 — Step6 : ActivePlayerIndex

- **Contexte :** mise en place/finalisation de la gestion de `ActivePlayerIndex`
  dans le flux de résolution de tour (Step6), nécessaire à l'enchaînement
  correct des tours entre joueurs.
- **Fichiers concernés :** `Assets/Scripts/Core/Services/TurnManager/TurnResult.cs`
  et services associés à la résolution de tour.
- **Résultat :** l'index du joueur actif est correctement propagé/mis à jour
  à chaque résolution de tour, sans effet de bord sur les autres services.

### Partie 2 — Correction erreurs de compilation nullable

- **Contexte :** `PlayResult.cs` et `TurnResult.cs` utilisaient des annotations
  nullable (`?`) sur des types référence (`GameState?`, `IReadOnlyList<int>?`)
  hors de tout contexte `#nullable`, provoquant des erreurs/avertissements
  incohérents entre Unity (CS8632, CS8604) et VS Code/OmniSharp (CS8625).
- **Cause racine :** absence de configuration explicite du nullable context
  au niveau du projet — Unity et OmniSharp déduisaient chacun une configuration
  différente en l'absence de directive commune.
- **Fichiers concernés :** `Assets/Scripts/Core/Services/PlayResult.cs`,
  `Assets/Scripts/Core/Services/TurnManager/TurnResult.cs`,
  `Assets/csc.rsp` (créé), `.vscode/settings.json`.
- **Solution appliquée :**
  - Ajout de `Assets/csc.rsp` avec `-nullable:disable` pour forcer un contexte
    nullable désactivé et cohérent sur toute la compilation Unity.
  - Régénération des `.csproj` via Unity après ajout du `csc.rsp`.
  - Suppression des annotations `?` superflues sur les types référence dans
    `PlayResult.cs` et `TurnResult.cs` (les `?` sur `PlayRejectionReason?` et
    `TurnResult?` sont conservés : ce sont des value types, donc `Nullable<T>`
    classique, indépendant du nullable reference context).
  - Alignement de `.vscode/settings.json` avec `omnisharp.enableRoslynAnalyzers`
    et `omnisharp.useModernNet` pour que VS Code suive la même configuration
    que la compilation Unity réelle.
- **Résultat :** zéro erreur/avertissement dans Unity et dans VS Code après
  recompilation des deux côtés.
- **Hors périmètre :** pas de réactivation future du nullable reference
  context sans décision explicite documentée ici.
- **Dépendances :** aucune.

## T30 — GameResultCalculator (calcul de fin de partie)

**Statut : Clos (code)**

### Résumé

`GameResultCalculator` implémenté comme source unique de vérité pour la détection de fin de partie, remplaçant toute logique dispersée précédente.

### Décision A/B appliquée

**Option A retenue** : fin de partie = 1 seul joueur actif restant (`CurrentPhase != DefPhase.Finished`). Ce joueur restant est désigné "Psycko" (perdant).

### Implémentation

- `Psycko.Core/Services/GameResultCalculator.cs` — pur, déterministe, zéro dépendance Unity.
- `GameOrchestrator` délègue entièrement le calcul de fin de partie à `GameResultCalculator`, sur tous les chemins (normal, replay, pickup).
- Aucune détection résiduelle de fin de partie dans les Steps (1-6) — restent lecture seule.
- Une seule source de vérité pour `IsGameOver` dans tout le code.

### Critères d'acceptation — statut

- `GameResultCalculator` implémenté, pur, déterministe, sans dépendance Unity.
- Une seule implémentation de la règle de fin de partie dans tout le code.
- `GameOrchestrator` délègue entièrement, sans doublon résiduel.
- Aucun calcul de fin de partie dans les Steps.
- Calcul effectué sur l'état final post-résolution (normal, replay, pickup).

### T31 — Remplacer les `!.Value` par des gardes explicites

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

### T32 — Rendre public `RequestPickup`

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

### T33 — Décider le type de retour avant les Bots

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

### T34 — Renommer le namespace TurnManager

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

