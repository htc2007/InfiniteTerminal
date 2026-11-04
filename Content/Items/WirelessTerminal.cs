using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using InfiniteTerminal.Common;
using InfiniteTerminal.Content.UI;

namespace InfiniteTerminal.Content.Items
{
    /// <summary>
    /// 无线终端：使用后打开聚合界面，远程访问所有绑定箱子。
    /// </summary>
    public class WirelessTerminal : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(0, 5, 0, 0);

            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item4;
            Item.autoReuse = false;
            Item.noMelee = true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;

            var modPlayer = player.GetModPlayer<InfiniteTerminalPlayer>();

            if (modPlayer.boundChests.Count == 0)
            {
                Main.NewText(Language.GetTextValue("Mods.InfiniteTerminal.Messages.NoBinding"), Color.OrangeRed);
                return false;
            }

            if (!modPlayer.HasAnyValidChest)
            {
                Main.NewText(Language.GetTextValue("Mods.InfiniteTerminal.Messages.AllInvalid"), Color.OrangeRed);
                return false;
            }

            ModContent.GetInstance<TerminalUISystem>().ShowUI();
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string help = Language.GetTextValue("Mods.InfiniteTerminal.Tooltips.Terminal");
            tooltips.Add(new TooltipLine(Mod, "InfiniteTerminalHelp", help));

            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            string count = Language.GetTextValue("Mods.InfiniteTerminal.Tooltips.BoundCount", modPlayer.boundChests.Count);
            tooltips.Add(new TooltipLine(Mod, "InfiniteTerminalCount", count));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .AddRecipeGroup(RecipeGroupID.IronBar, 2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
