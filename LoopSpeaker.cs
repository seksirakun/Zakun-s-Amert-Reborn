using System.IO;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class LoopSpeaker : ZAMERTInteractable
    {
        public new LSDTO Base { get; set; }

        private AudioPlayer _audioPlayer;
        private string _currentClip;
        private float _currentVolume;

        protected void Start()
        {
            Base = base.Base as LSDTO;
            Log.Debug("Registering LoopSpeaker: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.LoopSpeakers.Contains(this))
                ZAMERTPlugin.Singleton.LoopSpeakers.Add(this);

            if (Base.AutoStart && !string.IsNullOrEmpty(Base.AudioName))
                MEC.Timing.CallDelayed(0.5f, () => Play());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAndDestroy();
            ZAMERTPlugin.Singleton?.LoopSpeakers?.Remove(this);
        }

        private void EnsureClipLoaded(string audioName)
        {
            if (string.IsNullOrEmpty(audioName)) return;
            if (!Directory.Exists(ZAMERTPlugin.Singleton.Config.AudioFolderPath))
            {
                Log.Warn("LoopSpeaker: audio folder not found: " + ZAMERTPlugin.Singleton.Config.AudioFolderPath);
                return;
            }
            if (!AudioClipStorage.AudioClips.ContainsKey(audioName))
            {
                AudioClipStorage.LoadClip(
                    System.IO.Path.Combine(ZAMERTPlugin.Singleton.Config.AudioFolderPath, audioName),
                    audioName);
            }
        }

        private void StopAndDestroy()
        {
            if (_audioPlayer != null)
            {
                try { _audioPlayer.Destroy(); } catch { }
                _audioPlayer = null;
            }
        }

        public void Play(string audioName = null)
        {
            string clip = audioName ?? Base.AudioName;
            if (string.IsNullOrEmpty(clip)) return;

            StopAndDestroy();
            EnsureClipLoaded(clip);

            _currentClip = clip;
            _currentVolume = Base.Volume;

            _audioPlayer = AudioPlayer.Create("LoopSpeaker-" + gameObject.GetHashCode());
            Vector3 worldPos = transform.TransformPoint(Base.LocalPosition);
            Speaker speaker = _audioPlayer.AddSpeaker("Primary", worldPos, _currentVolume, Base.IsSpatial, Base.MinDistance, Base.MaxDistance);
            _audioPlayer.transform.parent = speaker.transform.parent = transform;
            _audioPlayer.transform.localPosition = speaker.transform.localPosition = Base.LocalPosition;
            _audioPlayer.AddClip(clip, _currentVolume, true, false);
            Log.Debug("LoopSpeaker: playing '" + clip + "' on " + gameObject.name);
        }

        public void Stop()
        {
            StopAndDestroy();
            Log.Debug("LoopSpeaker: stopped on " + gameObject.name);
        }

        public void ChangeClip(string audioName)
        {
            if (string.IsNullOrEmpty(audioName)) return;
            Play(audioName);
        }

        public void SetVolume(float volume)
        {
            _currentVolume = volume;
            if (_audioPlayer == null) return;
            if (_audioPlayer.SpeakersByName.TryGetValue("Primary", out Speaker speaker))
            {
                speaker.Volume = volume;
            }
        }
    }
}
