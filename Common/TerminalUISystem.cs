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
        }

        public void HideUI()
        {
            _userInterface.SetState(null);
            _state = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (IsOpen)
            {
                Player player = Main.LocalPlayer;
                if (player.dead || !player.active)
                {
                    HideUI();
                }
                else
                {
                    // 物品栏快捷键（默认 Esc）按下边沿时关闭界面
                    if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)
                        && !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                    {
                        HideUI();
                        return;
                    }
                    _userInterface.Update(gameTime);
                }
            }
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
        }

        public override void PreSaveAndQuit()
        {
            if (IsOpen)
                HideUI();
        }
    }
}
