using System.Collections.Generic;
using DanhCu.Core.Match;
using DanhCu.Core.Tops;
using DanhCu.Data;
using DanhCu.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DanhCu.Tests.EditMode.Data
{
    /// <summary>Validates the authored data set in the project and the definition rules themselves.</summary>
    public sealed class DataAssetTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        [Test]
        public void AllProjectDataAssets_AreValid()
        {
            List<string> errors = ProjectSetup.ValidateAllData();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void GameConfig_ExistsInResources()
        {
            Assert.That(Resources.Load<GameConfig>(GameConfig.ResourcesPath), Is.Not.Null,
                "Run 'Danh Cu > Setup > Run Project Setup'.");
        }

        [TestCase(GameModeType.SinglePlayer)]
        [TestCase(GameModeType.VersusAI)]
        [TestCase(GameModeType.OnlineMultiplayer)]
        public void GameConfig_DefaultMatchSettings_AreValid(GameModeType mode)
        {
            GameConfig config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            Assume.That(config, Is.Not.Null);

            MatchSettings settings = config.CreateDefaultMatchSettings(mode);
            List<string> errors = config.ValidateMatch(settings);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void GameConfig_ArenaScenes_AreInBuildSettings()
        {
            GameConfig config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            Assume.That(config, Is.Not.Null);

            var buildScenes = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    buildScenes.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
                }
            }

            Assert.That(buildScenes, Does.Contain(config.BootstrapScene));
            Assert.That(buildScenes, Does.Contain(config.MainMenuScene));
            Assert.That(buildScenes, Does.Contain(config.GameplayScene));
            foreach (ArenaDefinition arena in config.Arenas)
            {
                Assert.That(buildScenes, Does.Contain(arena.SceneName), $"Arena '{arena.Id}' scene missing from Build Settings");
            }
        }

        [TestCase("cu_go_oi", true)]
        [TestCase("a", true)]
        [TestCase("CuGoOi", false)]
        [TestCase("cu-go-oi", false)]
        [TestCase("1cu", false)]
        [TestCase("", false)]
        public void DefinitionIds_MustBeSnakeCase(string id, bool valid)
        {
            Assert.That(DefinitionAsset.IsValidId(id), Is.EqualTo(valid));
        }

        [Test]
        public void TopDefinition_WithoutMaterial_IsInvalid()
        {
            var top = Create<TopDefinition>();
            top.SetIdentity("test_top", new LocalizedText("Thử", "Test"));

            var errors = new List<string>();
            top.Validate(errors);

            Assert.That(errors, Has.Some.Contains("material"));
        }

        [Test]
        public void TopDefinition_MassProperties_ComeFromShapeAndWood()
        {
            TopData shape = CreateShape(TopShapePresets.Medium());
            var wood = Create<TopMaterialDefinition>();
            wood.Configure(900f, 0.45f, 0.35f, 0.75f);
            var top = Create<TopDefinition>();
            top.Configure(wood, TipType.IronNail, shape, 2800f, default);

            TopMassProperties expected = RevolvedMassIntegrator.Compute(new TopProfile(shape.ToParameters()), 900, shape.MetalDensity);
            Assert.That(top.Mass, Is.EqualTo((float)expected.Mass).Within(1e-6f));
            Assert.That(top.CenterOfMassHeight, Is.EqualTo((float)expected.CenterOfMassHeight).Within(1e-6f));
            Assert.That(top.AxialInertia, Is.EqualTo((float)expected.AxialInertia).Within(1e-12f));
            Assert.That(top.Height, Is.EqualTo(shape.TotalHeight));
            Assert.That(top.MaxLaunchAngularSpeed, Is.EqualTo(2800f * 2f * Mathf.PI / 60f).Within(1e-3f));
        }

        [Test]
        public void TopDefinition_DenserWood_IsHeavier()
        {
            TopData shape = CreateShape(TopShapePresets.Medium());
            var light = Create<TopMaterialDefinition>();
            light.Configure(600f, 0.5f, 0.4f, 0.5f);
            var heavy = Create<TopMaterialDefinition>();
            heavy.Configure(1050f, 0.4f, 0.3f, 0.9f);
            var a = Create<TopDefinition>();
            a.Configure(light, TipType.IronNail, shape, 2800f, default);
            var b = Create<TopDefinition>();
            b.Configure(heavy, TipType.IronNail, shape, 2800f, default);

            Assert.That(b.Mass, Is.GreaterThan(a.Mass));
        }

        [Test]
        public void TopDefinition_WithoutShape_IsInvalid()
        {
            var top = Create<TopDefinition>();
            top.SetIdentity("no_shape", new LocalizedText("Thử", "Test"));
            top.Configure(Create<TopMaterialDefinition>(), TipType.IronNail, null, 2800f, default);

            var errors = new List<string>();
            top.Validate(errors);

            Assert.That(errors, Has.Some.Contains("shape"));
            Assert.That(top.Mass, Is.EqualTo(0f));
        }

        [Test]
        public void TopDefinition_ThatCanNeverStandUp_IsInvalid()
        {
            var top = Create<TopDefinition>();
            top.SetIdentity("slow_top", new LocalizedText("Chậm", "Slow"));
            top.Configure(Create<TopMaterialDefinition>(), TipType.IronNail, CreateShape(TopShapePresets.Medium()), 800f, default);

            var errors = new List<string>();
            top.Validate(errors);

            Assert.That(errors, Has.Some.Contains("critical spin"));
        }

        [Test]
        public void TopData_RoundTripsParameters_AndDetectsMissingBake()
        {
            TopShapeParameters large = TopShapePresets.Large();
            TopData shape = CreateShape(large);

            Assert.That(shape.ToParameters().TotalHeight, Is.EqualTo(large.TotalHeight).Within(1e-6));
            Assert.That(shape.IsShapeValid, Is.True);
            Assert.That(shape.HasCurrentBake, Is.False);
        }

        [Test]
        public void TopData_InvalidDimensions_AreReported()
        {
            TopShapeParameters p = TopShapePresets.Medium();
            p.CollarDiameter = p.BodyDiameter;
            TopData shape = CreateShape(p);

            var errors = new List<string>();
            shape.Validate(errors);

            Assert.That(shape.IsShapeValid, Is.False);
            Assert.That(errors, Has.Some.Contains("CollarDiameter"));
        }

        [Test]
        public void PhysicsTuning_Defaults_RaiseMaxAngularSpeedAbovePhysXDefault()
        {
            var physics = Create<PhysicsTuningProfile>();
            Assert.That(physics.MaxAngularSpeed, Is.GreaterThanOrEqualTo(300f));
        }

        private TopData CreateShape(TopShapeParameters parameters)
        {
            var shape = Create<TopData>();
            shape.SetParameters(parameters);
            return shape;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T obj = ScriptableObject.CreateInstance<T>();
            _created.Add(obj);
            return obj;
        }
    }
}
