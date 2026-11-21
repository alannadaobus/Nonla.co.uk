using System.Collections.Generic;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Physical and visual properties of the wood a top (con cù) is carved from.
    /// Values are SI units; see Docs/PHYSICS_SPEC.md §3.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Top Material", fileName = "TopMaterial_")]
    public sealed class TopMaterialDefinition : DefinitionAsset
    {
        [Header("Physics")]
        [Tooltip("kg/m^3. Guava wood ~ 900, jackfruit ~ 600.")]
        [Min(100f)] [SerializeField] private float density = 900f;

        [Range(0f, 1f)] [SerializeField] private float restitution = 0.45f;

        [Tooltip("Friction coefficient of the wooden body against other tops.")]
        [Range(0f, 1.5f)] [SerializeField] private float bodyFriction = 0.35f;

        [Tooltip("Resistance to chipping when struck (0 = fragile, 1 = indestructible).")]
        [Range(0f, 1f)] [SerializeField] private float durability = 0.7f;

        [Header("Presentation")]
        [SerializeField] private Material visualMaterial;

        public float Density => density;

        public float Restitution => restitution;

        public float BodyFriction => bodyFriction;

        public float Durability => durability;

        public Material VisualMaterial => visualMaterial;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(density >= 100f && density <= 2000f, errors, "density must be within 100..2000 kg/m^3.");
            Require(restitution >= 0f && restitution <= 1f, errors, "restitution must be within 0..1.");
        }

        internal void Configure(float newDensity, float newRestitution, float newBodyFriction, float newDurability)
        {
            density = newDensity;
            restitution = newRestitution;
            bodyFriction = newBodyFriction;
            durability = newDurability;
        }

        internal void SetVisualMaterial(Material newVisualMaterial)
        {
            visualMaterial = newVisualMaterial;
        }
    }
}
