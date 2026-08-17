# Asset Quick Access Window

A fast, lightweight, and robust editor utility for Unity to bookmark, organize, and manage your most frequently used assets, prefabs, materials, and configurations. It also includes an integrated component/asset preset system with full Undo support.

---

##  Key Features

* **Drag-and-Drop Containers**: Group your bookmarks into custom-named container sections.
* **Safety Lock Toggles**: A native padlock icon next to each asset prevents renaming, replacing, or deleting it accidentally.
* **Inline Presets Manager**:
  * Save presets from the Inspector's component context menus (three dots).
  * Rename, apply (`Load`), overwrite (`Save`), or `Delete` presets directly from the Quick Access window.
  * Enforces custom limits on the number of presets allowed per bookmark.
* **Search Filter**: Instantly find specific containers or bookmarked assets.
* **Asset Integrity Check**: A single button to clean up missing or deleted references safely.
* **Sharper Layout**: Clean typographic hierarchy, vertically-aligned padlocks, and sharp, enlarged asset icons.

---

##  Installation & Setup

1. Put the `AssetQuickAccess` folder in your Unity project's `Assets/` directory.
2. Open the tool from the menu bar: **`Window > Asset Quick Access`**.

---

##  How to Use

### 1. Managing Bookmarks
* **Add Container**: Click the `+ Add Container` button in the top toolbar. Type a name and press Enter.
* **Add Bookmark**: Drag and drop any asset (prefab, config, material, audio, etc.) from the Project tab or scene directly into a container box.
* **Lock / Protect**: Click the **Padlock icon** at the left of the asset row. When active (locked), you cannot change the object field, rename, or delete the bookmark.

### 2. Inspector Preset System
* **Create a Preset**:
  1. Open the inspector for your bookmarked prefab, ScriptableObject, or Material.
  2. Click the **three dots** in the top-right corner of the Component or ScriptableObject header.
  3. Select **`Save Quick Access Preset`** from the context menu.
* **Manage Presets in the Tool**:
  * **Rename**: Double-click or select the preset text field to change its name. This renames the underlying `.preset` file on your project disk.
  * **Load**: Click the `Load` button to apply the preset's values back to the active component or configuration (supports standard `Ctrl+Z` Undo).
  * **Save**: Click `Save` to overwrite the preset file with the current values of the active component/asset in the Inspector.
  * **Delete**: Click `Delete` to remove the preset reference and delete the `.preset` file.

### 3. Config Settings
* Click the settings gear icon `⚙` on the top toolbar of the Quick Access window.
* This highlights the `AssetQuickAccessData` config file in your Inspector, letting you adjust the **Max Presets Per Bookmark** slider (default is `3`).
