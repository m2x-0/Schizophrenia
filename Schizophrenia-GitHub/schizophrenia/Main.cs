using MelonLoader;
using BoneLib.BoneMenu;
using BoneLib.Notifications;
using Il2CppSLZ.Marrow.Audio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Schizophrenia.Severitys;

[assembly: MelonInfo(
    typeof(Schizophrenia.SchizophreniaMod),
    "Schizophrenia",
    "0.1.1",
    "matrix"
)]

[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace Schizophrenia
{
    public class SchizophreniaMod : MelonMod
    {
        private Page schizophreniaPage;
        private Page spatialSoundOptionsPage;
        private Page headSoundOptionsPage;
        private Page entitySoundOptionsPage;
        private Page medicationSoundOptionsPage;
        private Page severity3HeadSoundOptionsPage;
        private Page severity3DisappearSoundOptionsPage;
        private Page entityImageOptionsPage;
        private Page severity3EntityImageOptionsPage;
        private readonly HashSet<string> registeredSoundOptions =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> registeredEntityImageOptions =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> registeredSeverity3EntityImageOptions =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // master switch
        private bool schizophreniaEnabled = false;

        private int currentSeverity = 0;
        private float currentSeverityEndTime;
        private bool severity1AudioEnabled = true;
        private bool severityDelusionsEnabled = true;
        private bool gameMusicMuteEnabled = true;
        private bool medicationSoundEnabled = true;

        private const float MinimumNormalcyDurationSeconds = 300f;
        private const float MaximumNormalcyDurationSeconds = 900f;
        private const float MinimumSeverityDurationSeconds = 300f;
        private const float MaximumSeverityDurationSeconds = 600f;

        // put delusions here also fuck you to whoever is reading this in IL spy quit tryna steal my shit
        private static readonly string[] Delusions =
        {
            "The object you touched made you unclean before the Holy Lamb of God",
            "They are coming for you now",
            "Demons are possessing you",
            "They are all watching you",
            "The chip the CIA put inside your brain is making this happen",
            "They live inside the walls around you",
            "They all can see you",
            "Run before they catch you for eternity",
            "Your family is conspiring against you",
            "Blood is needed for your true repentance",
            "Medication only makes it worse",
            "Don't stand still, they can see you that way",
            "Breathe quiet, they can hear you",
            "Beings are here",
            "Never close your eyes, they move when you don't look"
        };

        private bool timerRunning = false;

        private int timerVersion = 0;
        private int previousDelusionIndex = -1;
        private float pendingSeverityTimeAdjustmentSeconds;
        private BoolElement schizophreniaToggleElement;
        private bool originalMusicVolumeCaptured;
        private bool musicMuteFailureLogged;
        private float originalMusicVolume;

        private severities1 severities1;
        private severities2 severities2;
        private severities3 severities3;

        public override void OnInitializeMelon()
        {
            severities1 = new severities1();
            severities2 = new severities2();
            severities3 = new severities3(severities2);
            fightprotector.Initialize(HarmonyInstance);
            severities3.HeadAudioClipLoaded += AddSeverity3SpecificSoundOption;
            severities3.DisappearAudioClipLoaded += AddSeverity3DisappearSoundOption;
            severities3.EntityImageLoaded += AddSeverity3EntityImageOption;
            CreateBoneMenu();
            BoneLib.Hooking.OnLevelUnloaded += StopSeverity3FakeFight;
            severities2.LoadAudioFiles();
            severities3.LoadAudioFiles();
            severities3.LoadEntityEventAudioFiles();

            if (schizophreniaEnabled)
            {
                StartSeverityCycle();
            }
        }

        public override void OnUpdate()
        {
            audioplaceratio.Update();
            SchizophreniaAudio.Update();

            if (schizophreniaEnabled && gameMusicMuteEnabled)
                KeepGameMusicMuted();

            severities2?.Update();
            severities3?.Update();
        }

        public override void OnDeinitializeMelon()
        {
            BoneLib.Hooking.OnLevelUnloaded -= StopSeverity3FakeFight;
            severities3?.StopFakeFightNow();
            RestoreGameMusicVolume();
        }

        private void StopSeverity3FakeFight()
        {
            severities3?.StopFakeFightNow();
        }
        // here comes the pain
        private void CreateBoneMenu()
        {
            string rainbowTitle =
                "<color=#FF0000>S</color>" +
                "<color=#FF7F00>c</color>" +
                "<color=#FFFF00>h</color>" +
                "<color=#00FF00>i</color>" +
                "<color=#00FFFF>z</color>" +
                "<color=#4DB8FF>o</color>" +
                "<color=#8B00FF>p</color>" +
                "<color=#FF0000>h</color>" +
                "<color=#FF7F00>r</color>" +
                "<color=#FFFF00>e</color>" +
                "<color=#00FF00>n</color>" +
                "<color=#00FFFF>i</color>" +
                "<color=#8B00FF>a</color>";

            schizophreniaPage = Page.Root.CreatePage(
                rainbowTitle,
                Color.white
            );

            schizophreniaToggleElement = schizophreniaPage.CreateBool(
                "Schizophrenia",
                Color.white,
                schizophreniaEnabled,
                ToggleSchizophrenia
            );

            schizophreniaPage.CreateFunction(
                "Take Your Meds",
                Color.white,
                TakeYourMeds
            );

            Page developerOptionsPage = schizophreniaPage.CreatePage(
                "Developer Options",
                Color.white
            );

            Page severityControlsPage = developerOptionsPage.CreatePage(
                "Severity Controls",
                Color.white
            );
            severityControlsPage.CreateFunction(
                "Start Severity 1 Now",
                Color.white,
                () => StartDevelopmentSeverity(1)
            );
            severityControlsPage.CreateFunction(
                "Start Severity 2 Now",
                Color.white,
                () => StartDevelopmentSeverity(2)
            );
            severityControlsPage.CreateFunction(
                "Start Severity 3 Now",
                Color.white,
                () => StartDevelopmentSeverity(3)
            );
            severityControlsPage.CreateFunction(
                "Return to Normal",
                Color.white,
                RestartNormalCycleNow
            );
            severityControlsPage.CreateFunction(
                "Advance Severity Timer 30 Seconds",
                Color.white,
                () => ShiftCurrentSeverityTimer(-30f)
            );
            severityControlsPage.CreateFunction(
                "Rewind Severity Timer 30 Seconds",
                Color.white,
                () => ShiftCurrentSeverityTimer(30f)
            );

            Page globalDeveloperPage = developerOptionsPage.CreatePage(
                "Global Effects",
                Color.white
            );
            globalDeveloperPage.CreateBool(
                "Severity Delusion Notifications",
                Color.white,
                severityDelusionsEnabled,
                SetSeverityDelusionsEnabled
            );
            globalDeveloperPage.CreateBool(
                "Mute Game OST While Mod Is On",
                Color.white,
                gameMusicMuteEnabled,
                SetGameMusicMuteEnabled
            );
            globalDeveloperPage.CreateBool(
                "Audio4 on Take Your Meds",
                Color.white,
                medicationSoundEnabled,
                SetMedicationSoundEnabled
            );
            globalDeveloperPage.CreateFunction(
                "Send Random Delusion Now",
                Color.white,
                () => ShowSeverityDelusion(currentSeverity > 0 ? currentSeverity : 1, true)
            );
            globalDeveloperPage.CreateFunction(
                "Play Random Audio4 Medication Sound Now",
                Color.white,
                () => severities2.PlayMedicationSound()
            );
            globalDeveloperPage.CreateFunction(
                "Fade Out All Mod Audio Now",
                Color.white,
                () => SchizophreniaAudio.FadeOutCurrent()
            );
            globalDeveloperPage.CreateFunction(
                "Clear All Severity Entities Now",
                Color.white,
                ClearAllSeverityEntities
            );

            Page severity1DeveloperPage = developerOptionsPage.CreatePage(
                "Severity 1 Features",
                Color.white
            );
            severity1DeveloperPage.CreateBool(
                "Automatic NPC Audio",
                Color.white,
                severity1AudioEnabled,
                ToggleSeverity1Audio
            );
            severity1DeveloperPage.CreateFunction(
                "Play Random NPC Sound Now",
                Color.white,
                () => severities1.PlayRandomNpcSoundNow()
            );
            severity1DeveloperPage.CreateFunction(
                "Play Ford Big Pain Now",
                Color.white,
                () => severities1.PlayFordBigPainNow()
            );

            Page severity2DeveloperPage = developerOptionsPage.CreatePage(
                "Severity 2 Features",
                Color.white
            );
            severity2DeveloperPage.CreateBool(
                "Contrast / Saturation",
                Color.white,
                true,
                severities2.SetVisualsEnabled
            );
            severity2DeveloperPage.CreateBool(
                "Radial Menu: Error",
                Color.white,
                true,
                severities2.SetRadialMenuErrorEnabled
            );
            severity2DeveloperPage.CreateBool(
                "NO ESCAPE Clipboards",
                Color.white,
                true,
                severities2.SetClipboardMessageEnabled
            );
            severity2DeveloperPage.CreateBool(
                "Audio (Around You)",
                Color.white,
                true,
                severities2.SetSpatialAudioEnabled
            );
            severity2DeveloperPage.CreateBool(
                "Audio1 (Inside Your Head)",
                Color.white,
                true,
                severities2.SetHeadAudioEnabled
            );
            severity2DeveloperPage.CreateBool(
                "Severity 2 Entity Images",
                Color.white,
                true,
                severities2.SetEntityImagesEnabled
            );
            severity2DeveloperPage.CreateBool(
                "Audio2 on Entity Disappear",
                Color.white,
                true,
                severities2.SetEntityAudioEnabled
            );
            severity2DeveloperPage.CreateFunction(
                "Play Random Audio Sound Now",
                Color.white,
                () => severities2.PlayRandomSpatialSoundNow()
            );
            severity2DeveloperPage.CreateFunction(
                "Play Random Audio1 Sound Now",
                Color.white,
                () => severities2.PlayRandomHeadSoundNow()
            );
            severity2DeveloperPage.CreateFunction(
                "Play Random Audio2 Sound Now",
                Color.white,
                () => severities2.PlayRandomEntitySoundNow()
            );
            severity2DeveloperPage.CreateFunction(
                "Spawn Severity 2 Entity Now",
                Color.white,
                () => severities2.SpawnEntityImageForDeveloper()
            );
            severity2DeveloperPage.CreateFunction(
                "Apply NO ESCAPE to Clipboards Now",
                Color.white,
                () => severities2.ApplyClipboardMessageNow()
            );

            Page severity3DeveloperPage = developerOptionsPage.CreatePage(
                "Severity 3 Features",
                Color.white
            );
            severity3DeveloperPage.CreateBool(
                "Contrast / Saturation / Red Tint",
                Color.white,
                true,
                severities3.SetVisualsEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Radial Menu: NO ESCAPE FOR YOU",
                Color.white,
                true,
                severities3.SetRadialMenuWordsEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Continuous Audio5 In-Head",
                Color.white,
                true,
                severities3.SetHeadAudioEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Severity 3 Entity Waves",
                Color.white,
                true,
                severities3.SetEntitySpawnsEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Automatic NPC Waves (30-60 sec)",
                Color.white,
                true,
                severities3.SetNpcWavesEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Audio3 Disappear Sounds",
                Color.white,
                true,
                severities3.SetDisappearSoundsEnabled
            );
            severity3DeveloperPage.CreateBool(
                "Automatic Flash Images (5-15 sec)",
                Color.white,
                true,
                severities3.SetFlashImagesEnabled
            );
            severity3DeveloperPage.CreateFunction(
                "Flash Menu Phrase Now",
                Color.white,
                () => severities3.FlashMenuPhraseNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Play Random Audio5 Sound Now",
                Color.white,
                () => severities3.PlayRandomHeadSoundNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Spawn Severity 3 Entity Wave Now",
                Color.white,
                () => severities3.SpawnEntityWaveNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Play Random Audio3 Sound Now",
                Color.white,
                () => severities3.PlayRandomDisappearSoundNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Flash Random Image Now",
                Color.white,
                () => severities3.FlashRandomImageNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Start NPC Wave Now",
                Color.white,
                () => severities3.StartFakeFightNow()
            );
            severity3DeveloperPage.CreateFunction(
                "Stop / Clear NPC Wave Now",
                Color.white,
                () => severities3.StopFakeFightNow()
            );

            Page initiateSpecificEntityPage = developerOptionsPage.CreatePage(
                "Initiate Specific Entity",
                Color.white
            );
            entityImageOptionsPage = initiateSpecificEntityPage.CreatePage(
                "Severity 2 PNGs (entities)",
                Color.white
            );
            entityImageOptionsPage.CreateFunction(
                "Load / Refresh Severity 2 Entity List",
                Color.white,
                () => severities2.LoadEntityImageOptions()
            );
            severity3EntityImageOptionsPage = initiateSpecificEntityPage.CreatePage(
                "Severity 3 PNGs (entities1)",
                Color.white
            );
            severity3EntityImageOptionsPage.CreateFunction(
                "Load / Refresh Severity 3 Entity List",
                Color.white,
                () => severities3.LoadEntityImageOptions()
            );

            Page initiateSpecificSoundPage = developerOptionsPage.CreatePage(
                "Initiate Specific Sound",
                Color.white
            );
            initiateSpecificSoundPage.CreateFunction(
                "Load / Refresh All Sound Lists",
                Color.white,
                RefreshSpecificSoundList
            );
            spatialSoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio (Around You)",
                Color.white
            );
            headSoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio1 (Inside Your Head)",
                Color.white
            );
            entitySoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio2 (Severity 2 Entity Sounds)",
                Color.white
            );
            medicationSoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio4 (Medication UI Sounds)",
                Color.white
            );
            severity3DisappearSoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio3 (Severity 3 Disappear Sounds)",
                Color.white
            );
            severity3HeadSoundOptionsPage = initiateSpecificSoundPage.CreatePage(
                "Audio5 (Severity 3 In-Head)",
                Color.white
            );
            severities2.AudioClipLoaded += AddSpecificSoundOption;
            severities2.EntityAudioClipLoaded += AddEntitySoundOption;
            severities2.MedicationAudioClipLoaded += AddMedicationSoundOption;
            severities2.EntityImageLoaded += AddEntityImageOption;
            severities2.LoadEntityImageOptions();
            severities3.LoadEntityImageOptions();

        }

        private void AddSpecificSoundOption(AudioClip clip, bool playInsideHead)
        {
            if (clip == null)
                return;

            string optionKey = (playInsideHead ? "Audio1|" : "Audio|") + clip.name;
            if (!registeredSoundOptions.Add(optionKey))
                return;

            Page targetPage = playInsideHead
                ? headSoundOptionsPage
                : spatialSoundOptionsPage;
            if (targetPage == null)
                return;

            targetPage.CreateFunction(
                clip.name,
                Color.white,
                () => severities2.PlaySpecificSound(clip, playInsideHead)
            );
        }

        private void AddEntitySoundOption(AudioClip clip)
        {
            if (clip == null || entitySoundOptionsPage == null)
                return;

            string optionKey = "Audio2|" + clip.name;
            if (!registeredSoundOptions.Add(optionKey))
                return;

            entitySoundOptionsPage.CreateFunction(
                clip.name,
                Color.white,
                () => severities2.PlaySpecificEntitySound(clip)
            );
        }

        private void AddMedicationSoundOption(AudioClip clip)
        {
            if (clip == null || medicationSoundOptionsPage == null)
                return;

            string optionKey = "Audio4|" + clip.name;
            if (!registeredSoundOptions.Add(optionKey))
                return;

            medicationSoundOptionsPage.CreateFunction(
                clip.name,
                Color.white,
                () => severities2.PlaySpecificMedicationSound(clip)
            );
        }

        private void AddSeverity3SpecificSoundOption(AudioClip clip)
        {
            if (clip == null || severity3HeadSoundOptionsPage == null)
                return;

            string optionKey = "Audio5|" + clip.name;
            if (!registeredSoundOptions.Add(optionKey))
                return;

            severity3HeadSoundOptionsPage.CreateFunction(
                clip.name,
                Color.white,
                () => severities3.PlaySpecificHeadSound(clip)
            );
        }

        private void AddSeverity3DisappearSoundOption(AudioClip clip)
        {
            if (clip == null || severity3DisappearSoundOptionsPage == null)
                return;

            string optionKey = "Audio3|" + clip.name;
            if (!registeredSoundOptions.Add(optionKey))
                return;

            severity3DisappearSoundOptionsPage.CreateFunction(
                clip.name,
                Color.white,
                () => severities3.PlaySpecificDisappearSoundNow(clip)
            );
        }

        private void RefreshSpecificSoundList()
        {
            severities2.LoadAudioFiles();
            severities3.LoadAudioFiles();
            severities3.LoadEntityEventAudioFiles();
        }

        private void AddEntityImageOption(string imageName, string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || entityImageOptionsPage == null)
                return;

            if (!registeredEntityImageOptions.Add(imagePath))
                return;

            string optionName = string.IsNullOrEmpty(imageName)
                ? System.IO.Path.GetFileNameWithoutExtension(imagePath)
                : imageName;
            string selectedImagePath = imagePath;
            entityImageOptionsPage.CreateFunction(
                optionName,
                Color.white,
                () => severities2.SpawnSpecificEntityImageForDeveloper(selectedImagePath)
            );
        }

        private void AddSeverity3EntityImageOption(string imageName, string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || severity3EntityImageOptionsPage == null)
                return;

            if (!registeredSeverity3EntityImageOptions.Add(imagePath))
                return;

            string optionName = string.IsNullOrEmpty(imageName)
                ? System.IO.Path.GetFileNameWithoutExtension(imagePath)
                : imageName;
            string selectedImagePath = imagePath;
            severity3EntityImageOptionsPage.CreateFunction(
                optionName,
                Color.white,
                () => severities3.SpawnSpecificEntityImageNow(selectedImagePath)
            );
        }

        private void ToggleSeverity1Audio(bool enabled)
        {
            severity1AudioEnabled = enabled;
            if (currentSeverity != 1)
                return;

            if (enabled)
                severities1?.Start(GetCurrentSeveritySecondsRemaining());
            else
                severities1?.Stop();
        }

        private void SetSeverityDelusionsEnabled(bool enabled)
        {
            severityDelusionsEnabled = enabled;
        }

        private void SetGameMusicMuteEnabled(bool enabled)
        {
            gameMusicMuteEnabled = enabled;
            if (enabled && schizophreniaEnabled)
                KeepGameMusicMuted();
            else
                RestoreGameMusicVolume();

        }

        private void SetMedicationSoundEnabled(bool enabled)
        {
            medicationSoundEnabled = enabled;
        }

        private void RestartNormalCycleNow()
        {
            timerVersion++;
            pendingSeverityTimeAdjustmentSeconds = 0f;
            ResetToNormal();
            if (schizophreniaEnabled)
                StartSeverityCycle();
        }

        private void ClearAllSeverityEntities()
        {
            severities2?.DismissEntityForMedication();
            severities3?.ClearEntityImagesNow();
        }

        private void ToggleSchizophrenia(bool enabled)
        {
            schizophreniaEnabled = enabled;

            timerVersion++;

            if (schizophreniaEnabled)
            {
                if (gameMusicMuteEnabled)
                    KeepGameMusicMuted();

                ResetToNormal();
                StartSeverityCycle();
            }
            else
            {
                severities3?.StopFakeFightNow();
                RestoreGameMusicVolume();

                severities2?.DismissEntityForMedication();
                SchizophreniaAudio.FadeOutCurrent();

                ResetToNormal();
            }
        }

        private void KeepGameMusicMuted()
        {
            try
            {
                if (!originalMusicVolumeCaptured)
                {
                    originalMusicVolume = Audio3dManager.audio_Music;
                    originalMusicVolumeCaptured = true;
                }

                if (Audio3dManager.audio_Music != 0)
                    BoneLib.Audio.SetMusicVolume(0);
            }
            catch (System.Exception)
            {
                if (!musicMuteFailureLogged)
                {
                    musicMuteFailureLogged = true;
                }
            }
        }

        private void RestoreGameMusicVolume()
        {
            if (!originalMusicVolumeCaptured)
                return;

            try
            {
                Audio3dManager.audio_Music = originalMusicVolume;
            }
            catch (System.Exception)
            {
            }
            finally
            {
                originalMusicVolumeCaptured = false;
            }
        }

        private void StartSeverityCycle()
        {
            if (timerRunning)
                return;

            timerRunning = true;
            int thisTimerVersion = timerVersion;
            MelonCoroutines.Start(SeverityCycle(thisTimerVersion));
        }

        private IEnumerator SeverityCycle(int thisTimerVersion)
        {
            float normalcyDuration = RandomNormalcyDuration();
            yield return WaitForDuration(
                normalcyDuration,
                thisTimerVersion,
                "Severity 1"
            );

            if (!IsCurrentCycle(thisTimerVersion))
                yield break;

            float currentSeverityDuration = RandomSeverityDuration();
            SetSeverity(1, currentSeverityDuration);
            int nextSeverity = 2;
            bool randomFluctuationStarted = false;

            // goes through severity 1-3 before cycling through random ones without resolve
            while (true)
            {
                yield return WaitForDuration(
                    currentSeverityDuration,
                    thisTimerVersion,
                    $"Severity {nextSeverity}"
                );

                if (!IsCurrentCycle(thisTimerVersion))
                    yield break;

                int enteredSeverity = nextSeverity;
                currentSeverityDuration = RandomSeverityDuration();
                SetSeverity(enteredSeverity, currentSeverityDuration);

                if (!randomFluctuationStarted && enteredSeverity < 3)
                {
                    nextSeverity = enteredSeverity + 1;
                }
                else
                {
                    randomFluctuationStarted = true;
                    nextSeverity = RandomDifferentSeverity(enteredSeverity);
                }
            }
        }

        private IEnumerator WaitForDuration(
            float duration,
            int thisTimerVersion,
            string targetSeverity
        )
        {
            float remaining = duration;

            if (!string.IsNullOrEmpty(targetSeverity))
            {
                MelonLogger.Msg(
                    $"[Schizophrenia][Timer] {targetSeverity} in {FormatCountdown(remaining)}."
                );
            }

            while (remaining > 0f && IsCurrentCycle(thisTimerVersion))
            {
                float waitDuration = Mathf.Min(1f, remaining);
                yield return new WaitForSeconds(waitDuration);
                remaining -= waitDuration;

                if (currentSeverity > 0 && pendingSeverityTimeAdjustmentSeconds != 0f)
                {
                    float adjustmentSeconds = pendingSeverityTimeAdjustmentSeconds;
                    if (currentSeverity == 1)
                        severities1?.AdjustSeverityDuration(adjustmentSeconds);

                    remaining = Mathf.Max(
                        0f,
                        remaining + adjustmentSeconds
                    );
                    pendingSeverityTimeAdjustmentSeconds = 0f;
                    MelonLogger.Msg(
                        "[Schizophrenia][Timer] Developer adjustment applied; " +
                        $"{targetSeverity} in {FormatCountdown(remaining)}."
                    );
                }

                if (!string.IsNullOrEmpty(targetSeverity) && remaining > 0f)
                {
                    MelonLogger.Msg(
                        $"[Schizophrenia][Timer] {targetSeverity} in {FormatCountdown(remaining)}."
                    );
                }
            }
        }

        private string FormatCountdown(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private bool IsCurrentCycle(int thisTimerVersion)
        {
            return thisTimerVersion == timerVersion && schizophreniaEnabled;
        }

        private float RandomSeverityDuration()
        {
            return Random.Range(
                MinimumSeverityDurationSeconds,
                MaximumSeverityDurationSeconds
            );
        }

        private float RandomNormalcyDuration()
        {
            return Random.Range(
                MinimumNormalcyDurationSeconds,
                MaximumNormalcyDurationSeconds
            );
        }

        private int RandomDifferentSeverity(int severity)
        {
            int severityOffset = Random.Range(1, 3);
            return ((severity - 1 + severityOffset) % 3) + 1;
        }

        private void SetSeverity(int severity, float severityDurationSeconds = 0f)
        {
            int previousSeverity = currentSeverity;

            if (previousSeverity == 1 && severity != 1)
                severities1?.Stop();
            if (previousSeverity == 2 && severity != 2)
                severities2?.Stop();
            if (previousSeverity == 3 && severity != 3)
                severities3?.Stop();

            currentSeverity = severity;
            currentSeverityEndTime = Time.time + Mathf.Max(0f, severityDurationSeconds);

            if (currentSeverity > 0)
            {
                ShowSeverityDelusion(currentSeverity);
            }

            if (previousSeverity != 1 && currentSeverity == 1 && severity1AudioEnabled)
                severities1?.Start(severityDurationSeconds);
            if (previousSeverity != 2 && currentSeverity == 2)
                severities2?.Start();
            if (previousSeverity != 3 && currentSeverity == 3)
                severities3?.Start();
        }

        private void ShowSeverityDelusion(int severity, bool force = false)
        {
            if ((!severityDelusionsEnabled && !force) || Delusions.Length == 0)
                return;

            int delusionIndex = Random.Range(0, Delusions.Length);
            if (Delusions.Length > 1 && delusionIndex == previousDelusionIndex)
                delusionIndex = (delusionIndex + Random.Range(1, Delusions.Length)) % Delusions.Length;

            previousDelusionIndex = delusionIndex;

            try
            {
                Notifier.Send(new Notification
                {
                    Title = "Schizophrenia:",
                    Message = Delusions[delusionIndex],
                    ShowTitleOnPopup = true,
                    PopupLength = 6f,
                    Type = NotificationType.Information
                });

            }
            catch (System.Exception)
            {
            }
        }

        private void TakeYourMeds()
        {
            severities3?.StopFakeFightNow();
            severities2?.DismissEntityForMedication();

            if (!schizophreniaEnabled)
            {
                SchizophreniaAudio.FadeOutCurrent();
                if (medicationSoundEnabled)
                    severities2?.PlayMedicationSound();
                return;
            }


            // i keep having to invalidate the last timer because both wont stop running at the same time
            timerVersion++;
            pendingSeverityTimeAdjustmentSeconds = 0f;

            ResetToNormal();
            StartSeverityCycle();
            SchizophreniaAudio.FadeOutCurrent();
            if (medicationSoundEnabled)
                severities2?.PlayMedicationSound();
        }

        private void ResetToNormal()
        {
            SetSeverity(0);

            timerRunning = false;
            pendingSeverityTimeAdjustmentSeconds = 0f;

        }

        private void StartDevelopmentSeverity(int severity)
        {
            if (severity < 1 || severity > 3)
                return;

            if (!schizophreniaEnabled)
            {
                if (schizophreniaToggleElement != null)
                    schizophreniaToggleElement.Value = true;

                if (!schizophreniaEnabled)
                    ToggleSchizophrenia(true);
            }

            timerVersion++;
            timerRunning = true;
            pendingSeverityTimeAdjustmentSeconds = 0f;

            if (currentSeverity == 1)
                severities1?.Stop();
            else if (currentSeverity == 2)
                severities2?.Stop();
            else if (currentSeverity == 3)
                severities3?.Stop();
            currentSeverity = 0;

            float currentSeverityDuration = RandomSeverityDuration();
            SetSeverity(severity, currentSeverityDuration);
            MelonLogger.Msg(
                $"[Schizophrenia][Developer] Started Severity {severity} directly for " +
                $"{FormatCountdown(currentSeverityDuration)}."
            );

            int nextSeverity = severity < 3 ? severity + 1 : RandomDifferentSeverity(severity);
            bool randomFluctuationStarted = severity >= 3;
            MelonCoroutines.Start(
                DevelopmentSeverityCycle(
                    timerVersion,
                    currentSeverityDuration,
                    nextSeverity,
                    randomFluctuationStarted
                )
            );
        }

        private IEnumerator DevelopmentSeverityCycle(
            int thisTimerVersion,
            float currentSeverityDuration,
            int nextSeverity,
            bool randomFluctuationStarted
        )
        {
            while (IsCurrentCycle(thisTimerVersion))
            {
                yield return WaitForDuration(
                    currentSeverityDuration,
                    thisTimerVersion,
                    $"Severity {nextSeverity}"
                );

                if (!IsCurrentCycle(thisTimerVersion))
                    yield break;

                int enteredSeverity = nextSeverity;
                currentSeverityDuration = RandomSeverityDuration();
                SetSeverity(enteredSeverity, currentSeverityDuration);

                if (!randomFluctuationStarted && enteredSeverity < 3)
                {
                    nextSeverity = enteredSeverity + 1;
                }
                else
                {
                    randomFluctuationStarted = true;
                    nextSeverity = RandomDifferentSeverity(enteredSeverity);
                }
            }
        }

        private void ShiftCurrentSeverityTimer(float adjustmentSeconds)
        {
            if (!timerRunning || currentSeverity == 0)
            {
                return;
            }

            pendingSeverityTimeAdjustmentSeconds += adjustmentSeconds;
            currentSeverityEndTime -= adjustmentSeconds;
            string action = adjustmentSeconds < 0f
                ? "Advancing"
                : "Rewinding";
        }

        private float GetCurrentSeveritySecondsRemaining()
        {
            return Mathf.Max(1f, currentSeverityEndTime - Time.time);
        }
    }
}
