using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace NoFolderInspection
{

	[InitializeOnLoad]
	public class NoFolderInspection
	{
		private static Object lastNonFolderSelection;
		private static Object previousSelection;
		private const string PREF_KEY = "NoFolderInspection_Enabled";
		private static bool isEnabled;
		private static Dictionary<EditorWindow, bool> wasManuallyLocked = new Dictionary<EditorWindow, bool>();

		static NoFolderInspection()
		{
			isEnabled = EditorPrefs.GetBool(PREF_KEY, true);
			EditorApplication.update += OnEditorUpdate;
		}

		private static void OnEditorUpdate()
		{
			if (!isEnabled) return;

			// Check if selection has changed
			if (Selection.activeObject != previousSelection)
			{
				OnSelectionChanged();
				previousSelection = Selection.activeObject;
			}
		}

		[MenuItem("Tools/No Folder Inspector")]
		private static void ToggleFeature()
		{
			isEnabled = !isEnabled;
			EditorPrefs.SetBool(PREF_KEY, isEnabled);
			
			// If disabling, unlock any inspectors we locked
			if (!isEnabled)
			{
				UnlockOurAutoLockedInspectors();
			}
			
			Debug.Log($"No Folder Inspector: {(isEnabled ? "Enabled" : "Disabled")}");
		}

		[MenuItem("Tools/No Folder Inspector", true)]
		private static bool ToggleFeatureValidate()
		{
			Menu.SetChecked("Tools/No Folder Inspector", isEnabled);
			return true;
		}

		private static void OnSelectionChanged()
		{
			if (!isEnabled) return;

			Object currentSelection = Selection.activeObject;
			
			// If a folder is selected, auto-lock the inspectors immediately
			if (currentSelection != null && IsFolder(currentSelection))
			{
				// Lock immediately, before the inspector tries to update
				EditorApplication.delayCall += () =>
				{
					AutoLockInspectors();
					// If we had a previous non-folder selection, revert to it
					if (lastNonFolderSelection != null)
					{
						Selection.activeObject = lastNonFolderSelection;
					}
				};
			}
			// If a non-folder is selected, auto-unlock inspectors (that we locked)
			else if (currentSelection != null)
			{
				AutoUnlockInspectors();
				lastNonFolderSelection = currentSelection;
			}
		}

		private static void AutoLockInspectors()
		{
			var inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>()
				.Where(w => w.GetType().Name == "InspectorWindow");

			foreach (EditorWindow window in inspectors)
			{
				PropertyInfo isLockedProperty = window.GetType().GetProperty("isLocked",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (isLockedProperty != null)
				{
					bool currentLockState = (bool)isLockedProperty.GetValue(window);
					
					// Store the current lock state before we change it
					if (!wasManuallyLocked.ContainsKey(window))
					{
						wasManuallyLocked[window] = currentLockState;
					}
					
					// Only lock if it wasn't already locked (don't interfere with manual locks)
					if (!currentLockState)
					{
						isLockedProperty.SetValue(window, true);
						window.Repaint();
					}
				}
			}
		}

		private static void AutoUnlockInspectors()
		{
			var inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>()
				.Where(w => w.GetType().Name == "InspectorWindow");

			foreach (EditorWindow window in inspectors)
			{
				PropertyInfo isLockedProperty = window.GetType().GetProperty("isLocked",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (isLockedProperty != null)
				{
					// Only unlock if we were the ones who locked it (it wasn't manually locked before)
					if (wasManuallyLocked.ContainsKey(window) && !wasManuallyLocked[window])
					{
						isLockedProperty.SetValue(window, false);
						window.Repaint();
					}
					
					// Clean up our tracking
					wasManuallyLocked.Remove(window);
				}
			}
		}

		private static void UnlockOurAutoLockedInspectors()
		{
			var inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>()
				.Where(w => w.GetType().Name == "InspectorWindow");

			foreach (EditorWindow window in inspectors)
			{
				PropertyInfo isLockedProperty = window.GetType().GetProperty("isLocked",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (isLockedProperty != null && wasManuallyLocked.ContainsKey(window))
				{
					// Restore the original lock state
					isLockedProperty.SetValue(window, wasManuallyLocked[window]);
					window.Repaint();
				}
			}
			
			wasManuallyLocked.Clear();
		}

		private static bool IsFolder(Object obj)
		{
			if (obj == null) return false;
			string path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path)) return false;
			return Directory.Exists(path);
		}
	}
}