# Third-Party Notices

This project is distributed under the MIT License (see `LICENSE`). It depends
on the following third-party components, which are subject to their own terms.

---

## Kinect SDK for Unity ("Kinect with MS-SDK")

**Files:** `Assets/KinectSDKv2/`

- **Original author:** RF Solutions (Rumen Filkov)
- **Original distribution:** Unity Asset Store (free asset)
- **Modern fork used in this project:** Rabie Fathallah
  - Source: https://github.com/Rabie-Fathallah/Kinect-SDK
- **License:** No explicit open-source license is declared by the original
  author or by the fork's maintainer. The asset is distributed free of charge
  through the Unity Asset Store, and the fork is publicly available on GitHub.

**Usage in this project:** The Kinect SDK is used as a library to access the
Kinect for Windows v1 sensor from Unity. This project does not modify the SDK's
source code; it consumes it through its public API. The SDK is included in this
repository so the project can be opened and run directly in Unity without
additional setup.

**Attribution:** All credit for the SDK belongs to RF Solutions (original
author) and Rabie Fathallah (maintainer of the Unity 2020+ compatible fork).

**Recommendation for downstream users:** If you intend to redistribute this
project, we recommend that you review the Kinect SDK's own terms. If you are
unsure, we suggest removing `Assets/KinectSDKv2/` from your redistribution and
directing users to obtain the SDK through its official channels instead.

---

## Mixamo character — "Ninja"

**Files:** `Assets/KinectEnvironment/Characters/`

- **Author:** Adobe
- **Source:** https://www.mixamo.com/
- **License:** Adobe Mixamo Terms of Use
  (https://www.mixamo.com/legal/mixamo_terms_of_use)

**Usage in this project:** The Ninja character is included as a reference
Humanoid rig for recording and baking demonstrations. It must remain bundled
with the rest of the project and must not be extracted and redistributed as a
standalone asset.

---

## Microsoft Kinect for Windows SDK v1.8 and Developer Toolkit v1.8

**Files:** Not bundled. Users must install these separately.

- **Author:** Microsoft
- **Sources:**
  - https://www.microsoft.com/en-ca/download/details.aspx?id=40278
  - https://www.microsoft.com/en-us/download/details.aspx?id=40276
- **License:** Microsoft Kinect for Windows SDK License Agreement

**Usage in this project:** These SDKs are required at runtime. They are not
redistributed with this repository; users install them from Microsoft's
official download pages.
