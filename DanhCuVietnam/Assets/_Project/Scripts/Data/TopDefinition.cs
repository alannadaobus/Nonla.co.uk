using System.Collections.Generic;
using DanhCu.Core.Tops;
using UnityEngine;

namespace DanhCu.Data
{
    public enum TipType
    {
        /// <summary>Traditional iron nail tip (chân đinh). Low friction, sharp precession.</summary>
        IronNail = 0,

        /// <summary>Blunt rounded tip. More travel, less stable.</summary>
        Rounded = 1,

        /// <summary>Flat-ground tip. Very stable, short spin.</summary>
        Flat = 2,
    }

    /// <summary>
    /// A playable spinning top (con cù): which shape (<see cref="TopData"/>) it is turned in,
    /// which wood (<see cref="TopMaterialDefinition"/>) it is made of, and its gameplay values.
    /// Mass, centre of mass and inertia are integrated from shape + wood + metal tip
    /// (PHYSICS_SPEC §3); they are never authored by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Top", fileName = "Top_")]
    public sealed class TopDefinition : DefinitionAsset
    {
        public const float StandardGravity = 9.81f;

        /// <summary>A perfect throw must exceed the critical spin by this factor.</summary>
        public const float MinStabilityHeadroom = 1.2f;

        [SerializeField] private LocalizedText description;

        [Header("Construction")]
        [SerializeField] private TopData shape;
        [SerializeField] private TopMaterialDefinition material;

        [Tooltip("UI / gameplay label. The tip geometry itself comes from the TopData.")]
        [SerializeField] private TipType tipType = TipType.IronNail;

        [Header("Launch")]
        [Tooltip("Maximum spin a perfect throw can impart, in revolutions per minute.")]
        [Range(300f, 4000f)] [SerializeField] private float maxLaunchRpm = 2800f;

        [Tooltip("Rope wraps around the body; more wraps = more spin but slower wind-up.")]
        [Range(3, 12)] [SerializeField] private int ropeWraps = 7;

        [Header("Presentation")]
        [Tooltip("Normally Prefabs/Tops/TopModel.prefab; its TopModelController is configured from this definition at spawn.")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;

        public LocalizedText Description => description;

        public TopData Shape => shape;

        public TopMaterialDefinition Material => material;

        public TipType TipType => tipType;

        /// <summary>Integrated mass properties (shape + wood + metal); default (all zero) without a valid shape.</summary>
        public TopMassProperties MassProperties =>
            shape != null && shape.IsShapeValid
                ? shape.GetMassProperties(material != null ? material.Density : TopData.FallbackWoodDensity)
                : default;

        public float Mass => (float)MassProperties.Mass;

        public float BodyRadius => shape != null ? shape.BodyDiameter * 0.5f : 0f;

        public float Height => shape != null ? shape.TotalHeight : 0f;

        public float CenterOfMassHeight => (float)MassProperties.CenterOfMassHeight;

        public float TipRadius => shape != null ? shape.TipPointRadius : 0f;

        public float MaxLaunchRpm => maxLaunchRpm;

        /// <summary>Max launch spin in rad/s.</summary>
        public float MaxLaunchAngularSpeed => maxLaunchRpm * (2f * Mathf.PI / 60f);

        public int RopeWraps => ropeWraps;

        public GameObject Prefab => prefab;

        public Sprite Icon => icon;

        /// <summary>I₃: moment of inertia about the spin axis (kg·m²).</summary>
        public float AxialInertia => (float)MassProperties.AxialInertia;

        /// <summary>I₁: moment of inertia about a horizontal axis through the tip (kg·m²).</summary>
        public float TransverseInertiaAtTip => (float)MassProperties.TransverseInertiaAtPivot;

        /// <summary>
        /// Minimum spin (rad/s) for the top to stand upright ("sleep"):
        /// ω_crit = 2·√(m·g·l·I₁) / I₃. See PHYSICS_SPEC.md §4.2.
        /// </summary>
        public float CriticalAngularSpeed(float gravity) => (float)MassProperties.CriticalAngularSpeed(gravity);

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(material != null, errors, "material is not assigned.");
            Require(shape != null, errors, "shape (TopData) is not assigned.");
            Require(maxLaunchRpm > 0f, errors, "maxLaunchRpm must be positive.");

            if (shape != null && shape.IsShapeValid)
            {
                float critical = CriticalAngularSpeed(StandardGravity);
                Require(MaxLaunchAngularSpeed >= critical * MinStabilityHeadroom, errors,
                    $"max launch spin {MaxLaunchAngularSpeed:0} rad/s is below {MinStabilityHeadroom}x the critical spin {critical:0} rad/s; it could never stand upright.");
            }
        }

        internal void Configure(
            TopMaterialDefinition newMaterial,
            TipType newTipType,
            TopData newShape,
            float newMaxLaunchRpm,
            LocalizedText newDescription,
            GameObject newPrefab = null)
        {
            material = newMaterial;
            tipType = newTipType;
            shape = newShape;
            maxLaunchRpm = newMaxLaunchRpm;
            description = newDescription;
            prefab = newPrefab;
        }
    }
}
