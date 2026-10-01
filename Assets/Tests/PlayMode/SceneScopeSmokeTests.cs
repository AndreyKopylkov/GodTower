using System.Collections;
using GodTower.Scopes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer.Unity;

namespace GodTower.Tests.PlayMode
{
    /// <summary>Both build scenes load and their scene scopes are built as children of the root scope.</summary>
    public sealed class SceneScopeSmokeTests
    {
        [UnityTest]
        public IEnumerator MenuScene_LoadsWithScopeParentedToRoot()
        {
            yield return SceneManager.LoadSceneAsync(TestScenes.Menu);

            AssertScopeBuiltUnderRoot<MenuLifetimeScope>();
        }

        [UnityTest]
        public IEnumerator GameScene_LoadsWithScopeParentedToRoot()
        {
            yield return SceneManager.LoadSceneAsync(TestScenes.Game);

            AssertScopeBuiltUnderRoot<GameLifetimeScope>();
        }

        private static void AssertScopeBuiltUnderRoot<TScope>() where TScope : LifetimeScope
        {
            var scope = Object.FindAnyObjectByType<TScope>();
            Assert.That(scope, Is.Not.Null, $"{typeof(TScope).Name} missing from the scene.");
            Assert.That(scope.Container, Is.Not.Null, $"{typeof(TScope).Name} container was not built.");

            LifetimeScope root = VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance();
            Assert.That(root, Is.InstanceOf<RootLifetimeScope>());
            Assert.That(scope.Parent, Is.SameAs(root));
        }
    }
}
