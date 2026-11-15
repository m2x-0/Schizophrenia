using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.Audio;

namespace Schizophrenia
{
    internal static class SchizophreniaAudio
    {
        private const float DefaultFadeDurationSeconds = 0.75f;
        private const float MinimumDistanceMeters = 1f;
        private static readonly List<AudioSource> activeSources = new List<AudioSource>();

        private sealed class FadeTarget
        {
            public AudioSource Source;
            public float StartVolume;
        }

        public static void Play2D(
            AudioClip clip,
            AudioMixerGroup mixerGroup,
            float volume,
            float pitch,
            string category = "General"
        )
        {
            Play(
                clip,
                Vector3.zero,
                mixerGroup,
                volume,
                pitch,
                0f,
                MinimumDistanceMeters + 0.1f,
                category
            );
        }

        public static void PlayAtPoint(
            AudioClip clip,
            Vector3 position,
            AudioMixerGroup mixerGroup,
            float volume,
            float pitch,
            float spatialBlend,
            float maximumDistanceMeters,
            string category = "General"
        )
        {
            Play(
                clip,
                position,
                mixerGroup,
                volume,
                pitch,
                spatialBlend,
                maximumDistanceMeters,
                category
            );
        }

        private static void Play(
            AudioClip clip,
            Vector3 position,
            AudioMixerGroup mixerGroup,
            float volume,
            float pitch,
            float spatialBlend,
            float maximumDistanceMeters,
            string category
        )
        {
            if (clip == null)
                return;

            string categoryName = string.IsNullOrEmpty(category) ? "General" : category;
            GameObject audioObject = new GameObject(
                "Schizophrenia Audio Playback [" + categoryName + "]"
            );
            audioObject.hideFlags = HideFlags.HideInHierarchy;
            audioObject.transform.position = position;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.ignoreListenerPause = true;
            source.clip = clip;
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = MinimumDistanceMeters;
            source.maxDistance = Mathf.Max(
                MinimumDistanceMeters + 0.1f,
                maximumDistanceMeters
            );

            activeSources.Add(source);
            source.Play();
        }

        public static void Update()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeSources[i];
                if (source == null)
                {
                    activeSources.RemoveAt(i);
                }
                else if (!AudioListener.pause && !source.isPlaying)
                {
                    DestroySource(source);
                }
            }
        }

        public static void FadeOutCurrent(float fadeDurationSeconds = DefaultFadeDurationSeconds)
        {
            FadeOutCategory(null, fadeDurationSeconds);
        }

        public static void FadeOutCategory(
            string category,
            float fadeDurationSeconds = DefaultFadeDurationSeconds
        )
        {
            List<FadeTarget> targets = new List<FadeTarget>();
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeSources[i];
                if (source == null)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }

                if (category != null && !string.Equals(
                        source.gameObject.name,
                        "Schizophrenia Audio Playback [" + category + "]",
                        System.StringComparison.Ordinal
                    ))
                {
                    continue;
                }

                if (source.isPlaying)
                {
                    targets.Add(
                        new FadeTarget
                        {
                            Source = source,
                            StartVolume = source.volume
                        }
                    );
                }
                else
                {
                    DestroySource(source);
                }
            }

            if (targets.Count == 0)
                return;

            MelonCoroutines.Start(FadeOutTargets(targets, fadeDurationSeconds));
        }

        private static IEnumerator FadeOutTargets(
            List<FadeTarget> targets,
            float fadeDurationSeconds
        )
        {
            float duration = Mathf.Max(0f, fadeDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float remainingVolume = 1f - Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < targets.Count; i++)
                {
                    AudioSource source = targets[i].Source;
                    if (source != null)
                        source.volume = targets[i].StartVolume * remainingVolume;
                }
                yield return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                AudioSource source = targets[i].Source;
                if (source != null)
                    DestroySource(source);
            }
        }

        private static void DestroySource(AudioSource source)
        {
            if (source == null)
                return;

            activeSources.Remove(source);
            GameObject audioObject = source.gameObject;
            if (audioObject != null)
            {
                source.Stop();
                UnityEngine.Object.Destroy(audioObject);
            }
        }
    }
}
