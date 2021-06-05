using NUnit.Framework;
using DM.Net.Core;

namespace DM.Net.Test {
    public class Tests {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Test_IsSameFile_SameFile()
        {
            TargetResource tg1 = new TargetResource { Path = @"L:\TEST\go.gif" };
            TargetResource tg2 = new TargetResource { Path = @"L:\TEST\go1.gif" };
            Assert.IsTrue( tg1.IsSameFile( tg2 ) );
        }
        [Test]
        public void Test_IsSameFile_DifferentFiles()
        {
            TargetResource tg1 = new TargetResource { Path = @"L:\TEST\go.gif" };
            TargetResource tg2 = new TargetResource { Path = @"L:\TEST\1.jpg" };
            Assert.IsFalse( tg1.IsSameFile( tg2 ) );
        }
        [Test]
        public void Test_IsSameFile_DifferentFilesFullMD5()
        {
            TargetResource tg1 = new TargetResource { Path = @"L:\TEST\go.gif" };
            TargetResource tg2 = new TargetResource { Path = @"L:\TEST\1.jpg" };
            Assert.IsTrue( tg1.GetFullMD5() != tg2.GetFullMD5() );
        }
    }
}