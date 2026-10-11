using LabApi.Events;
using ZAMERT.Events.Arguments;

namespace ZAMERT.Events.Handlers
{
    public static class HealthObjectEventHandlers
    {
        public static event LabEventHandler<HealthObjectDiedEventArgs> HealthObjectDied
        {
            add { ZAMERTApi.HealthObjectDied += value; }
            remove { ZAMERTApi.HealthObjectDied -= value; }
        }

        public static event LabEventHandler<HealthObjectTakingDamageEventArgs> HealthObjectTakingDamage
        {
            add { ZAMERTApi.HealthObjectTakingDamage += value; }
            remove { ZAMERTApi.HealthObjectTakingDamage -= value; }
        }

        internal static void OnHealthObjectDied(HealthObjectDiedEventArgs ev)
            => ZAMERTApi.OnHealthObjectDied(ev);

        internal static void OnHealthObjectTakingDamage(HealthObjectTakingDamageEventArgs ev)
            => ZAMERTApi.OnHealthObjectTakingDamage(ev);
    }
}
