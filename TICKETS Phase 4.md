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

## Livraison
- Branche `T54-card-view` ; ne pas merger (bloqué par T51).
- Ekinox valide localement ; Notion en priorité après merge.


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
✅T50 →✅ T51 → ✅T52 → ✅T53 → T54 → T55 → T56 → T57 → T58 → T59 → T60