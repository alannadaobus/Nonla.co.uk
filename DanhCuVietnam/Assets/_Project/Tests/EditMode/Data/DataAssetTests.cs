using System.Collections.Generic;
using DanhCu.Core.Match;
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
        public void TopDefinition_Inertia_MatchesSolidConeFormula()
        {
            var top = Create<TopDefinition>();
            top.Configure(Create<TopMaterialDefinition>(), TipType.IronNail, 0.2f, 0.04f, 0.08f, 2000f, default);

            Assert.That(top.AxialInertia, Is.EqualTo(0.3f * 0.2f * 0.04f * 0.04f).Within(1e-9f));
            Assert.That(top.MaxLaunchAngularSpeed, Is.EqualTo(2000f * 2f * Mathf.PI / 60f).Within(1e-3f));
        }

        [Test]
        public void TopDefinition_CriticalSpeed_MatchesStabilityCriterion()
        {
            var top = Create<TopDefinition>();
            top.Configure(Create<TopMaterialDefinition>(), TipType.IronNail, 0.18f, 0.035f, 0.075f, 2200f, default);

            float l = top.CenterOfMassHeight;
            float i1 = (0.15f * 0.18f * ((0.035f * 0.035f) + (0.25f * 0.075f * 0.075f))) + (0.18f * l * l);
            float expected = 2f * Mathf.Sqrt(0.18f * 9.81f * l * i1) / top.AxialInertia;

            Assert.That(top.TransverseInertiaAtTip, Is.EqualTo(i1).Within(1e-9f));
            Assert.That(top.CriticalAngularSpeed(9.81f), Is.EqualTo(expected).Within(1e-2f));
            Assert.That(top.CriticalAngularSpeed(9.81f), Is.EqualTo(159f).Within(2f), "PHYSICS_SPEC §4.2 worked example");
        }

        [Test]
        public void TopDefinition_ThatCanNeverStandUp_IsInvalid()
        {
            var top = Create<TopDefinition>();
            top.SetIdentity("slow_top", new LocalizedText("Chậm", "Slow"));
            top.Configure(Create<TopMaterialDefinition>(), TipType.IronNail, 0.18f, 0.035f, 0.075f, 800f, default);

            var errors = new List<string>();
            top.Validate(errors);

            Assert.That(errors, Has.Some.Contains("critical spin"));
        }

        [Test]
        public void PhysicsTuning_Defaults_RaiseMaxAngularSpeedAbovePhysXDefault()
        {
            var physics = Create<PhysicsTuningProfile>();
            Assert.That(physics.MaxAngularSpeed, Is.GreaterThanOrEqualTo(300f));
        }

        private T Create<T>() where T : ScriptableObject
        {
            T obj = ScriptableObject.CreateInstance<T>();
            _created.Add(obj);
            return obj;
        }
    }
}
