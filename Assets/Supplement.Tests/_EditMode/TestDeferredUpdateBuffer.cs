using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools;

namespace Supplement.Tests.EditMode
{
    public class TestDeferredUpdateBuffer
    {
        private sealed class ControlledUpdater : IBulkUpdater<int>
        {
            public readonly List<List<int>> Committed = new();
            public UniTaskCompletionSource Pending;
            public bool Fail;

            public async UniTask UpdateAsync(List<int> entities)
            {
                // 保存の時点の中身を記録する(呼び出し後にバッファが使い回されても影響を受けないよう、コピーする)
                Committed.Add(new List<int>(entities));
                if (Pending != null)
                {
                    await Pending.Task;
                }

                if (Fail)
                {
                    throw new InvalidOperationException("update failed");
                }
            }
        }

        [UnityTest]
        [Description("保存を待っている間に次のBegin・Addを始めても、追加した分は消えずに次のコミットで保存される")]
        public IEnumerator AddDuringCommitIsKeptForNextCommit() => UniTask.ToCoroutine(async () =>
        {
            var updater = new ControlledUpdater { Pending = new UniTaskCompletionSource() };
            var buffer = new DeferredUpdateBuffer<int>(updater);

            buffer.Begin();
            buffer.Add(1);
            var firstCommit = buffer.CommitAsync();

            buffer.Begin();
            buffer.Add(2);
            updater.Pending.TrySetResult();
            await firstCommit;

            updater.Pending = null;
            await buffer.CommitAsync();

            CollectionAssert.AreEqual(new[] { 1 }, updater.Committed[0]);
            CollectionAssert.AreEqual(new[] { 2 }, updater.Committed[1]);
        });

        [UnityTest]
        [Description("保存が重なっても(前のコミットを待たずに次をコミットしても)、それぞれ自分の分だけを保存する")]
        public IEnumerator OverlappingCommitsSaveTheirOwnEntities() => UniTask.ToCoroutine(async () =>
        {
            var updater = new ControlledUpdater { Pending = new UniTaskCompletionSource() };
            var buffer = new DeferredUpdateBuffer<int>(updater);

            buffer.Begin();
            buffer.Add(1);
            var firstCommit = buffer.CommitAsync();

            buffer.Begin();
            buffer.Add(2);
            var secondCommit = buffer.CommitAsync();

            updater.Pending.TrySetResult();
            await firstCommit;
            await secondCommit;

            CollectionAssert.AreEqual(new[] { 1 }, updater.Committed[0]);
            CollectionAssert.AreEqual(new[] { 2 }, updater.Committed[1]);
        });

        [UnityTest]
        [Description("保存に失敗した分はバッファに残らず、次のコミットで一緒に保存されない")]
        public IEnumerator FailedCommitDoesNotLeakIntoNextCommit() => UniTask.ToCoroutine(async () =>
        {
            var updater = new ControlledUpdater { Fail = true };
            var buffer = new DeferredUpdateBuffer<int>(updater);

            buffer.Begin();
            buffer.Add(1);
            try
            {
                await buffer.CommitAsync();
                Assert.Fail("Commit should fail.");
            }
            catch (InvalidOperationException)
            {
            }

            updater.Fail = false;
            buffer.Begin();
            buffer.Add(2);
            await buffer.CommitAsync();

            CollectionAssert.AreEqual(new[] { 2 }, updater.Committed[1]);
        });
    }
}
