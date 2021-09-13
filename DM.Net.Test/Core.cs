using NUnit.Framework;
using DM.Net.Core;
using System.IO;
using System;
using System.Collections.Generic;

namespace DM.Net.Test {
    public class TargetResourceTests {
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

    public class DiskTests {
        [SetUp]
        public void Setup()
        {
            Logger.Init();
        }

        [Test]
        public void Test_IndiceDisks()
        {
            var dbDir = Path.Combine( Directory.GetCurrentDirectory(), "db" );
            if ( !Directory.Exists(dbDir) ) {
                Directory.CreateDirectory( dbDir );
            }
            var s = new List<TargetResouceManagers.DiskInfo>();
            var ms = new TargetResouceManagers( dbDir, ref s );
            var ds = ms.IndiceDisksOnThisPC();
        }


        [Test]
        public void Test_IndiceFiles()
        {
            var dbDir = Path.Combine( Directory.GetCurrentDirectory(), "db" );
            if ( !Directory.Exists( dbDir ) ) {
                Directory.CreateDirectory( dbDir );
            }
            var s = new List<TargetResouceManagers.DiskInfo>();
            var ms = new TargetResouceManagers( dbDir, ref s );
            var diskM = ms.Handles.Find( m => { return m.mMeta.GUID == "1ccb4746-9b95-476d-8660-0ca9990db594"; } );
            // ms.IndiceAllFileRecursionInDisk( "1ccb4746-9b95-476d-8660-0ca9990db594", @"M:\" );
        }
        [Test]
        public void Test_GetDisksStatus()
        {

        }
    }
}