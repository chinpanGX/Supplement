using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestRentedList
    {
        [Test]
        public void AddBeyondInitialCapacityKeepsAllItems()
        {
            using var list = RentedList<int>.Rent(2);
            for (var i = 0; i < 100; i++)
            {
                list.Add(i);
            }

            Assert.AreEqual(100, list.Count);
            Assert.AreEqual(99, list[99]);
            var expected = 0;
            foreach (var value in list)
            {
                Assert.AreEqual(expected++, value);
            }
        }

        [Test]
        public void DisposedInstanceIsReusedEmpty()
        {
            var first = RentedList<string>.Rent();
            first.Add("a");
            first.Dispose();

            using var second = RentedList<string>.Rent();

            Assert.AreSame(first, second);
            Assert.AreEqual(0, second.Count);
        }

        [Test]
        public void DisposingTwiceReturnsToPoolOnlyOnce()
        {
            var list = RentedList<long>.Rent();
            list.Dispose();
            list.Dispose();

            using var first = RentedList<long>.Rent();
            using var second = RentedList<long>.Rent();

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void IndexerOutOfRangeThrows()
        {
            using var list = RentedList<int>.Rent();
            list.Add(1);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => _ = list[1]);
        }

        [Test]
        public void RentAddForeachAndDisposeDoNotAllocateAfterWarmUp()
        {
            static int Run()
            {
                using var list = RentedList<int>.Rent();
                for (var i = 0; i < 100; i++)
                {
                    list.Add(i);
                }

                var sum = 0;
                foreach (var value in list)
                {
                    sum += value;
                }
                return sum;
            }

            // インスタンスとArrayPoolの配列が最初に作られる分を計測から外す。
            Run();

            Assert.That(() => { Run(); }, Is.Not.AllocatingGCMemory());
        }
    }
}