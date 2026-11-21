using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Base class for content definitions referenced by a stable string id.
    /// Ids are lowercase snake_case, never change after release and are what save
    /// data and network messages store. Definitions are read-only at runtime.
    /// </summary>
    public abstract class DefinitionAsset : ScriptableObject, IValidatable
    {
        private static readonly Regex IdPattern = new Regex("^[a-z][a-z0-9_]*$");

        [Tooltip("Stable lowercase_snake_case id. Never change after release.")]
        [SerializeField] private string id = string.Empty;

        [SerializeField] private LocalizedText displayName;

        public string Id => id;

        public LocalizedText DisplayName => displayName;

        public static bool IsValidId(string value) => !string.IsNullOrEmpty(value) && IdPattern.IsMatch(value);

        public virtual void Validate(List<string> errors)
        {
            if (!IsValidId(id))
            {
                errors.Add($"{GetType().Name} '{name}': id '{id}' must be lowercase snake_case.");
            }

            if (displayName.IsEmpty)
            {
                errors.Add($"{GetType().Name} '{name}': display name is empty.");
            }
        }

        internal void SetIdentity(string newId, LocalizedText newDisplayName)
        {
            id = newId;
            displayName = newDisplayName;
        }

        protected void Require(bool condition, List<string> errors, string message)
        {
            if (!condition)
            {
                errors.Add($"{GetType().Name} '{id}': {message}");
            }
        }
    }
}
