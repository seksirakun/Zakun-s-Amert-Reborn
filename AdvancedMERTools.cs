namespace ZAMERT
{

    internal static class AdvancedMERTools
    {
        internal static ZAMERTPlugin Singleton => ZAMERTPlugin.Singleton;
        internal static void ExecuteCommand(string ctx) => ZAMERTPlugin.ExecuteCommand(ctx);
    }
}
