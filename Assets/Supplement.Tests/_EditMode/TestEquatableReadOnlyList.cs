using System.Collections.Generic;
using Is = UnityEngine.TestTools.Constraints.Is;
using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;

namespace Supplement.Tests.EditMode
{
    public class TestEquatableReadOnlyList
    {
        [Test]
        public void SameElementsInSameOrderAreEqual()
        {
            var left = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
            var right = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

            Assert.IsTrue(left == right);
            Assert.IsTrue(left.Equals((object)right));
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void DifferentOrderOrCountAreNotEqual()
        {
            var list = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

            Assert.IsTrue(list != new EquatableReadOnlyList<int>(new[] { 3, 2, 1 }));
            Assert.IsTrue(list != new EquatableReadOnlyList<int>(new[] { 1, 2 }));
            Assert.IsTrue(list != null);
        }

        [Test]
        public void NullElementsAreCompared()
        {
            var left = new EquatableReadOnlyList<string>(new[] { "a", null });

            Assert.IsTrue(left == new EquatableReadOnlyList<string>(new[] { "a", null }));
            Assert.IsTrue(left != new EquatableReadOnlyList<string>(new[] { "a", "b" }));
        }

        [Test]
        public void EqualsDoesNotAllocate()
        {
            var left = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
            var right = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
            // EqualityComparer<T>.Defaultの初回生成を計測から外す。
            _ = left == right;

            Assert.That(() => { _ = left == right; }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void ForeachDoesNotAllocate()
        {
            var list = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

            Assert.That(() =>
            {
                var sum = 0;
                foreach (var value in list)
                {
                    sum += value;
                }
            }, Is.Not.AllocatingGCMemory());
        }

        // インターフェース越しの列挙は列挙子がボックス化される。GCの計測が効いていることの確認も兼ねる。
        [Test]
        public void ForeachAsInterfaceAllocates()
        {
            IReadOnlyList<int> list = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

            Assert.That(() =>
            {
                var sum = 0;
                foreach (var value in list)
                {
                    sum += value;
                }
            }, Is.AllocatingGCMemory());
        }
    }
}