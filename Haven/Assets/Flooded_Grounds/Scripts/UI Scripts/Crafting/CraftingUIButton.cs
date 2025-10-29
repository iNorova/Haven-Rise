using UnityEngine;
using UnityEngine.EventSystems;

namespace Haven.CraftingUI
{
	public class CraftingUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private CraftingRecipe _recipe;
		private CraftingService _service;
		private CraftingTooltip _tooltip;

		public void Initialize(CraftingRecipe recipe, CraftingService service, CraftingTooltip tooltip)
		{
			_recipe = recipe;
			_service = service;
			_tooltip = tooltip;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (_tooltip != null && _recipe != null)
			{
				_tooltip.Show(_recipe, _service);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (_tooltip != null) _tooltip.Hide();
		}
	}
}



