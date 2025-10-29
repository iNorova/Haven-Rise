using System.Text;
using UnityEngine;
using TMPro;

namespace Haven.CraftingUI
{
	public class CraftingTooltip : MonoBehaviour
	{
		[SerializeField] private CanvasGroup group;
		[SerializeField] private TextMeshProUGUI text;

		private void Awake()
		{
			if (group != null)
			{
				group.alpha = 0f;
				group.blocksRaycasts = false;
				group.interactable = false;
			}
		}

		public void Show(CraftingRecipe recipe, CraftingService service)
		{
			if (group == null || text == null || recipe == null) return;
			var sb = new StringBuilder();
			sb.AppendLine("Requires:");
			foreach (var r in recipe.Requirements)
			{
				int have = service != null ? service.CountByItemName(r.itemName) : 0;
				bool ok = have >= r.quantity;
				sb.AppendLine($"- {(ok ? "<color=green>" : "<color=red>")}{r.itemName} {have}/{r.quantity}</color>");
			}
			text.text = sb.ToString();
			group.alpha = 1f;
		}

		public void Hide()
		{
			if (group != null) group.alpha = 0f;
		}
	}
}



