using System;
using System.Collections.Generic;
using DanhCu.Core.Tops;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Physical shape of a spinning-top model: every adjustable dimension of the wooden body,
    /// the metal tip and the rope winding area, plus the metal's physical and render material.
    ///
    /// One TopData can be shared by many tops: the wood (density, render material) comes from
    /// the <see cref="TopDefinition"/> that uses it. Mass, centre of mass and inertia are never
    /// authored — they are integrated from this geometry (PHYSICS_SPEC §3, Docs/TOP_MODEL.md).
    ///
    /// All lengths are metres, measured from the tip point (the pivot) along +Y.
    /// The default values are the Medium prototype; they are assumptions, not historical data.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Top Data (Shape)", fileName = "TopData_")]
    public sealed class TopData : DefinitionAsset
    {
        /// <summary>Used when a model has no wood assigned (guava, kg/m³).</summary>
        public const double FallbackWoodDensity = 900.0;

        [Header("Overall (m)")]
        [Tooltip("Tip point to top of crown.")]
        [Range(0.02f, 0.2f)] [SerializeField] private float totalHeight = 0.075f;

        [Tooltip("Widest diameter of the wooden body (belly).")]
        [Range(0.015f, 0.15f)] [SerializeField] private float bodyDiameter = 0.055f;

        [Tooltip("Belly height above the wood underside, as a fraction of the wood height.")]
        [Range(0.2f, 0.85f)] [SerializeField] private float bellyHeightRatio = 0.5f;

        [Tooltip("Wood diameter where the nail enters (bottom of the rope cone).")]
        [Range(0.004f, 0.06f)] [SerializeField] private float collarDiameter = 0.01f;

        [Tooltip("Lower cone fullness: 1 = straight cone, higher = rounder flank.")]
        [Range(1f, 3f)] [SerializeField] private float lowerCurveExponent = 1.6f;

        [Tooltip("Crown shape: 2 = elliptical dome, higher = flatter top with a squarer shoulder.")]
        [Range(1.5f, 5f)] [SerializeField] private float crownFlatness = 2.6f;

        [Header("Metal tip (m)")]
        [Tooltip("Metal length visible below the wood.")]
        [Range(0.002f, 0.06f)] [SerializeField] private float tipExposedLength = 0.012f;

        [Range(0.001f, 0.02f)] [SerializeField] private float tipShankDiameter = 0.005f;

        [Tooltip("Length of the tapered part, measured from the virtual cone apex.")]
        [Range(0.001f, 0.06f)] [SerializeField] private float tipConeLength = 0.008f;

        [Tooltip("Radius of the rounded point: the ground contact. Larger = blunter, more travel.")]
        [Range(0.0001f, 0.008f)] [SerializeField] private float tipPointRadius = 0.0008f;

        [Tooltip("Hidden shank depth inside the wood. Lowers the centre of mass.")]
        [Range(0f, 0.06f)] [SerializeField] private float tipEmbedDepth = 0.015f;

        [Tooltip("kg/m³. Steel ~ 7850, iron ~ 7870, brass ~ 8500.")]
        [Range(2000f, 12000f)] [SerializeField] private float metalDensity = 7850f;

        [Tooltip("PhysX friction of the tip collider (combined with the arena surface).")]
        [Range(0f, 1.5f)] [SerializeField] private float tipFriction = 0.25f;

        [Range(0f, 1f)] [SerializeField] private float tipBounciness = 0.05f;

        [Header("Rope winding area")]
        [Tooltip("Start along the lower cone: 0 = collar, 1 = belly.")]
        [Range(0f, 1f)] [SerializeField] private float ropeZoneStart = 0.1f;

        [Range(0f, 1f)] [SerializeField] private float ropeZoneEnd = 0.9f;

        [Range(0, 12)] [SerializeField] private int ropeGrooveCount = 4;

        [Range(0f, 0.003f)] [SerializeField] private float ropeGrooveDepth = 0.0004f;

        [Range(0.0002f, 0.01f)] [SerializeField] private float ropeGrooveWidth = 0.0016f;

        [Header("Mesh detail (visual only)")]
        [Range(8, 128)] [SerializeField] private int radialSegments = 40;
        [Range(4, 128)] [SerializeField] private int profileSegments = 16;
        [Range(6, 24)] [SerializeField] private int collisionRadialSegments = 12;

        [Header("Rendering")]
        [SerializeField] private Material metalMaterial;

        [Tooltip("Optional. Empty = the rope zone uses the wood material.")]
        [SerializeField] private Material ropeZoneMaterial;

        // Written by the editor baker only (Danh Cu > Tops > Rebake All Top Meshes).
        [SerializeField] [HideInInspector] private Mesh bakedBodyMesh;
        [SerializeField] [HideInInspector] private Mesh bakedTipMesh;
        [SerializeField] [HideInInspector] private Mesh bakedCollisionMesh;
        [SerializeField] [HideInInspector] private string bakedSignature = string.Empty;

        [NonSerialized] private TopProfile _profile;
        [NonSerialized] private string _profileSignature;
        [NonSerialized] private double _cachedDensity = double.NaN;
        [NonSerialized] private TopMassProperties _cachedMass;

#if UNITY_EDITOR
        /// <summary>Editor only: raised when a TopData is edited, so baked meshes can be refreshed.</summary>
        public static event Action<TopData> EditorChanged;
#endif

        public float MetalDensity => metalDensity;

        public float TipFriction => tipFriction;

        public float TipBounciness => tipBounciness;

        public Material MetalMaterial => metalMaterial;

        public Material RopeZoneMaterial => ropeZoneMaterial;

        public Mesh BakedBodyMesh => bakedBodyMesh;

        public Mesh BakedTipMesh => bakedTipMesh;

        public Mesh BakedCollisionMesh => bakedCollisionMesh;

        /// <summary>True when baked meshes exist and were generated from the current dimensions.</summary>
        public bool HasCurrentBake =>
            bakedBodyMesh != null && bakedTipMesh != null && bakedCollisionMesh != null
            && bakedSignature == ToParameters().GetSignature();

        public float TotalHeight => totalHeight;

        public float BodyDiameter => bodyDiameter;

        public float TipPointRadius => tipPointRadius;

        /// <summary>Engine-free copy of the dimensions.</summary>
        public TopShapeParameters ToParameters()
        {
            return new TopShapeParameters
            {
                TotalHeight = totalHeight,
                BodyDiameter = bodyDiameter,
                BellyHeightRatio = bellyHeightRatio,
                CollarDiameter = collarDiameter,
                LowerCurveExponent = lowerCurveExponent,
                CrownFlatness = crownFlatness,
                TipExposedLength = tipExposedLength,
                TipShankDiameter = tipShankDiameter,
                TipConeLength = tipConeLength,
                TipPointRadius = tipPointRadius,
                TipEmbedDepth = tipEmbedDepth,
                RopeZoneStart = ropeZoneStart,
                RopeZoneEnd = ropeZoneEnd,
                RopeGrooveCount = ropeGrooveCount,
                RopeGrooveDepth = ropeGrooveDepth,
                RopeGrooveWidth = ropeGrooveWidth,
                RadialSegments = radialSegments,
                ProfileSegments = profileSegments,
                CollisionRadialSegments = collisionRadialSegments,
            };
        }

        /// <summary>The analytic outline, rebuilt only when dimensions change. Throws if invalid; check <see cref="IsShapeValid"/>.</summary>
        public TopProfile GetProfile()
        {
            TopShapeParameters parameters = ToParameters();
            string signature = parameters.GetSignature();
            if (_profile == null || _profileSignature != signature)
            {
                _profile = new TopProfile(parameters);
                _profileSignature = signature;
                _cachedDensity = double.NaN;
            }

            return _profile;
        }

        public bool IsShapeValid => ToParameters().Validate().Count == 0;

        /// <summary>Integrated mass properties for this shape made of wood with the given density.</summary>
        public TopMassProperties GetMassProperties(double woodDensity)
        {
            TopProfile profile = GetProfile();
            if (woodDensity != _cachedDensity)
            {
                _cachedMass = RevolvedMassIntegrator.Compute(profile, woodDensity, metalDensity);
                _cachedDensity = woodDensity;
            }

            return _cachedMass;
        }

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            foreach (string error in ToParameters().Validate())
            {
                errors.Add($"TopData '{Id}': {error}");
            }

            Require(metalDensity > 0f, errors, "metalDensity must be positive.");
            Require(metalMaterial != null, errors, "metalMaterial is not assigned.");
        }

        internal void SetParameters(TopShapeParameters p)
        {
            totalHeight = (float)p.TotalHeight;
            bodyDiameter = (float)p.BodyDiameter;
            bellyHeightRatio = (float)p.BellyHeightRatio;
            collarDiameter = (float)p.CollarDiameter;
            lowerCurveExponent = (float)p.LowerCurveExponent;
            crownFlatness = (float)p.CrownFlatness;
            tipExposedLength = (float)p.TipExposedLength;
            tipShankDiameter = (float)p.TipShankDiameter;
            tipConeLength = (float)p.TipConeLength;
            tipPointRadius = (float)p.TipPointRadius;
            tipEmbedDepth = (float)p.TipEmbedDepth;
            ropeZoneStart = (float)p.RopeZoneStart;
            ropeZoneEnd = (float)p.RopeZoneEnd;
            ropeGrooveCount = p.RopeGrooveCount;
            ropeGrooveDepth = (float)p.RopeGrooveDepth;
            ropeGrooveWidth = (float)p.RopeGrooveWidth;
            radialSegments = p.RadialSegments;
            profileSegments = p.ProfileSegments;
            collisionRadialSegments = p.CollisionRadialSegments;
            InvalidateCaches();
        }

        internal void SetRenderMaterials(Material metal, Material ropeZone)
        {
            metalMaterial = metal;
            ropeZoneMaterial = ropeZone;
        }

        internal void SetBake(Mesh body, Mesh tip, Mesh collision, string signature)
        {
            bakedBodyMesh = body;
            bakedTipMesh = tip;
            bakedCollisionMesh = collision;
            bakedSignature = signature;
        }

        private void InvalidateCaches()
        {
            _profile = null;
            _cachedDensity = double.NaN;
        }

        private void OnValidate()
        {
            InvalidateCaches();
#if UNITY_EDITOR
            EditorChanged?.Invoke(this);
#endif
        }
    }
}
