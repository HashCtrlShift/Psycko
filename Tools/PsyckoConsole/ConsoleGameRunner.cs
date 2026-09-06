using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Psycko;
using Psycko.Core;
using Psycko.Bots;

namespace Psycko.Console
{
    public class ConsoleGameRunner
    {
        private GameState _gameState;
        private List<Player> _players;
        private IPlayerAgent[] _agents;
        private Random _random = new Random();
        private StreamWriter _logWriter;
        private int _turnCounter = 0;

        // Couleurs ANSI
        private const string ColorReset = "\u001b[0m";
        private const string ColorSpades = "\u001b[34m";    // Bleu (piques)
        private const string ColorHearts = "\u001b[31m";    // Rouge (cœurs)
        private const string ColorClubs = "\u001b[32m";     // Vert (trèfles)
        private const string ColorDiamonds = "\u001b[33m";  // Jaune (carreaux)

        public ConsoleGameRunner(string logFilePath = "psycko_game.log")
        {
            try
            {
                _logWriter = new StreamWriter(logFilePath, append: true)
                {
                    AutoFlush = true
                };
            }
            catch
            {
                _logWriter = null;
            }
        }

        private void Log(string message)
        {
            Console.Write(message);
            _logWriter?.Write(message);
        }

        private void LogLine(string message)
        {
            Console.WriteLine(message);
            _logWriter?.WriteLine(message);
        }

        public void Start()
        {
            LogLine("\n========== PARTIE PSYCKO ==========\n");

            // Créer les joueurs
            _players = new List<Player>
            {
                new Player("P0", "Toi"),
                new Player("P1", "Bot1"),
                new Player("P2", "Bot2"),
                new Player("P3", "Bot3")
            };

            // Récupérer le seed
            Console.Write("Seed racine (laisser vide pour aléatoire): ");
            string seedInput = Console.ReadLine();
            int seed = string.IsNullOrWhiteSpace(seedInput) ? -1 : int.Parse(seedInput);

            // Créer l'état de jeu
            _gameState = new GameState(players: _players, pile: new Pile(), deck: new Deck(seed));

            // Créer les agents
            _agents = new IPlayerAgent[4];
            _agents[0] = new HumanPlayerAgent(this);
            _agents[1] = new RandomBot(_random);
            _agents[2] = new RandomBot(_random);
            _agents[3] = new RandomBot(_random);

            // Phase d'échange pré-jeu
            RunPreGameSwapPhase();

            // Distribuer les 9 cartes initiales
            DistributeInitialCards();

            // Boucle principale de jeu
            RunGameLoop();

            // Fin de partie
            Player loser = _gameState.TurnManager.GetLoser();
            if (loser != null)
            {
                LogLine($"\n🎉 {loser.Name} est le Psycko !\n");
            }

            _logWriter?.Close();
        }

        private void RunPreGameSwapPhase()
        {
            LogLine("\n=== Phase d'échange pré-jeu ===\n");

            // État : pour chaque joueur, "Ready" ou pas
            bool[] playerReady = new bool[4];

            while (!playerReady.All(r => r))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (playerReady[i])
                        continue; // Déjà prêt, passer

                    Player player = _players[i];
                    IPlayerAgent agent = _agents[i];

                    if (agent is HumanPlayerAgent)
                    {
                        // Humain : boucle libre jusqu'à "Prêt !"
                        bool playerDone = false;
                        while (!playerDone)
                        {
                            LogLine($"\n--- Échange de {player.Name} (P{i}) ---");
                            LogLine($"Face-up: {FormatCardList(player.FaceUp)}");
                            LogLine($"Main:    {FormatCardList(player.Hand)}");
                            LogLine("Format: '0,1' pour swapper face-up[0] ↔ main[1], ou 'prêt' pour terminer");
                            Console.Write("Choix : ");

                            string input = Console.ReadLine()?.Trim().ToLower();

                            if (input == "prêt")
                            {
                                playerReady[i] = true;
                                LogLine($"✓ {player.Name} est prêt !");
                                playerDone = true;
                            }
                            else if (string.IsNullOrWhiteSpace(input))
                            {
                                LogLine("❌ Entrée invalide.");
                            }
                            else
                            {
                                // Parser "0,1"
                                string[] parts = input.Split(',');
                                if (parts.Length == 2 &&
                                    int.TryParse(parts[0].Trim(), out int faceUpIdx) &&
                                    int.TryParse(parts[1].Trim(), out int handIdx) &&
                                    faceUpIdx >= 0 && faceUpIdx < player.FaceUp.Count &&
                                    handIdx >= 0 && handIdx < player.Hand.Count)
                                {
                                    // Swap
                                    Card temp = player.FaceUp[faceUpIdx];
                                    player.FaceUp[faceUpIdx] = player.Hand[handIdx];
                                    player.Hand[handIdx] = temp;
                                    LogLine($"Échange effectué ✓");
                                }
                                else
                                {
                                    LogLine("❌ Format invalide.");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Bot : 0-3 échanges aléatoires
                        int swapCount = _random.Next(4); // 0, 1, 2 ou 3
                        LogLine($"\n--- Échange de {player.Name} (P{i}) ---");

                        for (int swap = 0; swap < swapCount; swap++)
                        {
                            if (player.FaceUp.Count > 0 && player.Hand.Count > 0)
                            {
                                int faceUpIdx = _random.Next(player.FaceUp.Count);
                                int handIdx = _random.Next(player.Hand.Count);

                                Card temp = player.FaceUp[faceUpIdx];
                                player.FaceUp[faceUpIdx] = player.Hand[handIdx];
                                player.Hand[handIdx] = temp;

                                LogLine($"  Swap {swap + 1}/{swapCount}: face-up[{faceUpIdx}] ↔ main[{handIdx}] ✓");
                            }
                        }

                        playerReady[i] = true;
                        LogLine($"✓ {player.Name} est prêt !");
                    }
                }
            }
        }

        private void DistributeInitialCards()
        {
            // Distribuer 9 cartes par joueur (3 face cachée, 3 face découverte, 3 en main)
            for (int i = 0; i < 9; i++)
            {
                foreach (Player player in _players)
                {
                    Card card = _gameState.Deck.Draw();

                    if (i < 3)
                        player.FaceDown.Add(card);
                    else if (i < 6)
                        player.FaceUp.Add(card);
                    else
                        player.Hand.Add(card);
                }
            }

            LogLine("\n=== Distribution initiale ===");
            foreach (Player player in _players)
            {
                LogLine($"{player.Name} : 3 face cachée, 3 face découverte, 3 en main (total: {player.TotalCards})");
            }
        }

        private void RunGameLoop()
        {
            LogLine("\n=== Début de la partie — La Travail ===\n");

            while (!_gameState.TurnManager.IsGameOver())
            {
                _turnCounter++;
                Player currentPlayer = _gameState.TurnManager.CurrentTurn.CurrentPlayer;

                DisplayTurnStart(currentPlayer);

                // Joueur choisit son action
                List<Card> cardsToPlay = _agents[_players.IndexOf(currentPlayer)].ChooseCards(currentPlayer, _gameState);

                if (cardsToPlay == null || cardsToPlay.Count == 0)
                {
                    // Ramasser
                    HandlePlayerPickup(currentPlayer);
                }
                else
                {
                    // Jouer les cartes
                    HandleCardsPlayed(currentPlayer, cardsToPlay);
                }

                // Vérifier transition de phase
                PhaseTransitionResult phaseResult = _gameState.CheckPhaseTransition(currentPlayer);
                if (phaseResult != PhaseTransitionResult.NoChange)
                {
                    LogLine($"\n→ {currentPlayer.Name} passe à la phase suivante : {_gameState.CurrentPhase}");
                }
            }
        }

        private void DisplayTurnStart(Player player)
        {
            LogLine($"\n--- Tour de {player.Name} ---");
            LogLine($"Phase: {_gameState.CurrentPhase} | Pile: {_gameState.Pile.Count} carte(s)");
            LogLine($"Main: {FormatCardListWithIndices(player.Hand)}");

            if (_gameState.Pile.Count > 0)
            {
                Card topCard = _gameState.Pile.Top();
                LogLine($"Sommet pile: {FormatCard(topCard)}");
            }
        }

        private void HandleCardsPlayed(Player player, List<Card> cards)
        {
            // Valider et poser toutes les cartes
            foreach (Card card in cards)
            {
                if (!player.Hand.Contains(card))
                {
                    LogLine($"❌ {card} n'est pas en main.");
                    return;
                }

                if (!_gameState.IsPlayable(card))
                {
                    LogLine($"❌ {card} n'est pas jouable.");
                    return;
                }

                player.RemoveCardFromHand(card);
                _gameState.Pile.Add(card);
            }

            // Afficher la pose
            LogLine($"{player.Name} joue: {string.Join(", ", cards.Select(FormatCard))}");

            // Appliquer les effets du premier coup (tous les effets se basent sur une seule pose)
            Card firstCard = cards.First();
            bool pileDestroyed = false;
            DestructionReason destroyReason = DestructionReason.Two; // Placeholder

            // Carré ?
            if (_gameState.DetectSquare(_gameState.Pile))
            {
                LogLine("🎲 Carré détecté ! Pile détruite.");
                destroyReason = DestructionReason.Square;
                pileDestroyed = true;
            }

            // Doublon ? (avant mise à jour du tracker)
            int activePlayerCount = _gameState.TurnManager.GetActivePlayerCount();
            EffectResolver effectResolver = GetEffectResolver();
            if (effectResolver.DetectDoublet(firstCard, activePlayerCount))
            {
                LogLine("💎 Doublon ! Pile détruite + joueur suivant offre une carte.");
                destroyReason = DestructionReason.Square; // Arbitrairement (pas de DestructionReason.Doublet)
                pileDestroyed = true;
            }

            // Mettre à jour le tracker Doublon
            effectResolver.UpdateLastSignificantRank(firstCard);

            // Joker Couleur (Bombe) ?
            if (firstCard.IsJoker && firstCard.JokerType == JokerType.Color)
            {
                LogLine("💣 Joker Couleur ! Pile détruite, joueur suivant ouvre.");
                destroyReason = DestructionReason.Bomb;
                pileDestroyed = true;
            }

            // Carte "2" ?
            if (firstCard.Rank == CardRank.Two)
            {
                // Vérifier si c'est la dernière carte avant transition de phase
                PhaseTransitionResult transitionResult = _gameState.CheckPhaseTransition(player);

                if (transitionResult != PhaseTransitionResult.NoChange)
                {
                    // Dernière carte : joueur ramasse
                    LogLine("2️⃣ Carte 2 jouée en fin de phase ! Ramassage forcé.");
                    HandlePlayerPickup(player);
                }
                else
                {
                    // Cas normal : pile détruite, même joueur rejoue
                    LogLine("2️⃣ Carte 2 jouée ! Pile détruite, rejeu.");
                    destroyReason = DestructionReason.Two;
                    pileDestroyed = true;
                }
            }

            // Prêtre ?
            if (firstCard.Rank == CardRank.Priest)
            {
                LogLine("🙏 Prêtre joué ! Prochain joueur limité à ≤ 8.");
                effectResolver.ActivatePriestBlock();
            }

            // Valet ?
            if (firstCard.Rank == CardRank.Jack)
            {
                LogLine("🔄 Valet joué ! Sens inversé.");
                _gameState.TurnManager.ReverseDirection();
            }

            // 7 (Don) ? ← APRÈS la destruction de pile éventuelle et AVANT RefillHand
            if (firstCard.Rank == CardRank.Seven && 
                (_gameState.CurrentPhase == GamePhase.Travail || _gameState.CurrentPhase == GamePhase.Talent))
            {
                Player nextPlayer = GetNextPlayer(player);
                if (nextPlayer != null)
                {
                    Card giftCard = _agents[_players.IndexOf(player)].ChooseGiftCard(player, nextPlayer);
                    if (giftCard != null && player.Hand.Contains(giftCard))
                    {
                        player.RemoveCardFromHand(giftCard);
                        nextPlayer.AddCardToHand(giftCard);
                        LogLine($"🎁 {player.Name} donne {FormatCard(giftCard)} à {nextPlayer.Name}");
                    }
                }
            }

            // Remplir la main du joueur courant (après le don éventuel)
            RefillHand(player);

            // Détruire la pile si nécessaire
            if (pileDestroyed)
            {
                effectResolver.ResolvePileDestruction(_gameState.Pile, destroyReason);
            }

            // Remplir la main du joueur courant (s'il en a besoin)
            RefillHand(player);

            // Gestion des tours après effets
            if (firstCard.Rank == CardRank.Two && _gameState.CheckPhaseTransition(player) == PhaseTransitionResult.NoChange)
            {
                // Carte 2 normale : pas d'avancement (rejeu)
            }
            else if (firstCard.IsJoker && firstCard.JokerType == JokerType.Color)
            {
                // Bombe : joueur suivant ouvre
                _gameState.TurnManager.HandleBombPlayed();
            }
            else if (firstCard.Rank == CardRank.Square) // Carré (arbitraire)
            {
                // Carré : rejeu
            }
            else
            {
                // Action normale : joueur suivant
                _gameState.TurnManager.AdvanceToNextPlayer();
            }
        }

        private void HandlePlayerPickup(Player player)
        {
            int pileSize = _gameState.Pile.Count;
            LogLine($"[Ramassage] {player.Name} ramasse {pileSize} carte(s).");

            // Ajouter toutes les cartes de la pile à la main du joueur
            foreach (Card card in _gameState.Pile.Cards)
            {
                player.AddCardToHand(card);
            }

            // Vider la pile
            while (!_gameState.Pile.IsEmpty())
            {
                _gameState.Pile.Pop();
            }

            // Réinitialiser les trackers d'effets
            EffectResolver effectResolver = GetEffectResolver();
            effectResolver.ResetPriestBlock();
            effectResolver.ResetDoubletTracker();

            // Joueur suivant ouvre
            _gameState.TurnManager.HandlePlayerPickedUp();

            RefillHand(player);
        }

        private void RefillHand(Player player)
        {
            while (player.Hand.Count < 3 && !_gameState.Deck.IsEmpty())
            {
                player.AddCardToHand(_gameState.Deck.Draw());
            }
        }

        private Player GetNextPlayer(Player currentPlayer)
        {
            int currentIndex = _players.IndexOf(currentPlayer);
            if (currentIndex < 0)
                return null;

            GameDirection direction = _gameState.TurnManager.CurrentTurn.Direction;
            int nextIndex = direction == GameDirection.Clockwise
                ? (currentIndex + 1) % _players.Count
                : (currentIndex - 1 + _players.Count) % _players.Count;

            return _players[nextIndex];
        }

        private EffectResolver GetEffectResolver()
        {
            // À implémenter : récupérer l'instance EffectResolver du GameState
            // Pour l'instant, placeholder
            return new EffectResolver();
        }

        // --- Utilitaires d'affichage ---

        private string FormatCard(Card card)
        {
            if (card.IsJoker)
            {
                return card.JokerType switch
                {
                    JokerType.Glass => "🔮 Verre",
                    JokerType.Black => "⚫ Noir",
                    JokerType.Color => "🟡 Bombe",
                    _ => "? Joker"
                };
            }

            string rank = card.Rank switch
            {
                CardRank.Three => "3",
                CardRank.Four => "4",
                CardRank.Five => "5",
                CardRank.Six => "6",
                CardRank.Seven => "7",
                CardRank.Eight => "8",
                CardRank.Nine => "9",
                CardRank.Ten => "10",
                CardRank.Priest => "P",
                CardRank.Jack => "V",
                CardRank.Knight => "C",
                CardRank.Queen => "D",
                CardRank.King => "R",
                CardRank.Ace => "A",
                CardRank.Two => "2",
                _ => "?"
            };

            string suit = card.Suit switch
            {
                CardSuit.Spades => "♠",
                CardSuit.Hearts => "♥",
                CardSuit.Clubs => "♣",
                CardSuit.Diamonds => "♦",
                _ => "?"
            };

            string suitColor = card.Suit switch
            {
                CardSuit.Spades => ColorSpades,
                CardSuit.Hearts => ColorHearts,
                CardSuit.Clubs => ColorClubs,
                CardSuit.Diamonds => ColorDiamonds,
                _ => ColorReset
            };

            return $"{suitColor}{rank}{suit}{ColorReset}";
        }

        private string FormatCardList(List<Card> cards)
        {
            return $"[{string.Join(", ", cards.Select(FormatCard))}]";
        }

        private string FormatCardListWithIndices(List<Card> cards)
        {
            return $"[{string.Join(", ", cards.Select((c, i) => $"{i}]{FormatCard(c)}"))}]";
        }
    }
}