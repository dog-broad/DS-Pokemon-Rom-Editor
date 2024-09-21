using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace DSPRE
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // Check if the button is in "Close" mode
            if (buttonCancel.Text == "Close")
            {
                this.Close();
                return;
            }

            // Ask user for confirmation to cancel the operation
            if (MessageBox.Show("Are you sure you want to cancel?", "Cancel", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }
        }

        public BackgroundWorker BackgroundWorker => backgroundWorker;

        public ProgressBar ProgressBar => progressBar;

        public ListBox ListBoxLogs => listBoxLogs;

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            backgroundWorker.RunWorkerAsync();
        }

        private void InitializeComponent()
        {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.listBoxLogs = new System.Windows.Forms.ListBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 12);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(776, 23);
            this.progressBar.TabIndex = 0;
            // 
            // listBoxLogs
            // 
            this.listBoxLogs.FormattingEnabled = true;
            this.listBoxLogs.ItemHeight = 16;
            this.listBoxLogs.Location = new System.Drawing.Point(12, 41);
            this.listBoxLogs.Name = "listBoxLogs";
            this.listBoxLogs.Size = new System.Drawing.Size(776, 356);
            this.listBoxLogs.TabIndex = 1;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(713, 403);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.WorkerReportsProgress = true;
            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            // 
            // ProgressForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.listBoxLogs);
            this.Controls.Add(this.progressBar);
            this.Name = "ProgressForm";
            this.Load += new System.EventHandler(this.ProgressForm_Load);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.ListBox listBoxLogs;
        private System.Windows.Forms.Button buttonCancel;
        private System.ComponentModel.BackgroundWorker backgroundWorker;

        internal void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string sourcePath = Path.Combine(RomInfo.workDir, "..", "script_export");
            string destinationPath = Path.Combine(RomInfo.workDir, "script_export");

            try
            {
                Directory.CreateDirectory(destinationPath);
                string[] files = Directory.GetFiles(sourcePath);

                for (int i = 0; i < files.Length; i++)
                {
                    if (backgroundWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    string file = files[i];
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationPath, fileName);

                    File.Copy(file, destFile, true);

                    // Report progress
                    backgroundWorker.ReportProgress((i + 1) * 100 / files.Length, $"Copied: {fileName}");
                }
            }
            catch (Exception ex)
            {
                e.Result = ex.Message;
            }
        }

        internal void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            listBoxLogs.Items.Add(e.UserState.ToString());
            // Ensure the latest log entry is visible
            listBoxLogs.TopIndex = listBoxLogs.Items.Count - 1;
        }

        internal void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                listBoxLogs.Items.Add("Operation cancelled by the user.");
            }
            else if (e.Error != null)
            {
                listBoxLogs.Items.Add($"An error occurred: {e.Error.Message}");
            }
            else if (e.Result != null)
            {
                listBoxLogs.Items.Add($"An error occurred: {e.Result}");
            }
            else
            {
                listBoxLogs.Items.Add("All files have been moved and replaced successfully.");
            }
            buttonCancel.Text = "Close";
        }
    }
}
