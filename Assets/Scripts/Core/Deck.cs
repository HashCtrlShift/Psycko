using System;
using System.Collections.Generic;
public class Deck
{
    private readonly List<Card> _cards;
    private readonly Random _random;
    
    // Constructeur : initialise un deck complet de 63 cartes
    public Deck(int seed = -1)
    {
        _random = seed >= 0 ? new Random(seed) : new Random();
        _cards = new List<Card>(63);
        InitializeDeck();
    }
    
    private void InitializeDeck()
    {
        // 60 cartes standard : 4 couleurs × 15 rangs
        for (int rankIdx = 0; rankIdx <= 14; rankIdx++)
        {
            CardRank rank = (CardRank)rankIdx;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                _cards.Add(new Card(rank, suit));
            }
        }
        
        // 3 Jokers spécialisés
        _cards.Add(new Card(JokerType.Glass));
        _cards.Add(new Card(JokerType.Black));
        _cards.Add(new Card(JokerType.Color));
        
        Shuffle();
    }
    
    public void Shuffle()
    {
        // Fisher-Yates shuffle
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int randomIndex = _random.Next(i + 1);
            (_cards[i], _cards[randomIndex]) = (_cards[randomIndex], _cards[i]);
        }
    }
    
    public Card Draw()
    {
        if (_cards.Count == 0)
            throw new InvalidOperationException("Impossible de piocher : deck vide.");
        
        Card card = _cards[_cards.Count - 1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }
    
    public int Count => _cards.Count;
    
    public IReadOnlyList<Card> Peek(int count)
    {
        if (count < 0 || count > _cards.Count)
            throw new ArgumentException($"Impossible de voir {count} cartes (deck a {_cards.Count}).");
        
        return _cards.GetRange(_cards.Count - count, count).AsReadOnly();
    }
    
    public List<Card> GetAllCards()
    {
        return new List<Card>(_cards);
    }
}