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

**Statut : EN COURS** (T55-0 mergé et poussé ; T55-1 conçu et validé ; T55-2 à T55-12 à faire)

Branche : `T55-human-input` (depuis `main` à jour). Merge bloqué par T51.
Dépendances : T54, T55a (✅ 89/89). T55-9 dépend de T62 (Core : ordre de fin).

Fichiers : Controllers/InputHandler.cs, Models/HumanSelectionState.cs, Models/HandVisualStateResolver.cs,
Models/ISwapPhaseClock.cs, Models/SwapPhaseClock.cs, UI/CardView.cs, UI/PlayerSeatView.cs, UI/ZoneView.cs,
UI/GameTableView.cs, Scenes/GameplayLocal.unity

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
- Phase d'échange : 90 s (champ Inspector `_exchangeSeconds`, défaut 90, Ekinox le règle).
  Échanges main ↔ FaceUp **infinis** : une même carte peut être échangée autant de fois qu'on veut.
  « Prêt ! » termine immédiatement.
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

#### T55-2 — HumanSelectionState (C# pur, Models/)
- Garder `Toggle / Remove / Clear / Prune` ; pas d'`Add`.
- `Toggle` : même rang → ajoute ; autre rang → `Clear` puis ajoute ; Joker → `Clear` puis ajoute ;
  déjà sélectionnée → retire.
- Ajouter `event Action Changed`, levé une fois par modification effective.
- Aucun `using UnityEngine`. Aucune règle Core dupliquée au-delà de « même rang » (affichage).

#### T55-3 — Surbrillance
- `Models/HandVisualStateResolver.cs` (C# pur) :
  `CardVisualState Resolve(HumanRequestKind request, bool submitted, bool selected, bool isGiftCard, bool isPlayable)`.
- `InputHandler.ReapplyCardStates` boucle et appelle `CardView.SetState`.
- Recalcul uniquement sur `GameTableView.Rendered`.
- `CardView._highlight` : Image enfant, ancres stretch/stretch, offsets −4/−4/+4/+4,
  Raycast Target décoché, désactivé par défaut.

#### T55-4 — Sélection, relevé, boutons (correctif du bug « cartes de même rang qui restent levées »)
- Une seule méthode `InputHandler.CompleteRequest()` : `_request = None; _submitted = true;
  _selection.Clear(); _giftCard = null; _giftRecipientId = -1; HideActions();`
  appelée par tous les `Submit*`. `Clear` aussi au début de chaque `OnRequest`.
- Relevé : `CardView._selectedLift = 24f`, appliqué dans `Refresh` uniquement si `Selected`
  (le visuel suit exactement `HumanSelectionState`).
- Vérifier `Card.Equals` (`Contains` / `Prune` en dépendent).
- Boutons : `Jouer` interactable ssi sélection non vide et `EverySelectedCardPlayable()` ;
  `Ramasser` toujours interactable en `NormalPlay`.

#### T55-5 — FaceDown aveugle
- `PlayerSeatView.FaceDownIndexClicked : Action<int>`, index borné par `IFaceDownBlindView.SelfFaceDownCount`.
- Jamais de `Card`.

#### T55-6 — Don du 7
- Clic carte → `_giftCard` ; clic siège → soumission immédiate (`SeatClicked(int playerId)`).
- Siège Finished : bouton `interactable = false` d'après `IOpponentVisibleInfo.CurrentPhase`
  (lecture d'état, pas de règle). Clic sans effet.
- Minuteur `_giftSeconds` (Inspector, défaut 30, 0 = illimité) via `ISwapPhaseClock` ;
  expiration → `UnityHumanAgent.Fallback`.

#### T55-7 — Phase d'échange
- `ISwapPhaseClock` : `Start(float) / Tick(float) / Remaining / IsExpired / Skip() / event Expired` ;
  `SwapPhaseClock` pur.
- `InputHandler` : `_exchangeSeconds` (défaut 90), `Tick(Time.deltaTime)` dans `Update`,
  `Expired` ou « Prêt ! » → `SubmitExchanges`.
- `_timerLabel` : Text 48 px, ancre top-center, RectTransform 200×60, pos (0, −40).

#### T55-7a — Échanges infinis (à faire avant T55-7)
- Lire `Core/Domain/Exchange/` (`PreGameExchange`, `PreGameExchangeState`, `ExchangeChoice`,
  `ExchangeRejectionReason`) et `InputHandler` (gestion des échanges).
- Déterminer pourquoi une carte ne s'échange qu'une fois : liste d'échanges cumulée côté UI,
  carte « consommée » dans le Core, ou rejet métier.
- Cible : l'UI maintient un aperçu local main/FaceUp ; un échange peut être annulé ou refait sur la
  même carte ; seul l'état final est soumis à « Prêt ! » ou à l'expiration.
- Si la limite vient du Core : ticket Core dédié (accord d'Ekinox avant modification).

#### T55-8 — Dos des cartes adverses
- `PlayerSeatView.RenderOpponent` : `HandCount` dos en éventail, `FaceDownCount` dos sous les FaceUp ;
  spacing = `Min(_defaultSpacing, (_zoneWidth − _cardWidth) / (n − 1))`, plancher 12 px ;
  jamais de plafond de nombre de dos.
- Source : `IOpponentVisibleInfo.HandCount / FaceDownCount`. Aucune `Card` cachée lue.

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
T55-2 → 4 → 3 → 5 → 6 → 7a → 7 → 11 → 8 → 10 → (T61) → 9 → 12.

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