using System;
using System.Collections.Generic;
using UnityEngine;

namespace Psycko.Presentation.Models
{
    [CreateAssetMenu(fileName = "SkinSet_New", menuName = "Psycko/Skin Set")]
    public sealed class SkinSet : ScriptableObject
    {
        [Serializable]
        public sealed class FaceEntry
        {
            public string Key; // ex: "Hearts_Ace" ou "Joker_Glass"
            public Sprite Sprite;
        }

        [SerializeField] private List<FaceEntry> _faces = new List<FaceEntry>();
        [SerializeField] private Sprite _cardBack;
        [SerializeField] private Color _tableColor = new Color(0.07f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color _seatColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Font _font;
        [SerializeField, Min(0)] private int _fontSize = 0;   // 0 = AutoSize
        public int FontSize { get { return _fontSize; } }

        public Sprite CardBack { get { return _cardBack; } }
        public Color TableColor { get { return _tableColor; } }
        public Color SeatColor { get { return _seatColor; } }
        public Font Font { get { return _font; } }
        public IReadOnlyList<FaceEntry> Faces { get { return _faces; } }
    }
}