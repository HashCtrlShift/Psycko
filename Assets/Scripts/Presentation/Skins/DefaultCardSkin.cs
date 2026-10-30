using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Models;
using UnityEngine;

namespace Psycko.Presentation.Skins
{
    public sealed class DefaultCardSkin : ICardSkin
    {
        private readonly CardSkinDefinition _definition;
        private readonly Dictionary<string, Sprite> _faces = new Dictionary<string, Sprite>();

        public DefaultCardSkin(CardSkinDefinition definition)
        {
            _definition = definition;
            if (definition == null || definition.SkinSet == null) return;
            foreach (var entry in definition.SkinSet.Faces)
                if (entry != null && !string.IsNullOrEmpty(entry.Key)) _faces[entry.Key] = entry.Sprite;
        }

        public string Id { get { return _definition == null ? "default" : _definition.Id; } }
        public string DisplayName { get { return _definition == null ? "Défaut" : _definition.DisplayName; } }
        public Color TableColor { get { return HasSet ? _definition.SkinSet.TableColor : new Color(0.07f, 0.35f, 0.2f, 1f); } }
        public Color SeatColor { get { return HasSet ? _definition.SkinSet.SeatColor : new Color(0f, 0f, 0f, 0.35f); } }
        public Font Font { get { return HasSet ? _definition.SkinSet.Font : null; } }

        private bool HasSet { get { return _definition != null && _definition.SkinSet != null; } }

        public Sprite GetBack() { return HasSet ? _definition.SkinSet.CardBack : null; }

        public Sprite GetFace(Card card)
        {
            if (card == null) return null;
            Sprite sprite;
            return _faces.TryGetValue(KeyOf(card), out sprite) ? sprite : null;
        }

        public static string KeyOf(Card card)
        {
            if (card.IsJoker) return "Joker_" + card.JokerType;
            return card.Suit + "_" + card.Rank;
        }
    }
}