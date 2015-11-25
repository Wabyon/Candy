using System;
using System.Windows.Forms;

namespace Candy.Updater
{
    public partial class MainForm : Form
    {
        private readonly CandyUpdater _updater;

        public MainForm(UpdateArgs args)
        {
            InitializeComponent();
            _updater = new CandyUpdater(args);

            lblMessage.Text = String.Format("{0} をアップデート中", args.ApplicationName);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            var progress = new Progress<ProgressStatus>();
            progress.ProgressChanged += (_, status) =>
            {
                progressBar1.Value = status.Percentage;
                lblStatus.Text = status.Message;
            };

            await _updater.UpdateApplicationAsync(progress);

            Close();
        }
    }
}
