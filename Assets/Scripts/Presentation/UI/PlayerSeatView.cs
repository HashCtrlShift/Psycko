using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Controllers;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Vue d'un siège. Les cartes adverses privées ne sont jamais demandées ni rendues.</summary>
    public sealed class PlayerSeatView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _handCountLabel;
        [SerializeField] private Text _faceUpCountLabel;
        [SerializeField] private Text _faceDownCountLabel;
        [SerializeField] private Text _rankBanner;                 // T55-9
        [SerializeField] private GameObject _activeIndicator;
        [SerializeField] private CardView _cardViewPrefab;
        [SerializeField] private RectTransform _handContainer;
        [SerializeField] private RectTransform _faceUpContainer;
        [SerializeField] private RectTransform _faceDownContainer;
        [SerializeField] private Button _seatButton;

        [Header("Dos adverses (T55-8)")]
        [SerializeField] private bool _opponentBacksVertical;      // true : cartes alignées sur l'axe Y local du conteneur
        [SerializeField] private float _cardWidth = 140f;          // taille du prefab CardView
        [SerializeField] private float _cardHeight = 196f;
        [SerializeField] private float _handBackScale = 0.75f;     // échelle des dos de MAIN uniquement
        [SerializeField] private float _handDefaultSpacing = 40f;
        [SerializeField] private float _faceDownDefaultSpacing = 150f; // pas des FaceDown ET des FaceUp adverses
        [SerializeField] private float _handZoneLength = 480f;     // longueur utile de la ligne de main
        [SerializeField] private int _handMaxVisibleBacks = 8;     // plafond de dos de MAIN affichés
        [SerializeField] private float _faceDownZoneLength = 480f; // longueur utile de la ligne FaceDown / FaceUp
        [SerializeField] private float _minSpacing = HandSpacing.DefaultMinSpacing;
        [SerializeField] private float _tableRotationZ = 0f;       // 90 (ou -90) sur SeatLeft/SeatRight : FaceUp/FaceDown couchées

        [Header("Main humaine (T55-11)")]

        [Tooltip("Faux : petits rangs à gauche. Vrai : grands rangs à gauche. Affichage uniquement.")]
        [SerializeField] private bool _selfHandDescending = false;
        [SerializeField] private float _selfHandSpacing = 90f;      // espacement souhaité entre colonnes
        [SerializeField] private float _selfHandZoneLength = 1500f; // largeur utile de la main
        [SerializeField] private float _selfHandMinSpacing = 60f;   // plancher : en dessous, on regroupe par rang

        [Header("Éventail du groupe ouvert (T55-11)")]
        [SerializeField] private float _fanLift = 220f;             // hauteur de l'éventail au-dessus de la main
        [SerializeField] private float _fanOverlap = 0.6f;          // pas entre cartes de l'éventail = largeur × 0.6

        private readonly List<CardView> _handViews = new List<CardView>();
        private readonly List<CardView> _faceUpViews = new List<CardView>();
        private readonly List<CardView> _faceDownViews = new List<CardView>();
        private readonly List<CardView> _fanViews = new List<CardView>();

        // T55-11 : tampons réutilisés (aucune allocation de listes par mise en page).
        private readonly List<List<CardView>> _columns = new List<List<CardView>>();
        private readonly List<List<CardView>> _columnPool = new List<List<CardView>>();
        private readonly List<Card> _sortInput = new List<Card>();
        private readonly List<CardView> _order = new List<CardView>();
        private readonly List<bool> _used = new List<bool>();

        private int _playerId = -1;
        private bool _groupingAllowed = true;
        private bool _isSelfHand;
        private Card _openKey; // une carte du groupe ouvert (référence stable entre deux relayouts), null = fermé

        public event Action<Card, CardView> HandCardClicked;
        public event Action<Card, CardView> FaceUpCardClicked;
        public event Action<int> FaceDownIndexClicked;
        public event Action<int> SeatClicked;

        public IReadOnlyList<CardView> HandViews { get { return _handViews.AsReadOnly(); } }
        public IReadOnlyList<CardView> FaceUpViews { get { return _faceUpViews.AsReadOnly(); } }
        public IReadOnlyList<CardView> FaceDownViews { get { return _faceDownViews.AsReadOnly(); } }
        public int PlayerId { get { return _playerId; } }
        public bool IsFanOpen { get { return _openKey != null; } }

        private void Awake()
        {
            if (_seatButton == null) _seatButton = GetComponent<Button>();
            if (_seatButton != null) _seatButton.onClick.AddListener(RaiseSeatClicked);
        }

        private void OnDestroy()
        {
            if (_seatButton != null) _seatButton.onClick.RemoveListener(RaiseSeatClicked);
            Unsubscribe(_handViews, OnHandClicked);
            Unsubscribe(_faceUpViews, OnFaceUpClicked);
            Unsubscribe(_faceDownViews, OnFaceDownClicked);
        }

        private void RaiseSeatClicked() { if (SeatClicked != null && _playerId >= 0) SeatClicked(_playerId); }

        public void SetSeatClickable(bool clickable)
        {
            if (_seatButton != null) _seatButton.interactable = clickable;
            else if (clickable) Debug.LogWarning("[PlayerSeatView] Assigner un Button au siège pour activer les clics.");
        }

                /// <summary>T55-11 : sens d'affichage de la main humaine. Purement visuel.</summary>
        public void SetSelfHandDescending(bool descending)
        {
            if (_selfHandDescending == descending) return;
            _selfHandDescending = descending;
            LayoutSelfHand();
        }

        /// <summary>
        /// T55-11 : autorise le regroupement par rang de la main humaine.
        /// Don du 7 et échange : false, cartes unitaires (éventail refermé). Relayout immédiat si la valeur change.
        /// </summary>
        public void SetGroupingAllowed(bool allowed)
        {
            if (_groupingAllowed == allowed) return;
            _groupingAllowed = allowed;
            if (!allowed) _openKey = null;
            if (_isSelfHand) LayoutSelfHand();
        }

        // ── Éventail ─────────────────────────────────────────────────────

        /// <summary>Nombre de cartes de la colonne contenant la vue (1 = carte seule), 0 si absente.</summary>
        public int ColumnSizeOf(CardView view)
        {
            int c = ColumnOf(view);
            return c < 0 ? 0 : _columns[c].Count;
        }

        /// <summary>Vrai si la vue fait partie de l'éventail actuellement ouvert.</summary>
        public bool IsInOpenFan(CardView view) { return view != null && _fanViews.Contains(view); }

        /// <summary>
        /// Ouvre l'éventail du groupe contenant la vue, ou le referme s'il est déjà ouvert.
        /// Ne fait rien pour une carte seule. Retourne true si l'état a changé.
        /// </summary>
        public bool ToggleColumnOf(CardView view)
        {
            if (!_isSelfHand) return false;
            int c = ColumnOf(view);
            if (c < 0 || _columns[c].Count < 2) return false;
            bool alreadyOpen = _openKey != null && ColumnContains(_columns[c], _openKey);
            _openKey = alreadyOpen ? null : _columns[c][0].Card;
            LayoutSelfHand();
            return true;
        }

        /// <summary>Referme l'éventail (jeu, ramassage, clic sur le fond, nouvelle requête).</summary>
        public void CloseFan()
        {
            if (_openKey == null) return;
            _openKey = null;
            if (_isSelfHand) LayoutSelfHand();
        }

        private int ColumnOf(CardView view)
        {
            if (view == null) return -1;
            for (int c = 0; c < _columns.Count; c++)
                if (_columns[c].Contains(view)) return c;
            return -1;
        }

        private static bool ColumnContains(List<CardView> col, Card card)
        {
            for (int k = 0; k < col.Count; k++)
                if (ReferenceEquals(col[k].Card, card)) return true;
            return false;
        }

        // ── Rendu ────────────────────────────────────────────────────────

        /// <summary>
        /// Masque le siège (null) ou affiche un adversaire. Cartes FaceUp publiques rendues ;
        /// main et FaceDown : dos uniquement, à partir des compteurs (T55-8).
        /// FaceUp et FaceDown partagent les mêmes emplacements : la FaceUp i recouvre le dos i.
        /// Le label de main n'apparaît que si la main dépasse le nombre de dos affichables.
        /// </summary>
        public void RenderOpponent(OpponentSeatViewModel model, ICardSkin skin)
        {
            _isSelfHand = false;
            _openKey = null;
            _fanViews.Clear();
            if (model == null)
            {
                gameObject.SetActive(false);
                _playerId = -1;
                return;
            }

            gameObject.SetActive(true);
            _playerId = model.PlayerId;
            Set(_nameLabel, model.Name);
            Set(_handCountLabel, model.HandCount.ToString());
            Set(_faceUpCountLabel, model.FaceUpCount.ToString());
            Set(_faceDownCountLabel, model.FaceDownCount.ToString());
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
            UpdateRankBanner(model.FinishRank); // T55-9

            // Par défaut (skin absent), pas de dos affichés : le label ne doit pas s'afficher seul.
            int visibleHandBacks = 0;

            if (skin != null)
            {
                // Même nombre d'emplacements pour FaceUp et FaceDown : centrage identique.
                int tableSlots = Math.Max(model.FaceUpCount, model.FaceDownCount);

                // T55-8 : uniquement des compteurs, jamais de Card cachée.
                visibleHandBacks = RenderBackRow(_handViews, _handContainer, model.HandCount, skin, OnHandClicked,
                    _handBackScale, _handDefaultSpacing, _handZoneLength, _handMaxVisibleBacks, 0f, model.HandCount);
                RenderBackRow(_faceDownViews, _faceDownContainer, model.FaceDownCount, skin, OnFaceDownClicked,
                    1f, _faceDownDefaultSpacing, _faceDownZoneLength, int.MaxValue, _tableRotationZ, tableSlots);
                RenderFaceRow(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked,
                    _faceDownDefaultSpacing, _faceDownZoneLength, _tableRotationZ, tableSlots);
            }

            UpdateHandCountLabel(model.HandCount, visibleHandBacks);
        }

        public void RenderSelf(SelfSeatViewModel model, string displayName, ICardSkin skin)
        {
            if (model == null)
            {
                gameObject.SetActive(false); _playerId = -1; _isSelfHand = false; _openKey = null; _fanViews.Clear();
                return;
            }
            gameObject.SetActive(true);
            _isSelfHand = true;
            _playerId = model.PlayerId;
            Set(_nameLabel, displayName);
            Set(_handCountLabel, model.Hand.Count.ToString());
            Set(_faceUpCountLabel, model.FaceUp.Count.ToString());
            Set(_faceDownCountLabel, model.FaceDownCount.ToString()); // T55-5
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
            UpdateRankBanner(model.FinishRank); // T55-9
            // Soi-même : toutes les cartes sont visibles, le label reste affiché comme avant.
            if (_handCountLabel != null) _handCountLabel.gameObject.SetActive(true);
            RenderCards(_handViews, _handContainer, model.Hand, skin, OnHandClicked);
            RenderCards(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked);
            // Jamais de Card liée aux FaceDown : uniquement le nombre et l'index aveugle.
            RenderFaceDownSlots(model.FaceDownCount, skin); // T55-5
            LayoutSelfHand(); // T55-11
        }

        /// <summary>
        /// T55-11 : placement par code de la main humaine. Affichage pur.
        /// - Tri visuel via HandSorter, sans jamais réordonner _handViews (ordre du Core conservé).
        /// - Regroupement progressif par rang, le plus petit d'abord, si les colonnes ne tiennent pas au plancher.
        /// - Groupe fermé : seule la carte du dessus est active, avec badge ×N.
        /// - Groupe ouvert : ses cartes sont étalées au-dessus de la main (éventail), sans badge.
        /// - Jamais de débordement : l'espacement descend sous le plancher si nécessaire.
        /// </summary>
        private void LayoutSelfHand()
        {
            _fanViews.Clear();
            if (_handContainer == null) return;
            int n = _handViews.Count;
            ReleaseColumns();
            if (n == 0) { _openKey = null; return; }

            // 1. Ordre visuel (tri stable et déterministe).
            _sortInput.Clear();
            for (int i = 0; i < n; i++) _sortInput.Add(_handViews[i].Card);
            List<Card> sorted = HandSorter.Sort(_sortInput);

            _order.Clear();
            _used.Clear();
            for (int i = 0; i < n; i++) _used.Add(false);
            for (int s = 0; s < sorted.Count; s++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (_used[i] || !ReferenceEquals(_handViews[i].Card, sorted[s])) continue;
                    _used[i] = true;
                    _order.Add(_handViews[i]);
                    break;
                }
            }
            // Sécurité : toute vue non appariée garde sa place à la fin.
            for (int i = 0; i < n; i++) if (!_used[i]) _order.Add(_handViews[i]);

            // 2. Colonnes initiales : une carte par colonne.
            for (int i = 0; i < _order.Count; i++)
            {
                List<CardView> col = RentColumn();
                col.Add(_order[i]);
                _columns.Add(col);
            }

            // 3. Regroupement progressif, rang le plus petit d'abord.
            if (_groupingAllowed)
            {
                while (_columns.Count > 1 && !Fits(_columns.Count))
                {
                    if (!MergeSmallestRank()) break;
                }
            }

            // 4. Groupe ouvert : retrouvé par sa carte clé ; fermé s'il n'existe plus ou n'est plus groupé.
            int open = -1;
            if (_openKey != null)
            {
                for (int c = 0; c < _columns.Count; c++)
                    if (ColumnContains(_columns[c], _openKey)) { open = c; break; }
                if (open < 0 || _columns[open].Count < 2) { open = -1; _openKey = null; }
            }

            // 5. Espacement des colonnes : souhaité, réduit pour tenir dans la zone.
            int count = _columns.Count;
            float step = 0f;
            if (count > 1)
            {
                float free = _selfHandZoneLength - _cardWidth;
                step = Mathf.Max(0f, Mathf.Min(_selfHandSpacing, free / (count - 1)));
            }
            float total = step * (count - 1);
            float halfZone = Mathf.Max(0f, (_selfHandZoneLength - _cardWidth) * 0.5f);

            // 6. Placement. En décroissant, les positions sont inversées (le regroupement est inchangé).
            for (int c = 0; c < count; c++)
            {
                List<CardView> col = _columns[c];
                int slot = _selfHandDescending ? count - 1 - c : c;
                float x = -total * 0.5f + step * slot;

                if (c == open)
                {
                    // Éventail centré sur la colonne, borné à la zone de main, dans le même sens que la main.
                    float spread = _cardWidth * _fanOverlap;
                    float width = spread * (col.Count - 1);
                    float startX = Mathf.Clamp(x - width * 0.5f, -halfZone, Mathf.Max(-halfZone, halfZone - width));
                    for (int k = 0; k < col.Count; k++)
                    {
                        CardView view = col[k];
                        int fk = _selfHandDescending ? col.Count - 1 - k : k;
                        if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                        Place(view, startX + spread * fk, _fanLift);
                        view.SetCountBadge(0);
                        _fanViews.Add(view);
                    }
                    continue;
                }

                for (int k = 0; k < col.Count; k++)
                {
                    CardView view = col[k];
                    bool top = k == col.Count - 1;
                    if (view.gameObject.activeSelf != top) view.gameObject.SetActive(top);
                    if (!top) continue;
                    Place(view, x, 0f);
                    view.SetCountBadge(col.Count);
                }
            }

            // 7. Ordre de rendu : de gauche à droite à l'écran, éventail par-dessus tout.
            int sibling = 0;
            for (int i = 0; i < count; i++)
            {
                int c = _selfHandDescending ? count - 1 - i : i;
                if (c == open) continue;
                _columns[c][_columns[c].Count - 1].transform.SetSiblingIndex(sibling++);
            }
            if (_selfHandDescending)
                for (int k = _fanViews.Count - 1; k >= 0; k--) _fanViews[k].transform.SetAsLastSibling();
            else
                for (int k = 0; k < _fanViews.Count; k++) _fanViews[k].transform.SetAsLastSibling();
        }

        private void Place(CardView view, float x, float y)
        {
            var rt = (RectTransform)view.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cardWidth, _cardHeight);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Vrai si columnCount colonnes tiennent sans descendre sous le plancher.</summary>
        private bool Fits(int columnCount)
        {
            if (columnCount <= 1) return true;
            float free = _selfHandZoneLength - _cardWidth;
            return free / (columnCount - 1) >= _selfHandMinSpacing;
        }

        /// <summary>
        /// Fusionne la première paire de colonnes voisines de même rang (colonnes triées par rang,
        /// donc rang le plus petit d'abord). Jokers jamais regroupés. False si plus rien à fusionner.
        /// </summary>
                /// <summary>
        /// Fusionne entièrement le plus petit rang encore éclaté (colonnes triées par rang).
        /// Un rang n'est jamais coupé : toutes ses colonnes voisines sont absorbées d'un coup.
        /// Jokers jamais regroupés. False si plus rien à fusionner.
        /// </summary>
        private bool MergeSmallestRank()
        {
            for (int c = 0; c < _columns.Count - 1; c++)
            {
                Card a = _columns[c][0].Card;
                if (a == null || a.IsJoker || !a.Rank.HasValue) continue;

                bool mergedAny = false;
                while (c + 1 < _columns.Count)
                {
                    Card b = _columns[c + 1][0].Card;
                    if (b == null || b.IsJoker || !b.Rank.HasValue || b.Rank.Value != a.Rank.Value) break;

                    List<CardView> merged = _columns[c + 1];
                    _columns[c].AddRange(merged);
                    _columns.RemoveAt(c + 1);
                    merged.Clear();
                    _columnPool.Add(merged);
                    mergedAny = true;
                }
                if (mergedAny) return true;
            }
            return false;
        }

        private List<CardView> RentColumn()
        {
            int last = _columnPool.Count - 1;
            if (last < 0) return new List<CardView>(4);
            List<CardView> col = _columnPool[last];
            _columnPool.RemoveAt(last);
            return col;
        }

        private void ReleaseColumns()
        {
            for (int c = 0; c < _columns.Count; c++)
            {
                _columns[c].Clear();
                _columnPool.Add(_columns[c]);
            }
            _columns.Clear();
        }

        /// <summary>
        /// T55-9 : « #1 », « #2 », « #3 », « Psycko » (rang 4). Rang absent : bannière masquée.
        /// Affichage pur : le rang (y compris celui du Psycko) est calculé par le Core.
        /// </summary>
        private void UpdateRankBanner(int? finishRank)
        {
            if (_rankBanner == null) return;

            string text = FormatRank(finishRank);
            _rankBanner.text = text;
            _rankBanner.gameObject.SetActive(text.Length > 0);
        }

        /// <summary>Mapping d'affichage pur rang -> texte (aucun calcul de règle).</summary>
        internal static string FormatRank(int? finishRank)
        {
            switch (finishRank)
            {
                case 1: return "#1";
                case 2: return "#2";
                case 3: return "#3";
                case 4: return "Psycko";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Affiche le label de main uniquement quand il reste des cartes non représentées
        /// (compte réel supérieur au nombre de dos visibles).
        /// </summary>
        private void UpdateHandCountLabel(int realCount, int visibleBacks)
        {
            if (_handCountLabel == null) return;
            _handCountLabel.gameObject.SetActive(realCount > visibleBacks);
        }

        private void RenderFaceDownSlots(int count, ICardSkin skin)
        {
            EnsureCount(_faceDownViews, _faceDownContainer, count, OnFaceDownClicked);
            for (int i = 0; i < _faceDownViews.Count; i++)
            {
                _faceDownViews[i].Bind(null, skin);
                _faceDownViews[i].SetState(CardVisualState.FaceDown);
            }
        }

        /// <summary>
        /// T55-8 : rangée de dos centrés et positionnés par code (aucun LayoutGroup sur ce conteneur).
        /// slotCount = nombre d'emplacements servant au centrage (aligne FaceUp et FaceDown).
        /// Retourne le nombre de dos réellement affichés.
        /// </summary>
        private int RenderBackRow(List<CardView> views, RectTransform container, int count, ICardSkin skin,
            Action<CardView> handler, float scale, float defaultSpacing, float zoneLength, int maxVisible,
            float rotationZ, int slotCount)
        {
            if (_cardViewPrefab == null || container == null) return 0;

            float cardW = _cardWidth * scale;
            float cardH = _cardHeight * scale;

            HandLayoutResult layout = HandSpacing.Compute(
                Math.Max(count, slotCount), defaultSpacing, zoneLength,
                RowLength(cardW, cardH, rotationZ), _minSpacing, maxVisible);
            int visible = Math.Min(count, layout.VisibleCount);
            EnsureCount(views, container, visible, handler);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                view.Bind(null, skin);
                view.SetState(CardVisualState.FaceDown);
                view.SetInteractable(false);
                PlaceInRow((RectTransform)view.transform, i, layout.Spacing, layout.VisibleCount,
                    cardW, cardH, rotationZ);
            }

            return visible;
        }

        /// <summary>
        /// FaceUp adverses placées par code sur les mêmes emplacements que les FaceDown
        /// (la carte i recouvre exactement le dos i).
        /// </summary>
        private void RenderFaceRow(List<CardView> views, RectTransform container, IReadOnlyList<Card> cards,
            ICardSkin skin, Action<CardView> handler, float defaultSpacing, float zoneLength,
            float rotationZ, int slotCount)
        {
            int count = cards == null ? 0 : cards.Count;
            if (_cardViewPrefab == null || container == null)
            {
                if (count > 0) Debug.LogWarning("[PlayerSeatView] Prefab ou conteneur de cartes non assigné.");
                return;
            }

            HandLayoutResult layout = HandSpacing.Compute(
                Math.Max(count, slotCount), defaultSpacing, zoneLength,
                RowLength(_cardWidth, _cardHeight, rotationZ), _minSpacing, int.MaxValue);
            int visible = Math.Min(count, layout.VisibleCount);
            EnsureCount(views, container, visible, handler);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                view.Bind(cards[i], skin);
                view.SetState(CardVisualState.Normal);
                view.SetInteractable(false);
                PlaceInRow((RectTransform)view.transform, i, layout.Spacing, layout.VisibleCount,
                    _cardWidth, _cardHeight, rotationZ);
            }
        }

        /// <summary>Extension d'une carte le long de l'axe de la rangée, rotation prise en compte.</summary>
        private float RowLength(float cardW, float cardH, float rotationZ)
        {
            bool swapped = Mathf.Abs(Mathf.Repeat(rotationZ, 180f) - 90f) < 1f;
            float alongX = swapped ? cardH : cardW;
            float alongY = swapped ? cardW : cardH;
            return _opponentBacksVertical ? alongY : alongX;
        }

        private void PlaceInRow(RectTransform rt, int index, float step, int slotCount,
            float cardW, float cardH, float rotationZ)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(cardW, cardH);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            float total = slotCount <= 1 ? 0f : step * (slotCount - 1);
            float offset = -total * 0.5f + step * index;
            rt.anchoredPosition = _opponentBacksVertical ? new Vector2(0f, -offset) : new Vector2(offset, 0f);
        }

        private void RenderCards(List<CardView> views, RectTransform container,
            IReadOnlyList<Card> cards, ICardSkin skin, Action<CardView> handler)
        {
            int count = cards == null ? 0 : cards.Count;
            if (_cardViewPrefab == null || container == null)
            {
                if (count > 0) Debug.LogWarning("[PlayerSeatView] Prefab ou conteneur de cartes non assigné.");
                return;
            }
            EnsureCount(views, container, count, handler);
            for (int i = 0; i < count; i++)
            {
                if (!views[i].gameObject.activeSelf) views[i].gameObject.SetActive(true);
                views[i].Bind(cards[i], skin);
                views[i].SetState(CardVisualState.Normal);
            }
        }

        private void EnsureCount(List<CardView> views, RectTransform container, int count, Action<CardView> handler)
        {
            if (_cardViewPrefab == null || container == null) return;
            while (views.Count < count)
            {
                var view = Instantiate(_cardViewPrefab, container);
                views.Add(view);
                view.Clicked += handler;
            }
            while (views.Count > count)
            {
                int last = views.Count - 1;
                views[last].Clicked -= handler;
                Destroy(views[last].gameObject);
                views.RemoveAt(last);
            }
        }

        private static void Unsubscribe(List<CardView> views, Action<CardView> handler)
        {
            foreach (var view in views) if (view != null) view.Clicked -= handler;
        }

        private void OnHandClicked(CardView view) { if (HandCardClicked != null) HandCardClicked(view.Card, view); }
        private void OnFaceUpClicked(CardView view) { if (FaceUpCardClicked != null) FaceUpCardClicked(view.Card, view); }
        private void OnFaceDownClicked(CardView view)
        {
            int index = _faceDownViews.IndexOf(view);
            if (index >= 0 && FaceDownIndexClicked != null) FaceDownIndexClicked(index);
        }

        private static void Set(Text label, string value) { if (label != null) label.text = value; }
    }
}