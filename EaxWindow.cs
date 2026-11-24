using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
	// Token: 0x02000005 RID: 5
	internal sealed partial class EaxWindow : PanelBase
	{
		// Token: 0x06000012 RID: 18 RVA: 0x00003944 File Offset: 0x00001B44
		public EaxWindow(FxChain fx) : base(fx, "eax", "EAX Settings", 500, 260)
		{
			Label label = new Label();
			label.Text = "Audio Effects are high quality music enhancement effects created by the powerful audio engine in your sound device.";
			label.SetBounds(10, 6, 480, 34);
			label.BackColor = Color.Transparent;
			this.Body.Controls.Add(label);
			this._on = new CheckBox();
			this._on.Text = "Enable Audio Effects";
			this._on.AutoSize = true;
			this._on.BackColor = Color.Transparent;
			this._on.Location = new Point(12, 46);
			this.Body.Controls.Add(this._on);
			Panel panel = base.Group(this.Body, "", 6, 72, 488, 158);
			base.Cap(panel, "Environment", 10, 10, 120, ContentAlignment.MiddleLeft);
			base.Cap(panel, "Active Preset", 140, 10, 160, ContentAlignment.MiddleLeft);
			this._presetVal = base.Cap(panel, EaxWindow.Presets[0], 140, 26, 170, ContentAlignment.MiddleLeft);
			this._list = new ListBox();
			this._list.SetBounds(140, 46, 170, 100);
			this._list.IntegralHeight = false;
			ListBox.ObjectCollection items = this._list.Items;
			object[] presets = EaxWindow.Presets;
			items.AddRange(presets);
			panel.Controls.Add(this._list);
			this._amtVal = base.Cap(panel, "Effects Amount : 0", 330, 10, 150, ContentAlignment.MiddleLeft);
			base.Cap(panel, "0", 330, 36, 18, ContentAlignment.MiddleLeft);
			this._amt = base.HBar(panel, 350, 30, 108, 0, 100);
			base.Cap(panel, "100", 460, 36, 26, ContentAlignment.MiddleLeft);
			this._on.CheckedChanged += delegate(object <p0>, EventArgs <p1>)
			{
				if (!this.Loading)
				{
					this.Fx.EaxOn = this._on.Checked;
					this.Apply();
				}
			};
			this._list.SelectedIndexChanged += delegate(object <p0>, EventArgs <p1>)
			{
				if (this._list.SelectedIndex >= 0)
				{
					string text = EaxWindow.Presets[this._list.SelectedIndex];
					this._presetVal.Text = text;
					if (!this.Loading)
					{
						this.Fx.EaxPreset = text;
						this.Apply();
					}
				}
			};
			this._amt.ValueChanged += delegate(object <p0>, EventArgs <p1>)
			{
				this._amtVal.Text = "Effects Amount : " + this._amt.Value.ToString();
				if (!this.Loading)
				{
					this.Fx.EaxAmount = this._amt.Value;
					this.Apply();
				}
			};
			this.Reload();
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00003B87 File Offset: 0x00001D87
		private void Apply()
		{
			EngineBoot.SetState(this.Fx.EaxOn, this.Fx.EaxPreset, this.Fx.EaxAmount);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00003BAF File Offset: 0x00001DAF
		protected override void DoDefault()
		{
			this.Fx.EaxOn = false;
			this.Fx.EaxAmount = 0;
			this.Fx.EaxPreset = EaxWindow.Presets[0];
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00003BDC File Offset: 0x00001DDC
		public override void Reload()
		{
			bool loading = this.Loading;
			this.Loading = true;
			try
			{
				this._on.Checked = this.Fx.EaxOn;
				int num = Array.IndexOf<string>(EaxWindow.Presets, this.Fx.EaxPreset);
				if (num < 0)
				{
					num = 0;
				}
				this._list.SelectedIndex = num;
				this._presetVal.Text = EaxWindow.Presets[num];
				this._amt.Value = this.Fx.EaxAmount;
				this._amtVal.Text = "Effects Amount : " + this.Fx.EaxAmount.ToString();
			}
			finally
			{
				this.Loading = loading;
			}
			this.Apply();
		}

		// Token: 0x0400003C RID: 60
		private static readonly string[] Presets = new string[]
		{
			"Bathroom",
			"Church",
			"Concert Hall",
			"Garage",
			"Haunted Cavern",
			"Indoor Arena",
			"Living Room"
		};

		// Token: 0x0400003D RID: 61
		private CheckBox _on;

		// Token: 0x0400003E RID: 62
		private ListBox _list;

		// Token: 0x0400003F RID: 63
		private Label _presetVal;

		// Token: 0x04000040 RID: 64
		private Label _amtVal;

		// Token: 0x04000041 RID: 65
		private CSlider _amt;
	}
}
