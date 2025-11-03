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

		[Header("Layout")]
		[SerializeField] private bool useGridLayout = true;
		[SerializeField] private int gridColumns = 4;
		[SerializeField] private Vector2 cellSize = new Vector2(128, 128);
		[SerializeField] private Vector2 spacing = new Vector2(12, 12);

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

			// Ensure a GridLayoutGroup exists and is configured for rows/columns
			if (useGridLayout)
			{
				var vGroup = listContainer.GetComponent<VerticalLayoutGroup>();
				if (vGroup != null) vGroup.enabled = false; // disable vertical stacking if present

				var grid = listContainer.GetComponent<GridLayoutGroup>();
				if (grid == null)
				{
					grid = listContainer.gameObject.AddComponent<GridLayoutGroup>();
				}
				grid.cellSize = cellSize;
				grid.spacing = spacing;
				grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
				grid.startAxis = GridLayoutGroup.Axis.Horizontal; // fill rows first
				grid.childAlignment = TextAnchor.UpperLeft;
				grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				grid.constraintCount = Mathf.Max(1, gridColumns);
			}

			foreach (var recipe in recipes)
			{
				var btn = Instantiate(recipeButtonPrefab, listContainer);
				_spawnedButtons.Add(btn);


				// Try to set icon image first (child Image named "Icon" preferred), fallback to label text
				Image iconImage = null;
				var images = btn.GetComponentsInChildren<Image>(true);
				foreach (var img in images)
				{
					if (img.gameObject == btn.gameObject) continue; // skip the Button's own background image
					if (img.name.ToLower().Contains("icon")) { iconImage = img; break; }
					if (iconImage == null) iconImage = img; // fallback to first child image
				}

				Sprite iconSprite = null;
				if (recipe != null)
				{
					// 1) Prefer recipe override if provided
					if (recipe.IconOverride != null) iconSprite = recipe.IconOverride;
					// 2) Else try to fetch from output prefab's ItemIconProvider
					else if (recipe.OutputPrefab != null)
					{
						var provider = recipe.OutputPrefab.GetComponent<ItemIconProvider>();
						if (provider != null) iconSprite = provider.icon;
					}
				}

				if (iconImage != null && iconSprite != null)
				{
					iconImage.sprite = iconSprite;
					iconImage.preserveAspect = true;
					iconImage.enabled = true;
					// Optional: hide any TMP label if present
					var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
					if (tmp != null) tmp.gameObject.SetActive(false);
				}
				else
				{
					var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
					if (label != null)
					{
						label.gameObject.SetActive(true);
						label.text = recipe != null && recipe.OutputPrefab != null ? recipe.OutputPrefab.name : "Unknown";
					}
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



