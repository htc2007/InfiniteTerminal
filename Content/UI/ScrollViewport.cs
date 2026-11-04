using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace InfiniteTerminal.Content.UI
{
    /// <summary>
    /// 带裁剪的滚动视口，鼠标滚轮驱动滚动条并同步内部内容的偏移。
    /// </summary>
    public class ScrollViewport : UIElement
    {
        public UIScrollbar ScrollBar;
        public UIElement Content;
        public float ContentHeight;
        private float _lastView = -1f;

        public ScrollViewport()
        {
            OverflowHidden = true;
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (ScrollBar == null)
                return;

            ScrollBar.ViewPosition = MathHelper.Clamp(
                ScrollBar.ViewPosition - evt.ScrollWheelValue,
                0f, Math.Max(0f, ContentHeight - Height.Pixels));
            Sync();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Sync();
        }

        private void Sync()
        {
            if (Content != null && ScrollBar != null && ScrollBar.ViewPosition != _lastView)
            {
                Content.Top.Set(-ScrollBar.ViewPosition, 0f);
                Recalculate();
                _lastView = ScrollBar.ViewPosition;
            }
        }
    }
}
