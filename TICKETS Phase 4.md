# TICKETS Phase 4 — Présentation locale (GameplayLocal)
Phase 4 : interface Unity 2D, jouable en local (1 humain + 3 bots), paysage uniquement. Zéro logique de jeu dans la Présentation. Toute décision passe par GameRunner / GameOrchestrator et les contrats existants.
Hors périmètre Phase 4 : Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise de partie, mode spectateur depuis GameLog, options d'accessibilité daltonisme et taille de carte (reportées Phase 5).

Interface Unity 2D jouable en local (1 humain + 3 bots), en paysage uniquement. La Présentation ne contient aucune logique de jeu : toute décision passe par `GameRunner` / `GameOrchestrator` et les contrats existants.

**Hors périmètre Phase 4 :** Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise, mode spectateur depuis GameLog, options daltonisme et taille de carte (Phase 5).

**Ordre :** T50 ✅ → T51 (EN PAUSE) → T52 ✅ → T53 ✅ → T54 (en validation) → T55 → T60 → T56 → T57 → T58 → T59. Tests reportés en Phase 5 (décision Ekinox), sauf demande explicite. Le merge de T52–T55 est bloqué tant que T51 n'est pas repris.

## T50 — Bootstrap scène, asmdef, paysage verrouillé

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

---

# T52 — Vue de table (sièges, zones) en paysage

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

# T53 — Contrat de skin et skins de base interchangeables

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

# T54 — CardView utilisant le skin

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

## T55 — Input humain, surbrillance, relevé, phase d'échange, lisibilité de table

**Statut : EN COURS** (T55-0 mergé et poussé ; T55-1 à T55-7 faits et validés ; T55-8 à T55-12 à faire)

Branche : `T55-human-input` (depuis `main` à jour). Merge bloqué par T51.
Dépendances : T54, T55a (✅ 89/89). T55-9 dépend de T62 (Core : ordre de fin).

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

#### T55-9 — Bannière de classement #1 / #2 (dépend de T62)
- `PlayerSeatView._rankBanner` (Text 28 px, ancre top-right, 56×32), « #1 » / « #2 »
  selon l'ordre de fin exposé par T62 ; vide pour les autres sièges.

#### T55-10 — Pile désalignée + carte sous le Joker de Verre
- `ZoneView.RenderPile` : décalage déterministe par index (hash de l'index, pas de `Random`) :
  position ±8 px, rotation ±6°.
- Si le dessus est le Joker de Verre : rendre aussi la carte juste dessous (`Pile.Cards`).
- Sprite Joker de Verre : importeur `Sprite Mode = Single`, `Mesh Type = Full Rect`,
  `Alpha Is Transparency` coché (le mode Multiple/slicing auto ne détecte que les pixels opaques).

#### T55-11 — Main triée et contenue
- `HandSorter.Sort` appliqué à la main humaine (rang puis couleur).
- Spacing : `Min(_defaultSpacing, (_handMaxWidth − _cardWidth) / (n − 1))`,
  `_defaultSpacing = 90`, `_cardWidth = 120`, `_handMaxWidth = 1500` (Inspector), plancher 28 px.

#### T55-12 — Validation finale
- Run All EditMode (89/89), test manuel complet (jouer même rang / autre rang / Joker, ramasser,
  FaceDown aveugle, Don, échange infini, 90 s, « Prêt ! », bannières, dos adverses, pile, main contenue).
- Commit `Phase 4 T55`. Ne pas merger (T51).

### Critères
- Aucun `if` de règle dans Controllers/ ni UI/. Aucune Card FaceDown/main adverse lue.
- Sélection toujours cohérente avec le relevé. Durées réglables : `_exchangeSeconds`, `_giftSeconds`.

### Ordre d'attaque
T55-2✅ → 4✅ → 3✅ → 5✅ → 6 → 7a → 7 → 11 → 8 → 10 → (T61) → 9 → 12.

### Hors périmètre
- Tests (Phase 5). Animations, bandeau d'effets, `TableColor` / `SeatColor`.
- Timer par joueur + score « Fidélité » (serveur, avec Photon).
- Canal « rejet métier → message UI » : ticket Phase 5.

---

## T60 — Performance (atlas de sprites, draw calls)

**Statut : À FAIRE**

Dépendances : T54.

## Travail
- Regrouper les sprites d'un même skin dans un Sprite Atlas unique.
- Un draw call = une instruction de rendu envoyée au GPU ; moins de draw calls = moins de charge sur téléphone.
- Cible : 60 fps sur appareil d'entrée de gamme.
- Mesure avant/après au Profiler Unity, consignée dans le ticket.

## Critères
- Mesure Profiler consignée.

## Note d'ordre
Placé avant T56 : l'atlas conditionne le chargement des sprites dont dépendent les animations.

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

## T61 — Core : exposer l'ordre de fin des joueurs

**Statut : À FAIRE** — Prérequis de T55-9. Branche : `T62-finish-order` (depuis `main`).

### Contexte
Vérifié sur `main` : `GameState` n'enregistre pas l'ordre dans lequel les joueurs passent en
`DefPhase.Finished` (`AdvancePlayerPhase`, `EliminatePlayer`). `OpponentVisibleInfo` ne porte que
`CurrentPhase`. Aucune source pour afficher « #1 » / « #2 ».

#### T62 — Fix : 2 terminal par phase (`LastCardValidator`)

**Statut** : à faire. Ticket Core : **les tests NUnit EditMode sont obligatoires avant le merge**.

**Contexte (bug constaté)**
CLAUDE.md (lignes 394 à 396) interdit de terminer une phase sur un 2, dans les trois phases. Si le 2 est la dernière carte, le joueur le pose, **ramasse toute la pile (2 compris)** et passe son tour, sans destruction de la pile ni rejeu.

Aujourd'hui, `LastCardValidator` ne considère un 2 comme terminal que si **les trois couches sont vides** (`handAfter <= 0 && faceUpAfter <= 0 && faceDownAfter <= 0`). Résultat : un 2 qui termine la phase Work ou Talent détruit la pile et déclenche la transition de phase.

Le bug a été reproduit en console : il restait 3 FaceDown, le dernier 2♠ de la main a été joué, et on a obtenu `Detruite2, PhaseTransition (Luck)`. Le même bug existe dans Unity, puisque la console et Unity partagent le Core.

**Objectif** : vérifier si le coup termine la **phase en cours**, et non l'ensemble des cartes du joueur.

**Comportement**
- `LastCardValidator` évalue le coup selon `player.CurrentPhase`, sur l'état **avant** le coup. C'est important parce que `TurnManager.ApplyPlay` déclenche la transition de phase avant que l'orchestrateur appelle le validateur.
  - **Work** : le coup est terminal si `handAfter <= 0 && state.DrawPile.Count == 0`.
  - **Talent** : le coup est terminal si `handAfter <= 0 && faceUpAfter <= 0`. Cette condition marche que les FaceUp passent dans la main ou soient jouées depuis la table. La console montre qu'elles passent dans la main ; c'est à confirmer et à écrire dans le GDD.
  - **Luck** : le coup est terminal si `handAfter <= 0 && faceUpAfter <= 0 && faceDownAfter <= 0`. C'est le comportement actuel, qui doit rester inchangé.
- Si le coup est terminal **et** que les cartes jouées contiennent un 2 :
  - le 2 est posé ;
  - le joueur ramasse toute la pile, 2 compris ;
  - le tour passe au joueur suivant ;
  - il n'y a **ni** `Detruite2`, **ni** rejeu, **ni** `PhaseTransition`.
- La transition de phase normale reprend au prochain tour du joueur, avec sa nouvelle main.
- `GameOrchestrator` (lignes 104 à 129) applique ce verdict avant tout effet de destruction ou de transition.

**Fichiers**
- [ ] Modifié : `Psycko.Core/Rules/Validation/LastCardValidator.cs`
- [ ] Modifié si nécessaire : `Psycko.Core/Services/GameOrchestrator.cs` (ordre validation → effets)
- [ ] Nouveau : `Tests/EditMode/Rules/Validation/LastCardValidatorTests.cs`
- [ ] Nouveau ou modifié : test d'intégration dans `GameOrchestratorTests`

**Tests NUnit EditMode**

Cas où la règle doit s'appliquer :
- [ ] Work : la main contient un seul 2, la pioche est vide, il reste des FaceUp et des FaceDown. Le joueur ramasse, 2 compris, sans destruction ni rejeu.
- [ ] Work : la main contient seulement une paire de 2, la pioche est vide. Le joueur ramasse.
- [ ] Talent : la dernière carte de la phase est un 2 et il reste des FaceDown. Le joueur ramasse.
- [ ] Luck : le dernier FaceDown retourné est un 2. Le joueur ramasse (non-régression).

Cas où la règle ne doit **pas** s'appliquer :
- [ ] Il reste une autre carte dans la phase : le 2 détruit la pile normalement.
- [ ] Work avec une pioche non vide : le 2 détruit la pile normalement.
- [ ] La dernière carte n'est pas un 2 : transition de phase normale.

Intégration :
- [ ] `GameOrchestrator.ApplyPlay` : le ramassage forcé a bien lieu, et aucun effet `Detruite2` ni `PhaseTransition` n'est émis.

**Critères d'acceptation**
- [ ] Le scénario console reproduit (dernier 2 de la main, pioche vide, 3 FaceDown) provoque un ramassage forcé.
- [ ] Tous les nouveaux tests passent, et les tests existants restent verts.
- [ ] La simulation PsyckoConsole sur 1M parties se termine sans exception ni blocage.
- [ ] La règle est vérifiée en partie Unity : le joueur humain ramasse automatiquement, et les bots aussi.
- [ ] La couverture de `LastCardValidator` est d'au moins 90 %.
- [ ] Aucun changement dans `Presentation/`.

**Hors périmètre**
- Signalement visuel du ramassage forcé (bandeau d'effet de carte, reporté).
- Une stratégie des bots pour éviter de finir sur un 2 (Phase 4, StrategicBot).

**Dépendances** : `LastCardValidator`, `TurnManager`, `GameOrchestrator`. Indépendant de T55-x.
**Après le merge** : mettre à jour le GDD Notion, section « 2 en dernière carte », avec la condition de fin de chaque phase.

### Travail
1. Vérifier d'abord `GameResultCalculator` et le journal (`Core/Services/Log`) : si l'ordre y est déjà
   calculable de façon fiable, T62 devient une simple exposition en lecture.
2. Sinon, ajouter à `GameState` une liste immuable `FinishOrder` (ids de joueurs, par ordre de fin),
   alimentée par `AdvancePlayerPhase` / `EliminatePlayer` quand le joueur passe à `Finished`
   (jamais deux fois le même id). Mettre à jour tous les `With*` pour la propager.
3. Exposer `IReadOnlyList<int> FinishOrder` dans `IGameStateQuery` et `IGameState`.
4. Exposer dans `IOpponentVisibleInfo` / `IPlayerVisibleState` le rang de fin du joueur (0 = pas fini,
   1, 2…), sans rien révéler de caché.
5. `GameResultCalculator` : ne pas modifier sans accord d'Ekinox ; ajouter seulement un test de
   cohérence avec `FinishOrder` si les tests reprennent.

### Critères
- Core pur, immuabilité conservée, aucun comportement de jeu modifié.
- Simulation console (Tools/PsyckoConsole) inchangée : mêmes résultats à seed égal.
- Tests existants verts (89/89). Commit `Phase 4 T62`. Merge après T51.

## Ordre d'exécution

T50 ✅ → T51 ⏸ → T52 ✅ → T53 ✅ → T54 🔄 → T55 [+ T61] 🔄 → T60 → T56 → T57 → T58 → T59