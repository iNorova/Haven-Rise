using System;
using System.Collections.Generic;
using UnityEngine;

namespace Haven.CraftingUI
{
	[CreateAssetMenu(menuName = "Haven/Crafting/Recipe (By Item Name)", fileName = "NewCraftingRecipe")]
	public class CraftingRecipe : ScriptableObject
	{
		[Serializable]
		public struct Requirement
		{
			public string itemName; // Matches ItemIconProvider.itemName on item instances/prefabs
			[Min(1)] public int quantity;
		}

		[Header("Inputs")]
		[SerializeField] private List<Requirement> requirements = new List<Requirement>();

		[Header("Output")]
		[SerializeField] private GameObject outputPrefab;
		[SerializeField, Min(1)] private int outputQuantity = 1;

		public IReadOnlyList<Requirement> Requirements => requirements;
		public GameObject OutputPrefab => outputPrefab;
		public int OutputQuantity => outputQuantity;
	}
}



