using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfiniteTerminal.Content.UI
{
    /// <summary>
    /// 物品在玩家背包与绑定箱子之间转移的工具方法。
    /// </summary>
    public static class ItemTransfer
    {
        /// <summary>
        /// 把 source 中的物品尽可能移入 dest：先堆叠到同类槽，再放入空槽。
        /// 返回实际移动的数量。
        /// </summary>
        public static int Deposit(Item source, Item[] dest, int destLen)
        {
            if (source == null || source.IsAir)
                return 0;

            int moved = 0;

            // 第一遍：堆叠到已有的同类槽
            for (int i = 0; i < destLen && source.stack > 0; i++)
            {
                Item d = dest[i];
                if (d == null || d.IsAir)
                    continue;
                if (!ItemLoader.CanStack(d, source))
                    continue;

                int space = d.maxStack - d.stack;
                int take = Math.Min(space, source.stack);
                if (take <= 0)
                    continue;

                d.stack += take;
                source.stack -= take;
                moved += take;
            }

            // 第二遍：放入空槽
            for (int i = 0; i < destLen && source.stack > 0; i++)
            {
                if (dest[i] != null && !dest[i].IsAir)
                    continue;

                int take = Math.Min(source.maxStack, source.stack);
                Item placed = source.Clone();
                placed.stack = take;
                dest[i] = placed;
                source.stack -= take;
                moved += take;

                if (source.stack <= 0)
                    source.TurnToAir();
            }

            return moved;
        }

        /// <summary>
        /// 快速堆叠：仅当目标容器中已存在同类物品时才移动，否则保持不动。
        /// </summary>
        public static int QuickStack(Item source, Item[] dest, int destLen)
        {
            if (source == null || source.IsAir)
                return 0;

            bool sameTypeExists = false;
            for (int i = 0; i < destLen; i++)
            {
                Item d = dest[i];
                if (d != null && !d.IsAir && d.type == source.type)
                {
                    sameTypeExists = true;
                    break;
                }
            }

            if (!sameTypeExists)
                return 0;

            return Deposit(source, dest, destLen);
        }

        /// <summary>
        /// 对一个箱子的物品排序：先按大类（钱币/装备/武器/消耗品/放置物/其他），
        /// 再按物品类型、前缀、堆叠数量排序，空槽统一移到末尾。不丢失任何物品。
        /// </summary>
        public static void SortChestItems(Item[] items, int len)
        {
            var occupied = new List<Item>();
            for (int i = 0; i < len; i++)
            {
                if (items[i] != null && !items[i].IsAir)
                    occupied.Add(items[i]);
            }

            occupied.Sort((a, b) =>
            {
                int categoryCompare = Category(a).CompareTo(Category(b));
                if (categoryCompare != 0)
                    return categoryCompare;
                if (a.type != b.type)
                    return a.type.CompareTo(b.type);
                if (a.prefix != b.prefix)
                    return a.prefix.CompareTo(b.prefix);
                return b.stack.CompareTo(a.stack);
            });

            for (int i = 0; i < len; i++)
                items[i] = i < occupied.Count ? occupied[i] : new Item();
        }

        private const int NoPlaceable = -1; // createTile/createWall 为 -1 表示不创建任何瓷砖/墙

        private static int Category(Item item)
        {
            if (ItemID.Sets.CommonCoin[item.type])
                return 0;
            if (item.accessory || item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0)
                return 1;
            if (item.damage > 0 || item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.fishingPole > 0)
                return 2;
            if (item.consumable || item.buffType > 0 || item.potion || item.healLife > 0 || item.healMana > 0)
                return 3;
            if (item.createTile != NoPlaceable || item.createWall != NoPlaceable)
                return 4;
            return 5;
        }
    }
}
