using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools;
using VContainer;

namespace Supplement.Tests
{
    public class TestBootInitializer
    {
        [UnityTest]
        public IEnumerator RegisterBootInitializerInjectsRegisteredTasks() => UniTask.ToCoroutine(async () =>
        {
            var log = new List<string>();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new RecordingTask(InitializationPriority.Low, "Low", log)).As<IBootInitializationTask>();
            builder.RegisterInstance(new GatedTask(InitializationPriority.Normal, UniTask.CompletedTask)).As<IBootInitializationTask>();
            builder.RegisterBootInitializer();
            var bootInitializer = builder.Build().Resolve<BootInitializer>();

            await bootInitializer.RunAsync();

            CollectionAssert.AreEqual(new[] { "Low" }, log);
        });

        [UnityTest]
        public IEnumerator RunsTasksInPriorityOrder() => UniTask.ToCoroutine(async () =>
        {
            var log = new List<string>();
            var bootInitializer = new BootInitializer(new IBootInitializationTask[]
            {
                new RecordingTask(InitializationPriority.Low, "Low", log),
                new RecordingTask(InitializationPriority.Highest, "Highest", log),
                new RecordingTask(InitializationPriority.Normal, "Normal", log),
            });

            await bootInitializer.RunAsync();

            CollectionAssert.AreEqual(new[] { "Highest", "Normal", "Low" }, log);
        });

        [UnityTest]
        public IEnumerator RunsTasksWithSamePriorityInParallel() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource();
            var first = new GatedTask(InitializationPriority.Normal, gate.Task);
            var second = new GatedTask(InitializationPriority.Normal, gate.Task);
            var bootInitializer = new BootInitializer(new IBootInitializationTask[] { first, second });

            var running = bootInitializer.RunAsync();

            // 1つ目の完了を待たずに、同じPriorityの2つ目も開始されている
            Assert.IsTrue(first.Started);
            Assert.IsTrue(second.Started);

            gate.TrySetResult();
            await running;
        });

        [UnityTest]
        public IEnumerator DoesNotStartNextPriorityUntilPreviousCompletes() => UniTask.ToCoroutine(async () =>
        {
            var gate = new UniTaskCompletionSource();
            var high = new GatedTask(InitializationPriority.High, gate.Task);
            var low = new GatedTask(InitializationPriority.Low, UniTask.CompletedTask);
            var bootInitializer = new BootInitializer(new IBootInitializationTask[] { high, low });

            var running = bootInitializer.RunAsync();
            Assert.IsTrue(high.Started);
            Assert.IsFalse(low.Started);

            gate.TrySetResult();
            await running;
            Assert.IsTrue(low.Started);
        });

        [UnityTest]
        public IEnumerator CompletesWithoutTasks() => UniTask.ToCoroutine(async () =>
        {
            var bootInitializer = new BootInitializer(Array.Empty<IBootInitializationTask>());

            await bootInitializer.RunAsync();
        });

        [UnityTest]
        public IEnumerator ThrowsAndSkipsLaterPrioritiesWhenTaskFails() => UniTask.ToCoroutine(async () =>
        {
            var log = new List<string>();
            var bootInitializer = new BootInitializer(new IBootInitializationTask[]
            {
                new FailingTask(InitializationPriority.High),
                new RecordingTask(InitializationPriority.Low, "Low", log),
            });

            await AssertThrowsAsync<InvalidOperationException>(bootInitializer.RunAsync());

            CollectionAssert.IsEmpty(log);
        });

        [UnityTest]
        public IEnumerator PassesCancellationTokenToTasks() => UniTask.ToCoroutine(async () =>
        {
            var task = new GatedTask(InitializationPriority.Normal, UniTask.CompletedTask);
            var bootInitializer = new BootInitializer(new IBootInitializationTask[] { task });
            using var cts = new CancellationTokenSource();

            await bootInitializer.RunAsync(cts.Token);

            Assert.AreEqual(cts.Token, task.ReceivedToken);
        });

        [UnityTest]
        public IEnumerator ThrowsWhenRunTwice() => UniTask.ToCoroutine(async () =>
        {
            var log = new List<string>();
            var bootInitializer = new BootInitializer(new IBootInitializationTask[]
            {
                new RecordingTask(InitializationPriority.Normal, "Normal", log),
            });

            await bootInitializer.RunAsync();
            await AssertThrowsAsync<InvalidOperationException>(bootInitializer.RunAsync());

            Assert.AreEqual(1, log.Count);
        });

        private static async UniTask AssertThrowsAsync<TException>(UniTask task) where TException : Exception
        {
            try
            {
                await task;
            }
            catch (TException)
            {
                return;
            }
            Assert.Fail($"{typeof(TException).Name} was not thrown.");
        }

        private sealed class RecordingTask : IBootInitializationTask
        {
            private readonly string name;
            private readonly List<string> log;

            public RecordingTask(InitializationPriority priority, string name, List<string> log)
            {
                Priority = priority;
                this.name = name;
                this.log = log;
            }

            public InitializationPriority Priority { get; }

            public UniTask InitializeAsync(CancellationToken ct)
            {
                log.Add(name);
                return UniTask.CompletedTask;
            }
        }

        private sealed class GatedTask : IBootInitializationTask
        {
            private readonly UniTask gate;

            public GatedTask(InitializationPriority priority, UniTask gate)
            {
                Priority = priority;
                this.gate = gate;
            }

            public InitializationPriority Priority { get; }
            public bool Started { get; private set; }
            public CancellationToken ReceivedToken { get; private set; }

            public UniTask InitializeAsync(CancellationToken ct)
            {
                Started = true;
                ReceivedToken = ct;
                return gate;
            }
        }

        private sealed class FailingTask : IBootInitializationTask
        {
            public FailingTask(InitializationPriority priority)
            {
                Priority = priority;
            }

            public InitializationPriority Priority { get; }

            public UniTask InitializeAsync(CancellationToken ct)
            {
                return UniTask.FromException(new InvalidOperationException("boot failed"));
            }
        }
    }
}
