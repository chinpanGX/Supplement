using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using Supplement.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace Supplement.Tests.EditMode
{
    public class TestHierarchyMessageBroker
    {
        private struct SampleMessage
        {
            public int Value;
        }

        private struct OtherMessage
        {
        }

        private GameObject root;
        private HierarchyMessageBroker broker;
        // UniTaskScheduler.UnobservedTaskExceptionに流れた例外。
        private readonly List<Exception> unobserved = new();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(nameof(TestHierarchyMessageBroker));
            broker = root.AddComponent<HierarchyMessageBroker>();
            unobserved.Clear();
            UniTaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        [TearDown]
        public void TearDown()
        {
            UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private void OnUnobservedTaskException(Exception exception)
        {
            unobserved.Add(exception);
        }

        [Test]
        public void PublishFromChildReachesSubscribersInOrder()
        {
            var received = new List<string>();
            broker.Subscribe<SampleMessage>(x => received.Add($"first:{x.Value}"));
            broker.Subscribe<SampleMessage>(x => received.Add($"second:{x.Value}"));
            var child = new GameObject("child").transform;
            child.SetParent(root.transform);

            HierarchyMessageBroker.Publish(child, new SampleMessage { Value = 1 });

            CollectionAssert.AreEqual(new[] { "first:1", "second:1" }, received);
        }

        [Test]
        public void PublishDoesNotReachOtherMessageTypes()
        {
            var count = 0;
            broker.Subscribe<SampleMessage>(_ => count++);

            broker.Publish(new OtherMessage());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void DisposingSubscriptionStopsDelivery()
        {
            var count = 0;
            var subscription = broker.Subscribe<SampleMessage>(_ => count++);

            subscription.Dispose();
            broker.Publish(new SampleMessage());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void SameDelegateSubscribedTwiceIsCalledTwiceUntilEachIsDisposed()
        {
            var count = 0;
            Action<SampleMessage> handler = _ => count++;
            var first = broker.Subscribe(handler);
            var second = broker.Subscribe(handler);

            broker.Publish(new SampleMessage());
            Assert.AreEqual(2, count);

            // 先に登録した側が解除しても、後から登録した側にはまだ届く
            first.Dispose();
            broker.Publish(new SampleMessage());
            Assert.AreEqual(3, count);

            second.Dispose();
            broker.Publish(new SampleMessage());
            Assert.AreEqual(3, count);
        }

        [Test]
        public void DisposingSubscriptionTwiceDoesNotRemoveResubscribedHandler()
        {
            var count = 0;
            Action<SampleMessage> handler = _ => count++;
            var first = broker.Subscribe(handler);
            first.Dispose();
            broker.Subscribe(handler);

            first.Dispose();
            broker.Publish(new SampleMessage());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SubscriberDisposedDuringPublishIsNotCalled()
        {
            var received = new List<string>();
            IDisposable second = null;
            broker.Subscribe<SampleMessage>(_ =>
            {
                received.Add("first");
                second.Dispose();
            });
            second = broker.Subscribe<SampleMessage>(_ => received.Add("second"));
            broker.Subscribe<SampleMessage>(_ => received.Add("third"));

            // 発行中に解除した購読者には、その発行でも届けない。後ろの購読者は飛ばさない。
            broker.Publish(new SampleMessage());
            CollectionAssert.AreEqual(new[] { "first", "third" }, received);

            received.Clear();
            broker.Publish(new SampleMessage());
            CollectionAssert.AreEqual(new[] { "first", "third" }, received);
        }

        [Test]
        public void ExceptionInSubscriberDoesNotStopOthersAndIsNotThrown()
        {
            var called = false;
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException());
            broker.Subscribe<SampleMessage>(_ => called = true);

            Assert.DoesNotThrow(() => broker.Publish(new SampleMessage()));
            Assert.IsTrue(called);
            Assert.AreEqual(1, unobserved.Count);
        }

        [Test]
        public void PublishDoesNotAllocate()
        {
            var sum = 0;
            broker.Subscribe<SampleMessage>(x => sum += x.Value);
            broker.Subscribe<SampleMessage>(x => sum -= x.Value);
            broker.Publish(new SampleMessage { Value = 1 });

            Assert.That(() => broker.Publish(new SampleMessage { Value = 1 }), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SubscribeAwaitRunsHandlerWithStrategy()
        {
            var started = new List<int>();
            var gate = new UniTaskCompletionSource();
            broker.SubscribeAwait<SampleMessage>(async (x, ct) =>
            {
                started.Add(x.Value);
                await gate.Task.AttachExternalCancellation(ct);
            }, AsyncSubscribeStrategy.Drop);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });

            CollectionAssert.AreEqual(new[] { 1 }, started);
            gate.TrySetResult();
        }

        [UnityTest]
        public IEnumerator FirstAsyncReceivesNextMessage() => UniTask.ToCoroutine(async () =>
        {
            var task = broker.FirstAsync<SampleMessage>();
            var child = new GameObject("child").transform;
            child.SetParent(root.transform);

            HierarchyMessageBroker.Publish(child, new SampleMessage { Value = 5 });

            Assert.AreEqual(5, (await task).Value);
        });
    }
}