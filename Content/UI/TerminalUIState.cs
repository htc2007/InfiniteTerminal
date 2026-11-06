using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using InfiniteTerminal.Common;
using InfiniteTerminal.Content.Items;

namespace InfiniteTerminal.Content.UI
{
    /// <summary>
    /// 无线终端聚合界面：上方为可滚动的绑定箱子区，下方为玩家背包。
    /// </summary>
    public class TerminalUIState : UIState
    {
        private UIPanel _panel;
        private ScrollViewport _viewport;
        private UIScrollbar _scrollbar;
        private UIElement _content;

        public override void OnInitialize()
        {
            BuildFrame();
        }

        /// <summary>每次打开界面时根据当前绑定重建箱子分组。</summary>
        public void BuildContents()
        {
            _content.RemoveAllChildren();
            float y = 0f;
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();

            foreach (Point16 pos in modPlayer.boundChests)
            {
                int chestIndex = Chest.FindChest(pos.X, pos.Y);
                bool valid = chestIndex >= 0;

                // 每次打开/刷新界面时，对有效箱子先排序，让物品集中在前部槽位可见
                if (valid)
                    ItemTransfer.SortChestItems(Main.chest[chestIndex].item, Chest.maxItems);

                string title;
                if (valid)
                {
                    string chestName = Main.chest[chestIndex].name;
                    title = string.IsNullOrEmpty(chestName)
                        ? Language.GetTextValue("Mods.InfiniteTerminal.UI.ChestTitle", pos.X, pos.Y)
                        : chestName;
                }
                else
                {
                    title = Language.GetTextValue("Mods.InfiniteTerminal.UI.ChestInvalid", pos.X, pos.Y);
                }

                var label = new UIText(title, 0.85f, false)
                {
                    TextColor = valid ? Color.White : Color.OrangeRed
                };
                label.Left.Set(2f, 0f);
                label.Top.Set(y, 0f);
                _content.Append(label);
                y += 26f;

                for (int s = 0; s < Chest.maxItems; s++)
                {
                    var slot = new ChestSlotElement(pos, s);
                    slot.Left.Set((s % 10) * 42f, 0f);
                    slot.Top.Set(y + (s / 10) * 42f, 0f);
                    _content.Append(slot);
                }
                y += 4 * 42f + 14f;
            }

            _content.Height.Set(y, 0f);
            _viewport.ContentHeight = y;
            _scrollbar.SetView(_viewport.Height.Pixels, y);
            _scrollbar.ViewPosition = 0f;

            // 立即重算，保证分组与槽位在首帧就定位正确
            Recalculate();
        }

        private void BuildFrame()
        {
            _panel = new UIPanel();
            _panel.Width.Set(480f, 0f);
            _panel.Height.Set(706f, 0f);
            _panel.HAlign = 0.5f;
            _panel.VAlign = 0.5f;
            _panel.SetPadding(12f);
            Append(_panel);

            var title = new UIText(Language.GetTextValue("Mods.InfiniteTerminal.UI.Title"), 1f, true);
            title.Left.Set(8f, 0f);
            title.Top.Set(6f, 0f);
            _panel.Append(title);

            var close = MakeButton("UI.Close", 80f);
            close.HAlign = 1f;
            close.Top.Set(6f, 0f);
            close.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                ModContent.GetInstance<TerminalUISystem>().HideUI();
            };
            _panel.Append(close);

            string[] buttonKeys = { "UI.LootAll", "UI.DepositAll", "UI.QuickStack", "UI.Sort" };
            Action[] actions = { LootAll, DepositAll, QuickStack, SortChests };
            for (int i = 0; i < 4; i++)
            {
                var button = MakeButton(buttonKeys[i], 100f);
                button.Left.Set(8f + i * 110f, 0f);
                button.Top.Set(34f, 0f);
                int captured = i;
                button.OnLeftClick += (evt, el) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    actions[captured]();
                };
                _panel.Append(button);
            }

            _viewport = new ScrollViewport();
            _viewport.Left.Set(8f, 0f);
            _viewport.Top.Set(70f, 0f);
            _viewport.Width.Set(410f, 0f);
            _viewport.Height.Set(330f, 0f);
            _panel.Append(_viewport);

            _content = new UIElement();
            _content.Width.Set(390f, 0f);
            _content.Left.Set(0f, 0f);
            _content.Top.Set(0f, 0f);
            _viewport.Append(_content);

            _scrollbar = new UIScrollbar();
            _scrollbar.Width.Set(20f, 0f);
            _scrollbar.Height.Set(330f, 0f);
            _scrollbar.HAlign = 1f;
            _scrollbar.Top.Set(70f, 0f);
            _panel.Append(_scrollbar);

            _viewport.ScrollBar = _scrollbar;
            _viewport.Content = _content;

            BuildInventory();
        }

        private void BuildInventory()
        {
            for (int s = 0; s < 50; s++)
            {
                var slot = new InventorySlotElement(s);
                slot.Left.Set(8f + (s % 10) * 42f, 0f);
                slot.Top.Set(418f + (s / 10) * 42f, 0f);
                _panel.Append(slot);
            }

            for (int s = 50; s < 58; s++)
            {
                var slot = new InventorySlotElement(s);
                slot.Left.Set(8f + (s - 50) * 42f, 0f);
                slot.Top.Set(634f, 0f);
                _panel.Append(slot);
            }
        }

        private UITextPanel<string> MakeButton(string localizationKey, float width)
        {
            var button = new UITextPanel<string>(
                Language.GetTextValue("Mods.InfiniteTerminal." + localizationKey),
                0.55f, true);
            button.Width.Set(width, 0f);
            button.Height.Set(30f, 0f);
            button.PaddingTop = 6f;
            button.PaddingBottom = 6f;
            return button;
        }

        /// <summary>全部取出：把所有绑定箱子中的物品移入玩家主库存（不写入钱币/弹药栏）。</summary>
        private void LootAll()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            foreach (int chestIndex in modPlayer.EnumerateValidChestIndices())
            {
                Item[] items = Main.chest[chestIndex].item;
                for (int s = 0; s < Chest.maxItems; s++)
                {
                    Item it = items[s];
                    if (it != null && !it.IsAir)
                        ItemTransfer.Deposit(it, Main.LocalPlayer.inventory, 50);
                }
            }
            BuildContents();
        }

        /// <summary>
        /// 判断背包槽位是否应跳过存入操作：
        /// 已收藏、无线终端、无线终端接收器本身，永不存入箱子；其余槽位（含快捷栏）均可存入。
        /// </summary>
        private static bool ShouldSkipDeposit(int slot, Item it)
        {
            if (it == null || it.IsAir) return true;
            if (it.favorited) return true;               // 已收藏永不存入
            if (it.type == ModContent.ItemType<WirelessTerminal>()) return true;
            if (it.type == ModContent.ItemType<Receiver>()) return true;
            return false;
        }

        /// <summary>全部存入：把背包中可存入的物品移入绑定箱子（收藏/终端/接收器不动，钱币弹药不动）。</summary>
        private void DepositAll()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            var valid = new List<int>(modPlayer.EnumerateValidChestIndices());
            Player player = Main.LocalPlayer;

            for (int s = 0; s < 50; s++)
            {
                Item it = player.inventory[s];
                if (ShouldSkipDeposit(s, it))
                    continue;

                foreach (int chestIndex in valid)
                {
                    if (it.IsAir)
                        break;
                    ItemTransfer.Deposit(it, Main.chest[chestIndex].item, Chest.maxItems);
                }
            }
            BuildContents();
        }

        /// <summary>快速堆叠：仅把箱子中已有同类的物品存入（跳过收藏/终端/接收器）。</summary>
        private void QuickStack()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            var valid = new List<int>(modPlayer.EnumerateValidChestIndices());
            Player player = Main.LocalPlayer;

            for (int s = 0; s < 50; s++)
            {
                Item it = player.inventory[s];
                if (ShouldSkipDeposit(s, it))
                    continue;

                foreach (int chestIndex in valid)
                {
                    if (it.IsAir)
                        break;
                    ItemTransfer.QuickStack(it, Main.chest[chestIndex].item, Chest.maxItems);
                }
            }
            BuildContents();
        }

        /// <summary>整理：对每个绑定箱子分别执行排序，并刷新界面。</summary>
        private void SortChests()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            foreach (int chestIndex in modPlayer.EnumerateValidChestIndices())
                ItemTransfer.SortChestItems(Main.chest[chestIndex].item, Chest.maxItems);
            BuildContents();
        }
    }
}
