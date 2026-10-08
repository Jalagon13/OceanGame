using TMPro;
using UnityEngine;

namespace OceanGame
{
    public static class Tooltip
    {
        public static TooltipsInstantiateHandler InstantiateHandler;
        public static TooltipReferenceHolder ReferenceHolder;

        public static void HideUI()
        {
            ReferenceHolder.HideUI();
        }
        public static void ShowUI()
        {
            ReferenceHolder.ShowUI();
        }
        public static void ShowNew()
        {
            ClearOldPrefabs();
            ShowUI();
            ReturnBackgroundToDefault();
            ReferenceHolder.Layout.padding = ReferenceHolder.DefaultPadding;
        }
        
        public static void ReturnBackgroundToDefault()
        {
            ReferenceHolder.Background.sprite = ReferenceHolder.DefaultBackgroundSprite;
            ReferenceHolder.Background.color = ReferenceHolder.DefaultBackgroundColor;

        }
        
        public static void ClearOldPrefabs()
        {
            ReferenceHolder.ClearOldPrefabs();
        }
        
        public static void CustomizeBackground(Sprite sprite, Color color)
        {
            ReferenceHolder.Background.sprite = sprite;
            ReferenceHolder.Background.color = color;
        }

        #region  just text
        
        //  If font == null -> will use default font
        public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
        {
            JustTextTT script = InstantiateHandler.InstantiateJustText(customLayout);
            script.icon.sprite = icon;
            script.icon.color = colorOfIcon;

            script.text.font = font == null ? ReferenceHolder.DefaultFont : font;
            script.text.text = text;
            script.text.color = colorOfTheText;
            script.text.fontSize = fontSize;
        }

        public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, float iconScale, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
        {
            JustTextTT script = InstantiateHandler.InstantiateJustText(customLayout);
            script.icon.sprite = icon;
            script.icon.color = colorOfIcon;

            script.text.font = font == null ? ReferenceHolder.DefaultFont : font;
            script.text.text = text;
            script.text.color = colorOfTheText;
            script.text.fontSize = fontSize;
            script.icon.transform.localScale = Vector3.one * iconScale;
        }

        public static void JustText(string text, Color colorOfTheText, TMP_FontAsset font = null, float fontSize = 20, Transform customLayout = null)
        {
            JustText(icon: null, new(0, 0, 0, 0), text, colorOfTheText, font: font, fontSize: fontSize, customLayout: customLayout);
        }

        public static void CraftingRecipeDisplay(RecipeSO recipeSO, Transform customLayout = null, float fontSize = 10, float iconScale = 1)
        {
            JustText($"{recipeSO.OutputItem.ItemName} <br>Recipe ({recipeSO.OutputAmonut}):<br>", Color.white, fontSize: fontSize);

            //for each ingredient in the recipe resource list
            foreach (Ingredient ingredient in recipeSO.Recipe)
            {
                JustTextTT script = InstantiateHandler.InstantiateJustText(customLayout);
                script.icon.sprite = ingredient.Item.DisplayIcon;
                script.icon.transform.localScale = Vector3.one * iconScale;

                script.text.fontSize = fontSize;
                script.text.text = InventoryManager.Instance.GetTotalItemCount(ingredient.Item.GetId()) >= ingredient.Amount ?
                    $"{ingredient.Item.ItemName} ({ingredient.Amount})<br>" :
                    $"<color=red>{ingredient.Item.ItemName} ({ingredient.Amount})</color><br>";
            }
        }

        #endregion
    }
}