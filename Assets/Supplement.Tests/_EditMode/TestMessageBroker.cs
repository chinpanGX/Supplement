using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestMessageBroker
    {
        private struct SampleMessage
        {
            public int Value;
        }

        private struct OtherMessage
        {
        }

        private MessageBroker broker;
        // UniTaskScheduler.UnobservedTaskExceptionに流れた例外。
        private readonly List<Exception> unobserved = new();

        [SetUp]
        public void SetUp()
        {
            broker = new MessageBroker();
            unobserved.Clear();
            UniTaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        [TearDown]
        public void TearDown()
        {
            UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            broker.Dispose();
        }

        private void OnUnobservedTaskException(Exception exception)
        {
            unobserved.Add(exception);
        }

        [Test]
        public void PublishReachesSubscribersInOrder()
        {
            var received = new List<string>();
            broker.Subscribe<SampleMessage>(x => received.Add($"first:{x.Value}"));
            broker.Subscribe<SampleMessage>(x => received.Add($"second:{x.Value}"));

            broker.Publish(new SampleMessage { Value = 1 });

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
        public void PublishDoesNotReachOtherBrokers()
        {
            using var other = new MessageBroker();
            var count = 0;
            other.Subscribe<SampleMessage>(_ => count++);

            broker.Publish(new SampleMessage());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void PublishWithoutSubscribersDoesNothing()
        {
            Assert.DoesNotThrow(() => broker.Publish(new SampleMessage()));
        }

        [Test]
        public void DisposedSubscriptionNoLongerReceives()
        {
            var count = 0;
            var subscription = broker.Subscribe<SampleMessage>(_ => count++);

            broker.Publish(new SampleMessage());
            subscription.Dispose();
            subscription.Dispose();
            broker.Publish(new SampleMessage());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SameDelegateSubscribedTwiceIsCalledTwiceUntilEachIsDisposed()
        {
            var count = 0;
            Action<SampleMessage> handler = _ => count++;
            var first = broker.Subscribe(handler);
            broker.Subscribe(handler);

            broker.Publish(new SampleMessage());
            first.Dispose();
            broker.Publish(new SampleMessage());

            Assert.AreEqual(3, count);
        }

        [Test]
        public void SubscriberAddedDuringPublishIsCalledFromNextPublish()
        {
            var lateCount = 0;
            var added = false;
            broker.Subscribe<SampleMessage>(_ =>
            {
                if (added) return;
                added = true;
                broker.Subscribe<SampleMessage>(_ => lateCount++);
            });

            broker.Publish(new SampleMessage());
            Assert.AreEqual(0, lateCount);

            broker.Publish(new SampleMessage());
            Assert.AreEqual(1, lateCount);
        }

        [Test]
        public void SubscriberDisposedDuringPublishIsNotCalled()
        {
            IDisposable second = null;
            var secondCount = 0;
            broker.Subscribe<SampleMessage>(_ => second.Dispose());
            second = broker.Subscribe<SampleMessage>(_ => secondCount++);

            broker.Publish(new SampleMessage());

            Assert.AreEqual(0, secondCount);
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void DisposingFirstMiddleOrLastKeepsOrderOfOthers(int disposedIndex)
        {
            var received = new List<int>();
            var subscriptions = new IDisposable[5];
            for (var i = 0; i < subscriptions.Length; i++)
            {
                var index = i;
                subscriptions[i] = broker.Subscribe<SampleMessage>(_ => received.Add(index));
            }

            subscriptions[disposedIndex].Dispose();
            broker.Subscribe<SampleMessage>(_ => received.Add(5));
            broker.Publish(new SampleMessage());

            var expected = new List<int> { 0, 1, 2, 3, 4, 5 };
            expected.Remove(disposedIndex);
            CollectionAssert.AreEqual(expected, received);
        }

        [Test]
        public void SubscriberDisposingItselfDuringPublishDoesNotStopLaterSubscribers()
        {
            var received = new List<string>();
            IDisposable self = null;
            broker.Subscribe<SampleMessage>(_ => received.Add("first"));
            self = broker.Subscribe<SampleMessage>(_ =>
            {
                received.Add("self");
                self.Dispose();
            });
            broker.Subscribe<SampleMessage>(_ => received.Add("last"));

            broker.Publish(new SampleMessage());
            broker.Publish(new SampleMessage());

            CollectionAssert.AreEqual(new[] { "first", "self", "last", "first", "last" }, received);
        }

        [Test]
        public void DisposingAllSubscriptionsThenSubscribingAgainWorks()
        {
            var count = 0;
            var first = broker.Subscribe<SampleMessage>(_ => count++);
            var second = broker.Subscribe<SampleMessage>(_ => count++);
            second.Dispose();
            first.Dispose();

            broker.Subscribe<SampleMessage>(_ => count += 10);
            broker.Publish(new SampleMessage());

            Assert.AreEqual(10, count);
        }

        [Test]
        public void PublishCallsAllSubscribersAndForwardsExceptionsWithoutThrowing()
        {
            var called = 0;
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException("first"));
            broker.Subscribe<SampleMessage>(_ => called++);
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException("third"));
            broker.Subscribe<SampleMessage>(_ => called++);

            // 呼び出し元には投げない。
            Assert.DoesNotThrow(() => broker.Publish(new SampleMessage()));

            // 例外を投げた購読者の後ろも呼び、例外は投げた順にUnobservedTaskExceptionへ流す。
            Assert.AreEqual(2, called);
            Assert.AreEqual(2, unobserved.Count);
            Assert.AreEqual("first", unobserved[0].Message);
            Assert.AreEqual("third", unobserved[1].Message);
        }

        [Test]
        public void PublishContinuesAfterSubscriberThatDisposesItselfAndThrows()
        {
            var called = 0;
            IDisposable self = null;
            self = broker.Subscribe<SampleMessage>(_ =>
            {
                self.Dispose();
                throw new InvalidOperationException();
            });
            broker.Subscribe<SampleMessage>(_ => called++);

            broker.Publish(new SampleMessage());

            Assert.AreEqual(1, called);
            Assert.AreEqual(1, unobserved.Count);
        }

        [Test]
        public void PublishDoesNotDeliverToSubscriberAddedByThrowingHandler()
        {
            var added = 0;
            broker.Subscribe<SampleMessage>(_ =>
            {
                broker.Subscribe<SampleMessage>(_ => added++);
                throw new InvalidOperationException();
            });

            broker.Publish(new SampleMessage());

            Assert.AreEqual(0, added);
            Assert.AreEqual(1, unobserved.Count);
        }

        [Test]
        public void SubscribeThrowsForNullHandler()
        {
            Assert.Throws<ArgumentNullException>(() => broker.Subscribe<SampleMessage>(null));
            Assert.Throws<ArgumentNullException>(() => broker.SubscribeAwait<SampleMessage>(null));
        }

        [Test]
        public void DisposedBrokerUnsubscribesAllAndRejectsUse()
        {
            var count = 0;
            var subscription = broker.Subscribe<SampleMessage>(_ => count++);

            broker.Dispose();

            Assert.Throws<ObjectDisposedException>(() => broker.Publish(new SampleMessage()));
            Assert.Throws<ObjectDisposedException>(() => broker.Subscribe<SampleMessage>(_ => { }));
            Assert.Throws<ObjectDisposedException>(() => broker.Subscribe<OtherMessage>(_ => { }));
            Assert.DoesNotThrow(() => subscription.Dispose());
            Assert.AreEqual(0, count);
        }

        [Test]
        public void DefaultIsSharedInstance()
        {
            Assert.AreSame(MessageBroker.Default, MessageBroker.Default);
        }

        [Test]
        public void PublishDoesNotAllocate()
        {
            var sum = 0;
            broker.Subscribe<SampleMessage>(x => sum += x.Value);
            broker.Subscribe<SampleMessage>(x => sum -= x.Value);
            broker.Publish(new SampleMessage { Value = 1 });

            Assert.That(() => { broker.Publish(new SampleMessage { Value = 1 }); }, Is.Not.AllocatingGCMemory());
        }

        [TestCase(AsyncSubscribeStrategy.Sequential)]
        [TestCase(AsyncSubscribeStrategy.Parallel)]
        [TestCase(AsyncSubscribeStrategy.Drop)]
        public void PublishToSynchronouslyCompletingAsyncSubscriberDoesNotAllocate(AsyncSubscribeStrategy strategy)
        {
            var sum = 0;
            broker.SubscribeAwait<SampleMessage>((x, _) =>
            {
                sum += x.Value;
                return UniTask.CompletedTask;
            }, strategy);
            broker.Publish(new SampleMessage { Value = 1 });

            Assert.That(() => { broker.Publish(new SampleMessage { Value = 1 }); }, Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator SequentialRunsMessagesOneAtATimeInOrder() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Sequential);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });
            broker.Publish(new SampleMessage { Value = 3 });
            CollectionAssert.AreEqual(new[] { 1 }, handler.Started);

            handler.Complete(0);
            CollectionAssert.AreEqual(new[] { 1, 2 }, handler.Started);

            handler.Complete(1);
            handler.Complete(2);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, handler.Started);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, handler.Completed);
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator ParallelStartsMessagesWithoutWaiting() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Parallel);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });
            CollectionAssert.AreEqual(new[] { 1, 2 }, handler.Started);

            handler.Complete(1);
            handler.Complete(0);
            CollectionAssert.AreEqual(new[] { 2, 1 }, handler.Completed);
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator DropIgnoresMessagesWhileRunning() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Drop);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });
            CollectionAssert.AreEqual(new[] { 1 }, handler.Started);

            handler.Complete(0);
            broker.Publish(new SampleMessage { Value = 3 });
            CollectionAssert.AreEqual(new[] { 1, 3 }, handler.Started);
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator SwitchCancelsRunningHandlerAndStartsNext() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Switch);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });

            CollectionAssert.AreEqual(new[] { 1, 2 }, handler.Started);
            Assert.IsTrue(handler.Tokens[0].IsCancellationRequested);
            Assert.IsFalse(handler.Tokens[1].IsCancellationRequested);
            CollectionAssert.AreEqual(new[] { 1 }, handler.Canceled);

            handler.Complete(1);
            CollectionAssert.AreEqual(new[] { 2 }, handler.Completed);
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator DisposingAsyncSubscriptionCancelsRunningHandler() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            var subscription = broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Sequential);

            broker.Publish(new SampleMessage { Value = 1 });
            broker.Publish(new SampleMessage { Value = 2 });
            subscription.Dispose();
            broker.Publish(new SampleMessage { Value = 3 });

            Assert.IsTrue(handler.Tokens[0].IsCancellationRequested);
            CollectionAssert.AreEqual(new[] { 1 }, handler.Started);
            CollectionAssert.AreEqual(new[] { 1 }, handler.Canceled);
            await UniTask.CompletedTask;
        });

        [Test]
        public void SubscribeAwaitThrowsForUndefinedStrategy()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                broker.SubscribeAwait<SampleMessage>((_, _) => UniTask.CompletedTask, (AsyncSubscribeStrategy)99));
        }

        // 呼ばれるたびに、テストから完了させるまで終わらない処理を作る。
        private sealed class GatedHandler
        {
            private readonly List<UniTaskCompletionSource> gates = new();

            public List<int> Started { get; } = new();
            public List<int> Completed { get; } = new();
            public List<int> Canceled { get; } = new();
            public List<CancellationToken> Tokens { get; } = new();

            public void Complete(int callIndex)
            {
                gates[callIndex].TrySetResult();
            }

            public async UniTask HandleAsync(SampleMessage message, CancellationToken ct)
            {
                var gate = new UniTaskCompletionSource();
                gates.Add(gate);
                Tokens.Add(ct);
                Started.Add(message.Value);
                try
                {
                    await gate.Task.AttachExternalCancellation(ct);
                    Completed.Add(message.Value);
                }
                catch (OperationCanceledException)
                {
                    Canceled.Add(message.Value);
                    throw;
                }
            }
        }
    }
}
