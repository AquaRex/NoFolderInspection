using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;

namespace NoFolderInspection
{

	[InitializeOnLoad]
	public class NoFolderInspection
	{
		private static Object lastNonFolderSelection;
		private static bool lockedByScript = false;
		private const string PREF_KEY = "NoFolderInspection_Enabled";
		private static bool isEnabled;

		static NoFolderInspection()
		{
			isEnabled = EditorPrefs.GetBool(PREF_KEY, true);
			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
			Selection.selectionChanged += OnSelectionChanged;
		}

		[MenuItem("Tools/No Folder Inspector")]
		private static void ToggleFeature()
		{
			isEnabled = !isEnabled;
			EditorPrefs.SetBool(PREF_KEY, isEnabled);
			if (!isEnabled)
			{
				LockInspectors(false);
			}
		}

		[MenuItem("Tools/No Folder Inspector", true)]
		private static bool ToggleFeatureValidate()
		{
			Menu.SetChecked("Tools/No Folder Inspector", isEnabled);
			return true;
		}

		private static bool IsAnyInspectorLockedByUser()
		{
			EditorWindow[] inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (EditorWindow window in inspectors)
			{
				if (window.GetType().Name == "InspectorWindow")
				{
					PropertyInfo isLockedProperty = window.GetType().GetProperty("isLocked",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (isLockedProperty != null)
					{
						bool isLocked = (bool)isLockedProperty.GetValue(window);
						if (isLocked) return true;
					}
				}
			}
			return false;
		}

		private static void OnProjectWindowItemGUI(string guid, Rect rect)
		{
			if (!isEnabled) return;
			Event e = Event.current;
			if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
				{
					// Only lock if not already locked by user
					if (!IsAnyInspectorLockedByUser())
					{
						LockInspectors(true);
						lockedByScript = true;
					}
				}
			}
		}

		   private static void OnSelectionChanged()
		   {
			   if (!isEnabled) return;

			   Object currentSelection = Selection.activeObject;
			   if (currentSelection != null && !IsFolder(currentSelection))
			   {
				   if (lockedByScript)
				   {
					   LockInspectors(false);
					   lockedByScript = false;
				   }
				   lastNonFolderSelection = currentSelection;
			   }
		   }

		private static void LockInspectors(bool lockState)
		{
			EditorWindow[] inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (EditorWindow window in inspectors)
			{
				if (window.GetType().Name == "InspectorWindow")
				{
					PropertyInfo isLockedProperty = window.GetType().GetProperty("isLocked",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

					if (isLockedProperty != null)
					{
						isLockedProperty.SetValue(window, lockState);
						window.Repaint();
					}
				}
			}
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