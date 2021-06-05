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
    }
    public class TargetResouceManager {

    }
    public class TargetResource {
        public Int64 Id { get; set; }
        public string Description { get; set; }
        public string Uni { get; set; }
        public string Path { get; set; }
        public List<TargetResource> BackupList { get; set; }
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
                throw new Exception( $"In GetMD5():\n {this} \n {ex.Message} \n {ex.StackTrace}" );
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
                throw new Exception( $"In IsSameFile():\n {this} \n {ex.Message} \n {ex.StackTrace}" );
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
