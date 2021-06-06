using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

using System;
using System.Collections.Generic;
using System.Text;

namespace DM.Net.Core {
    public class Logger {
        static public void Init()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = $@"logs\{ DateTime.Now.ToString( "yyyy-dd-MM" ) }.txt";
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 5;
            roller.MaximumFileSize = "1GB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender( roller );

            MemoryAppender memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender( memory );

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
        }
    }
    public class DMException : Exception {
        private ILog log = LogManager.GetLogger( "DMException" );
        public DMException( string msg ) : base( msg )
        {
            log.Error( msg );
        }
    }
}
