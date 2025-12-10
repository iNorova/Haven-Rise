using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI drop target for shipwreck repair. Place on the shipwreck panel or drop area.
/// Requires ShipwreckRepairManager and InventorySystem references.
/// </summary>
public class ShipwreckDropTarget : MonoBehaviour, IDropHandler
{
	[SerializeField] private ShipwreckRepairManager shipwreck;

	public void OnDrop(PointerEventData eventData)
	{
		var sourceSlot = InventorySlot.itemBeingDraggedSlot;
		if (sourceSlot == null)
		{
			return;
		}

		// Clean up the dragged icon using the public method
		sourceSlot.CleanupDraggedIcon();

		// Get the dragged item GameObject
		var item = sourceSlot.GetItem();
		if (item == null || shipwreck == null || InventorySystem.Instance == null)
		{
			InventorySlot.itemBeingDraggedSlot = null;
			return;
		}

		// Try to consume into shipwreck (pass the GameObject directly)
		if (shipwreck.TryAddMaterial(item))
		{
			InventorySystem.Instance.ConsumeOneFromSlot(sourceSlot);
		}

		// Reset the dragged slot reference
		InventorySlot.itemBeingDraggedSlot = null;
	}
}

