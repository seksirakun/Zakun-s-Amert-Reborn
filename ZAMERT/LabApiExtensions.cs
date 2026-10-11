using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace ZAMERT
{
    public static class LabApiExtensions
    {
        public static bool IsAirborne(this Player player)
            => player.RoleBase is IFpcRole fpc && !fpc.FpcModule.IsGrounded;

        public static bool IsJumping(this Player player)
            => player.RoleBase is IFpcRole fpc && fpc.FpcModule.Motor.JumpController.IsJumping;

        public static bool IsReloading(this Player player)
            => player.CurrentItem is FirearmItem fi && fi.Base.TryGetModule(out IReloaderModule m) && m.IsReloading;

        public static bool IsUsingStamina(this Player player)
            => player.RoleBase is IFpcRole fpc && fpc.FpcModule.CurrentMovementState.HasFlag(PlayerMovementState.Sprinting);

        public static string GetUniqueRole(this Player player)
            => player.RoleBase.RoleName;

        public static void SendSafeHitMarker(this Player player, float size)
        {
            if (player == null)
                return;

            try
            {
                player.SendHitMarker(Mathf.Max(0.1f, size));
            }
            catch (System.Exception ex)
            {
                ZAMERTLogger.Warn("Failed to send hitmarker to " + player.Nickname + ": " + ex.Message);
            }
        }
    }
}
