using UnityEngine;
using System.Collections;

public class ApplySavedPlayerState : MonoBehaviour
{
	[Tooltip("If assigned, this transform will be moved. Otherwise, this GameObject's transform is used.")]
	public Transform playerTransformOverride;

	void Awake()
	{
		var t = playerTransformOverride != null ? playerTransformOverride : transform;

		// If there is a saved scene, try to read player transform and apply
		string savedScene = PlayerPrefs.GetString(PauseMenuManager.LastSaveSceneKey, string.Empty);
		if (!string.IsNullOrEmpty(savedScene))
		{
			// Only apply if keys exist (pos X is a good sentinel)
			if (PlayerPrefs.HasKey(PauseMenuManager.LastPlayerPosXKey))
			{
				float px = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerPosXKey);
				float py = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerPosYKey);
				float pz = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerPosZKey);
				float rx = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerRotXKey);
				float ry = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerRotYKey);
				float rz = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerRotZKey);
				float rw = PlayerPrefs.GetFloat(PauseMenuManager.LastPlayerRotWKey);

				// Apply position and rotation
				t.position = new Vector3(px, py, pz);
				t.rotation = new Quaternion(rx, ry, rz, rw);
			}

			// Load inventory and hotbar data
			PauseMenuManager.LoadInventoryData();

			// Load temperature data with a small delay to ensure all components are initialized
			StartCoroutine(LoadSavedDataDelayed());
		}
	}

	private IEnumerator LoadSavedDataDelayed()
	{
		// Wait a frame to ensure all components are initialized
		yield return null;
		PauseMenuManager.LoadTemperatureData();
		
		// DayNightCycle now loads its state in Start(), so we don't need to call LoadDayNightCycleData here
		// But we'll keep it as a backup in case Start() hasn't run yet
		PauseMenuManager.LoadDayNightCycleData();
		
		// Wait another frame for spawners to initialize
		yield return null;
		PauseMenuManager.LoadSpawnerStates();
	}
}


