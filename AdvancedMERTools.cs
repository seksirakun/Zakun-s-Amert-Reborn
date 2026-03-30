namespace ZAMERT
{
    // Keep this so old internal references compile during transition
    internal static class AdvancedMERTools
    {
        internal static ZAMERTPlugin Singleton => ZAMERTPlugin.Singleton;
        internal static void ExecuteCommand(string ctx) => ZAMERTPlugin.ExecuteCommand(ctx);
    }
}
