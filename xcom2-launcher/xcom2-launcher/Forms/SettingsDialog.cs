﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XCOM2Launcher.Classes;
using XCOM2Launcher.XCOM;

namespace XCOM2Launcher.Forms
{
    partial class SettingsDialog : Form
    {
        protected Settings Settings { get; set; }

        public bool IsRestartRequired { get; private set; }

        public SettingsDialog(Settings settings)
        {
            InitializeComponent();

            Settings = settings;

            // Init GUI and other locals
            gamePathTextBox.Text = settings.GamePath;
            closeAfterLaunchCheckBox.Checked = settings.CloseAfterLaunch;
            searchForUpdatesCheckBox.Checked = settings.CheckForUpdates;
            showHiddenEntriesCheckBox.Checked = settings.ShowHiddenElements;
            autoNumberModIndexesCheckBox.Checked = settings.AutoNumberIndexes;
            useModSpecifiedCategoriesCheckBox.Checked = settings.UseSpecifiedCategories;
            neverAdoptTagsAndCatFromprofile.Checked = settings.NeverImportTags;
            ShowQuickLaunchArgumentsToggle.Checked = settings.ShowQuickLaunchArguments;
            checkForPreReleaseUpdates.Checked = settings.CheckForPreReleaseUpdates;
            useSentry.Checked = GlobalSettings.Instance.IsSentryEnabled;
            allowMutipleInstances.Checked = settings.AllowMultipleInstances;
            checkForPreReleaseUpdates.Enabled = searchForUpdatesCheckBox.Checked;
            useDuplicateModWorkaround.Checked = settings.EnableDuplicateModIdWorkaround;
            useTranslucentModListSelection.Checked = settings.UseTranslucentModListSelection;

            foreach (var modPath in settings.ModPaths)
                modPathsListbox.Items.Add(modPath);

            argumentsTextBox.Text = string.Join(" ", settings.ArgumentList);
            quickArgumentsTextBox.Text = string.Join(" ", settings.QuickToggleArguments);

            // Create autofill values for arguments box
            var defaultArgs = Argument.DefaultArguments.Select(arg => arg.Parameter).ToArray();
            argumentsTextBox.Values = defaultArgs;
            quickArgumentsTextBox.Values = defaultArgs;
        }

        private void BrowseGamePathButtonOnClick(object sender, EventArgs eventArgs)
        {
            var dialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                InitialDirectory = gamePathTextBox.Text
            };

            if (Program.XEnv.Game == GameId.X2)
            {
                dialog.FileName = "XCom2.exe";
                dialog.Filter = @"XCOM 2 Executable|XCom2.exe";
            }
            else
            {
                dialog.FileName = "XCom.exe";
                dialog.Filter = @"XCOM CS Executable|XCom.exe";
            }

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var path = Path.GetFullPath(Path.Combine(dialog.FileName, "../../.."));
            gamePathTextBox.Text = path;
        }

        private void RemoveModPathButtonOnClick(object sender, EventArgs e)
        {
            if (modPathsListbox.SelectedItem == null)
                return;

            var path = (string) modPathsListbox.SelectedItem;
            modPathsListbox.Items.Remove(path);
        }

        private void AddModPathButtonOnClick(object sender, EventArgs eventArgs)
        {
            var dialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = "Add a new mod path. Note: This should be the directory that contains the mod directories."
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            // make sure the mod path ends with a trailing backslash as required for entry in XCOM ini file
            var path = dialog.SelectedPath.EndsWith(@"\") ? dialog.SelectedPath : dialog.SelectedPath + @"\";

            modPathsListbox.Items.Add(path);
        }

        private void SettingsDialog_Shown(object sender, EventArgs e)
        {
            // if (Settings.Windows.ContainsKey("settings"))
            //     Bounds = Settings.Windows["settings"].Bounds;
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            var newModPaths = modPathsListbox.Items.Cast<string>().ToList();

            // indicate if some changes require an application restart
            bool sentryEnableChanged = useSentry.Checked != GlobalSettings.Instance.IsSentryEnabled;
            bool gamePathChanged = !gamePathTextBox.Text.Equals(Settings.GamePath, StringComparison.OrdinalIgnoreCase);
            var modFoldersChanged = !Settings.ModPaths.OrderBy(x => x).SequenceEqual(newModPaths.OrderBy(x => x));
            IsRestartRequired = sentryEnableChanged || gamePathChanged || modFoldersChanged;

            // Verify settings
            if (!Directory.Exists(gamePathTextBox.Text)) {
                MessageBox.Show("The specified base path does not exist.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Apply changes
            Settings.GamePath = Path.GetFullPath(gamePathTextBox.Text);
            Settings.CloseAfterLaunch = closeAfterLaunchCheckBox.Checked;
            Settings.CheckForUpdates = searchForUpdatesCheckBox.Checked;
            Settings.ShowHiddenElements = showHiddenEntriesCheckBox.Checked;
            Settings.AutoNumberIndexes = autoNumberModIndexesCheckBox.Checked;
            Settings.UseSpecifiedCategories = useModSpecifiedCategoriesCheckBox.Checked;
            Settings.NeverImportTags = neverAdoptTagsAndCatFromprofile.Checked;
            Settings.ShowQuickLaunchArguments = ShowQuickLaunchArgumentsToggle.Checked;
            Settings.CheckForPreReleaseUpdates = checkForPreReleaseUpdates.Checked;
            GlobalSettings.Instance.IsSentryEnabled = useSentry.Checked;
            Settings.AllowMultipleInstances = allowMutipleInstances.Checked;
            Settings.EnableDuplicateModIdWorkaround = useDuplicateModWorkaround.Checked;
            Settings.UseTranslucentModListSelection = useTranslucentModListSelection.Checked;
            Settings.GamePath = gamePathTextBox.Text;
            Settings.ModPaths = newModPaths;

            var newArguments = argumentsTextBox.Text.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            Settings.ArgumentList = newArguments.AsReadOnly();

            var newQuickArguments = quickArgumentsTextBox.Text.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            Settings.QuickToggleArguments = newQuickArguments;

            // Save dimensions
            Settings.Windows["settings"] = new WindowSettings(this);

            GlobalSettings.Instance.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void searchForUpdatesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            checkForPreReleaseUpdates.Enabled = searchForUpdatesCheckBox.Checked;
        }
    }
}