using System;
using System.Collections.Generic;
using NUnit.Framework;
using Supplement.Unity;
using UnityEngine;
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

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(nameof(TestHierarchyMessageBroker));
            broker = root.AddComponent<HierarchyMessageBroker>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
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
        public void DuplicateSubscriptionIsCalledOnceAndKeptUntilAllDisposed()
        {
            var count = 0;
            Action<SampleMessage> handler = _ => count++;
            var first = broker.Subscribe(handler);
            var second = broker.Subscribe(handler);

            broker.Publish(new SampleMessage());
            Assert.AreEqual(1, count);

            // 先に登録した側が解除しても、後から登録した側にはまだ届く
            first.Dispose();
            broker.Publish(new SampleMessage());
            Assert.AreEqual(2, count);

            second.Dispose();
            broker.Publish(new SampleMessage());
            Assert.AreEqual(2, count);
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
        public void UnsubscribingDuringPublishDoesNotSkipOtherSubscribers()
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

            broker.Publish(new SampleMessage());
            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, received);

            received.Clear();
            broker.Publish(new SampleMessage());
            CollectionAssert.AreEqual(new[] { "first", "third" }, received);
        }

        [Test]
        public void ExceptionInSubscriberDoesNotStopOthers()
        {
            var called = false;
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException());
            broker.Subscribe<SampleMessage>(_ => called = true);

            Assert.Throws<AggregateException>(() => broker.Publish(new SampleMessage()));
            Assert.IsTrue(called);
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
    }
}