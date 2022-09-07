using System.Diagnostics;
using Smartstore.Core.Theming;
using Smartstore.Packager.Properties;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Engine;
using Smartstore.Core.Packaging;
using Smartstore.Utilities;

namespace Smartstore.Packager
{
    public partial class MainForm : Form
    {
        private readonly IApplicationContext _appContext;

        public MainForm(IApplicationContext appContext)
        {
            _appContext = appContext;

            InitializeComponent();

            this.Load += (object sender, EventArgs e) =>
            {
                var settings = Settings.Default;
                settings.Reload();

                try
                {
                    var rootPath = new DirectoryInfo(appContext.RuntimeInfo.BaseDirectory).Parent.Parent.Parent.Parent.Parent;

                    if (settings.RootPath.IsEmpty())
                    {
                        var version = SmartstoreVersion.Version;
                        var versionStr = $"{version.Major}.{version.Minor}.{version.Revision}";
                        settings.RootPath = Path.Combine(rootPath.FullName, $"build\\artifacts\\Community.{versionStr}.win-x64");
                    }

                    if (settings.OutputPath.IsEmpty())
                    {
                        settings.OutputPath = Path.Combine(rootPath.FullName, "build\\packages");
                    }
                }
                catch 
                { 
                }

                txtRootPath.Text = settings.RootPath;
                txtOutputPath.Text = settings.OutputPath;
            };

            this.FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                Settings.Default.RootPath = txtRootPath.Text;
                Settings.Default.OutputPath = txtOutputPath.Text;

                Settings.Default.Save();
            };
        }

        private async void btnCreatePackages_Click(object sender, EventArgs e)
        {
            if (!ValidatePaths())
                return;

            var rootPath = txtRootPath.Text;
            var outputPath = txtOutputPath.Text;

            var erroredIds = new List<string>();
            IExtensionDescriptor currentPackage = null;

            try
            {
                btnCreatePackages.Enabled = false;
                btnClose.Enabled = false;

                var creator = new PackageCreator(rootPath, outputPath);

                var selectedItems = lstModules.SelectedItems
                    .OfType<IExtensionDescriptor>()
                    .Concat(lstThemes.SelectedItems.OfType<IExtensionDescriptor>())
                    .ToArray();

                // Create extension packages
                foreach (var selectedPackage in selectedItems)
                {
                    currentPackage = selectedPackage;
                    var fi = await creator.CreateExtensionPackageAsync(currentPackage);
                    var location = selectedPackage as IExtensionLocation;
                    var name = location?.Path ?? currentPackage.Name;

                    if (!LogMessage(fi, name))
                    {
                        erroredIds.Add(name);
                    }
                }

                var msg = string.Empty;

                if (erroredIds.Count == 0)
                {
                    msg = "Successfully created all {0} packages".FormatCurrent(selectedItems.Length);
                    MessageBox.Show(msg, "Packages created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    msg = "Successfully created {0} packages.\n\nUnable to create:\n\n".FormatCurrent(selectedItems.Length - erroredIds.Count);
                    erroredIds.Each(x => msg += x + "\n");
                    MessageBox.Show(msg, "Packages created with errors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[{0}]: {1}".FormatCurrent(currentPackage.Name, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCreatePackages.Enabled = true;
                btnClose.Enabled = true;
            }

        }

        private bool LogMessage(FileInfo fi, string packageName)
        {
            string msg = string.Empty;
            if (fi != null)
            {
                msg = "Created package for '{0}'...".FormatCurrent(packageName);
            }
            else
            {
                msg = "Unable to create package for '{0}'...".FormatCurrent(packageName);
            }

            Debug.WriteLine(msg);

            return fi != null;
        }

        private void btnReadDescriptions_Click(object sender, EventArgs e)
        {
            if (!ValidatePaths())
                return;

            btnReadDescriptions.Enabled = false;

            var rootPath = txtRootPath.Text;
            var root = new LocalFileSystem(rootPath);
            ReadPackages(root);

            btnReadDescriptions.Enabled = true;
        }


        private void ReadPackages(IFileSystem root)
        {
            if (!ValidatePaths())
                return;

            lstModules.DisplayMember = "Name";
            //lstModules.ValueMember = "Path";
            lstThemes.DisplayMember = "Name";
            //lstThemes.ValueMember = "Path";

            lstModules.Items.Clear();
            lstThemes.Items.Clear();

            IEnumerable<IDirectory> dirs = Enumerable.Empty<IDirectory>();

            var dirModules = root.GetDirectory("Modules");
            var dirThemes = root.GetDirectory("Themes");

            var modulesRoot = dirModules.Exists
                ? new LocalFileSystem(dirModules.PhysicalPath)
                : root;

            var themesRoot = dirThemes.Exists
                ? new LocalFileSystem(dirThemes.PhysicalPath)
                : root;

            if (dirModules.Exists || dirThemes.Exists)
            {
                if (dirModules.Exists)
                {
                    dirs = dirs.Concat(dirModules.EnumerateDirectories());
                }

                if (dirThemes.Exists)
                {
                    dirs = dirs.Concat(dirThemes.EnumerateDirectories());
                }
            }
            else
            {
                dirs = root.EnumerateDirectories("");
            }

            lstModules.DisplayMember = "SystemName";

            foreach (var dir in dirs)
            {
                bool isTheme = false;

                // is it a module?
                var filePath = PathUtility.Join(dir.SubPath, "module.json");
                if (!root.FileExists(filePath))
                {
                    // ...no! is it a theme?
                    filePath = PathUtility.Join(dir.SubPath, "theme.config");
                    if (!root.FileExists(filePath))
                        continue;

                    isTheme = true;
                }

                try
                {
                    if (isTheme)
                    {
                        var manifest = ThemeDescriptor.Create(dir.Name, themesRoot);
                        lstThemes.Items.Add(manifest);
                    }
                    else
                    {
                        var descriptor = ModuleDescriptor.Create(dir, modulesRoot);
                        if (descriptor != null)
                        {
                            lstModules.Items.Add(descriptor);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    continue;
                }
            }

            if (lstModules.Items.Count > 0)
            {
                tabMain.SelectedIndex = 0;
            }
            else if (lstThemes.Items.Count > 0)
            {
                tabMain.SelectedIndex = 1;
            }
        }

        private bool ValidatePaths()
        {
            if (!Directory.Exists(txtRootPath.Text))
            {
                MessageBox.Show("Root path does not exist! Please specify an existing path", "Path error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRootPath.Focus();
                return false;
            }

            return true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            btnReadDescriptions.Enabled = txtRootPath.Text.Length > 0;
            var selCount = lstModules.SelectedItems.Count + lstThemes.SelectedItems.Count;
            btnCreatePackages.Enabled = selCount > 0;
            if (selCount > 1)
            {
                btnCreatePackages.Text = "Create {0} packages".FormatCurrent(selCount);
            }
            else
            {
                btnCreatePackages.Text = "Create package";
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnBrowseRootPath_Click(object sender, EventArgs e)
        {
            if (txtRootPath.Text.Length > 0)
            {
                fb.SelectedPath = txtRootPath.Text;
            }

            var result = fb.ShowDialog();
            txtRootPath.Text = fb.SelectedPath;
        }

        private void btnBrowseOutputPath_Click(object sender, EventArgs e)
        {
            if (txtOutputPath.Text.Length > 0)
            {
                fb.SelectedPath = txtOutputPath.Text;
            }

            var result = fb.ShowDialog();
            txtOutputPath.Text = fb.SelectedPath;
        }

    }
}
