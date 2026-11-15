using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.PuppetMasta;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Schizophrenia.Severitys
{
    public class severities3
    {
        private const float TransitionDurationSeconds = 5f;
        private const float Severity3Contrast = 50f;
        private const float Severity3Saturation = -25f;
        private const float RadialMenuWordIntervalSeconds = 0.5f;
        private const float MinimumEntitySpawnDelaySeconds = 10f;
        private const float MaximumEntitySpawnDelaySeconds = 15f;
        private const float MinimumNpcWaveDelaySeconds = 30f;
        private const float MaximumNpcWaveDelaySeconds = 60f;
        private const float MinimumFlashDelaySeconds = 5f;
        private const float MaximumFlashDelaySeconds = 15f;
        private const float FlashDurationSeconds = 0.02f;
        private const float ScreenImageDistanceMeters = 0.35f;
        private const float EntityLifetimeSeconds = 5f;
        private const float MinimumEntityDistanceFeet = 5f;
        private const float MaximumEntityDistanceFeet = 10f;
        private const float EntityWallClearanceMeters = 0.3f;
        private const float EntityMinimumSeparationMeters = 0.6f;
        private const int EntityPositionAttempts = 24;
        private const float FakeFightMinimumDurationSeconds = 10f;
        private const float FakeFightMaximumDurationSeconds = 15f;
        private const float FakeFightMinimumDistanceFeet = 5f;
        private const float FakeFightMaximumDistanceFeet = 10f;
        private const float FeetToMeters = 0.3048f;
        private const string VolumeObjectName = "Schizophrenia Severity 3 Color Volume";
        private const string HeadAudioFolderName = "Audio5";
        private const string DisappearAudioFolderName = "Audio3";
        private const string EntityImageFolderName = "entities1";
        private const string FlashImageFolderName = "flash";
        private static readonly Color Severity3RedTint = new Color(1f, 0.45f, 0.45f, 1f);
        private static readonly string[] RadialMenuWords = { "NO", "ESCAPE", "FOR", "YOU" };

        private readonly severities2 severities2;
        private readonly List<AudioClip> headAudioClips = new List<AudioClip>();
        private readonly List<AudioClip> disappearAudioClips = new List<AudioClip>();
        private readonly List<string> entityImagePaths = new List<string>();
        private readonly List<Severity3ScreenImage> flashImages =
            new List<Severity3ScreenImage>();
        private readonly List<Severity3EntityImage> activeEntityImages =
            new List<Severity3EntityImage>();
        private readonly List<GameObject> activeFakeFightNpcs = new List<GameObject>();
        private readonly HashSet<int> fakeFightExistingPooleeIds = new HashSet<int>();
        private readonly HashSet<int> fakeFightTrackedPooleeIds = new HashSet<int>();
        private readonly HashSet<int> fakeFightTrackedObjectIds = new HashSet<int>();
        private readonly HashSet<int> fakeFightClaimedSpawnSlots = new HashSet<int>();
        private readonly List<Vector3> fakeFightSpawnPositions = new List<Vector3>();
        private readonly HashSet<string> loadedAudioFilePaths =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadedDisappearAudioFilePaths =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private Volume severity3Volume;
        private GameObject activeFlashOverlay;
        private bool running;
        private bool audioLoading;
        private bool disappearAudioLoading;
        private bool headAudioLoopActive;
        private bool visualsEnabled = true;
        private bool radialMenuWordsEnabled = true;
        private bool headAudioEnabled = true;
        private bool entitySpawnsEnabled = true;
        private bool disappearSoundsEnabled = true;
        private bool fakeFightRunning;
        private bool npcWavesEnabled = true;
        private bool flashImagesEnabled = true;
        private bool screenImageAssetsLoaded;
        private bool loggedMissingEntityAudio;
        private bool loggedMissingEntityImages;
        private int screenImageLoopVersion;
        private int screenImageLoadVersion;
        private int transitionVersion;
        private int headAudioLoopVersion;
        private int entitySpawnLoopVersion;
        private int fakeFightVersion;
        private int npcWaveLoopVersion;
        private int flashImageLoopVersion;
        private int previousHeadAudioClipIndex = -1;
        private int nextRadialMenuWordIndex;
        private int previousFlashImageIndex = -1;
        private float nextRadialMenuWordTime;

        public event Action<AudioClip> HeadAudioClipLoaded;
        public event Action<AudioClip> DisappearAudioClipLoaded;
        public event Action<string, string> EntityImageLoaded;

        private sealed class Severity3EntityImage
        {
            public GameObject GameObject;
            public Texture2D Texture;
            public Sprite Sprite;
        }

        private sealed class Severity3ScreenImage
        {
            public string Path;
            public Texture2D Texture;
            public Sprite Sprite;
        }

        public severities3(severities2 severities2)
        {
            this.severities2 = severities2;
        }

        public void Start()
        {
            if (running)
                return;

            running = true;
            nextRadialMenuWordIndex = 0;
            nextRadialMenuWordTime = 0f;
            if (visualsEnabled)
                BeginVisuals();

            StartHeadAudioLoop();
            LoadAudioFiles();
            LoadDisappearAudioFiles();
            StartEntitySpawnLoop();
            StartNpcWaveLoop();
            int thisScreenLoop = ++screenImageLoopVersion;
            StartScreenImageAssetLoading();
            if (flashImagesEnabled)
                StartFlashImageLoop();
        }

        public void Stop()
        {
            StopFakeFightNow();

            if (!running)
                return;

            running = false;
            headAudioLoopVersion++;
            entitySpawnLoopVersion++;
            npcWaveLoopVersion++;
            screenImageLoopVersion++;
            screenImageLoadVersion++;
            flashImageLoopVersion++;
            headAudioLoopActive = false;
            ClearScreenOverlays();
            ClearScreenImageAssets();
            for (int i = activeEntityImages.Count - 1; i >= 0; i--)
                DestroyEntityImage(activeEntityImages[i], false);
            SchizophreniaAudio.FadeOutCurrent();
            severities2?.RestoreRadialMenuText();

            if (severity3Volume != null)
            {
                int thisTransition = ++transitionVersion;
                MelonCoroutines.Start(FadeVolumeWeight(0f, thisTransition));
            }

        }

        public void Update()
        {
            if (!running || !radialMenuWordsEnabled ||
                Time.unscaledTime < nextRadialMenuWordTime)
                return;

            string word = RadialMenuWords[nextRadialMenuWordIndex];
            severities2?.DisplaySeverity3MenuWord(word);
            nextRadialMenuWordIndex = (nextRadialMenuWordIndex + 1) % RadialMenuWords.Length;
            nextRadialMenuWordTime = Time.unscaledTime + RadialMenuWordIntervalSeconds;
        }

        public void SetVisualsEnabled(bool enabled)
        {
            visualsEnabled = enabled;
            if (!running)
                return;

            if (enabled)
                BeginVisuals();
            else
                EndVisuals();
        }

        public void SetRadialMenuWordsEnabled(bool enabled)
        {
            radialMenuWordsEnabled = enabled;
            if (!running)
                return;

            if (enabled)
            {
                nextRadialMenuWordIndex = 0;
                nextRadialMenuWordTime = 0f;
                Update();
            }
            else
                severities2?.RestoreRadialMenuText();
        }

        public void SetHeadAudioEnabled(bool enabled)
        {
            headAudioEnabled = enabled;
            headAudioLoopVersion++;
            headAudioLoopActive = false;
            if (enabled && running)
                StartHeadAudioLoop();
            else if (!enabled)
                SchizophreniaAudio.FadeOutCategory("Severity3Head");
        }

        public void SetEntitySpawnsEnabled(bool enabled)
        {
            entitySpawnsEnabled = enabled;
            if (enabled && running)
                StartEntitySpawnLoop();
            else if (!enabled)
            {
                entitySpawnLoopVersion++;
                for (int i = activeEntityImages.Count - 1; i >= 0; i--)
                    DestroyEntityImage(activeEntityImages[i], false);
                SchizophreniaAudio.FadeOutCategory("Severity3Entity");
            }
        }

        public void SetDisappearSoundsEnabled(bool enabled)
        {
            disappearSoundsEnabled = enabled;
        }

        public void SetFlashImagesEnabled(bool enabled)
        {
            flashImagesEnabled = enabled;
            flashImageLoopVersion++;

            if (enabled && running)
                StartFlashImageLoop();
            else if (!enabled)
                ClearFlashOverlay();

        }

        private void StartFlashImageLoop()
        {
            if (!running || !flashImagesEnabled)
                return;

            MelonCoroutines.Start(FlashImageLoop(++flashImageLoopVersion));
        }

        public void SetNpcWavesEnabled(bool enabled)
        {
            npcWavesEnabled = enabled;
            npcWaveLoopVersion++;

            if (enabled && running)
            {
                StartNpcWaveLoop();
                return;
            }

            if (!enabled)
            {
                StopFakeFightNow();
            }
        }

        private void StartNpcWaveLoop()
        {
            if (!running || !npcWavesEnabled)
                return;

            int thisLoop = ++npcWaveLoopVersion;
            MelonCoroutines.Start(SpawnNpcWaves(thisLoop));
        }

        private IEnumerator SpawnNpcWaves(int thisLoop)
        {
            while (running && npcWavesEnabled && thisLoop == npcWaveLoopVersion)
            {
                yield return new WaitForSecondsRealtime(
                    UnityEngine.Random.Range(
                        MinimumNpcWaveDelaySeconds,
                        MaximumNpcWaveDelaySeconds
                    )
                );

                if (!running || !npcWavesEnabled || thisLoop != npcWaveLoopVersion)
                    yield break;

                if (fakeFightRunning)
                {
                    continue;
                }

                StartFakeFightWave(false);
            }
        }

        private void BeginVisuals()
        {
            if (!EnsureVolume())
            {
                return;
            }

            int thisTransition = ++transitionVersion;
            MelonCoroutines.Start(FadeVolumeWeight(1f, thisTransition));
        }

        private void EndVisuals()
        {
            if (severity3Volume == null)
                return;

            int thisTransition = ++transitionVersion;
            MelonCoroutines.Start(FadeVolumeWeight(0f, thisTransition));
        }

        public void PlaySpecificHeadSound(AudioClip clip)
        {
            if (clip == null)
                return;

            SchizophreniaAudio.Play2D(
                clip,
                BoneLib.Audio.InHead,
                audioplaceratio.GetPlaybackVolume(),
                1f,
                "Severity3Head"
            );
            audioplaceratio.IgnoreMixWhileClipPlays(clip);
        }

        public void PlayRandomHeadSoundNow()
        {
            if (headAudioClips.Count == 0)
            {
                LoadAudioFiles();
                return;
            }

            PlaySpecificHeadSound(
                headAudioClips[UnityEngine.Random.Range(0, headAudioClips.Count)]
            );
        }

        public void PlayRandomDisappearSoundNow()
        {
            if (disappearAudioClips.Count == 0)
            {
                LoadDisappearAudioFiles();
                return;
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
                return;

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 position = playerCamera.transform.position + new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            ) * (7f * FeetToMeters);
            AudioClip clip = disappearAudioClips[
                UnityEngine.Random.Range(0, disappearAudioClips.Count)
            ];
            PlayDisappearAudioAt(clip, position, true);
        }

        public void PlaySpecificDisappearSoundNow(AudioClip clip)
        {
            if (clip == null)
                return;

            Camera playerCamera = Camera.main;
            Vector3 position = playerCamera != null
                ? playerCamera.transform.position + playerCamera.transform.forward * (7f * FeetToMeters)
                : Vector3.zero;
            PlayDisappearAudioAt(clip, position, true);
        }

        public void FlashMenuPhraseNow()
        {
            if (!running)
                return;

            nextRadialMenuWordIndex = 0;
            nextRadialMenuWordTime = 0f;
            if (radialMenuWordsEnabled)
                Update();
        }

        public void LoadEntityEventAudioFiles()
        {
            LoadDisappearAudioFiles();
        }

        public void LoadEntityImageOptions()
        {
            RefreshEntityImagePaths();
        }

        public void SpawnEntityWaveNow()
        {
            List<Severity3EntityImage> wave = SpawnEntityWave();
            if (wave.Count == 0)
                return;

            MelonCoroutines.Start(DespawnEntityWave(wave));
        }

        public void SpawnSpecificEntityImageNow(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return;
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
                return;

            Severity3EntityImage image = SpawnEntityImage(
                playerCamera,
                imagePath,
                new List<Vector3>()
            );
            if (image == null)
                return;

            activeEntityImages.Add(image);
            MelonCoroutines.Start(
                DespawnEntityWave(new List<Severity3EntityImage> { image })
            );
        }

        public void StartFakeFightNow()
        {
            StartFakeFightWave(true);
        }

        private void StartFakeFightWave(bool developerInitiated)
        {
            StopFakeFightNow();

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            int thisFight = ++fakeFightVersion;
            float duration = UnityEngine.Random.Range(
                FakeFightMinimumDurationSeconds,
                FakeFightMaximumDurationSeconds
            );
            fakeFightRunning = true;
            if (!fightprotector.SetActive(true))
            {
                fakeFightRunning = false;
                return;
            }

            ResetFakeFightTracking();
            Il2CppArrayBase<Poolee> existingPoolees =
                UnityEngine.Object.FindObjectsOfType<Poolee>();
            if (existingPoolees != null)
            {
                foreach (Poolee poolee in existingPoolees)
                {
                    if (poolee != null)
                        fakeFightExistingPooleeIds.Add(poolee.GetInstanceID());
                }
            }

            string[] waveBarcodes =
            {
                CommonBarcodes.NPCs.Nullbody,
                CommonBarcodes.NPCs.Crablet,
                CommonBarcodes.NPCs.EarlyExitZombie
            };
            for (int i = waveBarcodes.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string swap = waveBarcodes[i];
                waveBarcodes[i] = waveBarcodes[j];
                waveBarcodes[j] = swap;
            }

            List<Vector3> chosenPositions = new List<Vector3>();
            for (int i = 0; i < waveBarcodes.Length; i++)
            {
                Vector3 spawnPosition;
                float distanceFeet;
                if (!TryChooseFakeFightPosition(
                        playerCamera,
                        chosenPositions,
                        out spawnPosition,
                        out distanceFeet
                    ))
                {
                    break;
                }

                chosenPositions.Add(spawnPosition);
                fakeFightSpawnPositions.Add(spawnPosition);
                string barcode = waveBarcodes[i];
                Quaternion rotation = Quaternion.LookRotation(
                    playerCamera.transform.position - spawnPosition,
                    Vector3.up
                );
                SpawnFakeFightNpc(barcode, spawnPosition, rotation, thisFight);
            }

            MelonCoroutines.Start(TrackFakeFightNpcs(thisFight));
            MelonCoroutines.Start(FakeFightLifetime(thisFight, duration));
        }

        private static string GetNpcDisplayName(string barcode)
        {
            if (barcode == CommonBarcodes.NPCs.Nullbody)
                return "Null Body";
            if (barcode == CommonBarcodes.NPCs.Crablet)
                return "Crablet";
            if (barcode == CommonBarcodes.NPCs.EarlyExitZombie)
                return "Corrupted Ford (Early Exit Zombie)";

            return barcode;
        }

        public void StopFakeFightNow()
        {
            bool hadEncounter = fakeFightRunning || activeFakeFightNpcs.Count > 0;
            fakeFightRunning = false;
            fakeFightVersion++;

            int clearedCount = activeFakeFightNpcs.Count;
            for (int i = activeFakeFightNpcs.Count - 1; i >= 0; i--)
                DespawnFakeFightNpc(activeFakeFightNpcs[i]);
            activeFakeFightNpcs.Clear();
            fightprotector.SetActive(false);

            if (hadEncounter)
            {
            }

            ResetFakeFightTracking();
        }

        private void ResetFakeFightTracking()
        {
            fakeFightExistingPooleeIds.Clear();
            fakeFightTrackedPooleeIds.Clear();
            fakeFightTrackedObjectIds.Clear();
            fakeFightClaimedSpawnSlots.Clear();
            fakeFightSpawnPositions.Clear();
        }

        private IEnumerator TrackFakeFightNpcs(int thisFight)
        {
            while (fakeFightRunning && thisFight == fakeFightVersion)
            {
                TrackNewFakeFightNpcs(thisFight);
                if (activeFakeFightNpcs.Count >= fakeFightSpawnPositions.Count)
                    yield break;

                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private void TrackNewFakeFightNpcs(int thisFight)
        {
            Il2CppArrayBase<Poolee> poolees = UnityEngine.Object.FindObjectsOfType<Poolee>();
            if (poolees == null)
                return;

            foreach (Poolee poolee in poolees)
            {
                if (poolee == null)
                    continue;

                int pooleeId = poolee.GetInstanceID();
                if (fakeFightExistingPooleeIds.Contains(pooleeId) ||
                    fakeFightTrackedPooleeIds.Contains(pooleeId))
                    continue;

                GameObject npcObject = poolee.gameObject;
                if (npcObject == null || fakeFightTrackedObjectIds.Contains(npcObject.GetInstanceID()))
                    continue;

                int spawnSlot = FindNearestUnclaimedFakeFightSpawnSlot(
                    npcObject.transform.position
                );
                if (spawnSlot < 0)
                    continue;

                fakeFightTrackedPooleeIds.Add(pooleeId);
                RegisterFakeFightNpc(npcObject, thisFight);
            }
        }

        private int FindNearestUnclaimedFakeFightSpawnSlot(Vector3 npcPosition)
        {
            const float MaximumMatchDistanceMeters = 2f;
            int bestSlot = -1;
            float bestDistance = MaximumMatchDistanceMeters;

            for (int i = 0; i < fakeFightSpawnPositions.Count; i++)
            {
                if (fakeFightClaimedSpawnSlots.Contains(i))
                    continue;

                float distance = Vector3.Distance(npcPosition, fakeFightSpawnPositions[i]);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    bestSlot = i;
                }
            }

            return bestSlot;
        }

        private void SpawnFakeFightNpc(
            string barcode,
            Vector3 position,
            Quaternion rotation,
            int thisFight
        )
        {
            try
            {
                HelperMethods.SpawnCrate(
                    barcode,
                    position,
                    rotation,
                    Vector3.one,
                    false,
                    new Action<GameObject>(spawned => RegisterFakeFightNpc(spawned, thisFight)),
                    new Action<GameObject>(_ => { })
                );
            }
            catch (System.Exception)
            {
            }
        }

        private void RegisterFakeFightNpc(GameObject spawned, int thisFight)
        {
            if (spawned == null)
            {
                return;
            }

            if (!fakeFightRunning || thisFight != fakeFightVersion)
            {
                DespawnFakeFightNpc(spawned);
                return;
            }

            int objectId = spawned.GetInstanceID();
            if (!fakeFightTrackedObjectIds.Add(objectId))
                return;

            AIBrain brain = spawned.GetComponentInChildren<AIBrain>(true);
            Poolee poolee = spawned.GetComponent<Poolee>();
            if (poolee == null)
                poolee = spawned.GetComponentInChildren<Poolee>(true);
            if (poolee != null)
                fakeFightTrackedPooleeIds.Add(poolee.GetInstanceID());

            int slot = FindNearestUnclaimedFakeFightSpawnSlot(spawned.transform.position);
            if (slot >= 0)
                fakeFightClaimedSpawnSlots.Add(slot);

            activeFakeFightNpcs.Add(spawned);
            MelonCoroutines.Start(EngageFakeFightNpc(spawned, thisFight));
        }

        private IEnumerator EngageFakeFightNpc(GameObject npcObject, int thisFight)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                yield return new WaitForSecondsRealtime(attempt == 0 ? 0.2f : 1f);
                if (npcObject == null || !fakeFightRunning || thisFight != fakeFightVersion)
                    yield break;

                AIBrain brain = npcObject.GetComponentInChildren<AIBrain>(true);
                if (brain == null || brain.behaviour == null)
                    continue;

                bool wasFallen = false;
                string failure = null;
                try
                {
                    TriggerRefProxy playerProxy = GetPlayerTargetProxy();
                    if (playerProxy != null)
                    {
                        brain.behaviour.SetAgro(playerProxy);
                        brain.behaviour.SetEngaged(playerProxy);
                    }

                    wasFallen = brain.behaviour.locoState ==
                        BehaviourBaseNav.LocoState.Fallen;
                    if (wasFallen)
                    {
                        brain.behaviour.SwitchLocoState(
                            BehaviourBaseNav.LocoState.GetUp,
                            0.5f,
                            true
                        );
                    }
                }
                catch (System.Exception exception)
                {
                    failure = exception.Message;
                }

                if (failure != null)
                {
                    yield break;
                }

                if (!wasFallen)
                {
                    yield break;
                }
            }

        }

        private TriggerRefProxy GetPlayerTargetProxy()
        {
            if (Player.Head != null)
            {
                TriggerRefProxy headProxy = Player.Head
                    .GetComponentInParent<TriggerRefProxy>();
                if (headProxy != null)
                    return headProxy;
            }

            RigManager playerRig = Player.RigManager;
            return playerRig != null
                ? playerRig.GetComponentInChildren<TriggerRefProxy>(true)
                : null;
        }

        private void DespawnFakeFightNpc(GameObject npcObject)
        {
            if (npcObject == null)
                return;

            Poolee poolee = npcObject.GetComponent<Poolee>();
            if (poolee == null)
                poolee = npcObject.GetComponentInChildren<Poolee>(true);

            if (poolee != null)
                poolee.Despawn();
            else
                UnityEngine.Object.Destroy(npcObject);
        }

        private IEnumerator FakeFightLifetime(int thisFight, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (fakeFightRunning && thisFight == fakeFightVersion)
                StopFakeFightNow();
        }

        private bool TryChooseFakeFightPosition(
            Camera playerCamera,
            List<Vector3> existingPositions,
            out Vector3 position,
            out float distanceFeet
        )
        {
            position = Vector3.zero;
            distanceFeet = 0f;

            Vector3 flatForward = Vector3.ProjectOnPlane(
                playerCamera.transform.forward,
                Vector3.up
            );
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            flatForward.Normalize();

            float minimumDistance = FakeFightMinimumDistanceFeet * FeetToMeters;
            float maximumDistance = FakeFightMaximumDistanceFeet * FeetToMeters;

            for (int attempt = 0; attempt < EntityPositionAttempts; attempt++)
            {
                Vector3 direction = Quaternion.AngleAxis(
                    UnityEngine.Random.Range(0f, 360f),
                    Vector3.up
                ) * flatForward;
                direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                float requestedDistance = UnityEngine.Random.Range(
                    minimumDistance,
                    maximumDistance
                );

                float chosenDistance = requestedDistance;
                float obstructionDistance;
                if (TryGetEntityObstructionDistance(
                        playerCamera,
                        direction,
                        requestedDistance,
                        out obstructionDistance
                    ))
                {
                    chosenDistance = obstructionDistance - EntityWallClearanceMeters;
                }

                if (chosenDistance < minimumDistance || chosenDistance > maximumDistance)
                    continue;

                Vector3 candidate = playerCamera.transform.position +
                    direction * chosenDistance;
                Vector3 groundRayOrigin = candidate + Vector3.up * 2.5f;
                RaycastHit groundHit;
                if (!Physics.Raycast(
                        groundRayOrigin,
                        Vector3.down,
                        out groundHit,
                        8f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore
                    ))
                {
                    continue;
                }

                if (Vector3.Angle(groundHit.normal, Vector3.up) > 35f)
                    continue;

                Vector3 candidatePosition = groundHit.point + Vector3.up * 0.05f;
                bool tooClose = false;
                for (int i = 0; i < existingPositions.Count; i++)
                {
                    if (Vector3.Distance(candidatePosition, existingPositions[i]) < 0.9f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose || Physics.CheckCapsule(
                        candidatePosition + Vector3.up * 0.32f,
                        candidatePosition + Vector3.up * 1.65f,
                        0.28f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore
                    ))
                {
                    continue;
                }

                position = candidatePosition;
                distanceFeet = chosenDistance / FeetToMeters;
                return true;
            }

            return false;
        }

        public void ClearEntityImagesNow()
        {
            int removedCount = activeEntityImages.Count;
            for (int i = activeEntityImages.Count - 1; i >= 0; i--)
                DestroyEntityImage(activeEntityImages[i], false);

            SchizophreniaAudio.FadeOutCategory("Severity3Entity");
        }

        public void LoadAudioFiles()
        {
            if (audioLoading)
            {
                return;
            }

            audioLoading = true;
            MelonCoroutines.Start(LoadHeadAudioFilesCoroutine());
        }

        private IEnumerator LoadHeadAudioFilesCoroutine()
        {
            string audioDirectory = Path.Combine(
                Schizophrenia.ModAssetPaths.Root,
                HeadAudioFolderName
            );

            string[] files;
            try
            {
                Directory.CreateDirectory(audioDirectory);
                files = Directory.GetFiles(audioDirectory);
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
            }
            catch (System.Exception)
            {
                audioLoading = false;
                yield break;
            }

            int loadedCount = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                if ((extension != ".wav" && extension != ".mp3") ||
                    loadedAudioFilePaths.Contains(files[i]))
                {
                    continue;
                }

                try
                {
                    AudioClip clip = AudioImportLib.API.LoadAudioClip(files[i], true);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileNameWithoutExtension(files[i]);
                        loadedAudioFilePaths.Add(files[i]);
                        headAudioClips.Add(clip);
                        HeadAudioClipLoaded?.Invoke(clip);
                        loadedCount++;
                    }
                    else
                    {
                    }
                }
                catch (System.Exception)
                {
                }

                yield return null;
            }

            audioLoading = false;
            StartHeadAudioLoop();
        }

        private void LoadDisappearAudioFiles()
        {
            if (disappearAudioLoading)
                return;

            disappearAudioLoading = true;
            MelonCoroutines.Start(LoadDisappearAudioFilesCoroutine());
        }

        private IEnumerator LoadDisappearAudioFilesCoroutine()
        {
            string audioDirectory = Path.Combine(
                Schizophrenia.ModAssetPaths.Root,
                DisappearAudioFolderName
            );

            string[] files;
            try
            {
                Directory.CreateDirectory(audioDirectory);
                files = Directory.GetFiles(audioDirectory);
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
            }
            catch (System.Exception)
            {
                disappearAudioLoading = false;
                yield break;
            }

            int loadedCount = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                if ((extension != ".wav" && extension != ".mp3") ||
                    loadedDisappearAudioFilePaths.Contains(files[i]))
                {
                    continue;
                }

                try
                {
                    AudioClip clip = AudioImportLib.API.LoadAudioClip(files[i], true);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileNameWithoutExtension(files[i]);
                        loadedDisappearAudioFilePaths.Add(files[i]);
                        disappearAudioClips.Add(clip);
                        DisappearAudioClipLoaded?.Invoke(clip);
                        loadedCount++;
                    }
                    else
                    {
                    }
                }
                catch (System.Exception)
                {
                }

                yield return null;
            }

            disappearAudioLoading = false;
        }

        private void StartScreenImageAssetLoading()
        {
            ClearScreenImageAssets();
            screenImageAssetsLoaded = false;
            int thisLoad = ++screenImageLoadVersion;
            MelonCoroutines.Start(LoadScreenImageAssets(thisLoad));
        }

        private IEnumerator LoadScreenImageAssets(int thisLoad)
        {
            string directory = Path.Combine(
                Schizophrenia.ModAssetPaths.Root,
                FlashImageFolderName
            );
            string[] files;
            try
            {
                Directory.CreateDirectory(directory);
                files = Directory.GetFiles(directory);
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
            }
            catch (System.Exception)
            {
                yield break;
            }

            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                if (!IsSupportedScreenImage(files[fileIndex]))
                {
                    if (string.Equals(
                            Path.GetExtension(files[fileIndex]),
                            ".webp",
                            System.StringComparison.OrdinalIgnoreCase
                        ))
                    {
                    }
                    continue;
                }

                if (thisLoad != screenImageLoadVersion || !running)
                    yield break;

                Severity3ScreenImage image = LoadScreenImageAsset(files[fileIndex]);
                if (image != null)
                    flashImages.Add(image);

                yield return null;
            }

            if (thisLoad != screenImageLoadVersion || !running)
                yield break;

            screenImageAssetsLoaded = true;
        }

        private bool IsSupportedScreenImage(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }

        private Severity3ScreenImage LoadScreenImageAsset(string path)
        {
            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Il2CppStructArray<byte> imageBytes =
                    new Il2CppStructArray<byte>(File.ReadAllBytes(path));
                if (!ImageConversion.LoadImage(texture, imageBytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(path);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = texture.name;
                return new Severity3ScreenImage
                {
                    Path = path,
                    Texture = texture,
                    Sprite = sprite
                };
            }
            catch (System.Exception)
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);

                return null;
            }
        }

        private IEnumerator FlashImageLoop(int thisLoop)
        {
            while (running && flashImagesEnabled && thisLoop == flashImageLoopVersion)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(
                    MinimumFlashDelaySeconds,
                    MaximumFlashDelaySeconds
                ));

                if (!running || !flashImagesEnabled || thisLoop != flashImageLoopVersion)
                    yield break;

                while (!screenImageAssetsLoaded && running && flashImagesEnabled &&
                       thisLoop == flashImageLoopVersion)
                    yield return null;

                if (!running || !flashImagesEnabled || thisLoop != flashImageLoopVersion)
                    yield break;

                GameObject flashObject = CreateRandomFlashOverlay();
                if (flashObject == null)
                    yield break;

                yield return new WaitForSecondsRealtime(FlashDurationSeconds);
                if (activeFlashOverlay == flashObject)
                    ClearFlashOverlay();
            }
        }

        public void FlashRandomImageNow()
        {
            if (!running)
            {
                return;
            }

            MelonCoroutines.Start(FlashRandomImageOnce());
        }

        private IEnumerator FlashRandomImageOnce()
        {
            while (!screenImageAssetsLoaded && running)
                yield return null;

            if (!running)
                yield break;

            GameObject flashObject = CreateRandomFlashOverlay();
            if (flashObject == null)
                yield break;

            yield return new WaitForSecondsRealtime(FlashDurationSeconds);
            if (activeFlashOverlay == flashObject)
                ClearFlashOverlay();
        }

        private GameObject CreateRandomFlashOverlay()
        {
            if (flashImages.Count == 0)
            {
                return null;
            }

            int imageIndex = UnityEngine.Random.Range(0, flashImages.Count);
            if (flashImages.Count > 1 && imageIndex == previousFlashImageIndex)
                imageIndex = (imageIndex + UnityEngine.Random.Range(
                    1,
                    flashImages.Count
                )) % flashImages.Count;
            previousFlashImageIndex = imageIndex;

            Severity3ScreenImage image = flashImages[imageIndex];
            if (activeFlashOverlay != null)
                UnityEngine.Object.Destroy(activeFlashOverlay);

            activeFlashOverlay = CreateScreenImageOverlay(
                image,
                "Schizophrenia Severity 3 Flash"
            );
            if (activeFlashOverlay == null)
                return null;

            return activeFlashOverlay;
        }

        private GameObject CreateScreenImageOverlay(
            Severity3ScreenImage image,
            string objectName
        )
        {
            Camera playerCamera = Camera.main;
            if (playerCamera == null || image == null || image.Sprite == null)
            {
                return null;
            }

            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(playerCamera.transform, false);
            imageObject.transform.localPosition = Vector3.forward * ScreenImageDistanceMeters;
            imageObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = image.Sprite;
            spriteRenderer.sortingOrder = 32767;
            spriteRenderer.flipX = true;

            float verticalFieldOfView = Mathf.Clamp(playerCamera.fieldOfView, 1f, 179f);
            float viewHeight = 2f * ScreenImageDistanceMeters * Mathf.Tan(
                verticalFieldOfView * 0.5f * Mathf.Deg2Rad
            );
            float viewWidth = viewHeight * Mathf.Max(0.1f, playerCamera.aspect);
            Vector3 spriteSize = image.Sprite.bounds.size;
            imageObject.transform.localScale = new Vector3(
                viewWidth / Mathf.Max(0.001f, spriteSize.x),
                viewHeight / Mathf.Max(0.001f, spriteSize.y),
                1f
            );

            return imageObject;
        }

        private void ClearScreenOverlays()
        {
            ClearFlashOverlay();
        }

        private void ClearFlashOverlay()
        {
            if (activeFlashOverlay != null)
                UnityEngine.Object.Destroy(activeFlashOverlay);

            activeFlashOverlay = null;
        }

        private void ClearScreenImageAssets()
        {
            for (int i = 0; i < flashImages.Count; i++)
                DestroyScreenImageAsset(flashImages[i]);
            flashImages.Clear();
            screenImageAssetsLoaded = false;
        }

        private void DestroyScreenImageAsset(Severity3ScreenImage image)
        {
            if (image == null)
                return;
            if (image.Sprite != null)
                UnityEngine.Object.Destroy(image.Sprite);
            if (image.Texture != null)
                UnityEngine.Object.Destroy(image.Texture);
        }

        private void StartEntitySpawnLoop()
        {
            if (!running || !entitySpawnsEnabled)
                return;

            int thisLoop = ++entitySpawnLoopVersion;
            MelonCoroutines.Start(SpawnEntityImages(thisLoop));
        }

        private IEnumerator SpawnEntityImages(int thisLoop)
        {
            while (running && thisLoop == entitySpawnLoopVersion)
            {
                yield return new WaitForSecondsRealtime(
                    UnityEngine.Random.Range(
                        MinimumEntitySpawnDelaySeconds,
                        MaximumEntitySpawnDelaySeconds
                    )
                );

                if (!running || thisLoop != entitySpawnLoopVersion)
                    yield break;

                List<Severity3EntityImage> wave = SpawnEntityWave();
                if (wave.Count == 0)
                    continue;

                MelonCoroutines.Start(DespawnEntityWave(wave));
            }
        }

        private List<Severity3EntityImage> SpawnEntityWave()
        {
            List<Severity3EntityImage> wave = new List<Severity3EntityImage>();
            RefreshEntityImagePaths();
            if (entityImagePaths.Count == 0)
            {
                if (!loggedMissingEntityImages)
                {
                    loggedMissingEntityImages = true;
                }
                return wave;
            }

            loggedMissingEntityImages = false;
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return wave;
            }

            int requestedCount = UnityEngine.Random.Range(1, 4);
            List<Vector3> spawnPositions = new List<Vector3>();
            List<string> availablePaths = new List<string>(entityImagePaths);

            for (int i = 0; i < requestedCount; i++)
            {
                if (availablePaths.Count == 0)
                    availablePaths.AddRange(entityImagePaths);

                int pathIndex = UnityEngine.Random.Range(0, availablePaths.Count);
                string imagePath = availablePaths[pathIndex];
                availablePaths.RemoveAt(pathIndex);

                Severity3EntityImage image = SpawnEntityImage(
                    playerCamera,
                    imagePath,
                    spawnPositions
                );
                if (image == null)
                    continue;

                wave.Add(image);
                activeEntityImages.Add(image);
                spawnPositions.Add(image.GameObject.transform.position);
            }

            return wave;
        }

        private IEnumerator DespawnEntityWave(List<Severity3EntityImage> wave)
        {
            yield return new WaitForSecondsRealtime(EntityLifetimeSeconds);

            for (int i = 0; i < wave.Count; i++)
            {
                if (activeEntityImages.Contains(wave[i]))
                    DestroyEntityImage(wave[i], true);
            }
        }

        private void RefreshEntityImagePaths()
        {
            entityImagePaths.Clear();
            string entityDirectory = Path.Combine(
                Schizophrenia.ModAssetPaths.Root,
                EntityImageFolderName
            );

            try
            {
                Directory.CreateDirectory(entityDirectory);
                string[] files = Directory.GetFiles(entityDirectory);
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < files.Length; i++)
                {
                    if (string.Equals(
                            Path.GetExtension(files[i]),
                            ".png",
                            System.StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        entityImagePaths.Add(files[i]);
                    }
                }


                for (int i = 0; i < entityImagePaths.Count; i++)
                {
                    string imagePath = entityImagePaths[i];
                    EntityImageLoaded?.Invoke(
                        Path.GetFileNameWithoutExtension(imagePath),
                        imagePath
                    );
                }
            }
            catch (System.Exception)
            {
            }
        }

        private Severity3EntityImage SpawnEntityImage(
            Camera playerCamera,
            string imagePath,
            List<Vector3> existingPositions
        )
        {
            Texture2D texture = null;
            Sprite sprite = null;
            GameObject imageObject = null;

            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Il2CppStructArray<byte> imageBytes =
                    new Il2CppStructArray<byte>(File.ReadAllBytes(imagePath));
                if (!ImageConversion.LoadImage(texture, imageBytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(imagePath);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                float pixelsPerUnit = Mathf.Max(
                    1f,
                    Mathf.Max(texture.width, texture.height) / 2f
                );
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit
                );
                sprite.name = texture.name;

                Vector3 position;
                float distanceFeet;
                if (!TryChooseEntityPosition(
                        playerCamera,
                        existingPositions,
                        out position,
                        out distanceFeet
                    ))
                {
                    UnityEngine.Object.Destroy(sprite);
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                imageObject = new GameObject("Schizophrenia Severity 3 Entity");
                imageObject.transform.position = position;
                imageObject.transform.rotation = Quaternion.LookRotation(
                    playerCamera.transform.position - position,
                    Vector3.up
                );

                SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = 32767;

                return new Severity3EntityImage
                {
                    GameObject = imageObject,
                    Texture = texture,
                    Sprite = sprite
                };
            }
            catch (System.Exception)
            {
                if (imageObject != null)
                    UnityEngine.Object.Destroy(imageObject);
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);

                return null;
            }
        }

        private bool TryChooseEntityPosition(
            Camera playerCamera,
            List<Vector3> existingPositions,
            out Vector3 position,
            out float distanceFeet
        )
        {
            position = Vector3.zero;
            distanceFeet = 0f;
            Vector3 flatForward = Vector3.ProjectOnPlane(
                playerCamera.transform.forward,
                Vector3.up
            );
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = playerCamera.transform.forward;
            flatForward.Normalize();

            float minimumDistance = MinimumEntityDistanceFeet * FeetToMeters;
            float maximumDistance = MaximumEntityDistanceFeet * FeetToMeters;
            Vector3 origin = playerCamera.transform.position;

            for (int attempt = 0; attempt < EntityPositionAttempts; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * flatForward;
                direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                float requestedDistance = UnityEngine.Random.Range(
                    minimumDistance,
                    maximumDistance
                );

                float chosenDistance = requestedDistance;
                float obstructionDistance;
                if (TryGetEntityObstructionDistance(
                        playerCamera,
                        direction,
                        requestedDistance,
                        out obstructionDistance
                    ))
                {
                    chosenDistance = obstructionDistance - EntityWallClearanceMeters;
                }

                if (chosenDistance < minimumDistance || chosenDistance > maximumDistance)
                    continue;

                Vector3 candidatePosition = origin + direction * chosenDistance;
                bool tooCloseToAnotherImage = false;
                for (int i = 0; i < existingPositions.Count; i++)
                {
                    if (Vector3.Distance(candidatePosition, existingPositions[i]) <
                        EntityMinimumSeparationMeters)
                    {
                        tooCloseToAnotherImage = true;
                        break;
                    }
                }

                if (tooCloseToAnotherImage)
                    continue;

                position = candidatePosition;
                distanceFeet = chosenDistance / FeetToMeters;
                return true;
            }

            return false;
        }

        private bool TryGetEntityObstructionDistance(
            Camera playerCamera,
            Vector3 direction,
            float maximumDistance,
            out float obstructionDistance
        )
        {
            obstructionDistance = 0f;
            Vector3 rayOrigin = playerCamera.transform.position;
            float distanceTravelled = 0f;
            float remainingDistance = maximumDistance;

            for (int cast = 0; cast < 16 && remainingDistance > 0.01f; cast++)
            {
                RaycastHit hit;
                if (!Physics.Raycast(
                        rayOrigin,
                        direction,
                        out hit,
                        remainingDistance,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore
                    ))
                {
                    return false;
                }

                if (IsPlayerRigCollider(hit.transform, playerCamera.transform))
                {
                    float skipDistance = hit.distance + 0.03f;
                    rayOrigin += direction * skipDistance;
                    distanceTravelled += skipDistance;
                    remainingDistance = maximumDistance - distanceTravelled;
                    continue;
                }

                obstructionDistance = distanceTravelled + hit.distance;
                return true;
            }

            return false;
        }

        private bool IsPlayerRigCollider(Transform hitTransform, Transform cameraTransform)
        {
            if (hitTransform == null || cameraTransform == null)
                return false;

            return hitTransform == cameraTransform ||
                hitTransform.IsChildOf(cameraTransform) ||
                cameraTransform.IsChildOf(hitTransform) ||
                hitTransform.root == cameraTransform.root;
        }

        private void DestroyEntityImage(Severity3EntityImage image, bool playDisappearSound)
        {
            if (image == null || !activeEntityImages.Contains(image))
                return;

            Vector3 soundPosition = image.GameObject != null
                ? image.GameObject.transform.position
                : Vector3.zero;
            if (image.GameObject != null)
                UnityEngine.Object.Destroy(image.GameObject);
            if (image.Sprite != null)
                UnityEngine.Object.Destroy(image.Sprite);
            if (image.Texture != null)
                UnityEngine.Object.Destroy(image.Texture);
            activeEntityImages.Remove(image);

            if (!playDisappearSound || !disappearSoundsEnabled)
                return;

            if (disappearAudioClips.Count == 0)
            {
                if (!loggedMissingEntityAudio)
                {
                    loggedMissingEntityAudio = true;
                }
                return;
            }

            AudioClip clip = disappearAudioClips[UnityEngine.Random.Range(0, disappearAudioClips.Count)];
            PlayDisappearAudioAt(clip, soundPosition, false);
        }

        private void PlayDisappearAudioAt(AudioClip clip, Vector3 soundPosition, bool developerInitiated)
        {
            if (clip == null)
                return;

            SchizophreniaAudio.PlayAtPoint(
                clip,
                soundPosition,
                BoneLib.Audio.NPCVocals,
                audioplaceratio.GetPlaybackVolume(),
                UnityEngine.Random.Range(0.9f, 1.1f),
                1f,
                25f * FeetToMeters,
                "Severity3Entity"
            );
            audioplaceratio.IgnoreMixWhileClipPlays(clip);
        }

        private void StartHeadAudioLoop()
        {
            if (!running || !headAudioEnabled || audioLoading || headAudioLoopActive ||
                headAudioClips.Count == 0)
                return;

            headAudioLoopActive = true;
            int thisLoop = ++headAudioLoopVersion;
            MelonCoroutines.Start(PlayHeadAudioContinuously(thisLoop));
        }

        private IEnumerator PlayHeadAudioContinuously(int thisLoop)
        {
            while (running && thisLoop == headAudioLoopVersion)
            {
                if (headAudioClips.Count == 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                int clipIndex = UnityEngine.Random.Range(0, headAudioClips.Count);
                if (headAudioClips.Count > 1 && clipIndex == previousHeadAudioClipIndex)
                    clipIndex = (clipIndex + 1) % headAudioClips.Count;
                previousHeadAudioClipIndex = clipIndex;

                AudioClip clip = headAudioClips[clipIndex];
                if (clip == null)
                {
                    yield return null;
                    continue;
                }

                SchizophreniaAudio.Play2D(
                    clip,
                    BoneLib.Audio.InHead,
                    audioplaceratio.GetPlaybackVolume(),
                    1f,
                    "Severity3Head"
                );
                audioplaceratio.IgnoreMixWhileClipPlays(clip);

                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, clip.length));
            }
        }

        private bool EnsureVolume()
        {
            if (severity3Volume != null && severity3Volume.profile != null)
                return true;

            GameObject volumeObject = new GameObject(VolumeObjectName);
            UnityEngine.Object.DontDestroyOnLoad(volumeObject);

            severity3Volume = volumeObject.AddComponent<Volume>();
            severity3Volume.isGlobal = true;
            severity3Volume.priority = 10000f;
            severity3Volume.weight = 0f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = VolumeObjectName + " Profile";
            severity3Volume.profile = profile;

            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = Severity3Contrast;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = Severity3Saturation;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = Severity3RedTint;

            return true;
        }

        private IEnumerator FadeVolumeWeight(float targetWeight, int thisTransition)
        {
            float startWeight = severity3Volume.weight;
            float elapsed = 0f;

            while (elapsed < TransitionDurationSeconds &&
                   thisTransition == transitionVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / TransitionDurationSeconds);
                severity3Volume.weight = Mathf.Lerp(startWeight, targetWeight, progress);
                yield return null;
            }

            if (thisTransition == transitionVersion && severity3Volume != null)
                severity3Volume.weight = targetWeight;
        }
    }
}
