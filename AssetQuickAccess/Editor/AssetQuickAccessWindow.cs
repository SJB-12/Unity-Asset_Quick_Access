using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace AssetQuickAccess
{
    public class AssetQuickAccessWindow : EditorWindow
    {
        private AssetQuickAccessData data;
        private Vector2 scrollPosition;
        private string searchString = "";
        private UnityEditor.IMGUI.Controls.SearchField searchField;
        private GUIStyle iconButtonStyle;

        // UI state for inline renaming
        private int renamingContainerIndex = -1;
        private string renamingContainerName = "";
        private bool focusedRenameContainerField = false;

        private int renamingItemContainerIndex = -1;
        private int renamingItemIndex = -1;
        private string renamingItemName = "";
        private bool focusedRenameItemField = false;

        // Visual tracking for drag and drop
        private int activeDragContainerIndex = -1;

        [MenuItem("Window/Asset Quick Access")]
        public static void ShowWindow()
        {
            AssetQuickAccessWindow window = GetWindow<AssetQuickAccessWindow>("Quick Access");
            window.titleContent = new GUIContent("Quick Access", EditorGUIUtility.IconContent("d_Favorite Icon").image);
            window.minSize = new Vector2(250, 300);
            window.Show();
        }

        [MenuItem("Window/Asset Quick Access/Export Unity Package")]
        public static void ExportUnityPackage()
        {
            string packagePath = "AssetQuickAccess_v0.0.1.unitypackage";
            string[] assetPaths = new string[]
            {
                "Assets/AssetQuickAccess"
            };

            AssetDatabase.ExportPackage(assetPaths, packagePath, ExportPackageOptions.Recurse);
            EditorUtility.DisplayDialog("Export Package", $"Successfully exported Unity Package to project root as:\n{packagePath}", "OK");
        }



        private void OnEnable()
        {
            data = AssetQuickAccessData.GetOrCreateData();
            searchField = new UnityEditor.IMGUI.Controls.SearchField();
        }

        private void OnGUI()
        {
            if (iconButtonStyle == null)
            {
                iconButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    padding = new RectOffset(1, 1, 1, 1)
                };
            }
            if (data == null)
            {
                data = AssetQuickAccessData.GetOrCreateData();
                if (data == null)
                {
                    EditorGUILayout.HelpBox("Failed to load or create AssetQuickAccessData.", MessageType.Error);
                    return;
                }
            }

            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (data.containers.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                for (int i = 0; i < data.containers.Count; i++)
                {
                    DrawContainer(i);
                }
            }

            EditorGUILayout.EndScrollView();

            // Handle general drag and drop events at the window level (e.g. dragging outside any container)
            HandleWindowDragAndDrop();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Search field
            if (searchField == null)
            {
                searchField = new UnityEditor.IMGUI.Controls.SearchField();
            }
            searchString = searchField.OnToolbarGUI(searchString);

            GUILayout.FlexibleSpace();

            // Action buttons
            if (GUILayout.Button(new GUIContent(" Add Container", EditorGUIUtility.IconContent("CreateAddNew").image), EditorStyles.toolbarButton))
            {
                CreateNewContainer();
            }

            if (GUILayout.Button(new GUIContent("Clean Missing", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), EditorStyles.toolbarButton))
            {
                CleanMissingAssets();
            }

            if (GUILayout.Button(new GUIContent("", EditorGUIUtility.IconContent("d_Settings").image, "Select Data Asset"), EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                Selection.activeObject = data;
                EditorGUIUtility.PingObject(data);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("No containers created yet.", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("Create Container", GUILayout.Height(30)))
            {
                CreateNewContainer();
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void CreateNewContainer()
        {
            string baseName = "New Container";
            string containerName = baseName;
            int counter = 1;

            while (data.containers.Exists(c => c.name == containerName))
            {
                containerName = $"{baseName} ({counter})";
                counter++;
            }

            QuickAccessContainer newContainer = new QuickAccessContainer { name = containerName };
            data.containers.Add(newContainer);
            data.Save();

            // Put the new container into rename mode immediately
            renamingContainerIndex = data.containers.Count - 1;
            renamingContainerName = containerName;
            focusedRenameContainerField = false;
        }

        private void DrawContainer(int containerIndex)
        {
            QuickAccessContainer container = data.containers[containerIndex];

            // Filter containers based on search if needed (keep container visible if its name matches or any of its items match)
            bool containerMatchesSearch = string.IsNullOrEmpty(searchString) || 
                                          container.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
            
            bool hasMatchingItems = false;
            if (!containerMatchesSearch && !string.IsNullOrEmpty(searchString))
            {
                foreach (var item in container.items)
                {
                    if (item.displayName.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (item.targetObject != null && item.targetObject.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        hasMatchingItems = true;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(searchString) && !containerMatchesSearch && !hasMatchingItems)
            {
                return; // Hide container if it does not match and none of its items match
            }

            // Add vertical spacing between containers
            if (containerIndex > 0)
            {
                GUILayout.Space(8f);
            }

            // Begin Container Area GUI Box
            Rect boxRect = EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(4f); // Padding at the top inside container box
            
            // Draw Drag & Drop outline if this container is the active drop target
            if (activeDragContainerIndex == containerIndex)
            {
                Color highlightColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.3f, 0.5f, 0.8f, 0.15f) 
                    : new Color(0.2f, 0.4f, 0.7f, 0.15f);
                EditorGUI.DrawRect(boxRect, highlightColor);
                DrawOutline(boxRect, new Color(0.3f, 0.6f, 0.9f, 0.8f), 2f);
            }

            DrawContainerHeader(containerIndex, container);

            if (container.isExpanded)
            {
                GUILayout.Space(10f); // Space between header and item list (increased gap)
                EditorGUI.indentLevel++;
                DrawContainerItems(containerIndex, container);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(4f); // Padding at the bottom inside container box
            EditorGUILayout.EndVertical();

            // Handle Drag & Drop specifically for this container's visual rect
            HandleContainerDragAndDrop(boxRect, containerIndex);
        }

        private void DrawContainerHeader(int containerIndex, QuickAccessContainer container)
        {
            EditorGUILayout.BeginHorizontal();

            // Foldout arrow (restricted width to prevent pushing the label)
            Rect foldoutRect = GUILayoutUtility.GetRect(15f, 18f, GUILayout.Width(15f));
            bool isExpanded = EditorGUI.Foldout(foldoutRect, container.isExpanded, "", true);
            if (isExpanded != container.isExpanded)
            {
                container.isExpanded = isExpanded;
                data.Save();
            }

            GUILayout.Space(4f); // Space between foldout arrow and container name

            // Inline Rename container
            if (renamingContainerIndex == containerIndex)
            {
                GUI.SetNextControlName("RenameContainerField");
                renamingContainerName = EditorGUILayout.TextField(renamingContainerName, GUILayout.ExpandWidth(true));
                
                // Save Rename
                if (GUILayout.Button("Save", GUILayout.Width(50)) || 
                    (Event.current.isKey && Event.current.keyCode == KeyCode.Return))
                {
                    if (!string.IsNullOrEmpty(renamingContainerName.Trim()))
                    {
                        container.name = renamingContainerName.Trim();
                        data.Save();
                    }
                    renamingContainerIndex = -1;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
                
                // Cancel Rename
                if (GUILayout.Button("Cancel", GUILayout.Width(60)) || 
                    (Event.current.isKey && Event.current.keyCode == KeyCode.Escape))
                {
                    renamingContainerIndex = -1;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }

                if (!focusedRenameContainerField)
                {
                    EditorGUI.FocusTextInControl("RenameContainerField");
                    focusedRenameContainerField = true;
                }
            }
            else
            {
                // Double click to rename container label
                GUILayout.Label(container.name, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                Rect labelRect = GUILayoutUtility.GetLastRect();
                
                if (Event.current.type == EventType.MouseDown && 
                    labelRect.Contains(Event.current.mousePosition) && 
                    Event.current.clickCount == 2)
                {
                    renamingContainerIndex = containerIndex;
                    renamingContainerName = container.name;
                    focusedRenameContainerField = false;
                    Event.current.Use();
                }
            }

            GUILayout.FlexibleSpace();

            // Add Asset button (plus icon) - Creates an empty bookmark field directly in the container
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus").image, "Add Empty Reference Field"), iconButtonStyle, GUILayout.Width(24), GUILayout.Height(20)))
            {
                container.items.Add(new QuickAccessItem
                {
                    displayName = "New Link",
                    targetObject = null,
                    guid = ""
                });
                container.isExpanded = true;
                data.Save();

                // Put the newly created item into rename mode immediately!
                renamingItemContainerIndex = containerIndex;
                renamingItemIndex = container.items.Count - 1;
                renamingItemName = "New Link";
                focusedRenameItemField = false;
            }

            // Options dropdown menu button (using Settings gear icon instead of text)
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle, GUILayout.Width(22), GUILayout.Height(20)))
            {
                ShowContainerContextMenu(containerIndex);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawContainerItems(int containerIndex, QuickAccessContainer container)
        {
            if (container.items.Count == 0)
            {
                EditorGUILayout.HelpBox("Empty container. Drag & Drop assets here to add.", MessageType.None);
                return;
            }

            // Track preset deletion to execute outside layout loops
            QuickAccessItem presetDeleteOwner = null;
            int presetDeleteIndex = -1;

            // Calculate widths dynamically based on window size to prevent right-side clipping
            float windowWidth = position.width;
            float indentSpace = 15f * EditorGUI.indentLevel;
            // Increased width for the asset field (45% of window width, clamped between 110px and 220px)
            float objectFieldWidth = Mathf.Clamp(windowWidth * 0.45f, 110f, 220f);
            // Deduct an extra 20px for the Lock button to avoid clipping
            float labelWidth = Mathf.Max(50f, windowWidth - indentSpace - objectFieldWidth - 75f);

            for (int j = 0; j < container.items.Count; j++)
            {
                var item = container.items[j];

                // Filter items based on search query
                if (!string.IsNullOrEmpty(searchString))
                {
                    bool matchesSearch = item.displayName.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         (item.targetObject != null && item.targetObject.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!matchesSearch) continue;
                }

                // Check for missing asset references
                bool isMissing = item.targetObject == null;

                // Inline rename item
                if (renamingItemContainerIndex == containerIndex && renamingItemIndex == j)
                {
                    // Draw a vertical block for the rename state
                    EditorGUILayout.BeginVertical();

                    // Line 1: TextField + ObjectField + Options Button
                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));
                    
                    // Zebra striping for edit row
                    if (j % 2 == 0)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.04f));
                    }

                    GUI.SetNextControlName("RenameItemField");
                    renamingItemName = EditorGUILayout.TextField(renamingItemName, GUILayout.Width(labelWidth), GUILayout.Height(18));

                    GUILayout.Space(4f); // Horizontal space before object field

                    // Object Field aligned perfectly using explicit rect with vertical centering
                    Rect editObjectFieldRect = GUILayoutUtility.GetRect(objectFieldWidth, 18, GUILayout.Width(objectFieldWidth));
                    editObjectFieldRect.y += 3f; // Vertically center inside 24px row
                    
                    // Track mouse clicks on the object field to select and ping the asset (excluding the circle picker)
                    Event currentEvent = Event.current;
                    Rect clickRect = new Rect(editObjectFieldRect.x, editObjectFieldRect.y, editObjectFieldRect.width - 20f, editObjectFieldRect.height);
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && clickRect.Contains(currentEvent.mousePosition))
                    {
                        if (item.targetObject != null)
                        {
                            Selection.activeObject = item.targetObject;
                            EditorGUIUtility.PingObject(item.targetObject);
                            Repaint();
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    Object editObj = EditorGUI.ObjectField(editObjectFieldRect, item.targetObject, typeof(Object), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        item.targetObject = editObj;
                        if (editObj != null)
                        {
                            if (string.IsNullOrEmpty(item.displayName) || item.displayName == "New Link" || item.displayName == "Empty Reference")
                            {
                                item.displayName = editObj.name;
                            }
                            item.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(editObj));
                        }
                        else
                        {
                            item.guid = "";
                        }
                        data.Save();
                    }

                    GUILayout.Space(4f); // Horizontal space before options button

                    // Dummy Options Button (disabled during rename)
                    GUI.enabled = false;
                    GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle, GUILayout.Width(18), GUILayout.Height(18));
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();

                    // Line 2: Save and Cancel buttons on the next line
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(15f); // Indent slightly to align under text field

                    if (GUILayout.Button("Save", GUILayout.Width(50), GUILayout.Height(18)) || 
                        (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "RenameItemField"))
                    {
                        if (!string.IsNullOrEmpty(renamingItemName.Trim()))
                        {
                            item.displayName = renamingItemName.Trim();
                            data.Save();
                        }
                        renamingItemContainerIndex = -1;
                        renamingItemIndex = -1;
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }

                    GUILayout.Space(4f);

                    if (GUILayout.Button("Cancel", GUILayout.Width(60), GUILayout.Height(18)) || 
                        (Event.current.isKey && Event.current.keyCode == KeyCode.Escape && GUI.GetNameOfFocusedControl() == "RenameItemField"))
                    {
                        renamingItemContainerIndex = -1;
                        renamingItemIndex = -1;
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(4f); // Spacing after buttons

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(3f); // Vertical space between items

                    if (!focusedRenameItemField)
                    {
                        EditorGUI.FocusTextInControl("RenameItemField");
                        focusedRenameItemField = true;
                    }
                }
                else
                {
                    // Standard Row (Not Renaming)
                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));

                    // Zebra striping for item rows
                    if (j % 2 == 0)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.04f));
                    }

                    // Draw Lock Toggle Button using native "IN LockButton" GUIStyle aligned perfectly using Rect
                    Rect lockRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
                    lockRect.y += 1f; // Shift up slightly to align perfectly with asset text and icon
                    
                    bool newLocked = GUI.Toggle(lockRect, item.isLocked, "", "IN LockButton");
                    if (newLocked != item.isLocked)
                    {
                        item.isLocked = newLocked;
                        data.Save();
                    }
                    GUILayout.Space(2f); // Horizontal space between lock and icon

                    // Item Icon
                    Texture2D assetIcon = null;
                    if (!isMissing)
                    {
                        assetIcon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(item.targetObject)) as Texture2D;
                    }
                    
                    if (assetIcon == null)
                    {
                        // Use a generic document icon if no asset is assigned yet
                        assetIcon = EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
                    }

                    // Item Icon drawn separately to make it larger (20x20px)
                    Rect iconRect = GUILayoutUtility.GetRect(20f, 20f, GUILayout.Width(20f));
                    iconRect.y += 2f; // Vertically center inside 24px row
                    if (assetIcon != null)
                    {
                        GUI.DrawTexture(iconRect, assetIcon);
                    }
                    
                    GUILayout.Space(4f); // Space between icon and name

                    // Display Name (acts as bookmark link) - normal styling
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.label);

                    // Note: Deducted 24f from labelWidth to account for the separately drawn icon (20px + 4px spacing)
                    if (GUILayout.Button(item.displayName, labelStyle, GUILayout.Width(labelWidth - 24f), GUILayout.Height(18)))
                    {
                        if (!isMissing)
                        {
                            Selection.activeObject = item.targetObject;
                            EditorGUIUtility.PingObject(item.targetObject);
                        }
                    }

                    // Double click to rename item (only if unlocked)
                    Rect labelRect = GUILayoutUtility.GetLastRect();
                    if (!item.isLocked && Event.current.type == EventType.MouseDown && 
                        labelRect.Contains(Event.current.mousePosition) && 
                        Event.current.clickCount == 2)
                    {
                        renamingItemContainerIndex = containerIndex;
                        renamingItemIndex = j;
                        renamingItemName = item.displayName;
                        focusedRenameItemField = false;
                        Event.current.Use();
                    }

                    GUILayout.Space(4f); // Horizontal space before object field

                    // Interactive Object Field with Y-offset vertical alignment
                    Rect objectFieldRect = GUILayoutUtility.GetRect(objectFieldWidth, 18, GUILayout.Width(objectFieldWidth));
                    objectFieldRect.y += 3f; // Vertically center inside 24px row
                    
                    // Track mouse clicks on the object field to select and ping the asset (excluding the circle picker)
                    Event currentEvent = Event.current;
                    Rect clickRect = new Rect(objectFieldRect.x, objectFieldRect.y, objectFieldRect.width - 20f, objectFieldRect.height);
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && clickRect.Contains(currentEvent.mousePosition))
                    {
                        if (item.targetObject != null)
                        {
                            Selection.activeObject = item.targetObject;
                            EditorGUIUtility.PingObject(item.targetObject);
                            Repaint();
                        }
                    }
                    
                    EditorGUI.BeginChangeCheck();
                    // Disable editing if locked
                    GUI.enabled = !item.isLocked;
                    Object newObj = EditorGUI.ObjectField(objectFieldRect, item.targetObject, typeof(Object), false);
                    GUI.enabled = true;
                    if (EditorGUI.EndChangeCheck())
                    {
                        item.targetObject = newObj;
                        if (newObj != null)
                        {
                            // Only overwrite the name if it is empty or default
                            if (string.IsNullOrEmpty(item.displayName) || 
                                item.displayName == "New Link" || 
                                item.displayName == "Empty Reference")
                            {
                                item.displayName = newObj.name;
                            }
                            item.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newObj));
                        }
                        else
                        {
                            item.guid = "";
                        }
                        data.Save();
                    }

                    GUILayout.Space(4f); // Horizontal space before options button

                    // Options/Operations context button
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle, GUILayout.Width(18), GUILayout.Height(18)) || 
                        (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition)))
                    {
                        ShowItemContextMenu(containerIndex, j);
                        if (Event.current.type == EventType.ContextClick)
                        {
                            Event.current.Use();
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    // Draw presets sub-row below standard row if targetObject is assigned and has presets
                    if (item.targetObject != null && item.presets.Count > 0)
                    {
                        // Clean null references from list in case preset files were deleted from project externally
                        item.presets.RemoveAll(p => p == null);

                        GUILayout.Space(2f); // Small vertical gap between bookmark and first preset row

                        for (int p = 0; p < item.presets.Count; p++)
                        {
                            var pr = item.presets[p];
                            if (pr == null) continue;

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(indentSpace + 24f); // Align under the asset name
                            
                            // Name TextField (directly edits the preset asset name!)
                            EditorGUI.BeginChangeCheck();
                            string newPresetName = EditorGUILayout.TextField(pr.name, GUILayout.Width(120), GUILayout.Height(16));
                            if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(newPresetName.Trim()))
                            {
                                string assetPath = AssetDatabase.GetAssetPath(pr);
                                AssetDatabase.RenameAsset(assetPath, newPresetName.Trim());
                                AssetDatabase.SaveAssets();
                            }
                            
                            GUILayout.Space(6f);

                            // Load Button
                            if (GUILayout.Button(new GUIContent("Load", "Load/Apply this preset's saved values to the component/asset."), EditorStyles.miniButton, GUILayout.Width(45), GUILayout.Height(16)))
                            {
                                ApplyPresetToContext(item, pr);
                            }

                            GUILayout.Space(4f);

                            // Save Button
                            if (GUILayout.Button(new GUIContent("Save", "Save/Overwrite this preset with the current values of the component/asset."), EditorStyles.miniButton, GUILayout.Width(45), GUILayout.Height(16)))
                            {
                                UpdatePresetFromContext(item, pr);
                            }

                            GUILayout.Space(4f);

                            // Delete Button (queues deletion to run outside layout frame)
                            if (GUILayout.Button(new GUIContent("Delete", "Delete this preset permanently."), EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(16)))
                            {
                                presetDeleteOwner = item;
                                presetDeleteIndex = p;
                            }

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(3f); // Vertical space between preset rows
                        }
                    }

                    GUILayout.Space(3f); // Vertical space between items
                }
            }

            // Execute queued preset deletion outside the layout loops to prevent GUILayout state corruption
            if (presetDeleteOwner != null && presetDeleteIndex != -1)
            {
                var pr = presetDeleteOwner.presets[presetDeleteIndex];
                if (pr != null)
                {
                    if (EditorUtility.DisplayDialog("Delete Preset", $"Are you sure you want to delete the preset '{pr.name}'? This will delete the preset file from your project.", "Yes", "No"))
                    {
                        string path = AssetDatabase.GetAssetPath(pr);
                        presetDeleteOwner.presets.RemoveAt(presetDeleteIndex);
                        data.Save();
                        
                        if (!string.IsNullOrEmpty(path))
                        {
                            AssetDatabase.DeleteAsset(path);
                        }
                        AssetDatabase.SaveAssets();
                    }
                }
                presetDeleteOwner = null;
                presetDeleteIndex = -1;
                GUIUtility.ExitGUI(); // Force immediate repaint and exit GUI frame cleanly
            }
        }

        private void ShowContainerContextMenu(int containerIndex)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Rename Container"), false, () => 
            {
                renamingContainerIndex = containerIndex;
                renamingContainerName = data.containers[containerIndex].name;
                focusedRenameContainerField = false;
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Move Up"), containerIndex > 0, () => 
            {
                var temp = data.containers[containerIndex];
                data.containers[containerIndex] = data.containers[containerIndex - 1];
                data.containers[containerIndex - 1] = temp;
                data.Save();
            });
            
            menu.AddItem(new GUIContent("Move Down"), containerIndex < data.containers.Count - 1, () => 
            {
                var temp = data.containers[containerIndex];
                data.containers[containerIndex] = data.containers[containerIndex + 1];
                data.containers[containerIndex + 1] = temp;
                data.Save();
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Clear All Bookmarks"), data.containers[containerIndex].items.Count > 0, () => 
            {
                if (EditorUtility.DisplayDialog("Clear Items", $"Are you sure you want to clear all items in container '{data.containers[containerIndex].name}'?", "Yes", "No"))
                {
                    data.containers[containerIndex].items.Clear();
                    data.Save();
                }
            });
            
            menu.AddItem(new GUIContent("Delete Container"), false, () => 
            {
                if (data.containers[containerIndex].items.Count == 0 || 
                    EditorUtility.DisplayDialog("Delete Container", $"Are you sure you want to delete container '{data.containers[containerIndex].name}' and all its bookmarked items?", "Yes", "No"))
                {
                    data.containers.RemoveAt(containerIndex);
                    data.Save();
                }
            });
            
            menu.ShowAsContext();
        }

        private void ShowItemContextMenu(int containerIndex, int itemIndex)
        {
            GenericMenu menu = new GenericMenu();
            var container = data.containers[containerIndex];
            var item = container.items[itemIndex];

            if (!item.isLocked)
            {
                menu.AddItem(new GUIContent("Rename Bookmark"), false, () => 
                {
                    renamingItemContainerIndex = containerIndex;
                    renamingItemIndex = itemIndex;
                    renamingItemName = item.displayName;
                    focusedRenameItemField = false;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename Bookmark (Locked)"));
            }

            // Only add separator and move option if there's more than one container
            if (data.containers.Count > 1)
            {
                menu.AddSeparator("");
                if (!item.isLocked)
                {
                    foreach (var targetContainer in data.containers)
                    {
                        if (targetContainer == container) continue;
                        
                        menu.AddItem(new GUIContent($"Move to Container/{targetContainer.name}"), false, () =>
                        {
                            targetContainer.items.Add(item);
                            container.items.RemoveAt(itemIndex);
                            data.Save();
                        });
                    }
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Move to Container (Locked)"));
                }
            }

            menu.AddSeparator("");

            if (!item.isLocked)
            {
                menu.AddItem(new GUIContent("Delete Bookmark"), false, () => 
                {
                    container.items.RemoveAt(itemIndex);
                    data.Save();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Delete Bookmark (Locked)"));
            }

            menu.ShowAsContext();
        }

        private void HandleContainerDragAndDrop(Rect rect, int containerIndex)
        {
            Event evt = Event.current;
            if (!rect.Contains(evt.mousePosition))
            {
                if (activeDragContainerIndex == containerIndex && evt.type == EventType.DragExited)
                {
                    activeDragContainerIndex = -1;
                    Repaint();
                }
                return;
            }

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (activeDragContainerIndex != containerIndex)
                    {
                        activeDragContainerIndex = containerIndex;
                        Repaint();
                    }
                    evt.Use();
                    break;
                    
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    activeDragContainerIndex = -1;
                    
                    QuickAccessContainer container = data.containers[containerIndex];
                    bool addedAny = false;
                    
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;
                        
                        // Avoid duplicates in the same container
                        if (container.items.Exists(item => item.targetObject == obj))
                            continue;

                        QuickAccessItem newItem = new QuickAccessItem
                        {
                            displayName = obj.name,
                            targetObject = obj,
                            guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj))
                        };
                        container.items.Add(newItem);
                        addedAny = true;
                    }
                    
                    if (addedAny)
                    {
                        data.Save();
                    }
                    
                    evt.Use();
                    Repaint();
                    break;
                    
                case EventType.DragExited:
                    if (activeDragContainerIndex == containerIndex)
                    {
                        activeDragContainerIndex = -1;
                        Repaint();
                    }
                    break;
            }
        }

        private void HandleWindowDragAndDrop()
        {
            // If dragging files onto the window but not over a container, we can allow dropping to automatically create a new container
            Event evt = Event.current;
            Rect windowRect = new Rect(0, 0, position.width, position.height);
            
            if (!windowRect.Contains(evt.mousePosition) || activeDragContainerIndex != -1)
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                
                // Create a container named after the first dragged item or just "Dropped Assets"
                string newContainerName = DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] != null
                    ? $"{DragAndDrop.objectReferences[0].name} Group"
                    : "Dropped Bookmarks";

                // Ensure unique container name
                string finalName = newContainerName;
                int counter = 1;
                while (data.containers.Exists(c => c.name == finalName))
                {
                    finalName = $"{newContainerName} ({counter})";
                    counter++;
                }

                QuickAccessContainer newContainer = new QuickAccessContainer { name = finalName };
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (obj == null) continue;
                    newContainer.items.Add(new QuickAccessItem
                    {
                        displayName = obj.name,
                        targetObject = obj,
                        guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj))
                    });
                }
                
                data.containers.Add(newContainer);
                data.Save();
                
                evt.Use();
                Repaint();
            }
        }

        private void CleanMissingAssets()
        {
            int cleanCount = 0;
            foreach (var container in data.containers)
            {
                int countBefore = container.items.Count;
                // Only clean up unassigned references if they are UNLOCKED!
                container.items.RemoveAll(item => !item.isLocked && item.targetObject == null);
                cleanCount += (countBefore - container.items.Count);
            }

            if (cleanCount > 0)
            {
                data.Save();
                EditorUtility.DisplayDialog("Clean Bookmarks", $"Successfully removed {cleanCount} bookmarks that were missing or deleted.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Clean Bookmarks", "No missing bookmarks were found.", "OK");
            }
        }

        private void DrawOutline(Rect rect, Color color, float width = 1f)
        {
            // Top outline
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            // Bottom outline
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            // Left outline
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            // Right outline
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        [MenuItem("CONTEXT/Component/Save Quick Access Preset")]
        private static void SaveComponentPreset(MenuCommand command)
        {
            Component comp = command.context as Component;
            if (comp != null)
            {
                SavePresetFromContext(comp);
            }
        }

        [MenuItem("CONTEXT/ScriptableObject/Save Quick Access Preset")]
        private static void SaveScriptableObjectPreset(MenuCommand command)
        {
            ScriptableObject so = command.context as ScriptableObject;
            if (so != null)
            {
                SavePresetFromContext(so);
            }
        }

        [MenuItem("CONTEXT/Material/Save Quick Access Preset")]
        private static void SaveMaterialPreset(MenuCommand command)
        {
            Material mat = command.context as Material;
            if (mat != null)
            {
                SavePresetFromContext(mat);
            }
        }

        private static void SavePresetFromContext(Object contextObject)
        {
            if (contextObject == null) return;

            // Find matching bookmark item
            QuickAccessItem matchingItem = null;
            var data = AssetQuickAccessData.GetOrCreateData();
            if (data == null) return;

            // We look for direct match or prefab-component match
            Object targetSourceObj = contextObject;
            if (contextObject is Component comp)
            {
                targetSourceObj = comp.gameObject;
                Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(comp.gameObject);
                if (prefabAsset != null)
                {
                    GameObject prefabAssetRoot = (prefabAsset as GameObject).transform.root.gameObject;
                    targetSourceObj = prefabAssetRoot;
                }
            }

            foreach (var container in data.containers)
            {
                foreach (var item in container.items)
                {
                    if (item.targetObject == null) continue;

                    if (item.targetObject == targetSourceObj)
                    {
                        matchingItem = item;
                        break;
                    }

                    if (item.targetObject is GameObject bookmarkedGO && contextObject is Component c)
                    {
                        if (c.gameObject == bookmarkedGO || c.transform.IsChildOf(bookmarkedGO.transform))
                        {
                            matchingItem = item;
                            break;
                        }
                        
                        GameObject pAsset = PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject);
                        if (pAsset != null)
                        {
                            GameObject prefabRoot = pAsset.transform.root.gameObject;
                            if (prefabRoot == bookmarkedGO)
                            {
                                matchingItem = item;
                                break;
                            }
                        }
                    }
                }
                if (matchingItem != null) break;
            }

            if (matchingItem == null)
            {
                string objName = contextObject is Component ? (contextObject as Component).gameObject.name : contextObject.name;
                EditorUtility.DisplayDialog("Save Quick Access Preset", 
                    $"To save a preset for '{objName}', you must first add this asset (or its Prefab GameObject) to your Quick Access bookmarks window.", "OK");
                return;
            }

            // Clean null presets
            matchingItem.presets.RemoveAll(p => p == null);

            if (matchingItem.presets.Count >= data.maxPresetsLimit)
            {
                EditorUtility.DisplayDialog("Save Quick Access Preset", 
                    $"You have reached the maximum limit of {data.maxPresetsLimit} presets for this bookmarked asset. Please delete an existing preset in the Quick Access window before adding a new one.", "OK");
                return;
            }

            // Ensure directory exists
            string folderPath = "Assets/AssetQuickAccess/Presets";
            if (!AssetDatabase.IsValidFolder("Assets/AssetQuickAccess"))
            {
                AssetDatabase.CreateFolder("Assets", "AssetQuickAccess");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/AssetQuickAccess", "Presets");
            }

            // Generate unique filename
            string safeName = matchingItem.displayName.Replace(" ", "_");
            string typeName = contextObject.GetType().Name;
            string presetPath = $"{folderPath}/{safeName}_{typeName}_Preset.preset";
            presetPath = AssetDatabase.GenerateUniqueAssetPath(presetPath);

            // Create preset asset
            Preset preset = new Preset(contextObject);
            AssetDatabase.CreateAsset(preset, presetPath);
            AssetDatabase.SaveAssets();

            // Store in data model
            matchingItem.presets.Add(preset);
            data.Save();

            Debug.Log($"[Quick Access] Saved new preset '{preset.name}' under bookmark '{matchingItem.displayName}'.");
            
            // Repaint the Quick Access window if it is open
            var windows = Resources.FindObjectsOfTypeAll<AssetQuickAccessWindow>();
            if (windows.Length > 0 && windows[0] != null)
            {
                windows[0].Repaint();
            }
        }

        private static void ApplyPresetToContext(QuickAccessItem item, Preset preset)
        {
            if (preset == null || item.targetObject == null) return;

            GameObject go = item.targetObject as GameObject;
            if (go != null)
            {
                Component[] components = go.GetComponents<Component>();
                bool applied = false;
                foreach (var comp in components)
                {
                    if (comp != null && preset.CanBeAppliedTo(comp))
                    {
                        Undo.RegisterCompleteObjectUndo(comp, "Load Quick Access Preset");
                        preset.ApplyTo(comp);
                        applied = true;
                        break;
                    }
                }

                if (applied)
                {
                    EditorUtility.SetDirty(go);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go);
                    Debug.Log($"[Quick Access] Loaded preset '{preset.name}' onto component on Prefab '{go.name}'.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Preset Error", $"Could not find a component on prefab '{go.name}' matching the preset type '{preset.GetTargetTypeName()}'.", "OK");
                }
            }
            else
            {
                if (preset.CanBeAppliedTo(item.targetObject))
                {
                    Undo.RegisterCompleteObjectUndo(item.targetObject, "Load Quick Access Preset");
                    preset.ApplyTo(item.targetObject);
                    EditorUtility.SetDirty(item.targetObject);
                    Debug.Log($"[Quick Access] Loaded preset '{preset.name}' onto asset '{item.targetObject.name}'.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Preset Error", $"Preset '{preset.name}' is not compatible with asset '{item.targetObject.name}'.", "OK");
                }
            }
        }

        private static void UpdatePresetFromContext(QuickAccessItem item, Preset preset)
        {
            if (preset == null || item.targetObject == null) return;

            Object sourceObj = null;
            GameObject go = item.targetObject as GameObject;
            if (go != null)
            {
                Component[] components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null && preset.CanBeAppliedTo(comp))
                    {
                        sourceObj = comp;
                        break;
                    }
                }
            }
            else
            {
                sourceObj = item.targetObject;
            }

            if (sourceObj != null)
            {
                preset.UpdateProperties(sourceObj);
                EditorUtility.SetDirty(preset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Quick Access] Saved/Updated preset '{preset.name}' with the current values of '{sourceObj.name}'.");
            }
            else
            {
                EditorUtility.DisplayDialog("Save Preset Error", "Could not find the target object or component to save from.", "OK");
            }
        }

    }
}
