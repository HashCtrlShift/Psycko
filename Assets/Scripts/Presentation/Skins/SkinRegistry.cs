using System.Collections.Generic;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Models;
using UnityEngine;

namespace Psycko.Presentation.Skins
{
    public sealed class SkinRegistry : MonoBehaviour
    {
        [SerializeField] private List<CardSkinDefinition> _definitions = new List<CardSkinDefinition>();
        [SerializeField] private string _defaultSkinId = "classique";

        private readonly Dictionary<string, ICardSkin> _skins = new Dictionary<string, ICardSkin>();
        private ICardSkin _fallback;

        public ICardSkin Current { get; private set; }

        private void Awake()
        {
            foreach (var def in _definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                _skins[def.Id] = new DefaultCardSkin(def);
            }
            _fallback = Resolve(_defaultSkinId, null) ?? new DefaultCardSkin(null);
            Current = _fallback;
        }

        public ICardSkin Select(string id)
        {
            Current = Resolve(id, _fallback);
            return Current;
        }

        private ICardSkin Resolve(string id, ICardSkin fallback)
        {
            ICardSkin skin;
            if (!string.IsNullOrEmpty(id) && _skins.TryGetValue(id, out skin)) return skin;
            return fallback;
        }
    }
}