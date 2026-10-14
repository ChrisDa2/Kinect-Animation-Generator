# Kinect Animation Generator

> Record full-body motion with a Kinect v1 sensor and Unity, export it as FBX animation clips ready for any Humanoid rig.

![Unity](https://img.shields.io/badge/Unity-6000.3.13f1-000000?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)
![Kinect](https://img.shields.io/badge/Kinect-for%20Windows%20v1-00A4EF)
![License](https://img.shields.io/badge/License-MIT-green)

A Unity-based pipeline for capturing skeleton motion from a **Kinect for Windows v1** sensor and baking it into **FBX animation clips** compatible with Mixamo / Humanoid rigs. The project includes everything from the recording scene to the FBX exporter, so a full-body motion capture and animation export can be done from inside the Unity Editor.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Project Structure](#project-structure)
- [Usage](#usage)
  - [Recording an Animation](#recording-an-animation)
  - [Baking the Animation to FBX](#baking-the-animation-to-fbx)
  - [Previewing a Recording](#previewing-a-recording)
- [Notes and Limitations](#notes-and-limitations)
- [Credits](#credits)
- [License](#license)

---

## Overview

This project provides an end-to-end workflow for **low-cost motion capture** using a first-generation Kinect sensor:

1. **Capture** — A dedicated Unity scene streams the Kinect skeleton onto a live avatar and records per-frame data (PNG images + JSON).
2. **Bake** — An Editor window converts the recorded JSON into a Humanoid **animation clip** and exports it as an FBX file.
3. **Play** — The exported FBX can be dragged into any scene and used on any Humanoid rig, including characters from Mixamo.

The result is a pipeline similar in spirit to commercial retargeting tools, but built around the Kinect v1 hardware and Unity's native Humanoid retargeting system.

---

## Features

- **Live Kinect v1 tracking** — Real-time skeleton tracking with an on-screen preview.
- **Avatar retargeting** — The recorded pose drives a Humanoid avatar in real time (Ninja from Mixamo included as reference).
- **Structured recording output** — Joint positions, rotations, tracking state, and reference RGB / depth images are stored per frame in JSON.
- **Custom FBX exporter** — Bakes the recorded data into a loopable Humanoid animation clip and exports it directly to `.fbx`.
- **Two recording scenes** — One mirrored (character faces the camera) and one non-mirrored (character faces the same direction as the camera), covering the two common conventions.
- **Loop-friendly** — Exported clips are configured for looping and correct root-motion baking.

---

## System Requirements

| | |
|---|---|
| **OS** | Windows 10 / 11 (64-bit) |
| **Sensor** | Kinect for Windows **v1** + a USB 2.0 port |
| **Unity** | 6000.3.13f1 (any recent Unity version should work) |
| **GPU** | NVIDIA GPU with CUDA support (required by the Kinect SDK wrapper) |

---

## Installation

### 1. Kinect drivers and SDK

Install both packages from Microsoft (order matters — SDK first, then Toolkit):

1. [Kinect for Windows SDK v1.8](https://www.microsoft.com/en-ca/download/details.aspx?id=40278)
2. [Kinect for Windows Developer Toolkit v1.8](https://www.microsoft.com/en-us/download/details.aspx?id=40276)

Verify the installation by opening the **Kinect Explorer** sample from the Toolkit. If the skeleton appears, you're ready.

### 2. Clone the repository

```bash
git clone https://github.com/ChrisDa2/Kinect-Animation-Generator.git
```

### 3. Open in Unity

Open the project folder with Unity Hub using **Unity 6000.3.13f1** or a compatible version. The Kinect SDK wrapper is already included under `Assets/KinectSDKv2` — no additional packages need to be installed.

---

## Project Structure

```
Assets/
├── KinectSDKv2/                    # Kinect SDK for Unity (based on Rabie Fathallah's wrapper)
└── KinectEnvironment/
    ├── Scenes/
    │   ├── RecordingSceneMirror    # Character faces the camera, Mirror flag on
    │   ├── RecordingSceneDefault   # Character faces the camera direction, Mirror off
    │   └── PreviewScene            # Play back a recording inside Unity
    ├── Scripts/
    │   ├── KinectDataRecorder.cs   # Captures joint data + images each frame
    │   └── ...                     # Live-preview and helper scripts
    ├── Characters/                 # Reference Humanoid rig (Ninja from Mixamo)
    ├── KinectCaptures/             # Recorded JSON + PNG output (created at runtime)
    │   ├── kinect_recording_data.json
    │   ├── RawImages/              # rgb_XXXXXX.png
    │   └── DepthImages/            # depth_XXXXXX.png
    └── GeneratedAnimations/        # Baked FBX clips
```

---

## Usage

### Recording an Animation

1. Connect the Kinect v1 sensor to a USB 2.0 port and confirm the drivers are working.
2. In Unity, open `RecordingSceneMirror` (or `RecordingSceneDefault` if you want a non-mirrored capture).
3. Press **Play**. Wait for the status text to show `Press SPACE to start recording`.
4. Stand **1.5 – 2 meters** in front of the sensor. The Cubeman skeleton and the Ninja avatar should track you in real time.
5. Press **Space** to start the 5-second countdown, then settle into your starting pose.
6. Perform the motion you want to record.
7. Press **Escape** to stop recording.

All captured data is written to:

```
Assets/KinectEnvironment/KinectCaptures/
```

Each recorded frame contains:

- `RawImages/rgb_XXXXXX.png` — mirrored RGB frame
- `DepthImages/depth_XXXXXX.png` — mirrored depth frame
- A JSON entry with joint positions, rotations, tracking state, and the avatar's root transform

### Baking the Animation to FBX

1. In Unity, open **Tools → Kinect → Bake Avatar Animation → FBX Export**.
2. In the window:

   | Field | Recommended value |
   |---|---|
   | JSON file path | `Assets/KinectEnvironment/KinectCaptures/kinect_recording_data.json` |
   | Clip name | Any name, e.g. `BakedAnimation` |
   | Character Prefab | A Humanoid prefab (the Ninja avatar is a good default) |
   | Output path | `Assets/KinectEnvironment/GeneratedAnimations/` |

3. Click **Create FBX with Animation**.

The generated FBX is automatically imported and configured with:

- **Rig:** Humanoid
- **Root Transform Rotation:** Based Upon Original
- **Root Transform Position (Y):** Bake Into Pose (based on Feet)
- **Root Transform Position (XZ):** Bake Into Pose (based on Hips)

You can drag the resulting `.fbx` into any scene and use it on any Humanoid character.

### Previewing a Recording

Open `PreviewScene` to inspect a recording without re-running the capture step. This is useful for quickly validating that the recorded joint data looks correct before baking.

---

## Notes and Limitations

- **Sensor position matters.** For full-body captures that include the feet, mount the sensor at roughly **0.9 – 1.2 m** above the floor and place the subject 1.5 – 2 m away. A sensor placed too high can crop the feet out of the frame, which the SDK silently interpolates.
- **Kinect v1 has limited joint detail.** Fingers are not tracked (the hand is a single joint) and the wrist/foot joints carry noticeable noise during fast motion.
- **Retargeting preserves proportions of the reference rig.** The FBX clip drives joint **rotations** on the target avatar. Bone **lengths** come from the rig itself, which is standard behaviour for Humanoid animation and is what allows one clip to be reused on any character.
- **Single subject only.** The recorder tracks Player 1 (`GetPlayer1ID`) and ignores additional people in the frame.
- **Windows-only.** The Kinect v1 SDK has no Linux or macOS support, and neither does the Unity wrapper.

> **A note on an abandoned experiment.** An additional pipeline that reconstructed 3D joint positions from OpenPose 2D keypoints was prototyped on top of this project but was removed from this release because joint depth estimation from a single RGB camera proved unreliable for full-body animation. The present release contains only the Kinect-based pipeline, which is the version that produces consistent, usable results.

---

## Credits

This project is built on top of work by several people, and their contribution is gratefully acknowledged.

### Kinect SDK for Unity

The Kinect integration is powered by the **"Kinect with MS-SDK"** Unity asset, originally created by **RF Solutions** (Rumen Filkov) and later updated for modern Unity versions by **Rabie Fathallah**.

- Original author: RF Solutions / Rumen Filkov (Unity Asset Store, free asset)
- Modern fork: [Rabie Fathallah](https://github.com/Rabie-Fathallah/Kinect-SDK)

The SDK is bundled in this repository so the project can be opened and run without manual setup. All rights to the SDK belong to its original authors. See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for the full details.

### Mixamo character

The **Ninja** character used as a reference Humanoid rig is provided by [Mixamo](https://www.mixamo.com/) (Adobe) under the Mixamo Terms of Use.

### Microsoft Kinect for Windows SDK v1.8

The low-level sensor access at runtime is provided by Microsoft's Kinect for Windows SDK v1.8 and Developer Toolkit v1.8, which users install separately.

---

## License

This project's original code is released under the **MIT License**. See [`LICENSE`](LICENSE) for the full text.

Third-party components bundled with the project are subject to their own licenses; see [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for details.
