using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace Schizophrenia
{
    internal static class audioplaceratio
    {
        private const int SampleCount = 1024;
        private const float SampleIntervalSeconds = 0.25f;
        private const float SmoothingTimeSeconds = 0.8f;
        private const float QuietMixDecibels = -50f;
        private const float LoudMixDecibels = -20f;
        private const float QuietPlaybackVolume = 0.7f;
        private const float LoudPlaybackVolume = 1f;
        private const float ClipTailSeconds = 0.25f;
        private const float FallbackPlaybackVolume = 0.85f;

        private static readonly Il2CppStructArray<float> outputSamples =
            new Il2CppStructArray<float>(SampleCount);

        private static float nextSampleTime;
        private static float ignoreSamplesUntil;
        private static float lastSampleTime;
        private static float smoothedRms;
        private static bool hasSample;
        private static bool meterUnavailable;

        public static void Update()
        {
            float now = Time.unscaledTime;
            if (meterUnavailable || now < nextSampleTime || now < ignoreSamplesUntil)
                return;

            nextSampleTime = now + SampleIntervalSeconds;

            try
            {
                AudioListener.GetOutputData(outputSamples, 0);

                double sumSquares = 0d;
                for (int i = 0; i < outputSamples.Length; i++)
                {
                    float sample = outputSamples[i];
                    sumSquares += sample * sample;
                }

                float currentRms = Mathf.Sqrt((float)(sumSquares / outputSamples.Length));
                if (float.IsNaN(currentRms) || float.IsInfinity(currentRms))
                    return;

                if (!hasSample)
                {
                    smoothedRms = currentRms;
                    hasSample = true;
                }
                else
                {
                    float elapsed = Mathf.Max(0f, now - lastSampleTime);
                    float smoothing = 1f - Mathf.Exp(-elapsed / SmoothingTimeSeconds);
                    smoothedRms = Mathf.Lerp(smoothedRms, currentRms, smoothing);
                }

                lastSampleTime = now;
            }
            catch (System.Exception)
            {
                meterUnavailable = true;
            }
        }

        public static float GetPlaybackVolume()
        {
            if (!hasSample || meterUnavailable)
                return FallbackPlaybackVolume;

            float decibels = 20f * Mathf.Log10(Mathf.Max(smoothedRms, 0.00001f));
            float level = Mathf.Clamp01(
                (decibels - QuietMixDecibels) /
                (LoudMixDecibels - QuietMixDecibels)
            );

            return Mathf.Lerp(QuietPlaybackVolume, LoudPlaybackVolume, level);
        }

        public static void IgnoreMixWhileClipPlays(AudioClip clip)
        {
            if (clip == null)
                return;

            ignoreSamplesUntil = Mathf.Max(
                ignoreSamplesUntil,
                Time.unscaledTime + clip.length + ClipTailSeconds
            );
        }
    }
}
