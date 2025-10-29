using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Haven.CraftingUI
{
	public class CraftingUIPanel : MonoBehaviour
	{
		[Header("Data")]
		[SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();
		[SerializeField] private CraftingService craftingService;

		[Header("UI")]
		[SerializeField] private RectTransform listContainer;
		[SerializeField] private Button recipeButtonPrefab;
		[SerializeField] private CraftingTooltip tooltip;

		private readonly List<Button> _spawnedButtons = new List<Button>();

		private void OnEnable()
		{
			Refresh();
		}

		public void Refresh()
		{
			// Clear old
			foreach (var btn in _spawnedButtons)
			{
				if (btn != null) Destroy(btn.gameObject);
			}
			_spawnedButtons.Clear();

			if (listContainer == null || recipeButtonPrefab == null || craftingService == null) return;

			foreach (var recipe in recipes)
			{
				var btn = Instantiate(recipeButtonPrefab, listContainer);
				_spawnedButtons.Add(btn);

				// Set button label
				var label = btn.GetComponentInChildren<TextMeshProUGUI>();
				if (label != null)
				{
					label.text = recipe != null && recipe.OutputPrefab != null ? recipe.OutputPrefab.name : "Unknown";
				}

				// Interactable state
				btn.interactable = craftingService.CanCraft(recipe);

				// Click to craft
				btn.onClick.AddListener(() =>
				{
					if (craftingService.TryCraft(recipe))
					{
						Refresh(); // Update counts and interactability
					}
				});

				// Hover tooltip
				var hover = btn.gameObject.AddComponent<CraftingUIButton>();
				hover.Initialize(recipe, craftingService, tooltip);
			}
		}
	}
}



