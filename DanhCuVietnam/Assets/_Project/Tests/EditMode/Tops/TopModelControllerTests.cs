using System.Collections.Generic;
using DanhCu.Core.Tops;
using DanhCu.Data;
using DanhCu.Editor;
using DanhCu.Tops;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DanhCu.Tests.EditMode.Tops
{
    /// <summary>Builds the TopModel hierarchy in memory and checks pivot, colliders and mass properties.</summary>
    public sealed class TopModelControllerTests
    {
        private readonly List<Object> _created = new List<Object>();
        private GameObject _root;
        private TopModelController _controller;

        [SetUp]
        public void SetUp()
        {
            _root = TopModelAssetBuilder.CreateHierarchy(out _controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.ReleaseRuntimeResources();
            Object.DestroyImmediate(_root);
            foreach (Object obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private static IEnumerable<TestCaseData> Sizes()
        {
            yield return new TestCaseData(TopShapePresets.Small()).SetName("Small");
            yield return new TestCaseData(TopShapePresets.Medium()).SetName("Medium");
            yield return new TestCaseData(TopShapePresets.Large()).SetName("Large");
        }

        [TestCaseSource(nameof(Sizes))]
        public void Apply_SetsRigidbodyMassPropertiesFromGeometry(TopShapeParameters size)
        {
            TopData shape = CreateShape(size);
            TopMaterialDefinition wood = CreateWood(900f);

            Assert.That(_controller.Apply(shape, wood, true), Is.True);

            TopMassProperties expected = shape.GetMassProperties(900);
            Rigidbody rb = _root.GetComponent<Rigidbody>();
            Assert.That(rb.mass, Is.EqualTo((float)expected.Mass).Within(1e-6f));
            Assert.That(rb.centerOfMass.x, Is.EqualTo(0f));
            Assert.That(rb.centerOfMass.z, Is.EqualTo(0f));
            Assert.That(rb.centerOfMass.y, Is.EqualTo((float)expected.CenterOfMassHeight).Within(1e-6f));
            Assert.That(rb.inertiaTensor.y, Is.EqualTo((float)expected.AxialInertia).Within(1e-12f));
            Assert.That(rb.inertiaTensor.x, Is.EqualTo(rb.inertiaTensor.z));
            Assert.That(rb.angularDamping, Is.EqualTo(0f));
        }

        [TestCaseSource(nameof(Sizes))]
        public void TipCollider_TouchesThePivotOnTheAxis(TopShapeParameters size)
        {
            _controller.Apply(CreateShape(size), CreateWood(900f), true);
            SphereCollider tip = _root.GetComponentInChildren<SphereCollider>();

            Assert.That(tip.center.x, Is.EqualTo(0f));
            Assert.That(tip.center.z, Is.EqualTo(0f));
            Assert.That(tip.center.y - tip.radius, Is.EqualTo(0f).Within(1e-7f));
            Assert.That(tip.radius, Is.EqualTo((float)size.TipPointRadius).Within(1e-7f));
        }

        [TestCaseSource(nameof(Sizes))]
        public void Meshes_MatchConfiguredDimensions(TopShapeParameters size)
        {
            _controller.Apply(CreateShape(size), CreateWood(900f), true);
            MeshFilter[] filters = _root.GetComponentsInChildren<MeshFilter>();
            Bounds body = filters[0].sharedMesh.bounds;
            Bounds tip = filters[1].sharedMesh.bounds;

            Assert.That(body.max.y, Is.EqualTo((float)size.TotalHeight).Within(1e-5f));
            Assert.That(body.extents.x, Is.EqualTo((float)size.BodyRadius).Within(2e-4f));
            Assert.That(tip.min.y, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(filters[0].sharedMesh.subMeshCount, Is.EqualTo(2));
            Assert.That(_root.GetComponentInChildren<MeshCollider>().convex, Is.True);
            Assert.That(_root.GetComponentInChildren<MeshCollider>().sharedMesh.bounds.min.y, Is.GreaterThan(0f), "hull must not reach below the tip sphere");
        }

        [Test]
        public void Children_AreAtIdentity_SoThePivotIsTheRoot()
        {
            foreach (Transform child in _root.transform)
            {
                Assert.That(child.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(child.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(child.localScale, Is.EqualTo(Vector3.one));
            }
        }

        [Test]
        public void Apply_AssignsSeparateWoodAndMetalMaterials()
        {
            TopData shape = CreateShape(TopShapePresets.Medium());
            var metal = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "metal" };
            var woodRender = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "wood" };
            _created.Add(metal);
            _created.Add(woodRender);
            shape.SetRenderMaterials(metal, null);
            TopMaterialDefinition wood = CreateWood(900f);
            wood.SetVisualMaterial(woodRender);

            _controller.Apply(shape, wood, true);

            MeshRenderer[] renderers = _root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers[0].sharedMaterials, Is.EqualTo(new[] { woodRender, woodRender }), "rope zone falls back to wood");
            Assert.That(renderers[1].sharedMaterial, Is.SameAs(metal));
        }

        [Test]
        public void Apply_InvalidShape_IsRefused()
        {
            TopShapeParameters bad = TopShapePresets.Medium();
            bad.TipConeLength = bad.TipExposedLength * 2;
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("invalid"));

            Assert.That(_controller.Apply(CreateShape(bad), null, true), Is.False);
        }

        [Test]
        public void Apply_InEditModeWithoutBake_DoesNotGenerateUnsavedMeshes()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("baked"));

            Assert.That(_controller.Apply(CreateShape(TopShapePresets.Medium()), null, false), Is.False);
            Assert.That(_root.GetComponentInChildren<MeshFilter>().sharedMesh, Is.Null);
        }

        [Test]
        public void TopModelPrefab_ExistsAndIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Tops/TopModel.prefab");
            Assume.That(prefab, Is.Not.Null, "Run 'Danh Cu > Setup > Run Project Setup'.");

            var controller = prefab.GetComponent<TopModelController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Shape, Is.Not.Null);
            Assert.That(controller.Shape.HasCurrentBake, Is.True);
            Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<SphereCollider>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<MeshCollider>(), Is.Not.Null);
        }

        private TopData CreateShape(TopShapeParameters parameters)
        {
            var shape = ScriptableObject.CreateInstance<TopData>();
            shape.name = "TestShape";
            shape.SetParameters(parameters);
            _created.Add(shape);
            return shape;
        }

        private TopMaterialDefinition CreateWood(float density)
        {
            var wood = ScriptableObject.CreateInstance<TopMaterialDefinition>();
            wood.Configure(density, 0.45f, 0.35f, 0.75f);
            _created.Add(wood);
            return wood;
        }
    }
}
