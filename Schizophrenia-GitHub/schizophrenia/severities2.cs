using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Bonelab;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Schizophrenia.Severitys
{
    public class severities2
    {
        private const float TransitionDurationSeconds = 5f;
        private const float Severity2Contrast = 50f;
        private const float Severity2Saturation = -25f;
        private const string VolumeObjectName = "Schizophrenia Severity 2 Color Volume";
        private const float MinimumSpatialAudioDelaySeconds = 10f;
        private const float MaximumSpatialAudioDelaySeconds = 30f;
        private const float MinimumHeadAudioDelaySeconds = 10f;
        private const float MaximumHeadAudioDelaySeconds = 20f;
        private const float MinimumEntitySpawnDelaySeconds = 30f;
        private const float MaximumEntitySpawnDelaySeconds = 60f;
        private const float EntityDistanceFeet = 30f;
        private const float MinimumEntityDistanceFeet = 5f;
        private const float MaximumEntityDistanceFeet = 50f;
        private const float EntityGeometryClearanceMeters = 1.5f;
        private const int EntityPositionAttempts = 24;
        private const float EntityLookDurationSeconds = 0.25f;
        private const float EntityCenterHorizontalTolerance = 0.08f;
        private const float EntityCenterVerticalTolerance = 0.08f;
        private const float ClipboardRefreshIntervalSeconds = 1f;
        private const float ClipboardObjectScanIntervalSeconds = 5f;
        private const float RadialMenuRefreshIntervalSeconds = 0.5f;
        private const float RadialMenuObjectScanIntervalSeconds = 5f;
        private const string ClipboardMessage = "NO ESCAPE";
        private const string RadialMenuCanvasName = "CANVAS_RADIALUI";
        private const string Severity2RadialMenuLabel = "Error.";
        private const float RoseScreamingMinimumFeet = 20f;
        private const float RoseScreamingMaximumFeet = 50f;
        private const float GirlLaughingMinimumFeet = 1f;
        private const float GirlLaughingMaximumFeet = 3f;
        private const float MinimumSpatialAudioDistanceFeet = 5f;
        private const float MaximumSpatialAudioDistanceFeet = 15f;
        private const float FeetToMeters = 0.3048f;
        private const string RoseScreamingClipName = "rosescreaming";
        private const string GirlLaughingClipName = "girllaugh";
        private const string SpatialAudioFolderName = "Audio";
        private const string HeadAudioFolderName = "Audio1";
        private const string EntityAudioFolderName = "Audio2";
        private const string MedicationAudioFolderName = "Audio4";
        private const string EntityImageFolderName = "entities";
        private Volume severity2Volume;
        private readonly List<AudioClip> severity2AudioClips = new List<AudioClip>();
        private readonly List<AudioClip> headAudioClips = new List<AudioClip>();
        private readonly List<AudioClip> entityAudioClips = new List<AudioClip>();
        private readonly List<AudioClip> medicationAudioClips = new List<AudioClip>();
        private readonly List<string> entityImagePaths = new List<string>();
        private readonly HashSet<string> loadedAudioFilePaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<LoreClipboard, ClipboardTextSnapshot> clipboardTextSnapshots =
            new Dictionary<LoreClipboard, ClipboardTextSnapshot>();
        private readonly List<LoreClipboard> cachedClipboards = new List<LoreClipboard>();
        private readonly Dictionary<TMP_Text, RadialTextSnapshot> radialTmpTextSnapshots =
            new Dictionary<TMP_Text, RadialTextSnapshot>();
        private readonly Dictionary<Text, RadialTextSnapshot> radialUiTextSnapshots =
            new Dictionary<Text, RadialTextSnapshot>();
        private readonly List<TMP_Text> cachedRadialTmpLabels = new List<TMP_Text>();
        private readonly List<Text> cachedRadialUiLabels = new List<Text>();
        private bool running;
        private bool audioLoading;
        private bool visualsEnabled = true;
        private bool radialMenuErrorEnabled = true;
        private bool clipboardMessageEnabled = true;
        private bool spatialAudioEnabled = true;
        private bool headAudioEnabled = true;
        private bool entityImagesEnabled = true;
        private bool entityAudioEnabled = true;
        private int transitionVersion;
        private int spatialAudioLoopVersion;
        private int headAudioLoopVersion;
        private int entityImageLoopVersion;
        private int previousSpatialAudioClipIndex = -1;
        private int previousHeadAudioClipIndex = -1;
        private int previousEntityImageIndex = -1;
        private int previousMedicationAudioIndex = -1;
        private float nextClipboardRefreshTime;
        private float nextClipboardObjectScanTime;
        private float nextRadialMenuRefreshTime;
        private float nextRadialMenuObjectScanTime;
        private float entityCenterLookDuration;
        private GameObject activeEntityObject;
        private Texture2D activeEntityTexture;
        private Sprite activeEntitySprite;
        private bool loggedMissingEntityAudio;
        private bool loggedMissingMedicationAudio;
        private bool loggedClipboardReplacement;
        private bool loggedRadialMenuMessage;

        private sealed class ClipboardTextSnapshot
        {
            public string Title;
            public string Body;
            public Color BodyColor;
            public bool HasBodyColor;
        }

        private sealed class RadialTextSnapshot
        {
            public string Text;
            public Color Color;
        }

        public event Action<AudioClip, bool> AudioClipLoaded;
        public event Action<AudioClip> EntityAudioClipLoaded;
        public event Action<AudioClip> MedicationAudioClipLoaded;
        public event Action<string, string> EntityImageLoaded;

        public void Start()
        {
            if (running)
                return;

            running = true;
            nextRadialMenuRefreshTime = 0f;
            nextRadialMenuObjectScanTime = 0f;
            if (radialMenuErrorEnabled)
                UpdateRadialMenuErrorDisplay();
            if (visualsEnabled)
                BeginVisuals();

            if (clipboardMessageEnabled)
            {
                nextClipboardObjectScanTime = 0f;
                ApplyClipboardMessage();
                nextClipboardRefreshTime = Time.time + ClipboardRefreshIntervalSeconds;
            }
            StartAudioLoops();
            if (entityImagesEnabled)
                StartEntityImageLoop();
            LoadAudioFiles();
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            spatialAudioLoopVersion++;
            headAudioLoopVersion++;
            entityImageLoopVersion++;
            RestoreRadialMenuText();
            DestroyActiveEntityImage(false);
            RestoreClipboardText();
            SchizophreniaAudio.FadeOutCategory("Severity2Spatial");
            SchizophreniaAudio.FadeOutCategory("Severity2Head");
            SchizophreniaAudio.FadeOutCategory("Severity2Entity");

            EndVisuals();
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

        public void SetRadialMenuErrorEnabled(bool enabled)
        {
            radialMenuErrorEnabled = enabled;
            if (!running)
                return;

            if (enabled)
            {
                nextRadialMenuRefreshTime = 0f;
                UpdateRadialMenuErrorDisplay();
            }
            else
                RestoreRadialMenuText();
        }

        public void SetClipboardMessageEnabled(bool enabled)
        {
            clipboardMessageEnabled = enabled;
            if (!running)
                return;

            if (enabled)
            {
                nextClipboardObjectScanTime = 0f;
                ApplyClipboardMessage();
                nextClipboardRefreshTime = Time.time + ClipboardRefreshIntervalSeconds;
            }
            else
                RestoreClipboardText();
        }

        public void SetSpatialAudioEnabled(bool enabled)
        {
            spatialAudioEnabled = enabled;
            spatialAudioLoopVersion++;
            if (enabled && running)
                StartSpatialAudioLoop();
            else if (!enabled)
                SchizophreniaAudio.FadeOutCategory("Severity2Spatial");
        }

        public void SetHeadAudioEnabled(bool enabled)
        {
            headAudioEnabled = enabled;
            headAudioLoopVersion++;
            if (enabled && running)
                StartHeadAudioLoop();
            else if (!enabled)
                SchizophreniaAudio.FadeOutCategory("Severity2Head");
        }

        public void SetEntityImagesEnabled(bool enabled)
        {
            entityImagesEnabled = enabled;
            entityImageLoopVersion++;
            if (enabled && running)
                StartEntityImageLoop();
            else if (!enabled)
            {
                DestroyActiveEntityImage(false);
                SchizophreniaAudio.FadeOutCategory("Severity2Entity");
            }
        }

        public void SetEntityAudioEnabled(bool enabled)
        {
            entityAudioEnabled = enabled;
            if (!enabled)
                SchizophreniaAudio.FadeOutCategory("Severity2Entity");
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
            if (severity2Volume == null)
                return;

            int thisTransition = ++transitionVersion;
            MelonCoroutines.Start(FadeVolumeWeight(0f, thisTransition));
        }

        private bool EnsureVolume()
        {
            if (severity2Volume != null && severity2Volume.profile != null)
                return true;

            GameObject volumeObject = new GameObject(VolumeObjectName);
            UnityEngine.Object.DontDestroyOnLoad(volumeObject);

            severity2Volume = volumeObject.AddComponent<Volume>();
            severity2Volume.isGlobal = true;
            severity2Volume.priority = 9999f;
            severity2Volume.weight = 0f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = VolumeObjectName + " Profile";
            severity2Volume.profile = profile;

            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = Severity2Contrast;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = Severity2Saturation;

            return true;
        }

        private IEnumerator FadeVolumeWeight(float targetWeight, int thisTransition)
        {
            float startWeight = severity2Volume.weight;
            float elapsed = 0f;

            while (elapsed < TransitionDurationSeconds &&
                   thisTransition == transitionVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / TransitionDurationSeconds);
                severity2Volume.weight = Mathf.Lerp(startWeight, targetWeight, progress);
                yield return null;
            }

            if (thisTransition == transitionVersion && severity2Volume != null)
                severity2Volume.weight = targetWeight;
        }

        public void LoadAudioFiles()
        {
            if (audioLoading)
            {
                return;
            }

            audioLoading = true;
            MelonCoroutines.Start(LoadSeverity2Audio());
        }

        public void PlaySpecificSound(AudioClip clip, bool playInsideHead)
        {
            PlayAudioClip(clip, playInsideHead, true);
        }

        public void PlayRandomSpatialSoundNow()
        {
            if (severity2AudioClips.Count == 0)
            {
                return;
            }

            PlayAudioClip(
                severity2AudioClips[UnityEngine.Random.Range(0, severity2AudioClips.Count)],
                false,
                true
            );
        }

        public void PlayRandomHeadSoundNow()
        {
            if (headAudioClips.Count == 0)
            {
                return;
            }

            PlayAudioClip(
                headAudioClips[UnityEngine.Random.Range(0, headAudioClips.Count)],
                true,
                true
            );
        }

        public void PlayRandomEntitySoundNow()
        {
            if (entityAudioClips.Count == 0)
            {
                return;
            }

            PlaySpecificEntitySound(
                entityAudioClips[UnityEngine.Random.Range(0, entityAudioClips.Count)]
            );
        }

        public void ApplyClipboardMessageNow()
        {
            if (!running)
            {
                return;
            }

            ApplyClipboardMessage();
        }

        public void PlaySpecificEntitySound(AudioClip clip)
        {
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            Vector3 soundPosition =
                playerCamera.transform.position +
                playerCamera.transform.forward * (EntityDistanceFeet * FeetToMeters);
            PlayEntityAudioAt(clip, soundPosition, true);
        }

        public void PlayMedicationSound()
        {
            if (medicationAudioClips.Count == 0)
            {
                string medicationAudioDirectory = Path.Combine(
                    Schizophrenia.ModAssetPaths.Root,
                    MedicationAudioFolderName
                );
                LoadAudioFolder(
                    medicationAudioDirectory,
                    true,
                    false,
                    true
                );
            }

            if (medicationAudioClips.Count == 0)
            {
                if (!loggedMissingMedicationAudio)
                {
                    loggedMissingMedicationAudio = true;
                }
                return;
            }

            int clipIndex = UnityEngine.Random.Range(0, medicationAudioClips.Count);
            if (medicationAudioClips.Count > 1 && clipIndex == previousMedicationAudioIndex)
                clipIndex = (clipIndex + 1) % medicationAudioClips.Count;
            previousMedicationAudioIndex = clipIndex;

            AudioClip clip = medicationAudioClips[clipIndex];
            PlayMedicationSound(clip, false);
        }

        public void PlaySpecificMedicationSound(AudioClip clip)
        {
            PlayMedicationSound(clip, true);
        }

        private void PlayMedicationSound(AudioClip clip, bool developerInitiated)
        {
            if (clip == null)
                return;

            PlayAudioClip(clip, true, developerInitiated, "Medication");
        }

        public void SpawnEntityImageForDeveloper()
        {
            if (activeEntityObject != null)
            {
                return;
            }

            RefreshEntityImagePaths();
            if (entityImagePaths.Count == 0)
            {
                return;
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            if (entityAudioClips.Count == 0 && !audioLoading)
                LoadAudioFiles();

            SpawnRandomEntityImage(playerCamera);
        }

        public void DismissEntityForMedication()
        {
            if (activeEntityObject == null)
                return;

            DestroyActiveEntityImage(false);
        }

        public void LoadEntityImageOptions()
        {
            RefreshEntityImagePaths();
            for (int i = 0; i < entityImagePaths.Count; i++)
            {
                string imagePath = entityImagePaths[i];
                EntityImageLoaded?.Invoke(
                    Path.GetFileNameWithoutExtension(imagePath),
                    imagePath
                );
            }
        }

        public void SpawnSpecificEntityImageForDeveloper(string imagePath)
        {
            if (activeEntityObject != null)
            {
                return;
            }

            RefreshEntityImagePaths();
            string resolvedImagePath = null;
            for (int i = 0; i < entityImagePaths.Count; i++)
            {
                if (string.Equals(
                        entityImagePaths[i],
                        imagePath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    resolvedImagePath = entityImagePaths[i];
                    break;
                }
            }

            if (resolvedImagePath == null)
            {
                return;
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            if (entityAudioClips.Count == 0 && !audioLoading)
                LoadAudioFiles();

            SpawnEntityImage(playerCamera, resolvedImagePath);
        }

        private void RefreshClipboardCache()
        {
            cachedClipboards.Clear();
            try
            {
                Il2CppArrayBase<LoreClipboard> clipboards =
                    Resources.FindObjectsOfTypeAll<LoreClipboard>();
                if (clipboards == null)
                    return;

                for (int i = 0; i < clipboards.Length; i++)
                {
                    LoreClipboard clipboard = clipboards[i];
                    if (clipboard != null && clipboard.gameObject.scene.IsValid())
                        cachedClipboards.Add(clipboard);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ApplyClipboardMessage()
        {
            try
            {
                if (Time.time >= nextClipboardObjectScanTime)
                {
                    RefreshClipboardCache();
                    nextClipboardObjectScanTime =
                        Time.time + ClipboardObjectScanIntervalSeconds;
                }

                int changedCount = 0;
                for (int i = 0; i < cachedClipboards.Count; i++)
                {
                    LoreClipboard clipboard = cachedClipboards[i];
                    if (clipboard == null || !clipboard.gameObject.scene.IsValid())
                        continue;

                    if (!clipboardTextSnapshots.ContainsKey(clipboard))
                    {
                        clipboardTextSnapshots.Add(
                            clipboard,
                            new ClipboardTextSnapshot
                            {
                                Title = clipboard.title ?? string.Empty,
                                Body = clipboard.body ?? string.Empty,
                                BodyColor = clipboard.tmp_body != null
                                    ? clipboard.tmp_body.color
                                    : Color.white,
                                HasBodyColor = clipboard.tmp_body != null
                            }
                        );
                    }

                    ClipboardTextSnapshot snapshot = clipboardTextSnapshots[clipboard];
                    if (!string.IsNullOrEmpty(clipboard.title))
                        clipboard.title = string.Empty;
                    if (clipboard.body != ClipboardMessage)
                        clipboard.body = ClipboardMessage;
                    if (clipboard.tmp_title != null && clipboard.tmp_title.text.Length != 0)
                        clipboard.tmp_title.text = string.Empty;
                    if (clipboard.tmp_body != null)
                    {
                        if (!snapshot.HasBodyColor)
                        {
                            snapshot.BodyColor = clipboard.tmp_body.color;
                            snapshot.HasBodyColor = true;
                        }
                        if (clipboard.tmp_body.text != ClipboardMessage)
                            clipboard.tmp_body.text = ClipboardMessage;
                        if (clipboard.tmp_body.color != Color.red)
                            clipboard.tmp_body.color = Color.red;
                    }

                    changedCount++;
                }

                if (changedCount > 0 && !loggedClipboardReplacement)
                {
                    loggedClipboardReplacement = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void RestoreClipboardText()
        {
            foreach (KeyValuePair<LoreClipboard, ClipboardTextSnapshot> entry in clipboardTextSnapshots)
            {
                LoreClipboard clipboard = entry.Key;
                if (clipboard == null)
                    continue;

                try
                {
                    clipboard.title = entry.Value.Title;
                    clipboard.body = entry.Value.Body;
                    if (clipboard.tmp_title != null)
                        clipboard.tmp_title.text = entry.Value.Title;
                    if (clipboard.tmp_body != null)
                    {
                        clipboard.tmp_body.text = entry.Value.Body;
                        if (entry.Value.HasBodyColor)
                            clipboard.tmp_body.color = entry.Value.BodyColor;
                    }
                }
                catch (Exception)
                {
                }
            }

            if (clipboardTextSnapshots.Count > 0)
            {
            }

            clipboardTextSnapshots.Clear();
            cachedClipboards.Clear();
            nextClipboardObjectScanTime = 0f;
            loggedClipboardReplacement = false;
        }

        public void Update()
        {
            UpdateEntityLookDetection();

            if (!running)
                return;

            if (radialMenuErrorEnabled && Time.unscaledTime >= nextRadialMenuRefreshTime)
            {
                UpdateRadialMenuErrorDisplay();
                nextRadialMenuRefreshTime =
                    Time.unscaledTime + RadialMenuRefreshIntervalSeconds;
            }

            if (clipboardMessageEnabled && Time.time >= nextClipboardRefreshTime)
            {
                ApplyClipboardMessage();
                nextClipboardRefreshTime = Time.time + ClipboardRefreshIntervalSeconds;
            }
        }

        public void DisplaySeverity3MenuWord(string word)
        {
            if (!string.IsNullOrEmpty(word))
                ApplyRadialMenuMessage(word, "Severity 3");
        }

        private void UpdateRadialMenuErrorDisplay()
        {
            ApplyRadialMenuMessage(Severity2RadialMenuLabel, "Severity 2");
        }

        private void ApplyRadialMenuMessage(string message, string severityName)
        {
            try
            {
                if (Time.unscaledTime >= nextRadialMenuObjectScanTime)
                {
                    RefreshRadialMenuLabelCache();
                    nextRadialMenuObjectScanTime =
                        Time.unscaledTime + RadialMenuObjectScanIntervalSeconds;
                }

                int changedTextCount = 0;
                for (int i = 0; i < cachedRadialTmpLabels.Count; i++)
                {
                    TMP_Text label = cachedRadialTmpLabels[i];
                    if (label == null)
                        continue;

                    if (!radialTmpTextSnapshots.ContainsKey(label))
                    {
                        radialTmpTextSnapshots.Add(
                            label,
                            new RadialTextSnapshot
                            {
                                Text = label.text,
                                Color = label.color
                            }
                        );
                    }

                    if (label.text != message || label.color != Color.red)
                    {
                        label.text = message;
                        label.color = Color.red;
                        changedTextCount++;
                    }
                }

                for (int i = 0; i < cachedRadialUiLabels.Count; i++)
                {
                    Text label = cachedRadialUiLabels[i];
                    if (label == null)
                        continue;

                    if (!radialUiTextSnapshots.ContainsKey(label))
                    {
                        radialUiTextSnapshots.Add(
                            label,
                            new RadialTextSnapshot
                            {
                                Text = label.text,
                                Color = label.color
                            }
                        );
                    }

                    if (label.text != message || label.color != Color.red)
                    {
                        label.text = message;
                        label.color = Color.red;
                        changedTextCount++;
                    }
                }

                if (changedTextCount > 0 && !loggedRadialMenuMessage)
                {
                    loggedRadialMenuMessage = true;
                }
            }
            catch (Exception)
            {
                if (!loggedRadialMenuMessage)
                {
                    loggedRadialMenuMessage = true;
                }
            }
        }

        private void RefreshRadialMenuLabelCache()
        {
            cachedRadialTmpLabels.Clear();
            cachedRadialUiLabels.Clear();

            try
            {
                Il2CppArrayBase<GameObject> sceneObjects =
                    GameObject.FindObjectsOfType<GameObject>(true);
                if (sceneObjects == null)
                    return;

                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    GameObject radialCanvas = sceneObjects[i];
                    if (radialCanvas == null ||
                        radialCanvas.name.IndexOf(
                            RadialMenuCanvasName,
                            StringComparison.OrdinalIgnoreCase
                        ) < 0)
                    {
                        continue;
                    }

                    Il2CppArrayBase<TMP_Text> tmpLabels =
                        radialCanvas.GetComponentsInChildren<TMP_Text>(true);
                    if (tmpLabels != null)
                    {
                        for (int labelIndex = 0; labelIndex < tmpLabels.Length; labelIndex++)
                        {
                            TMP_Text label = tmpLabels[labelIndex];
                            if (label != null && !cachedRadialTmpLabels.Contains(label))
                                cachedRadialTmpLabels.Add(label);
                        }
                    }

                    Il2CppArrayBase<Text> uiLabels =
                        radialCanvas.GetComponentsInChildren<Text>(true);
                    if (uiLabels != null)
                    {
                        for (int labelIndex = 0; labelIndex < uiLabels.Length; labelIndex++)
                        {
                            Text label = uiLabels[labelIndex];
                            if (label != null && !cachedRadialUiLabels.Contains(label))
                                cachedRadialUiLabels.Add(label);
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (!loggedRadialMenuMessage)
                {
                    loggedRadialMenuMessage = true;
                }
            }
        }

        public void RestoreRadialMenuText()
        {
            foreach (KeyValuePair<TMP_Text, RadialTextSnapshot> entry in radialTmpTextSnapshots)
            {
                if (entry.Key == null)
                    continue;

                entry.Key.text = entry.Value.Text;
                entry.Key.color = entry.Value.Color;
            }
            radialTmpTextSnapshots.Clear();
            cachedRadialTmpLabels.Clear();

            foreach (KeyValuePair<Text, RadialTextSnapshot> entry in radialUiTextSnapshots)
            {
                if (entry.Key == null)
                    continue;

                entry.Key.text = entry.Value.Text;
                entry.Key.color = entry.Value.Color;
            }
            radialUiTextSnapshots.Clear();
            cachedRadialUiLabels.Clear();
            nextRadialMenuObjectScanTime = 0f;
            loggedRadialMenuMessage = false;
        }

        private IEnumerator LoadSeverity2Audio()
        {
            string modAudioDirectory = Schizophrenia.ModAssetPaths.Root;

            bool loadedAnyAudio = false;
            loadedAnyAudio |= LoadAudioFolder(
                Path.Combine(modAudioDirectory, SpatialAudioFolderName),
                false,
                false,
                false
            );
            yield return null;
            loadedAnyAudio |= LoadAudioFolder(
                Path.Combine(modAudioDirectory, HeadAudioFolderName),
                true,
                false,
                false
            );
            yield return null;
            loadedAnyAudio |= LoadAudioFolder(
                Path.Combine(modAudioDirectory, EntityAudioFolderName),
                false,
                true,
                false
            );
            yield return null;
            loadedAnyAudio |= LoadAudioFolder(
                Path.Combine(modAudioDirectory, MedicationAudioFolderName),
                true,
                false,
                true
            );

            audioLoading = false;

            if (running && loadedAnyAudio)
                StartAudioLoops();
        }

        private bool LoadAudioFolder(
            string audioDirectory,
            bool playInsideHead,
            bool playForEntity,
            bool playForMedication
        )
        {
            bool loadedAnyAudio = false;
            string[] files = new string[0];
            string audioType = playForEntity
                ? "entity"
                : playForMedication
                    ? "medication UI"
                    : playInsideHead
                    ? "inside-head"
                    : "spatial";

            try
            {
                Directory.CreateDirectory(audioDirectory);
                files = Directory.GetFiles(audioDirectory);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }

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
                    if (clip == null)
                    {
                    }
                    else
                    {
                        clip.name = Path.GetFileNameWithoutExtension(files[i]);
                        loadedAudioFilePaths.Add(files[i]);
                        if (playForEntity)
                            entityAudioClips.Add(clip);
                        else if (playForMedication)
                            medicationAudioClips.Add(clip);
                        else if (playInsideHead)
                            headAudioClips.Add(clip);
                        else
                            severity2AudioClips.Add(clip);

                        loadedAnyAudio = true;
                        if (playForEntity)
                            EntityAudioClipLoaded?.Invoke(clip);
                        else if (playForMedication)
                            MedicationAudioClipLoaded?.Invoke(clip);
                        else
                            AudioClipLoaded?.Invoke(clip, playInsideHead);
                    }
                }
                catch (Exception)
                {
                }
            }

            return loadedAnyAudio;
        }

        private void StartAudioLoops()
        {
            StartSpatialAudioLoop();
            StartHeadAudioLoop();
        }

        private void StartSpatialAudioLoop()
        {
            if (!running || !spatialAudioEnabled || severity2AudioClips.Count == 0)
                return;

            int thisLoop = ++spatialAudioLoopVersion;
            MelonCoroutines.Start(PlayRandomAudio(severity2AudioClips, false, thisLoop));
        }

        private void StartHeadAudioLoop()
        {
            if (!running || !headAudioEnabled || headAudioClips.Count == 0)
                return;

            int thisLoop = ++headAudioLoopVersion;
            MelonCoroutines.Start(PlayRandomAudio(headAudioClips, true, thisLoop));
        }

        private void StartEntityImageLoop()
        {
            if (!running || !entityImagesEnabled)
                return;

            RefreshEntityImagePaths();
            int thisLoop = ++entityImageLoopVersion;
            MelonCoroutines.Start(SpawnEntityImages(thisLoop));
        }

        private IEnumerator SpawnEntityImages(int thisLoop)
        {
            while (running && thisLoop == entityImageLoopVersion)
            {
                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(
                        MinimumEntitySpawnDelaySeconds,
                        MaximumEntitySpawnDelaySeconds
                    )
                );

                if (!running || thisLoop != entityImageLoopVersion)
                    yield break;

                RefreshEntityImagePaths();
                if (entityImagePaths.Count == 0)
                {
                    continue;
                }

                if (activeEntityObject != null)
                    continue;

                Camera playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    continue;
                }

                SpawnRandomEntityImage(playerCamera);
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
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < files.Length; i++)
                {
                    if (string.Equals(
                            Path.GetExtension(files[i]),
                            ".png",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        entityImagePaths.Add(files[i]);
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        private void SpawnRandomEntityImage(Camera playerCamera)
        {
            int imageIndex = UnityEngine.Random.Range(0, entityImagePaths.Count);
            if (entityImagePaths.Count > 1 && imageIndex == previousEntityImageIndex)
                imageIndex = (imageIndex + 1) % entityImagePaths.Count;

            previousEntityImageIndex = imageIndex;
            string imagePath = entityImagePaths[imageIndex];
            SpawnEntityImage(playerCamera, imagePath);
        }

        private void SpawnEntityImage(Camera playerCamera, string imagePath)
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
                    return;
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
                if (!TryChooseEntityPosition(playerCamera, out position, out distanceFeet))
                {
                    UnityEngine.Object.Destroy(sprite);
                    UnityEngine.Object.Destroy(texture);
                    return;
                }

                imageObject = new GameObject("Schizophrenia Severity 2 Entity");
                imageObject.transform.position = position;
                imageObject.transform.rotation = Quaternion.LookRotation(
                    playerCamera.transform.position - position,
                    Vector3.up
                );

                SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = 32767;

                activeEntityObject = imageObject;
                activeEntityTexture = texture;
                activeEntitySprite = sprite;
                entityCenterLookDuration = 0f;
            }
            catch (Exception)
            {
                if (imageObject != null)
                    UnityEngine.Object.Destroy(imageObject);
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);

            }
        }

        private bool TryChooseEntityPosition(
            Camera playerCamera,
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
                float angle = UnityEngine.Random.Range(-90f, 90f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * -flatForward;
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
                    chosenDistance = obstructionDistance - EntityGeometryClearanceMeters;
                }

                if (chosenDistance < minimumDistance || chosenDistance > maximumDistance)
                    continue;

                position = origin + direction * chosenDistance;
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

        private void UpdateEntityLookDetection()
        {
            if (activeEntityObject == null)
                return;

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                entityCenterLookDuration = 0f;
                return;
            }

            activeEntityObject.transform.rotation = Quaternion.LookRotation(
                playerCamera.transform.position - activeEntityObject.transform.position,
                Vector3.up
            );

            Vector3 viewportPosition = playerCamera.WorldToViewportPoint(
                activeEntityObject.transform.position
            );
            bool centeredInView = viewportPosition.z > 0f &&
                Mathf.Abs(viewportPosition.x - 0.5f) <= EntityCenterHorizontalTolerance &&
                Mathf.Abs(viewportPosition.y - 0.5f) <= EntityCenterVerticalTolerance;
            Vector3 toEntity = activeEntityObject.transform.position -
                playerCamera.transform.position;
            float entityDistance = toEntity.magnitude;
            bool lineOfSightBlocked = entityDistance > 0.01f &&
                TryGetEntityObstructionDistance(
                    playerCamera,
                    toEntity / entityDistance,
                    entityDistance,
                    out _
                );
            if (!centeredInView || lineOfSightBlocked)
            {
                entityCenterLookDuration = 0f;
                return;
            }

            entityCenterLookDuration += Time.unscaledDeltaTime;
            if (entityCenterLookDuration >= EntityLookDurationSeconds)
            {
                DestroyActiveEntityImage(true);
            }
        }

        private void DestroyActiveEntityImage(bool playSound)
        {
            if (activeEntityObject == null)
                return;

            Vector3 soundPosition = activeEntityObject.transform.position;
            UnityEngine.Object.Destroy(activeEntityObject);
            activeEntityObject = null;

            if (activeEntitySprite != null)
                UnityEngine.Object.Destroy(activeEntitySprite);
            if (activeEntityTexture != null)
                UnityEngine.Object.Destroy(activeEntityTexture);
            activeEntitySprite = null;
            activeEntityTexture = null;
            entityCenterLookDuration = 0f;

            if (!playSound || !entityAudioEnabled)
                return;

            if (entityAudioClips.Count == 0)
            {
                if (!loggedMissingEntityAudio)
                {
                    loggedMissingEntityAudio = true;
                }
                return;
            }

            int clipIndex = UnityEngine.Random.Range(0, entityAudioClips.Count);
            AudioClip clip = entityAudioClips[clipIndex];
            PlayEntityAudioAt(clip, soundPosition, false);
        }

        private void PlayEntityAudioAt(AudioClip clip, Vector3 position, bool developerInitiated)
        {
            if (clip == null)
            {
                return;
            }

            float volume = audioplaceratio.GetPlaybackVolume();
            float pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioplaceratio.IgnoreMixWhileClipPlays(clip);
            SchizophreniaAudio.PlayAtPoint(
                clip,
                position,
                BoneLib.Audio.NPCVocals,
                volume,
                pitch,
                1f,
                MaximumEntityDistanceFeet * 2f * FeetToMeters,
                "Severity2Entity"
            );
        }

        private IEnumerator PlayRandomAudio(
            List<AudioClip> clips,
            bool playInsideHead,
            int thisLoop
        )
        {
            while (running && IsCurrentAudioLoop(thisLoop, playInsideHead))
            {
                float minimumDelay = playInsideHead
                    ? MinimumHeadAudioDelaySeconds
                    : MinimumSpatialAudioDelaySeconds;
                float maximumDelay = playInsideHead
                    ? MaximumHeadAudioDelaySeconds
                    : MaximumSpatialAudioDelaySeconds;

                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(minimumDelay, maximumDelay)
                );

                if (!running || !IsCurrentAudioLoop(thisLoop, playInsideHead))
                    yield break;

                if (clips.Count == 0)
                    continue;

                int clipIndex = UnityEngine.Random.Range(0, clips.Count);
                int previousIndex = playInsideHead
                    ? previousHeadAudioClipIndex
                    : previousSpatialAudioClipIndex;
                if (clips.Count > 1 && clipIndex == previousIndex)
                    clipIndex = (clipIndex + 1) % clips.Count;

                if (playInsideHead)
                    previousHeadAudioClipIndex = clipIndex;
                else
                    previousSpatialAudioClipIndex = clipIndex;

                float clipPlaybackDuration =
                    PlayAudioClip(clips[clipIndex], playInsideHead, false);
                if (playInsideHead && clipPlaybackDuration > 0f)
                    yield return new WaitForSeconds(clipPlaybackDuration);
            }
        }

        private bool IsCurrentAudioLoop(int loopVersion, bool playInsideHead)
        {
            return loopVersion == (playInsideHead
                ? headAudioLoopVersion
                : spatialAudioLoopVersion);
        }

        private float PlayAudioClip(
            AudioClip clip,
            bool playInsideHead,
            bool developerInitiated,
            string audioCategory = null
        )
        {
            if (clip == null)
            {
                return 0f;
            }

            float volume = audioplaceratio.GetPlaybackVolume();
            float pitch = UnityEngine.Random.Range(0.9f, 1.1f);

            if (playInsideHead)
            {
                SchizophreniaAudio.Play2D(
                    clip,
                    BoneLib.Audio.InHead,
                    volume,
                    pitch,
                    audioCategory ?? "Severity2Head"
                );
                audioplaceratio.IgnoreMixWhileClipPlays(clip);
            }
            else
            {
                Camera playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    return 0f;
                }

                audioplaceratio.IgnoreMixWhileClipPlays(clip);
                SchizophreniaAudio.PlayAtPoint(
                    clip,
                    RandomPositionAround(playerCamera, clip),
                    BoneLib.Audio.NPCVocals,
                    volume,
                    pitch,
                    1f,
                    GetSpatialAudioMaximumDistance(clip),
                    audioCategory ?? "Severity2Spatial"
                );
            }

            return clip.length / pitch;
        }

        private float GetSpatialAudioMaximumDistance(AudioClip clip)
        {
            if (clip != null && clip.name.IndexOf(
                    RoseScreamingClipName,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                return (RoseScreamingMaximumFeet + 10f) * FeetToMeters;
            }

            if (clip != null && clip.name.IndexOf(
                    GirlLaughingClipName,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                return GirlLaughingMaximumFeet * 2f * FeetToMeters;
            }

            return MaximumSpatialAudioDistanceFeet * 2f * FeetToMeters;
        }

        private Vector3 RandomPositionAround(Camera playerCamera, AudioClip clip)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            if (clip != null && clip.name.IndexOf(
                    RoseScreamingClipName,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                float roseDistance = UnityEngine.Random.Range(
                    RoseScreamingMinimumFeet,
                    RoseScreamingMaximumFeet
                ) * FeetToMeters;
                Vector3 roseDirection = new Vector3(
                    Mathf.Cos(angle),
                    UnityEngine.Random.Range(-0.12f, 0.12f),
                    Mathf.Sin(angle)
                ).normalized;

                return playerCamera.transform.position + roseDirection * roseDistance;
            }

            bool isGirlLaughing = clip != null && clip.name.IndexOf(
                GirlLaughingClipName,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
            float minimumDistanceFeet = isGirlLaughing
                ? GirlLaughingMinimumFeet
                : MinimumSpatialAudioDistanceFeet;
            float maximumDistanceFeet = isGirlLaughing
                ? GirlLaughingMaximumFeet
                : MaximumSpatialAudioDistanceFeet;
            float distance = UnityEngine.Random.Range(
                minimumDistanceFeet,
                maximumDistanceFeet
            ) * FeetToMeters;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                UnityEngine.Random.Range(-0.12f, 0.12f),
                Mathf.Sin(angle)
            ).normalized;

            return playerCamera.transform.position + direction * distance;
        }
    }
}
