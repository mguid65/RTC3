﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using static RTC.RTC_Unispec;

namespace RTC
{
	public partial class RTC_Core_Form : Form, IAutoColorize // replace by : UserControl for panel
	{
		public Form previousForm = null;
		public Form activeForm = null;

		private const int CP_NOCLOSE_BUTTON = 0x200;

		protected override CreateParams CreateParams
		{
			get
			{
				if (RTC_Core.isStandalone)
					return base.CreateParams;

				CreateParams myCp = base.CreateParams;
				myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
				return myCp;
			}
		}

		public bool AutoCorrupt
		{
			get
			{
				return (bool)RTCSpec[RTCSPEC.CORE_AUTOCORRUPT.ToString()];
			}
			set
			{
				if (value)
					btnAutoCorrupt.Text = "Stop Auto-Corrupt";
				else
					btnAutoCorrupt.Text = "Start Auto-Corrupt";

				RTCSpec.Update(RTCSPEC.CORE_AUTOCORRUPT.ToString(), value);
			}
		}

		public RTC_Core_Form()
		{
			InitializeComponent();

			if (RTC_Core.isStandalone)
				pnAutoKillSwitch.Visible = true;
		}

		public void btnManualBlast_Click(object sender, EventArgs e)
		{
			RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.ASYNCBLAST));
		}

		public void btnAutoCorrupt_Click(object sender, EventArgs e)
		{
			if (btnAutoCorrupt.ForeColor == Color.Silver)
				return;

			this.AutoCorrupt = !this.AutoCorrupt;
			RTC_Unispec.RTCSpec.Update(RTCSPEC.STEP_RUNBEFORE.ToString(), true);
		}

		private void RTC_Form_Load(object sender, EventArgs e)
		{
			btnLogo.Text = "   Version " + RTC_Core.RtcVersion;

			if (!RTC_Params.IsParamSet("DISCLAIMER_READ"))
			{
				MessageBox.Show(File.ReadAllText(RTC_Core.rtcDir + "\\LICENSES\\DISCLAIMER.TXT").Replace("[ver]", RTC_Core.RtcVersion), "RTC", MessageBoxButtons.OK, MessageBoxIcon.Information);
				RTC_Params.SetParam("DISCLAIMER_READ");
			}

			RTC_Core.DownloadProblematicProcesses();
			RTC_Core.CheckForProblematicProcesses();

			if (RTC_Core.isStandalone)
			{
				GhostBoxInvisible(btnEasyMode);
				GhostBoxInvisible(btnEngineConfig);
				GhostBoxInvisible(btnGlitchHarvester);
				GhostBoxInvisible(btnStockpilePlayer);
				GhostBoxInvisible(btnRTCMultiplayer);
				GhostBoxInvisible(btnManualBlast);
				GhostBoxInvisible(btnAutoCorrupt);
				GhostBoxInvisible(pnCrashProtection);
			}
			else
			{
				btnEngineConfig_Click(sender, e);
				pnCrashProtectionUnavailable.Visible = true;
			}
		}

		private void btnGlitchHarvester_Click(object sender, EventArgs e)
		{
			if (!btnGlitchHarvester.Text.Contains("○"))
				btnGlitchHarvester.Text = "○ " + btnGlitchHarvester.Text;

			S.GET<RTC_GlitchHarvester_Form>().Show();
			S.GET<RTC_GlitchHarvester_Form>().Focus();
		}

		private void RTC_Form_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!RTC_Core.isStandalone && e.CloseReason != CloseReason.FormOwnerClosing)
			{
				e.Cancel = true;
				this.Hide();
				return;
			}
			else if (RTC_Core.isStandalone)
			{
				if (RTC_StockpileManager.UnsavedEdits && !RTC_Core.isClosing && MessageBox.Show("You have unsaved edits in the Glitch Harvester Stockpile. \n\n Are you sure you want to close RTC without saving?", "Unsaved edits in Stockpile", MessageBoxButtons.YesNo) == DialogResult.No)
				{
					e.Cancel = true;
					return;
				}

				if (RTC_Core.RemoteRTC_SupposedToBeConnected)
				{
					RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.REMOTE_EVENT_CLOSEBIZHAWK));
					Thread.Sleep(1000);
				}

				RTC_Core.CloseAllRtcForms();
			}
		}

		public void btnEasyModeCurrent_Click(object sender, EventArgs e)
		{
			StartEasyMode(false);
		}

		public void btnEasyModeTemplate_Click(object sender, EventArgs e)
		{
			StartEasyMode(true);
		}

		public void StartEasyMode(bool useTemplate)
		{
			if (RTC_Core.isStandalone && !S.GET<RTC_Core_Form>().cbUseGameProtection.Checked)
				S.GET<RTC_Core_Form>().cbUseGameProtection.Checked = true;


			if (useTemplate)
			{
				//Put Console templates HERE
				string thisSystem = (string)RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.REMOTE_DOMAIN_SYSTEM), true);

				switch (thisSystem)
				{
					case "NES":     //Nintendo Entertainment system
						RTC_Core.SetEngineByName("Nightmare Engine");
						S.GET<RTC_GeneralParameters_Form>().Intensity = 2;
						S.GET<RTC_GeneralParameters_Form>().ErrorDelay = 1;
						break;

					case "GB":      //Gameboy
					case "GBC":     //Gameboy Color
						RTC_Core.SetEngineByName("Nightmare Engine");
						S.GET<RTC_GeneralParameters_Form>().Intensity = 1;
						S.GET<RTC_GeneralParameters_Form>().ErrorDelay = 4;
						break;

					case "SNES":    //Super Nintendo
						RTC_Core.SetEngineByName("Nightmare Engine");
						S.GET<RTC_GeneralParameters_Form>().Intensity = 1;
						S.GET<RTC_GeneralParameters_Form>().ErrorDelay = 2;
						break;

					case "GBA":     //Gameboy Advance
						RTC_Core.SetEngineByName("Nightmare Engine");
						S.GET<RTC_GeneralParameters_Form>().Intensity = 1;
						S.GET<RTC_GeneralParameters_Form>().ErrorDelay = 1;
						break;

					case "N64":     //Nintendo 64
						RTC_Core.SetEngineByName("Vector Engine");
						S.GET<RTC_GeneralParameters_Form>().Intensity = 75;
						S.GET<RTC_GeneralParameters_Form>().ErrorDelay = 1;
						break;

					case "SG":      //Sega SG-1000
					case "GG":      //Sega GameGear
					case "SMS":     //Sega Master System
					case "GEN":     //Sega Genesis and CD
					case "PCE":     //PC-Engine / Turbo Grafx
					case "PSX":     //Sony Playstation 1
					case "A26":     //Atari 2600
					case "A78":     //Atari 7800
					case "LYNX":    //Atari Lynx
					case "INTV":    //Intellivision
					case "PCECD":   //related to PC-Engine / Turbo Grafx
					case "SGX":     //related to PC-Engine / Turbo Grafx
					case "TI83":    //Ti-83 Calculator
					case "WSWAN":   //Wonderswan
					case "C64":     //Commodore 64
					case "Coleco":  //Colecovision
					case "SGB":     //Super Gameboy
					case "SAT":     //Sega Saturn
					case "DGB":
						MessageBox.Show("WARNING: No Easy-Mode template was made for this system. Please configure it manually and use the current settings.");
						return;

						//TODO: Add more domains for systems like gamegear, atari, turbo graphx
				}
			}

			this.AutoCorrupt = true;
		}

		private void cbUseGameProtection_CheckedChanged(object sender, EventArgs e)
		{
			if (cbUseGameProtection.Checked)
			{
				RTC_GameProtection.Start();
			}
			else
			{
				RTC_GameProtection.Stop();

				RTCSpec.Update(RTCSPEC.STOCKPILE_BACKUPEDSTATE.ToString(), null);
				RTC_StockpileManager.AllBackupStates.Clear();
				btnGpJumpBack.Visible = false;
				btnGpJumpNow.Visible = false;
			}
		}

		private void btnLogo_MouseClick(object sender, MouseEventArgs e)
		{
			if (RTC_Core.isStandalone)
				ShowPanelForm(S.GET<RTC_ConnectionStatus_Form>(), false);
		}

		private void btnEasyMode_MouseDown(object sender, MouseEventArgs e)
		{
			Point locate = new Point(((Button)sender).Location.X + e.Location.X, ((Button)sender).Location.Y + e.Location.Y);

			ContextMenuStrip easyButtonMenu = new ContextMenuStrip();
			easyButtonMenu.Items.Add("Start with Recommended Settings", null, new EventHandler(btnEasyModeTemplate_Click));
			easyButtonMenu.Items.Add(new ToolStripSeparator());
			//EasyButtonMenu.Items.Add("Watch a tutorial video", null, new EventHandler((ob,ev) => Process.Start("https://www.youtube.com/watch?v=sIELpn4-Umw"))).Enabled = false;
			easyButtonMenu.Items.Add("Open the online wiki", null, new EventHandler((ob, ev) => Process.Start("https://corrupt.wiki/")));
			easyButtonMenu.Show(this, locate);
		}

		public void GhostBoxInvisible(Control ctrl)
		{
			Panel pn = new Panel();
			Color col = ctrl.Parent.BackColor;
			pn.BorderStyle = BorderStyle.None;
			pn.BackColor = col.ChangeColorBrightness(-0.10f);
			pn.Tag = "GHOST";
			pn.Location = ctrl.Location;
			pn.Size = ctrl.Size;
			ctrl.Parent.Controls.Add(pn);
			ctrl.Visible = false;
		}

		public void RemoveGhostBoxes()
		{
			List<Control> controlsToBeRemoved = new List<Control>();
			foreach (Control ctrl in Controls)
				if (ctrl.Tag != null && ctrl.Tag.ToString() == "GHOST")
					controlsToBeRemoved.Add(ctrl);

			foreach (Control ctrl in controlsToBeRemoved)
				Controls.Remove(ctrl);
		}

		public void ShowPanelForm(Form frm, bool hideButtons = true)
		{
			if (hideButtons && frm is RTC_ConnectionStatus_Form)
			{
				GhostBoxInvisible(btnEasyMode);
				GhostBoxInvisible(btnEngineConfig);
				GhostBoxInvisible(btnGlitchHarvester);
				GhostBoxInvisible(btnRTCMultiplayer);
				GhostBoxInvisible(btnStockpilePlayer);
				GhostBoxInvisible(btnAutoCorrupt);
				GhostBoxInvisible(btnManualBlast);
			}

			btnEngineConfig.Text = btnEngineConfig.Text.Replace("● ", "");
			btnStockpilePlayer.Text = btnStockpilePlayer.Text.Replace("● ", "");
			btnRTCMultiplayer.Text = btnRTCMultiplayer.Text.Replace("● ", "");

			Button btn = null;

			if (frm is RTC_EngineConfig_Form)
				btn = btnEngineConfig;
			else if (frm is RTC_StockpilePlayer_Form)
				btn = btnStockpilePlayer;
			else if (frm is RTC_Multiplayer_Form)
				btn = btnRTCMultiplayer;

			if (btn != null)
				btn.Text = "● " + btn.Text;

			if (!this.Controls.Contains(frm))
			{
				if (activeForm != null)
					activeForm.Hide();

				Controls.Remove(activeForm);
				frm.TopLevel = false;
				this.Controls.Add(frm);
				frm.Dock = DockStyle.Left;
				frm.SendToBack();
				frm.BringToFront();
				if(activeForm?.GetType() != typeof(RTC_ConnectionStatus_Form))
					previousForm = activeForm;
				activeForm = frm;
				frm.Show();
			}

			if (!(frm is RTC_ConnectionStatus_Form) || !hideButtons)
			{
				if (!(frm is RTC_Settings_Form) || !hideButtons)
				{
					btnEasyMode.Visible = true;
					btnEngineConfig.Visible = true;
					btnGlitchHarvester.Visible = true;
					btnRTCMultiplayer.Visible = true;
					btnStockpilePlayer.Visible = true;
					btnAutoCorrupt.Visible = true;
					btnManualBlast.Visible = true;

					RemoveGhostBoxes();

					if (!RTC_Core.FirstConnection)
						pnCrashProtection.Visible = true;
				}
			}
		}

		public void btnEngineConfig_Click(object sender, EventArgs e) => ShowPanelForm(S.GET<RTC_EngineConfig_Form>());

		private void btnSettings_Click(object sender, EventArgs e) => ShowPanelForm(S.GET<RTC_Settings_Form>());

		private void btnStockPilePlayer_Click(object sender, EventArgs e) => ShowPanelForm(S.GET<RTC_StockpilePlayer_Form>());

		private void btnRTCMultiplayer_Click(object sender, EventArgs e)
		{
			if (RTC_Core.isStandalone)
				MessageBox.Show("Multiplayer unsupported in Detached mode");
			else
				ShowPanelForm(S.GET<RTC_Multiplayer_Form>());
		}

		private void btnGpJumpBack_Click(object sender, EventArgs e)
		{
			try
			{
				btnGpJumpBack.Visible = false;

				if (RTC_StockpileManager.AllBackupStates.Count == 0)
					return;

				StashKey sk = RTC_StockpileManager.AllBackupStates.Pop();

				sk?.Run();

				RTC_GameProtection.Reset();
			}
			finally
			{
				if (RTC_StockpileManager.AllBackupStates.Count != 0)
					btnGpJumpBack.Visible = true;
			}
		}

		private void btnGpJumpNow_Click(object sender, EventArgs e)
		{
			try
			{
				btnGpJumpNow.Visible = false;

				if (RTC_Unispec.RTCSpec[RTCSPEC.STOCKPILE_BACKUPEDSTATE.ToString()] != null)
					((StashKey)RTC_Unispec.RTCSpec[RTCSPEC.STOCKPILE_BACKUPEDSTATE.ToString()]).Run();

				RTC_GameProtection.Reset();
			}
			finally
			{
				btnGpJumpNow.Visible = true;
			}
		}

		private void BlastRawStash()
		{
			RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.ASYNCBLAST));
			S.GET<RTC_GlitchHarvester_Form>().btnSendRaw_Click(null, null);
		}

		private void btnManualBlast_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				Point locate = new Point(((Control)sender).Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

				ContextMenuStrip columnsMenu = new ContextMenuStrip();
				columnsMenu.Items.Add("Blast + Send RAW To Stash (Glitch Harvester)", null, new EventHandler((ob, ev) =>
				{
					BlastRawStash();
				}));
				columnsMenu.Show(this, locate);
			}
		}

		private void cbUseAutoKillSwitch_CheckedChanged(object sender, EventArgs e)
		{
			pbAutoKillSwitchTimeout.Visible = cbUseAutoKillSwitch.Checked;
		}

		private void cbAutoKillSwitchExecuteAction_SelectedIndexChanged(object sender, EventArgs e)
		{
			btnAutoKillSwitchExecute.Text = cbAutoKillSwitchExecuteAction.SelectedItem?.ToString() ?? "Kill + Restart";
		}

		private void btnAutoKillSwitchExecute_Click(object sender, EventArgs e)
		{
			RTC_NetCore.HugeOperationReset();

			ShowPanelForm(S.GET<RTC_ConnectionStatus_Form>());

			RTC.RTC_RPC.Heartbeat = false;
			RTC.S.GET<RTC_Core_Form>().pbAutoKillSwitchTimeout.Value = RTC.S.GET<RTC_Core_Form>().pbAutoKillSwitchTimeout.Maximum;
			RTC.RTC_RPC.Freeze = true;

			RTC_NetCoreSettings.PlayCrashSound(true);

			switch (btnAutoKillSwitchExecute.Text.ToUpper())
			{
				case "KILL":
					Process.Start("KILLDETACHEDRTC.bat");
					break;
				case "KILL + RESTART":
					Process.Start("RESTARTDETACHEDRTC.bat");
					break;
				case "RESTART + RESET":
					Process.Start("RESETDETACHEDRTC.bat");
					break;
			}
		}
	}
}
