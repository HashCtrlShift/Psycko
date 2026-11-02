# TICKETS Phase 4 — Présentation locale (GameplayLocal)

Interface Unity 2D jouable en local (1 humain + 3 bots), en paysage uniquement. La Présentation ne contient aucune logique de jeu : toute décision passe par `GameRunner` / `GameOrchestrator` et les contrats existants.

**Hors périmètre Phase 4 :** Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise, mode spectateur depuis GameLog, options daltonisme et taille de carte (Phase 5).

**Ordre :** T50 ✅ → T51 (EN PAUSE) → T52 ✅ → T53 ✅ → T54 (en validation) → T55 → T60 → T56 → T57 → T58 → T59. Tests reportés en Phase 5 (décision Ekinox), sauf demande explicite. Le merge de T52–T55 est bloqué tant que T51 n'est pas repris.

## T50 — Bootstrap scène, asmdef, paysage verrouillé

**Statut :** ✅ FAIT (en attente de merge)

**Contexte :** Socle Unity.

**Fichiers :** `Assets/Scenes/GameplayLocal.unity` ; `Assets/Scripts/Presentation/Psycko.Presentation.asmdef` (références Psycko.Core, Psycko.Bots).

**Travail :** Player Settings : Landscape Left par défaut, auto-rotation Landscape Left/Right seulement. Scène template 2D URP en index 0 (Main Camera, Global Light 2D, MainCanvas, EventSystem). Caméra Orthographic, Default Renderer 2D, fond Solid `#100d1f`. MainCanvas Screen Space - Camera (Plane Distance 100), Scaler Scale With Screen Size 1920×1080, Match 0.5, PPU 100. Décision : Screen Space - Camera pour gérer le tri sprites/UI pour les animations et rester compatible post-traitement URP.

**Dépendances :** Aucune.

**Critères :** Tous cochés : scène s'ouvre ; portrait impossible ; compilation verte ; fond `#100d1f` ; asmdef sans assembly de test.

**Tests :** Contrôle manuel en 16:9 et 19.5:9.

**Hors périmètre :** —

---

## T51 — Adaptateur de partie (GameplayController, UnityHumanAgent)

**Statut :** EN PAUSE, non mergé. Tests EditMode 8/8 verts, mais couverture Psycko.Core ≈ 1 % (UnityHumanAgent ≈ 27 %, GameplayController ≈ 63 %) : le critère ≥ 90 % n'est pas atteint. Reprise en Phase 5 (rattrapage de couverture Core), puis merge.

**Contexte :** Lancer une partie via GameRunner (1 humain + 3 RandomBot) et recevoir les événements sans logique de jeu.

**Fichiers :** `Controllers/GameplayController.cs`, `Controllers/UnityHumanAgent.cs` (`IPlayerAgent`), tests `Assets/Tests/EditMode/Presentation/UnityHumanAgentTests.cs`.

**Travail :** L'humain ne propose qu'une intention brute ; la validation se fait dans GameOrchestrator ; nullable désactivé. UnityHumanAgent expose l'intention de façon non bloquante et fournit `Submit(intention)`. Sans intention, il reste en attente sans exception ni intention inventée ; une double soumission est ignorée ; une soumission hors tour est rejetée. GameplayController construit la partie via GameRunner, s'abonne à `IGameObserver`, relaie via événement C# et se désabonne en `OnDestroy`.

**Dépendances :** T50.

**Critères :** Tous cochés sauf la couverture ≥ 90 %.

**Tests :** Intention renvoyée ; absence d'intention ; double soumission ; soumission hors tour ; partie complète avec agent scripté + 3 RandomBot à graine fixe ; abonnement/désabonnement.

**Hors périmètre :** —

---

## T52 — Vue de table (sièges, zones) en paysage

**Statut :** ✅ FAIT (merge bloqué par T51)

**Contexte :** Afficher 4 sièges et les zones pioche/pile depuis `IPlayerVisibleState` uniquement.

**Fichiers :** `UI/GameTableView.cs`, `PlayerSeatView.cs`, `ZoneView.cs`, `TableProjection.cs` (pure), `TableViewModels.cs` (`OpponentSeatViewModel`, `SelfSeatViewModel`, `ZoneViewModel`, `TableViewModel`, `SeatPosition`).

**Travail :** Croix en paysage (validée en 2340×1080), humain en bas, bots gauche/haut/droite, pile au centre. Compteurs adverses (main, FaceUp, FaceDown) sans cartes ; projection pure. Le DiscardBot est placé aléatoirement sur un siège. Compteurs pioche/pile provisoires, remplacés par le rendu de vraies cartes (pioche et pile ont désormais un CardView sur DrawCard/PileCard, composants ajoutés sur ZoneView). Compteurs de siège non étiquetés. Ekinox a retravaillé l'affichage du siège de droite (rotation Z -90, colonne), à affiner.

**Dépendances :** T51.

**Critères :** Aucun contenu caché adverse rendu (`OpponentSeatViewModel` sans propriété Card) ; rendu paysage conforme.

**Tests :** Tests unitaires du mapping reportés en Phase 5.

**Hors périmètre :** —

---

## T53 — Contrat de skin et skins de base

**Statut :** ✅ FAIT (merge bloqué par T51)

**Contexte :** Skins interchangeables, apparence seulement.

**Fichiers :** Sous `Assets/Scripts/Presentation/` : `Interfaces/ICardSkin.cs` (`Id`, `DisplayName`, `GetFace(Card)`, `GetBack()`, `TableColor`, `SeatColor`, `Font`, `FontSize`), `Models/SkinSet.cs` (ScriptableObject : `_faces`, `_cardBack`, `_tableColor`, `_seatColor`, `_font`), `Models/CardSkinDefinition.cs`, `Skins/DefaultCardSkin.cs`, `Skins/SkinRegistry.cs` (`Select(id)`, `Current`, `All`), `Controllers/GameplayController.cs` modifié. Assets : `SkinSet_Classique.asset`, `SkinSet_Sombre.asset`.

**Travail :** Retrait de `_autoAnswerExchanges` / `OnAutoRequest`, remplacés par `OnHumanRequest` avec réponse par défaut « aucun échange ».

**Dépendances :** T52.

**Critères :** Cochés : changer de skin ne touche aucun RectTransform ; nouveau SkinSet sans toucher au code ; `_autoAnswerExchanges` supprimé ; partie démarre, NormalPlay atteint ; couleurs Sombre distinctes. Non coché : identifiant inconnu → skin par défaut (implémenté, vérification Phase 5).

**Tests :** Vérification du cas identifiant inconnu reportée en Phase 5.

**Hors périmètre :** Branchement TableColor/SeatColor (→ T54b) ; TextMeshPro vs Font ; aucune UI n'appelle `SkinRegistry.Select` ; échange par défaut provisoire. Livraison : branche `T53-skins`, message de commit « T53: contrat de skin, SkinSet, SkinRegistry, retrait _autoAnswerExchanges » ; ne pas merger.

---

## T54 — CardView utilisant le skin

**Statut :** EN COURS DE VALIDATION

**Contexte :** Rendu d'une carte à partir de Card + `ICardSkin`.

**Fichiers :** `UI/CardView.cs`, `UI/CardSpriteKey.cs`.

**Travail :** Clé pure `{Suit}_{Rank}` et `Joker_{JokerType}` ; dos via `GetBack()` (champ Card Back du SkinSet, pas la liste Faces). États Normal/Playable/Selected (relevé 24 px sur `_visualRoot`, hitbox inchangée)/FaceDown. Placeholder explicite si sprite absent, avec un seul warning par clé. Prefab 240×336 (5:7). `UnityEngine.UI.Text` (pas de TMP). Avancement au 31/10/2026 : skins nettoyés, prefab en place, faces réelles visibles, sprites assignés (rangs standards + Jokers Noir et Couleur). Sprite de dos Back désormais assigné au champ Card Back (liste Faces passée de 55 à 54), dos visibles, validé le 02/11/2026.

**Dépendances :** T53.

**Critères :** Les skins ne modifient ni tailles ni hitboxes ; aucune logique de jeu ; éventail/chevauchements gérés par ZoneView/PlayerSeatView. Vérifier les 4 états et leur réversibilité, la hitbox en Selected et un seul warning par clé.

**Tests :** Phase 5 : mapping Card → clé, unicité/couverture des 63 cartes.

**Hors périmètre :** Sprites absents volontaires : Cavalier (4), Prêtre (4), Joker de Verre (1), soit 9 cartes (illustrations hors périmètre). Hauteur SeatSelf, Spacing HandContainer et éventail définitif reportés. T54b (TableColor/SeatColor), TMP, animations, logique et tests. Commit sur `T54-card-view`. Ne pas merger (bloqué T51).

---

## T55 — Input humain, surbrillance, relevé, phase d'échange pré-partie

**Statut :** À FAIRE

**Contexte :** L'humain joue, ramasse, choisit une FaceDown, fait un Don du 7 et participe à la phase d'échange. La Présentation collecte des intentions, le Core décide et tout rejet métier est affiché. Règles confirmées par Ekinox : seuls les Jokers sont joués seuls ; sinon, autant de cartes de même rang qu'on veut. La phase d'échange dure 1 min 30 et le bouton « Prêt ! » y met fin immédiatement (serveur à l'avenir). Les échanges main↔FaceUp sont infinis dans la limite de temps. Durée de 90 s décidée, ne pas modifier (Ekinox sait où la changer). Don du 7 : sélectionner une carte puis cliquer sur le siège d'un joueur. TurnManager/GameRunner font foi (à relire avant de coder). Pré-requis T55-0 (dos de carte) : FAIT/validé.

**Fichiers :** `Controllers/InputHandler.cs`, `Models/HumanSelectionState.cs`, `UI/ActionBarView.cs`, `UI/GiftPanelView.cs`, `UI/SwapPhaseView.cs`.

**Travail :**

- **T55-1 — HumanSelectionState :** C# pur : Add/Remove/Toggle/Clear. Même rang s'ajoute, un autre rang remplace, Joker remplace et reste seul, carte étrangère ignorée. Événement Changed une fois par modification. Clear à chaque changement de joueur et après intention.
- **T55-2 — Surbrillance :** Via `ICardPlayabilityChecker`, `SetState(Playable/Normal)`, recalcul à chaque `StateChanged` seulement si l'humain est actif ; Selected prioritaire ; projection pure testable.
- **T55-3 — Sélection/relevé et boutons :** Boutons Jouer/Ramasser actifs selon le Core. Rejet = message court et sélection conservée ; succès = Clear.
- **T55-4 — FaceDown aveugle :** `PlayerSeatView.FaceDownIndexClicked(int)`, index seul, jamais de Card. Actif seulement si main et FaceUp vides et autorisé par le Core.
- **T55-5 — Don du 7 :** Une carte, puis `SeatClicked(int playerId)` sur sièges éligibles. Valider désactivé tant que carte ou destinataire manque, jamais contournable, pas d'Annuler.
- **T55-6 — Phase d'échange :** 1 carte main ↔ 1 FaceUp, `RenderVisible` sans GameState, bouton Prêt !, fin automatique après 90 s, compte à rebours visible, minuteur pur `Tick/Remaining/IsExpired/Skip` derrière `ISwapPhaseClock`.

**Dépendances :** T54, T55a (✅ test FaceUp public adverse corrigé, 89/89).

**Critères :** Aucune intention invalide transmise hors rejet Core ; Don non contournable ; aucun `if` de règle dans Controllers/ ou UI/ ; aucune Card FaceDown adverse ou main adverse lue ; tests NUnit EditMode uniquement ; couverture HumanSelectionState ≥ 90 % ; tests existants verts (89/89).

**Tests :** À prévoir en NUnit EditMode, quand les tests reprennent : chaque sous-étape, dont N échanges acceptés avant expiration puis refusés après, minuteur 90 s, Skip et Valider du Don. Ekinox a demandé de ne pas écrire de tests pendant la session de préparation ; la conception doit rester testable.

**Hors périmètre :** Tests pendant la session de préparation (sur demande d'Ekinox). Plan pas à pas : 0 branche `T55-human-input` + relire TurnManager/GameRunner ; 1 conception validée ; 2 HumanSelectionState ; 3 surbrillance ; 4 boutons ; 5 FaceDown ; 6 Don ; 7 échange + minuteur ; 8 Run All, test manuel, commit, merge.

---

## T60 — Performance (atlas de sprites, draw calls)

**Statut :** À FAIRE

**Contexte :** Fixer une cible de fluidité dès le début.

**Fichiers :** —

**Travail :** Regrouper les sprites d'un même skin dans un Sprite Atlas unique. Un draw call = une instruction de rendu envoyée au GPU ; moins de draw calls = moins de charge sur téléphone. Cible 60 fps sur appareil d'entrée de gamme ; mesure avant/après au Profiler Unity consignée dans le ticket.

**Dépendances :** T54.

**Critères :** Mesure Profiler consignée.

**Tests :** —

**Hors périmètre :** —. Ordre proposé : T60 avant T56, car l'atlas conditionne le chargement des sprites dont dépendent les animations.

---

## T56 — Animations et vitesse de jeu

**Statut :** À FAIRE

**Contexte :** —

**Fichiers :** `Controllers/AnimationController.cs`.

**Travail :** Animations pilotées par `IGameObserver` (pose, pioche, ramassage, destruction), sans aucune décision. Multiplicateurs humain ×0.5, ×1, ×2. Mode accéléré bots seuls (humain hors jeu ou terminé) à vitesse maximale, y compris les boucles longues.

**Dépendances :** T55 (T60 recommandé avant).

**Critères :** La vitesse n'influe jamais sur l'état de jeu ; partie bots seuls vitesse max fluide ; un mode instantané ne saute jamais un événement d'`IGameObserver`. Note de revue : scinder en (a) multiplicateurs et calcul de durée, (b) mode bots seuls vitesse max.

**Tests :** Calcul de durée selon multiplicateur.

**Hors périmètre :** Toute décision de jeu par les animations.

---

## T57 — Bandeau d'effets

**Statut :** À FAIRE

**Contexte :** —

**Fichiers :** `UI/EffectBannerView.cs`.

**Travail :** Bandeau court (« Doublon ! », « Don du 7 », « Valet : sens inversé », « Carré ! », etc.) piloté par `IGameObserver` / `EffectKind`.

**Dépendances :** T56.

**Critères :** Un bandeau par effet détecté ; jamais déclenché par la Présentation.

**Tests :** Mapping `EffectKind` → libellé, avec un test bouclant sur `Enum.GetValues(typeof(EffectKind))` qui échoue si un libellé manque.

**Hors périmètre :** Déclenchement d'effet par la Présentation.

---

## T58 — Écran de fin de partie

**Statut :** À FAIRE

**Contexte :** —

**Fichiers :** `UI/EndGameView.cs`.

**Travail :** Affichage du Psycko et du classement depuis `GameResult` ; boutons « Rejouer » et « Quitter ».

**Dépendances :** T51.

**Critères :** Aucun calcul de classement en Présentation. Note de revue : préciser si « Rejouer » recrée un GameState sans recharger la scène.

**Tests :** —

**Hors périmètre :** Calcul de classement en Présentation.

---

## T59 — Scène SkinPreview et menu de sélection

**Statut :** À FAIRE

**Contexte :** —

**Fichiers :** `Scenes/SkinPreview.unity`, `Controllers/SkinPreviewController.cs`.

**Travail :** Affichage des 63 cartes avec le skin courant ; changer de SkinSet sans relancer la scène, changement en direct.

**Dépendances :** T54.

**Critères :** 63 cartes visibles sur une seule scène ; changement instantané. Note : tracer l'absence des sprites Prêtre et Cavalier (54 sur 63 dans Faces, plus Joker de Verre à vérifier).

**Tests :** —

**Hors périmètre :** —
