using System;
using System.Collections;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Schizophrenia.Severitys
{
    public class severities1
    {
        private const float MinimumAudioDistanceFeet = 5f;
        private const float MaximumAudioDistanceFeet = 15f;
        private const float FeetToMeters = 0.3048f;
        private const float MinimumSoundDelaySeconds = 20f;
        private const float MaximumSoundDelaySeconds = 60f;
        private const float MinimumPainSoundDelaySeconds = 10f;
        private const float MaximumPainSoundDelaySeconds = 20f;
        private const float PainPhaseDurationSeconds = 180f;
        private const float MissingClipRescanIntervalSeconds = 30f;
        private const float MinimumFordPainPitch = 0.65f;
        private const float MaximumFordPainPitch = 0.8f;
        private const string FordNpcGroup = "Ford";
        private const string FordBigPainClipPrefix = "brett_pain_big";

        private static readonly string[] NpcSoundKeywords =
        {
            "npc",
            "voice",
            "dialog",
            "speech",
            "talk",
            "ford",
            "nullbody",
            "null_body",
            "null body",
            "saber",
            "bot",
            "agent",
            "human",
            "vocal",
            "vox",
            "grunt",
            "laugh",
            "breath",
            "pain"
        };

        private static readonly string[] PainSoundKeywords =
        {
            "pain",
            "hurt",
            "damag",
            "injur",
            "scream",
            "screech",
            "grunt",
            "agony",
            "wince",
            "groan",
            "yell"
        };

        private readonly Dictionary<string, List<AudioClip>> npcVoiceClips =
            new Dictionary<string, List<AudioClip>>();

        private readonly Dictionary<string, List<AudioClip>> npcPainClips =
            new Dictionary<string, List<AudioClip>>();

        private bool running;
        private bool reportedMissingSources;
        private bool reportedMissingPainSounds;
        private int runVersion;
        private float painPhaseStartTime;
        private float nextMissingClipScanTime;

        public void Start(float severityDurationSeconds)
        {
            if (running)
                return;

            running = true;
            reportedMissingSources = false;
            reportedMissingPainSounds = false;
            int thisRunVersion = ++runVersion;

            painPhaseStartTime = Time.time + Mathf.Max(
                0f,
                severityDurationSeconds - PainPhaseDurationSeconds
            );

            RefreshNpcVoiceClips();
            MelonCoroutines.Start(RandomNpcSounds(thisRunVersion));
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            runVersion++;
            SchizophreniaAudio.FadeOutCategory("Severity1NPC");
        }

        public void PlayRandomNpcSoundNow()
        {
            if (npcVoiceClips.Count == 0)
                RefreshNpcVoiceClips();

            AudioClip clip = SelectRandomNpcVoiceClip();
            if (clip == null)
            {
                return;
            }

            PlayNpcClip(clip, false);
        }

        public void PlayFordBigPainNow()
        {
            if (!HasFinalPainClips())
                RefreshNpcVoiceClips();

            AudioClip clip = SelectFinalPainClip();
            if (clip == null)
            {
                return;
            }

            PlayNpcClip(clip, true);
        }

        public void AdjustSeverityDuration(float adjustmentSeconds)
        {
            if (!running || adjustmentSeconds == 0f)
                return;

            painPhaseStartTime += adjustmentSeconds;
        }

        private IEnumerator RandomNpcSounds(int thisRunVersion)
        {
            while (IsCurrentRun(thisRunVersion))
            {
                bool painPhase = IsInPainPhase();
                float delay = painPhase
                    ? Random.Range(MinimumPainSoundDelaySeconds, MaximumPainSoundDelaySeconds)
                    : Random.Range(MinimumSoundDelaySeconds, MaximumSoundDelaySeconds);

                float remaining = delay;
                bool phaseChangedDuringWait = false;

                while (remaining > 0f && IsCurrentRun(thisRunVersion))
                {
                    float waitDuration = Mathf.Min(1f, remaining);
                    yield return new WaitForSeconds(waitDuration);
                    remaining -= waitDuration;

                    if (painPhase != IsInPainPhase())
                    {
                        phaseChangedDuringWait = true;
                        break;
                    }
                }

                if (!IsCurrentRun(thisRunVersion))
                    yield break;

                if (phaseChangedDuringWait)
                {
                    continue;
                }

                if (Time.time >= nextMissingClipScanTime &&
                    (npcVoiceClips.Count == 0 || (painPhase && !HasFinalPainClips())))
                {
                    RefreshNpcVoiceClips();
                }

                AudioClip clip = painPhase
                    ? SelectFinalPainClip()
                    : SelectRandomNpcVoiceClip();

                if (clip == null)
                {
                    if (painPhase && !reportedMissingPainSounds)
                    {
                        reportedMissingPainSounds = true;
                    }

                    continue;
                }

                if (painPhase)
                {
                }

                PlayNpcClip(clip, painPhase);
            }
        }
        // oh shittings
        private void PlayNpcClip(AudioClip clip, bool painPhase)
        {
            Camera playerCamera = Camera.main;
            if (clip == null || playerCamera == null)
                return;

            Vector3 soundPosition = RandomPositionAround(playerCamera);
            float volume = audioplaceratio.GetPlaybackVolume();
            audioplaceratio.IgnoreMixWhileClipPlays(clip);
            SchizophreniaAudio.PlayAtPoint(
                clip,
                soundPosition,
                BoneLib.Audio.NPCVocals,
                volume,
                painPhase
                    ? Random.Range(MinimumFordPainPitch, MaximumFordPainPitch)
                    : Random.Range(0.9f, 1.1f),
                1f,
                MaximumAudioDistanceFeet * 2f * FeetToMeters,
                "Severity1NPC"
            );
        }

        private bool IsCurrentRun(int thisRunVersion)
        {
            return running && thisRunVersion == runVersion;
        }

        private bool IsInPainPhase()
        {
            return Time.time >= painPhaseStartTime;
        }

        private void RefreshNpcVoiceClips()
        {
            npcVoiceClips.Clear();
            npcPainClips.Clear();

            int loadedClipCount = 0;
            int audioSourceCount = 0;

            Il2CppArrayBase<AudioClip> loadedClips =
                Resources.FindObjectsOfTypeAll<AudioClip>();

            if (loadedClips != null)
            {
                loadedClipCount = loadedClips.Length;
                for (int i = 0; i < loadedClips.Length; i++)
                {
                    AudioClip clip = loadedClips[i];
                    if (clip == null)
                        continue;

                    if (IsNpcSound("", clip.name))
                        AddClipToNpcPool(npcVoiceClips, clip.name, clip);

                    if (IsNpcPainSound("", clip.name))
                        AddClipToNpcPool(npcPainClips, clip.name, clip);
                }
            }

            Il2CppArrayBase<AudioSource> audioSources =
                Resources.FindObjectsOfTypeAll<AudioSource>();

            if (audioSources != null)
            {
                audioSourceCount = audioSources.Length;
                for (int i = 0; i < audioSources.Length; i++)
                {
                    AudioSource audioSource = audioSources[i];
                    if (audioSource == null || audioSource.clip == null)
                        continue;

                    string sourceName = GetAudioSourceIdentity(audioSource);

                    if (IsNpcSound(sourceName, audioSource.clip.name))
                    {
                        AddClipToNpcPool(
                            npcVoiceClips,
                            sourceName + "|" + audioSource.clip.name,
                            audioSource.clip
                        );
                    }

                    if (IsNpcPainSound(sourceName, audioSource.clip.name))
                    {
                        AddClipToNpcPool(
                            npcPainClips,
                            sourceName + "|" + audioSource.clip.name,
                            audioSource.clip
                        );
                    }
                }
            }

            nextMissingClipScanTime = Time.time + MissingClipRescanIntervalSeconds;

            int voiceClipCount = CountClips(npcVoiceClips);
            int painClipCount = CountClips(npcPainClips);
            if (voiceClipCount > 0)
            {

                LogNpcGroups("General", npcVoiceClips);
                LogNpcGroups("Pain", npcPainClips);

                reportedMissingSources = false;
                if (HasFinalPainClips())
                    reportedMissingPainSounds = false;
            }
            else if (!reportedMissingSources)
            {
                reportedMissingSources = true;
            }
        }

        private void AddClipToNpcPool(
            Dictionary<string, List<AudioClip>> pools,
            string identity,
            AudioClip clip
        )
        {
            if (clip == null)
                return;

            string npcGroup = GetNpcGroup(identity);
            List<AudioClip> clips;
            if (!pools.TryGetValue(npcGroup, out clips))
            {
                clips = new List<AudioClip>();
                pools.Add(npcGroup, clips);
            }

            if (!clips.Contains(clip))
                clips.Add(clip);
        }

        private string GetNpcGroup(string identity)
        {
            string name = (identity ?? string.Empty).ToLowerInvariant();

            if (name.Contains("nullbody") || name.Contains("null_body") || name.Contains("null body"))
                return "NullBody";
            if (name.Contains("saberlake") || name.Contains("saber lake"))
                return "Saberlake Bot";
            if (name.Contains("ford") || name.Contains("brett"))
                return "Ford";
            if (name.Contains("crablet"))
                return "Crablet";
            if (name.Contains("peasant"))
                return "Peasant";
            if (name.Contains("turret"))
                return "Turret";
            if (name.Contains("agent"))
                return "Agent";
            if (name.Contains("bot"))
                return "Bot";
            if (name.Contains("human"))
                return "Human";

            return "Other NPC";
        }

        private string GetAudioSourceIdentity(AudioSource audioSource)
        {
            if (audioSource == null)
                return string.Empty;

            string identity = audioSource.name ?? string.Empty;
            Transform current = audioSource.transform;
            int ancestorDepth = 0;

            while (current != null && ancestorDepth < 8)
            {
                if (current.gameObject != null)
                    identity = current.gameObject.name + "|" + identity;

                current = current.parent;
                ancestorDepth++;
            }

            return identity;
        }

        private bool IsNpcSound(string sourceName, string clipName)
        {
            string normalizedName = NormalizeNames(sourceName, clipName);

            for (int i = 0; i < NpcSoundKeywords.Length; i++)
            {
                if (normalizedName.Contains(NpcSoundKeywords[i]))
                    return true;
            }

            return false;
        }

        private bool IsNpcPainSound(string sourceName, string clipName)
        {
            string normalizedName = NormalizeNames(sourceName, clipName);
            if (!IsNpcSound(sourceName, clipName))
                return false;

            for (int i = 0; i < PainSoundKeywords.Length; i++)
            {
                if (normalizedName.Contains(PainSoundKeywords[i]))
                    return true;
            }

            return false;
        }

        private string NormalizeNames(string sourceName, string clipName)
        {
            return $"{sourceName ?? string.Empty}|{clipName ?? string.Empty}".ToLowerInvariant();
        }

        private AudioClip SelectRandomNpcVoiceClip()
        {
            return SelectRandomClipFromGroup(npcVoiceClips);
        }

        private bool HasFinalPainClips()
        {
            return GetFordIntensePainClips().Count > 0;
        }

        private AudioClip SelectFinalPainClip()
        {
            List<AudioClip> clips = GetFordIntensePainClips();
            if (clips.Count == 0)
                return null;

            return clips[Random.Range(0, clips.Count)];
        }

        private List<AudioClip> GetFordIntensePainClips()
        {
            List<AudioClip> fordPainClips;
            List<AudioClip> intensePainClips = new List<AudioClip>();
            if (!npcPainClips.TryGetValue(FordNpcGroup, out fordPainClips))
                return intensePainClips;

            for (int i = 0; i < fordPainClips.Count; i++)
            {
                AudioClip clip = fordPainClips[i];
                if (clip != null && !string.IsNullOrEmpty(clip.name) && clip.name.IndexOf(
                        FordBigPainClipPrefix,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    intensePainClips.Add(clip);
                }
            }

            return intensePainClips;
        }

        private AudioClip SelectRandomClipFromGroup(
            Dictionary<string, List<AudioClip>> pools
        )
        {
            if (pools.Count == 0)
                return null;

            List<string> groups = new List<string>(pools.Keys);
            string npcGroup = groups[Random.Range(0, groups.Count)];
            List<AudioClip> clips = pools[npcGroup];
            if (clips.Count == 0)
                return null;

            return clips[Random.Range(0, clips.Count)];
        }

        private int CountClips(Dictionary<string, List<AudioClip>> pools)
        {
            int count = 0;
            foreach (KeyValuePair<string, List<AudioClip>> group in pools)
                count += group.Value.Count;

            return count;
        }

        private void LogNpcGroups(
            string category,
            Dictionary<string, List<AudioClip>> pools
        )
        {
            List<string> groups = new List<string>(pools.Keys);
            groups.Sort(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < groups.Count; i++)
            {
                List<AudioClip> clips = pools[groups[i]];
            }
        }

        private Vector3 RandomPositionAround(Camera playerCamera)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(
                MinimumAudioDistanceFeet,
                MaximumAudioDistanceFeet
            ) * FeetToMeters;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                Random.Range(-0.12f, 0.12f),
                Mathf.Sin(angle)
            ).normalized;

            return playerCamera.transform.position + direction * distance;
        }
    }
}
