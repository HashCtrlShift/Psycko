# ARCHITECTURE.md — Carte du projet Psycko

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

Psycko/
├── CLAUDE.md
├── ARCHITECTURE.md
├── TICKETS Phase 2.md
├── TICKETS Phase 3.md
├── TICKETS Phase 4.md
├── .editorconfig
├── .gitignore
├── Psycko.slnx
│
├── CodeCoverage/                          (ignoré) Rapports de couverture NUnit/Unity, régénérés à chaque run
│   └── .gitignore                         Ignore tout le contenu sauf lui-même
│
├── Assets/
│   ├── csc.rsp                            -nullable:disable (nullable reference types désactivé)
│   ├── Scenes/
│   │
│   ├── Presentation/                      Assets Unity de présentation (hors code)
│   │   └── Skins/
│   │       ├── SkinSet_Classique.asset    (T53) SkinSet de base : couleurs et sprites « Classique »
│   │       └── SkinSet_Sombre.asset       (T53) SkinSet de base : couleurs distinctes, skin « Sombre »
│   │
│   ├── Scripts/
│   │   ├── Bots/                          C# pur, dépend Core — noEngineReferences: true
│   │   │   ├── DiscardBot.cs              Agent qui sélectionne la plus petite carte à jouer et applique une stratégie
│   │   │   ├── Psycko.Bots.asmdef
│   │   │   └── RandomBot.cs               Agent qui sélectionne aléatoirement une option parmi celles possibles
│   │   │
│   │   ├── Core/                          C# pur, zéro dépendance Unity — noEngineReferences: true
│   │   │   ├── Psycko.Core.asmdef
│   │   │   ├── Domain/
│   │   │   │   ├── Exchange/
│   │   │   │   │   ├── ExchangeChoice.cs            Échange pré-partie : 1 carte Main contre 1 carte FaceUp du même joueur
│   │   │   │   │   ├── ExchangeRejectionReason.cs   Enum d'erreurs liées à l'échange
│   │   │   │   │   ├── PreGameExchange.cs           Résultat d'une opération d'échange : nouvel état, ou rejet motivé
│   │   │   │   │   └── PreGameExchangeState.cs      État immuable de la phase d'échange pré-partie
│   │   │   │   │
│   │   │   │   ├── Log/
│   │   │   │   │   ├── ActionKind.cs                Types d'actions enregistrables dans le log d'une partie
│   │   │   │   │   ├── EffectCategory.cs            Catégories des Effets enregistrés
│   │   │   │   │   ├── EffectKind.cs                Effet enregistré dans la colonne "Effets" du GameLog
│   │   │   │   │   ├── EffectLogDetail.cs           Détail immuable d'un effet enregistré dans une entrée de log
│   │   │   │   │   ├── ExceptionalGameCriteria.cs   Seuils qui rendent une partie "exceptionnelle"
│   │   │   │   │   ├── ExceptionalGameDetector.cs   Isole les parties "exceptionnelles"
│   │   │   │   │   ├── GameLog.cs                   Log complet et immuable d'une partie
│   │   │   │   │   ├── GameLogEntry.cs              Entrée immuable du log de partie
│   │   │   │   │   ├── GameResult.cs                Résultat chiffré d'une partie (coups, classement, ramassages, erreur)
│   │   │   │   │   └── LogMode.cs                   Modes de logging pour la simulation (Off / All / ExceptionalOnly)
│   │   │   │   │
│   │   │   │   ├── Card.cs                       Création des Cartes
│   │   │   │   ├── Deck.cs                       Création de la Pioche et Mélange
│   │   │   │   ├── DefCard.cs                    Enums des Hauteurs, Couleurs et Jokers
│   │   │   │   ├── DefConstraint.cs              Enum des Contraintes
│   │   │   │   ├── DefDirection.cs               Enum des Directions
│   │   │   │   ├── DefLayer.cs                   Enum de l'Origine d'une carte lorsqu'elle est jouée
│   │   │   │   ├── DefPhase.cs                   Enum des Phases de Jeu
│   │   │   │   ├── FirstPlayerResolver.cs        Désigne le premier joueur
│   │   │   │   ├── GameState.cs                  Définit l'État d'une partie à un instant donné
│   │   │   │   ├── GiftResolutionChoice.cs       Définit la Carte Donnée et le Joueur Choisi
│   │   │   │   ├── OpponentVisibleInfo.cs        Vue immuable et filtrée d'un adversaire
│   │   │   │   ├── Pile.cs                       Définit la Pile
│   │   │   │   ├── Play.cs                       Définit un "Coup" joué
│   │   │   │   ├── Player.cs                     Définit un Joueur
│   │   │   │   ├── PlayerVisibleState.cs         Vue immuable de l'état de la partie pour UN joueur
│   │   │   │   └── PlayRejectionReason.cs        Définit le Rejet d'un Play
│   │   │   │
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICardFormatter.cs             Contrat commun : Format(Card) → string
│   │   │   │   ├── ICardPlayabilityChecker.cs    Contrat de jouabilité des cartes d'un joueur
│   │   │   │   ├── IFaceDownBlindView.cs         Vue minimale pour le choix aveugle FaceDown : expose uniquement FaceDownCount (aucun contenu de carte)
│   │   │   │   ├── IGameLogRecorder.cs           Contrat d'enregistrement des actions d'une partie
│   │   │   │   ├── IGameObserver.cs              Contrat d'observation en direct d'une partie (Console, Présentation)
│   │   │   │   ├── IGameState.cs                 Interface composite
│   │   │   │   ├── IGameStateCommand.cs          Contrat de transition
│   │   │   │   ├── IGameStateQuery.cs            Lecture seule de l'état d'une partie
│   │   │   │   ├── IPlayerAgent.cs               Contrat de décision d'un agent joueur
│   │   │   │   └── IPlayerVisibleState.cs        Vue filtrée pour les joueurs
│   │   │   │
│   │   │   ├── Rules/
│   │   │   │   ├── Comparison/
│   │   │   │   │   └── HeightComparison.cs       Compare la Hauteur de 2 DefRank
│   │   │   │   │
│   │   │   │   ├── Detection/
│   │   │   │   │   ├── PairDetection.cs          Définit un "Doublon"
│   │   │   │   │   ├── ProverbDetection.cs       Définit le "Proverbe"
│   │   │   │   │   └── QuadDetection.cs          Définit un "Carré"
│   │   │   │   │
│   │   │   │   ├── Jokers/
│   │   │   │   │   ├── BlackJokerResolver.cs     Définit le Joker Noir
│   │   │   │   │   ├── ColorJokerResolver.cs     Définit le Joker Couleur
│   │   │   │   │   └── GlassJokerResolver.cs     Définit le Joker de Verre
│   │   │   │   │
│   │   │   │   ├── Phase/
│   │   │   │   │   ├── LuckPhaseResolver.cs      Phase 3 - La Chance
│   │   │   │   │   ├── PhaseResolver.cs          Contrat abstrait commun aux phases de jeu
│   │   │   │   │   ├── TalentPhaseResolver.cs    Phase 2 - Le Talent
│   │   │   │   │   └── WorkPhaseResolver.cs      Phase 1 - Le Travail
│   │   │   │   │
│   │   │   │   ├── SpecialCards/
│   │   │   │   │   ├── JackHandler.cs            Définit le Valet
│   │   │   │   │   ├── PriestHandler.cs          Définit le Prêtre
│   │   │   │   │   ├── SevenHandler.cs           Définit le 7
│   │   │   │   │   └── TwoHandler.cs             Définit le 2
│   │   │   │   │
│   │   │   │   └── Validation/
│   │   │   │       ├── CardPlayability.cs           Détermine si une carte est jouable d'après l'état actuel de la Partie
│   │   │   │       ├── CardPlayabilityChecker.cs    Détecte les cartes jouables d'un joueur
│   │   │   │       ├── HandReconstructionPolicy.cs  Contrat de Pioche commun à Step2 et Step4
│   │   │   │       └── LastCardValidator.cs         Valide la règle : interdiction de terminer une phase sur un 2
│   │   │   │
│   │   │   └── Services/
│   │   │       ├── Log/
│   │   │       │   ├── Format/
│   │   │       │   │   ├── CardFormatter.cs          "2♥", "Valet♠", "Joker de Verre"
│   │   │       │   │   └── CardSymbols.cs            ♥ ♦ ♣ ♠ — universel, aucune langue
│   │   │       │   │
│   │   │       │   ├── CsvLogFormatter.cs            Traduit une entrée de log en texte lisible en français
│   │   │       │   ├── CsvLogWriter.cs               Exporte les GameLog en CSV Excel (bilan + 4 colonnes par partie)
│   │   │       │   ├── GameLogEffectDetector.cs      Traduit le résultat d'un coup en liste d'effets pour le log
│   │   │       │   ├── GameLogRecorder.cs            Enregistre chaque action/coup avec horodatage/tour
│   │   │       │   ├── GameResultTracker.cs          Collecte les compteurs d'une partie (coups, plays, ramassages)
│   │   │       │   ├── ListGameLogRecorder.cs        Recorder en mémoire : une instance par partie
│   │   │       │   ├── ObservableLogRecorder.cs      Recorder qui relaie chaque entrée à un IGameObserver en direct
│   │   │       │   └── SimulationStats.cs            Agrège les GameResult (moyenne, médiane, écart-type, P1/P99, équité par siège)
│   │   │       │
│   │   │       ├── Turn/
│   │   │       │   ├── PickupResolution.cs               Résultat immutable de BeginTurn/ResolvePickup
│   │   │       │   ├── Step1_PlaceCardsResolver.cs       Étape 1 — Pose des cartes sur la pile
│   │   │       │   ├── Step2_ReconstructionResolver.cs   Étape 2 — Reconstruction de main
│   │   │       │   ├── Step3_CardEffectsResolver.cs      Étape 3 — Effets des cartes spéciales
│   │   │       │   ├── Step4_FinalDrawResolver.cs        Étape 4 — Repioche finale après Don éventuel
│   │   │       │   ├── Step5_PileEffectsResolver.cs      Étape 5 — Effets de pile (Doublon/Carré)
│   │   │       │   ├── Step6_AdvanceTurnResolver.cs      Étape 6 — Avancement de tour (joueur suivant, skip, rejeu)
│   │   │       │   ├── TurnManager.cs                    Définit le déroulement d'un tour pour un joueur
│   │   │       │   └── TurnResult.cs                     Résultat immutable porté entre les Steps (état + intentions)
│   │   │       │
│   │   │       ├── BlindPlayResolutions.cs       Carte Révélée en Luck
│   │   │       ├── CardDealer.cs                 Distribue un paquet mélangé
│   │   │       ├── FaceDownBlindView.cs          Implémentation immuable de IFaceDownBlindView, construite par GameRunner pour chaque tour aveugle
│   │   │       ├── GameOrchestrator.cs           Définit le déroulement d'une partie
│   │   │       ├── GameResultCalculator.cs       Définit la Fin d'une partie
│   │   │       ├── GameRunner.cs                 Joue une partie complète (seed → distribution → échange → tours → GameResult), notifie un IGameObserver optionnel. Contient GameRunOutcome (Result + Log optionnel + Error). Une boucle unique pour Console, Unity et réseau. En cas d'erreur, GameResult.Error reçoit TotalMoves/TotalPlays du tracker
│   │   │       ├── GameSeed.cs                   Génération + stockage de la seed RNG d'une partie
│   │   │       └── PlayResult.cs                 Définit un appel à GameOrchestrator
│   │   │
│   │   └── Presentation/                  (Unity 2D, dépend Core + Bots, zéro logique de jeu)
│   │       ├── Psycko.Presentation.asmdef       Références : Psycko.Core, Psycko.Bots
│   │       ├── Interfaces/
│   │       │   └── ICardSkin.cs                 (T53) Contrat visuel : Id, DisplayName, GetCardSprite(Card), GetCardBackSprite(), TableColor, SeatColor, Font. Aucune logique
│   │       │
│   │       ├── UI/
│   │       │   ├── CardView.cs                       (T54, à venir) Rendu d'une carte via ICardSkin : états normal / jouable / sélectionnée / dos
│   │       │   ├── GameTableView.cs                  (T52) Vue de table en paysage : disposition en croix, humain en bas
│   │       │   ├── PlayerSeatView.cs                 (T52) Siège d'un joueur : compteurs main / FaceUp / FaceDown
│   │       │   ├── TableProjection.cs                (T52) Projection pure IPlayerVisibleState → compteurs, sans MonoBehaviour, testable
│   │       │   ├── TableViewModels.cs                (T52) Modèles d'affichage de la table
│   │       │   └── ZoneView.cs                       (T52) Zones pioche et pile
│   │       │
│   │       ├── Controllers/
│   │       │   ├── AnimationController.cs            (à venir)
│   │       │   ├── GameplayController.cs             (T53) Lance une partie via LocalGameSession. Réponse d'échange pré-partie par défaut « aucun échange » (OnHumanRequest), _autoAnswerExchanges retiré
│   │       │   ├── HandSorter.cs                     Tri d'une main pour l'affichage, sans logique de jeu
│   │       │   ├── InputHandler.cs                   (à venir)
│   │       │   ├── LocalGameSession.cs               Session de partie locale : 1 humain + 3 bots via GameRunner
│   │       │   ├── SeatLayout.cs                     Disposition des sièges autour de la table (humain en bas)
│   │       │   └── UnityHumanAgent.cs                IPlayerAgent humain
│   │       │
│   │       ├── Models/
│   │       │   ├── CardSkinDefinition.cs             (T53) Définition d'un skin : Id, DisplayName, référence au SkinSet
│   │       │   ├── HumanSelectionState.cs            (à venir)
│   │       │   └── SkinSet.cs                        (T53) ScriptableObject « Create > Psycko > Skin Set » : sprites, dos, couleurs de table et de siège, police
│   │       │
│   │       ├── Skins/
│   │       │   ├── DefaultCardSkin.cs                (T53) Implémentation de ICardSkin lisant un SkinSet, valeurs de repli si SkinSet nul
│   │       │   └── SkinRegistry.cs                   (T53) MonoBehaviour : liste de CardSkinDefinition, Select(id) (inconnu → skin par défaut), Current, All
│   │       │
│   │       └── Scenes/
│   │           └── GameplayLocal.unity
│   │
│   └── Tests/
│       └── EditMode/                      NUnit EditMode, miroir de la structure Core (tests reportés en Phase 5 pour T51/T52/T53)
│           ├── Psycko.Tests.EditMode.asmdef     Assembly de test EditMode
│           └── Presentation/
│               ├── GameplayControllerTests.cs       Tests de GameplayController
│               ├── HandSorterTests.cs               Tests de HandSorter
│               ├── LocalGameSessionTests.cs         Tests de LocalGameSession
│               ├── SeatLayoutTests.cs               Tests de SeatLayout
│               ├── UnityHumanAgentExtraTests.cs     Tests complémentaires de UnityHumanAgent
│               └── UnityHumanAgentTests.cs          Tests de UnityHumanAgent
│
└── Tools/
    └── PsyckoConsole/                     App console dotnet : simulation de parties + parties Humain contre Bots
        ├── ArgumentParser.cs              Parsing CLI
        ├── BotFactory.cs                  Crée les agents selon SeatKind (Human, Random)
        ├── ConsoleGameLocal.cs            IMenu interactif : ne construit que des arguments, rejoués dans ArgumentParser
        ├── ConsoleHumanAgent.cs           Agent "Humain" en console ('q' = abandon), cartes formatées via CardFormatter
        ├── ConsoleLogPrinter.cs           Affichage en direct d'une partie
        ├── GameSession.cs                 Moteur d'exécution unique
        ├── Program.cs                     Point d'entrée minimal
        ├── RunConfiguration.cs            Configuration d'exécution unique et immuable
        ├── PsyckoConsole.csproj           Compile Include relatif vers Assets/Scripts/Core et Assets/Scripts/Bots
        ├── .gitignore                     Ignore bin/, obj/ et Logs/
        ├── bin/                           (ignoré) Debug/net8.0, Release/net8.0
        └── Logs/                          (ignoré) CSV générés
---

## Notes 
- **Notion** : Source d'un grand nombre d'informations sur le projet 
- **GitHub** :  Repo : https://github.com/HashCtrlShift/Psycko
- **Chemin local** :  C:\Users\raphs\Psycko 

---

# PsyckoConsole — Guide des commandes

Application console `dotnet` de simulation et de jeu manuel (`Tools/PsyckoConsole/`).
Elle utilise `Psycko.Core` et `Psycko.Bots`, sans dépendance Unity.

## Lancer

```powershell
cd Tools\PsyckoConsole

# Depuis les sources
dotnet run -- <commande> [arguments] [options]

# Depuis le build Release
dotnet build -c Release
dotnet bin\Release\net8.0\PsyckoConsole.dll <commande> [arguments] [options]
```

Sans argument, le menu interactif se lance (`ConsoleGameLocal`). Le menu et la ligne de commande construisent la même `RunConfiguration` et passent par le même point d'exécution (`Program.Execute`).

## Commandes

### `sim` — simulation de masse

```
sim <n> <seed> <off|all|exceptional> [options]
```

| Argument | Description | Contrainte |
|---|---|---|
| `n` | Nombre de parties | entier ≥ 1 |
| `seed` | Seed de départ | entier ; `seed + n - 1` ≤ 2 147 483 647 |
| mode de log | `off`, `all` ou `exceptional` | insensible à la casse |

Les 3 arguments sont obligatoires. Sièges par défaut : 4 × `random`. Noms par défaut : `Bot1, Bot2, Bot3, Bot4`.

### `play` — partie interactive

```
play [seed] [off|all|exceptional] [options]
```

| Argument | Défaut |
|---|---|
| `seed` | `Environment.TickCount` (différente à chaque lancement) |
| mode de log | `all` |

Sièges par défaut : toi au siège 0 (`human`) contre 3 `random`. Noms par défaut : `Humain, Bot1, Bot2, Bot3`. Une seule partie est jouée. Seed et mode de log sont positionnels : pour donner le mode, il faut aussi donner la seed.

## Modes de log

| Mode | Effet |
|---|---|
| `off` | Aucun log. Statistiques uniquement. |
| `all` | Toutes les parties sont écrites en CSV, par lots de `--batch` parties. |
| `exceptional` | Les parties exceptionnelles sont écrites (au plus `--cap`), plus les extrêmes (voir ci-dessous). |

Les fichiers sont écrits dans `Logs/` (relatif à `Tools/PsyckoConsole/`). Chaque fichier écrit est annoncé par `Log écrit : <chemin>`.

### Mode `exceptional` en détail

Une partie est exceptionnelle si l'un de ces critères est vrai :

- moins de `--short` coups ;
- plus de `--long` coups ;
- plafond `--move-cap` atteint ;
- partie en erreur.

Pendant la simulation, chaque exceptionnelle est annoncée : `Exceptionnelle : seed S (raison, N coups)`.
Seules les `--cap` premières sont écrites ; les suivantes sont comptées mais non loguées.

En fin de simulation, les 10 parties non exceptionnelles les plus courtes et les 10 plus longues sont ajoutées dans un fichier séparé (`Extrême Courte` / `Extrême Longue`). Un récapitulatif est affiché :

```
Exceptionnelles : X détectées, Y loguées, Z non loguées (plafond C). Extrêmes écrites : E.
```

## Sortie console

- Progression en `sim` : `[i/N]` tous les `min(10 000, N/10)` parties.
- Erreurs de partie : `[ERREUR] seed S : message (TotalMoves=M)`, limitées aux 20 premières, puis `... K autres erreurs non affichées.`
- Fin de `sim` : le bloc de statistiques (`SimulationStats`).
- Fin de `play` : `Fin de partie. Psycko : <nom>` ou `Partie interrompue : <message>`.

## Codes de sortie

| Code | Signification |
|---|---|
| `0` | Exécution terminée sans erreur de partie, ou `--dry-run` |
| `1` | Au moins une partie en erreur (dont plafond `--move-cap` atteint) ; en `play`, partie interrompue |
| `2` | Argument invalide, ou construction des bots impossible (ex. `discard`) |

## Options

| Option | Valeur | Défaut | Rôle |
|---|---|---|---|
| `--bots a,b,c,d` | 4 types parmi `random`, `human`, `discard` | voir commandes | Type de bot par siège. Un seul `human` maximum. |
| `--names a,b,c,d` | 4 noms non vides et distincts (casse ignorée) | voir commandes | Noms des joueurs. |
| `--short N` | entier ≥ 0 | 250 | Seuil bas : une partie avec **moins** de N coups est exceptionnelle. |
| `--long X` | entier ≥ 1 | 6000 | Seuil haut : une partie avec **plus** de X coups est exceptionnelle. |
| `--batch Y` | entier ≥ 1 | 50 | Parties par fichier CSV en mode `all`. |
| `--cap Z` | entier ≥ 1 | 200 | Plafond de logs exceptionnels conservés. |
| `--move-cap M` | entier ≥ 1 | 250 000 | Plafond anti-boucle : partie interrompue à M coups (toujours exceptionnelle, comptée en erreur). |
| `--reveal` | aucune | désactivé | Affiche les mains des bots. **`play` uniquement.** |
| `--dry-run` | aucune | désactivé | Affiche la configuration résolue et quitte (code 0), sans jouer. |

Règle : `--short` doit être strictement inférieur à `--long`.

Non réglables en ligne de commande (fixés dans `ArgumentParser`) : `TopN = 10` (10 parties les plus longues isolées), `BottomN = 10` (10 plus courtes). `IncludeErrors` reste à `true`.

## Exemples

```powershell
# Validation de référence
dotnet run -- sim 1000 1 off
dotnet run -- sim 1000 1 all
dotnet run -- sim 1000 1 exceptional

# Réglage des critères exceptionnels
dotnet run -- sim 10000 1 exceptional --short 300 --long 4000 --cap 50

# Plafond anti-boucle personnalisé
dotnet run -- sim 5 1 off --move-cap 500000

# Partie humaine, seed fixe, log complet, mains visibles
dotnet run -- play 42 all --reveal

# Humain au siège 2, noms personnalisés
dotnet run -- play 7 off --bots random,random,human,random --names A,B,Moi,D

# Simulation 100 % bots avec noms
dotnet run -- sim 100 1 off --names Ana,Ben,Cleo,Dan

# Voir la configuration résolue sans lancer
dotnet run -- sim 1000 1 all --batch 25 --dry-run
```

## Messages d'erreur

Toute entrée invalide affiche un message, puis l'usage, et retourne le code 2, sans exception non gérée.

| Cas | Message |
|---|---|
| Commande inconnue | `Commande inconnue : '<x>'.` |
| `sim` incomplet | `sim attend <n> <seed> <off|all|exceptional>.` |
| Nombre non numérique | `<libellé> doit être numérique : '<x>'.` |
| Nombre hors plage | `<libellé> hors plage (<min> à <max>) : <v>.` |
| Mode de log inconnu | `Mode de log inconnu : '<x>'. Valeurs possibles : off, all, exceptional.` |
| `--bots` mauvais nombre | `--bots attend exactement 4 valeurs (reçu : N).` |
| Type de bot inconnu | `Type de bot inconnu : '<x>'. Valeurs possibles : random, human, discard.` |
| Deux humains | `Un seul siège humain est autorisé.` |
| `--names` invalide | `--names attend 4 noms non vides et distincts.` |
| `--reveal` en `sim` | `--reveal est réservé à play.` |
| Option inconnue | `Option inconnue : '<x>'.` |
| Valeur manquante | `Valeur manquante pour <option>.` |
| Plage de seeds | `Plage de seeds invalide : seed X + N parties dépasse 2147483647.` |
| `--short` ≥ `--long` | `--short doit être inférieur à --long.` |
| `discard` demandé | `Bot 'discard' non disponible : il sera livré par T45.` (au moment de construire les agents) |

## Codes de sortie

| Code | Signification |
|---|---|
| `0` | Exécution terminée, ou `--dry-run` |
| `2` | Argument invalide (message + usage affichés) |
| `1` | Erreurs de parties détectées (à confirmer dans `GameSession`) |

## Reproductibilité

- La distribution des cartes dépend du seed : la partie *i* utilise `seed + i`.
- Les décisions des bots ne dépendent pas du seed : `BotFactory` crée un `Random` sans seed (choix assumé). Deux exécutions avec le même seed ne donnent donc pas forcément la même partie.

## Bots disponibles

| Type | Statut |
|---|---|
| `random` | Disponible (`RandomBot`) |
| `human` | Disponible (`ConsoleHumanAgent`), un seul siège |
| `discard` | Reconnu par le parseur, refusé à la construction jusqu'à T45 |