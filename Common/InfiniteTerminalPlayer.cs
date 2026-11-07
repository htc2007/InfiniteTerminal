using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using InfiniteTerminal.Content.Items;

namespace InfiniteTerminal.Common
{
    /// <summary>
    /// 保存玩家的箱子绑定数据，并向配方系统提供绑定箱子内的材料。
    /// </summary>
    public class InfiniteTerminalPlayer : ModPlayer
    {
        /// <summary>已绑定箱子的左上角瓷砖坐标。</summary>
        public List<Point16> boundChests = new();

        /// <summary>是否已发放过开局物品；持久化，保证整套只发一次。</summary>
        public bool starterKitGiven = false;

        /// <summary>终端面板位置（像素），-1 表示未设置用默认位置。</summary>
        public float panelX = -1f;
        public float panelY = -1f;

        /// <summary>终端面板大小（像素），-1 表示用默认大小。</summary>
        public float panelW = -1f;
        public float panelH = -1f;

        /// <summary>格子显示倍率，默认 1.0。</summary>
        public float panelZoom = 1.0f;

        public override void Initialize()
        {
            boundChests = new List<Point16>();
            starterKitGiven = false;
            panelX = -1f;
            panelY = -1f;
            panelW = -1f;
            panelH = -1f;
            panelZoom = 1.0f;
        }

        public override void OnEnterWorld()
        {
            // 仅本地玩家、且此前未发放过时，发放一套开局物品
            if (Player.whoAmI != Main.myPlayer || starterKitGiven)
                return;

            GiveStarterItem(ModContent.ItemType<Receiver>());
            GiveStarterItem(ModContent.ItemType<WirelessTerminal>());
            starterKitGiven = true;
        }

        /// <summary>把一个物品放入背包空槽；背包已满则掉落在玩家身上，保证不丢失。</summary>
        private void GiveStarterItem(int type)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i].IsAir)
                {
                    Player.inventory[i].SetDefaults(type);
                    return;
                }
            }

            Item.NewItem(Player.GetSource_GiftOrReward(), Player.Center, type, 1);
        }

        /// <summary>判断指定坐标的箱子是否仍然存在。</summary>
        public bool IsChestValid(Point16 pos) => Chest.FindChest(pos.X, pos.Y) >= 0;

        /// <summary>枚举所有仍然有效的绑定箱子在 Main.chest 中的索引。</summary>
        public IEnumerable<int> EnumerateValidChestIndices()
        {
            foreach (Point16 pos in boundChests)
            {
                int index = Chest.FindChest(pos.X, pos.Y);
                if (index >= 0)
                    yield return index;
            }
        }

        public bool HasAnyValidChest => EnumerateValidChestIndices().Any();

        /// <summary>无线终端是否在玩家背包（含热栏）中。</summary>
        public bool HasTerminalInInventory()
        {
            int terminalType = ModContent.ItemType<WirelessTerminal>();
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i].type == terminalType)
                    return true;
            }
            return false;
        }

        /// <summary>绑定一个箱子，返回是否为新绑定；已存在则返回 false。</summary>
        public bool BindChest(Point16 topLeft)
        {
            if (boundChests.Contains(topLeft))
                return false;

            boundChests.Add(topLeft);
            return true;
        }

        /// <summary>解绑一个箱子，返回是否确实移除了绑定。</summary>
        public bool UnbindChest(Point16 topLeft) => boundChests.Remove(topLeft);

        public override void SaveData(TagCompound tag)
        {
            // 用 List<Vector2> 序列化绑定坐标，比嵌套 TagCompound 更稳定，避免重载后数据丢失
            var list = new List<Vector2>(boundChests.Count);
            foreach (Point16 p in boundChests)
                list.Add(new Vector2(p.X, p.Y));
            tag["boundChests"] = list;
            tag["starterKitGiven"] = starterKitGiven;
            tag["panelX"] = panelX;
            tag["panelY"] = panelY;
            tag["panelW"] = panelW;
            tag["panelH"] = panelH;
            tag["panelZoom"] = panelZoom;
        }

        public override void LoadData(TagCompound tag)
        {
            boundChests = new List<Point16>();
            if (tag.TryGet("boundChests", out List<Vector2> list) && list != null)
            {
                foreach (Vector2 v in list)
                    boundChests.Add(new Point16((int)v.X, (int)v.Y));
            }
            starterKitGiven = tag.ContainsKey("starterKitGiven") && tag.GetBool("starterKitGiven");
            panelX = tag.ContainsKey("panelX") ? tag.GetFloat("panelX") : -1f;
            panelY = tag.ContainsKey("panelY") ? tag.GetFloat("panelY") : -1f;
            panelW = tag.ContainsKey("panelW") ? tag.GetFloat("panelW") : -1f;
            panelH = tag.ContainsKey("panelH") ? tag.GetFloat("panelH") : -1f;
            panelZoom = tag.ContainsKey("panelZoom") ? tag.GetFloat("panelZoom") : 1.0f;
        }

        /// <summary>
        /// 无线终端在背包中时，把所有绑定箱子内的物品作为可制作材料暴露给配方系统。
        /// 返回的是箱子 Item 的直接引用，框架消耗材料时修改的就是箱子内的原物品。
        /// </summary>
        public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback itemConsumedCallback)
        {
            itemConsumedCallback = null;

            if (!HasTerminalInInventory() || !HasAnyValidChest)
                return null;

            return EnumerateBoundItems();
        }

        private IEnumerable<Item> EnumerateBoundItems()
        {
            foreach (int chestIndex in EnumerateValidChestIndices())
            {
                Chest chest = Main.chest[chestIndex];
                for (int slot = 0; slot < Chest.maxItems; slot++)
                {
                    Item item = chest.item[slot];
                    if (item != null && !item.IsAir)
                        yield return item;
                }
            }
        }
    }
}
