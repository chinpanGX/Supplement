using System.Text;
using NUnit.Framework;
using Supplement.Core;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Supplement.Tests.EditMode
{
    public class TestCrc32
    {
        // 保存ファイル名に使っているため、ArrayPoolを使う実装へ書き換える前の出力と一致し続けることを確かめる。
        [TestCase("", "00000000")]
        [TestCase("123456789", "cbf43926")]
        [TestCase("deviceCredentials", "98e11467")]
        [TestCase("staleTempFileTest", "f7d29ee4")]
        [TestCase("日本語のキー", "60776241")]
        public void ComputeKeepsPreviousOutput(string source, string expected)
        {
            Assert.AreEqual(expected, Crc32.Compute(source));
        }

        [Test]
        public void ComputeKeepsPreviousOutputForLongString()
        {
            Assert.AreEqual("89971909", Crc32.Compute(new string('a', 300)));
        }

        [Test]
        public void ComputeSpanReturnsStandardCrc32()
        {
            Assert.AreEqual(0xCBF43926u, Crc32.Compute(Encoding.UTF8.GetBytes("123456789")));
        }

        [Test]
        public void ComputeSpanDoesNotAllocate()
        {
            var bytes = Encoding.UTF8.GetBytes("123456789");
            Crc32.Compute(bytes);

            Assert.That(() => { Crc32.Compute(bytes); }, Is.Not.AllocatingGCMemory());
        }
    }
}