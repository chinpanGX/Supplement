using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestScopedList
    {
        [Test]
        public void AddBeyondInitialCapacityKeepsAllItems()
        {
            using var builder = new ScopedList<int>(2);
            for (var i = 0; i < 100; i++)
            {
                builder.Add(i);
            }

            Assert.AreEqual(100, builder.Count);
            Assert.AreEqual(99, builder[99]);
            CollectionAssert.AreEqual(Enumerable(100), builder.ToArray());
        }

        [Test]
        public void DefaultBuilderRentsOnFirstAdd()
        {
            using var builder = new ScopedList<string>();
            Assert.AreEqual(0, builder.Count);
            Assert.AreEqual(0, builder.AsSpan().Length);

            builder.Add("a");

            Assert.AreEqual("a", builder[0]);
        }

        [Test]
        public void IndexerReturnsReferenceToItem()
        {
            using var builder = new ScopedList<int>();
            builder.Add(1);

            builder[0] = 10;

            Assert.AreEqual(10, builder[0]);
        }

        [Test]
        public void IndexerOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                using var builder = new ScopedList<int>();
                builder.Add(1);
                _ = builder[1];
            });
        }

        [Test]
        public void ClearResetsCount()
        {
            using var builder = new ScopedList<int>();
            builder.Add(1);

            builder.Clear();

            Assert.AreEqual(0, builder.Count);
        }

        [Test]
        public void AddForeachAndDisposeDoNotAllocateAfterWarmUp()
        {
            static int Run()
            {
                using var builder = new ScopedList<int>();
                for (var i = 0; i < 100; i++)
                {
                    builder.Add(i);
                }

                var sum = 0;
                foreach (var value in builder)
                {
                    sum += value;
                }
                return sum;
            }

            // ArrayPoolが最初に配列を作る分を計測から外す。
            Run();

            Assert.That(() => { Run(); }, Is.Not.AllocatingGCMemory());
        }

        private static int[] Enumerable(int count)
        {
            var result = new int[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = i;
            }
            return result;
        }
    }
}