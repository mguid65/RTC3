﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RTC.RTC_Unispec;

namespace RTC
{
	public partial class RTC_CorruptionEngine_Form : ComponentForm, IAutoColorize
	{
		public new void HandleMouseDown(object s, MouseEventArgs e) => base.HandleMouseDown(s, e);
		public new void HandleFormClosing(object s, FormClosingEventArgs e) => base.HandleFormClosing(s, e);

		private int defaultPrecision = -1;
		private bool updatingMinMax = false;



		public RTC_CorruptionEngine_Form()
		{
			InitializeComponent();

			this.undockedSizable = false;
		}

		private void RTC_CorruptionEngine_Form_Load(object sender, EventArgs e)
		{

			gbNightmareEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbHellgenieEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbDistortionEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbFreezeEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbPipeEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbVectorEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbBlastGeneratorEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);
			gbCustomEngine.Location = new Point(gbSelectedEngine.Location.X, gbSelectedEngine.Location.Y);


			cbSelectedEngine.SelectedIndex = 0;
			cbBlastType.SelectedIndex = 0;
			cbCustomPrecision.SelectedIndex = 0;

			//Do this here as if it's stuck into the designer, it keeps defaulting out
			cbVectorValueList.DataSource = RTC_Core.ValueListBindingSource;
			cbVectorLimiterList.DataSource = RTC_Core.LimiterListBindingSource;


			cbVectorValueList.DisplayMember = "Name";
			cbVectorLimiterList.DisplayMember = "Name";

			cbVectorValueList.ValueMember = "Value";
			cbVectorLimiterList.ValueMember = "Value";

			if (RTC_Core.LimiterListBindingSource.Count > 0)
			{
				cbVectorLimiterList_SelectedIndexChanged(cbVectorLimiterList, null);
			}
			if (RTC_Core.ValueListBindingSource.Count > 0)
			{
				cbVectorValueList_SelectedIndexChanged(cbVectorValueList, null);
			}

		}


		private void nmMaxCheats_ValueChanged(object sender, EventArgs e)
		{
			if (Convert.ToInt32(nmMaxCheats.Value) != (int)RTC_Unispec.RTCSpec[RTCSPEC.STEP_MAXINFINITEBLASTUNITS.ToString()])
				RTC_Unispec.RTCSpec.Update(RTCSPEC.STEP_MAXINFINITEBLASTUNITS.ToString(), Convert.ToInt32(nmMaxCheats.Value));

			if (nmMaxCheats.Value != nmMaxFreezes.Value)
				nmMaxFreezes.Value = nmMaxCheats.Value;
		}


		private void cbClearCheatsOnRewind_CheckedChanged(object sender, EventArgs e)
		{
			if (cbClearFreezesOnRewind.Checked != cbClearCheatsOnRewind.Checked)
				cbClearFreezesOnRewind.Checked = cbClearCheatsOnRewind.Checked;

			RTCSpec.Update(RTCSPEC.CORE_CLEARSTEPACTIONSONREWIND.ToString(), true);
		}

		private void nmDistortionDelay_ValueChanged(object sender, EventArgs e)
		{;
			RTCSpec.Update(RTCSPEC.DISTORTION_DELAY.ToString(), Convert.ToInt32(nmDistortionDelay.Value));
		}

		private void btnResyncDistortionEngine_Click(object sender, EventArgs e)
		{
			RTC_StepActions.ClearStepBlastUnits();
		}

		//Todo - refactor the hell out of this
		public void UpdateDefaultPrecision()
		{
			defaultPrecision = RTC_MemoryDomains.MemoryInterfaces[RTC_MemoryDomains.MainDomain].WordSize;
			lbCoreDefault.Text = $"Core default: { defaultPrecision * 8}-bit";

			
			if ((int)RTCSpec[RTCSPEC.CORE_CUSTOMPRECISION.ToString()] == -1)
			{
				updateMinMaxBoxes(defaultPrecision);
				RTCSpec.Update(RTCSPEC.CORE_CURRENTPRECISION.ToString(), defaultPrecision);
			}

		}


		private void cbSelectedEngine_SelectedIndexChanged(object sender, EventArgs e)
		{
			gbNightmareEngine.Visible = false;
			gbHellgenieEngine.Visible = false;
			gbDistortionEngine.Visible = false;
			gbFreezeEngine.Visible = false;
			gbPipeEngine.Visible = false;
			gbVectorEngine.Visible = false;
			gbBlastGeneratorEngine.Visible = false;
			gbCustomEngine.Visible = false;

			pnCustomPrecision.Visible = false;
			

			S.GET<RTC_Core_Form>().btnAutoCorrupt.Visible = true;
			S.GET<RTC_GlitchHarvester_Form>().pnIntensity.Visible = true;
			S.GET<RTC_EngineConfig_Form>().pnGeneralParameters.Visible = true;
			S.GET<RTC_EngineConfig_Form>().pnMemoryDomains.Visible = true;

			switch (cbSelectedEngine.SelectedItem.ToString())
			{
				case "Nightmare Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(),CorruptionEngine.NIGHTMARE);
					gbNightmareEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Hellgenie Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.HELLGENIE);
					gbHellgenieEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Distortion Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.DISTORTION);
					gbDistortionEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Freeze Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.FREEZE);
					gbFreezeEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Pipe Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.PIPE);
					gbPipeEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Vector Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.VECTOR);
					gbVectorEngine.Visible = true;
					break;

				case "Custom Engine":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.CUSTOM);
					gbCustomEngine.Visible = true;
					pnCustomPrecision.Visible = true;
					break;

				case "Blast Generator":
					RTCSpec.Update(RTCSPEC.CORE_SELECTEDENGINE.ToString(), CorruptionEngine.BLASTGENERATORENGINE);
					gbBlastGeneratorEngine.Visible = true;

					S.GET<RTC_Core_Form>().AutoCorrupt = false;
					S.GET<RTC_Core_Form>().btnAutoCorrupt.Visible = false;
					S.GET<RTC_EngineConfig_Form>().pnGeneralParameters.Visible = false;
					S.GET<RTC_EngineConfig_Form>().pnMemoryDomains.Visible = false;

					S.GET<RTC_GlitchHarvester_Form>().pnIntensity.Visible = false;
					break;

				default:
					break;
			}

			if (cbSelectedEngine.SelectedItem.ToString() == "Blast Generator")
			{
				S.GET<RTC_GeneralParameters_Form>().labelBlastRadius.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().labelIntensity.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().labelIntensityTimes.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().labelErrorDelay.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().labelErrorDelaySteps.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().nmIntensity.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().track_ErrorDelay.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().track_Intensity.Visible = false;
				S.GET<RTC_GeneralParameters_Form>().cbBlastRadius.Visible = false;
			}
			else
			{
				S.GET<RTC_GeneralParameters_Form>().labelBlastRadius.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().labelIntensity.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().labelIntensityTimes.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().labelErrorDelay.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().labelErrorDelaySteps.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().nmIntensity.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().track_ErrorDelay.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().track_Intensity.Visible = true;
				S.GET<RTC_GeneralParameters_Form>().cbBlastRadius.Visible = true;
			}

			cbSelectedEngine.BringToFront();
			pnCustomPrecision.BringToFront();

			RTC_StepActions.ClearStepBlastUnits();
		}

		private void cbClearFreezesOnRewind_CheckedChanged(object sender, EventArgs e)
		{
			if (cbClearFreezesOnRewind.Checked != cbClearCheatsOnRewind.Checked)
				cbClearCheatsOnRewind.Checked = cbClearFreezesOnRewind.Checked;

			RTCSpec.Update(RTCSPEC.CORE_CLEARSTEPACTIONSONREWIND.ToString(), cbClearFreezesOnRewind.Checked);
		}

		private void nmMaxFreezes_ValueChanged(object sender, EventArgs e)
		{
			if (Convert.ToInt32(nmMaxFreezes.Value) != (int)RTC_Unispec.RTCSpec[RTCSPEC.STEP_MAXINFINITEBLASTUNITS.ToString()])
			{
				RTC_Unispec.RTCSpec.Update(RTCSPEC.STEP_MAXINFINITEBLASTUNITS.ToString(), Convert.ToInt32(nmMaxFreezes.Value));
			}

			if (nmMaxCheats.Value != nmMaxFreezes.Value)
				nmMaxCheats.Value = nmMaxFreezes.Value;
		}

		private void nmMaxPipes_ValueChanged(object sender, EventArgs e)
		{
			RTC_Unispec.RTCSpec.Update(RTCSPEC.STEP_MAXINFINITEBLASTUNITS.ToString(), Convert.ToInt32(nmMaxPipes.Value));
		}

		private void btnClearPipes_Click(object sender, EventArgs e)
		{
			RTC_StepActions.ClearStepBlastUnits();
		}

		private void cbLockPipes_CheckedChanged(object sender, EventArgs e)
		{
			RTCSpec.Update(RTCSPEC.STEP_LOCKEXECUTION.ToString(), cbLockPipes.Checked);
		}


		private void cbClearPipesOnRewind_CheckedChanged(object sender, EventArgs e)
		{
			RTCSpec.Update(RTCSPEC.CORE_CLEARSTEPACTIONSONREWIND.ToString(), cbClearPipesOnRewind.Checked);
		}

		private void cbVectorLimiterList_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBoxItem<string> item = (ComboBoxItem<string>)((ComboBox)sender).SelectedItem;
			RTCSpec.Update(RTCSPEC.VECTOR_LIMITERLISTHASH.ToString(), item.Value);
		}

		private void cbVectorValueList_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBoxItem<string> item = (ComboBoxItem<string>)((ComboBox)sender).SelectedItem;
			RTCSpec.Update(RTCSPEC.VECTOR_VALUELISTHASH.ToString(), item);
		}

		private void btnClearCheats_Click(object sender, EventArgs e)
		{
			RTC_StepActions.ClearStepBlastUnits();
		}

		private void cbUseCustomPrecision_CheckedChanged(object sender, EventArgs e)
		{
			if (cbUseCustomPrecision.Checked)
			{
				if (cbCustomPrecision.SelectedIndex == -1)
					cbCustomPrecision.SelectedIndex = 0;
			}
			else
			{
				cbCustomPrecision.SelectedIndex = -1;
				RTCSpec.Update(RTCSPEC.CORE_CUSTOMPRECISION.ToString(), -1);
			}
		}

		private void updateMinMaxBoxes(int precision)
		{
			updatingMinMax = true;
			switch (precision)
			{
				case 1:
					nmMinValueNightmare.Maximum = byte.MaxValue;
					nmMaxValueNightmare.Maximum = byte.MaxValue;

					nmMinValueHellgenie.Maximum = byte.MaxValue;
					nmMaxValueHellgenie.Maximum = byte.MaxValue;


					nmMinValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MINVALUE8BIT.ToString()];
					nmMaxValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MAXVALUE8BIT.ToString()];

					nmMinValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MINVALUE8BIT.ToString()];
					nmMaxValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MAXVALUE8BIT.ToString()];
					break;

				case 2:
					nmMinValueNightmare.Maximum = UInt16.MaxValue;
					nmMaxValueNightmare.Maximum = UInt16.MaxValue;

					nmMinValueHellgenie.Maximum = UInt16.MaxValue;
					nmMaxValueHellgenie.Maximum = UInt16.MaxValue;

					nmMinValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MINVALUE16BIT.ToString()];
					nmMaxValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MAXVALUE16BIT.ToString()];

					nmMinValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MINVALUE16BIT.ToString()];
					nmMaxValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MAXVALUE16BIT.ToString()];
					break;
				case 4:
					nmMinValueNightmare.Maximum = UInt32.MaxValue;
					nmMaxValueNightmare.Maximum = UInt32.MaxValue;

					nmMinValueHellgenie.Maximum = UInt32.MaxValue;
					nmMaxValueHellgenie.Maximum = UInt32.MaxValue;

					nmMinValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MINVALUE32BIT.ToString()];
					nmMaxValueNightmare.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.NIGHTMARE_MAXVALUE32BIT.ToString()];
												 
					nmMinValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MINVALUE32BIT.ToString()];
					nmMaxValueHellgenie.Value = (long)RTC_Unispec.RTCSpec[RTCSPEC.HELLGENIE_MAXVALUE32BIT.ToString()];

					break;
			}
			updatingMinMax = false;
		}

		private void cbCustomPrecision_SelectedIndexChanged(object sender, EventArgs e)
		{
			//Deselect the updown boxes so they commit if they're selected.
			//As you can use the scroll wheel over the combobox while the textbox is focused, this is required
			cbCustomPrecision.Focus();

			if (cbCustomPrecision.SelectedIndex != -1)
			{
				cbUseCustomPrecision.Checked = true;

				switch (cbCustomPrecision.SelectedIndex)
				{
					case 0:
						RTCSpec.Update(RTCSPEC.CORE_CUSTOMPRECISION.ToString(), 1);
						break;
					case 1:
						RTCSpec.Update(RTCSPEC.CORE_CUSTOMPRECISION.ToString(), 2);
						break;
					case 2:
						RTCSpec.Update(RTCSPEC.CORE_CUSTOMPRECISION.ToString(), 4);
						break;
				}
				updateMinMaxBoxes((int)RTCSpec[RTCSPEC.CORE_CUSTOMPRECISION.ToString()]);
				S.GET<RTC_CustomEngineConfig_Form>().UpdateMinMaxBoxes((int)RTCSpec[RTCSPEC.CORE_CUSTOMPRECISION.ToString()]);
			}
		}

		private void btnOpenBlastGenerator_Click(object sender, EventArgs e)
		{
			MessageBox.Show("New Blast Generator currently not implemented");
			/*
			if (S.GET<RTC_BlastGenerator_Form>() != null)
				S.GET<RTC_BlastGenerator_Form>().Close();
			S.SET(new RTC_BlastGenerator_Form());
			S.GET<RTC_BlastGenerator_Form>().LoadNoStashKey();
			*/
		}

		//TODO
		//Refactor this into a struct or something

		private void nmMinValueNightmare_ValueChanged(object sender, EventArgs e)
		{
			//We don't want to trigger this if it caps when stepping downwards
			if (updatingMinMax)
				return;

			long value = Convert.ToInt64(nmMinValueNightmare.Value);


			switch ((int)RTCSpec[RTCSPEC.CORE_CUSTOMPRECISION.ToString()])
			{
				case 1:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MINVALUE8BIT.ToString(), value);
					break;
				case 2:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MINVALUE16BIT.ToString(), value);
					break;
				case 4:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MINVALUE32BIT.ToString(), value);
					break;
			}

		}

		private void nmMaxValueNightmare_ValueChanged(object sender, EventArgs e)
		{
			//We don't want to trigger this if it caps when stepping downwards
			if (updatingMinMax)
				return;
			long value = Convert.ToInt64(nmMaxValueNightmare.Value);
			

			switch (RTCSpec[RTCSPEC.CORE_CURRENTPRECISION.ToString()])
			{
				case 1:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MAXVALUE8BIT.ToString(), value);
					break;
				case 2:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MAXVALUE16BIT.ToString(), value);
					break;
				case 4:
					RTCSpec.Update(RTCSPEC.NIGHTMARE_MAXVALUE32BIT.ToString(), value);
					break;
			}
		}

		private void nmMinValueHellgenie_ValueChanged(object sender, EventArgs e)
		{
			//We don't want to trigger this if it caps when stepping downwards
			if (updatingMinMax)
				return;
			long value = Convert.ToInt64(nmMinValueHellgenie.Value);

			switch (RTCSpec[RTCSPEC.CORE_CURRENTPRECISION.ToString()])
			{
				case 1:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MINVALUE8BIT.ToString(), value);
					break;
				case 2:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MINVALUE16BIT.ToString(), value);
					break;
				case 4:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MINVALUE32BIT.ToString(), value);
					break;
			}
		}

		private void nmMaxValueHellgenie_ValueChanged(object sender, EventArgs e)
		{
			//We don't want to trigger this if it caps when stepping downwards
			if (updatingMinMax)
				return;

			long value = Convert.ToInt64(nmMaxValueHellgenie.Value);

			switch (RTCSpec[RTCSPEC.CORE_CURRENTPRECISION.ToString()])
			{
				case 1:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MAXVALUE8BIT.ToString(), value);
					break;
				case 2:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MAXVALUE16BIT.ToString(), value);
					break;
				case 4:
					RTCSpec.Update(RTCSPEC.HELLGENIE_MAXVALUE32BIT.ToString(), value);
					break;
			}
		}


		private void cbBlastType_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (cbBlastType.SelectedItem.ToString())
			{
				case "RANDOM":
					RTCSpec.Update(RTCSPEC.NIGHTMARE_TYPE.ToString(), NightmareAlgo.RANDOM);
					nmMinValueNightmare.Enabled = true;
					nmMaxValueNightmare.Enabled = true;
					break;

				case "RANDOMTILT":
					RTCSpec.Update(RTCSPEC.NIGHTMARE_TYPE.ToString(), NightmareAlgo.RANDOMTILT);
					nmMinValueNightmare.Enabled = true;
					nmMaxValueNightmare.Enabled = true;
					break;

				case "TILT":
					RTCSpec.Update(RTCSPEC.NIGHTMARE_TYPE.ToString(), NightmareAlgo.TILT);
					nmMinValueNightmare.Enabled = false;
					nmMaxValueNightmare.Enabled = false;
					break;
			}
		}

		private void btnOpenCustomEngine_Click(object sender, EventArgs e)
		{
			S.GET<RTC_CustomEngineConfig_Form>().Show();
			S.GET<RTC_CustomEngineConfig_Form>().Focus();
		}
	}
}
