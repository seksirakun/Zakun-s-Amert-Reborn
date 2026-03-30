using LabApi.Events;
using ZAMERT.Events.Arguments;

namespace ZAMERT.Events.Handlers
{
    public static class InteractableObjectEventHandlers
    {
        public static event LabEventHandler<InteractableObjectInteractedEventArgs> InteractableObjectInteracted
        {
            add { ZAMERTApi.InteractableObjectInteracted += value; }
            remove { ZAMERTApi.InteractableObjectInteracted -= value; }
        }

        public static void OnPlayerIOInteracted(InteractableObjectInteractedEventArgs ev)
            => ZAMERTApi.OnInteractableObjectInteracted(ev);
    }
}
