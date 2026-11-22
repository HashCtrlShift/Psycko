# TICKETS Phase 4 — Présentation locale (GameplayLocal)
Phase 4 : interface Unity 2D, jouable en local (1 humain + 3 bots), paysage uniquement. Zéro logique de jeu dans la Présentation. Toute décision passe par GameRunner / GameOrchestrator et les contrats existants.
Hors périmètre Phase 4 : Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise de partie, mode spectateur depuis GameLog, options d'accessibilité daltonisme et taille de carte (reportées Phase 5).

Interface Unity 2D jouable en local (1 humain + 3 bots), en paysage uniquement. La Présentation ne contient aucune logique de jeu : toute décision passe par `GameRunner` / `GameOrchestrator` et les contrats existants.

**Hors périmètre Phase 4 :** Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise, mode spectateur depuis GameLog, options daltonisme et taille de carte (Phase 5).

**Ordre :** T50✅ → T51✅ → T52✅ → T53✅ → T54✅ → T55 → T60 → T56 → T57 → T58 → T59. Tests reportés en Phase 5 (décision Ekinox), sauf demande explicite. Le merge de T52–T55 est bloqué tant que T51 n'est pas repris.

## T50 — Bootstrap scène, asmdef, paysage verrouillé✅

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

### Suivi Git / Notion

- Branche : `T50-bootstrap-scene`
- Commit : `T50: bootstrap scène GameplayLocal, asmdef Presentation, paysage verrouillé`
- Après merge : fermer le ticket GitHub T50 et mettre à jour Notion (statut T50 + choix Screen Space - Camera).

---

## T51 — Adaptateur de partie (GameplayController, UnityHumanAgent)✅

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

---

# T52 — Vue de table (sièges, zones) en paysage✅

**Statut : FAIT** *(merge bloqué par la dépendance T51)*

**Contexte :** afficher les 4 sièges et les zones pioche/pile à partir de `IPlayerVisibleState` uniquement.

**Fichiers :**
- `UI/GameTableView.cs`
- `UI/PlayerSeatView.cs`
- `UI/ZoneView.cs`
- `UI/TableProjection.cs` (projection pure, sans `UnityEngine`)
- `UI/TableViewModels.cs` (`OpponentSeatViewModel`, `SelfSeatViewModel`, `ZoneViewModel`, `TableViewModel`, `SeatPosition`)

**Travail réalisé :**
- Disposition en croix en paysage (validée en 2340×1080) : humain en bas (main pleine largeur), bots à gauche / en haut / à droite, pile au centre.
- Compteurs visibles pour les adversaires (main, FaceUp, FaceDown) : quantités seulement, jamais de cartes.
- Projection état → modèle de vue extraite en classe pure (`TableProjection`).
- Pioche et pile affichées en compteurs (26 = pioche, 1 = pile au départ).

**Dépendances :** T51.

**Critères :**
- Aucun contenu caché d'un adversaire n'est rendu. ✅ `OpponentSeatViewModel` ne contient aucune propriété de type `Card`.
- Rendu conforme en paysage. ✅

**Notes :**
- Le siège adverse affiche le nom du bot. `DiscardBot` est placé aléatoirement sur l'un des trois sièges, son nom peut donc apparaître à gauche, en haut ou à droite.
- Les compteurs de pioche et de pile sont **provisoires** : ils seront remplacés par le rendu des vraies cartes.
- Les compteurs de siège ne sont pas encore étiquetés (main / FaceUp / FaceDown). À traiter avec le rendu des cartes.

**Tests :** unitaires sur le mapping état → compteurs (projection pure, extraite en classe testable). **Reportés en Phase 5** (rattrapage de couverture de l'ensemble du code).

**Merge :** bloqué tant que T51 n'est pas repris et mergé.

---

# T53 — Contrat de skin et skins de base interchangeables✅

**Statut : FAIT — code, assets et validation éditeur OK. MERGE BLOQUÉ par T51**

**Contexte :** plusieurs skins de base sélectionnables ; un skin ne change que l'apparence.

**Dépendances :** T52 (fait, merge bloqué). T51 en pause (couverture Core ≈ 1 %, critère ≥ 90 % non atteint, reprise en Phase 5). Le merge de T52 et T53 reste donc bloqué tant que T51 n'est pas repris et mergé.

## Fichiers (tous sous `Assets/Scripts/Presentation/`)

| Fichier | Chemin | Rôle |
|---|---|---|
| `ICardSkin.cs` | `Interfaces/ICardSkin.cs` | Contrat : `Id`, `DisplayName`, `GetCardSprite(Card)`, `GetCardBackSprite()`, `TableColor`, `SeatColor`, `Font`. Aucune logique. |
| `SkinSet.cs` | `Models/SkinSet.cs` | ScriptableObject (`Create > Psycko > Skin Set`) : `_faces`, `_cardBack`, `_tableColor`, `_seatColor`, `_font`. |
| `CardSkinDefinition.cs` | `Models/CardSkinDefinition.cs` | Classe `[Serializable]` : `_id`, `_displayName`, `_skinSet`. |
| `DefaultCardSkin.cs` | `Skins/DefaultCardSkin.cs` | Implémentation de `ICardSkin` qui lit un `SkinSet`. Valeurs de repli si `SkinSet` nul. |
| `SkinRegistry.cs` | `Skins/SkinRegistry.cs` | MonoBehaviour : liste de `CardSkinDefinition`, `Select(id)`, `Current`, `All`. |
| `GameplayController.cs` | `Controllers/GameplayController.cs` | Modifié : retrait de `_autoAnswerExchanges` et de `OnAutoRequest`, remplacés par `OnHumanRequest` (réponse par défaut explicite « aucun échange »). |

## Assets créés

- `Assets/Presentation/Skins/SkinSet_Classique.asset`
- `Assets/Presentation/Skins/SkinSet_Sombre.asset` (couleurs distinctes de Classique)

## Critères d'acceptation

- [x] Changer de skin ne modifie aucune position ni taille d'élément interactif (structurel : aucun `RectTransform` touché).
- [x] Un nouveau `SkinSet` s'ajoute sans toucher au code (création d'un asset + une entrée dans `Definitions`).
- [x] `_autoAnswerExchanges` n'existe plus dans le code (recherche globale : 0 résultat, `.unity` inclus).
- [x] Partie locale démarre sans blocage : échange pré-partie répondu par défaut, `NormalPlay` atteint, aucune erreur rouge en Play.
- [x] `SkinSet_Sombre` a des couleurs distinctes de `SkinSet_Classique` (validé dans l'Inspector).
- [ ] Identifiant inconnu → skin par défaut : implémenté (`Select` retombe sur le repli). **Vérification reportée en Phase 5** avec les tests.

## Hors périmètre / reporté

- **Tests : reportés en Phase 5** (décision d'Ekinox, rattrapage de couverture globale). Le cas « identifiant inconnu → skin par défaut, pas d'exception » y sera couvert.
- Rendu des vraies cartes sur la table : ticket suivant. `GetCardSprite` renvoie `null` pour l'instant.
- Application de `TableColor` / `SeatColor` à `GameTableView` et `PlayerSeatView` : non branchée dans T53 (à trancher, voir ci-dessous).
- Animations, logique de jeu, validation de coups.

## Points ouverts

1. **TextMeshPro :** `SkinSet` utilise `UnityEngine.Font`. Si l'UI utilise TextMeshPro, remplacer par `TMP_FontAsset`.
2. **Branchement visuel :** décider si `TableColor` et `SeatColor` sont appliqués dans T53 ou dans le ticket de rendu des cartes. Nécessite de relire `GameTableView.cs`.
3. **Sélection d'un skin à l'exécution :** aucune UI n'appelle `SkinRegistry.Select` pour l'instant.
4. **Réponse d'échange par défaut :** « aucun échange » est un comportement provisoire, à remplacer par l'UI d'échange (ticket dédié).

## Notes

- DiscardBot est placé aléatoirement sur un siège adverse : son nom peut apparaître à gauche, en haut ou à droite. Comportement normal.
- La table affiche toujours des compteurs provisoires (pioche, pile).
- Les logs `[Gameplay] ...` viennent de `_verboseDiagnostics = true` (désactivable dans l'Inspector).

## Étapes suivantes

1. **Commit** sur la branche `T53-skins` avec le message : `T53: contrat de skin, SkinSet, SkinRegistry, retrait _autoAnswerExchanges`. **Ne pas merger** (bloqué par T51).
2. **Notion** en priorité après merge : statut T53, décision sur la réponse d'échange par défaut, assets de skin.
3. **Phase 5** : reprise de T51, puis merge de T52 et T53, puis tests de l'identifiant inconnu.

---

# T54 — CardView utilisant le skin✅

**Statut : EN COURS DE VALIDATION** (code et prefab en place ; états visuels et commit restants)

Fichiers : Assets/Scripts/Presentation/UI/CardView.cs, Assets/Scripts/Presentation/UI/CardSpriteKey.cs

Dépendances : T53 (merge bloqué par T51)

## Travail
- Rendu d’une carte à partir de `Card` + `ICardSkin`.
- Mapping `Card` → clé de sprite via la fonction pure `CardSpriteKey` : format `{Suit}_{Rank}` et `Joker_{JokerType}` ; dos via `GetBack()`.
- États visuels : Normal / Playable (surbrillance) / Selected (relevée, décalage visuel sur enfant, hitbox inchangée) / FaceDown.
- Si sprite absent : placeholder explicite (fond uni + libellé) et un seul warning par clé.
- Prefab CardView : 240x336 (ratio 5:7).
- Remplir les 63 entrées `SkinSet._faces` (liste `{Key, Sprite}`) ; `SkinSet` inchangé.

## Décisions
- Clé : format ci-dessus.
- `SkinSet` : liste inchangée.
- `TableColor` / `SeatColor` → T54b.
- Pas de TextMeshPro : `UnityEngine.UI.Text`.

## Règles
- Le skin ne modifie ni tailles de zone ni hitboxes.
- Aucune logique de jeu dans `CardView`.
- Aucun changement de `RectTransform` imposé par le skin.
- Éventail, rotations et chevauchements des mains = `ZoneView` / `PlayerSeatView`, pas `CardView`.

## Critères
- 63 cartes sans sprite manquant (placeholder explicite si absent).
- États distincts et réversibles.

## Avancement (31/10/2026)
- [x] Skins nettoyés et renommés (étape 1).
- [x] Prefab CardView 240x336 en place (étape 2).
- [x] Sprites affichés en scène : faces réelles visibles dans la main humaine.
- [x] `SkinSet._faces` : sprites assignés pour tous les rangs standards et les Jokers Noir et Couleur.
- [ ] Sprites absents, donc **placeholder volontaire** : Cavalier (4), Prêtre (4), Joker de Verre (1) = 9 cartes. Illustrations à créer (hors périmètre du ticket).
- [ ] Vérification des 4 états visuels (Normal / Playable / Selected / FaceDown) et de leur réversibilité.
- [ ] Vérification de la hitbox inchangée en Selected.
- [ ] Vérification du placeholder (un seul warning par clé).
- [ ] Commit sur `T54-card-view`.

## Reporté (décision d'Ekinox)
- Hauteur de `SeatSelf` et Spacing de `HandContainer` : réglés en détail quand une partie complète sera jouable (valeurs actuelles provisoires).
- Éventail et chevauchements définitifs : `PlayerSeatView`.

## Tests (Phase 5, pas maintenant)
- Mapping `Card` → clé, unicité et couverture des 63 cartes.
- Test paramétré NUnit EditMode.

## Hors périmètre
- `TableColor` / `SeatColor` (T54b), TextMeshPro, tweens / animations, éventail des mains, logique de jeu, écriture des tests, choix des illustrations.

---

## T55 — Input humain, surbrillance, relevé, phase d'échange, lisibilité de table ✅

**Statut : EN COURS** (T55-0 mergé et poussé ; T55-1 à T55-7 faits et validés ; T55-8 à T55-12 à faire)

Branche : `T55-human-input` (depuis `main` à jour). Merge bloqué par T51.
Dépendances : T54, T55a (✅ 89/89). T55-9 dépend de T61 (Core : ordre de fin).

Fichiers : Controllers/InputHandler.cs, Controllers/UnityHumanAgent.cs, Models/HumanSelectionState.cs,
Models/HandVisualStateResolver.cs, Models/CountdownTimer.cs, UI/CountdownView.cs, UI/CardView.cs,
UI/PlayerSeatView.cs, UI/ZoneView.cs, UI/GameTableView.cs, Scenes/GameplayLocal.unity

### Contexte
- `InputHandler.cs` existe déjà sur `main` et couvre l'essentiel : T55 = audit, découpage, correction,
  puis ajouts de lisibilité.
- La Présentation propose des intentions brutes ; `GameOrchestrator` valide. Aucun `if` de règle
  dans Controllers/ ni UI/.

### Règles confirmées par Ekinox (04/11/2026)
- Jokers : toujours seuls. Le Core le garantit déjà (levée d'`ArgumentException`
  « Un Joker se joue toujours seul : mélange Joker/carte standard interdit », vérifié en console pour
  Bombe+carte, Bombe+Noir, Noir+carte, Verre+Bombe, Verre+carte). Pas de `Play.CanCombine` (T55-2b abandonné).
- Sélection : clic carte de même rang → s'ajoute et se lève. Clic autre rang → les cartes levées
  redescendent, la nouvelle se lève seule. Joker → remplace tout et reste seul.
- Phase d'échange : 90 s (champ Inspector `_exchangeSeconds`, défaut 90, 0 = illimité, Ekinox le règle
  indépendamment de `_giftSeconds`). Échanges main ↔ FaceUp **infinis** (fonctionnel). « Prêt ! »
  termine immédiatement. À l'expiration, les échanges non validés sont interrompus (abandonnés).
- Don du 7 : sélectionner une carte, puis cliquer sur un siège = soumission immédiate (pas de bouton
  Valider, pas d'Annuler). Un siège Finished ne réagit pas au clic. Minuteur 30 s
  (champ Inspector `_giftSeconds`, défaut 30, 0 = illimité).
- Bannière « #1 » / « #2 » sur le siège du 1er / 2e joueur qui finit. Pas de « #3 » ni « Psycko ».
- Adversaires : on affiche TOUS les dos (main et FaceDown), spacing réduit si trop de cartes pour
  rester dans la zone du siège.
- Pile : décalage/rotation naturels. Joker de Verre : la carte dessous reste visible.
- Main humaine : triée par rang ; jamais hors écran (spacing réduit).
- Tests : reportés en Phase 5 (conception testable, C# pur isolé).

### Travail

#### T55-0 — Dos de carte ✅ FAIT, mergé, poussé.

#### T55-1 — Conception et branche ✅ FAIT (validé 04/11/2026).

#### T55-2 — HumanSelectionState (C# pur, Models/) ✅
- Garder `Toggle / Remove / Clear / Prune` ; pas d'`Add`.
- `Toggle` : même rang → ajoute ; autre rang → `Clear` puis ajoute ; Joker → `Clear` puis ajoute ;
  déjà sélectionnée → retire.
- Ajouter `event Action Changed`, levé une fois par modification effective.
- Aucun `using UnityEngine`. Aucune règle Core dupliquée au-delà de « même rang » (affichage).

#### T55-3 — Surbrillance ✅

**Objectif** : afficher l'état visuel de chaque carte de la main (normal, jouable, sélectionnée) à partir de la logique, sans règle recopiée dans la Présentation.

**Implémentation**
- `Models/CardStateProjector.cs` (C# pur) :
  `CardProjection[] Project(IReadOnlyList<Card> cards, Func<Card,bool> isSelected, Func<Card,bool> isPlayable, bool showPlayable)`.
  Retourne `Selected`, `Playable` ou `Normal` par carte. `Playable` n'est produit que si `showPlayable` est vrai.
- `InputHandler.ReapplyCardStates()` : point unique de projection. Elle boucle sur la main, calcule les états via `CardStateProjector`, les convertit avec `ToVisual` (`CardProjection` → `CardVisualState`) et appelle `CardView.SetState`.
- Surbrillance `Playable` uniquement en pose normale (`NormalPlay`), jamais en don ni en échange.
- Jouabilité : `ICardPlayabilityChecker.IsPlayable(card, visibleState)` (Core). Aucune règle dupliquée.
- Pendant l'échange, la carte considérée est la carte simulée affichée (par position), pas `view.Card`.

**Moments de recalcul**
`ReapplyCardStates` est appelé sur : `GameTableView.Rendered`, changement de `HumanSelectionState` (`Changed`), `GameplayController.StateChanged` et `OnRequest`. Le recalcul sur changement de sélection est volontaire : la sélection doit se refléter immédiatement.

**CardView**
- `_highlight` : Image enfant `Highlight`, ancres stretch/stretch (Min 0/0, Max 1/1), offsets Left/Top/Right/Bottom = −1/−1/−1/−1 (débord visuel d'1 px), Raycast Target décoché, Source Image vide (aplat de couleur).
- Activation pilotée par `Image.enabled` dans `CardView.Refresh()` (actif si l'état est `Playable` ou `Selected`), et non par le GameObject.

**Critères d'acceptation**
- [x] Les cartes jouables sont surlignées en pose normale uniquement.
- [x] La sélection est surlignée et suit exactement `HumanSelectionState`.
- [x] Aucune règle de jeu dans la Présentation (jouabilité déléguée au Core).
- [x] Configuration du prefab `Highlight` vérifiée dans l'Inspector.
- [x] Test manuel dans Unity validé.

**Hors périmètre**
Pas de tests NUnit ici (prévus en Phase 5).

---

#### T55-4 — Sélection, relevé, boutons ✅
(correctif du bug « cartes de même rang qui restent levées »)

**Objectif** : le relevé visuel suit exactement la sélection logique, et les boutons reflètent le Core.

**Implémentation**
- Source unique de la sélection : `HumanSelectionState`. `CardView` ne décide rien, il applique l'état reçu.
- Relevé : `CardView._selectedLift = 24f`, appliqué dans `Refresh` sur `_visualRoot.anchoredPosition.y` uniquement si l'état est `Selected`. La hitbox (racine + Button) reste immobile. `_visualRoot` doit être assigné dans le prefab.
- Projection : `InputHandler.ReapplyCardStates()` recalcule l'état de chaque carte via `CardStateProjector` après tout changement de sélection, de requête, de rendu ou d'état. La sélection est élaguée (`Prune`) aux cartes encore en main et jouables.
- `Card` est un `sealed record` : `Equals` / `==` comparent par valeur, donc `Contains`, `Prune` et les comparaisons de cartes sont fiables (point vérifié).
- Boutons (`RefreshButtons`) :
  - `Jouer` : visible en `NormalPlay`, interactable ssi sélection non vide et `EverySelectedCardPlayable()` (délégué à `ICardPlayabilityChecker`).
  - `Ramasser` : visible et interactable en `NormalPlay` et `Pickup`.
- Rejet par le Core : carte non jouable → message « Coup refusé », sélection conservée. `Play.Create` lève `ArgumentException` → message « Sélection invalide », sélection conservée.
- Succès : `_selection.Clear()` puis `HideActions()`.
- Échange prépartie : une seule carte de main levée à la fois (re-clic = baisse, autre carte = déplacement).

**Critères d'acceptation**
- [x] Les cartes sélectionnées de même rang restent levées ensemble.
- [x] Après Jouer, plus aucune carte ne reste levée.
- [x] Aucune règle de jeu dans `Controllers/` ou `UI/` (jouabilité via `ICardPlayabilityChecker`).
- [x] `Card.Equals` vérifié (record, égalité par valeur).
- [x] Test manuel dans Unity validé.

**Dette technique optionnelle (non bloquante)**
- Factoriser la fin de requête dans une méthode unique `InputHandler.CompleteRequest()` (`_request = None; _submitted = true; _selection.Clear(); _giftCard = null; _pendingExchangeHand = null; HideActions();`), appelée par tous les chemins de soumission, avec un `Clear` commun au début de `OnRequest`. Aujourd'hui chaque chemin nettoie à sa façon. Non fait car le comportement observé est correct.

**Hors périmètre**
Pas de tests NUnit ici (prévus en Phase 5). La projection est pure (`CardStateProjector`), donc testable en EditMode à ce moment-là.

#### T55-5 — FaceDown aveugle ✅

**Objectif.** Les FaceDown du joueur humain se jouent à l'aveugle : la Présentation ne connaît, ne reçoit et n'affiche jamais la `Card` correspondante. Elle ne transmet qu'une position, le Core résout la carte.

**Contrat**
- `PlayerSeatView.FaceDownIndexClicked : event Action<int>`, émis uniquement pour un index valide dans la liste des vues FaceDown (`_faceDownViews.IndexOf(view) >= 0`).
- `InputHandler.OnFaceDownIndex(int)` : garde-fous avant envoi, dans cet ordre :
  - `_request == HumanRequestKind.FaceDownIndex` (le Core a demandé un FaceDown) ;
  - `_blind != null`, `_controller.Human != null`, `!_submitted` ;
  - `0 <= index < IFaceDownBlindView.SelfFaceDownCount`.
  Puis `_submitted = true`, `_controller.Human.SubmitFaceDownIndex(index)`, `HideActions()`.
- Cliquabilité : `ReapplyCardStates` n'active `SetInteractable` sur les FaceDownViews que si `_request == HumanRequestKind.FaceDownIndex`. Aucune règle de jeu (main vide, etc.) recopiée en Présentation : c'est la requête du Core qui fait foi.
- Aucun impact sur `HumanSelectionState` : le clic FaceDown ne la modifie pas.

**Aveuglement des données (modification de ce ticket)**
- `SelfSeatViewModel` : `IReadOnlyList<Card> FaceDown` remplacé par `int FaceDownCount` (borné à ≥ 0).
- `TableProjection.Build` : passe `state.SelfFaceDown.Count` (0 si null), jamais la liste.
- `PlayerSeatView.RenderSelf` : lit `model.FaceDownCount`. Les slots FaceDown sont rendus par `Bind(null, skin)` + `CardVisualState.FaceDown` (dos de carte uniquement).
- Adversaires : déjà aveugles (`OpponentSeatViewModel.FaceDownCount`, aucune `Card` FaceDown).

**Fichiers modifiés**
- `Presentation/UI/TableViewModel.cs`
- `Presentation/UI/TableProjection.cs`
- `Presentation/UI/PlayerSeatView.cs`
- `Tests/EditMode/Presentation/TableProjectionTests.cs` (adaptation à la nouvelle API + 2 tests ajoutés)

**Critères d'acceptation**
- [x] `IFaceDownBlindView.SelfFaceDownCount` borne l'index (`InputHandler.OnFaceDownIndex`).
- [x] Aucune `Card` FaceDown dans les ViewModels (verrouillé par réflexion : `SelfViewModel_DoesNotExposeFaceDownCards`, `OpponentViewModel_ExposesOnlyPublicFaceUpCards`).
- [x] Clic FaceDown sans effet hors requête `FaceDownIndex`.
- [x] Compilation Unity propre, **91/91 tests EditMode verts**.
- [x] Partie testée manuellement : aucune régression visible (changement interne, rendu identique).

**Hors périmètre (dette optionnelle, inchangée)**
- Factorisation `CompleteRequest()`.

**Dépendances.** T54, T55a, T55-3, T55-4. Merge toujours bloqué par T51.

#### T55-6 — Don du 7 ✅

**Statut** : terminé et validé (91/91 tests EditMode verts, partie testée).

**Comportement**
- Clic carte → `_giftCard` (re-clic = désélection) ; clic siège → soumission immédiate
  (`SeatClicked(int playerId)`), uniquement si une carte est choisie.
- Siège Finished : bouton `interactable = false` d'après `IOpponentVisibleInfo.CurrentPhase`
  (lecture d'état, pas de règle). Clic sans effet (garde aussi dans `ChooseGiftRecipient`).
- Minuteur `_giftSeconds` (Inspector, défaut 30, 0 = illimité) porté par `CountdownTimer`
  (logique pure) et affiché par `CountdownView` (police, format, couleurs, jauge réglables
  dans l'Inspector). Pas d'`ISwapPhaseClock` : abandonné, remplacé par `CountdownTimer`.
- Le don automatique ne se déclenche QUE si le joueur n'a rien soumis dans les N secondes.
  Don manuel avant la fin : minuteur arrêté, aucun Fallback.
- Expiration → `UnityHumanAgent.ExpirePending()` → Fallback Gift (décidé dans l'agent,
  jamais dans la Présentation) : carte non-Joker la plus faible (clé rang x 4 + couleur,
  comme `FirstPlayerResolver`), donnée à un adversaire non Finished tiré au hasard.
  Joker tiré au hasard uniquement s'il n'y a aucune autre carte.
- Minuteur arrêté à la soumission, au changement de requête et dans `OnDisable`.

**Fichiers**
- [x] Nouveau : `Assets/Scripts/Presentation/Models/CountdownTimer.cs`
- [x] Nouveau : `Assets/Scripts/Presentation/UI/CountdownView.cs`
- [x] Modifié : `Assets/Scripts/Presentation/Controllers/UnityHumanAgent.cs`
- [x] Modifié : `Assets/Scripts/Presentation/Controllers/InputHandler.cs`
- [x] Inspector : `_giftCountdownView` assigné ; `_giftSeconds` = 30.

**Critères d'acceptation**
- [x] Clic carte en don : Selected, re-clic désélectionne, pas de Playable.
- [x] Clic siège avec carte : soumission immédiate ; sans carte : aucun effet.
- [x] Siège Finished non cliquable, clic sans effet.
- [x] Compte à rebours affiché, `_giftSeconds` = 0 : illimité, rien d'affiché.
- [x] Expiration : carte la plus faible vers un adversaire non Finished aléatoire.
- [x] Don manuel avant la fin : aucun Fallback.
- [x] Minuteur arrêté à la soumission, au changement de requête, à la fin de partie.
- [x] Re-rendu pendant le don : sélection et sièges cohérents.
- [x] FaceDown aveugle inchangé.
- [x] Aucune règle de jeu dans `Presentation/`.
- [x] Tests EditMode : 91/91 verts (dont `UnityHumanAgentTests`).

**Hors périmètre (reporté)**
- Bandeau d'effet de carte.
- Factorisation `CompleteRequest()`.
- Tests NUnit complémentaires de la couche Presentation (Phase 5).
- (La migration du minuteur d'échange est faite à T55-7.)

**Dépendances** : T54, T55a. Merge bloqué par T51.
**Suite** : T55-8.

#### T55-7 — Phase d'échange ✅

**Statut** : terminé et validé en partie jouée. Tests NUnit reportés en Phase 5.

**Comportement**
- Le minuteur d'échange réutilise `CountdownTimer` (logique pure) et `CountdownView` (affichage), comme
  le don du 7. `ISwapPhaseClock` / `SwapPhaseClock` ne sont pas créés (abandonnés, cf. T55-6).
- Deux minuteurs indépendants, chacun avec sa durée réglable dans l'Inspector :
  `_giftSeconds` (30) pour le don, `_exchangeSeconds` (90) pour l'échange. 0 = illimité (aucun
  décompte affiché, seul « Prêt ! » termine l'échange).
- `Update` : `_exchangeTimer.Tick(Time.deltaTime)` tant que la requête est `Exchanges` et non soumise ;
  affichage via `_exchangeCountdownView.Show(Remaining, Total)`.
- « Prêt ! » avant la fin : minuteur arrêté et masqué, `SubmitExchanges` envoie les échanges réalisés,
  aucun Fallback.
- Expiration : les échanges non validés sont interrompus (abandonnés). Le Fallback est décidé dans
  `UnityHumanAgent.ExpirePending()`, jamais dans la Présentation.
- Minuteur arrêté et masqué à la soumission, au changement de requête et dans `OnDisable`.
- `HumanSelectionState.Clear` en début de requête d'échange.
- `_timerLabel` supprimé de `InputHandler` (plus de gestion manuelle du `Text` dans `RefreshButtons`
  ni de `_exchangeRemaining`).
- Format d'affichage : `{0}` (sans « s »), réglable dans le champ `Format` de chaque `CountdownView`.

**Fichiers**
- [x] Modifié : `Assets/Scripts/Presentation/Controllers/InputHandler.cs` (champ
  `_exchangeCountdownView`, `_exchangeTimer`, `OnExchangeTimerExpired`, suppression de `_timerLabel`
  et `_exchangeRemaining`).
- [x] Modifié si nécessaire : `Assets/Scripts/Presentation/Controllers/UnityHumanAgent.cs`
  (`ExpirePending` couvre `HumanRequestKind.Exchanges`).
- [x] Aucun nouveau script (réutilisation de `CountdownTimer` et `CountdownView`).

**Scène (GameplayLocal.unity)**
- [x] `ExchangeCountdown` (objet vide) avec composant `CountdownView` : ancre top-center
  (Min/Max 0.5/1), pivot (0.5, 1), RectTransform 200×60, pos (0, −40).
- [x] `TimerText` (Text) déplacé en enfant de `ExchangeCountdown` : ancres stretch, offsets 0,
  Font Size 48, alignement centre/milieu, couleur blanche, overflow Overflow.
- [x] `CountdownView` : `_label` = `TimerText`, `_fill` = None, `_format` = `{0}`, couleurs
  blanc/rouge, `_warningThreshold` = 5.
- [x] `InputHandler` : `_exchangeCountdownView` = `ExchangeCountdown`, `_exchangeSeconds` = 90.
- [x] `GiftCountdown` : `_format` = `{0}`.

**Critères d'acceptation**
- [x] Décompte de 90 s affiché en haut au centre pendant l'échange, rouge à 5 s.
- [x] « Prêt ! » avant la fin : décompte masqué, échanges envoyés, aucun Fallback.
- [x] Expiration : décompte masqué, échanges non validés interrompus, partie lancée.
- [x] `_exchangeSeconds` = 0 : illimité, aucun décompte affiché.
- [x] `_giftSeconds` et `_exchangeSeconds` réglables indépendamment.
- [x] Minuteur arrêté à la soumission, au changement de requête et dans `OnDisable`.
- [x] Non-régression : don du 7, FaceDown aveugle, sélection, surbrillance.
- [x] Aucune règle de jeu dans `Presentation/`.
- [x] Partie testée manuellement.

**Hors périmètre**
- Tests NUnit de `CountdownTimer` et du comportement d'échange (Phase 5). `CountdownTimer` est pur
  et testable en EditMode à ce moment-là.
- Factorisation `CompleteRequest()`.

**Dépendances** : T54, T55a, T55-6 (`CountdownTimer`, `CountdownView`). Merge bloqué par T51.
**Suite** : T55-8.

#### T55-8 — Dos des cartes adverses ✅

**Statut** : terminé et validé en partie jouée à 4 joueurs. De petits ajustements visuels restent possibles plus tard. Tests NUnit de `HandSpacing` reportés en Phase 5 (calcul pur, testable en EditMode).

**Objectif** : afficher les dos des cartes adverses (main et FaceDown) à partir des compteurs uniquement, sans jamais sortir de la zone du siège.

**Comportement**
- `PlayerSeatView.RenderOpponent` affiche `HandCount` dos pour la main. Il affiche aussi `FaceDownCount` dos sous les FaceUp : la FaceUp i recouvre le dos i.
- Les dos sont alignés droit sur une ligne, sans éventail et sans rotation locale. Sur les sièges latéraux, c'est le conteneur qui est tourné de ±90° : la ligne apparaît donc verticale à l'écran.
- Les dos de main adverse sont à l'échelle `_handBackScale` (**0,75** par défaut). Les FaceUp et FaceDown adverses gardent leur taille.
- La disposition est calculée par `HandSpacing.Compute` (C# pur, avec un argument `maxVisible`) :
  - spacing = `Min(_defaultSpacing, (_zoneWidth − _cardWidth) / (n − 1))`, plancher 12 px ;
  - les rangées sont centrées sur le milieu du conteneur.
- **Capacité** : on n'affiche que `VisibleCount` dos au maximum, plafonné par `maxVisible`, et jamais hors de la zone. Le label du nombre réel (`HandCount`) n'apparaît que si la main dépasse le nombre de dos affichés.
- **Cas limites** :
  - n = 0 : rien n'est affiché ;
  - n = 1 : pas de calcul de spacing (pas de division par zéro) ;
  - `_zoneWidth <= _cardWidth` : un seul dos.
- Les FaceUp adverses sont placées par le code (`RenderFaceRow`), alignées sur les FaceDown qu'elles recouvrent.
- **Source** : `IOpponentVisibleInfo.HandCount` et `FaceDownCount` uniquement. Aucune `Card` cachée n'est lue ni stockée.
- **Dos** : `CardView.Bind(null, skin)` + `CardVisualState.FaceDown`, non cliquables.
- **Layout Groups supprimés** sur tous les conteneurs adverses (main, FaceUp, FaceDown) des 3 sièges : les positions sont entièrement pilotées par le script.

**Fichiers**
- [x] Nouveau : `Assets/Scripts/Presentation/Models/HandSpacing.cs`
- [x] Modifié : `Assets/Scripts/Presentation/UI/PlayerSeatView.cs`
- [x] Scène : conteneurs adverses des 3 sièges sans Layout Group. Champs assignés : `_defaultSpacing`, `_zoneWidth`, `_cardWidth`, `_handBackScale`, `_tableRotationZ`, et le nombre maximal de dos visibles.

**Réglages Inspector de référence (SeatLeft)**

| Objet | Pos X | Pos Y | Width | Height | Rot Z |
|---|---|---|---|---|---|
| `HandContainerL` | 70 | 0 | 400 | 140 | -90 |
| `FaceUpContainerL` | 260 | 40 | 400 | 140 | -90 |
| `FaceDownContainerL` | 280 | 60 | 400 | 140 | -90 |

- `PlayerSeatView` : Opponent Backs Vertical décoché, Table Rotation Z 0.
- Main : Hand Back Scale 0,75, spacing par défaut 50, zone 400, 7 dos visibles au maximum.
- FaceDown : spacing par défaut 150, zone 400.
- SeatRight est réglé en miroir. Les valeurs finales des 3 sièges sont à reporter dans Notion.

**Critères d'acceptation**
- [x] Main adverse : autant de dos que `HandCount`, jusqu'à la capacité de la zone.
- [x] Au-delà de la capacité : nombre maximal de dos affiché, et label du nombre réel visible.
- [x] Aucun dos hors de la zone du siège, y compris avec une main de 15 cartes ou plus.
- [x] FaceDown adverses affichés sous les FaceUp, à leur taille actuelle.
- [x] Dos droits, sans rotation locale ; rangées verticales sur les sièges latéraux.
- [x] n = 0, n = 1 et `_zoneWidth <= _cardWidth` sans erreur.
- [x] Aucune `Card` cachée lue ni stockée.
- [x] Pas de régression sur le don du 7, l'échange, les FaceDown aveugles, la sélection et la surbrillance.
- [x] Aucune règle de jeu dans `Presentation/`.
- [x] Compilation propre, 91/91 tests EditMode verts, partie testée à 4 joueurs avec de grandes et de petites mains.

**Hors périmètre (reporté)**
- Tests NUnit de `HandSpacing` (Phase 5).
- Spacing adaptatif de la main humaine (T55-11, qui pourra réutiliser `HandSpacing`).
- Décalage en diagonale des FaceUp pour laisser voir les FaceDown (offset `Vector2` dans `PlaceInRow`).
- Factorisation de `CompleteRequest()`.

**Dépendances** : T54, T55a, T55-5. Merge bloqué par T51.
**Suite** : T55-9.

#### T55-9 — Bannière de classement #1 / #2 / #3 / Psycko ✅

**Statut** : terminé et validé en partie jouée à 4 joueurs. Habillage graphique et animation reportés en Phase 5 (UI finale). Tests NUnit de la projection du rang et de `FormatRank` reportés en Phase 6.

**Objectif** : afficher le rang d'arrivée sur le siège de chaque joueur (« #1 », « #2 », « #3 », « Psycko » pour le dernier), à partir du rang exposé par le Core (T61), sans aucun calcul de rang côté Présentation.

**Comportement**
- `PlayerSeatView._rankBanner` affiche « #1 », « #2 », « #3 » ou « Psycko » (rang 4). Il est masqué quand le rang est absent (`null`).
- La bannière apparaît dès que le joueur termine, sans attendre la fin de partie. Elle vaut aussi pour le siège humain. Le rang « Psycko » apparaît dès que le 3e joueur sort.
- **Source** : `IOpponentVisibleInfo.FinishRank` (adversaires) et `IPlayerVisibleState.SelfFinishRank` (soi), tous deux `int?`, exposés par T61. Phase d'échange : rang `null`, donc aucune bannière.
- Le rang du Psycko est **calculé par le Core** (T61, valeur dérivée dans `GetFinishRank`). La Présentation ne fait que le recopier.
- `OpponentSeatViewModel` et `SelfSeatViewModel` reçoivent une propriété `FinishRank` (`int?`), en paramètre optionnel de fin de constructeur (rétrocompatible).
- `TableProjection.Build` recopie `SelfFinishRank` et `o.FinishRank` dans les ViewModels.
- `PlayerSeatView.RenderSelf` et `RenderOpponent` appellent `UpdateRankBanner(model.FinishRank)`, qui renseigne le texte et active/désactive l'objet.
- `FormatRank(int?)` : mapping d'affichage pur (`1→"#1"`, `2→"#2"`, `3→"#3"`, `4→"Psycko"`, sinon chaîne vide). `internal static` pour être testable en EditMode (nécessite `InternalsVisibleTo` vers l'assembly de tests, sinon passer en `public static`).
- La bannière a **Raycast Target décoché** : elle ne bloque pas le clic du siège.

**Fichiers**
- [x] Modifié : `Assets/Scripts/Presentation/UI/TableViewModels.cs`
- [x] Modifié : `Assets/Scripts/Presentation/UI/TableProjection.cs`
- [x] Modifié : `Assets/Scripts/Presentation/UI/PlayerSeatView.cs`
- [x] Scène : enfant `RankBanner` sur les 4 sièges, champ `_rankBanner` assigné, largeur élargie à 120

`GameTableView.cs` : inchangé. Core : aucune modification dans ce ticket (le rang du Psycko relève de T61).

**Réglages Inspector de référence (RankBanner, 4 sièges)**

| Objet | Anchor | Pivot | Pos X | Pos Y | Width | Height |
|---|---|---|---|---|---|---|
| `RankBanner` | top-right (1, 1) | (1, 1) | 0 | 0 | 120 | 32 |

- `Text` : Font Size 48, alignement centre/centre, Horizontal et Vertical Overflow = Overflow, Raycast Target décoché.
- Sièges Left/Right : bannière lisible et non couverte par les cartes.
- La largeur passe de 56 à 120 pour contenir « Psycko ». Overflow conservé en secours.

**Critères d'acceptation**
- [x] Le 1er à finir affiche « #1 », le 2e « #2 », le 3e « #3 », le dernier « Psycko ».
- [x] Même comportement sur le siège humain (`SelfFinishRank`).
- [x] Aucune bannière en phase d'échange ni en début de partie.
- [x] Bannière visible dès la sortie du joueur, en cours de partie.
- [x] Le clic sur un siège fonctionne toujours.
- [x] Aucune `Card` cachée lue ni stockée.
- [x] Aucune règle de jeu dans `Presentation/` (le rang du Psycko vient du Core).
- [x] Compilation propre, tests EditMode existants verts, partie testée à 4 joueurs.

**Hors périmètre (reporté)**
- Habillage graphique et animation de la bannière (Phase 5, UI finale).
- Tests NUnit de la projection du rang et de `FormatRank` (Phase 6).
- Affichage adapté aux parties à moins de 4 joueurs : `FormatRank` ne gère que les rangs 1 à 4. À 3 joueurs, le dernier afficherait « #3 ». Sans objet tant que le jeu reste à 4 joueurs.
- Gestion de l'écran de fin de partie recouvrant la table (visibilité de « Psycko » à cet instant) : à traiter en Phase 5.

**Dépendances** : T61 (dont rang du Psycko), T55-8.

---

#### T55-10 — Pile désalignée + cartes visibles sous le dessus✅

**Statut :** ✅ FAIT (validé localement à 4 joueurs). Branche : `T55-10-pile-offset`.

**Objectif :**
Donner à la pile centrale un rendu « posé à la main » : cartes éparpillées de façon irrégulière, avec le plus de cartes possibles visibles, afin que la carte précédente reste lisible sous le Joker de Verre (transparent).

**Comportement :**
- `ZoneView` affiche les N dernières cartes de la pile via un **pool** d'instances `CardView` (N = `Max Visible Pile Cards`).
- Décalage **déterministe** par index de carte dans la pile (hash de l'index, ni `UnityEngine.Random` ni `System.Random`) : rendu identique à chaque rafraîchissement, aucun scintillement. Le décalage dépend de l'index, pas de l'identité de la carte.
- Position : point dans un **disque** de rayon `Scatter Radius` (et non un carré, pour éviter un effet de grille).
- Rotation : jusqu'à ±180° pour les cartes du dessous, ±`Top Max Rotation` (15°) pour la carte du dessus, qui reste lisible.
- Ordre d'affichage (sibling index) : le dessus est rendu en dernier, chaque carte du dessous est rendue sous la suivante.
- `ZoneViewModel` expose les cartes visibles de la pile, projetées par `TableProjection` depuis `Pile.Cards` (données publiques uniquement, aucune carte cachée lue ou stockée).
- **Aucune détection du Joker de Verre dans `Presentation/`** : sa transparence est portée par le sprite. La carte sous lui reste visible par simple empilement.
- Pile d'une seule carte : une seule carte rendue, aucune erreur. Pile vide : aucune carte, `EmptyPileMarker` actif.
- Les cartes de pile ne sont pas interactives (`SetInteractable(false)`) et ne bloquent aucun clic.

**Fichiers :**
- `Assets/Scripts/Presentation/UI/ZoneView.cs` (pool, décalage déterministe, rotation, sibling index)
- `Assets/Scripts/Presentation/UI/TableViewModels.cs` (`ZoneViewModel` : cartes visibles de la pile)
- `Assets/Scripts/Presentation/UI/TableProjection.cs` (projection des dernières cartes publiques)
- Aucune modification du Core ni de `Psycko.Bots/`.

**Réglages (Inspector / import) :**
- `ZoneView` : `Pile Card Prefab` = `PileCard` (prefab `CardView`) ; `Pile Cards Root` = `PileCardsRoot` (RectTransform, ancré au centre, pivot 0.5 / 0.5) ; `Max Visible Pile Cards` = 8 ; `Scatter Radius` = 16 ; `Top Max Rotation` = 15 ; `Below Max Rotation` = 180.
- `PileCard` : Raycast Target décoché sur les images.
- Sprite `Joker_Glass` : `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Alpha Is Transparency` coché.
- Asset `SkinSet` (skin `classique`) : liste `Faces` avec une entrée `Key = Joker_Glass` et le champ `Sprite` assigné. La clé suit le format `Joker_{JokerType}` (`DefaultCardSkin.KeyOf`).

**Correctif découvert en validation :**
Le Joker de Verre s'affichait en placeholder gris « Joker Glass ». Cause : l'entrée `Joker_Glass` du `SkinSet` n'avait pas de sprite utilisable (warning `[CardView] Sprite absent pour la clé 'Joker_Glass'`). Les scripts de mapping (`CardSpriteKey`, `DefaultCardSkin`) étaient corrects. Correction : assignation du sprite dans la liste `Faces`. Aucun changement de code.

**Critères d'acceptation :**
- [x] Compilation propre.
- [x] Tests EditMode existants verts (aucun test nouveau : tests de la projection reportés en Phase 6).
- [x] Partie à 4 joueurs : pile éparpillée, stable d'un frame à l'autre, sans scintillement.
- [x] Plusieurs cartes de la pile visibles (jusqu'à `Max Visible Pile Cards`).
- [x] Carte précédente visible sous le Joker de Verre, sprite transparent affiché correctement.
- [x] Aucun clic bloqué par la pile.
- [x] Aucun `Random` utilisé, aucune règle de jeu dans `Presentation/`.

**Hors périmètre :**
- Tests NUnit de la projection (Phase 6).
- Nouveaux sprites de cartes (travail d'illustration d'Ekinox, à part).
- Animations de pose et de ramassage de la pile.
- Outil Editor de vérification des clés manquantes d'un `SkinSet` (ticket séparé possible).

**Dépendances :** T61 (Core, ordre de fin), T55-9 (bannière de classement).

# T55-11 ✅ — Main triée, contenue et regroupée par rang

**Branche** : `feature/T55-11-main-triee-groupee`
**Phase** : 5 — Présentation
**Couches touchées** : Presentation uniquement (aucun changement Core / Bots)
**Fichiers** : `PlayerSeatView.cs`, `CardView.cs`, `InputHandler.cs` (patch minimal)

## 1. Objectif

La main humaine (jusqu'à 25 cartes) est triée, ne déborde jamais de la zone,
et se regroupe par rang quand la place manque. Un clic sur un groupe ouvre un
éventail au-dessus, où le joueur choisit de 1 à N cartes du rang.

## 2. Contraintes

- Aucune logique de jeu en Présentation.
- Aucun `Image` raycastable ajouté à `_handContainer`.
- `_handViews` conserve l'ordre du Core (inchangé), car `SimCardAt`,
  `ApplyExchangePreview` et `DisplayedHandCard` s'appuient sur l'indexation.
- Le tri est purement visuel (positions et ordre des siblings).
- Aucune allocation de `CardView` ni de prefab nouveau. Les listes de colonnes
  sont recyclées via un pool (`RentColumn` / `ReleaseColumns`).

## 3. Tri

`HandSorter.Sort` est appliqué à la main humaine (rang puis couleur) pour
calculer la position d'affichage de chaque `CardView`. `_handViews` n'est jamais
réordonné.

Sens d'affichage : `_selfHandDescending` (Inspector, ou `SetSelfHandDescending(bool)`).
Faux = petits rangs à gauche ; vrai = grands rangs à gauche. Seules les positions
sont inversées : le calcul des colonnes et le regroupement restent en ordre croissant.

## 4. Contenance

Pas = `Min(_selfHandSpacing, (_selfHandZoneLength − _cardWidth) / (n − 1))`,
où n = nombre de colonnes. Pour `n ≤ 1`, pas de calcul de pas.
Si, après regroupement maximal, le pas reste sous `_selfHandMinSpacing`,
il est compressé sous le plancher (chevauchement), jamais de débordement.

| Champ (Inspector) | Valeur actuelle |
|---|---|
| `_cardWidth` | 120 |
| `_cardHeight` | 196 |
| `_selfHandSpacing` | 90 |
| `_selfHandZoneLength` | 1500 |
| `_selfHandMinSpacing` | 60 |
| `_fanLift` | 220 |
| `_fanOverlap` | 0.6 (fraction de `_cardWidth`, soit 72 px) |
| `_selfHandDescending` | false |

`_selfHandZoneLength` est volontairement proche de la largeur d'écran moins
la place réservée aux futurs boutons et au chat. Valeur à ajuster plus tard.

## 5. Regroupement

5.1 Le regroupement n'intervient que si, au plancher `_selfHandMinSpacing`,
la main ne tient pas (`Fits`).

5.2 Les rangs se regroupent progressivement, du plus petit au plus grand,
jusqu'à ce que la main tienne. Chaque pas regroupe un rang **entier** :
un rang n'est jamais coupé, quitte à regrouper une carte de plus que le strict
nécessaire. Un rang à une seule carte n'est jamais groupé.
Les Jokers et cartes sans rang ne sont jamais regroupés.

5.3 Un groupe replié affiche une carte représentante (la **dernière** carte
de la colonne dans l'ordre trié) avec un badge `×N`, ancré bas-gauche.

5.4 Les autres cartes du groupe restent des `CardView` vivants de `_handViews`,
masqués via `SetActive(false)`. Ils sont réactivés à l'ouverture de l'éventail
ou au rendu suivant.

5.5 Le badge `×N` (`SetCountBadge(int)`) est affiché sur la représentante
d'un groupe fermé ; `SetCountBadge(0)` le masque.

## 6. Éventail du groupe ouvert

6.1 Un clic sur la représentante d'un groupe replié ouvre l'éventail au-dessus
(`_fanLift`), avec les N cartes du rang espacées de `_cardWidth × _fanOverlap`,
centré sur la colonne et borné à la zone de main, dans le même sens que la main.
Les `CardView` masqués sont réutilisés, aucune instance nouvelle.

6.2 Un clic sur une carte de l'éventail émet `HandCardClicked(card, view)`
normalement. `InputHandler` appelle `HumanSelectionState.Toggle` sans
changement. Choix libre de 1 à N cartes, sans seuil.

6.3 Les cartes sélectionnées se lèvent (état `Selected` existant) et restent
levées, y compris quand on clique d'autres cartes du même rang.

6.4 Un clic sur une carte d'un autre rang efface la sélection précédente
(règle existante) ; les cartes se baissent. L'éventail s'ouvre sur le nouveau
groupe si c'en est un, sinon se ferme.

6.5 L'éventail reste ouvert après chaque clic de carte.

6.6 Le groupe ouvert est mémorisé par sa carte clé (`_openKey`) et retrouvé
à chaque rendu ; il se ferme s'il n'existe plus ou ne compte plus qu'une carte.
Fermeture aussi sur requête différente de `NormalPlay`, soumission d'une pose
ou clic sur un autre rang.

## 7. Modes spéciaux

7.1 **Échange** (`Exchanges`) : regroupement désactivé, tri visuel conservé,
simulation locale inchangée.

7.2 **Don** (`Gift`) : regroupement désactivé, un clic sélectionne une carte unique.

7.3 `InputHandler` appelle `SetGroupingAllowed(bool)` sur `PlayerSeatView`
à chaque changement de requête. Le retour en `NormalPlay` réactive le regroupement.

## 8. Fichiers et changements

| Fichier | Changement |
|---|---|
| `PlayerSeatView.cs` | tri visuel, sens d'affichage, calcul du pas, regroupement par rang entier, éventail, `SetGroupingAllowed`, `SetSelfHandDescending` |
| `CardView.cs` | `_countBadge`, `SetCountBadge(int)` |
| `InputHandler.cs` | appels à `SetGroupingAllowed` et fermeture de l'éventail après pose (patch minimal) |

## 9. Réglages Unity

**`PlayerSeatView`** : valeurs du tableau section 4.

**`CardView` (prefab)** :
- `_countBadge` : `Text` enfant de `_visualRoot`, ancré bas-gauche,
  Pos (8, 8), taille 56 × 36, police 24 gras, `Raycast Target` désactivé,
  masqué par défaut.
- Pas de `CanvasGroup` requis (retirer s'il a été ajouté).

**`_handContainer`** : pas de `RectMask2D`, pas de `Image` raycastable.

## 10. Risques (résolus)

1. Cartes masquées déplacées par l'éventail : repositionnées à chaque rendu par `LayoutSelfHand`.
2. 25 cartes non groupables : pas compressé sous le plancher, sans masquer de carte. Validé en jeu.
3. Chaque `StateChanged` recalcule tout depuis l'état du Core (`ReleaseColumns` en tête de rendu).

## 11. Validation

- [x] Compilation propre.
- [ ] Tests EditMode existants verts.
- [x] Main de 8, 15 et 25 cartes à 4 joueurs : aucun débordement.
- [x] Regroupement progressif, du plus petit rang au plus grand, rang entier.
- [x] Clic sur un groupe : éventail ouvert au-dessus, cartes du rang visibles.
- [x] Sélection de 1 à N cartes dans l'éventail ; elles restent levées.
- [x] Clic sur un autre rang : les cartes sélectionnées se baissent.
- [x] Pose : cartes retirées de la main, éventail fermé.
- [x] Échange : clic main/FaceUp, aperçu et soumission inchangés.
- [x] Don : sélection unitaire, aucun regroupement.
- [x] Retour à `NormalPlay` : regroupement de nouveau actif.
- [x] Aucun clic bloqué.
- [x] Badge non raycastable (vérifier `Raycast Target` décoché).
- [x] Sens décroissant : main et éventail inversés, regroupement inchangé.

## 12. Hors périmètre

- Tests NUnit (Phase 6).
- Modification du Core ou des Bots.
- Chat et boutons futurs.
- Ajustement fin de `_selfHandZoneLength`.
- Exposition de `_selfHandDescending` dans un menu d'options joueur.

### Ordre d'attaque
T55-2✅ → 4✅ → 3✅ → 5✅ → 6✅ → 7✅ → 11✅ → 8✅ → 10✅ → T61✅ → 9✅

### Hors périmètre
- Tests (Phase 6). Animations, bandeau d'effets, `TableColor` / `SeatColor`.
- Timer par joueur + score « Fidélité » (serveur, avec Photon).
- Canal « rejet métier → message UI » : ticket Phase 5.

---

## T56 — Animations et vitesse de jeu

**Statut : À FAIRE**

Fichiers : Controllers/AnimationController.cs

Dépendances : T55 (T60 recommandé avant).

## Travail
- Animations pilotées par `IGameObserver` (pose, pioche, ramassage, destruction), sans aucune décision.
- Multiplicateurs humain : ×0,5, ×1, ×2.
- Mode accéléré bots seuls (humain hors jeu ou terminé) : vitesse maximale, y compris les boucles longues.

## Critères
- La vitesse n'influe jamais sur l'état de jeu.
- Partie bots seuls à vitesse maximale fluide.
- Un mode instantané ne saute jamais un événement d'`IGameObserver`.

## Note de revue
Scinder en (a) multiplicateurs et calcul de durée, (b) mode bots seuls vitesse max.

## Tests (Phase 5)
- Calcul de durée d'animation selon le multiplicateur.

## Hors périmètre
- Toute décision de jeu par les animations.

---

## T57 — Bandeau d'effets

**Statut : À FAIRE**

Fichiers : UI/EffectBannerView.cs

Dépendances : T56.

## Travail
- Bandeau court (« Doublon ! », « Don du 7 », « Valet : sens inversé », « Carré ! », etc.) piloté par `IGameObserver` / `EffectKind`.

## Critères
- Un bandeau par effet détecté.
- Jamais déclenché par la Présentation elle-même.

## Tests (Phase 5)
- Mapping `EffectKind` → libellé, avec un test bouclant sur `Enum.GetValues(typeof(EffectKind))` qui échoue si un libellé manque.

## Hors périmètre
- Déclenchement d'effet par la Présentation.

---

## T58 — Écran de fin de partie

**Statut : À FAIRE**

Fichiers : UI/EndGameView.cs

Dépendances : T51.

## Travail
- Affichage du Psycko et du classement depuis `GameResult`.
- Boutons « Rejouer » et « Quitter ».

## Critères
- Aucun calcul de classement en Présentation.

## Note de revue
Préciser si « Rejouer » recrée un `GameState` sans recharger la scène.

## Hors périmètre
- Calcul de classement en Présentation.

---

## T59 — Scène SkinPreview et menu de sélection

**Statut : À FAIRE**

Fichiers : Scenes/SkinPreview.unity, Controllers/SkinPreviewController.cs

Dépendances : T54.

## Travail
- Affichage des 63 cartes avec le skin courant.
- Changer de SkinSet sans relancer la scène ; changement visible en direct.

## Critères
- 63 cartes visibles sur une seule scène ; changement de skin instantané.

## Note
Tracer l'absence des sprites Prêtre et Cavalier (54 sur 63 dans Faces, Joker de Verre à vérifier).

---

## T61 — Ordre de fin des joueurs et rang du Psycko dans le Core ✅

**Statut** : terminé et validé en partie jouée à 4 joueurs. Tests NUnit reportés en Phase 6.

### Objectif
Enregistrer dans le Core l'ordre dans lequel les joueurs terminent la partie et exposer leur rang, y compris celui du dernier joueur (le Psycko), sans révéler d'informations privées sur leurs cartes. Prérequis de l'affichage des rangs côté Présentation (T55-9).

### Périmètre
- Ajouter `Domain/FinishOrder.cs`, une valeur immuable qui conserve l'ordre des identifiants de joueurs terminés. `Add` sur un Id déjà présent retourne l'instance inchangée.
- Faire porter à `GameState` un `FinishOrder` et le préserver dans toutes les transitions d'état (`With*`, `PlayCards`, `PickUpPile`, `DrawCards`, `TransferCard`, `PickUpFaceUp`, etc.).
- Ajouter `IGameStateQuery.GetFinishRank(int playerId)` : rang à partir de 1, ou `null` si le joueur n'a pas terminé.
- Attribuer le rang une seule fois lors du passage à `DefPhase.Finished`, dans `AdvancePlayerPhase` et `EliminatePlayer`. Un joueur éliminé reçoit un rang au moment de l'appel.
- Un appel sur un joueur déjà `Finished` ne modifie pas l'ordre et n'attribue pas de rang supplémentaire.
- En cas de fins multiples, l'ordre est celui des transitions effectuées : la première transition vers `Finished` obtient le rang le plus bas. Règle documentée dans le code.
- **Rang du Psycko** : le dernier joueur ne passe jamais en `Finished`. `GameState.GetFinishRank` le dérive : si `FinishOrder.Count == Players.Count - 1` et que le joueur demandé n'est pas dans l'ordre, il reçoit `Players.Count` (4 à 4 joueurs). Cette valeur est **dérivée, jamais stockée** : `FinishOrder` n'est pas modifié. Tant que moins de `Players.Count - 1` joueurs ont fini, les joueurs restants renvoient `null`.
- `GetFinishRank` lève `ArgumentException` si l'Id est inconnu (via `GetSeatIndex`).
- Ajouter `FinishRank` (`int?`) à `IOpponentVisibleInfo` et `OpponentVisibleInfo` (paramètre optionnel de fin de constructeur, validé ≥ 1), alimenté depuis `PlayerVisibleState.From` et `PlayerVisibleState.FromExchange` (`null` en échange).
- Ajouter `SelfFinishRank` (`int?`) à `IPlayerVisibleState` et `PlayerVisibleState`, pour le siège du joueur lui-même.
- Mettre à jour la doc de `IGameStateQuery.GetFinishRank` (rang du dernier joueur déduit).
- Aucune carte cachée exposée par ces vues : le rang est une donnée publique.

### Fichiers
- [x] Ajouté : `Psycko.Core/Domain/FinishOrder.cs`
- [x] Modifié : `Psycko.Core/Domain/GameState.cs`
- [x] Modifié : `Psycko.Core/Interfaces/IGameStateQuery.cs`
- [x] Modifié : `Psycko.Core/Interfaces/IPlayerVisibleState.cs` (+ `IOpponentVisibleInfo`)
- [x] Modifié : `Psycko.Core/Domain/PlayerVisibleState.cs`
- [x] Modifié : `Psycko.Core/Domain/OpponentVisibleInfo.cs`

### Hors périmètre
- Pas de modification de `GameResultTracker` ni de `GameResultCalculator` (ticket séparé).
- Pas de modification de `Presentation/` (voir T55-9).
- Pas de mélange avec le correctif T62 (le 2).
- **Tests NUnit EditMode reportés en Phase 6** (inversion Phase 5 UI finale / Phase 6 Tests NUnit). À écrire alors : `FinishOrder` (Add, doublon, GetRank, immuabilité), `GetFinishRank` (ordre, joueur non fini, éliminé, appel sur joueur déjà `Finished`, rang du Psycko = `Players.Count` dès que tous les autres ont fini, `null` avant), vues filtrées (`FinishRank`, `SelfFinishRank`, `null` en échange). Couverture cible ≥ 90 % sur les fichiers ajoutés ou modifiés.

### Validation
- [x] Compilation propre du Core.
- [x] Partie jouée à 4 joueurs : rangs 1, 2, 3 attribués dans l'ordre de sortie, puis rang 4 au dernier joueur.
- [ ] Simulation `Tools/PsyckoConsole/` (1M parties, ou 10 000 si le 1M est reporté) : aucune exception, rangs de 1 à N sans doublon, inchangés après attribution. **À exécuter** (à cocher une fois lancée).
- [ ] Tests NUnit EditMode : Phase 6.

### Dépendances et suite
Dépendance : aucune. Suite : T55-9 (affichage des rangs dans `Presentation/`).

---

#### T62✅ — Fix : 2 terminal par phase (`LastCardValidator`)

**Statut** : ✅ Fait. Implémentation validée en console (Work et Talent confirmés, Luck vérifié par lecture de code). Reste à clore : simulation 1M, vérification en partie Unity, tests NUnit (reportés en Phase 6).

**Contexte (bug constaté)**
CLAUDE.md (lignes 394 à 396) interdit de terminer une phase sur un 2, dans les trois phases. Si le 2 est la dernière carte de la phase, le joueur le pose, **ramasse toute la pile (2 compris)** et passe son tour, sans destruction de la pile ni rejeu.

Avant le fix, `LastCardValidator` ne considérait un 2 comme terminal que si **les trois couches étaient vides**. Un 2 qui terminait la phase Work ou Talent détruisait donc la pile et déclenchait la transition de phase. Reproduit en console : il restait 3 FaceDown, le dernier 2♠ de la main a été joué, résultat `Detruite2, PhaseTransition (Luck)`. Même bug dans Unity (Core partagé).

**Objectif** : vérifier si le coup termine la **phase en cours**, et non l'ensemble des cartes du joueur.

**Cause racine**
Dans `GameOrchestrator.ApplyPlay`, le verdict `TerminatesOnTwo` était calculé **après** `TurnManager.ApplyPlay`. À ce stade, la reconstruction (pioche, ramassage FaceUp, transition de phase) était déjà appliquée, donc le validateur lisait un état déjà transformé.

**Comportement implémenté**
- `LastCardValidator` évalue le coup selon `player.CurrentPhase`, sur l'état **avant** le coup :
  - **Work** : terminal si `handAfter <= 0` et pioche vide.
  - **Talent** : terminal si `handAfter <= 0` et `faceUpAfter <= 0`.
  - **Luck** : terminal si plus aucune carte en main, FaceUp ou FaceDown. Comportement inchangé.
- Si le coup est terminal **et** contient un 2 :
  - le 2 est posé (`PlayCards`) ;
  - le joueur ramasse toute la pile, 2 compris ;
  - le tour passe au joueur suivant ;
  - **ni** `Detruite2`, **ni** rejeu, **ni** `PhaseTransition`.
- La transition de phase normale reprend au prochain tour du joueur, avec sa nouvelle main.
- `GameOrchestrator.ApplyPlay` : la garde est déplacée **avant** `TurnManager.ApplyPlay`. Pas de pioche, de ramassage FaceUp ni de transition sur ce coup. `ResolvePickup` reste appelé sur l'état d'origine, et `ExecutePickup` sur l'état posé.
- Log : une ligne `Play` sans effet, puis la ligne `PickupPile`.
- `tracker.RecordPlay` n'est pas appelé pour ce coup (comportement inchangé).
- **Luck (carte FaceDown)** : déjà géré par `GameOrchestrator.ApplyBlindPlay` (`play.EffectiveRank == DefRank.Two && player.FaceDown.Count == 1`), chemin non modifié par T62.

**Fichiers**
- [x] Modifié : `Psycko.Core/Rules/Validation/LastCardValidator.cs`
- [x] Modifié : `Psycko.Core/Services/GameOrchestrator.cs` (garde déplacée avant `TurnManager.ApplyPlay`)
- [ ] ⏭️ Reporté en Phase 6 : `Tests/EditMode/Rules/Validation/LastCardValidatorTests.cs`
- [ ] ⏭️ Reporté en Phase 6 : test d'intégration dans `GameOrchestratorTests`

**Tests NUnit EditMode (reportés en Phase 6)**

Cas où la règle doit s'appliquer :
- [ ] Work : un seul 2 en main, pioche vide, FaceUp et FaceDown restants. Ramassage 2 compris, sans destruction ni rejeu.
- [ ] Work : seulement une paire de 2 en main, pioche vide. Ramassage.
- [ ] Talent : dernière carte de la phase = 2, FaceDown restants. Ramassage.
- [ ] Luck : dernier FaceDown retourné = 2. Ramassage (non-régression).

Cas où la règle ne doit **pas** s'appliquer :
- [ ] Il reste une autre carte dans la phase : le 2 détruit la pile normalement.
- [ ] Work avec pioche non vide : destruction normale.
- [ ] Dernière carte autre qu'un 2 : transition de phase normale.

Intégration :
- [ ] `GameOrchestrator.ApplyPlay` : ramassage forcé, aucun effet `Detruite2` ni `PhaseTransition` émis.

**Critères d'acceptation**
- [x] Scénario console reproduit (dernier 2 de la main, pioche vide, FaceDown restants) : ramassage forcé confirmé en Work et en Talent.
- [x] Luck : cas couvert par `ApplyBlindPlay` (vérifié par lecture de code, non modifié).
- [x] Le projet compile.
- [ ] Simulation `Tools/PsyckoConsole/` sur 1M parties sans exception ni blocage.
- [ ] Règle vérifiée en partie Unity : l'humain ramasse automatiquement, les bots aussi.
- [ ] ⏭️ Tests NUnit verts et couverture de `LastCardValidator` ≥ 90 % (Phase 6).
- [x] Aucun changement dans `Presentation/`.

**Hors périmètre**
- Signalement visuel du ramassage forcé (bandeau d'effet de carte, reporté).
- Stratégie des bots pour éviter de finir sur un 2 (Phase 4, StrategicBot).

**Dépendances** : `LastCardValidator`, `TurnManager`, `GameOrchestrator`. Indépendant de T55-x.

**Après le merge** : mettre à jour le GDD Notion, section « 2 en dernière carte », avec la condition de fin de chaque phase :
- Work : main et pioche vides.
- Talent : main et FaceUp vides (les FaceUp passent dans la main à la transition).
- Luck : plus aucune carte.

Préciser aussi que la transition de phase n'a pas lieu au tour du ramassage forcé.

## T63 ✅— Rythme des bots : délai de jeu et mode accéléré

**Statut : FAIT** (validé manuellement en partie Unity, humain contre 3 bots)

Branche : `feat/T63-rythme-bots`

Fichiers :
- [x] `Assets/Scripts/Presentation/Controllers/BotPacing.cs` (nouveau)
- [x] `Assets/Scripts/Presentation/Controllers/GameplayController.cs` (modifié)
- [x] `Assets/Scripts/Presentation/Controllers/LocalGameSession.cs` (modifié : ajout de `TryPeek`)

Dépendances : T55. Absorbe le point (b) de T56. T56 se limite désormais aux multiplicateurs et à la durée des animations.

## Objectif
Donner au joueur humain le temps de suivre la partie quand les bots jouent, puis accélérer fortement la fin de partie une fois l'humain sorti du jeu (gagné ou éliminé).

## Périmètre
- Temporisation côté Présentation uniquement, à la consommation des événements de session. Le Core décide toujours instantanément ; la Présentation retarde seulement l'affichage.
- Délai de base réglable dans l'Inspector : ~1 s entre chaque coup de bot tant que l'humain est en jeu.
- Mode accéléré **automatique**, sans clic, dès que `GetFinishRank` de l'humain est non nul ou que l'humain est éliminé : délai de 0,02 s, réglable dans l'Inspector, avec plusieurs coups par frame (plafonné) pour rester fluide dans les boucles longues.
- Aucun délai avant le tour de l'humain : après le dernier coup de bot, la demande à l'humain est affichée sans attente.
- Délai fixe, indépendant du type d'action (pose, ramassage, destruction).
- Les `LogEntry` ne sont pas temporisés (traitement du log hors périmètre).
- Aucun bouton « Passer » : le jeu reste rapide, non nécessaire.

## Décisions
1. Délai fixe, non dépendant de l'action.
2. Fin de l'humain détectée par lecture de `GetFinishRank` sur l'état reçu, sans passer par le log.
3. Pas de délai sur le tour de l'humain.
4. Délai de base réglable dans l'Inspector (`GameplayController`, scène `GameplayLocal.unity`).
5. Délai accéléré réglable dans l'Inspector, valeur 0,02 s.
6. Humain éliminé : accélération aussi.
7. Pas de bouton « Passer ».

## Champs Inspector (`GameplayController`, scène `GameplayLocal.unity`)
- `_botDelaySeconds` : valeur retenue en test (1,0 par défaut)
- `_fastDelaySeconds` : 0,02
- `_fastMaxEventsPerFrame` : 8

## Critères d'acceptation
- [x] Contre bots, chaque bot attend le délai avant de jouer ; le joueur suit la partie.
- [x] Dès que l'humain a gagné, la partie se termine très vite sans gel de l'interface.
- [x] Accélération aussi quand l'humain est éliminé.
- [x] Le délai n'influe jamais sur l'état de jeu : mêmes décisions, même résultat à seed égal.
- [x] Aucune décision de jeu en Présentation.
- [x] Aucun événement du Core sauté ni réordonné (file FIFO préservée).
- [x] Aucun changement dans `Psycko.Core/`, `Psycko.Bots/`, `Tools/`.

## Hors périmètre
- Animations et multiplicateurs de durée (T56).
- Modification du Core ou des bots.
- Bouton « Passer ».
- Traitement du log et détection d'effets.
- Tests NUnit (Phase 6).

## Validation
Procédure manuelle, partie Unity humain contre 3 bots :
- [x] Délai visible entre chaque coup de bot.
- [x] Aucun délai avant le tour de l'humain.
- [x] Accélération automatique dès que l'humain gagne.
- [x] Accélération automatique quand l'humain est éliminé.
- [x] Aucun gel de l'interface.

## Après merge
- Notion : mettre à jour la section rythme des bots (GDD Présentation).
- Notion : retirer le point (b) de T56.

T64 : Dédoublonnage de la règle « 2 terminal en Luck » (ApplyBlindPlay → LastCardValidator)
Statut : À faire (à planifier après merge de T62)
Branche : refactor/T64-dedoublonnage-2-terminal-luck
Couches touchées : Core uniquement. Aucun changement dans Presentation/, Bots/ ni Tools/.
Objectif
Supprimer la duplication de la règle « 2 terminal ». GameOrchestrator.ApplyBlindPlay calcule sa propre condition (play.EffectiveRank == DefRank.Two && player.FaceDown.Count == 1) au lieu de passer par LastCardValidator. Après T62, le validateur est la source unique de la règle, évaluée par phase. Le blind play (Luck) doit l'appeler.
Aucun changement de comportement. C'est un refactor pur. En Luck avec la main vide, les deux calculs sont équivalents : un 2 est terminal si FaceDown.Count == 1.
Périmètre

Remplacer le calcul local terminatesOnTwo dans ApplyBlindPlay par LastCardValidator.TerminatesOnTwo(play, player, state).
Vérifier que le résultat est identique dans tous les cas Luck : dernier FaceDown, FaceDown restants, carte non jouable, 2 non terminal.
Conserver la branche !playable || terminatesOnTwo et les deux entrées de log inchangées.
Mettre à jour le commentaire de ApplyBlindPlay pour référencer le validateur.

Fichiers

 Modifié : Psycko.Core/Services/GameOrchestrator.cs (ApplyBlindPlay uniquement)
 Modifié ou nouveau : tests de non-régression ApplyBlindPlay dans GameOrchestratorTests
 Aucun changement dans LastCardValidator.cs (déjà traité par T62)

Hors périmètre

Toute modification de la règle elle-même (T62).
Signalement visuel du ramassage forcé.
Refactor des autres méthodes de GameOrchestrator.

Validation

 Tests NUnit EditMode verts, dont les tests existants (aucune régression).
 Tests ApplyBlindPlay ajoutés : dernier FaceDown = 2 (ramassage forcé), 2 non terminal (destruction normale), carte non jouable (ramassage forcé).
 Simulation Tools/PsyckoConsole/ sur 1M parties : sans exception ni blocage, et résultats identiques à la baseline post-T62 (même graine).
 Aucune règle « 2 terminal » codée en dehors de LastCardValidator (recherche DefRank.Two dans GameOrchestrator).

Dépendance : T62 mergé.

# Série T65 — Refonte Présentation et événements

Cette série refond la présentation et le flux d’événements afin d’assurer une timeline cohérente, des vues synchronisées et des requêtes humaines sans blocage.

| Ticket | Titre | Statut | Dépendances |
|---|---|---|---|
| T65-a | Correctifs Présentation d'origine (série T65) | NON RÉSOLU — absorbé par la refonte T65-b à T65-g | T65-b à T65-g |
| T65-b | Core : événement OnCardsPlaced | À FAIRE | Aucune |
| T65-c | Timeline de Présentation (C# pur) | À FAIRE | T65-b |
| T65-d | Séquenceur de Présentation et PresentationTimings | À FAIRE | T65-c |
| T65-e | GameTableView.Render(state) et vues passives | À FAIRE | T65-d |
| T65-f | Requêtes humaines, routage des clics, échange pré-partie | À FAIRE | T65-e |
| T65-g | Rebranchement de la scène, nettoyage et playtest | À FAIRE | T65-b à T65-f ; T51 si toujours en pause |

---

# T65-a — Correctifs Présentation d'origine (série T65)
**Statut : NON RÉSOLU — absorbé par la refonte T65-b à T65-g**
**Contexte :** les 5 correctifs de jouabilité relevés en test ont montré un défaut de conception : la table est dessinée depuis plusieurs sources non synchronisées (Pile sur l'aperçu, main sur l'état résolu). Symptômes : carte visible en main ET sur la Pile pendant les effets, flash sur toute pose, interblocage du Don du 7 (la requête Gift reste derrière un CardsPlaced qui attend un StateChanged qui ne viendra qu'après la réponse). Plutôt que d'empiler des correctifs, on refait la Présentation et le flux d'événements proprement.
**État des correctifs d'origine :** tableau Correctif | État | Devenir :
1. Pile stable (hash par index absolu) | Fait, validé visuellement | Conservé ; tests et extraction PilePoseCalculator dans T65-e
2. Aperçu avant destruction/ramassage | Abandonné tel que conçu | Remplacé par T65-b (OnCardsPlaced) et T65-d (séquenceur)
3. Ramassage forcé automatique | Non fait | T65-d (délai) et T65-f (exécution)
4. Zones cliquables (Don, Pile, ramassage volontaire) | Non fait | T65-e (vues) et T65-f (routage)
5. Échange pré-partie | Non fait | T65-f
**Valeur retenue :** PreviewBeforeClearSeconds = 1,0 s par défaut (le 1,5 s du texte d'origine est abandonné).
**À faire pour clôturer :** [ ] Push Git avec la mention « T65-a : non résolu, absorbé par T65-b à T65-g ». [ ] Branche refactor/presentation-timeline. [ ] Clore T65-a quand T65-g est validé.
**Critères d'acceptation de la série :** hérités de T65, répartis dans T65-b à T65-g, vérifiés ensemble dans la checklist de T65-g.

---

# T65-b — Core : événement OnCardsPlaced
**Statut : À FAIRE**
**Contexte :** l'état intermédiaire « carte posée, effets pas encore appliqués » est aujourd'hui reconstruit par la Présentation (PreviewStateBuilder), ce qui recalcule une règle côté Présentation et crée la désynchronisation. Le Core doit fournir ce vrai état.
**Dépendances :** aucune (premier ticket de la refonte).
**Fichiers probables :** Core/Interfaces/IGameObserver.cs (ajout de OnCardsPlaced(GameState)) ; Core/Services/GameRunner.cs et/ou GameOrchestrator.cs (émission juste après la pose, avant les effets) ; observateurs existants (console, tests, SessionObserver) : implémentation vide ou adaptée.
**Contenu :** OnCardsPlaced(GameState placed) appelé après retrait des cartes de la main et ajout à la Pile, avant application des effets (2, 7, Valet, etc.). Pour un FaceDown, appelé après la révélation. L'état fourni est un vrai GameState cohérent : la carte n'est plus en main et est au sommet de la Pile. Le contenu du log est inchangé (même séquence, même format, rejeu identique). Les observateurs existants compilent sans changement de comportement.
**Tests NUnit EditMode :** [ ] appelé une fois par pose (Play et BlindPlay), jamais pour un ramassage. [ ] l'état contient la carte sur la Pile et pas dans la main du joueur. [ ] appelé avant OnStateChanged du même tour. [ ] pour un 7 : appelé avant la demande de Don. [ ] pour un 2 : l'état contient encore la Pile précédente + le 2 (pas encore détruite). [ ] log identique avant/après pour une graine fixée (comparaison octet à octet). [ ] tests Core existants verts.
**Critères d'acceptation :** [ ] aucune règle de jeu modifiée. [ ] simulation console 1M parties : résultats identiques (mêmes statistiques pour mêmes graines). [ ] aucun appel superflu ni ordre différent des événements existants.
**Hors périmètre :** toute modification de la Présentation.

---

# T65-c — Timeline de Présentation (C# pur)
**Statut : À FAIRE**
**Contexte :** remplacer la file SessionEvent + Pump par une timeline ordonnée de vraies étapes. Une requête humaine est une étape comme les autres ; elle n'attend jamais un état futur.
**Dépendances :** T65-b.
**Fichiers probables (Presentation/Logic/) :** TimelineStep.cs (Snapshot(Placed), Snapshot(Resolved), Request(kind, visibleState), Log, Finished) ; PresentationTimeline.cs (file ordonnée, thread-safe, produite par le thread de jeu) ; TimelineObserver.cs (IGameObserver qui alimente la timeline).
**Contenu :** chaque étape porte un GameState complet quand elle en a un ; aucune vue ne reconstruit d'état. L'étape Request embarque l'état visible fourni par le Core au moment de la demande (main déjà sans la carte posée pendant un Don). Aucune règle de blocage « attendre le prochain état résolu ». Aucun UnityEngine. Remplace SessionEvent, PeekNextState et PreviewStateBuilder, supprimés dans T65-g.
**Tests NUnit EditMode :** [ ] ordre FIFO conservé. [ ] séquence Placed, Request(Gift), Resolved : la requête est consommable sans attendre Resolved. [ ] aucun état partagé modifiable entre threads. [ ] Finished toujours dernière étape. [ ] état vide ou null refusé proprement.
**Critères d'acceptation :** [ ] le scénario de l'interblocage du 7 est couvert par un test et passe. [ ] aucune dépendance Unity.
**Hors périmètre :** rythme et délais (T65-d), affichage (T65-e).

---

# T65-d — Séquenceur de Présentation et PresentationTimings
**Statut : À FAIRE**
**Contexte :** remplacer BotPacing et la logique de Pump par un séquenceur unique qui décide quelle étape afficher et combien de temps la garder. Il absorbe l'ancien correctif 2 (aperçu) et le délai du correctif 3 (ramassage forcé).
**Dépendances :** T65-c.
**Fichiers probables :** Logic/PresentationSequencer.cs (C# pur, Tick(dt)) ; Models/PresentationTimings.cs (ScriptableObject, Create > Psycko > Presentation Timings) ; Logic/SequencerClock.cs ou équivalent (horloge injectable pour tests).
**Paramètres PresentationTimings :** tableau Champ | Défaut | Usage : PreviewBeforeClearSeconds | 1,0 | durée d'affichage de l'état Placed avant résolution quand l'étape suivante détruit ou ramasse ; ForcedPickupDelaySeconds | 1,0 | délai avant ramassage forcé automatique ; BotDelaySeconds | à reprendre de BotPacing | rythme des bots. Valeurs ≥ 0 ; négatif ramené à 0 dans OnValidate ; 0 = instantané.
La détection « clear » reste une comparaison de tailles de Pile entre l'état Placed et l'état Resolved : aucune règle de jeu. Mode accéléré (humain sorti du jeu) : aperçu ignoré, délais bots réduits. Une étape Request stoppe l'avancée jusqu'à la réponse.
**Tests NUnit EditMode :** [ ] Placed affiché PreviewBeforeClearSeconds avant Resolved quand la Pile diminue. [ ] pose simple : pas d'attente d'aperçu. [ ] délai 0 : instantané, FIFO conservé. [ ] mode accéléré : aucun aperçu. [ ] étape Request : le séquenceur s'arrête et reprend à la réponse. [ ] PresentationTimings : négatif ramené à 0. [ ] ramassage forcé : délai de ForcedPickupDelaySeconds avant exécution.
**Critères d'acceptation :** [ ] un 2 posé reste visible 1,0 s avant destruction. [ ] FaceDown destructeur visible avant destruction ; ramassage montre la Pile avant transfert. [ ] délais modifiables dans l'Inspector sans recompiler.
**Hors périmètre :** affichage (T65-e), clics (T65-f).

---

# T65-e — GameTableView.Render(state) et vues passives
**Statut : À FAIRE**
**Contexte :** supprimer le flash et le double affichage. Toutes les vues se redessinent depuis le même GameState complet, dans la même frame, via un seul point d'entrée.
**Dépendances :** T65-d.
**Fichiers probables :** UI/GameTableView.cs (Render(GameState), point d'entrée unique) ; UI/ZoneView.cs, PlayerSeatView.cs, CardView.cs (vues passives, sans abonnement aux événements) ; Logic/PilePoseCalculator.cs (nouveau, C# pur, extraction du correctif 1).
**Contenu :** une seule image de la table. PilePoseCalculator : Hash(index, salt), Unit(hash), calcul (x, y, rotation) depuis l'index absolu et les plages, plages réglables dans l'Inspector de ZoneView (valeurs retenues à noter : _maxOffsetX, _maxOffsetY, _maxRotation, _maxVisiblePileCards). Collider/Raycast Target du siège couvrant toute la zone (cartes, nom, fond, sans trou) pour préparer le Don du 7 (correctif 4.1). Commit repris : « T65 (1/5): Pile stable, pose déterministe par index absolu » via PilePoseCalculator.
**Tests NUnit EditMode :** [ ] même index et mêmes plages = même pose. [ ] ajouter des cartes ne change pas les poses précédentes. [ ] valeurs dans [-max, +max] (balayage 0 à 1000). [ ] plages à 0 = pose nulle. [ ] projection GameState vers modèle de vue : une carte n'est jamais à la fois dans la main et sur la Pile.
**Critères d'acceptation :** [ ] plus de flash à la pose, sur toutes les cartes. [ ] plus de carte à la fois en main et sur la Pile pendant un effet. [ ] cartes déjà posées stables sur 10 poses consécutives.
**Hors périmètre :** gestion des clics (T65-f).

---

# T65-f — Requêtes humaines, routage des clics, échange pré-partie
**Statut : À FAIRE**
**Contexte :** regrouper les correctifs 3, 4 et 5 de T65 sur la nouvelle base. Aucune règle dupliquée : le Core valide tout.
**Dépendances :** T65-e.
**Fichiers probables :** Controllers/HumanRequestController.cs (cycle de vie d'une requête : affichage, réponse, timer) ; Controllers/InputHandler.cs (routage des clics) ; Logic/PileClickRouter.cs (C# pur, priorités du clic sur la Pile) ; Models/HumanSelectionState.cs (sélection FaceUp en premier, Main en premier) ; Logic/PreGameExchangeState.cs (C# pur, état d'échange pré-partie).
**Contenu :** Ramassage forcé auto : après ForcedPickupDelaySeconds, chemin existant du ramassage ; message « Aucune carte jouable » pendant le délai. Don du 7 : clic valide sur cartes, zone ou nom du siège cible ; siège terminé ou donneur refusé ; timer de 15 s côté interface, à l'expiration l'interface appelle ExpirePending() (le Core applique le repli), compte à rebours affiché. Clic sur la Pile, priorité : 1. phase de Don : ignoré ; 2. cartes sélectionnées : jouer (équivalent bouton Jouer) ; 3. aucune sélection et ramassage volontaire autorisé : ramasser ; 4. sinon ignoré. Échange pré-partie : FaceUp puis Main = échange ; Main puis FaceUp = échange ; même carte deux fois = désélection ; 2e carte de la même zone = remplacement. Fin du décompte : échanges déjà faits conservés, sélection à moitié abandonnée sans modifier la main ; l'état envoyé au Core est l'état d'échange réel, pas « aucun échange ».
**Tests NUnit EditMode :** [ ] PileClickRouter : 4 cas de priorité. [ ] Don : siège valide accepté ; terminé ou donneur refusé. [ ] Échange : les 4 comportements de sélection. [ ] fin du décompte avec échanges faits : état conservé ; sélection à moitié ignorée. [ ] expiration du Don : ExpirePending() appelé une seule fois, réponse de repli transmise. [ ] ramassage forcé : exécuté après le délai, jamais deux fois.
**Critères d'acceptation :** [ ] Don du 7 sans blocage (régression de l'interblocage couverte). [ ] sans carte jouable, ramassage automatique après le délai. [ ] clic Pile avec sélection joue ; sans sélection ramasse quand autorisé. [ ] échange dans les deux sens ; échanges conservés à la fin du décompte.
**Hors périmètre :** modification des règles Core (2, FaceDown, ramassage, Don, échange).

---

# T65-g — Rebranchement de la scène, nettoyage et playtest
**Statut : À FAIRE**
**Contexte :** brancher la nouvelle chaîne sur GameplayLocal, supprimer l'ancien code et valider toute la série.
**Dépendances :** T65-b à T65-f. Le merge reste conditionné à T51 si T51 est toujours en pause.
**Contenu :** GameplayController réduit à un orchestrateur fin : session, séquenceur, GameTableView.Render, contrôleur de requêtes. Suppression : SessionEvent, PreviewStateBuilder, BotPacing, Pump, PeekNextState, HasQueuedHumanRequest (si ajouté), logs [HumanAgent] de diagnostic (ou derrière un drapeau). Inspector : référence PresentationTimings sur le contrôleur ; valeurs de ZoneView ressaisies. Clôture de T65-a.
**Checklist de playtest (1 humain + 3 bots) :** [ ] aucune erreur rouge en Play, partie complète jouable. [ ] pose normale : pas de flash ni double affichage. [ ] 7 : Don sans blocage, timer de 15 s opérationnel. [ ] 2 : visible 1,0 s avant destruction. [ ] FaceDown destructeur visible avant destruction ; ramassage : Pile visible avant transfert. [ ] ramassage forcé automatique après le délai ; ramassage volontaire par clic sur la Pile. [ ] échange pré-partie dans les deux sens ; échanges conservés à la fin du décompte. [ ] mode accéléré : aucun aperçu. [ ] tests NUnit EditMode verts (Core et Présentation).
**Critères d'acceptation :** [ ] Core inchangé hors T65-b ; log et rejeu identiques. [ ] aucune règle de jeu en Présentation. [ ] message de commit final : « T65: Présentation refondue (timeline, séquenceur, vue unique, requêtes, clics, échange) ».
**Étapes suivantes :** mise à jour Notion (statuts T65-a à T65-g, valeurs retenues de PresentationTimings et de ZoneView).

---

Message de commit proposé pour le push : « T65-a: non résolu, absorbé par la refonte (T65-b à T65-g). Ajout des fiches de la série. »


## Ordre d'exécution

T50 ✅ → T51 ✅ → T52 ✅ → T53 ✅ → T54 ✅ → T55✅ [+ T61✅]  → T62✅ → T63✅ → T58 → T59 → T56 (a) → T57 → T64 → T65