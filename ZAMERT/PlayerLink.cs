using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{
    public class PlayerLinkController : MonoBehaviour
    {
        private Player _player;
        private Transform _target;
        private ReferenceHub _dummyHub;
        private Player _dummy;
        private bool _lockRotation;
        private float _timer;
        private bool _flashOnEnd;
        private bool _originalMute;
        private Vector3 _originalPos;
        private Quaternion _originalRot;
        private Vector3 _originalScale;
        private bool _wasGodMode;
        private Vector3 _originalGravity;
        private ushort _previousItemSerial;
        private float _startDelay;
        private bool _isStarted;
        private float _endDelay;
        private bool _isEnding;
        private bool _dummyAlive;

        private void OnEnable()
        {
            PlayerEvents.Death += OnPlayerDeath;
            PlayerEvents.Left += OnPlayerLeft;
        }

        private void OnDisable()
        {
            PlayerEvents.Death -= OnPlayerDeath;
            PlayerEvents.Left -= OnPlayerLeft;
        }

        public void Init(Player player, Transform target, bool lockRotation, float duration, bool flashOnStart, bool flashOnEnd, bool createDummy)
        {
            if (player == null)
            {
                Destroy(this);
                return;
            }

            _player = player;
            _target = target;
            _lockRotation = lockRotation;
            _timer = Mathf.Max(0.05f, duration);
            _flashOnEnd = flashOnEnd;
            _originalMute = player.IsMuted;
            _originalPos = player.Position;
            _originalRot = player.Rotation;
            _originalScale = player.Scale;
            _originalGravity = player.Gravity;
            _wasGodMode = player.IsGodModeEnabled;
            _previousItemSerial = player.ReferenceHub != null ? player.ReferenceHub.inventory.CurItem.SerialNumber : (ushort)0;

            if (createDummy)
                SpawnDummy();

            if (flashOnStart)
            {
                player.EnableEffect<Flashed>(1, 1);
                _startDelay = 0.8f;
                _isStarted = false;
            }
            else
            {
                _startDelay = 0f;
                StartMoving();
            }
        }

        private void StartMoving()
        {
            if (_player == null)
                return;

            _isStarted = true;
            _player.Mute();
            _player.IsGodModeEnabled = true;
            _player.Gravity = Vector3.zero;
            _player.Scale = Vector3.zero;

            if (_target != null)
            {
                _player.Position = _target.position;
                if (_lockRotation)
                    _player.Rotation = _target.rotation;
            }
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                Destroy(this);
                return;
            }

            if (!_isStarted)
            {
                _startDelay -= Time.deltaTime;
                if (_startDelay <= 0f)
                    StartMoving();
                return;
            }

            if (_isEnding)
            {
                _endDelay -= Time.deltaTime;
                if (_endDelay <= 0f)
                    FinalizeLink();
                return;
            }

            if (_target == null || _timer <= 0f)
            {
                BeginEndLink();
                return;
            }

            _timer -= Time.deltaTime;
            _player.Position = _target.position;

            if (_lockRotation)
                _player.Rotation = _target.rotation;

            if (_player.ReferenceHub != null)
                _player.ReferenceHub.inventory.ServerSelectItem(0);
        }

        private void BeginEndLink()
        {
            if (_isEnding)
                return;

            if (_flashOnEnd && _player != null)
            {
                _player.EnableEffect<Flashed>(1, 1);
                _isEnding = true;
                _endDelay = 0.8f;
            }
            else
            {
                FinalizeLink();
            }
        }

        private void FinalizeLink()
        {
            if (_player != null)
            {
                _player.IsGodModeEnabled = _wasGodMode;
                _player.Gravity = _originalGravity;
                _player.Scale = _originalScale;

                if (!_originalMute)
                    _player.Unmute(false);

                if (_dummyAlive)
                {
                    _player.Position = _originalPos;
                    _player.Rotation = _originalRot;
                    RestoreItemSelection();
                }
                else
                {
                    _player.ClearInventory();
                    _player.SetRole(RoleTypeId.Spectator);
                }
            }

            CleanupDummy();
            Destroy(this);
        }

        private void RestoreItemSelection()
        {
            if (_previousItemSerial == 0 || _player == null || _player.ReferenceHub == null)
                return;

            Timing.CallDelayed(0.2f, () =>
            {
                if (_player != null && _player.ReferenceHub != null)
                    _player.ReferenceHub.inventory.ServerSelectItem(_previousItemSerial);
            });
        }

        private void SpawnDummy()
        {
            _dummyHub = DummyUtils.SpawnDummy(_player.Nickname);
            if (_dummyHub == null)
                return;

            Timing.CallDelayed(0.2f, () =>
            {
                _dummy = Player.Get(_dummyHub);
                if (_dummy == null || _player == null)
                    return;

                _dummy.Role = _player.Role;
                _dummyAlive = true;

                Timing.CallDelayed(0.3f, () =>
                {
                    if (_dummy == null || _player == null)
                        return;

                    _dummy.IsGodModeEnabled = true;
                    _dummy.Position = _originalPos;
                    _dummy.Rotation = _originalRot;
                    _dummy.Scale = _originalScale;
                    _dummy.CustomInfo = _player.CustomInfo;
                    _dummy.InfoArea = _player.InfoArea;
                    _dummy.UserGroup = _player.UserGroup;
                    _dummy.IsSpectatable = false;

                    foreach (var itemBase in _player.Inventory.UserInventory.Items.Values)
                        _dummy.AddItem(itemBase.ItemTypeId);

                    if (_previousItemSerial != 0 && _player.Inventory.UserInventory.Items.ContainsKey(_previousItemSerial))
                    {
                        ItemType originalType = _player.Inventory.UserInventory.Items[_previousItemSerial].ItemTypeId;
                        Item matchingItem = _dummy.Items.FirstOrDefault(item => item.Type == originalType);
                        if (matchingItem != null)
                            _dummy.CurrentItem = matchingItem;
                    }
                });
            });
        }

        private void CleanupDummy()
        {
            if (_dummy != null && _dummy.GameObject != null)
                NetworkServer.Destroy(_dummy.GameObject);
        }

        private void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev.Player != _player)
                return;

            if (_dummy != null)
                _dummy.Kill();
        }

        private void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            if (ev.Player == _dummy)
                _dummyAlive = false;
        }
    }
}
