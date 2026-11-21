using System.Collections.Generic;
using DanhCu.Core.Tops;
using DanhCu.Data;
using UnityEngine;

namespace DanhCu.Tops
{
    /// <summary>
    /// Configures a TopModel prefab instance from a <see cref="TopData"/> shape and a wood
    /// <see cref="TopMaterialDefinition"/>: meshes, materials, colliders and rigidbody mass
    /// properties. Contains no gameplay or spin logic.
    ///
    /// Pivot: the transform origin is the lowest point of the metal tip, on the spin axis
    /// (local +Y). Children are at identity, so rotating the root rotates about the contact point.
    ///
    /// Colliders: a convex hull for the body (starting above the tip point) plus a sphere of
    /// radius = tip point radius centred on the axis, so ground contact is always exactly at the pivot.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TopModelController : MonoBehaviour
    {
        public const string BodyChildName = "Body";
        public const string TipChildName = "Tip";

        [SerializeField] private TopData shape;

        [Tooltip("Wood the top is made of. Empty = guava density and the renderer's current material.")]
        [SerializeField] private TopMaterialDefinition wood;

        [Header("Parts (wired by the prefab builder)")]
        [SerializeField] private MeshFilter bodyFilter;
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private MeshCollider bodyCollider;
        [SerializeField] private MeshFilter tipFilter;
        [SerializeField] private MeshRenderer tipRenderer;
        [SerializeField] private SphereCollider tipCollider;

        [Tooltip("Apply the assigned shape and wood in Awake.")]
        [SerializeField] private bool applyOnAwake = true;

        private readonly List<Object> _runtimeObjects = new List<Object>();
        private Rigidbody _rigidbody;

        public TopData Shape => shape;

        public TopMaterialDefinition Wood => wood;

        public TopMassProperties MassProperties { get; private set; }

        public Rigidbody Body => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

        /// <summary>Rope zone heights above the pivot (local Y, metres), for rope winding visuals.</summary>
        public Vector2 RopeZoneHeights
        {
            get
            {
                if (shape == null || !shape.IsShapeValid)
                {
                    return Vector2.zero;
                }

                TopProfile profile = shape.GetProfile();
                return new Vector2((float)profile.RopeZoneStartHeight, (float)profile.RopeZoneEndHeight);
            }
        }

        /// <summary>Local position of the centre of mass (on the axis).</summary>
        public Vector3 LocalCenterOfMass => new Vector3(0f, (float)MassProperties.CenterOfMassHeight, 0f);

        /// <summary>Wood radius at a local height, e.g. to wrap a rope (metres).</summary>
        public float WoodRadiusAt(float localHeight)
        {
            return shape != null && shape.IsShapeValid ? (float)shape.GetProfile().WoodRadiusAt(localHeight) : 0f;
        }

        /// <summary>Configures this model for a top definition (shape + wood).</summary>
        public bool Configure(TopDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[DanhCu] TopModelController.Configure: definition is null.", this);
                return false;
            }

            return Apply(definition.Shape, definition.Material);
        }

        /// <summary>
        /// Applies shape and wood. Baked meshes are used when they match the shape; otherwise
        /// meshes are generated at runtime (play mode only — in the editor, rebake instead).
        /// </summary>
        public bool Apply(TopData newShape, TopMaterialDefinition newWood)
        {
            return Apply(newShape, newWood, Application.isPlaying);
        }

        internal bool Apply(TopData newShape, TopMaterialDefinition newWood, bool generateIfNotBaked)
        {
            if (newShape == null)
            {
                Debug.LogError("[DanhCu] TopModelController: no TopData assigned.", this);
                return false;
            }

            var errors = new List<string>();
            newShape.Validate(errors);
            if (!newShape.IsShapeValid)
            {
                Debug.LogError($"[DanhCu] TopData '{newShape.name}' is invalid:\n- " + string.Join("\n- ", errors), this);
                return false;
            }

            if (!HasAllParts())
            {
                Debug.LogError("[DanhCu] TopModelController: part references are missing; rebuild the prefab (Danh Cu > Tops).", this);
                return false;
            }

            shape = newShape;
            wood = newWood;
            ReleaseRuntimeResources();

            TopProfile profile = shape.GetProfile();
            MassProperties = shape.GetMassProperties(wood != null ? wood.Density : TopData.FallbackWoodDensity);

            if (!AssignMeshes(profile, generateIfNotBaked))
            {
                return false;
            }

            AssignRenderMaterials();
            ConfigureColliders(profile);
            ConfigureRigidbody();
            return true;
        }

        /// <summary>Destroys meshes and physics materials this component created.</summary>
        internal void ReleaseRuntimeResources()
        {
            foreach (Object obj in _runtimeObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }

            _runtimeObjects.Clear();
        }

        internal void SetParts(MeshFilter newBodyFilter, MeshRenderer newBodyRenderer, MeshCollider newBodyCollider, MeshFilter newTipFilter, MeshRenderer newTipRenderer, SphereCollider newTipCollider)
        {
            bodyFilter = newBodyFilter;
            bodyRenderer = newBodyRenderer;
            bodyCollider = newBodyCollider;
            tipFilter = newTipFilter;
            tipRenderer = newTipRenderer;
            tipCollider = newTipCollider;
        }

        private bool HasAllParts()
        {
            return bodyFilter != null && bodyRenderer != null && bodyCollider != null
                && tipFilter != null && tipRenderer != null && tipCollider != null;
        }

        private bool AssignMeshes(TopProfile profile, bool generateIfNotBaked)
        {
            Mesh body, tip, collision;
            if (shape.HasCurrentBake)
            {
                body = shape.BakedBodyMesh;
                tip = shape.BakedTipMesh;
                collision = shape.BakedCollisionMesh;
            }
            else if (generateIfNotBaked)
            {
                TopMeshUtility.BuildAll(profile, shape.name, out body, out tip, out collision);
                foreach (Mesh mesh in new[] { body, tip, collision })
                {
                    mesh.hideFlags = HideFlags.DontSave;
                    _runtimeObjects.Add(mesh);
                }
            }
            else
            {
                Debug.LogWarning($"[DanhCu] TopData '{shape.name}' has no up-to-date baked meshes. Use 'Danh Cu > Tops > Rebake All Top Meshes'.", this);
                return false;
            }

            bodyFilter.sharedMesh = body;
            tipFilter.sharedMesh = tip;
            bodyCollider.sharedMesh = collision;
            return true;
        }

        private void AssignRenderMaterials()
        {
            Material woodMaterial = wood != null && wood.VisualMaterial != null ? wood.VisualMaterial : bodyRenderer.sharedMaterial;
            Material ropeMaterial = shape.RopeZoneMaterial != null ? shape.RopeZoneMaterial : woodMaterial;

            // Submesh order is fixed by TopMeshFactory: [wood, rope zone].
            var bodyMaterials = new Material[2];
            bodyMaterials[TopMeshFactory.WoodSubmesh] = woodMaterial;
            bodyMaterials[TopMeshFactory.RopeZoneSubmesh] = ropeMaterial;
            bodyRenderer.sharedMaterials = bodyMaterials;

            if (shape.MetalMaterial != null)
            {
                tipRenderer.sharedMaterial = shape.MetalMaterial;
            }
        }

        private void ConfigureColliders(TopProfile profile)
        {
            bodyCollider.convex = true;

            float pointRadius = (float)profile.Parameters.TipPointRadius;
            tipCollider.radius = pointRadius;
            tipCollider.center = new Vector3(0f, pointRadius, 0f); // sphere touches y = 0 on the axis

            // Physics materials are runtime-only so they never get serialized into prefabs/scenes.
            if (!Application.isPlaying)
            {
                return;
            }

            var tipPhysics = new PhysicsMaterial(shape.name + "_Tip")
            {
                dynamicFriction = shape.TipFriction,
                staticFriction = shape.TipFriction,
                bounciness = shape.TipBounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            _runtimeObjects.Add(tipPhysics);
            tipCollider.sharedMaterial = tipPhysics;

            if (wood != null)
            {
                var woodPhysics = new PhysicsMaterial(wood.name + "_Body")
                {
                    dynamicFriction = wood.BodyFriction,
                    staticFriction = wood.BodyFriction,
                    bounciness = wood.Restitution,
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Average,
                };
                _runtimeObjects.Add(woodPhysics);
                bodyCollider.sharedMaterial = woodPhysics;
            }
        }

        /// <summary>Explicit mass properties: must be set after colliders change, which resets them.</summary>
        private void ConfigureRigidbody()
        {
            Rigidbody rb = Body;
            TopMassProperties m = MassProperties;
            float transverse = (float)m.TransverseInertiaAtCenterOfMass;

            rb.mass = (float)m.Mass;
            rb.centerOfMass = LocalCenterOfMass;
            rb.inertiaTensor = new Vector3(transverse, (float)m.AxialInertia, transverse);
            rb.inertiaTensorRotation = Quaternion.identity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Spin losses are modelled explicitly (PHYSICS_SPEC §4); PhysX damping would double-count them.
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, Physics.defaultMaxAngularSpeed);
        }

        private void Awake()
        {
            if (applyOnAwake && shape != null)
            {
                Apply(shape, wood);
            }
        }

        private void OnDestroy()
        {
            ReleaseRuntimeResources();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            // Component changes are not allowed inside OnValidate; defer to the next editor tick.
            UnityEditor.EditorApplication.delayCall += EditorRefresh;
        }

        private void EditorRefresh()
        {
            if (this == null || Application.isPlaying || shape == null || !HasAllParts()
                || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                return;
            }

            if (shape.HasCurrentBake)
            {
                Apply(shape, wood, false);
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            if (shape == null || !shape.IsShapeValid)
            {
                return;
            }

            TopProfile profile = shape.GetProfile();
            TopShapeParameters p = profile.Parameters;
            TopMassProperties m = MassProperties.Mass > 0 ? MassProperties : shape.GetMassProperties(wood != null ? wood.Density : TopData.FallbackWoodDensity);

            // Spin axis through the pivot.
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.TransformPoint(Vector3.down * 0.01f), transform.TransformPoint(Vector3.up * ((float)p.TotalHeight + 0.01f)));

            // Pivot and centre of mass.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.0015f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(0f, (float)m.CenterOfMassHeight, 0f)), 0.002f);

            // Rope winding area.
            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            DrawRing((float)profile.RopeZoneStartHeight, (float)profile.WoodRadiusAt(profile.RopeZoneStartHeight));
            DrawRing((float)profile.RopeZoneEndHeight, (float)profile.WoodRadiusAt(profile.RopeZoneEndHeight));
        }

        private void DrawRing(float height, float radius)
        {
            const int segments = 32;
            Vector3 previous = transform.TransformPoint(new Vector3(radius, height, 0f));
            for (int i = 1; i <= segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                Vector3 next = transform.TransformPoint(new Vector3(radius * Mathf.Cos(a), height, radius * Mathf.Sin(a)));
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
