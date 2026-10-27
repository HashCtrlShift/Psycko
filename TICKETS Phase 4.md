TICKETS Phase 4 — Présentation locale (GameplayLocal)
Phase 4 : interface Unity 2D, jouable en local (1 humain + 3 bots), paysage uniquement. Zéro logique de jeu dans la Présentation. Toute décision passe par GameRunner / GameOrchestrator et les contrats existants.
Hors périmètre Phase 4 : Photon Fusion, PlayFab, Unity IAP, sauvegarde/reprise de partie, mode spectateur depuis GameLog, options d'accessibilité daltonisme et taille de carte (reportées Phase 5).

T50 — Bootstrap scène, asmdef, paysage verrouillé

Statut : À FAIRE
Contexte : poser le socle Unity (scène, assemblies, orientation) avant toute logique.
Fichiers : Assets/Scenes/GameplayLocal.unity, Assets/Scripts/Presentation/Psycko.Presentation.asmdef (références : Psycko.Core, Psycko.Bots).
Règles CLAUDE.md : Presentation dépend Core + Bots, zéro logique de jeu.
Travail :
Player Settings : Default Orientation = Landscape Left, Auto Rotation limitée à Landscape Left / Landscape Right, Portrait désactivé.
Canvas Scale With Screen Size, Reference Resolution 1920×1080, Match = 0.5.
Caméra URP 2D, fond uni #100d1f.


Dépendances : aucune.
Critères : la scène s'ouvre sans erreur ; la rotation portrait est impossible ; compilation verte.
Tests : contrôle manuel en Game View 16:9 et 19.5:9 ; aucun test NUnit (structure).

T51 — Adaptateur de partie (GameplayController, UnityHumanAgent)

Statut : À FAIRE
Contexte : lancer une partie via GameRunner (3 RandomBot + 1 humain) et recevoir les événements.
Fichiers : Controllers/GameplayController.cs, Controllers/UnityHumanAgent.cs (implémente IPlayerAgent, attend l'intention humaine).
Règles CLAUDE.md : l'humain ne propose qu'une intention brute ; la validation reste dans GameOrchestrator.
Travail :
Abonnement à IGameObserver pour les notifications.
UnityHumanAgent expose une intention asynchrone, complétée par l'UI.


Dépendances : T50.
Critères : une partie complète se termine sans exception, humain inclus (actions pilotées par code de test).
Tests NUnit EditMode : UnityHumanAgent renvoie l'intention reçue ; comportement défini tant qu'aucune intention n'est fournie.

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
T50 → T51 → T52 → T53 → T54 → T55 → T56 → T57 → T58 → T59 → T60