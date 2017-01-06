using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;

namespace Leopard
{
    class SelectAttributesButton : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
    {
        public SelectAttributesButton(GH_Component owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();

            System.Drawing.Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            rec0.Height += 22;

            System.Drawing.Rectangle rec1 = rec0;
            rec1.Y = rec1.Bottom - 22;
            rec1.Height = 22;
            rec1.Inflate(-2, -2);

            Bounds = rec0;
            ButtonBounds = rec1;
        }
        private System.Drawing.Rectangle ButtonBounds { get; set; }

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule button;
                if (((SelectComponent)Owner).inputLock)
                    button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, "Locked", 2, 0);
                else
                    button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, "Select", 2, 0);
                button.Render(graphics, Selected, Owner.Locked, false);
                button.Dispose();
            }
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Clicks == 2 && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    ((SelectComponent)Owner).inputLock = !((SelectComponent)Owner).inputLock;
                    if (((SelectComponent)Owner).inputLock == false) //unlock
                    {
                        //((SelectablePreviewComponent)Owner).addSelection = true;
                        ((SelectComponent)Owner).ForceExpireSolution(true, false, true);
                        ((SelectComponent)Owner).SelectStoredPathObj();
                        //((SelectablePreviewComponent)Owner).addSelection = false;
                    }
                    else
                        ((SelectComponent)Owner).ForceExpireSolution(false, false);
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override bool Selected //respond to select event
        {
            get { return base.Selected; }
            set
            {
                base.Selected = value;
                ((SelectComponent)this.Owner).UpdatePreview();
            }
        }
    }
}
