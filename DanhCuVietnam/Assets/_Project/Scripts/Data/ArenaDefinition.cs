using System.Collections.Generic;
using UnityEngine;

namespace DanhCu.Data
{
    public enum ArenaSurface
    {
        /// <summary>Packed earth yard (sân đất): high friction, slight unevenness.</summary>
        PackedEarth = 0,

        /// <summary>Fired brick courtyard (sân gạch): medium friction, grout seams.</summary>
        Brick = 1,

        /// <summary>Polished stone / tile: low friction, long spins.</summary>
        Stone = 2,
    }

    /// <summary>
    /// A place to play. The arena's environment lives in its own scene, loaded additively
    /// on top of the Gameplay scene (see TECHNICAL_DESIGN.md §6).
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Arena", fileName = "Arena_")]
    public sealed class ArenaDefinition : DefinitionAsset
    {
        [SerializeField] private LocalizedText description;

        [Tooltip("Scene name (not path) of the environment. Must be in Build Settings.")]
        [SerializeField] private string sceneName = string.Empty;

        [Header("Surface physics")]
        [SerializeField] private ArenaSurface surface = ArenaSurface.PackedEarth;

        [Tooltip("Sliding friction between the tip and the ground.")]
        [Range(0.05f, 1.5f)] [SerializeField] private float tipFriction = 0.35f;

        [Tooltip("Rolling resistance coefficient for the tip; drains spin.")]
        [Range(0f, 0.05f)] [SerializeField] private float rollingResistance = 0.004f;

        [Range(0f, 1f)] [SerializeField] private float restitution = 0.2f;

        [Tooltip("Amplitude of surface unevenness in metres (perturbs wobble).")]
        [Range(0f, 0.005f)] [SerializeField] private float unevenness = 0.0008f;

        [Header("Play area")]
        [Tooltip("Radius of the drawn ring (vòng) in metres. Used by RingOut and to bound AI aim.")]
        [Range(0.3f, 3f)] [SerializeField] private float ringRadius = 0.6f;

        public LocalizedText Description => description;

        public string SceneName => sceneName;

        public ArenaSurface Surface => surface;

        public float TipFriction => tipFriction;

        public float RollingResistance => rollingResistance;

        public float Restitution => restitution;

        public float Unevenness => unevenness;

        public float RingRadius => ringRadius;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(!string.IsNullOrWhiteSpace(sceneName), errors, "sceneName is empty.");
            Require(ringRadius > 0f, errors, "ringRadius must be positive.");
        }

        internal void Configure(string newSceneName, ArenaSurface newSurface, float newTipFriction, float newRingRadius, LocalizedText newDescription)
        {
            sceneName = newSceneName;
            surface = newSurface;
            tipFriction = newTipFriction;
            ringRadius = newRingRadius;
            description = newDescription;
        }
    }
}
