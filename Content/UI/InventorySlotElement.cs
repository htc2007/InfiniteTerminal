using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace InfiniteTerminal.Content.UI
{
    /// <summary>
    /// 显示并操作玩家背包中的一个槽位（主库存/钱币/弹药）。
    /// </summary>
    public class InventorySlotElement : UIElement
    {
        private readonly int _slot;
        private readonly int _context;

        public InventorySlotElement(int slot)
        {
            _slot = slot;
            _context = slot < 50 ? ItemSlot.Context.InventoryItem
                : slot < 54 ? ItemSlot.Context.InventoryCoin
                : ItemSlot.Context.InventoryAmmo;
            Width.Set(42f, 0f);
            Height.Set(42f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rectangle = GetDimensions().ToRectangle();
            ref Item item = ref Main.LocalPlayer.inventory[_slot];

            if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.Handle(ref item, _context);
            }

            ItemSlot.Draw(spriteBatch, ref item, _context, rectangle.TopLeft(), Color.White);
        }
    }
}
