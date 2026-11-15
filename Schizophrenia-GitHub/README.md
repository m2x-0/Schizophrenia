# Schizophrenia Features

- Delusions
- Paranoia
- Visual Distortions
- Auditory Hallucinations
- Visual Hallucinations
- Medication System
- Interactive Hallucinations

## Information

This mod is meant to simulate Schizophrenia as described by the DSM-V-TR however does not fully or accurately replicate it.

This mod is also meant to be played in the story mode, however it works really anywhere to be honest but this is an alpha release, more dynamic features and story implementation will be added in the future releases.

This mod is very early in development, I probably shouldn't even really be releasing it right now but I've been working on it for like a month and trying to just get a basic alpha release working and now it does so I'm releasing it now.

Once you have enabled the mod, there will be a randomized time between 5-15 minutes of normalcy.

However as time goes on without you taking your prescribed medication, you will begin to first experience symptoms of psychosis. If you continue to let this escalate, your game will progressively be plagued by visual hallucinations, auditory hallucinations, and extreme paranoia. However to simulate realism, psychosis untreated will fluctuate in intensity over time.

## Additional Information

**BIPOLAR DISORDER COMING SOON!**

I'm working on it however it will probably be a while seeing as I'm working on other projects than just Schizophrenia/Bipolar.

Another note is that although I do have some experience in modding/C# in general, AI was used as an assistant therefore I am giving it an AI generated tag just to be safe.

I wanted to try and make a good interactive horror mod with AI that wasn't slop like the rest of thunderstore nowadays is unfortunately.

However, I am a very new dev, so mistakes are expected.

I would request that if you have any questions or issues/bugs with the mod that you would contact me on discord, my username is: **m2x_0**

I'm also on the Bonelab discord server often so you can also just ping me there.

## Installation Guide

Once you have downloaded the .zip file from thunderstore, take the .zip file and extract it.

Once you have extracted it, go into the new extracted file and you will see 2 things, Schizophrenia.dll, and a folder named "Schizophrenia" Once you see both, take both of those files and drag them into your Bonelab code mods folder.

Just for verification, the inside of the Schizophrenia folder that you are dragging into your code mods folder should have these in there:

- Audio
- Audio1
- Audio2
- Audio3
- Audio4
- Audio5
- entities
- entities1
- flash
- ost
- radio

(Don't worry, there shouldn't be anything in ost or radio yet that is for future releases)
## Building from source

Open schizophrenia.slnx in Visual Studio with the .NET Framework 4.7.2 targeting pack installed. The project references DLLs from a local BONELAB install with MelonLoader, BoneLib, and AudioImportLib. Set the MSBuild property BonelabPath if your game is installed outside the default Steam location. The runtime audio and image assets are available in the Thunderstore download. To include them in local build output, copy the contents of the package's Schizophrenia folder into schizophrenia/Assets.

[Download Schizophrenia on Thunderstore](https://thunderstore.io/c/bonelab/p/matrix_mods/Schizophrenia/)
