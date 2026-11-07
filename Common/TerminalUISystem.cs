using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using InfiniteTerminal.Content.UI;

namespace InfiniteTerminal.Common
{
    /// <summary>
    /// 管理无线终端聚合界面的生命周期与绘制（仅客户端）。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class TerminalUISystem : ModSystem
    {
        private UserInterface _userInterface;
        private TerminalUIState _state;
        private bool _manualClosed;
        private bool _lastInventoryOpen;

        public bool IsOpen => _userInterface?.CurrentState != null;

        public override void PostSetupContent()
        {
            _userInterface = new UserInterface();
        }

        public void ShowUI()
        {
            _state = new TerminalUIState();
            _state.Activate();
            _state.BuildContents();
            _userInterface.SetState(_state);
            _manualClosed = false;
        }

        public void HideUI()
        {
            _userInterface.SetState(null);
            _state = null;
        }

        /// <summary>点击关闭按钮：暂时关闭面板，重新打开物品栏或点击终端可恢复。</summary>
        public void ManualClose()
        {
            _manualClosed = true;
            HideUI();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            Player player = Main.LocalPlayer;
            if (player.dead || !player.active)
            {
                if (IsOpen) HideUI();
                _lastInventoryOpen = Main.playerInventory;
                return;
            }

            var modPlayer = player.GetModPlayer<InfiniteTerminalPlayer>();

            // 检测物品栏从关闭→打开的边沿：重置手动关闭标志
            if (Main.playerInventory && !_lastInventoryOpen)
            {
                _manualClosed = false;
            }
            _lastInventoryOpen = Main.playerInventory;

            // 打开背包时：若背包内有无线终端且有有效绑定箱子，且未被手动关闭，自动弹出
            if (Main.playerInventory && !IsOpen && !_manualClosed)
            {
                if (modPlayer.HasTerminalInInventory() && modPlayer.HasAnyValidChest)
                    ShowUI();
            }
            // 关闭背包时：自动关闭终端界面
            else if (!Main.playerInventory && IsOpen)
            {
                HideUI();
            }

            if (IsOpen)
                _userInterface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "InfiniteTerminal: Terminal",
                    delegate
                    {
                        if (IsOpen)
                            _userInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void OnWorldUnload()
        {
            if (IsOpen)
                HideUI();
            _manualClosed = false;
            _lastInventoryOpen = false;
        }

        public override void PreSaveAndQuit()
        {
            if (IsOpen)
                HideUI();
        }
    }
}
