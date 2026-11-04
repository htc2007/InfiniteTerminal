using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using InfiniteTerminal.Common;

namespace InfiniteTerminal.Content.Items
{
    /// <summary>
    /// 无线终端接收器：手持时左键点击附近的箱子进行绑定/解绑。
    /// </summary>
    public class Receiver : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 允许在物品提示中显示绑定数量
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(0, 1, 0, 0);

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.noMelee = true;
        }

        public override bool? UseItem(Player player)
        {
            // 绑定逻辑只在本地客户端执行
            if (player.whoAmI != Main.myPlayer)
                return false;

            var modPlayer = player.GetModPlayer<InfiniteTerminalPlayer>();

            Point target = Main.MouseWorld.ToTileCoordinates();
            if (!WorldGen.InWorld(target.X, target.Y, 20))
            {
                Message("Messages.NotChest", Color.OrangeRed);
                return false;
            }

            Tile tile = Framing.GetTileSafely(target.X, target.Y);
            if (!tile.HasTile || !TileID.Sets.BasicChest[tile.TileType])
            {
                Message("Messages.NotChest", Color.OrangeRed);
                return false;
            }

            Point16 topLeft = TileObjectData.TopLeft(target.X, target.Y);

            if (!player.InInteractionRange(topLeft.X, topLeft.Y, TileReachCheckSettings.Simple))
            {
                Message("Messages.OutOfRange", Color.OrangeRed);
                return false;
            }

            int chestIndex = Chest.FindChest(topLeft.X, topLeft.Y);
            if (chestIndex < 0)
            {
                Message("Messages.NoValidChest", Color.OrangeRed);
                return false;
            }

            if (modPlayer.boundChests.Contains(topLeft))
            {
                modPlayer.UnbindChest(topLeft);
                Main.NewText(Language.GetTextValue("Mods.InfiniteTerminal.Messages.Unbound", topLeft.X, topLeft.Y), Color.Orange);
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            else
            {
                modPlayer.BindChest(topLeft);
                Main.NewText(Language.GetTextValue("Mods.InfiniteTerminal.Messages.Bound", topLeft.X, topLeft.Y), Color.LightGreen);
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
            return true;
        }

        private static void Message(string key, Color color)
        {
            Main.NewText(Language.GetTextValue("Mods.InfiniteTerminal." + key), color);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            string help = Language.GetTextValue("Mods.InfiniteTerminal.Tooltips.Receiver");
            tooltips.Add(new TooltipLine(Mod, "InfiniteTerminalHelp", help));

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
