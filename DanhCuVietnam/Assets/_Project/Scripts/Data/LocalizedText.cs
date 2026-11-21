using System;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Minimal bilingual text until the Unity Localization package is adopted (see TECHNICAL_DESIGN.md §9).
    /// Vietnamese is the primary language; English is the fallback.
    /// </summary>
    [Serializable]
    public struct LocalizedText
    {
        [SerializeField] private string vi;
        [SerializeField] private string en;

        public LocalizedText(string vietnamese, string english)
        {
            vi = vietnamese;
            en = english;
        }

        public string Vietnamese => vi;

        public string English => en;

        public bool IsEmpty => string.IsNullOrWhiteSpace(vi) && string.IsNullOrWhiteSpace(en);

        public string Get(bool vietnamese)
        {
            string preferred = vietnamese ? vi : en;
            return string.IsNullOrWhiteSpace(preferred) ? (vietnamese ? en : vi) ?? string.Empty : preferred;
        }

        public override string ToString() => Get(true);
    }
}
