using System;
using Logger = LabApi.Features.Console.Logger;

namespace ZAMERT
{
    public static class ZAMERTLogger
    {
        public static void Raw(string message, ConsoleColor color) => Logger.Raw(message, color);
        public static void Debug(object message) => Logger.Debug(message, canBePrinted: ZAMERTPlugin.Singleton?.Config?.Debug ?? false);
        public static void Info(object message) => Logger.Info(message);
        public static void Warn(object message) => Logger.Warn(message);
        public static void Error(object message) => Logger.Error(message);
    }
}
