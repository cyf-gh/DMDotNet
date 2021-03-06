using log4net;
using log4net.Core;

using SQLite;

using SQLiteNetExtensions.Attributes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DM.Net.Core {
    public class Tag {
        public Int64 Id { get; set; }
        public string Name { get; set; }
    }
    public class ByteHelper {
        public static string ByteArrayToString( byte[] arrInput )
        {
            int i;
            StringBuilder sOutput = new StringBuilder( arrInput.Length );
            for ( i = 0; i < arrInput.Length; i++ ) {
                sOutput.Append( arrInput[i].ToString( "X2" ) );
            }
            return sOutput.ToString();
        }
        public static string GUID()
        {
            Guid g = Guid.NewGuid();
            return g.ToString();
        }
    }
    public class TargetResouceManagers {
        public class DiskInfo {
            public string GUID { get; set; }
        }
        private ILog log = LogManager.GetLogger( "TargetResouceManagers" );
        private SQLiteConnection mMetaDB;
        private readonly string dbPath;
        public List<TargetResouceManager> Handles { get; set; } = new List<TargetResouceManager>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbPath"></param>
        /// <param name="fileDBNotInMetaDBs">存在db数据库，但meta中未索引该数据库，应当提示用户添加至meta</param>
        public TargetResouceManagers( string dbPath, ref List<DiskInfo> fileDBNotInMetaDBs )
        {
            this.dbPath = dbPath;
            mMetaDB = new SQLiteConnection( Path.Combine( dbPath, "___meta___.db" ) );
            mMetaDB.CreateTable<Disk>();
            mMetaDB.CreateTable<Tag>();

            DirectoryInfo folder = new DirectoryInfo( dbPath );
            foreach ( FileInfo file in folder.GetFiles( "*.db" ) ) {
                if ( file.Name == "___meta___.db" ) {
                    continue;
                 }
                var GUID = Path.GetFileNameWithoutExtension( file.Name );
                var d = mMetaDB.Table<Disk>().ToList().Find( d => d.GUID == GUID );
                if ( d == null ) {
                    // 存在db数据库，但meta中未索引该数据库，应当提示用户添加至meta
                    fileDBNotInMetaDBs.Add( new DiskInfo { GUID = GUID } );
                    continue;
                }
                Handles.Add( new TargetResouceManager( file.FullName, d ) );
            }
        }
        /// <summary>
        /// 获取所有磁盘的情况
        /// </summary>
        /// <param name="onlineDisks">已在meta中索引，且目处于激活状态的硬盘</param>
        /// <param name="offlineDisks">已在meta中索引，但目前不可使用的硬盘</param>
        /// <param name="notIndicatedDisks">未被索引，但处于激活状态的硬盘</param>
        public void GetDisksStatus( ref List<DiskInfo> onlineDisks, ref List<DiskInfo> offlineDisks, ref List<DriveInfo> notIndicatedDisks )
        {
            var infos = DriveInfo.GetDrives();
            var allDisks = mMetaDB.Table<Disk>().ToList();
            var GUID = string.Empty;
            foreach ( var i in infos ) {
                if ( i.IsReady ) {
                    if ( !IsDiskInDB( i, ref GUID ) ) {
                        notIndicatedDisks.Add(i);
                    } else {
                        onlineDisks.Add( new DiskInfo { GUID = GUID } );
                        allDisks.Remove( allDisks.Find( d => d.GUID == GUID ) );
                    }
                }
            }
            foreach ( var d in allDisks ) {
                offlineDisks.Add( new DiskInfo { GUID = d.GUID } );
            }
        }
        public List<Disk> GetAllDisks() => mMetaDB.Table<Disk>().ToList();
        public List<Tag> GetAllTags() => mMetaDB.Table<Tag>().ToList();
        /// <summary>
        /// 通过是否存在.dm目录判断磁盘是否被索引
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool IsDiskIndiced( DriveInfo info )
        {
            foreach ( var d in info.RootDirectory.GetDirectories() ) {
                if ( d.Name == ".dm" ) {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断磁盘是否已被索引入数据库
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool IsDiskInDB( DriveInfo i, ref string GUID )
        {
            if ( !IsDiskIndiced(i) ) {
                return false;
            }
            var metaPath = Path.Combine( i.RootDirectory.FullName, ".dm" );
            var metaDir = new DirectoryInfo( metaPath );
            foreach ( var f in metaDir.GetFiles() ) {
                var query = mMetaDB.Table<Disk>().ToList().Find( e => e.GUID == Path.GetFileNameWithoutExtension( f.Name ) );
                if ( query != null ) { 
                    GUID = query.GUID;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 索引磁盘
        /// </summary>
        /// <see cref="IndiceDisksOnThisPC()"/>
        /// <param name="i"></param>
        public void IndiceDisk( DriveInfo i )
        {
            try {
                if ( i.DriveType == DriveType.CDRom || i.DriveType == DriveType.Network || i.DriveType == DriveType.Ram ) {
                    log.Warn($"VolumeLabel: {i.VolumeLabel}\n DriveType: {i.DriveType}.\n, which has not been supported in dm yet.");
                    return;
                }

                var metaPath = Path.Combine( i.RootDirectory.FullName, ".dm" );
                var guid = ByteHelper.GUID();
                Directory.CreateDirectory( metaPath );
                File.Create( Path.Combine( metaPath, guid ) );

                mMetaDB.Insert( new Disk {
                    Letter = i.VolumeLabel,
                    GUID = guid,
                    RestSize = i.AvailableFreeSpace,
                    TotalSize = i.TotalSize,
                    Type = i.DriveType.ToString(),
                    DriveFormat = i.DriveFormat,
                    LastUpdateDate = DateTime.Now,
                } );
            } catch ( Exception ex ) {
                throw new DMException( $"In IndiceDisk(): { i.DriveType.ToString() } \n {ex.Message} \n {ex.StackTrace}" );
            }
        }
        /// <summary>
        /// 索引当前PC上的所有磁盘
        /// </summary>
        /// <returns></returns>
        public List<Disk> IndiceDisksOnThisPC()
        {
            var GUID = "";
            var infos = DriveInfo.GetDrives();
            foreach ( var i in infos ) {
                 // 挂载磁盘类型于IndiceDisk内解决
                if ( i.IsReady ) {
                    if ( !IsDiskInDB( i, ref GUID ) ) {
                        IndiceDisk( i );
                    }
                }
            }
            return mMetaDB.Table<Disk>().ToList();
        }
        /// <summary>
        /// 所有文件递归索引
        /// </summary>
        /// <param name="guid">该磁盘的GUID</param>
        /// <param name="root">形如C:\的路径</param>
        public void IndiceAllFileRecursionInDisk( string guid, string root )
        {
            SQLiteConnection db = new SQLiteConnection( Path.Combine( dbPath, guid + ".db" ) );
            db.CreateTable<TargetResource>();
            long count = -1;
            try {
                string[] files = Directory.GetFiles( root, "*.*", SearchOption.AllDirectories );
                count = files.Length;
                
                foreach ( string f in files ) {
                    var fi = new FileInfo( f );
                    db.Insert( new TargetResource {
                        BackupList = new List<TargetResource>(),
                        Path = fi.FullName,
                        Rating = 0,
                        Size = fi.Length,
                        Uni = "",
                        Tags = new List<Tag>(),
                        Description = ""
                    } );
                    count--;
                }
            } catch ( Exception ex ) {
                throw new DMException( $"In IndiceAllFileRecursion(): \n {ex.Message} \n {ex.StackTrace} \n rest count = { count }"  );
            }
        }
    }
    public class AsyncPackage {
        public long Total { get; set; }
        public long Current { get; set; }

    }
    /// <summary>
    /// TargetResouce映射于每个磁盘
    /// </summary>
    /// <seealso cref="TargetResouceManagers"/>
    public class TargetResouceManager {
        private SQLiteConnection mDB;
        public readonly Disk mMeta;

        public TargetResouceManager( string dbPath, Disk meta )
        {
            mMeta = meta;
            mDB = new SQLiteConnection( dbPath );
            mDB.CreateTable<TargetResource>();
        }

        public bool IndicateAllFilesRecursion( string root )
        {
            long count = -1;
            try {
                string[] files = Directory.GetFiles( root, "*.*", SearchOption.AllDirectories );
                count = files.Length;

                foreach ( string f in files ) {
                    var fi = new FileInfo( f );
                    mDB.Insert( new TargetResource {
                        BackupList = new List<TargetResource>(),
                        Path = fi.FullName,
                        Rating = 0,
                        Size = fi.Length,
                        Uni = "",
                        Tags = new List<Tag>(),
                        Description = ""
                    } );
                    count--;
                }
            } catch ( Exception ex ) {
                throw new DMException( $"In {mMeta.GUID} IndicateAllFilesRecursion(): \n {ex.Message} \n {ex.StackTrace} \n rest count = { count }" );
            }
            return true;
        }
    }
    public class Disk {
        [PrimaryKey, AutoIncrement]
        public Int64 Id { get; set; }
        public string GUID { get; set; }
        public string Letter { get; set; }
        public string Type { get; set; }
        public long TotalSize { get; set; }
        public long RestSize { get; set; }
        public string DriveFormat { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
    public class Task {
        [PrimaryKey, AutoIncrement]
        public Int64 Id { get; set; }
        public string Description { get; set; }
        public Disk OperateDisk { get; set; }
        public DateTime Date { get; set; }
        public string GUID { get; set; }
    }
    public class TargetResource {
        [PrimaryKey, AutoIncrement]
        public Int64 Id { get; set; }
        public string Description { get; set; }
        public string Uni { get; set; }
        public string Path { get; set; }
        [OneToMany( CascadeOperations = CascadeOperation.All )]
        public List<TargetResource> BackupList { get; set; }
        [OneToMany( CascadeOperations = CascadeOperation.All )]
        public List<Tag> Tags { get; set; }
        public int Rating { get; set; }
        public Int64 Size { get; set; }
        /// <summary>
        /// 获取完整的MD5校验值
        /// </summary>
        /// <returns></returns>
        public byte[] GetFullMD5()
        {
            try {
                using ( FileStream file = new FileStream( Path, FileMode.Open ) ) {
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    byte[] retVal = md5.ComputeHash( file );
                    return retVal;
                }
            } catch ( Exception ex ) {
                throw new DMException( $"In GetMD5():\n {this} \n {ex.Message} \n {ex.StackTrace}" );
            }
        }
        public bool IsSameFile( TargetResource r, int blockSize = 512 )
        {
            if ( r.Size != Size ) {
                return false;
            }
            if ( r.Path == Path ) {
                return true;
            }
            byte[] b1, b2;
            FileStream f1 = null, f2 = null;
            int offset = 0;
            try {
                f1 = new FileStream( Path, FileMode.Open );
                f2 = new FileStream( r.Path, FileMode.Open );
                b1 = new byte[blockSize];
                b2 = new byte[blockSize];
                // f1.Length == f2.Length == true
                while ( offset < f1.Length ) {
                    var count = offset + blockSize > f1.Length ? (int)( f1.Length - offset ) : blockSize;
                    f1.Read( b1, 0, count );
                    f2.Read( b2, 0, count );
                    offset += count;
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    if ( ByteHelper.ByteArrayToString( md5.ComputeHash( b1 ) ) != ByteHelper.ByteArrayToString( md5.ComputeHash( b2 ) ) ) {
                        return false;
                    }
                }
                return true;
            } catch ( Exception ex ) {
                throw new DMException( $"In IsSameFile():\n {this} \n {ex.Message} \n {ex.StackTrace}" );
            } finally {
                if ( f1 != null ) {
                    f1.Close();
                }
                if ( f2 != null ) {
                    f2.Close();
                }
            }
        }
    }
}
