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

### T38 — GameLogRecorder (ticket parent — à décomposer)

- Contexte : Fichier déjà présent (Assets/Scripts/Core/Services/GameLogRecorder.cs), rôle pressenti : tracer chaque action d'une partie pour permettre le débogage post-simulation (1M parties) et la détection de cas exceptionnels. Ticket volumineux — découpage recommandé en sous-tickets :

#### T38a — Modèle de données du log

- Définir la structure immuable d'une entrée de log (ex. GameLogEntry : type d'action, playerId, Play ou décision, état avant/après résumé, timestamp logique = numéro de tour).
- Définir le format d'un log de partie complet (GameLog : seed, liste ordonnée d'entrées, résultat final).
- Critères d'acceptation : types immuables, zéro dépendance Unity, compilation verte.

#### T38b — Intégration GameLogRecorder dans GameOrchestrator

- Décider : enregistrement opt-in (paramètre/flag injecté) ou toujours actif avec coût mémoire accepté ?
- Brancher l'enregistrement sur chaque point de décision de GameOrchestrator (ApplyPlay, RequestPickup, ApplyBlindPlay, ResolveGiftAndContinue) sans altérer leur comportement (principe : logging = side-effect pur, jamais de branche logique conditionnée par le log).
- Critères d'acceptation : GameOrchestrator produit un GameLog cohérent et complet pour une partie test, sans changement de comportement observable sur PlayResult.

#### T38c — Sérialisation / export du log

- Format de sortie pour analyse post-simulation (JSON ? CSV ? texte structuré ?) — à trancher selon l'usage prévu par la Console (T41).
- Export vers fichier, avec nommage incluant la seed pour traçabilité directe.
- Critères d'acceptation : un GameLog peut être sérialisé et rechargé/relu sans perte d'information exploitable.

#### T38d — Détection et extraction des cas exceptionnels

- Définir ce qu'est un "cas exceptionnel" loggable à part (exception levée, invariant violé, partie anormalement longue, etc.).
- Mécanisme d'extraction automatique : si une partie simulée lève une exception ou dépasse un seuil de tours, son GameLog + sa seed sont isolés dans un dossier/fichier dédié pour rejouabilité immédiate.
- Critères d'acceptation : une partie en échec est identifiable et rejouable seule via sa seed, sans re-simuler le million de parties.

- Dépendances globales T38 : T37 (GameSeed) pour la traçabilité, T35/T36 pour qu'il y ait des parties à logger.
- Hors périmètre global T38 : interface graphique de visualisation des logs (Presentation, hors scope actuel).

### T39 — Mise à jour de CardFormatter

- Contexte : CardFormatter (non listé explicitement dans l'arborescence fournie mais référencé comme existant) doit refléter le deck de 63 cartes (4×15 rangs + 3 Jokers), notamment Cavalier et Prêtre présents dès le modèle V1.
- Fichiers concernés : fichier CardFormatter.cs (localisation à confirmer — probablement Psycko.Core/Domain/ ou un utilitaire partagé Console/Presentation).
- Règles CLAUDE.md applicables : aucune logique métier dans le formatage — pur affichage/texte ; le modèle de données (DefRank, DefCard) reste la seule source de vérité des valeurs.
- Travail attendu :

    - Vérifier que tous les rangs (3 à As, 2, Prêtre, Valet, Cavalier, Dame, Roi) ont un libellé texte correct.
    - Vérifier le format des 3 Jokers (Verre, Noir/Passe, Couleur/Bombe) — nom court et nom long si distincts.
    - Vérifier la cohérence du format utilisé par GameLogRecorder (T38) et la future Console (lisibilité des logs en texte).
Supprimer tout résidu de l'ancien deck (si CardFormatter date d'avant le reset).

- Hors périmètre : rendu visuel (sprites, couleurs UI) — relève de Presentation.
- Dépendances : aucune bloquante ; utile avant T38c (sérialisation lisible) et T41 (Console).
- Critères d'acceptation :

- Toutes les 63 valeurs de cartes ont un format texte correct et testé.
- Aucune référence à l'ancien deck pré-reset.

- Pistes de tests NUnit futurs : test paramétré sur les 63 cartes vérifiant le format attendu pour chacune.

### T40 — Mise à jour de CardSymbols

- Contexte : Pendant de CardFormatter, probablement dédié aux symboles courts/unicode ou codes utilisés en log compact et/ou futurs tests.
- Fichiers concernés : fichier CardSymbols.cs (localisation à confirmer).
- Règles CLAUDE.md applicables : mêmes principes que T39 — zéro logique métier, simple table de correspondance.
- Travail attendu :

    - Vérifier/compléter les symboles pour les 15 rangs et les 3 Jokers du nouveau deck.
    - Vérifier la distinction visuelle/textuelle claire entre les 3 Jokers (éviter ambiguïté Noir/Couleur).
    - Aligner avec CardFormatter (T39) pour cohérence inter-fichiers (pas de divergence de nommage entre les deux).

- Hors périmètre : assets graphiques réels (skins, sprites) — Presentation / travail indépendant d'Ekinox.
- Dépendances : cohérent avec T39, à traiter ensemble ou immédiatement l'un après l'autre.
- Critères d'acceptation :

- Table complète des 63 cartes + symboles, sans doublon ni ambiguïté.
- Compilation verte, couverture par test si logique de lookup non triviale.

- Pistes de tests NUnit futurs : vérifier unicité des symboles, vérifier correspondance bijective avec DefRank/DefCard.

### T41 — Tools/PsyckoConsole/ (ticket parent — à décomposer)

- Contexte : Application console dotnet dédiée à la simulation de parties complètes (objectif 1M parties) pour valider Core + Bots avant d'attaquer Presentation. Gros ticket — découpage recommandé :

#### T41a — Squelette projet console

- Création du projet dotnet console dans Tools/PsyckoConsole/, référence à Psycko.Core et Psycko.Bots uniquement (zéro Unity).
- Point d'entrée minimal : lance une seule partie avec 4 RandomBot, affiche le résultat final.
- Critères d'acceptation : projet compile et exécute une partie de bout en bout via dotnet run.

#### T41b — Boucle de partie unique instrumentée

- Orchestration complète d'une partie : initialisation GameState (deck 63 cartes), boucle tant que !GameResultCalculator.IsGameOver, appel séquentiel des agents via IPlayerAgent selon ActivePlayerIndex.
- Gestion des 3 chemins de décision (pose, pickup, Don, FaceDown Luck) en interrogeant l'agent actif à chaque étape.
- Détection et arrêt propre sur blocage (ex. boucle infinie potentielle — seuil de tours max configurable).
- Critères d'acceptation : partie simulée du début à la fin déterministe avec une seed donnée, sans intervention manuelle.

#### T41c — Intégration GameLogRecorder + GameSeed dans la console

- Chaque partie simulée est loggée (T38) avec sa seed (T37).
- Export des logs des parties en échec (exception/incohérence) selon le mécanisme T38d.
- Critères d'acceptation : une partie en échec produit un fichier log exploitable et rejouable isolément.

#### T41d — Boucle de simulation massive (N parties)

- Paramétrage : nombre de parties à simuler (jusqu'à 1M), seed de départ, incrémentation des seeds.
- Compteurs agrégés : nombre de parties terminées normalement, nombre d'exceptions, distribution des longueurs de partie, répartition des victoires par siège (détection de biais de premier joueur par ex.).
- Affichage de progression (ex. tous les 10 000 parties) sans ralentir drastiquement l'exécution.
- Critères d'acceptation : exécution de 1M parties sans crash du process lui-même (hors parties individuellement en échec, qui sont isolées et continuent la boucle).

- Hors périmètre global T41 : interface graphique, intégration Unity, multijoueur réseau.
- Dépendances globales T41 : T35, T36 (Bots fonctionnels), T37 (GameSeed), T38 (GameLogRecorder), T39/T40 (formatage lisible des logs).

### T42 — Simulation (exécution et analyse des 1M parties)

- Contexte : Ticket d'exécution, distinct du développement de l'outil (T41) : il s'agit de faire tourner réellement la simulation à grande échelle et d'analyser les résultats pour valider (ou invalider) le Core avant Presentation.
- Fichiers concernés : aucun nouveau fichier de production — résultats stockés en logs/exports (T38c) et rapport d'analyse (document, potentiellement mis à jour dans Notion).
- Règles CLAUDE.md applicables : aucune règle métier modifiée à ce stade — ce ticket est une validation, pas une implémentation. Toute anomalie détectée doit générer un ticket de correction séparé (traçabilité 1 ticket = 1 responsabilité).
- Travail attendu :

    - Lancer la simulation complète (1M parties) via Tools/PsyckoConsole/.
    - Collecter les métriques : taux d'échec (exceptions/incohérences), distribution des durées de partie, équité statistique entre sièges, fréquence d'activation de chaque carte spéciale/Joker (validation indirecte que toutes les règles sont bien exercées au moins une fois).
    - Pour chaque échec détecté, isoler la seed concernée et ouvrir un ticket de correction dédié dans Core (avec repro minimal garanti par la seed).
    - Rapport de synthèse final (nombre de tickets de correction ouverts, taux de réussite global, recommandation GO/NO-GO vers Bots avancés + NUnit + Presentation).

- Hors périmètre : correction des bugs eux-mêmes (tickets séparés ouverts en conséquence) ; rédaction des tests NUnit définitifs (ticket futur distinct, nourri par cette analyse).
- Dépendances : T35 à T41 clos et fonctionnels.
- Critères d'acceptation :

- 1M parties exécutées, rapport chiffré produit.
- Toute partie en échec a sa seed isolée et un ticket de correction ouvert si nécessaire.
- Décision explicite actée avec Ekinox : GO vers rédaction des tests NUnit + Presentation, ou itération supplémentaire sur Core/Bots.

### T43 — Garantie structurelle de ProposeFaceDownPlay (dette technique issue de T35)

- Contexte : lors de la conception du contrat IPlayerAgent (T35), une tension a été identifiée entre deux règles validées:
- IPlayerVisibleState.SelfFaceDown expose le contenu complet de la couche 3 du joueur lui-même (nécessaire pour d'autres usages de lecture).
- ProposeFaceDownPlay doit choisir une carte à l'aveugle, par position uniquement — son contenu ne doit jamais être inspecté avant la décision (cohérence avec la règle du jeu : les FaceDown sont révélées une à une, sans anticipation).

- Problème : une interface C# standard ne peut pas interdire techniquement à un bot mal écrit de lire state.SelfFaceDown[i] avant de jouer. La garantie actuelle repose uniquement sur une discipline documentée en XML doc, pas sur le type lui-même. RandomBot (T36) respecte cette discipline nativement (choix par index aléatoire, sans lecture de contenu), mais un futur bot stratégique mal écrit pourrait tricher silencieusement.
- Travail attendu : trancher et, si retenu, implémenter une garantie structurelle plus forte, par exemple :
Créer une vue dédiée et plus stricte pour ProposeFaceDownPlay (ex. n'exposant que SelfFaceDownCount, sans accès au contenu des cartes), distincte de IPlayerVisibleState standard.
Ou documenter formellement et définitivement le compromis actuel comme acceptable si le coût d'un nouveau type est jugé disproportionné.

- Fichiers concernés (pressentis) : Psycko.Core.Interfaces/IPlayerVisibleState.cs, Psycko.Bots/IPlayerAgent.cs.
Hors périmètre : toute réécriture des bots existants tant que ce ticket n'est pas tranché.
- Dépendances : T35 (clos).
- Statut : 🔵 Dette technique notée, non bloquante pour T36 (RandomBot). À trancher avant qu'un bot stratégique (MCTS, heuristique) soit implémenté.

### Résumé de l'ordre d'exécution recommandé :
T35 → T36 → T37 → T39/T40 (en parallèle, indépendants) → T38a → T38b → T38c → T38d → T41a → T41b → T41c → T41d → T42 → T43