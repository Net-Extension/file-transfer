using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Factory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WinFormsSample;

public partial class MainForm : Form
{
    private readonly IFileTransferClient _transferClient;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _cancellationTokenSource;

    private TextBox txtSource = null!;
    private TextBox txtDestination = null!;
    private ProgressBar progressBar = null!;
    private Label lblStatus = null!;
    private Button btnUpload = null!;
    private Button btnDownload = null!;
    private Button btnCancel = null!;
    private CheckBox chkVerifyChecksum = null!;

    public MainForm()
    {
        // Setup DI
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddFileTransfer().AddAllProviders();
        _serviceProvider = services.BuildServiceProvider();
        _transferClient = _serviceProvider.GetRequiredService<IFileTransferClient>();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "File Transfer Demo";
        this.Size = new Size(600, 350);

        var lblSourceLabel = new Label
        {
            Text = "Source:",
            Location = new Point(20, 20),
            Size = new Size(80, 23)
        };

        txtSource = new TextBox
        {
            Location = new Point(110, 20),
            Size = new Size(350, 23)
        };

        var btnBrowseSource = new Button
        {
            Text = "Browse...",
            Location = new Point(470, 19),
            Size = new Size(90, 25)
        };
        btnBrowseSource.Click += BtnBrowseSource_Click;

        var lblDestLabel = new Label
        {
            Text = "Destination:",
            Location = new Point(20, 55),
            Size = new Size(80, 23)
        };

        txtDestination = new TextBox
        {
            Location = new Point(110, 55),
            Size = new Size(450, 23),
            Text = "https://api.example.com/upload"
        };

        chkVerifyChecksum = new CheckBox
        {
            Text = "Verify Checksum",
            Location = new Point(110, 90),
            Size = new Size(200, 23),
            Checked = true
        };

        btnUpload = new Button
        {
            Text = "Upload",
            Location = new Point(110, 130),
            Size = new Size(100, 30)
        };
        btnUpload.Click += async (s, e) => await BtnUpload_Click(s, e);

        btnDownload = new Button
        {
            Text = "Download",
            Location = new Point(220, 130),
            Size = new Size(100, 30)
        };
        btnDownload.Click += async (s, e) => await BtnDownload_Click(s, e);

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(330, 130),
            Size = new Size(100, 30),
            Enabled = false
        };
        btnCancel.Click += BtnCancel_Click;

        progressBar = new ProgressBar
        {
            Location = new Point(110, 180),
            Size = new Size(450, 30),
            Minimum = 0,
            Maximum = 100
        };

        lblStatus = new Label
        {
            Text = "Ready",
            Location = new Point(110, 220),
            Size = new Size(450, 60),
            AutoSize = false
        };

        this.Controls.Add(lblSourceLabel);
        this.Controls.Add(txtSource);
        this.Controls.Add(btnBrowseSource);
        this.Controls.Add(lblDestLabel);
        this.Controls.Add(txtDestination);
        this.Controls.Add(chkVerifyChecksum);
        this.Controls.Add(btnUpload);
        this.Controls.Add(btnDownload);
        this.Controls.Add(btnCancel);
        this.Controls.Add(progressBar);
        this.Controls.Add(lblStatus);
    }

    private void BtnBrowseSource_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            txtSource.Text = openFileDialog.FileName;
        }
    }

    private async Task BtnUpload_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSource.Text) || string.IsNullOrWhiteSpace(txtDestination.Text))
        {
            MessageBox.Show("Please specify source file and destination URL.", "Validation Error");
            return;
        }

        await PerformTransferAsync(isUpload: true);
    }

    private async Task BtnDownload_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSource.Text) || string.IsNullOrWhiteSpace(txtDestination.Text))
        {
            MessageBox.Show("Please specify source URL and destination file.", "Validation Error");
            return;
        }

        await PerformTransferAsync(isUpload: false);
    }

    private async Task PerformTransferAsync(bool isUpload)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        btnUpload.Enabled = false;
        btnDownload.Enabled = false;
        btnCancel.Enabled = true;
        progressBar.Value = 0;

        try
        {
            var request = new TransferRequest
            {
                Source = isUpload ? txtSource.Text : txtSource.Text,
                Destination = isUpload ? txtDestination.Text : txtDestination.Text,
                Options = new TransferOptions
                {
                    VerifyChecksum = chkVerifyChecksum.Checked
                },
                Progress = new Progress<TransferProgress>(progress =>
                {
                    // Update UI on UI thread
                    if (InvokeRequired)
                    {
                        Invoke(() => UpdateProgress(progress));
                    }
                    else
                    {
                        UpdateProgress(progress);
                    }
                })
            };

            TransferResult result;
            if (isUpload)
            {
                lblStatus.Text = "Uploading...";
                result = await _transferClient.UploadAsync(request, _cancellationTokenSource.Token);
            }
            else
            {
                lblStatus.Text = "Downloading...";
                result = await _transferClient.DownloadAsync(request, _cancellationTokenSource.Token);
            }

            if (result.Success)
            {
                lblStatus.Text = $"Success! Transferred {result.BytesTransferred:N0} bytes in {result.Duration.TotalSeconds:F2} seconds";
                MessageBox.Show($"Transfer completed successfully!\n\nBytes: {result.BytesTransferred:N0}\nDuration: {result.Duration}", "Success");
            }
            else
            {
                lblStatus.Text = $"Failed: {result.Error?.Message}";
                MessageBox.Show($"Transfer failed: {result.Error?.Message}", "Error");
            }
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Transfer cancelled by user";
            MessageBox.Show("Transfer was cancelled.", "Cancelled");
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error during transfer: {ex.Message}", "Error");
        }
        finally
        {
            btnUpload.Enabled = true;
            btnDownload.Enabled = true;
            btnCancel.Enabled = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void UpdateProgress(TransferProgress progress)
    {
        progressBar.Value = Math.Min((int)progress.PercentComplete, 100);
        lblStatus.Text = $"{progress.PercentComplete:F1}% - {progress.BytesTransferred:N0} / {progress.TotalBytes:N0} bytes " +
                        $"({progress.RateBytesPerSecond / 1024 / 1024:F2} MB/s)";
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
        base.Dispose(disposing);
    }
}
