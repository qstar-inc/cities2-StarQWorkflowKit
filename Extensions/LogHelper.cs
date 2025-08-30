using System;
using System.IO;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game.UI.Localization;

namespace StarQWorkflowKit.Extensions
{
    public enum LogLevel
    {
        Verbose,
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Critical,
        Fatal,
        Emergency,
    }

    public class LogHelper
    {
        public static LocalizedString LogText => LocalizedString.Id(logText);
        private static string logText = LocaleHelper.Translate($"{Mod.Id}.Mod.NoLog");

        public static void SendLog(string message, LogLevel level = LogLevel.Info)
        {
            ILog log = Mod.log;

            switch (level)
            {
                case LogLevel.Verbose:
                    log.Verbose(message);
                    break;
                case LogLevel.Trace:
                    log.Trace(message);
                    break;
                case LogLevel.Debug:
                    log.Debug(message);
                    break;
                case LogLevel.Info:
                    log.Info(message);
                    break;
                case LogLevel.Warn:
                    log.Warn(message);
                    break;
                case LogLevel.Error:
                    log.Error(message);
                    break;
                case LogLevel.Critical:
                    log.Critical(message);
                    break;
                case LogLevel.Fatal:
                    log.Fatal(message);
                    break;
                case LogLevel.Emergency:
                    log.Emergency(message);
                    break;
                default:
                    log.Info(message);
                    break;
            }
            try
            {
                logText = File.ReadAllText($"{EnvPath.kUserDataPath}/Logs/{Mod.Id}.log");
            }
            catch (Exception e)
            {
                log.Info(e);
            }
        }

        public static void SendLog(Exception exception, LogLevel level = LogLevel.Info)
        {
            SendLog($"{exception}", level);
        }
    }
}
