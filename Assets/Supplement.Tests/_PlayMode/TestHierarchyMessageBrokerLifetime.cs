using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Supplement.Tests.PlayMode
{
    // GameObjectの破棄で購読を解除するテスト。エディットモードではOnDestroyが呼ばれないため、PlayModeで回す。
    public class TestHierarchyMessageBrokerLifetime
    {
        private struct SampleMessage
        {
        }

        // UniTaskScheduler.UnobservedTaskExceptionに流れた例外。
        private readonly List<Exception> unobserved = new();

        [SetUp]
        public void SetUp()
        {
            unobserved.Clear();
            UniTaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        [TearDown]
        public void TearDown()
        {
            UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }

        private void OnUnobservedTaskException(Exception exception)
        {
            unobserved.Add(exception);
        }

        [UnityTest]
        public IEnumerator DestroyingGameObjectUnsubscribesAllAndCancelsRunningHandlers()
        {
            var root = new GameObject(nameof(TestHierarchyMessageBrokerLifetime));
            var broker = root.AddComponent<HierarchyMessageBroker>();
            var count = 0;
            var token = CancellationToken.None;
            var gate = new UniTaskCompletionSource();
            broker.Subscribe<SampleMessage>(_ => count++);
            broker.SubscribeAwait<SampleMessage>(async (_, ct) =>
            {
                token = ct;
                await gate.Task.AttachExternalCancellation(ct);
            });
            broker.Publish(new SampleMessage());
            Assert.AreEqual(1, count);

            Object.Destroy(root);
            yield return null;

            Assert.IsTrue(token.IsCancellationRequested);
            // 破棄の後に発行しても例外にならず、どこにも届かない。
            Assert.DoesNotThrow(() => broker.Publish(new SampleMessage()));
            Assert.AreEqual(1, count);
            Assert.AreEqual(0, unobserved.Count);
        }
    }
}
