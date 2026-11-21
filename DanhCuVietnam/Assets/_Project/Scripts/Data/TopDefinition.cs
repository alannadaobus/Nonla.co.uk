using System.Collections.Generic;
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
    /// A playable spinning top (con cù). Geometry is authored in metres and kilograms;
    /// derived inertia uses the solid-cone approximation from Docs/PHYSICS_SPEC.md §3.2.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Top", fileName = "Top_")]
    public sealed class TopDefinition : DefinitionAsset
    {
        public const float StandardGravity = 9.81f;

        /// <summary>A perfect throw must exceed the critical spin by this factor.</summary>
        public const float MinStabilityHeadroom = 1.2f;

        [SerializeField] private LocalizedText description;

        [Header("Construction")]
        [SerializeField] private TopMaterialDefinition material;
        [SerializeField] private TipType tipType = TipType.IronNail;

        [Header("Geometry (SI)")]
        [Tooltip("Total mass in kg, including the iron tip.")]
        [Range(0.05f, 0.6f)] [SerializeField] private float mass = 0.18f;

        [Tooltip("Widest body radius in metres.")]
        [Range(0.015f, 0.08f)] [SerializeField] private float bodyRadius = 0.035f;

        [Tooltip("Height from tip to crown in metres.")]
        [Range(0.03f, 0.15f)] [SerializeField] private float height = 0.075f;

        [Tooltip("Centre of mass height above the tip, as a fraction of height.")]
        [Range(0.2f, 0.8f)] [SerializeField] private float centerOfMassRatio = 0.55f;

        [Tooltip("Contact radius of the tip in metres.")]
        [Range(0.0005f, 0.01f)] [SerializeField] private float tipRadius = 0.0015f;

        [Header("Launch")]
        [Tooltip("Maximum spin a perfect throw can impart, in revolutions per minute.")]
        [Range(300f, 4000f)] [SerializeField] private float maxLaunchRpm = 2200f;

        [Tooltip("Rope wraps around the body; more wraps = more spin but slower wind-up.")]
        [Range(3, 12)] [SerializeField] private int ropeWraps = 7;

        [Header("Presentation")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;

        public LocalizedText Description => description;

        public TopMaterialDefinition Material => material;

        public TipType TipType => tipType;

        public float Mass => mass;

        public float BodyRadius => bodyRadius;

        public float Height => height;

        public float CenterOfMassHeight => height * centerOfMassRatio;

        public float TipRadius => tipRadius;

        public float MaxLaunchRpm => maxLaunchRpm;

        /// <summary>Max launch spin in rad/s.</summary>
        public float MaxLaunchAngularSpeed => maxLaunchRpm * (2f * Mathf.PI / 60f);

        public int RopeWraps => ropeWraps;

        public GameObject Prefab => prefab;

        public Sprite Icon => icon;

        /// <summary>Moment of inertia about the spin axis (kg·m²): solid cone, I₃ = 3/10·m·r².</summary>
        public float AxialInertia => 0.3f * mass * bodyRadius * bodyRadius;

        /// <summary>
        /// Moment of inertia about a horizontal axis through the tip (kg·m²): cone about its
        /// centre of mass, 3/20·m·(r² + h²/4), moved to the tip by the parallel-axis term m·l².
        /// </summary>
        public float TransverseInertiaAtTip
        {
            get
            {
                float l = CenterOfMassHeight;
                return (0.15f * mass * ((bodyRadius * bodyRadius) + (0.25f * height * height))) + (mass * l * l);
            }
        }

        /// <summary>
        /// Minimum spin (rad/s) for the top to stand upright ("sleep"):
        /// ω_crit = 2·√(m·g·l·I₁) / I₃. See PHYSICS_SPEC.md §4.2.
        /// </summary>
        public float CriticalAngularSpeed(float gravity)
        {
            float torque = mass * Mathf.Abs(gravity) * CenterOfMassHeight;
            return 2f * Mathf.Sqrt(torque * TransverseInertiaAtTip) / AxialInertia;
        }

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(material != null, errors, "material is not assigned.");
            Require(mass > 0f, errors, "mass must be positive.");
            Require(bodyRadius > tipRadius, errors, "bodyRadius must be larger than tipRadius.");
            Require(height > bodyRadius * 0.5f, errors, "height is implausibly small for the body radius.");
            Require(maxLaunchRpm > 0f, errors, "maxLaunchRpm must be positive.");

            if (mass > 0f && bodyRadius > 0f)
            {
                float critical = CriticalAngularSpeed(StandardGravity);
                Require(MaxLaunchAngularSpeed >= critical * MinStabilityHeadroom, errors,
                    $"max launch spin {MaxLaunchAngularSpeed:0} rad/s is below {MinStabilityHeadroom}x the critical spin {critical:0} rad/s; it could never stand upright.");
            }
        }

        internal void Configure(
            TopMaterialDefinition newMaterial,
            TipType newTipType,
            float newMass,
            float newBodyRadius,
            float newHeight,
            float newMaxLaunchRpm,
            LocalizedText newDescription)
        {
            material = newMaterial;
            tipType = newTipType;
            mass = newMass;
            bodyRadius = newBodyRadius;
            height = newHeight;
            maxLaunchRpm = newMaxLaunchRpm;
            description = newDescription;
        }
    }
}
