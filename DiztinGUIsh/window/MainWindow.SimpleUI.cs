﻿using System;
using System.Globalization;
using System.Windows.Forms;
using Diz.Core.model;
using Diz.Core.util;
using DiztinGUIsh.window.dialog;

namespace DiztinGUIsh.window
{
    public partial class MainWindow
    {
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) =>
            e.Cancel = !PromptContinueEvenIfUnsavedChanges();

        private void MainWindow_SizeChanged(object sender, EventArgs e) => UpdatePanels();
        private void MainWindow_ResizeEnd(object sender, EventArgs e) => UpdateDataGridView();
        private void MainWindow_Load(object sender, EventArgs e) => Init();
        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e) => CreateNewProject();
        private void openProjectToolStripMenuItem_Click(object sender, EventArgs e) => OpenProject();

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e) => SaveProject();

        private void saveProjectAsToolStripMenuItem_Click(object sender, EventArgs e) => PromptForFilenameToSave();
        private void exportLogToolStripMenuItem_Click(object sender, EventArgs e) => ExportAssembly();
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) => new About().ShowDialog();
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();

        private void runGameToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (Project?.AttachedRomFilename != null)
            {
                System.Diagnostics.Process.Start(Project.AttachedRomFilename);
            }

        }
        private void decimalToolStripMenuItem_Click(object sender, EventArgs e) => 
            UpdateBase(Util.NumberBase.Decimal);

        private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e) =>
            UpdateBase(Util.NumberBase.Hexadecimal);

        private void binaryToolStripMenuItem_Click(object sender, EventArgs e) => 
            UpdateBase(Util.NumberBase.Binary);
        
        private void importTraceLogBinary_Click(object sender, EventArgs e) => ImportBsnesBinaryTraceLog();
        private void addLabelToolStripMenuItem_Click(object sender, EventArgs e) => BeginAddingLabel();
        private void visualMapToolStripMenuItem_Click(object sender, EventArgs e) => ShowVisualizerForm();
        private void stepOverToolStripMenuItem_Click(object sender, EventArgs e) => Step(LastOffset);
        private void stepInToolStripMenuItem_Click(object sender, EventArgs e) => StepIn(LastOffset);
        private void autoStepSafeToolStripMenuItem_Click(object sender, EventArgs e) => AutoStepSafe(LastOffset);
        private void autoStepHarshToolStripMenuItem_Click(object sender, EventArgs e) => AutoStepHarsh(LastOffset);
        private void gotoToolStripMenuItem_Click(object sender, EventArgs e) => GoTo(PromptForGotoOffset());

        private void gotoIntermediateAddressToolStripMenuItem_Click(object sender, EventArgs e) =>
            GoToIntermediateAddress(LastOffset);

        private void gotoFirstUnreachedToolStripMenuItem_Click(object sender, EventArgs e) => 
            GoToUnreached(true, true);

        private void gotoNearUnreachedToolStripMenuItem_Click(object sender, EventArgs e) =>
            GoToUnreached(false, false);

        private void gotoNextUnreachedToolStripMenuItem_Click(object sender, EventArgs e) => 
            GoToUnreached(false, true);
        
        private void markOneToolStripMenuItem_Click(object sender, EventArgs e) => Mark(LastOffset);
        private void markManyToolStripMenuItem_Click(object sender, EventArgs e) => MarkMany(LastOffset, ColumnName(SelectedColumn));
        private void setDataBankToolStripMenuItem_Click(object sender, EventArgs e) => MarkMany(LastOffset, "db");
        private void setDirectPageToolStripMenuItem_Click(object sender, EventArgs e) => MarkMany(LastOffset, "dp");

        private void toggleAccumulatorSizeMToolStripMenuItem_Click(object sender, EventArgs e) => MarkMany(LastOffset, "m");

        private void toggleIndexSizeToolStripMenuItem_Click(object sender, EventArgs e) => MarkMany(LastOffset, "x");

        private void setBaseAddrToolStripMenuItem_Click(object sender, System.EventArgs e) => MarkMany(LastOffset, "base");
        private void addCommentToolStripMenuItem_Click(object sender, EventArgs e) => BeginEditingComment();

        private void unreachedToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Unreached);

        private void opcodeToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(Data.FlagType.Opcode);

        private void operandToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Operand);

        private void bitDataToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Data8Bit);

        private void graphicsToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Graphics);

        private void musicToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(Data.FlagType.Music);
        private void emptyToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(Data.FlagType.Empty);

        private void bitDataToolStripMenuItem1_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Data16Bit);

        private void wordPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Pointer16Bit);

        private void bitDataToolStripMenuItem2_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Data24Bit);

        private void longPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Pointer24Bit);

        private void bitDataToolStripMenuItem3_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Data32Bit);

        private void dWordPointerToolStripMenuItem_Click(object sender, EventArgs e) =>
            SetMarkerLabel(Data.FlagType.Pointer32Bit);

        private void textToolStripMenuItem_Click(object sender, EventArgs e) => SetMarkerLabel(Data.FlagType.Text);
        private void binToolStripMenuItem_Click(object sender, System.EventArgs e) => SetMarkerLabel(Data.FlagType.Binary);
        private void fixMisalignedInstructionsToolStripMenuItem_Click(object sender, EventArgs e) => FixMisalignedInstructions();
        private void openLastProjectAutomaticallyToolStripMenuItem_Click(object sender, EventArgs e) => SaveSettings();
        private void closeProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
        }

        private void hexadecimalConstantToolStripMenuItem_Click(object sender, System.EventArgs e) => SetConstantType(Data.ConstantType.Hexadecimal);
        private void decimalConstantToolStripMenuItem_Click(object sender, System.EventArgs e) => SetConstantType(Data.ConstantType.Decimal);
        private void binaryConstantToolStripMenuItem_Click(object sender, System.EventArgs e) => SetConstantType(Data.ConstantType.Binary);
        private void textConstantToolStripMenuItem_Click(object sender, System.EventArgs e) => SetConstantType(Data.ConstantType.Text);
        private void colorConstantToolStripMenuItem_Click(object sender, System.EventArgs e) => SetConstantType(Data.ConstantType.Color);

        private void referenceView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) => SelectOffset((int) e.Node.Tag);

        private void historyView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectOffset((int) e.Node.Tag, -1, false);
            e.Node.EnsureVisible();

        }
        private void referenceView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            int offset = (int)e.Node.Tag;
            if (offset >= 0 && offset < Project.Data.GetRomSize() && offset != SelectedOffset)
            {
                ViewOffset = offset;
                UpdateDataGridView();
            }

            e.Node.EnsureVisible();
        }
        private void referenceSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                if(int.TryParse(referenceSearch.Text, NumberStyles.HexNumber, null, out int snes))
                    ShowReferences(snes, false, true);
        }

        private void sidebarUpdateView(object sender, System.EventArgs e)
        {
            referenceGroup.Visible = showReferencesToolStripMenuItem.Checked;
            labelsGroup.Visible = showLabelsCommentsToolStripMenuItem.Checked;
            historyGroup.Visible = showNavigationHistoryToolStripMenuItem.Checked;
            historyGroup.Dock = labelsGroup.Visible ? DockStyle.Bottom : DockStyle.Fill;
            referenceGroup.Dock = labelsGroup.Visible || historyGroup.Visible ? DockStyle.Top : DockStyle.Fill;
            Sidebar.Width = sidebarToolStripMenuItem.Checked && (referenceGroup.Visible || labelsGroup.Visible || historyGroup.Visible) ? 200 : 1;
        }
        private void importCDLToolStripMenuItem_Click_1(object sender, EventArgs e) => ImportBizhawkCDL();

        private void importBsnesTracelogText_Click(object sender, EventArgs e) => ImportBsnesTraceLogText();

        private void graphicsWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // graphics view window
        }
        private void toolStripOpenLast_Click(object sender, EventArgs e) => OpenLastProject();
        private void rescanForInOutPointsToolStripMenuItem_Click(object sender, EventArgs e) => RescanForInOut();
        private void importUsageMapToolStripMenuItem_Click_1(object sender, EventArgs e) => ImportBSNESUsageMap();
        private void table_MouseWheel(object sender, MouseEventArgs e) => ScrollTableBy(e.Delta);
        private void toolStripSearchNext_Click(object sender, System.EventArgs e) => SelectOffset(SearchOffset(1));
        private void toolStripSearchPrevious_Click(object sender, System.EventArgs e) => SelectOffset(SearchOffset(-1));
        private void toolStripSearchBox_KeyDown(object sender, KeyEventArgs e) {
            switch(e.KeyCode)
            {
                case Keys.Enter: SelectOffset(SearchOffset(1)); break;
                case Keys.Escape: table.Focus(); break;
            }
        }

    }
}