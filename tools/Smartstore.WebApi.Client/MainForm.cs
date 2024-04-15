using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using Smartstore.WebApi.Client.Models;
using Smartstore.WebApi.Client.Properties;
using Smartstore.WebApi.Client.Utilities;

namespace Smartstore.WebApi.Client
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.Text = Program.AppName;

            this.Load += (object sender, EventArgs e) =>
            {
                var s = Settings.Default;
                s.Reload();

                cboMethod.SelectedIndex = 0;
                txtPublicKey.Text = s.ApiPublicKey;
                txtSecretKey.Text = s.ApiSecretKey;
                txtUrl.Text = s.ApiUrl;
                txtProxyPort.Text = s.ApiProxyPort;
                txtVersion.Text = s.ApiVersion;
                cboPath.Items.FromString(s.ApiPaths);
                cboQuery.Items.FromString(s.ApiQuery);
                cboContent.Items.FromString(s.ApiContent);
                cboHeaders.Items.FromString(s.ApiHeaders);
                cboFileUpload.Items.FromString(s.FileUpload);

                if (cboPath.Items.Count <= 0)
                {
                    cboPath.Items.Add("/Customers");
                }

                if (cboHeaders.Items.Count <= 0)
                {
                    cboHeaders.Items.Add("{\"Prefer\":\"return=representation\"}");
                }

                if (cboFileUpload.Items.Count <= 0)
                {
                    var model = new FileUploadModel
                    {
                        Files = new List<FileUploadModel.FileModel>
                        {
                            new FileUploadModel.FileModel { LocalPath = @"C:\my-upload-picture.jpg" }
                        }
                    };
                    var serializedModel = JsonConvert.SerializeObject(model);
                    cboFileUpload.Items.Add(serializedModel);
                }

                CboMethod_changeCommitted(null, null);

                openFileDialog1.Filter = "Supported files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.csv, *.xlsx, *.txt, *.tab, *.zip, *.story) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.csv; *.xlsx; *.txt; *.tab; *.zip; *.story";
                openFileDialog1.DefaultExt = ".jpg";
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "Please select files to upload";
                openFileDialog1.Multiselect = true;
            };

            this.FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                var s = Settings.Default;

                s.ApiPublicKey = txtPublicKey.Text;
                s.ApiSecretKey = txtSecretKey.Text;
                s.ApiUrl = txtUrl.Text;
                s.ApiProxyPort = txtProxyPort.Text;
                s.ApiVersion = txtVersion.Text;
                s.ApiQuery = cboQuery.Items.IntoString();
                s.ApiContent = cboContent.Items.IntoString();
                s.ApiHeaders = cboHeaders.Items.IntoString();
                s.FileUpload = cboFileUpload.Items.IntoString();

                Settings.Default["ApiPaths"] = cboPath.Items.IntoString();

                s.Save();
            };
        }

        private async Task Execute()
        {
            if (txtUrl.Text.HasValue() && !txtUrl.Text.EndsWith("/"))
            {
                txtUrl.Text += "/";
            }

            if (cboPath.Text.HasValue() && !cboPath.Text.StartsWith("/"))
            {
                cboPath.Text = "/" + cboPath.Text;
            }

            var url = txtUrl.Text.EndsWith("odata/") ? txtUrl.Text : (txtUrl.Text + "odata/");
            url += txtVersion.Text + cboPath.Text;

            _ = int.TryParse(txtProxyPort.Text, out var proxyPort);

            var request = new WebApiRequest
            {
                PublicKey = txtPublicKey.Text,
                SecretKey = txtSecretKey.Text,
                Url = url,
                ProxyPort = proxyPort,
                HttpMethod = cboMethod.Text,
                HttpAcceptType = MediaTypeNames.Application.Json,
                AdditionalHeaders = cboHeaders.Text,
                FileDialog = folderBrowserDialog1
            };

            if (chkIEEE754Compatible.Checked)
            {
                request.HttpAcceptType += ";IEEE754Compatible=true";
            }

            if (cboQuery.Text.HasValue())
            {
                request.Url = $"{request.Url}?{cboQuery.Text}";
            }
            if (request.ProxyPort > 0)
            {
                // API behind a reverse proxy.
                request.Url = new UriBuilder(request.Url) { Port = request.ProxyPort }.Uri.ToString();
            }

            if (!request.IsValid)
            {
                MessageBox.Show("Please enter Public-Key, Secret-Key, URL and method.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Debug.WriteLine(request.ToString());
                return;
            }

            lblRequest.Text = "Request: " + request.HttpMethod + " " + request.Url;
            lblRequest.Refresh();

            FileUploadModel uploadModel = null;
            if (cboFileUpload.Text.HasValue())
            {
                try
                {
                    uploadModel = JsonConvert.DeserializeObject(cboFileUpload.Text, typeof(FileUploadModel)) as FileUploadModel;
                }
                catch
                {
                    cboFileUpload.RemoveCurrent();
                    cboFileUpload.Text = string.Empty;
                    MessageBox.Show("File upload data is invalid.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            try
            {
                var response = await WebApiClient.StartRequestAsync(request, cboContent.Text, uploadModel);
                txtRequest.Text = response.RequestContent.ToString();
                lblResponse.Text = "Response: " + response.Status;

                var sb = new StringBuilder();
                sb.Append(response.Headers);

                if (response.Succeeded && !response.IsFileResponse)
                {
                    var customers = response.ParseCustomers();
                    if (customers != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"Parsed {customers.Count} customer(s):");
                        customers.ForEach(x => sb.AppendLine(x.ToString()));
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
                sb.Append(response.Content);
                txtResponse.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.ToString();
            }

            cboPath.InsertRolled(cboPath.Text, 64);
            cboQuery.InsertRolled(cboQuery.Text, 64);
            cboContent.InsertRolled(cboContent.Text, 64);
            cboHeaders.InsertRolled(cboHeaders.Text, 64);
            cboFileUpload.InsertRolled(cboFileUpload.Text, 64);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (txtVersion.Text.Length == 0)
                txtVersion.Text = "v1";

            if (txtUrl.Text.Length == 0)
                txtUrl.Text = "http://www.my-store.com/";

            cboPath.Focus();
        }

        private async void CallApi_Click(object sender, EventArgs e)
        {
            Clear_Click(null, null);

            using (new HourGlass())
            {
                var task = Execute();
                await task;
            }
        }

        private void CboMethod_changeCommitted(object sender, EventArgs e)
        {
            var isBodySupported = WebApiClient.BodySupported(cboMethod.Text);
            var isMultipartSupported = WebApiClient.MultipartSupported(cboMethod.Text);

            cboContent.Enabled = isBodySupported;
            btnDeleteContent.Enabled = isBodySupported;

            cboFileUpload.Enabled = isMultipartSupported;
            btnDeleteFileUpload.Enabled = isMultipartSupported;
            btnFileOpen.Enabled = isMultipartSupported;
        }

        private void BtnDeletePath_Click(object sender, EventArgs e)
        {
            cboPath.RemoveCurrent();
        }

        private void BtnDeleteQuery_Click(object sender, EventArgs e)
        {
            cboQuery.RemoveCurrent();
        }

        private void BtnDeleteContent_Click(object sender, EventArgs e)
        {
            cboContent.RemoveCurrent();
        }

        private void BtnDeleteHeaders_Click(object sender, EventArgs e)
        {
            cboHeaders.RemoveCurrent();
        }

        private void BtnDeleteFileUpload_Click(object sender, EventArgs e)
        {
            cboFileUpload.RemoveCurrent();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            txtRequest.Clear();
            lblRequest.Text = "Request";
            txtResponse.Clear();
            lblResponse.Text = "Response";

            txtRequest.Refresh();
            lblRequest.Refresh();
            txtResponse.Refresh();
            lblResponse.Refresh();
        }

        private void BtnFileOpen_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK && openFileDialog1.FileNames.Any())
            {
                FileUploadModel model = null;

                // Deserialize current model.
                if (cboFileUpload.Text.HasValue())
                {
                    try
                    {
                        model = JsonConvert.DeserializeObject(cboFileUpload.Text, typeof(FileUploadModel)) as FileUploadModel;
                    }
                    catch
                    {
                        cboFileUpload.RemoveCurrent();
                        cboFileUpload.Text = string.Empty;
                    }
                }

                if (model == null)
                {
                    model = new FileUploadModel();
                }

                // Remove files that no longer exist.
                for (var i = model.Files.Count - 1; i >= 0; --i)
                {
                    if (!File.Exists(model.Files[i].LocalPath))
                    {
                        model.Files.RemoveAt(i);
                    }
                }

                // Add new selected files.
                foreach (var fileName in openFileDialog1.FileNames)
                {
                    if (!model.Files.Any(x => x.LocalPath != null && x.LocalPath == fileName))
                    {
                        model.Files.Add(new FileUploadModel.FileModel
                        {
                            LocalPath = fileName
                        });
                    }
                }

                cboFileUpload.Text = JsonConvert.SerializeObject(model);
            }
        }
    }
}