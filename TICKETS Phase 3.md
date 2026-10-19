### T35 — Contrat IPlayerAgent 

- Contexte : IPlayerAgent.cs et RandomBot.cs existent aujourd'hui comme stubs dans Psycko.Bots/. Avant toute implémentation réelle, le contrat devait être figé pour que RandomBot, les futurs bots stratégiques et la simulation console s'appuient sur la même interface sans réécriture.
- Fichiers livrés :
Psycko.Core.Interfaces/IPlayerVisibleState.cs (+ IOpponentVisibleInfo)
Psycko.Bots/IPlayerAgent.cs (remplace le stub)

- Décision actée : vue filtrée retenue (pas d'accès au GameState complet). Règle de masquage validée :
    - Main et FaceDown adverses → quantité visible, contenu caché.
    - FaceUp adverses → contenu visible pour tous, tant qu'elles ne sont pas ramassées.
    - Propre main/FaceUp/FaceDown du joueur → contenu complet visible pour lui-même.

- Contrat final IPlayerAgent (4 méthodes, zéro dépendance Unity, XML doc complète) :
- Play? ProposeNormalPlay(IPlayerVisibleState state) — pose normale (Work/Talent). null = aucun coup proposé.
- bool DecidePickup(IPlayerVisibleState state, bool isForced) — réponse à un pickup forcé/volontaire.
- Play? ProposeFaceDownPlay(IPlayerVisibleState state) — pose FaceDown (phase Luck).
- GiftResolutionChoice ResolveGift(IPlayerVisibleState state) — résolution du Don quand RequiresGiftResolution est vrai.

- Type de retour : toujours une intention brute (Play?, bool, GiftResolutionChoice), jamais un résultat déjà validé (PlayResult, PickupResolution) — validation métier restant strictement centralisée dans GameOrchestrator/TurnManager.
- Fallback si aucun coup valide : porté par l'appelant (GameOrchestrator), jamais par l'agent — un agent renvoie null, il ne décide jamais du ramassage forcé à sa place.
- Hors périmètre (reporté) : implémentation de RandomBot (ticket T36) ; logique de masquage avancée type "mémoire de bot" ou heuristiques de jeu ; dette technique sur la garantie structurelle de ProposeFaceDownPlay → voir T43.
- Dépendances : T27–T34 clos et stabilisés.
- Critères d'acceptation : tous remplis — interface compilée (zéro dépendance Unity), documentation XML complète, revue explicite actée avec Ekinox sur le choix vue filtrée, aucune méthode ne retourne un résultat déjà validé.

### T36 — Implémentation RandomBot

- Contexte : Premier agent concret, utilisé pour valider IPlayerAgent en conditions réelles et faire tourner la simulation console.
- Fichiers concernés : Psycko.Bots/RandomBot.cs.
- Règles CLAUDE.md applicables : aucune logique métier dans Bots (délègue toute validation à Core) ; déterminisme reproductible requis pour les simulations (dépendance à GameSeed, voir T37).
- Travail attendu :
    - Implémenter IPlayerAgent : sélection aléatoire parmi les coups légaux disponibles (via ICardPlayabilityChecker ou équivalent exposé par Core).
    - Gérer les 3 cas de décision : pose normale, pickup (si aucun coup jouable → forcé, sinon jamais volontaire pour RandomBot v1), choix de Don (sélection aléatoire d'une carte à donner + destinataire aléatoire), pose FaceDown en phase Luck.
    - Injection d'un System.Random (ou seed dédiée issue de GameSeed) dans le constructeur — jamais de new Random() interne non traçable.


- Hors périmètre : toute stratégie non aléatoire (StrategicBot, MCTSBot — bots futurs mentionnés dans ARCHITECTURE.md).
- Dépendances : T35 (contrat IPlayerAgent figé).
- Critères d'acceptation :
- RandomBot capable de jouer une partie complète à 4 joueurs sans exception, jusqu'à IsGameOver.
- Déterminisme : même seed → même séquence de décisions.
- Compilation verte, zéro dépendance Unity.

- Pistes de tests NUnit futurs : partie complète simulée avec seed fixe → résultat reproductible ; comportement sur main vide ; comportement sur Don obligatoire.

### T37 — GameSeed

- Contexte : Core/Services/GameSeed.cs centralise la seed utilisée pour reproduire la distribution initiale du Deck. Ne sert pas à reproduire une partie entière — RandomBot n'utilise pas la seed, ses décisions seront tracées plus tard via un système de logs d'actions séparé.
- Fichiers concernés : Psycko.Core/Services/GameSeed.cs, Psycko.Core/Domain/Deck.cs (point d'injection via Shuffle(Random random)).
- Règles CLAUDE.md applicables : Core = C# pur ; aucune dépendance à UnityEngine.Random ; toute source d'aléatoire liée au Deck doit être traçable et rejouable.
- Travail attendu :
    - Garantir que GameSeed est l'unique point d'entrée d'aléatoire pour le mélange du Deck.
    - Exposer GameSeed.CreateRandom(long seed), utilisable identiquement par Core et Console.
    - Garantir qu'une seed identique reproduit toujours le même ordre de Deck.
    - Documenter que la Console génère une seed aléatoire au lancement de chaque partie simulée, puis la logge (pas de seed incrémentale).

- Hors périmètre :
    - Génération cryptographique.
    - Seed persistée en base/PlayFab.
    - Reproductibilité des décisions RandomBot (hors sujet — géré par logs d'actions, ticket séparé à créer).
    - Aléatoire pour les Jokers (aucun n'en nécessite).

- Dépendances : aucune bloquante ; utilisé par Deck (mélange) et T40+ (Console/Simulation).
- Critères d'acceptation :
    - Une seed donnée produit toujours le même ordre de Deck.
    - Aucun System.Random ou UnityEngine.Random non traçable ailleurs dans Core pour ce qui concerne le Deck.
    - Deck.Shuffle ne crée aucun Random en interne — reçoit uniquement celui fourni par GameSeed.

- Statut : Noyau terminé (GameSeed.cs + Deck.Shuffle(Random random) alignés). Reste en suspens : écriture réelle du code Console (génération + log de seed), à traiter dans T40+.
- Pistes de tests NUnit futurs (non prioritaires pour l'instant) : même seed → même ordre de Deck ; seeds différentes → ordres différents (non-garantie absolue, vérification de non-trivialité).

## T38 — GameLogRecorder (ticket parent)

- **Contexte** : tracer chaque action d'une partie pour le débogage post-simulation et la détection de cas exceptionnels. Fichier : `Assets/Scripts/Core/Services/GameLogRecorder.cs`.
- **Principes validés** :
  - Le logging est un **side-effect pur** : aucune branche logique ne dépend du log.
  - La seed ne sert qu'à reproduire la distribution initiale du Deck (T37). Elle ne rejoue pas les décisions des bots.
  - Le log peut être désactivé, avec un coût nul.
  - **Format d'une ligne** (3 colonnes) : `Action | Pile avant le coup | Effets`.
  - **Noms des joueurs** : `Bot1`, `Bot2`, `Bot3`, `Humain` (via `Player.Name`).
  - **Ordre des effets sur une ligne** : Carré / Doublon d'abord, puis l'effet de la carte, séparés par ` + `.
  - Un **Don** transite de main en main : il n'a aucun effet de carte.
- **Hors périmètre global** : interface graphique de visualisation (Presentation).
- **Dépendances globales** : T37 (GameSeed), T35/T36 (bots).

### T38a — Modèle de données du log 

Types immuables dans `Domain/Log` : `GameLog`, `GameLogEntry`, `ActionKind`, `EffectKind`, `EffectLogDetail`. Zéro dépendance Unity, compilation verte.

### T38b — Intégration du recorder dans GameOrchestrator 

**Décision : enregistrement opt-in.** Le recorder est un paramètre optionnel injecté à l'appel (`IGameLogRecorder recorder = null`).

- `null` : aucun log, aucun coût mémoire. C'est le cas par défaut (simulation de masse sans log).
- Fourni : toutes les actions sont enregistrées via `recorder?.Record(...)`.

**Branchement par point de décision :**

| Méthode | Entrées écrites |
|---|---|
| `ApplyPlay` (jeu normal) | 1 × `Play` (cartes, pile après, effets) |
| `ApplyPlay` (ramassage forcé) | 1 × `PickupPile` |
| `RequestPickup` | 1 × `RequestPickup` |
| `ApplyBlindPlay` (carte jouable) | 1 × `BlindPlay` (cartes, pile après, effets) |
| `ApplyBlindPlay` (non jouable, ou 2 terminal) | 2 entrées : `BlindPlay`, puis `PickupPile` |
| `ResolveGiftAndContinue` | 1 × `GiftCard` (carte, donneur, bénéficiaire) |
| Fin de partie | 1 × `GameEnd` (`PsyckoPlayerId`) |

**Fichiers livrés :**

- Domain/Log : `ActionKind`, `EffectKind`, `EffectLogDetail`, `GameLogEntry`, `GameLog`.
- Interfaces : `IGameLogRecorder`.
- Services : `GameLogRecorder`, `GameLogEffectDetector`, `GameOrchestrator` (branchement).
- Supprimé : `CardEffectType.cs`.

**Effets détectés (`EffectKind`)** : `Doublon`, `Detruite2`, `DetruiteCarre`, `DetruiteBombe`, `SensReverse` (le Detail porte la direction), `PriestEffect`, `JokerVerre`, `JokerNoir`.

**Points actés :**

- `Card.Rank` est nullable (`DefRank?`), car un Joker n'a pas de rang. `RankOf` est adapté.
- Pour `ApplyBlindPlay` jouable, l'entrée `BlindPlay` est écrite **après** la résolution, afin de porter les effets et la pile finale.
- La construction du `GameLog` (`new GameLog(seed, recorder.Entries)`) est de la responsabilité de la Console.

**Critères d'acceptation** : entrées cohérentes à chaque point de décision, aucun changement observable sur `PlayResult`.

### T38bis — FirstPlayerResolver + Don du 7 dans ApplyPlay

**1. `FirstPlayerResolver`** (`Core/Domain/FirstPlayerResolver.cs`, nouveau)

- Méthode statique `Resolve(IReadOnlyList<Player> players)`, qui retourne l'index de siège du premier joueur.
- Désigne le détenteur de la **plus petite carte en main** (mains finalisées après la phase d'échange).
- Ordre : rang croissant (3 … As, 2 = hauteur la plus élevée), puis couleur Trèfle < Carreau < Cœur < Pique.
- Jokers exclus. La carte désigne seulement qui commence, sans obligation de la jouer.
- Chaque nouvelle partie redétermine son premier joueur.
- Lève `InvalidOperationException` si aucune carte standard n'est en main (garde-fou).

**2. `GameState.CreateInitial`** (modifié)

- Le paramètre `firstPlayerIndex` est supprimé.
- Signature : `CreateInitial(IEnumerable<Player> players, IEnumerable<Card> drawPile)`. L'appel au resolver est automatique.
- Invariants conservés : joueurs déjà distribués, pioche restante, pile vide, sens horaire, contrainte normale.

**3. `GameOrchestrator.ApplyPlay`** (corrigé)

- Après `TurnManager.ApplyPlay`, ajout du test `if (turnResult.RequiresGiftResolution)`.
- Si vrai : le coup est loggé immédiatement, puis `PlayResult.AwaitingGift(turnResult)` est retourné.
- Aucune mutation (pioche, destruction, changement de joueur) tant que le Don n'est pas résolu.
- Séquence logée : `Play` (le 7), puis `GiftCard`, puis les effets restants.
- `ResolveGiftAndContinue` reprend ensuite `ResolveRemainder` (Step4 → Step6) sans duplication.

**Hors périmètre** : la phase d'échange pré-partie (autre ticket).

**Critères d'acceptation** :

- `FirstPlayerResolver` retourne le siège correct.
- `CreateInitial` compile à deux paramètres.
- `ApplyPlay` capture `RequiresGiftResolution` sans muter l'état.
- Aucune référence résiduelle à l'ancien `CreateInitial` à trois paramètres.

### T38c — Formateur de cartes 

- **Travail** : réécriture de `CardSymbols` et `CardFormatter` avec les vrais types du Core (`DefSuit`, `DefRank`, `DefJokerType`, `Card` nullable). Les anciens fichiers contenaient des références à l'ancien modèle.
- **Fichiers** :
  - `Core/Interfaces/ICardFormatter.cs` (déplacé dans le Core).
  - `Core/Services/Log/Format/CardSymbols.cs` et `CardFormatter.cs`.
  - Les trois anciens fichiers de `Tools/PsyckoConsole/` sont supprimés.
- **Format retenu** : `7♥`, `Valet♠`, `Joker de Verre`, etc. Les trois Jokers sont clairement distincts (Verre / Noir / Couleur).
- **Règle** : zéro logique métier, pur affichage.
- **Critères d'acceptation** :
  - Les 63 cartes ont un format texte correct, sans doublon ni ambiguïté.
  - Aucune référence à l'ancien deck.
  - Compilation verte.
- **Piste de test NUnit futur** : test paramétré sur les 63 cartes (format attendu, unicité, correspondance bijective avec `DefRank` / `DefSuit` / `DefJokerType`).

### T38d — Finalisation du logging : correctifs, export, cas exceptionnels, statistiques ✅ FAIT (étapes 0 à 7 réalisées ; anomalies connues et proposition X/Y en attente d’accord)

Ce ticket regroupe tout ce qui reste de T38. Il se fait **dans cet ordre**, avec un commit par étape.

**0. Correctifs de stabilité de la simulation** ✅ *(réalisés dans une session dédiée, à documenter précisément)*

**1. Corrections de `GameLogEffectDetector`** ✅

- **Doublon** : détecté sur la Pile entière (`PairDetection.IsPairDetected(stateAfterPlacement)`), donc aussi quand la carte jouée complète un Doublon avec la carte précédente de la Pile (deux coups successifs). Il n'est loggé que si le saut est réellement appliqué (paramètre `skipApplied`), donc jamais à 2 joueurs actifs, ni sur un coup Joker.
- **Ordre des effets** : Carré / Doublon d'abord (exclusifs, le Carré prime), puis l'effet propre de la carte. Une ligne peut cumuler plusieurs effets (` + `).
- **Don du 7** : aucun effet de carte (la carte transite de main en main). Pour la ligne qui suit le Don (`afterGift = true`), seul le Carré est retenu ; le Doublon n'est jamais redétecté.
- **Fichiers touchés** :
  - `GameLogEffectDetector.cs` : nouveaux paramètres `skipApplied` et `afterGift` (défaut `false`). Doublon de fichier supprimé (CS0101 / CS0111 / CS0121 résolus).
  - `Step5_PileEffectsResolver.cs` : fusion par OR logique `WithDestroysPile(incomingResult.DestroysPile || destroysPile)`, pour qu'un 2 ou une Bombe ne soit jamais écrasé par un `false`.
  - `GameOrchestrator.cs` : appels à `Detect(..., skipApplied, afterGift)`.
- **Tests de régression à passer avant merge** :
  - Un 2 isolé détruit toujours la Pile.
  - Une Bombe isolée détruit toujours la Pile.
  - Un Carré de 7 avec Don : le Don est résolu, puis le Carré détruit la Pile (log en deux entrées).
  - Un Carré révélé en phase Luck remet la contrainte à `(Normal, Three)`.
  - Un Doublon à 3+ joueurs actifs saute le suivant ; à 2 joueurs actifs, aucun saut ni log de Doublon.
  - Un Doublon sur deux coups successifs est loggé.

**2. Export CSV (`CsvLogWriter`)** ✅

- Un GameLog est sérialisé en CSV, **4 colonnes par partie** : Action | Pile avant | Effets | Détail.
- **2 colonnes de bilan en tête** : A = agrégats du fichier (parties, OK, erreurs, coups moyenne/médiane/min/max) ; B = une ligne par partie (seed, coups, plays, classement, ramassages, [ERREUR]).
- Chaque partie occupe ses propres colonnes (1re : C-D-E-F, 2e : G-H-I-J, etc.).
- Encodage UTF-8 avec BOM, séparateur `;`, échappement des `;`, `"` et retours ligne (compatible Excel).
- Nom de fichier : `GameLogs_[firstSeed]_[lastSeed]_[timestamp]_[part].csv`.
- **Plafond : 4 000 parties par fichier** (16 384 colonnes Excel − 2 de bilan, ÷ 4 = 4 095, plafond retenu 4 000) ; au-delà, découpage en plusieurs fichiers. Réglable via `gamesPerFile`.
- Seeds typées int (cohérent avec GameLog.Seed).
- La colonne « Pile avant » reflète `GameLogEntry.PileBefore` (état de la Pile AVANT l'action). Décision actée : le modèle stocke l'état avant, pas après.
- Les cartes sont formatées via `ICardFormatter` (`CardFormatter` : « Joker Verre / Noir / Couleur », « Valet♠ »), injecté dans `CsvLogFormatter`.
- Critère : un GameLog peut être exporté et relu sans perte d'information exploitable.

**3. Modes de log** ✅ VALIDÉ : `LogMode` = `Off` / `All` / `ExceptionalOnly`.

- Nullable désactivé dans `PsyckoConsole.csproj`, pour rester cohérent avec Unity (pas d'annotations `?` dans Core).
- `Off` : aucun recorder créé (coût nul). `All` : toutes les parties sont exportées. `ExceptionalOnly` : seules les parties exceptionnelles sont exportées.
- Décision : pas de `NullGameLogRecorder`. `GameRunner` crée le recorder uniquement si `recordLog` est vrai, sinon `Log` vaut null.
- Commande : `dotnet run -- sim <nbParties> <seedDépart> <off|all|exceptional>`. Un mode inconnu bascule sur `ExceptionalOnly` (voir anomalies de l'étape 4).
- **Validation (1 000 parties par mode, seed de départ 1) :**
  - `off` : 0 fichier, 0 erreur.
  - `all` : 1 fichier, 1 000 parties (seeds 1 à 1000), 0 erreur.
  - `exceptional` : 1 fichier, 24 parties exportées, 0 erreur.
  - Les stats ne sont pas strictement identiques d'un run à l'autre, car les bots ne sont pas seedés (choix assumé). Seule la distribution initiale dépend de la seed.

**4. Cas exceptionnels** ✅ VALIDÉ

- Décision : **pas de rejeu reproductible par seed.** Les décisions des bots ne sont pas seedées. Le `GameLog` suffit à retracer une partie ; la seed identifie la partie et reproduit uniquement la distribution initiale.
- Une partie exceptionnelle voit son `GameLog` et sa seed isolés dans le CSV (colonne `Action (seed N)`).
- **Paramètres de `ExceptionalGameCriteria` (valeurs par défaut) :**

| Paramètre | Rôle | Valeur |
|---|---|---|
| **N** (`TopN` / `BottomN`) | N parties les plus longues et N les plus courtes | **10** |
| **X** (`MaxTurnThreshold`) | exceptionnelle si coups globaux **> X** (strict) | **8 000** |
| **Y** (seuil bas) | exceptionnelle si coups globaux **< Y** (strict) | **160** |
| **Plafond anti-boucle** | arrêt d'une partie qui ne se termine pas, toujours exceptionnelle | **250 000** |
| **`IncludeErrors`** | toute partie en erreur est exceptionnelle | `true` |

- **Base de X et Y :** 10 000 parties en `Off`, seed 1 : min 127, médiane 859, moyenne 1 163,5, max 18 255. X et Y sont des estimations, à recaler avec P1 / P99 quand `SimulationStats` les calculera (étape 5). Ils restent réglables, jamais codés en dur dans la logique.
- Constat : sur 1 000 parties, X capte environ 0,5 à 1 % des parties. Y n'a jamais été déclenché seul (minimum observé 155 à 192).
- **Critères retenus :** 1 (durée extrême) + 2 (seuil haut X, seuil bas Y) + 4 (erreur ou plafond). Critère 3 (événement rare : plusieurs Carrés, Bombe juste après un Carré, pioche épuisée très tôt…) reporté.
- **Câblage :** extraction au fil de l'eau des parties `> X` ou en erreur, tampons bornés pour les N plus courtes et N plus longues, dédoublonnage par seed, export en un fichier (plafond CSV de 4 000 parties par fichier, voir étape 2).
- **Test du plafond anti-boucle :** avec un plafond temporaire de 300 coups, `sim 200 1 exceptional` : 194 parties interrompues, zéro crash du processus, toutes listées « plafond 300 coups » et exportées. Valeurs par défaut restaurées ensuite.
- Fichiers : `ExceptionalGameCriteria.cs`, `ExceptionalGameDetector.cs`, `Program.cs`.
- **Anomalies à ticketiser séparément (sans correction à la volée) :**
  1. `GameResult.Error` met `TotalMoves` à 0 : une partie interrompue affiche « 0 coups » et perd son nombre de coups réel.
  2. Un mode inconnu en ligne de commande (par exemple `on`) bascule silencieusement sur `ExceptionalOnly`.

**5 :  SimulationStats P1/P99 + branchement console** ✅ Validé
- SimulationStats : ajout P1Moves / P99Moves (rang le plus proche), ligne dédiée « Percentiles » dans ToString().
- Program : PrintStats remplacé par new SimulationStats(allResults), affiché en fin de sim dans les 3 modes (off / all / exceptional).
- Validation : build vert, 0 avertissement. Sommes victoires = Psycko = parties terminées (1000 et 10000).
- Relevé 10 000 parties (2 runs, bots non seedés) :
  P1 = 250 / 246 ; P99 = 5 536 / 6 122 ; médiane 874 / 875 ; moyenne 1 167,7 / 1 199,7 ;
  min 151 / 110 ; max 27 327 / 21 644 ; 0 erreur.
- Proposition (en attente d'accord) : Y 160 → 250, X 8 000 → 6 000.
- Fichiers : Core/Services/Log/SimulationStats.cs, Tools/PsyckoConsole/Program.cs.
- Anomalies connues non corrigées : GameResult.Error met TotalMoves à 0 ; mode inconnu → ExceptionalOnly silencieux.
- À planifier : mode play (humain vs 3 bots) doit supporter les trois LogMode (ticket séparé).

**6. Affichage console direct (`ConsoleLogPrinter`)** ✅ FAIT

- Affichage en direct des parties Humain vs Bots, avec les noms `Humain`, `Bot1`, `Bot2`, `Bot3`.
- `ConsoleLogPrinter` est dans `Tools/PsyckoConsole/` (Core reste pur). Il est branché sur `GameRunner` via `IGameObserver` (événement par événement, sans modification des règles métier).
- Pile affichée en intégralité. Cartes formatées par `CardFormatter` (ex. `7♥`, `Prêtre♣`, `Joker de Verre`).
- Mains des bots masquées par défaut. Option `--reveal` pour les afficher.
- Commande `play [seed] [off|all|exceptional] [--reveal]`, `all` par défaut.
- Interface `ConsoleGameLocal` : choix entre simulation et partie Humain vs Bots, puis choix du mode de log `off` / `all` / `exceptional`.
- Validation par simulation console : `play <seed>` dans les 3 modes, partie abandonnée (`q`) sans crash, `sim 1000 1` dans les 3 modes avec 0 erreur.

**Note — écart doc/code à vérifier lors de l’audit** : la description de l’étape 1 cite `skipApplied` / `afterGift`, alors que le code récent utilise `includeDoublon` / `reachedPhase`. Des événements de log ont aussi été ajoutés récemment (transitions de Phase, victoires, ramassages avec distinction d’un retournement raté) ; ils restent à documenter.

**7. `.gitignore`** ✅ FAIT : dossiers de logs et d'exports CSV générés ignorés (notamment `Tools/PsyckoConsole/Logs/`).

- **Dépendances** : T38a, T38b, T38bis, T38c, T37.
- **Critères d'acceptation globaux** :
  - Doublon sur deux coups détecté, ordre des effets respecté.
  - Un `GameLog` est exporté en CSV et relu sans perte.
  - Une partie en échec ou exceptionnelle est identifiable par sa seed et retraçable via son `GameLog` exporté.
  - Les statistiques de simulation sont affichées avec ou sans CSV.
  - Zéro crash du processus sur 1 000 parties dans chacun des trois modes (cohérent avec T41b).
  - Validation par simulation console sur un lot de seeds (aucun test unitaire).
- **Anomalies connues, hors périmètre T38d (tickets séparés)** :
  - `GameResult.Error` met `TotalMoves` à 0.
  - Un mode de log inconnu bascule silencieusement sur `ExceptionalOnly` côté `sim`.

# T39 — Correctifs de compilation Step3 / TurnManager / GameOrchestrator

Statut : ✅ ABSORBÉ PAR T38c / À CLORE (cohérent avec l’ordre d’exécution ; T38c absorbe T39 et T40)
Dépendances : T38d (étape 1/7), T23 (câblage TurnManager), T38c (formateur de cartes)
Périmètre : Psycko.Core uniquement (zéro dépendance Unity)

## Contexte
Après la découpe de la chaîne Step1→Step6 et le câblage de Step3_CardEffectsResolver dans TurnManager et GameOrchestrator, la compilation (Unity et VS Code / OmniSharp) remonte une série d'erreurs Roslyn. Elles ont toutes la même origine : un fichier Step3_CardEffectsResolver.cs mal formé, et une duplication de la méthode Resolve. Ce ticket regroupe l'ensemble des correctifs de la session.

## Erreurs initiales observées (tableau : Fichier | Ligne | Code | Message)
- GameOrchestrator.cs | 354 | CS0103 | Step3_CardEffectsResolver n'existe pas dans le contexte
- Turn/Step3_CardEffectsResolver.cs | 14 | CS0116 | Un namespace ne peut pas contenir directement des membres
- Turn/Step3_CardEffectsResolver.cs | 68 | CS0103 | GlassJokerResolver n'existe pas dans le contexte
- TurnManager.cs | 112 | CS0103 | Step3_CardEffectsResolver n'existe pas dans le contexte

## Erreurs apparues ensuite (même tableau)
- GameOrchestrator.cs | 354 | CS0121 | Appel ambigu entre deux Resolve (lignes 17 et 33)
- Turn/Step3_CardEffectsResolver.cs | 24 | CS0117 | TurnManager ne contient pas de définition de Resolve
- Turn/Step3_CardEffectsResolver.cs | 33 | CS0111 | Resolve déjà défini avec la même signature
- TurnManager.cs | 112 | CS0121 | Appel ambigu entre deux Resolve

## Causes racines (liste numérotée)
1. Accolades mal positionnées dans Step3_CardEffectsResolver.cs : la déclaration de la classe statique était absente ou fermée trop tôt. La méthode Resolve se retrouvait directement dans le namespace (CS0116), donc la classe n'existait pas pour les appelants (CS0103).
2. Imports manquants : le namespace Psycko.Core.Rules.Jokers n'était pas importé, donc GlassJokerResolver introuvable (CS0103).
3. Duplication de Resolve : deux corps identiques coexistaient dans le même fichier (copie en double), d'où l'ambiguïté (CS0121) et le doublon de signature (CS0111).
4. Mauvais appel : Step3_CardEffectsResolver appelait TurnManager.Resolve, qui n'existe pas (CS0117). L'appel doit être local.

## Correctifs appliqués
C1 — Structure du fichier : le fichier doit contenir, dans l'ordre : les imports (System, Psycko.Core.Domain, Psycko.Core.Rules.Jokers, Psycko.Core.Rules.SpecialCards), le namespace Psycko.Core.Services.Turn, une seule classe statique Step3_CardEffectsResolver avec une seule méthode publique statique Resolve(GameState, Play) retournant TurnResult. Vérifier que la déclaration de classe existe avec ses accolades appariées ; fin de fichier : fermeture méthode, puis classe, puis namespace.
C2 — Imports : ajouter les using des namespaces Rules.Jokers et Rules.SpecialCards ; vérifier qu'ils correspondent aux fichiers réels de SevenHandler, TwoHandler, JackHandler, PriestHandler, GlassJokerResolver, BlackJokerResolver, ColorJokerResolver.
C3 — Supprimer la seconde occurrence de Resolve (celle de la ligne 33) ; une seule méthode doit subsister.
C4 — Ligne 24 : remplacer l'appel TurnManager.Resolve par un appel local à Resolve (ou Step3_CardEffectsResolver.Resolve si l'appel vient d'ailleurs dans la chaîne).

## Critères d'acceptation (cases à cocher)
- Step3_CardEffectsResolver.cs compile sans CS0116, CS0103, CS0111, CS0117
- Une seule méthode Resolve(GameState, Play) dans le fichier
- GameOrchestrator.cs ligne 354 : plus de CS0103 ni CS0121
- TurnManager.cs ligne 112 : plus de CS0103 ni CS0121
- Compilation verte dans Unity et dans VS Code (OmniSharp)
- Aucune modification de la logique de jeu : seule la structure du fichier change

## Tests à passer avant merge
Aucun test unitaire nouveau (correctif de structure). Ekinox confirme la compilation, puis relance les tests NUnit EditMode existants (aucune régression attendue) et vérifie les tests de régression T38d : Carré de 7 avec Don, 2 isolé, Bombe isolée donnent le même résultat qu'avant.

## Points ouverts
- Vérifier les namespaces réels des handlers (supposés d'après ARCHITECTURE.md, non confirmés dans le code)
- Chercher d'éventuels doublons résiduels de Resolve dans les autres fichiers du dossier Turn/
- Mettre à jour ARCHITECTURE.md si le namespace réel de Step3 diffère de Psycko.Core.Services.Turn

## T41 — Tools/PsyckoConsole/ (ticket parent — 2 sous-tickets)

- **Contexte** : application console dotnet pour simuler des parties complètes et valider Core + Bots avant Presentation. Références : `Psycko.Core` et `Psycko.Bots` uniquement (zéro Unity). Le dossier `Tools/PsyckoConsole/` contient `Program.cs`, `ConsoleGameLocal.cs`, `PsyckoConsole.csproj` et `ConsoleLogPrinter`. Le menu Simuler/Jouer et les commandes `sim <n> <seed> <off|all|exceptional>` et `play [seed] [off|all|exceptional] [--reveal]` existent et sont validés.

### T41a — Squelette console et partie unique instrumentée ✅ FAIT

- Le projet dotnet existe dans `Tools/PsyckoConsole/` avec `Program.cs`, `ConsoleGameLocal.cs`, `PsyckoConsole.csproj` et `ConsoleLogPrinter`.
- **Menu** : Simuler / Jouer (Humain vs 3 Bots).
- **Commandes validées** : `sim <n> <seed> <off|all|exceptional>` et `play [seed] [off|all|exceptional] [--reveal]`.
- L'orchestration couvre une partie complète, l'initialisation de `GameState`, la boucle de jeu et les chemins pose, pickup, Don et FaceDown (Luck), avec arrêt propre et seuil anti-boucle.
- Noms des joueurs : `Bot1` à `Bot4` en simulation ; `Humain`, `Bot1`, `Bot2`, `Bot3` en partie contre les bots.
- Intégration du recorder, de la seed et de l'affichage direct.
- **Critères couverts** : le lanceur exécute une partie de bout en bout et la distribution initiale est déterminée par la seed.

### T41b — Simulation de masse ✅ FAIT (hors points T42 ci-dessous)

- **Couvert** : paramètres nombre de parties, seed de départ et `LogMode` ; branchement de `CsvLogWriter`, `SimulationStats` et `ExceptionalGameDetector` ; isolation des erreurs ; rapport final des statistiques.
- **Logs en mode `exceptional`** :
  - Plafond de logs `ExceptionalLogCap = 200` : au-delà, les parties exceptionnelles sont comptées mais pas loguées.
  - Écriture par lots de `ExceptionalBatchSize = 50` parties par fichier CSV (`Logs\GameLogs_<seedMin>_<seedMax>_<horodatage>_<n>.csv`).
  - Exceptionnelle = partie de moins de 250 coups ou de plus de 6000 coups (préfixe console `Exceptionnelle : seed ...`).
  - Extrêmes = les 10 parties les plus courtes et les 10 plus longues, écrites dans un fichier dédié (préfixes console `Extrême Courte : seed ...` et `Extrême Longue : seed ...`), distincts des vraies exceptionnelles.
  - Résumé final : `Exceptionnelles : X détectées, Y loguées, Z non loguées (plafond N). Extrêmes écrites : M.`
- **Validations par exécution** :
  - `sim 20000 1 off` : 20 000 parties terminées, 0 erreur, environ 1 min 07 en Release.
  - `sim 1000 1 exceptional` : 1 000 parties terminées, 0 erreur, plafond et lots vérifiés (tests à cap 5 / lot 2, puis valeurs finales 200 / 50 avec 354 exceptionnelles détectées sur 20 000 parties).
  - Statistiques cohérentes : moyenne d'environ 1 166 coups, médiane 860, P1 254, P99 5733 ; victoires par siège équilibrées (24,8 à 25,2 %).
- **Point ouvert (mémoire)** : la RAM atteint environ 13,6 Go lors d'une simulation de 20 000 parties. La mémoire est bornée en `exceptional` pour les logs, mais la consommation globale reste à analyser (agrégation au fil de l'eau de `SimulationStats` à évaluer pour 1M parties).
- **À vérifier dans le cadre de T42** : progression affichée tous les 10 000 parties ; garde de plage refusant `startSeed + gameCount` au-delà de `int.MaxValue` ; capacité à exécuter 1M parties sans crash du processus.
- Ces trois points restent à confirmer et ne sont pas présentés comme démontrés.

### Hors périmètre global T41

Interface graphique, intégration Unity, multijoueur réseau.

### Dépendances globales T41

T35, T36, T37, T38 (complet), T38c.
---

## T42 — Simulation (exécution et analyse des 1M parties) ⏳ À FAIRE

- **Contexte** : ticket d'exécution, distinct du développement de l'outil (T41). Faire tourner la simulation à grande échelle et analyser les résultats pour valider (ou invalider) le Core avant Presentation.
- **Règles** : validation uniquement, aucune règle métier modifiée. Toute anomalie génère un ticket de correction séparé.
- **Travail attendu** :
  - Lancer la simulation complète via `Tools/PsyckoConsole/`.
  - Collecter les métriques de T38d : taux d'échec, distribution des durées, équité entre sièges, **vainqueurs et Psycko par bot**.
  - Mesurer la fréquence d'activation des cartes spéciales et des Jokers (Valet, 7/Don, 2, Prêtre, Cavalier, Joker Verre, Noir, Couleur, Carré, Doublon). Cette mesure a été reportée de T38d (point 5) vers T42.
  - Pour chaque échec : isoler la seed et ouvrir un ticket de correction dédié dans Core.
  - Rapport de synthèse : tickets ouverts, taux de réussite global, recommandation GO / NO-GO vers Bots avancés + NUnit + Presentation.
- **Hors périmètre** : correction des bugs (tickets séparés), rédaction des tests NUnit définitifs (ticket futur nourri par cette analyse).
- **Dépendances** : T35 à T41 clos et fonctionnels ; T44 et T45 sont nécessaires pour comparer RandomBot vs DiscardBot par type de bot (agrégation des vainqueurs/Psycko par type de bot et par siège).
- **Critères d'acceptation** :
  - 1M parties exécutées, rapport chiffré produit.
  - Toute partie en échec a sa seed isolée et un ticket de correction si nécessaire.
  - Décision explicite actée avec Ekinox : GO vers NUnit + Presentation, ou itération sur Core/Bots.
- ❓ **En suspens** : où implémenter la collecte de fréquence d'activation ? Dans `SimulationStats` (ajout après T38d) ou dans un compteur dédié ? La décision reste à prendre avant le lancement de T42.

---

## T43 — Garantie structurelle de ProposeFaceDownPlay (dette technique issue de T35) 🔵

- **Contexte** : tension entre `IPlayerVisibleState.SelfFaceDown` (expose le contenu de la couche 3 du joueur) et `ProposeFaceDownPlay` (choix à l'aveugle, par position uniquement). Une interface C# ne peut pas interdire à un bot mal écrit de lire `state.SelfFaceDown[i]`. `RandomBot` (T36) respecte la règle par construction, mais un futur bot stratégique pourrait tricher silencieusement.
- **Travail attendu** : trancher entre :
  - une vue dédiée plus stricte pour `ProposeFaceDownPlay` (exposant seulement `SelfFaceDownCount`), distincte de `IPlayerVisibleState` ;
  - ou la documentation formelle et définitive du compromis actuel.
- **Fichiers pressentis** : `Psycko.Core.Interfaces/IPlayerVisibleState.cs`, `Psycko.Bots/IPlayerAgent.cs`.
- **Hors périmètre** : réécriture des bots existants tant que ce ticket n'est pas tranché.
- **Dépendances** : T35 (clos).
- **Statut** : dette technique, non bloquante. À trancher avant l'implémentation d'un bot stratégique (MCTS, heuristique). Le futur DiscardBot (T45) respecte la règle par construction ; T43 n’est donc pas bloquant pour lui.

---

## T44 — Console configurable (RunConfiguration, BotFactory, ArgumentParser) ⏳ À FAIRE

- **Contexte** : faire converger le lanceur en ligne de commande et le menu interactif vers une même configuration d’exécution, sans dupliquer le parsing ni la construction des parties.
- **Travail attendu** :
  - Introduire `RunConfiguration`, `BotFactory` et `ArgumentParser` dans `Tools/PsyckoConsole/`.
  - Construire la même configuration depuis le CLI et le menu : mode sim/play, nombre de parties, seed, `LogMode`, reveal, `ExceptionalGameCriteria` (N/X/Y/plafond) réglables, type de bot par siège (`random`/`discard`/`human`) et noms des joueurs.
  - Faire de `Program.cs` un simple point d’entrée ; `ConsoleGameLocal` ne construit plus que la configuration.
  - Produire des erreurs d’arguments lisibles (sans `int.Parse` non protégé) et parser `LogMode` une seule fois.
  - Permettre une partie 100 % bots observée en direct. Exemple : `dotnet run -- sim 10000 1 off --bots discard,random,random,random`.
  - Corriger les deux anomalies connues : mode inconnu → erreur explicite, et `GameResult.Error` conserve le `TotalMoves` réel. Préciser la modification Core éventuelle pour ce dernier point.
- **Fichiers pressentis** : `Tools/PsyckoConsole/Program.cs`, `ConsoleGameLocal.cs`, nouveaux `RunConfiguration.cs`, `BotFactory.cs`, `ArgumentParser.cs` ; Core uniquement pour `GameResult.Error` si nécessaire.
- **Hors périmètre** : modification des règles métier, de Core hors correction `GameResult.Error`, ou ajout de tests unitaires dans la console.
- **Dépendances** : T41, T38d ; T45 pour le type de bot DiscardBot.
- **Critères d’acceptation** : CLI et menu produisent la même `RunConfiguration` ; commandes et erreurs lisibles ; un seul parsing du `LogMode` ; modes `off/all/exceptional` fonctionnels en sim et play ; configuration des sièges et noms fonctionnelle ; parties 100 % bots observables ; anomalies corrigées.
- **Tests / validation** : validation par simulation console cohérente avec T38d : `sim 1000` dans les trois modes, 0 erreur ; pas de test unitaire dans la console.

## T45 — DiscardBot (Psycko.Bots) ⏳ À FAIRE

- **Contexte** : ajouter un bot déterministe permettant de comparer une stratégie simple à RandomBot dans les simulations.
- **Travail attendu** : jouer la plus petite carte jouable selon l’ordre de hauteur via `HeightComparison` / `ICardPlayabilityChecker`, et non l’ordre brut du rang (le 2 est la plus forte, le 3 la plus faible) ; donner la plus petite carte au Don du 7 ; `ProposeExchanges` retourne une liste vide.
- **Points à trancher explicitement** : Jokers (proposition : dernier recours seulement) ; groupes/Doublons/Carrés (proposition : une carte par coup, ou le plus petit groupe) ; destinataire du Don (proposition : même règle que RandomBot) ; égalités de hauteur (départage déterministe) ; FaceDown (index 0, sans lire la carte, règle T43 respectée par construction) ; `DecidePickup` avec `isForced` comme RandomBot.
- **Fichiers pressentis** : `Assets/Scripts/Bots/DiscardBot.cs` et tests NUnit EditMode associés.
- **Hors périmètre** : changement des règles Core, bot stratégique avancé, ou décision finale sur les points listés sans accord.
- **Dépendances** : T36, T44 (pour placer le bot à un siège), T35.
- **Critères d’acceptation** : choix de la plus petite carte ; le 2 n’est jamais choisi trop tôt ; Jokers en dernier selon la décision ; Don de la plus petite carte ; FaceDown sans lecture de la carte ; couverture ≥ 90 %.
- **Tests NUnit EditMode** : plus petite carte, 2 jamais choisi trop tôt, Jokers en dernier, Don de la plus petite carte, FaceDown sans lecture. Avec un bot déterministe et trois aléatoires, comparer en permutant les sièges.

## T46 — Audit documentaire et mise à jour Notion / ARCHITECTURE.md / CLAUDE.md ⏳ À FAIRE

- **Contexte** : réaligner la documentation après T44–T45 et l’audit de T38d.
- **Travail attendu** : mettre à jour `ARCHITECTURE.md` et `CLAUDE.md`, aligner la documentation de `GameLogEffectDetector`, des événements de log récents, et du statut Step3/T39 ; préparer le texte Notion.
- **Fichiers pressentis** : `ARCHITECTURE.md`, `CLAUDE.md`, texte de passation/Notion.
- **Hors périmètre** : écriture automatique dans Notion ; le texte Notion est fourni à Ekinox à coller.
- **Dépendances** : T44, T45, audit T38d.
- **Critères d’acceptation** : aucun écart doc/code identifié sans mention « à vérifier » ; texte Notion prêt à coller ; statuts et chemins cohérents.
- **Tests** : relecture croisée des documents et vérification des noms/chemins.

## T47 — Tickets d’anomalies séparés si non absorbés par T44 ⏳ À FAIRE

- **Contexte** : conserver une traçabilité explicite des anomalies de T38d et décider si elles sont absorbées par T44.
- **Anomalies à suivre** :
  - (a) `GameResult.Error` avec `TotalMoves = 0` : proposition, absorbé par T44.
  - (b) mode inconnu → `ExceptionalOnly` silencieux : proposition, absorbé par T44.
  - (c) mode `play` doit supporter les trois `LogMode` : proposition, reste traité dans T44 car il concerne le même fichier et le même parsing ; à confirmer par Ekinox.
- **Fichiers pressentis** : `Tools/PsyckoConsole/` et, pour (a), `GameResult` dans Core si nécessaire.
- **Hors périmètre** : autres anomalies non listées et correction de règles métier.
- **Dépendances** : T44 et confirmation d’Ekinox.
- **Critères d’acceptation** : pour chaque anomalie, statut « absorbée par T44 » ou ticket séparé décidé ; aucun comportement silencieux non documenté.
- **Tests** : cas d’argument inconnu, vérification du nombre réel de coups en erreur, et `play` dans `off/all/exceptional`.

## Ordre d'exécution

✅ T35 → ✅ T36 → ✅ T37 → ✅ T38a → ✅ T38b → ✅ T38bis → ✅ T38c (absorbe T39/T40) → ✅ T38d → ✅ T41a → 🔄 T41b (reste à vérifier) → T44 → T45 → T42 → T43 → T46

## Reste à faire

1. Obtenir l’accord sur les seuils X/Y : Y 160 → 250 et X 8 000 → 6 000.
2. Traiter ou confirmer les anomalies connues (TotalMoves réel, mode inconnu, LogMode de `play`) et clore T39.
3. Réaliser T44 (console configurable), puis T45 (DiscardBot).
4. Exécuter T42 et analyser la simulation à grande échelle ; décider où collecter la fréquence d’activation.
5. Trancher T43, puis effectuer T46 (audit documentaire et texte Notion).
6. Phase suivante : Présentation, dans un nouveau chat avec un document de passation.
