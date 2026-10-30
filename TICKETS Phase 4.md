TICKETS Phase 4 — Présentation locale (GameplayLocal)
Phase 4 : interface Unity 2D, jouable en local (1 humain + 3 bots), paysage uniquement. Zéro logique de jeu dans la Présentation. Toute décision passe par GameRunner / GameOrchestrator et les contrats existants.
Hors périmètre Phase 4 : Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise de partie, mode spectateur depuis GameLog, options d'accessibilité daltonisme et taille de carte (reportées Phase 5).

## T50 — Bootstrap scène, asmdef, paysage verrouillé ✅

- **Statut :** FAIT (en attente de merge)
- **Contexte :** poser le socle Unity (scène, assemblies, orientation) avant toute logique de jeu.
- **Fichiers :**
  - `Assets/Scenes/GameplayLocal.unity`
  - `Assets/Scripts/Presentation/Psycko.Presentation.asmdef` (références : `Psycko.Core`, `Psycko.Bots`)
- **Règles CLAUDE.md :** Presentation dépend de Core + Bots ; zéro logique de jeu ; aucune référence à un assembly de test en production.

### Travail réalisé

1. **Player Settings**
   - Default Orientation = **Landscape Left**
   - Auto Rotation : uniquement **Landscape Left** et **Landscape Right** ; Portrait et Portrait Upside Down désactivés.
2. **Scène `GameplayLocal`**
   - Template : 2D URP (Lit 2D), ajoutée en index 0 dans Build Profiles > Scene List.
   - Hierarchy : Main Camera, Global Light 2D, MainCanvas, EventSystem.
3. **Caméra**
   - Projection **Orthographic**
   - Renderer : **Default Renderer** (Renderer 2D)
   - Background Type = **Solid Color**, couleur **#100d1f**
4. **MainCanvas**
   - Render Mode = **Screen Space - Camera** (Render Camera = Main Camera, Plane Distance 100)
   - Canvas Scaler : **Scale With Screen Size**, Reference Resolution **1920 × 1080**, Screen Match Mode **Match Width Or Height**, Match **0.5**, Reference Pixels Per Unit 100

### Décision de conception

- **Render Mode = Screen Space - Camera** (et non Overlay) : permet de gérer le tri (Sorting Layer / Order in Layer) entre l'UI et les sprites de cartes pour les animations (T5x), et reste compatible avec le post-traitement URP.

### Dépendances

Aucune.

### Hors périmètre

Toute logique de jeu, tout script de contrôleur, toute UI de table (T51 et suivants).

### Critères d'acceptation

- [x] La scène `GameplayLocal` s'ouvre sans erreur.
- [x] La rotation portrait est impossible (vérifiée dans le Device Simulator).
- [x] Compilation verte dans Unity (Console vide) et dans VS Code (Problems vide), sans nouvel avertissement.
- [x] Le fond de caméra est `#100d1f`.
- [x] `Psycko.Presentation.asmdef` référence `Psycko.Core` et `Psycko.Bots`, sans assembly de test.

### Tests

Contrôle manuel en Game View 16:9 et 19.5:9 ; aucun test NUnit (ticket de structure).

## T51 — Adaptateur de partie (GameplayController, UnityHumanAgent)

- **Statut :** EN PAUSE — non mergé. Tests EditMode 8/8 verts, mais couverture Psycko.Core ≈ 1 % (rapport Summary.xml). Critère de couverture ≥ 90 % non atteint. Reprise prévue en Phase 5 (Rattrapage de couverture Core), puis merge.
- **Contexte :** lancer une partie via GameRunner (1 humain + 3 RandomBot) et recevoir les événements, sans aucune logique de jeu côté Présentation.
- **Fichiers :**
  - `Assets/Scripts/Presentation/Controllers/GameplayController.cs`
  - `Assets/Scripts/Presentation/Controllers/UnityHumanAgent.cs` (implémente `IPlayerAgent`)
  - Tests : `Assets/Tests/EditMode/Presentation/UnityHumanAgentTests.cs` (assembly de test EditMode référençant `Psycko.Presentation`, `Psycko.Core`, `Psycko.Bots`)
- **Règles CLAUDE.md :** l'humain ne propose qu'une intention brute ; la validation reste dans `GameOrchestrator` ; nullable désactivé, pas de `?` sur les types référence.

### Travail

1. `UnityHumanAgent : IPlayerAgent`
   - Expose l'intention humaine de façon non bloquante pour le thread principal.
   - Méthode de soumission appelée par l'UI (`Submit(intention)`) qui complète l'intention en attente.
   - Sans intention soumise : l'agent reste en attente, sans exception, sans intention par défaut inventée.
   - Double soumission : la seconde est ignorée (règle documentée dans le code).
   - Soumission hors tour (aucune demande en attente) : rejetée explicitement (règle documentée dans le code).
2. `GameplayController`
   - Construit la partie via `GameRunner` : 1 `UnityHumanAgent` + 3 `RandomBot`.
   - S'abonne à `IGameObserver` pour recevoir les notifications et les relayer (événement C#) vers la future UI.
   - Se désabonne proprement à la destruction (`OnDestroy`).
   - Aucune règle de jeu, aucune validation de coup.

### Dépendances

T50.

### Critères d'acceptation

- [x] Une partie complète se termine sans exception, humain inclus, avec actions pilotées par code de test.
- [x] `UnityHumanAgent` renvoie exactement l'intention reçue.
- [x] Comportement sans intention défini et testé.
- [x] Désabonnement vérifié (aucune fuite d'observateur).
- [x] Compilation verte Unity, sans nouvel avertissement.
- [x] Tests NUnit EditMode verts (8/8).
- [ ] Couverture ≥ 90 % sur les fichiers du ticket et sur Psycko.Core (non atteint : Core ≈ 1 %, UnityHumanAgent ≈ 27 %, GameplayController ≈ 63 %). Reprise en Phase 5.

### Tests (NUnit EditMode, obligatoires avant merge)

- `UnityHumanAgent` renvoie l'intention soumise.
- `UnityHumanAgent` sans intention : reste en attente, ne lève pas d'exception.
- Double soumission : seconde ignorée (conforme à la règle documentée).
- Soumission hors tour : rejetée (conforme à la règle documentée).
- Partie complète 1 agent de test scripté + 3 `RandomBot` (graine fixe) : se termine, aucun état illégal.
- `GameplayController` : abonnement et désabonnement à `IGameObserver`.

### Hors périmètre

Toute UI de table, toute animation, tout rendu de carte (T52 et suivants).

T52 — Vue de table (sièges, zones) en paysage

Statut : À FAIRE
Contexte : afficher les 4 sièges et les zones pioche/pile à partir de IPlayerVisibleState uniquement.
Fichiers : UI/GameTableView.cs, UI/PlayerSeatView.cs, UI/ZoneView.cs.
Travail :
Disposition en croix : humain en bas (main pleine largeur), bots à gauche / en haut / à droite, pile au centre.
Compteurs visibles pour les adversaires (main, FaceUp, FaceDown : quantités seulement).


Dépendances : T51.
Critères : aucun contenu caché d'un adversaire n'est rendu ; rendu conforme en paysage.
Tests : unitaires sur le mapping état → compteurs (projection pure, extraite en classe testable).

T53 — Contrat de skin et skins de base interchangeables

Statut : À FAIRE
Contexte : plusieurs skins de base sélectionnables ; un skin ne change que l'apparence.
Fichiers : Interfaces/ICardSkin.cs, Skins/DefaultCardSkin.cs, Models/SkinSet.cs (ScriptableObject : sprites, dos de carte, couleurs de table, polices), Models/CardSkinDefinition.cs, Skins/SkinRegistry.cs.
Règles : un skin ne modifie ni tailles de zone ni hitboxes d'input.
Travail :
ICardSkin : visuel d'une Card et du dos de carte, sans logique.
Au moins 2 SkinSet de base (ex. « Classique », « Sombre »).
SkinRegistry : liste de skins, sélection par identifiant.


Dépendances : T52.
Critères : changer de skin ne modifie aucune position ni taille d'élément interactif ; un nouveau SkinSet s'ajoute sans toucher au code.
Tests : identifiant de skin inconnu → skin par défaut (pas d'exception).

T54 — CardView utilisant le skin

Statut : À FAIRE
Fichiers : UI/CardView.cs.
Travail : rendu d'une carte à partir de Card + ICardSkin ; états visuels (normal, surbrillance jouable, relevée/sélectionnée, dos).
Dépendances : T53.
Critères : les 63 cartes s'affichent sans sprite manquant (placeholder explicite si absent).
Tests : mapping Card → clé de sprite, unicité et couverture des 63 cartes (test paramétré).

T55 — Input humain, surbrillance, relevé, phase d'échange pré-partie

Statut : À FAIRE
Contexte : l'humain joue, ramasse, choisit une carte FaceDown, effectue un Don, et participe à la phase d'échange (~1 min).
Fichiers : Controllers/InputHandler.cs, Models/HumanSelectionState.cs.
Travail :
Surbrillance des cartes jouables, calculée via le Core (ICardPlayabilityChecker), jamais en Presentation.
Sélection : relevé de la ou des carte(s) choisie(s) ; groupe de même rang uniquement ; boutons Jouer / Ramasser.
FaceDown : choix par position, sans révélation.
Don du 7 : choix carte + destinataire, bouton Valider bloqué tant que incomplet.
Échange pré-partie : échange 1 main ↔ 1 FaceUp, bouton « Prêt ! », fin automatique à 1 min.


Dépendances : T54.
Critères : aucune intention invalide n'est transmise hors rejet métier renvoyé par le Core ; le Don n'est jamais contournable.
Tests NUnit EditMode : HumanSelectionState (ajout/retrait, groupe de même rang, réinitialisation) ; blocage du Don incomplet.

T56 — Animations et vitesse de jeu

Statut : À FAIRE
Fichiers : Controllers/AnimationController.cs.
Travail :
Animations pilotées par les événements IGameObserver (pose, pioche, ramassage, destruction) ; aucune décision.
Multiplicateurs pour l'humain : x0.5, x1, x2.
Mode accéléré bots seuls : quand l'humain n'est plus en jeu ou a terminé, les bots s'affrontent à vitesse maximale (animations réduites ou instantanées), y compris lors d'une boucle longue.


Dépendances : T55.
Critères : la vitesse n'influe jamais sur l'état de jeu ; une partie bots seuls à vitesse maximale reste fluide.
Tests : calcul de durée d'animation selon le multiplicateur (unitaires).

T57 — Bandeau d'effets

Statut : À FAIRE
Fichiers : UI/EffectBannerView.cs.
Travail : bandeau court (« Doublon ! », « Don du 7 », « Valet : sens inversé », « Carré ! », etc.) piloté par IGameObserver / EffectKind.
Dépendances : T56.
Critères : un bandeau par effet détecté ; jamais déclenché par la Presentation elle-même.
Tests : mapping EffectKind → libellé, couverture complète.

T58 — Écran de fin de partie

Statut : À FAIRE
Fichiers : UI/EndGameView.cs.
Travail : affichage du Psycko et du classement depuis GameResult. Boutons « Rejouer » et « Quitter ».
Dépendances : T51.
Critères : aucun calcul de classement en Presentation.

T59 — Scène SkinPreview et menu de sélection

Statut : À FAIRE
Fichiers : Scenes/SkinPreview.unity, Controllers/SkinPreviewController.cs.
Travail : affichage des 63 cartes avec le skin courant ; sélection d'un autre SkinSet sans relancer la scène ; changement visible en direct.
Dépendances : T54.
Critères : les 63 cartes visibles sur une seule scène ; changement de skin instantané.

T60 — Performance (atlas de sprites, draw calls)

Statut : À FAIRE
Contexte : fixer une cible de fluidité dès le début, pas en fin de phase.
Travail :
Regrouper les sprites des cartes d'un même skin dans un Sprite Atlas unique : Unity peut alors dessiner plusieurs cartes en un seul appel de rendu (un « draw call » = une instruction envoyée au GPU). Moins de draw calls = moins de charge sur téléphone.
Cible : 60 fps sur appareil d'entrée de gamme ; mesure avant/après au Profiler Unity, consignée dans ce fichier.


Dépendances : T54.
Critères : mesure Profiler consignée.


Ordre d'exécution
✅T50 → T51 → T52 → T53 → T54 → T55 → T56 → T57 → T58 → T59 → T60