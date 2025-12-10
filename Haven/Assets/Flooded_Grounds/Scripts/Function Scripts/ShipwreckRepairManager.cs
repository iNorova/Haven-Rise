using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class MaterialRequirement
{
	[Tooltip("Drag the item prefab/asset here that you want to require.")]
	public GameObject requiredItem;
	public int requiredAmount = 1;
}

/// <summary>
/// Tracks shipwreck repair progress. Assign required materials in the inspector.
/// Call TryAddMaterial when a material is delivered (e.g., via drag/drop).
/// </summary>
public class ShipwreckRepairManager : MonoBehaviour
{
	[Header("Requirements")]
	public List<MaterialRequirement> requiredMaterials = new List<MaterialRequirement>();

	[Header("UI")]
	[Tooltip("Optional text to show requirement progress (e.g., Wood 2/5).")]
	public Text requirementsText;

	[Header("Events")]
	public UnityEvent onRepairCompleted;

	private Dictionary<GameObject, int> _progress;

	private void Awake()
	{
		// Initialize progress dictionary
		_progress = new Dictionary<GameObject, int>();
		foreach (var req in requiredMaterials)
		{
			if (req == null || req.requiredItem == null)
			{
				continue;
			}
			if (!_progress.ContainsKey(req.requiredItem))
			{
				_progress[req.requiredItem] = 0;
			}
		}

		UpdateUI();
	}

	private bool MatchesRequirement(GameObject draggedItem, GameObject requiredItem)
	{
		if (draggedItem == null || requiredItem == null)
		{
			return false;
		}

		// Check if dragged item is the same prefab as required item
		// Compare by name (without Clone suffix) or by direct reference
		string draggedName = draggedItem.name.Replace("(Clone)", "").Trim();
		string requiredName = requiredItem.name.Replace("(Clone)", "").Trim();
		
		if (draggedName == requiredName)
		{
			return true;
		}

		// Also check if the dragged item's prefab reference matches
		// This handles cases where items are instantiated from prefabs
		return draggedItem.name.StartsWith(requiredItem.name);
	}

	public bool IsComplete()
	{
		foreach (var req in requiredMaterials)
		{
			if (req == null || req.requiredItem == null) continue;
			if (!_progress.TryGetValue(req.requiredItem, out var have)) return false;
			if (have < req.requiredAmount) return false;
		}
		return true;
	}

	/// <summary>
	/// Try to add a material by GameObject reference. Returns true if accepted/consumed.
	/// </summary>
	public bool TryAddMaterial(GameObject draggedItem)
	{
		if (draggedItem == null)
		{
			return false;
		}

		// Find matching requirement
		MaterialRequirement matchingReq = null;
		foreach (var req in requiredMaterials)
		{
			if (req == null || req.requiredItem == null) continue;
			if (MatchesRequirement(draggedItem, req.requiredItem))
			{
				matchingReq = req;
				break;
			}
		}

		if (matchingReq == null)
		{
			Debug.Log($"[ShipwreckRepair] Item '{draggedItem.name}' not required.");
			return false;
		}

		_progress.TryGetValue(matchingReq.requiredItem, out var have);
		if (have >= matchingReq.requiredAmount)
		{
			Debug.Log($"[ShipwreckRepair] '{matchingReq.requiredItem.name}' already satisfied ({have}/{matchingReq.requiredAmount}).");
			return false;
		}

		_progress[matchingReq.requiredItem] = have + 1;
		Debug.Log($"[ShipwreckRepair] Added '{matchingReq.requiredItem.name}' ({_progress[matchingReq.requiredItem]}/{matchingReq.requiredAmount}).");

		UpdateUI();

		if (IsComplete())
		{
			Debug.Log("[ShipwreckRepair] All materials delivered. Repair complete!");
			onRepairCompleted?.Invoke();
		}

		return true;
	}

	private void UpdateUI()
	{
		if (requirementsText == null) return;

		var sb = new StringBuilder();
		foreach (var req in requiredMaterials)
		{
			if (req == null || req.requiredItem == null) continue;
			_progress.TryGetValue(req.requiredItem, out var have);
			string itemName = req.requiredItem.name.Replace("(Clone)", "").Trim();
			sb.AppendLine($"{itemName}: {have}/{req.requiredAmount}");
		}
		requirementsText.text = sb.ToString();
	}
}

