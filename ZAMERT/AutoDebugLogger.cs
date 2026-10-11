namespace ZAMERT
{
    internal static class AutoDebugLogger
    {
        public static void Debug(object m) => ZAMERTLogger.Debug(m);
        public static void Info(object m) => ZAMERTLogger.Info(m);
        public static void Warn(object m) => ZAMERTLogger.Warn(m);
        public static void Error(object m) => ZAMERTLogger.Error(m);
    }
}
