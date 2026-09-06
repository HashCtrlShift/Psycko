using System;
using System.Collections.Generic;
using System.Linq;
using Psycko;

namespace Psycko.Bots
{
    public class HumanPlayerAgent : IPlayerAgent
    {
        public HumanPlayerAgent()
        {
        }

        public List<Card> ChooseCards(Player player, GameState gameState)
        {
            while (true)
            {
                Console.WriteLine("\n📋 Choisissez votre action :");
                Console.WriteLine("  - Entrez les indices de cartes à jouer (ex: '0,1,2' pour jouer 3 cartes)");
                Console.WriteLine("  - Laissez vide ou tapez 'ramasser' pour ramasser");
                Console.Write("> ");

                string input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(input) || input == "ramasser")
                {
                    return null; // Ramasser
                }

                // Parser les indices
                string[] parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<Card> selectedCards = new List<Card>();

                bool valid = true;
                foreach (string part in parts)
                {
                    if (int.TryParse(part, out int idx))
                    {
                        if (idx >= 0 && idx < player.Hand.Count)
                        {
                            selectedCards.Add(player.Hand[idx]);
                        }
                        else
                        {
                            Console.WriteLine($"❌ Indice {idx} invalide (main : 0-{player.Hand.Count - 1})");
                            valid = false;
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Format invalide : {part}");
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    continue;
                }

                if (selectedCards.Count == 0)
                {
                    Console.WriteLine("❌ Sélectionnez au moins une carte.");
                    continue;
                }

                if (selectedCards.Count > 4)
                {
                    Console.WriteLine("❌ Maximum 4 cartes par coup.");
                    continue;
                }

                // Vérifier que toutes les cartes ont le même rang
                CardRank? expectedRank = null;
                bool sameRank = true;
                foreach (Card card in selectedCards)
                {
                    if (!card.IsJoker)
                    {
                        if (expectedRank == null)
                            expectedRank = card.Rank;
                        else if (card.Rank != expectedRank.Value)
                        {
                            sameRank = false;
                            break;
                        }
                    }
                }

                if (!sameRank)
                {
                    Console.WriteLine("❌ Toutes les cartes doivent être du même rang.");
                    continue;
                }

                return selectedCards;
            }
        }

        public Card? ChooseGiftCard(Player giver, Player receiver)
        {
            Console.WriteLine($"\n🎁 Vous avez joué un 7 ! Choisissez une carte à donner à {receiver.Name}.");
            Console.WriteLine($"Votre main : {string.Join(", ", giver.Hand.Select((c, i) => $"[{i}]{c}"))}");
            Console.Write("Indice de la carte : ");

            if (int.TryParse(Console.ReadLine(), out int idx) &&
                idx >= 0 && idx < giver.Hand.Count)
            {
                return giver.Hand[idx];
            }

            Console.WriteLine("❌ Indice invalide, pas de don.");
            return null;
        }
    }
}