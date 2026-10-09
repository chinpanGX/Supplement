using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Supplement.Unity;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Supplement.Tests.PlayMode
{
    public class TestUpdateDispatcher
    {
        private sealed class CountingUpdatable : IUpdatable
        {
            public int Count;
            public Action OnUpdated;

            public void OnUpdate()
            {
                Count++;
                OnUpdated?.Invoke();
            }
        }

        private sealed class LeakingBehaviour : MonoBehaviour, IUpdatable
        {
            public static int Count;

            private void OnEnable()
            {
                UpdateDispatcher.Register(this);
            }

            public void OnUpdate()
            {
                Count++;
            }
        }

        [UnityTest]
        public IEnumerator RegisteredIsCalledEveryFrameUntilUnregistered()
        {
            var updatable = new CountingUpdatable();
            UpdateDispatcher.Register(updatable);

            yield return null;
            yield return null;
            Assert.GreaterOrEqual(updatable.Count, 2);

            UpdateDispatcher.Unregister(updatable);
            var countAfterUnregister = updatable.Count;
            yield return null;
            yield return null;
            Assert.AreEqual(countAfterUnregister, updatable.Count);
        }

        [UnityTest]
        public IEnumerator UnregisteringSelfDuringUpdateDoesNotSkipOthers()
        {
            var updatables = new[] { new CountingUpdatable(), new CountingUpdatable(), new CountingUpdatable() };
            var self = updatables[0];
            self.OnUpdated = () => UpdateDispatcher.Unregister(self);
            foreach (var updatable in updatables)
            {
                UpdateDispatcher.Register(updatable);
            }

            yield return null;
            yield return null;

            Assert.AreEqual(1, self.Count);
            Assert.GreaterOrEqual(updatables[1].Count, 2);
            Assert.GreaterOrEqual(updatables[2].Count, 2);
            UpdateDispatcher.Unregister(updatables[1]);
            UpdateDispatcher.Unregister(updatables[2]);
        }

        [UnityTest]
        public IEnumerator DestroyedBehaviourIsRemovedAutomatically()
        {
            LeakingBehaviour.Count = 0;
            var gameObject = new GameObject(nameof(LeakingBehaviour));
            gameObject.AddComponent<LeakingBehaviour>();
            yield return null;
            Assert.Greater(LeakingBehaviour.Count, 0);

            Object.Destroy(gameObject);
            yield return null;
            var countAfterDestroy = LeakingBehaviour.Count;
            yield return null;
            yield return null;

            Assert.AreEqual(countAfterDestroy, LeakingBehaviour.Count);
        }

        [Test]
        public void InsertedIntoPlayerLoopUpdateExactlyOnce()
        {
            var update = PlayerLoop.GetCurrentPlayerLoop().subSystemList
                .First(x => x.type == typeof(UnityEngine.PlayerLoop.Update));

            Assert.AreEqual(1, update.subSystemList.Count(x => x.type == typeof(UpdateDispatcher)));
        }
    }
}