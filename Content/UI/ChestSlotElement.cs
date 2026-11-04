using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.UI;

namespace InfiniteTerminal.Content.UI
{
    /// <summary>
    /// 包装原版 ItemSlot，显示并操作指定绑定箱子中的一个槽位。
    /// 箱子失效时绘制灰色空槽且不响应交互。
    /// </summary>
    public class ChestSlotElement : UIElement
    {
        private readonly Point16 _chestPos;
        private readonly int _slot;
        private static Item _emptyPlaceholder;

        public ChestSlotElement(Point16 chestPos, int slot)
        {
            _chestPos = chestPos;
            _slot = slot;
            Width.Set(42f, 0f);
            Height.Set(42f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rectangle = GetDimensions().ToRectangle();
            int chestIndex = Chest.FindChest(_chestPos.X, _chestPos.Y);

            if (chestIndex < 0)
            {
                _emptyPlaceholder ??= new Item();
                ItemSlot.Draw(spriteBatch, ref _emptyPlaceholder, ItemSlot.Context.ChestItem,
                    rectangle.TopLeft(), Color.Gray * 0.55f);
                return;
            }

            ref Item item = ref Main.chest[chestIndex].item[_slot];

            if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.Handle(ref item, ItemSlot.Context.ChestItem);
            }

            ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.ChestItem,
                rectangle.TopLeft(), Color.White);
        }
    }
}
