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
    /// 支持标题栏拖拽、右下角缩放、放大镜倍率切换。
    /// </summary>
    public class TerminalUIState : UIState
    {
        private UIPanel _panel;
        private ScrollViewport _viewport;
        private UIScrollbar _scrollbar;
        private UIElement _content;
        private UIText _titleText;
        private readonly List<UIElement> _inventorySlots = new();
        private readonly List<UITextPanel<string>> _functionButtons = new();
        private UITextPanel<string> _closeButton;
        private bool _dragging;
        private bool _resizing;
        private float _dragOffsetX;
        private float _dragOffsetY;
        private float _lastPanelW;
        private float _lastPanelH;

        private float Zoom => Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>().panelZoom;
        private float SlotSize => 42f * Zoom;
        // UI 文字整体缩放：根据面板宽度相对默认 480 的比例
        private float UiScale => Math.Clamp(_panel.Width.Pixels / 480f, 0.65f, 1.6f);

        public override void OnInitialize()
        {
            BuildFrame();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_panel == null) return;

            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            CalculatedStyle dim = _panel.GetDimensions();
            Rectangle panelRect = new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height);

            // 标题栏拖拽区域：顶部 36px，排除右侧关闭按钮区域（最后 90px）
            Rectangle dragRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width - 90, 36);
            // 右下角 resize 区域
            Rectangle resizeRect = new Rectangle(panelRect.Right - 18, panelRect.Bottom - 18, 18, 18);

            if (_resizing)
            {
                float newW = Math.Max(360f, Main.mouseX - panelRect.X);
                float newH = Math.Max(400f, Main.mouseY - panelRect.Y);
                newW = Math.Min(newW, Main.screenWidth - panelRect.X);
                newH = Math.Min(newH, Main.screenHeight - panelRect.Y);
                _panel.Width.Set(newW, 0f);
                _panel.Height.Set(newH, 0f);
                modPlayer.panelW = newW;
                modPlayer.panelH = newH;
                if (!Main.mouseLeft) _resizing = false;
            }
            else if (_dragging)
            {
                float newX = Main.mouseX - _dragOffsetX;
                float newY = Main.mouseY - _dragOffsetY;
                newX = Math.Max(0, Math.Min(Main.screenWidth - panelRect.Width, newX));
                newY = Math.Max(0, Math.Min(Main.screenHeight - panelRect.Height, newY));
                _panel.Left.Set(newX, 0f);
                _panel.Top.Set(newY, 0f);
                modPlayer.panelX = newX;
                modPlayer.panelY = newY;
                if (!Main.mouseLeft) _dragging = false;
            }
            else if (Main.mouseLeft)
            {
                if (resizeRect.Contains(Main.mouseX, Main.mouseY))
                {
                    _resizing = true;
                    Main.LocalPlayer.mouseInterface = true;
                }
                else if (dragRect.Contains(Main.mouseX, Main.mouseY))
                {
                    _dragging = true;
                    _dragOffsetX = Main.mouseX - panelRect.X;
                    _dragOffsetY = Main.mouseY - panelRect.Y;
                    Main.LocalPlayer.mouseInterface = true;
                }
            }

            // 面板大小变化时自动重新布局
            if (Math.Abs(panelRect.Width - _lastPanelW) > 1f || Math.Abs(panelRect.Height - _lastPanelH) > 1f)
            {
                _lastPanelW = panelRect.Width;
                _lastPanelH = panelRect.Height;
                RebuildLayout();
            }
        }

        /// <summary>重建全部内部布局（箱子区 + 背包区 + 按钮 + 文字）。</summary>
        private void RebuildLayout()
        {
            // 获取当前面板尺寸
            CalculatedStyle dim = _panel.GetDimensions();
            float panelW = dim.Width;
            float panelH = dim.Height;
            float uiScale = UiScale;

            // 更新标题文字大小（UIText 用 SetText 改 scale）
            _titleText.SetText(_titleText.Text, 0.85f * uiScale, true);

            // 更新按钮尺寸、位置和文字大小
            LayoutButtons(panelW, uiScale);

            // 更新 viewport 和 scrollbar 尺寸
            float sz = SlotSize;
            float inventoryAreaH = 5 * sz + 10f + sz + 20f;
            float viewportW = panelW - 24f - 24f;
            float viewportH = panelH - 80f - inventoryAreaH - 12f;
            _viewport.Width.Set(viewportW, 0f);
            _viewport.Height.Set(viewportH, 0f);
            _content.Width.Set(viewportW - 20f, 0f);
            _scrollbar.Height.Set(viewportH, 0f);

            // 移除背包槽位
            foreach (var slot in _inventorySlots)
                _panel.RemoveChild(slot);
            _inventorySlots.Clear();

            // 重建箱子区
            _content.RemoveAllChildren();
            BuildContents();

            // 重建背包
            BuildInventory();
        }

        /// <summary>根据面板宽度动态计算功能按钮的宽度、位置和文字大小。</summary>
        private void LayoutButtons(float panelW, float uiScale)
        {
            const float gap = 6f;
            const float leftMargin = 8f;
            float available = panelW - leftMargin * 2f;
            // 5 个按钮等宽
            float btnW = (available - 4 * gap) / 5f;
            float btnH = 30f * uiScale;

            for (int i = 0; i < _functionButtons.Count; i++)
            {
                var btn = _functionButtons[i];
                btn.Width.Set(btnW, 0f);
                btn.Height.Set(btnH, 0f);
                btn.Left.Set(leftMargin + i * (btnW + gap), 0f);
                btn.TextScale = 0.4f * uiScale;
                btn.PaddingTop = 4f * uiScale;
                btn.PaddingBottom = 4f * uiScale;
            }

            // 关闭按钮同步
            _closeButton.Height.Set(btnH, 0f);
            _closeButton.TextScale = 0.4f * uiScale;
        }

        /// <summary>每次打开界面时根据当前绑定重建箱子分组。</summary>
        public void BuildContents()
        {
            _content.RemoveAllChildren();
            float y = 0f;
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            float sz = SlotSize;

            foreach (Point16 pos in modPlayer.boundChests)
            {
                int chestIndex = Chest.FindChest(pos.X, pos.Y);
                bool valid = chestIndex >= 0;

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

                var label = new UIText(title, 0.85f * UiScale, false)
                {
                    TextColor = valid ? Color.White : Color.OrangeRed
                };
                label.Left.Set(2f, 0f);
                label.Top.Set(y, 0f);
                _content.Append(label);
                y += 26f * UiScale;

                for (int s = 0; s < Chest.maxItems; s++)
                {
                    var slot = new ChestSlotElement(pos, s, sz);
                    slot.Left.Set((s % 10) * sz, 0f);
                    slot.Top.Set(y + (s / 10) * sz, 0f);
                    _content.Append(slot);
                }
                y += 4 * sz + 14f;
            }

            _content.Height.Set(y, 0f);
            _viewport.ContentHeight = y;
            _scrollbar.SetView(_viewport.Height.Pixels, y);
            _scrollbar.ViewPosition = 0f;

            Recalculate();
        }

        private void BuildFrame()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();

            _panel = new UIPanel();
            float panelW = modPlayer.panelW >= 0 ? modPlayer.panelW : 480f;
            float panelH = modPlayer.panelH >= 0 ? modPlayer.panelH : 706f;
            _panel.Width.Set(panelW, 0f);
            _panel.Height.Set(panelH, 0f);
            _panel.HAlign = 0f;
            _panel.VAlign = 0f;
            float defaultX = Main.screenWidth * 0.14f;
            float defaultY = Main.screenHeight * 0.40f;
            _panel.Left.Set(modPlayer.panelX >= 0 ? modPlayer.panelX : defaultX, 0f);
            _panel.Top.Set(modPlayer.panelY >= 0 ? modPlayer.panelY : defaultY, 0f);
            _panel.SetPadding(12f);
            Append(_panel);

            var title = new UIText(Language.GetTextValue("Mods.InfiniteTerminal.UI.Title"), 0.85f * UiScale, true);
            title.Left.Set(8f, 0f);
            title.Top.Set(4f, 0f);
            _panel.Append(title);
            _titleText = title;

            var close = MakeButton("UI.Close", 80f);
            close.HAlign = 1f;
            close.Top.Set(4f, 0f);
            close.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                ModContent.GetInstance<TerminalUISystem>().ManualClose();
            };
            _panel.Append(close);
            _closeButton = close;

            // 功能按钮行（含放大镜）
            string[] buttonKeys = { "UI.LootAll", "UI.DepositAll", "UI.QuickStack", "UI.Sort" };
            Action[] actions = { LootAll, DepositAll, QuickStack, SortChests };
            for (int i = 0; i < 4; i++)
            {
                var button = MakeButton(buttonKeys[i], 100f);
                button.Top.Set(38f, 0f);
                int captured = i;
                button.OnLeftClick += (evt, el) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    actions[captured]();
                };
                _panel.Append(button);
                _functionButtons.Add(button);
            }

            // 放大镜按钮：显示当前倍率
            var zoomButton = MakeButtonRaw(ZoomLabel(Zoom), 100f);
            zoomButton.Top.Set(38f, 0f);
            zoomButton.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                CycleZoom();
            };
            _panel.Append(zoomButton);
            _functionButtons.Add(zoomButton);

            LayoutButtons(panelW, UiScale);

            // 动态计算 viewport 尺寸
            float sz = SlotSize;
            float inventoryAreaH = 5 * sz + 10f + sz + 20f; // 5行主背包 + 1行钱币/弹药 + 间距
            float viewportW = panelW - 24f - 24f; // padding + scrollbar
            float viewportH = panelH - 80f - inventoryAreaH - 12f;

            _viewport = new ScrollViewport();
            _viewport.Left.Set(8f, 0f);
            _viewport.Top.Set(76f, 0f);
            _viewport.Width.Set(viewportW, 0f);
            _viewport.Height.Set(viewportH, 0f);
            _panel.Append(_viewport);

            _content = new UIElement();
            _content.Width.Set(viewportW - 20f, 0f);
            _content.Left.Set(0f, 0f);
            _content.Top.Set(0f, 0f);
            _viewport.Append(_content);

            _scrollbar = new UIScrollbar();
            _scrollbar.Width.Set(20f, 0f);
            _scrollbar.Height.Set(viewportH, 0f);
            _scrollbar.HAlign = 1f;
            _scrollbar.Top.Set(76f, 0f);
            _panel.Append(_scrollbar);

            _viewport.ScrollBar = _scrollbar;
            _viewport.Content = _content;

            var resizeHandle = new UIText("◢", 0.7f, false)
            {
                TextColor = Color.LightGray
            };
            resizeHandle.HAlign = 1f;
            resizeHandle.VAlign = 1f;
            _panel.Append(resizeHandle);

            BuildInventory();
        }

        private void BuildInventory()
        {
            float sz = SlotSize;
            float topStart = 76f + _viewport.Height.Pixels + 16f;

            for (int s = 0; s < 50; s++)
            {
                var slot = new InventorySlotElement(s, sz);
                slot.Left.Set(8f + (s % 10) * sz, 0f);
                slot.Top.Set(topStart + (s / 10) * sz, 0f);
                _panel.Append(slot);
                _inventorySlots.Add(slot);
            }

            for (int s = 50; s < 58; s++)
            {
                var slot = new InventorySlotElement(s, sz);
                slot.Left.Set(8f + (s - 50) * sz, 0f);
                slot.Top.Set(topStart + 5 * sz + 10f, 0f);
                _panel.Append(slot);
                _inventorySlots.Add(slot);
            }
        }

        private void CycleZoom()
        {
            var mp = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            mp.panelZoom = mp.panelZoom switch
            {
                0.75f => 1.0f,
                1.0f => 1.25f,
                1.25f => 1.5f,
                _ => 0.75f,
            };
            // 更新放大镜按钮文字为当前倍率
            _functionButtons[4].SetText(ZoomLabel(mp.panelZoom));
            RebuildLayout();
        }

        private static string ZoomLabel(float z)
        {
            return z switch
            {
                0.75f => "0.75x",
                1.0f => "1x",
                1.25f => "1.25x",
                1.5f => "1.5x",
                _ => z + "x",
            };
        }

        private UITextPanel<string> MakeButton(string localizationKey, float width)
        {
            return MakeButtonRaw(
                Language.GetTextValue("Mods.InfiniteTerminal." + localizationKey), width);
        }

        private UITextPanel<string> MakeButtonRaw(string text, float width)
        {
            var button = new UITextPanel<string>(text, 0.4f, true);
            button.Width.Set(width, 0f);
            button.Height.Set(30f, 0f);
            button.PaddingTop = 6f;
            button.PaddingBottom = 6f;
            return button;
        }

        private void LootAll()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            Player player = Main.LocalPlayer;
            foreach (int chestIndex in modPlayer.EnumerateValidChestIndices())
            {
                Item[] items = Main.chest[chestIndex].item;
                for (int s = 0; s < Chest.maxItems; s++)
                {
                    Item it = items[s];
                    if (it != null && !it.IsAir)
                        DepositToMainInventory(it, player);
                }
            }
            BuildContents();
        }

        private static void DepositToMainInventory(Item source, Player player)
        {
            if (source == null || source.IsAir) return;

            for (int i = 10; i < 50 && source.stack > 0; i++)
            {
                Item d = player.inventory[i];
                if (d == null || d.IsAir) continue;
                if (!ItemLoader.CanStack(d, source)) continue;
                int space = d.maxStack - d.stack;
                int take = Math.Min(space, source.stack);
                if (take <= 0) continue;
                d.stack += take;
                source.stack -= take;
            }

            for (int i = 10; i < 50 && source.stack > 0; i++)
            {
                if (player.inventory[i] != null && !player.inventory[i].IsAir) continue;
                int take = Math.Min(source.maxStack, source.stack);
                Item placed = new Item();
                placed.SetDefaults(source.type);
                placed.stack = take;
                placed.prefix = source.prefix;
                player.inventory[i] = placed;
                source.stack -= take;
            }
            if (source.stack <= 0) source.TurnToAir();
        }

        private void DepositAll()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            var valid = new List<int>(modPlayer.EnumerateValidChestIndices());
            Player player = Main.LocalPlayer;

            for (int s = 10; s < 50; s++)
            {
                Item it = player.inventory[s];
                if (it == null || it.IsAir) continue;
                if (it.favorited) continue;
                if (it.type == ModContent.ItemType<WirelessTerminal>()) continue;
                if (it.type == ModContent.ItemType<Receiver>()) continue;

                foreach (int chestIndex in valid)
                {
                    if (it.IsAir) break;
                    Item[] chest = Main.chest[chestIndex].item;

                    for (int i = 0; i < Chest.maxItems && !it.IsAir; i++)
                    {
                        Item d = chest[i];
                        if (d == null || d.IsAir) continue;
                        if (d.type != it.type) continue;
                        int space = d.maxStack - d.stack;
                        if (space <= 0) continue;
                        int take = Math.Min(space, it.stack);
                        d.stack += take;
                        it.stack -= take;
                    }

                    for (int i = 0; i < Chest.maxItems && !it.IsAir; i++)
                    {
                        if (chest[i] != null && !chest[i].IsAir) continue;
                        int take = Math.Min(it.maxStack, it.stack);
                        Item placed = new Item();
                        placed.SetDefaults(it.type);
                        placed.stack = take;
                        placed.prefix = it.prefix;
                        chest[i] = placed;
                        it.stack -= take;
                    }
                }

                if (it.stack <= 0) it.TurnToAir();
            }
            BuildContents();
        }

        private void QuickStack()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            var valid = new List<int>(modPlayer.EnumerateValidChestIndices());
            Player player = Main.LocalPlayer;

            for (int s = 10; s < 50; s++)
            {
                Item it = player.inventory[s];
                if (it == null || it.IsAir) continue;
                if (it.favorited) continue;
                if (it.type == ModContent.ItemType<WirelessTerminal>()) continue;
                if (it.type == ModContent.ItemType<Receiver>()) continue;

                foreach (int chestIndex in valid)
                {
                    if (it.IsAir) break;
                    ItemTransfer.QuickStack(it, Main.chest[chestIndex].item, Chest.maxItems);
                }
            }
            BuildContents();
        }

        private void SortChests()
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<InfiniteTerminalPlayer>();
            foreach (int chestIndex in modPlayer.EnumerateValidChestIndices())
                ItemTransfer.SortChestItems(Main.chest[chestIndex].item, Chest.maxItems);
            BuildContents();
        }
    }
}
