using Psycko.Presentation.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Affiche l’état visuel d’un compte à rebours, sans logique de jeu.</summary>
    public sealed class CountdownView : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] private string _format = "{0}";
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _warningColor = Color.red;
        [SerializeField] private float _warningThreshold = 5f;

        /// <summary>Met à jour le texte et, si présent, la jauge du compte à rebours.</summary>
        public void Show(float remaining, float total)
        {
            Color color = remaining <= _warningThreshold ? _warningColor : _normalColor;
            if (_label != null)
            {
                _label.gameObject.SetActive(true);
                _label.text = string.Format(_format, Mathf.CeilToInt(remaining));
                _label.color = color;
            }

            if (_fill != null)
            {
                _fill.gameObject.SetActive(true);
                _fill.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
                _fill.color = color;
            }
        }

        /// <summary>Masque le texte et la jauge lorsqu’ils ne sont pas utiles.</summary>
        public void Hide()
        {
            if (_label != null) _label.gameObject.SetActive(false);
            if (_fill != null) _fill.gameObject.SetActive(false);
        }
    }
}