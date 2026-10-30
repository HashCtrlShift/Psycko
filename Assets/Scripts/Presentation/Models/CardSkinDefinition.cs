using System;
using UnityEngine;

namespace Psycko.Presentation.Models
{
    [Serializable]
    public sealed class CardSkinDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private SkinSet _skinSet;

        public string Id { get { return _id; } }
        public string DisplayName { get { return _displayName; } }
        public SkinSet SkinSet { get { return _skinSet; } }
    }
}