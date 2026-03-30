using LabApi.Events;
using LabApi.Features.Wrappers;
using ZAMERT.Events.Arguments;

namespace ZAMERT
{
    public static class ZAMERTApi
    {
        public static event LabEventHandler<HealthObjectTakingDamageEventArgs> HealthObjectTakingDamage;

        public static event LabEventHandler<HealthObjectDiedEventArgs> HealthObjectDied;

        public static event LabEventHandler<InteractableObjectInteractedEventArgs> InteractableObjectInteracted;

        internal static void OnHealthObjectTakingDamage(HealthObjectTakingDamageEventArgs ev)
            => HealthObjectTakingDamage.InvokeEvent(ev);

        internal static void OnHealthObjectDied(HealthObjectDiedEventArgs ev)
            => HealthObjectDied.InvokeEvent(ev);

        internal static void OnInteractableObjectInteracted(InteractableObjectInteractedEventArgs ev)
            => InteractableObjectInteracted.InvokeEvent(ev);
    }
}
