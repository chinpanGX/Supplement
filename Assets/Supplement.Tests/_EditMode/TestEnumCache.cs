using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestEnumCache
    {
        private enum Sample
        {
            A = 0,
            B = 1,
            C = 4,
        }

        [Test]
        public void ValuesAndNamesAreInValueOrder()
        {
            CollectionAssert.AreEqual(new[] { Sample.A, Sample.B, Sample.C }, EnumCache<Sample>.Values.ToArray());
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, EnumCache<Sample>.Names.ToArray());
        }

        [Test]
        public void GetNameReturnsDefinedName()
        {
            Assert.AreEqual("C", EnumCache<Sample>.GetName(Sample.C));
        }

        [Test]
        public void GetNameFallsBackToToStringForUndefinedValue()
        {
            Assert.AreEqual("2", EnumCache<Sample>.GetName((Sample)2));
        }

        [Test]
        public void IsDefinedMatchesEnumIsDefined()
        {
            Assert.IsTrue(EnumCache<Sample>.IsDefined(Sample.B));
            Assert.IsFalse(EnumCache<Sample>.IsDefined((Sample)2));
        }

        [Test]
        public void GetNameAndIsDefinedDoNotAllocate()
        {
            EnumCache<Sample>.GetName(Sample.B);

            Assert.That(() =>
            {
                EnumCache<Sample>.GetName(Sample.B);
                EnumCache<Sample>.IsDefined(Sample.C);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
