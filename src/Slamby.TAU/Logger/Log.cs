using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Logger
{
    public class Log
    {
        private static ILog _log;

        static Log()
        {
            if (_log == null) InitLog();
        }

        public static void InitLog()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            log4net.Config.XmlConfigurator.Configure(new FileInfo(path + "\\Log4Net.config"));
            _log = LogManager.GetLogger("Slamby.Logger");
            _log.Debug("This is a DEBUG message test ");
            _log.Info("This is a INFO message test");
            _log.Warn("This is a WARN message test");
            _log.Error("This is a ERROR message test");
            _log.Fatal("This is a FATAL message test");
        }

        public static void Debug(string text)
        {
            _log.Debug(text);
        }

        public static void Info(string text)
        {
            _log.Info(text);
        }

        public static void Warn(string text)
        {
            _log.Warn(text);
        }

        public static void Error(Exception ex, string message = "", string applicationName = "")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                if (ex != null)
                {
                    _log.Error(string.Format("[Message]: {0} [StackTrace]: {1}", ex.Message, ex.StackTrace));
                    if (ex.InnerException != null)
                        _log.Error(string.Format("[InnerException]: {0} [StackTrace]: {1}", ex.InnerException.InnerException, ex.InnerException.StackTrace));
                }
            }
            else
            {
                if (ex != null)
                {
                    _log.Error(string.Format("[CustomMessage]: {2} [Message]: {0} [StackTrace]: {1}", ex.Message,
                        ex.StackTrace, message));
                    if (ex.InnerException != null)
                        _log.Error(string.Format("[InnerException]: {0} [StackTrace]: {1}", ex.InnerException.InnerException, ex.InnerException.StackTrace));
                }
                else

                    _log.Error(string.Format("[CustomMessage]: {0}", message));
            }
        }

        public static void Fatal(string text)
        {
            _log.Fatal(text);
        }
    }
}
