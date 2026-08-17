# Asset Quick Access (v0.0.1)

A fast, lightweight, and robust editor utility for Unity to bookmark, organize, and manage your most frequently used assets, prefabs, materials, and configurations. It also includes an integrated component/asset preset system with full Undo support.

---

##  Installation Options

### Option 1: Unity Package (.unitypackage) - Recommended
1. Download [AssetQuickAccess_v0.0.1.unitypackage](AssetQuickAccess_v0.0.1.unitypackage) from this repository.
2. Open your Unity Project, then double-click the downloaded file (or go to `Assets > Import Package > Custom Package...`).
3. Click **Import**.

### Option 2: Unity Package Manager (UPM via Git)
1. Open the Package Manager in Unity (`Window > Package Manager`).
2. Click the `+` button in the top-left corner and select **Add package from git URL...**
3. Paste the following URL (replace with your repository link):
   `https://github.com/YOUR_USERNAME/Unity-Asset-Quick-Access.git?path=/Assets/AssetQuickAccess`
4. Click **Add**.

---

##  Features
* **Drag-and-Drop Containers**: Group your bookmarks into custom-named sections.
* **Safety Lock Toggles**: A native padlock icon prevents renaming, replacing, or deleting bookmarks accidentally.
* **Inspector Context Menu Integration**: Right-click/click three dots on any Component/ScriptableObject/Material in the Inspector, choose `Save Quick Access Preset`.
* **Dynamic Preset Control**: Inline fields to rename, apply (`Load`), overwrite (`Save`), or `Delete` presets under their bookmark.
* **Configurable Limit**: Click the gear icon on the toolbar to set the maximum presets allowed per bookmark (defaults to 3).
* **Missing Reference Cleaner**: Easily purge dead or deleted assets with a single click.
