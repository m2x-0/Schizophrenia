using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using MelonLoader;
using UnityEngine;

namespace Schizophrenia.Severitys
{
    internal static class fightprotector
    {
        private static HarmonyLib.Harmony harmony;
        private static bool active;
        private static bool installAttempted;
        private static bool installed;
        private static float nextFeedbackTime;

        public static void Initialize(HarmonyLib.Harmony harmonyInstance)
        {
            harmony = harmonyInstance;
        }

        public static bool SetActive(bool enabled)
        {
            if (!enabled)
            {
                active = false;
                nextFeedbackTime = 0f;
                return true;
            }

            if (!installed && !TryInstall())
                return false;

            active = enabled;
            nextFeedbackTime = 0f;
            return true;
        }

        private static bool TryInstall()
        {
            if (installAttempted)
                return installed;

            installAttempted = true;
            if (harmony == null)
            {
                return false;
            }

            try
            {
                PatchPrefix(
                    typeof(Player_Health),
                    nameof(Player_Health.OnReceivedDamage),
                    nameof(OnPlayerReceivedDamage)
                );
                PatchPrefix(
                    typeof(Player_Health),
                    nameof(Player_Health.TAKEDAMAGE),
                    nameof(OnPlayerTakeDamage)
                );
                PatchPrefix(
                    typeof(Player_Health),
                    nameof(Player_Health.ApplyKillDamage),
                    nameof(OnPlayerInstantKillDamage)
                );
                PatchPrefix(
                    typeof(Player_Health),
                    nameof(Player_Health.Dying),
                    nameof(OnPlayerDying)
                );
                PatchPrefix(
                    typeof(Player_Health),
                    nameof(Player_Health.Death),
                    nameof(OnPlayerDeath)
                );
                installed = true;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static void PatchPrefix(System.Type targetType, string targetName, string prefixName)
        {
            var target = AccessTools.Method(targetType, targetName);
            if (target == null)
                throw new System.MissingMethodException(targetType.FullName, targetName);

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(
                    typeof(fightprotector),
                    prefixName
                )
            );
        }

        private static bool OnPlayerReceivedDamage(
            Player_Health __instance,
            Attack attack,
            PlayerDamageReceiver.BodyPart part
        )
        {
            return !SuppressPlayerDamage(__instance, attack.damage);
        }

        private static bool OnPlayerTakeDamage(Player_Health __instance, float damage)
        {
            return !SuppressPlayerDamage(__instance, damage);
        }

        private static bool OnPlayerInstantKillDamage(Player_Health __instance)
        {
            return !SuppressPlayerDamage(__instance, 1f);
        }

        private static bool OnPlayerDying(Player_Health __instance, float damage)
        {
            return !SuppressPlayerDamage(__instance, damage);
        }

        private static bool OnPlayerDeath(Player_Health __instance)
        {
            return !SuppressPlayerDamage(__instance, 1f);
        }

        private static bool SuppressPlayerDamage(Health health, float damage)
        {
            if (!active || health == null)
                return false;

            RigManager playerRig = Player.RigManager;
            if (playerRig == null || playerRig.health != health)
                return false;

            if (Time.unscaledTime < nextFeedbackTime)
                return true;

            nextFeedbackTime = Time.unscaledTime + 0.3f;
            PlayFakeHitFeedback(health, damage);
            return true;
        }

        private static void PlayFakeHitFeedback(Health health, float damage)
        {
            try
            {
                Player_Health playerHealth = health as Player_Health;
                if (playerHealth != null)
                {
                    playerHealth.StartHealthRegnerationAndVignette(
                        Mathf.Max(0.1f, damage)
                    );
                }
            }
            catch (System.Exception)
            {
            }

            try
            {
                HeadSFX headSfx = health._headSfx;
                if (headSfx != null)
                {
                    if (damage >= 0.65f)
                        headSfx.BigDamageVocal();
                    else
                        headSfx.SmallDamageVocal(Mathf.Clamp01(damage));
                }
            }
            catch (System.Exception)
            {
            }

            try
            {
                float hapticStrength = Mathf.Clamp(
                    0.3f + damage * 0.5f,
                    0.3f,
                    0.8f
                );
                Player.LeftController?.Haptic(hapticStrength);
                Player.RightController?.Haptic(hapticStrength);
            }
            catch (System.Exception)
            {
            }
        }
    }
}
