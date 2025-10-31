using System.Collections.Generic;
using UnityEngine;

namespace Haven.CraftingUI
{
	public class CraftingService : MonoBehaviour
	{
		[SerializeField] private InventoryManager inventoryManager;
		[SerializeField] private HotbarManager hotbarManager;

		public void Configure(InventoryManager inv, HotbarManager hotbar)
		{
			inventoryManager = inv;
			hotbarManager = hotbar;
		}

		public bool CanCraft(CraftingRecipe recipe)
		{
			if (recipe == null || inventoryManager == null || hotbarManager == null) return false;
			foreach (var req in recipe.Requirements)
			{
				int available = CountByItemName(req.itemName);
				if (available < req.quantity) return false;
			}
			return true;
		}

		public bool TryCraft(CraftingRecipe recipe)
		{
			if (!CanCraft(recipe)) return false;

			// Consume inputs by searching inventory first, then hotbar
			foreach (var req in recipe.Requirements)
			{
				int toConsume = req.quantity;
				// Inventory
				toConsume -= ConsumeFromInventory(req.itemName, toConsume);
				if (toConsume > 0)
				{
					ConsumeFromHotbar(req.itemName, toConsume);
				}
			}

			// Grant outputs
			for (int i = 0; i < recipe.OutputQuantity; i++)
			{
				if (recipe.OutputPrefab == null) continue;
				var instance = Instantiate(recipe.OutputPrefab);
				var iconProvider = instance.GetComponent<ItemIconProvider>();
				if (iconProvider != null) instance.name = iconProvider.itemName;
				if (!inventoryManager.AddItem(instance))
				{
					// If inventory full, try add to an empty hotbar slot
					int emptyHotbar = FindFirstEmptyHotbarSlot();
					if (emptyHotbar != -1)
					{
						hotbarManager.SetItem(emptyHotbar, instance);
						instance.transform.SetParent(hotbarManager.handHolder);
						instance.transform.localPosition = Vector3.zero;
						instance.transform.localRotation = Quaternion.identity;
						instance.SetActive(false);
					}
					else
					{
						Destroy(instance);
						Debug.Log("No space in inventory or hotbar for crafted item.");
					}
				}
			}
			
			// CRITICAL: Update inventory UI immediately after crafting to fix white slots
			if (inventoryManager != null)
			{
				inventoryManager.UpdateInventoryUI();
			}
			
			// Also update hotbar UI in case items were consumed from hotbar
			if (hotbarManager != null)
			{
				hotbarManager.UpdateHotbarUI();
			}
			
			return true;
		}

		public int CountByItemName(string targetName)
		{
			if (string.IsNullOrEmpty(targetName)) return 0;
			int count = 0;
			// Inventory
			for (int i = 0; ; i++)
			{
				var go = inventoryManager.GetItem(i);
				if (go == null && i >= GetInventoryLength() - 1) break;
				if (go != null && MatchesName(go, targetName)) count++;
			}
			// Hotbar
			for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
			{
				var go = hotbarManager.GetItem(i);
				if (go != null && MatchesName(go, targetName)) count++;
			}
			return count;
		}

		private int ConsumeFromInventory(string targetName, int quantity)
		{
			int consumed = 0;
			for (int i = 0; i < GetInventoryLength() && consumed < quantity; i++)
			{
				var go = inventoryManager.GetItem(i);
				if (go != null && MatchesName(go, targetName))
				{
					var removed = inventoryManager.RemoveItem(i);
					if (removed != null) Destroy(removed);
					consumed++;
				}
			}
			return consumed;
		}

		private int ConsumeFromHotbar(string targetName, int quantity)
		{
			int consumed = 0;
			for (int i = 0; i < hotbarManager.hotbarSlots.Length && consumed < quantity; i++)
			{
				var go = hotbarManager.GetItem(i);
				if (go != null && MatchesName(go, targetName))
				{
					// Remove from hotbar and destroy instance
					hotbarManager.SetItem(i, null);
					Destroy(go);
					consumed++;
				}
			}
			return consumed;
		}

		private bool MatchesName(GameObject go, string targetName)
		{
			var iconProvider = go.GetComponent<ItemIconProvider>();
			if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
			{
				return iconProvider.itemName == targetName;
			}
			return go.name == targetName;
		}

		private int GetInventoryLength()
		{
			// InventoryManager doesn't expose length; infer from assigned slots
			return inventoryManager != null && inventoryManager.inventorySlots != null
				? inventoryManager.inventorySlots.Length
				: 0;
		}

		private int FindFirstEmptyHotbarSlot()
		{
			for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
			{
				if (hotbarManager.GetItem(i) == null) return i;
			}
			return -1;
		}
	}
}



