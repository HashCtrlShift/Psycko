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

### T38d — Finalisation du logging : correctifs, export, cas exceptionnels, statistiques 🔄 EN COURS (étape 1/7 terminée)

Ce ticket regroupe tout ce qui reste de T38. Il se fait **dans cet ordre**, avec un commit par étape.

**1. Corrections de `GameLogEffectDetector`** ✅ TERMINÉ *(compilation verte, à valider par tests de régression avant merge)*

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

**2. Export CSV (CsvLogWriter) ✅ ÉCRIT** *(compilation à confirmer, validation par simulation console)*

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

**3. Modes de log** ⏳ À FAIRE : `LogMode` = `Off` / `All` / `ExceptionalOnly`.

- `Off` : aucun recorder n'est créé (coût nul).
- `ExceptionalOnly` : seules les parties exceptionnelles sont exportées.

**4. Cas exceptionnels** ⏳ À FAIRE

- Mécanisme d'extraction automatique : une partie exceptionnelle voit son `GameLog` et sa seed isolés pour une rejouabilité immédiate.
- Critère : une partie en échec est identifiable et **rejouable seule via sa seed**, sans re-simuler les autres.
- **❓ En suspens : définition d'une « partie exceptionnelle »**. Critères envisagés, combinables :
  1. *Durée extrême* : les N parties les plus longues et les N plus courtes (N réglable).
  2. *Seuil* : plus de X coups, avec un plafond anti-boucle infinie (toujours exceptionnel).
  3. *Événement rare* : plusieurs Carrés, Bombe juste après un Carré, pioche épuisée très tôt… (liste à définir).
  4. *Erreur* : toute partie interrompue par une exception (enregistrée d'office).
  - Proposition : **1 + 2 + 4** pour commencer, le critère 3 plus tard. **À trancher.**
- Plafond d'export CSV : 4 000 parties par fichier (voir étape 2).
- Implémentation partielle : `ExceptionalGameDetector` (critères 2 et 4 via `IsExceptional`, critère 1 via `SelectExtremeDurations`) et `ExceptionalGameCriteria` existent dans `Domain/Log`. Reste à câbler l'extraction dans la simulation. 

**5. Statistiques de simulation (SimulationStats) ⏳ À FAIRE — toujours calculées, même en LogMode.Off**

- Nombre de coups : moyenne, médiane, minimum, maximum (avec la seed de la partie concernée), écart-type.
- Classement par joueur : nombre de fois où chacun (Bot1 à Bot4) termine 1er, 2e, 3e, et Psycko (4e).
- Répartition par siège de ces classements (détection d'un biais du premier joueur).
- Ratio de coups pour gagner : pour chaque partie, nombre de Play du vainqueur rapporté au nombre total de Play de la partie (indicateur de difficulté / de qualité des adversaires).
- Nombre de parties terminées normalement et nombre de parties en erreur.
- Fréquence d'activation des cartes spéciales / Joker : reportée à T42.
- Le bilan CSV (colonnes A-B) ne remplace pas SimulationStats : il n'affiche que les agrégats de base. L'écart-type, la répartition par siège et le ratio de coups restent à produire dans `SimulationStats.cs`.

**6. Affichage console direct** (`ConsoleLogPrinter`) ⏳ À FAIRE

- Pour les parties Humain vs Bots : affichage en direct dans la console, avec les noms `Humain`, `Bot1`, `Bot2`, `Bot3`.

**7. `.gitignore`** ⏳ À FAIRE : ajout des dossiers de logs et d'exports CSV générés.

- **Dépendances** : T38a, T38b, T38bis, T38c, T37.
- **Critères d'acceptation globaux** :
  - Doublon sur deux coups détecté, ordre des effets respecté.
  - Un `GameLog` est exporté en CSV et relu sans perte.
  - Une partie en échec est identifiable et rejouable seule via sa seed.
  - Les statistiques de simulation sont affichées avec ou sans CSV.
  - Validation par simulation console sur un lot de seeds (aucun test unitaire).
---

## T41 — Tools/PsyckoConsole/ (ticket parent — 2 sous-tickets)

- **Contexte** : application console dotnet pour simuler des parties complètes et valider Core + Bots avant Presentation. Références : `Psycko.Core` et `Psycko.Bots` uniquement (zéro Unity). Le dossier n'existe pas encore (pas de `Program.cs`).

### T41a — Squelette console et partie unique instrumentée ⏳ À FAIRE

- Création du projet dotnet dans `Tools/PsyckoConsole/` et de `Program.cs`.
- **Menu** : Simuler / Jouer (Humain vs 3 Bots).
- Orchestration d'**une** partie complète :
  - initialisation de `GameState` (deck 63 cartes, seed T37) ;
  - boucle tant que `!GameResultCalculator.IsGameOver`, avec appel des agents (`IPlayerAgent`) selon `ActivePlayerIndex` ;
  - gestion des chemins de décision : pose, pickup, Don, FaceDown (Luck) ;
  - arrêt propre sur blocage, avec un **seuil de coups maximum configurable** (anti-boucle infinie).
- Noms des joueurs : `Bot1` à `Bot4` en simulation ; `Humain`, `Bot1`, `Bot2`, `Bot3` en partie contre les bots.
- Intégration du recorder et de la seed (T38).
- **Critères d'acceptation** : `dotnet run` exécute une partie de bout en bout, **déterministe pour une seed donnée** (distribution initiale), sans intervention manuelle.

### T41b : Simulation de masse ⏳ À FAIRE

- Paramètres réglables : nombre de parties (jusqu'à 1M, défaut raisonnable), seed de départ (int), incrémentation des seeds, LogMode, seuil de coups maximum, ExceptionalGameCriteria.
- Branchement des modules de T38d (CsvLogWriter, SimulationStats, ExceptionalGameDetector).
- Mémoire bornée en ExceptionalOnly : on ne garde que les N parties les plus longues et N plus courtes au fil de l'eau, plus les parties en erreur ou au-delà du seuil, jamais tous les logs.
- Affichage de progression régulier (ex. tous les 10 000 parties), sans ralentissement notable.
- Isolation des erreurs : une partie qui lève une exception est isolée (seed + log exportés) et la boucle continue.
- Contrôle de plage : refuser un lancement où startSeed + gameCount dépasse int.MaxValue.
- Rapport final : statistiques de T38d (durées, classements, Psycko par bot, ratios, taux d'erreur).
- Critère d'acceptation : 1M parties sans crash du processus (hors parties individuellement en échec, isolées).
- Hors périmètre global T41 : interface graphique, intégration Unity, multijoueur réseau.
- Dépendances globales T41 : T35, T36, T37, T38 (complet), T38c.

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
- **Dépendances** : T35 à T41 clos et fonctionnels.
- **Critères d'acceptation** :
  - 1M parties exécutées, rapport chiffré produit.
  - Toute partie en échec a sa seed isolée et un ticket de correction si nécessaire.
  - Décision explicite actée avec Ekinox : GO vers NUnit + Presentation, ou itération sur Core/Bots.
- ❓ **En suspens** : où implémenter la collecte de fréquence d'activation ? Dans `SimulationStats` (ajout après T38d) ou dans un compteur dédié ? À décider avant le lancement de T42.

---

## T43 — Garantie structurelle de ProposeFaceDownPlay (dette technique issue de T35) 🔵

- **Contexte** : tension entre `IPlayerVisibleState.SelfFaceDown` (expose le contenu de la couche 3 du joueur) et `ProposeFaceDownPlay` (choix à l'aveugle, par position uniquement). Une interface C# ne peut pas interdire à un bot mal écrit de lire `state.SelfFaceDown[i]`. `RandomBot` (T36) respecte la règle par construction, mais un futur bot stratégique pourrait tricher silencieusement.
- **Travail attendu** : trancher entre :
  - une vue dédiée plus stricte pour `ProposeFaceDownPlay` (exposant seulement `SelfFaceDownCount`), distincte de `IPlayerVisibleState` ;
  - ou la documentation formelle et définitive du compromis actuel.
- **Fichiers pressentis** : `Psycko.Core.Interfaces/IPlayerVisibleState.cs`, `Psycko.Bots/IPlayerAgent.cs`.
- **Hors périmètre** : réécriture des bots existants tant que ce ticket n'est pas tranché.
- **Dépendances** : T35 (clos).
- **Statut** : dette technique, non bloquante. À trancher avant l'implémentation d'un bot stratégique (MCTS, heuristique).

---

## Ordre d'exécution

✅ T35 → ✅ T36 → ✅ T37 → ✅ T38a → ✅ T38b → ✅ T38bis → ✅ T38c *(absorbe T39 et T40)* → 🔄 **T38d** (étape 1/7 ✅) → T41a → T41b → T42 → T43

**Prochaine étape : T38d, point 2** (Export CSV, `CsvLogWriter`), après validation des tests de régression de l'étape 1 et merge. Les deux points en suspens du point 4 (définition d'une « partie exceptionnelle » et plafond d'export CSV) sont à trancher avant d'implémenter les étapes 3 et 4.