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

        [SetUp]
        public void SetUp()
        {
            broker = new MessageBroker();
        }

        [TearDown]
        public void TearDown()
        {
            broker.Dispose();
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
        public void PublishCallsAllSubscribersAndThrowsAggregateException()
        {
            var called = 0;
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException("first"));
            broker.Subscribe<SampleMessage>(_ => called++);
            broker.Subscribe<SampleMessage>(_ => throw new InvalidOperationException("third"));

            var exception = Assert.Throws<AggregateException>(() => broker.Publish(new SampleMessage()));

            Assert.AreEqual(1, called);
            Assert.AreEqual(2, exception.InnerExceptions.Count);
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

        [UnityTest]
        public IEnumerator PublishAsyncWaitsForAllHandlers() => UniTask.ToCoroutine(async () =>
        {
            var first = new GatedHandler();
            var second = new GatedHandler();
            var syncCount = 0;
            broker.SubscribeAwait<SampleMessage>(first.HandleAsync);
            broker.SubscribeAwait<SampleMessage>(second.HandleAsync);
            broker.Subscribe<SampleMessage>(_ => syncCount++);

            var publish = broker.PublishAsync(new SampleMessage { Value = 1 }).Preserve();
            CollectionAssert.AreEqual(new[] { 1 }, first.Started);
            CollectionAssert.AreEqual(new[] { 1 }, second.Started);
            Assert.AreEqual(1, syncCount);

            first.Complete(0);
            Assert.AreEqual(UniTaskStatus.Pending, publish.Status);

            second.Complete(0);
            Assert.AreEqual(UniTaskStatus.Succeeded, publish.Status);
            await publish;
        });

        [UnityTest]
        public IEnumerator PublishAsyncSequentialWaitsForEachHandlerBeforeNext() => UniTask.ToCoroutine(async () =>
        {
            var first = new GatedHandler();
            var second = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(first.HandleAsync);
            broker.SubscribeAwait<SampleMessage>(second.HandleAsync);

            var publish = broker.PublishAsync(new SampleMessage { Value = 1 }, AsyncPublishStrategy.Sequential).Preserve();
            CollectionAssert.AreEqual(new[] { 1 }, first.Started);
            CollectionAssert.IsEmpty(second.Started);

            first.Complete(0);
            CollectionAssert.AreEqual(new[] { 1 }, second.Started);

            second.Complete(0);
            await publish;
        });

        [UnityTest]
        public IEnumerator PublishAsyncThrowsAggregateExceptionAfterAllHandlers() => UniTask.ToCoroutine(async () =>
        {
            var completed = 0;
            broker.SubscribeAwait<SampleMessage>(async (_, _) =>
            {
                await UniTask.CompletedTask;
                throw new InvalidOperationException();
            }, AsyncSubscribeStrategy.Parallel);
            broker.SubscribeAwait<SampleMessage>(async (_, _) =>
            {
                await UniTask.CompletedTask;
                completed++;
            }, AsyncSubscribeStrategy.Parallel);
            broker.Subscribe<SampleMessage>(_ => throw new ArgumentException());

            var exception = await CatchAsync(broker.PublishAsync(new SampleMessage()));

            Assert.IsInstanceOf<AggregateException>(exception);
            Assert.AreEqual(2, ((AggregateException)exception).InnerExceptions.Count);
            Assert.AreEqual(1, completed);
        });

        [UnityTest]
        public IEnumerator PublishAsyncCanceledSkipsQueuedMessage() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Sequential);
            using var cts = new CancellationTokenSource();

            broker.Publish(new SampleMessage { Value = 1 });
            var publish = broker.PublishAsync(new SampleMessage { Value = 2 }, cancellationToken: cts.Token).Preserve();
            cts.Cancel();

            // 前の処理(1)の終わりを待たずに、取り消した時点で終わる。
            Assert.AreEqual(UniTaskStatus.Canceled, publish.Status);
            Assert.IsInstanceOf<OperationCanceledException>(await CatchAsync(publish));

            handler.Complete(0);
            CollectionAssert.AreEqual(new[] { 1 }, handler.Started);
        });

        [Test]
        public void PublishAsyncThrowsForUndefinedStrategyEvenWithoutSubscribers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                broker.PublishAsync(new SampleMessage(), (AsyncPublishStrategy)99));
        }

        [Test]
        public void SubscribeAwaitThrowsForUndefinedStrategy()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                broker.SubscribeAwait<SampleMessage>((_, _) => UniTask.CompletedTask, (AsyncSubscribeStrategy)99));
        }

        [UnityTest]
        public IEnumerator PublishAsyncCompletesWhenSubscriptionIsDisposed() => UniTask.ToCoroutine(async () =>
        {
            var handler = new GatedHandler();
            var subscription = broker.SubscribeAwait<SampleMessage>(handler.HandleAsync, AsyncSubscribeStrategy.Sequential);

            var running = broker.PublishAsync(new SampleMessage { Value = 1 }).Preserve();
            var queued = broker.PublishAsync(new SampleMessage { Value = 2 }).Preserve();
            subscription.Dispose();

            await running;
            await queued;
            CollectionAssert.AreEqual(new[] { 1 }, handler.Started);
        });

        [UnityTest]
        public IEnumerator PublishAsyncWithoutSubscribersCompletes() => UniTask.ToCoroutine(async () =>
        {
            await broker.PublishAsync(new SampleMessage());
        });

        private static async UniTask<Exception> CatchAsync(UniTask task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                return e;
            }
            Assert.Fail("例外が投げられなかった。");
            return null;
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
