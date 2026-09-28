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