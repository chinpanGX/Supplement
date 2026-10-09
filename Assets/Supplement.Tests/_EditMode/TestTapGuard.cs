using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestTapGuard
    {
        [Test]
        public void IsNotGuardingInitially()
        {
            var guard = new TapGuard();

            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void IsGuardingUntilHandleDisposed()
        {
            var guard = new TapGuard();

            var handle = guard.BeginGuard();
            Assert.IsTrue(guard.IsGuarding);

            handle.Dispose();
            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void UsingBlockEndsGuardOnExit()
        {
            var guard = new TapGuard();

            using (guard.BeginGuard())
            {
                Assert.IsTrue(guard.IsGuarding);
            }

            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void NestedGuardsContinueUntilAllHandlesDisposed()
        {
            var guard = new TapGuard();

            var outer = guard.BeginGuard();
            var inner = guard.BeginGuard();

            inner.Dispose();
            Assert.IsTrue(guard.IsGuarding);

            outer.Dispose();
            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void NestedHandlesCanBeDisposedInAnyOrder()
        {
            var guard = new TapGuard();

            var first = guard.BeginGuard();
            var second = guard.BeginGuard();

            first.Dispose();
            Assert.IsTrue(guard.IsGuarding);

            second.Dispose();
            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void DisposingSameHandleTwiceDoesNotReleaseOtherGuards()
        {
            var guard = new TapGuard();

            var first = guard.BeginGuard();
            using var second = guard.BeginGuard();

            first.Dispose();
            first.Dispose();

            Assert.IsTrue(guard.IsGuarding);
        }

        [Test]
        public void CanGuardAgainAfterRelease()
        {
            var guard = new TapGuard();

            guard.BeginGuard().Dispose();

            using (guard.BeginGuard())
            {
                Assert.IsTrue(guard.IsGuarding);
            }

            Assert.IsFalse(guard.IsGuarding);
        }

        [Test]
        public void DisposingCopiedHandleReleasesOnlyOnce()
        {
            var guard = new TapGuard();

            var handle = guard.BeginGuard();
            var copy = handle;
            using var other = guard.BeginGuard();

            handle.Dispose();
            copy.Dispose();

            Assert.IsTrue(guard.IsGuarding);
        }

        [Test]
        public void BeginAndEndGuardDoNotAllocate()
        {
            var guard = new TapGuard();
            guard.BeginGuard().Dispose();

            Assert.That(() =>
            {
                using (guard.BeginGuard())
                {
                }
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void GuardsAreIndependentPerInstance()
        {
            var guardA = new TapGuard();
            var guardB = new TapGuard();

            using (guardA.BeginGuard())
            {
                Assert.IsTrue(guardA.IsGuarding);
                Assert.IsFalse(guardB.IsGuarding);
            }
        }
    }
}
