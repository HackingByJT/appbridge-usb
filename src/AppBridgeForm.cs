using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppBridgeUsb
{
    internal sealed class AppBridgeForm : Form
    {
        private static readonly Color Ink = Color.FromArgb(24, 42, 56);
        private static readonly Color Muted = Color.FromArgb(78, 96, 108);
        private static readonly Color Accent = Color.FromArgb(0, 102, 104);
        private static readonly Color Canvas = Color.FromArgb(242, 245, 247);
        private static readonly Color Border = Color.FromArgb(216, 224, 229);
        private static readonly Color ErrorColor = Color.FromArgb(164, 39, 40);
        private const string XuperPackage = "com.msandroid.mobile";

        private readonly RuntimeManager runtime = new RuntimeManager();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly ComboBox devices = new ComboBox();
        private readonly ComboBox packages = new ComboBox();
        private readonly Button prepare = new Button();
        private readonly Button refresh = new Button();
        private readonly Button listApps = new Button();
        private readonly Button startSession = new Button();
        private readonly LinkLabel xuperProfile = new LinkLabel();
        private readonly Label runtimeTitle = new Label();
        private readonly Label runtimeDetail = new Label();
        private readonly Label deviceHelp = new Label();
        private readonly Label packageHelp = new Label();
        private readonly Label statusTitle = new Label();
        private readonly Label statusDetail = new Label();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly CheckBox fullscreen = new CheckBox();
        private readonly CheckBox screenOff = new CheckBox();
        private readonly CheckBox audio = new CheckBox();
        private readonly CheckBox restart = new CheckBox();
        private readonly ToolTip hints = new ToolTip();
        private bool busy, preparing, updating, closing;
        private Process session;

        internal AppBridgeForm()
        {
            SuspendLayout();
            Text = "AppBridge USB";
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = Ink;
            BackColor = Canvas;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(960, 880);
            MinimumSize = new Size(840, 720);
            BuildInterface();
            WireEvents();
            ResumeLayout(true);
            UpdateControls();
            ApplyIdleStatus();
        }

        private void BuildInterface()
        {
            Panel scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Canvas };
            Controls.Add(scroll);
            TableLayoutPanel root = new TableLayoutPanel {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, RowCount = 3, Padding = new Padding(24, 24, 24, 8), Margin = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scroll.Controls.Add(root);
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildRuntime(), 0, 1);

            TableLayoutPanel body = new TableLayoutPanel {
                Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1,
                Margin = new Padding(0, 16, 0, 0), Padding = Padding.Empty
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            TableLayoutPanel inputs = new TableLayoutPanel {
                Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 3,
                Margin = new Padding(0, 0, 16, 0), Padding = Padding.Empty
            };
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 144));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
            inputs.Controls.Add(BuildDevice(), 0, 0);
            inputs.Controls.Add(BuildPackage(), 0, 1);
            inputs.Controls.Add(BuildOptions(), 0, 2);
            body.Controls.Add(inputs, 0, 0);
            body.Controls.Add(BuildGuide(), 1, 0);
            root.Controls.Add(body, 0, 2);
            TableLayoutPanel actions = new TableLayoutPanel {
                Dock = DockStyle.Bottom, Height = 154, BackColor = Canvas,
                ColumnCount = 1, RowCount = 2, Padding = new Padding(24, 12, 24, 12), Margin = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(actions);
            actions.Controls.Add(BuildActions(), 0, 0);
            actions.Controls.Add(new Label {
                Text = "AppBridge USB 0.1.0 beta  /  Windows x64",
                ForeColor = Muted, Dock = DockStyle.Fill, Height = 26,
                TextAlign = ContentAlignment.BottomLeft, Margin = new Padding(0, 8, 0, 0),
                Font = new Font(Font.FontFamily, 8.5F)
            }, 0, 1);
        }

        private Control BuildHeader()
        {
            TableLayoutPanel header = new TableLayoutPanel {
                Dock = DockStyle.Fill, Height = 88, ColumnCount = 3, RowCount = 1,
                Margin = Padding.Empty, Padding = Padding.Empty
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
            header.Controls.Add(new BridgeMark { Size = new Size(48, 48), Margin = new Padding(0, 3, 0, 0) }, 0, 0);
            Panel words = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            words.Controls.Add(new Label {
                Text = "AppBridge USB", Font = new Font(Font.FontFamily, 23, FontStyle.Bold),
                Dock = DockStyle.Top, Height = 42, ForeColor = Ink
            });
            Label subtitle = new Label {
                Text = "Your Android app. A dedicated window. Connected by USB.",
                ForeColor = Muted, Dock = DockStyle.Bottom, Height = 35
            };
            words.Controls.Add(subtitle);
            header.Controls.Add(words, 1, 0);
            header.Controls.Add(new Label {
                Text = "BETA 0.1.0", BackColor = Color.FromArgb(221, 237, 237), ForeColor = Accent,
                Height = 28, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold), Margin = new Padding(8, 8, 0, 0)
            }, 2, 0);
            return header;
        }

        private Control BuildRuntime()
        {
            TableLayoutPanel card = Card(100, new Padding(0));
            card.ColumnCount = 2;
            card.RowCount = 3;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            ConfigureLabel(runtimeTitle, "Components", true);
            ConfigureLabel(runtimeDetail, "", false);
            runtimeDetail.Font = new Font(Font.FontFamily, 8.75F);
            ConfigureButton(prepare, "&Prepare components", false);
            prepare.Dock = DockStyle.Fill;
            prepare.Margin = new Padding(12, 4, 0, 12);
            prepare.AccessibleName = "Prepare components";
            card.Controls.Add(runtimeTitle, 0, 0);
            card.Controls.Add(runtimeDetail, 0, 1);
            card.Controls.Add(prepare, 1, 0);
            card.SetRowSpan(prepare, 2);
            progress.Dock = DockStyle.Fill;
            progress.Height = 5;
            progress.Margin = Padding.Empty;
            progress.AccessibleName = "Component preparation progress";
            card.Controls.Add(progress, 0, 2);
            card.SetColumnSpan(progress, 2);
            hints.SetToolTip(prepare, "Download scrcpy 5.0.1 from its official release and verify its SHA-256 checksum.");
            return card;
        }

        private Control BuildDevice()
        {
            TableLayoutPanel card = Card(132, new Padding(0, 0, 0, 12));
            AddInputColumns(card, 138);
            AddSectionHeading(card, "01  USB device");
            devices.DropDownStyle = ComboBoxStyle.DropDownList;
            devices.Dock = DockStyle.Fill;
            devices.Margin = new Padding(0, 3, 8, 3);
            devices.AccessibleName = "USB device";
            ConfigureButton(refresh, "&Refresh devices", false);
            refresh.Dock = DockStyle.Fill;
            refresh.Margin = new Padding(0, 1, 0, 1);
            card.Controls.Add(devices, 0, 1);
            card.Controls.Add(refresh, 1, 1);
            ConfigureLabel(deviceHelp, "Connect a phone with a data cable and allow USB debugging.", false);
            deviceHelp.Font = new Font(Font.FontFamily, 8.75F);
            deviceHelp.Margin = new Padding(0, 8, 0, 0);
            card.Controls.Add(deviceHelp, 0, 2);
            card.SetColumnSpan(deviceHelp, 2);
            return card;
        }

        private Control BuildPackage()
        {
            TableLayoutPanel card = Card(158, new Padding(0, 0, 0, 12));
            AddInputColumns(card, 100);
            card.RowCount = 4;
            AddSectionHeading(card, "02  Android package");
            packages.DropDownStyle = ComboBoxStyle.DropDown;
            packages.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            packages.AutoCompleteSource = AutoCompleteSource.ListItems;
            packages.Dock = DockStyle.Fill;
            packages.Margin = new Padding(0, 3, 8, 3);
            packages.MaxLength = 255;
            packages.AccessibleName = "Android package";
            ConfigureButton(listApps, "&List apps", false);
            listApps.Dock = DockStyle.Fill;
            listApps.Margin = new Padding(0, 1, 0, 1);
            card.Controls.Add(packages, 0, 1);
            card.Controls.Add(listApps, 1, 1);
            ConfigureLabel(packageHelp, "Choose an installed package or enter its ID, e.g. org.videolan.vlc.", false);
            packageHelp.Font = new Font(Font.FontFamily, 8.75F);
            packageHelp.Margin = new Padding(0, 8, 0, 0);
            card.Controls.Add(packageHelp, 0, 2);
            card.SetColumnSpan(packageHelp, 2);
            xuperProfile.Text = "Use XuperTV";
            xuperProfile.LinkColor = Accent;
            xuperProfile.ActiveLinkColor = Ink;
            xuperProfile.VisitedLinkColor = Accent;
            xuperProfile.AutoSize = true;
            xuperProfile.Margin = new Padding(0, 2, 0, 0);
            xuperProfile.Visible = false;
            card.Controls.Add(xuperProfile, 0, 3);
            card.SetColumnSpan(xuperProfile, 2);
            hints.SetToolTip(xuperProfile, "Select the XuperTV package already installed on this phone.");
            return card;
        }

        private Control BuildOptions()
        {
            TableLayoutPanel card = Card(154, Padding.Empty);
            card.ColumnCount = 2;
            card.RowCount = 4;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            AddSectionHeading(card, "03  Session options");
            ConfigureOption(fullscreen, "&Fullscreen", true);
            ConfigureOption(screenOff, "Turn phone screen &off", true);
            ConfigureOption(audio, "Forward device &audio", true);
            ConfigureOption(restart, "R&estart app to apply scale", false);
            card.Controls.Add(fullscreen, 0, 1);
            card.Controls.Add(screenOff, 1, 1);
            card.Controls.Add(audio, 0, 2);
            card.Controls.Add(restart, 1, 2);
            Label audioHint = new Label {
                Text = "Audio can include sound from other apps on the phone.",
                Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font(Font.FontFamily, 8.5F),
                Margin = new Padding(0, 4, 0, 0)
            };
            card.Controls.Add(audioHint, 0, 3);
            card.SetColumnSpan(audioHint, 2);
            hints.SetToolTip(screenOff, "Turn off the physical phone panel while the virtual display stays active.");
            hints.SetToolTip(restart, "Force-stop the selected app before launching it so the new display scale takes effect. Unsaved app work may be lost.");
            hints.SetToolTip(audio, "Forward the device audio output to this computer. Audio availability depends on Android and the app.");
            return card;
        }

        private Control BuildGuide()
        {
            TableLayoutPanel card = Card(468, Padding.Empty);
            card.BackColor = Color.FromArgb(231, 239, 240);
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.RowCount = 6;
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            Label heading = new Label();
            ConfigureLabel(heading, "A window for one app", true);
            heading.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            card.Controls.Add(heading, 0, 0);
            card.Controls.Add(new Label {
                Text = "USB ONLY  /  ANDROID 12+", Dock = DockStyle.Fill,
                ForeColor = Accent, Font = new Font(Font.FontFamily, 8, FontStyle.Bold)
            }, 0, 1);
            card.Controls.Add(GuideStep("Connect your phone", "Enable USB debugging, unlock the phone and accept the authorization prompt."), 0, 2);
            card.Controls.Add(GuideStep("Keep the mobile layout", "The app starts on its own portrait virtual display, using the phone's size and density."), 0, 3);
            card.Controls.Add(GuideStep("Control your session", "F12 enters fullscreen. Esc returns to a window. Close that window to finish."), 0, 4);
            card.Controls.Add(new Label {
                Text = "Beta compatibility varies by app and device. Some apps or protected content may not work on a virtual display.",
                Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font(Font.FontFamily, 8.5F),
                Margin = new Padding(0, 12, 0, 0)
            }, 0, 5);
            return card;
        }

        private Control BuildActions()
        {
            TableLayoutPanel card = Card(102, Padding.Empty);
            card.ColumnCount = 2;
            card.RowCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
            TableLayoutPanel status = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Margin = Padding.Empty, Padding = Padding.Empty
            };
            status.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            status.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            ConfigureLabel(statusTitle, "", true);
            ConfigureLabel(statusDetail, "", false);
            statusDetail.Font = new Font(Font.FontFamily, 8.75F);
            status.Controls.Add(statusTitle, 0, 0);
            status.Controls.Add(statusDetail, 0, 1);
            ConfigureButton(startSession, "&Start USB session", true);
            startSession.Dock = DockStyle.Fill;
            startSession.Margin = new Padding(16, 8, 0, 8);
            card.Controls.Add(status, 0, 0);
            card.Controls.Add(startSession, 1, 0);
            return card;
        }

        private static TableLayoutPanel Card(int height, Padding margin)
        {
            TableLayoutPanel card = new TableLayoutPanel {
                Height = height, Dock = DockStyle.Fill, Margin = margin,
                Padding = new Padding(16), BackColor = Color.White
            };
            card.Paint += delegate(object sender, PaintEventArgs args) {
                using (Pen pen = new Pen(Border))
                    args.Graphics.DrawRectangle(pen, 0, 0, card.ClientSize.Width - 1, card.ClientSize.Height - 1);
            };
            return card;
        }

        private static void AddInputColumns(TableLayoutPanel card, int buttonWidth)
        {
            card.ColumnCount = 2;
            card.RowCount = 3;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttonWidth));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        private void AddSectionHeading(TableLayoutPanel card, string text)
        {
            Label heading = new Label();
            ConfigureLabel(heading, text, true);
            card.Controls.Add(heading, 0, 0);
            card.SetColumnSpan(heading, card.ColumnCount);
        }

        private void ConfigureLabel(Label label, string text, bool strong)
        {
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.ForeColor = strong ? Ink : Muted;
            label.Font = new Font(Font.FontFamily, strong ? 10 : 9, strong ? FontStyle.Bold : FontStyle.Regular);
            label.AccessibleRole = AccessibleRole.StaticText;
        }

        private static void ConfigureButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = primary ? Accent : Border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = primary ? Accent : Color.White;
            button.ForeColor = primary ? Color.White : Ink;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        private static void ConfigureOption(CheckBox option, string text, bool selected)
        {
            option.Text = text;
            option.Checked = selected;
            option.Dock = DockStyle.Fill;
            option.Margin = Padding.Empty;
            option.AutoSize = false;
            option.UseVisualStyleBackColor = true;
        }

        private Control GuideStep(string title, string detail)
        {
            TableLayoutPanel step = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Margin = Padding.Empty, Padding = Padding.Empty
            };
            step.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            step.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label caption = new Label(), body = new Label();
            ConfigureLabel(caption, title, true);
            ConfigureLabel(body, detail, false);
            body.Font = new Font(Font.FontFamily, 9);
            step.Controls.Add(caption, 0, 0);
            step.Controls.Add(body, 0, 1);
            return step;
        }

        private void WireEvents()
        {
            Load += delegate {
                Rectangle workArea = Screen.FromControl(this).WorkingArea;
                int availableWidth = Math.Max(320, workArea.Width - 32);
                int availableHeight = Math.Max(320, workArea.Height - 32);
                MinimumSize = new Size(Math.Min(MinimumSize.Width, availableWidth), Math.Min(MinimumSize.Height, availableHeight));
                if (Width > availableWidth || Height > availableHeight)
                {
                    Size = new Size(Math.Min(Width, availableWidth), Math.Min(Height, availableHeight));
                    Location = new Point(workArea.Left + (workArea.Width - Width) / 2, workArea.Top + (workArea.Height - Height) / 2);
                }
            };
            Shown += async delegate {
                if (runtime.IsReady) await RefreshDevicesAsync();
            };
            prepare.Click += async delegate { await PrepareAsync(); };
            refresh.Click += async delegate { await RefreshDevicesAsync(); };
            listApps.Click += async delegate { await ListAppsAsync(); };
            startSession.Click += async delegate { await StartSessionAsync(); };
            devices.SelectedIndexChanged += delegate {
                if (updating) return;
                ClearPackages();
                UpdateControls();
                ApplyIdleStatus();
            };
            packages.TextChanged += delegate {
                if (updating) return;
                UpdateControls();
                if (!busy && session == null) ApplyIdleStatus();
            };
            xuperProfile.LinkClicked += delegate { packages.Text = XuperPackage; };
            FormClosing += delegate {
                closing = true;
                lifetime.Cancel();
                // The independent controller owns scrcpy until its window is closed.
                if (session != null)
                {
                    session.Exited -= SessionExited;
                    session.Dispose();
                    session = null;
                }
            };
            FormClosed += delegate { hints.Dispose(); };
        }

        private async Task PrepareAsync()
        {
            if (busy || runtime.IsReady) return;
            busy = preparing = true;
            progress.Value = 0;
            UpdateControls();
            SetStatus("Preparing components", "Downloading and verifying the official scrcpy release. This may take a moment.", false);
            try
            {
                Progress<int> report = new Progress<int>(delegate(int value) {
                    if (closing) return;
                    progress.Value = Math.Max(0, Math.Min(100, value));
                    runtimeDetail.Text = "Downloading and verifying components... " + progress.Value + "%";
                });
                await runtime.EnsureAsync(report, lifetime.Token);
                if (closing) return;
                progress.Value = 100;
                SetStatus("Components ready", "Connect an Android phone with a USB data cable.", false);
            }
            catch (OperationCanceledException) { if (!closing) SetStatus("Preparation cancelled", "Select Prepare components to try again.", false); }
            catch (Exception error) { ShowFailure("Could not prepare components", error); }
            finally
            {
                busy = preparing = false;
                UpdateControls();
            }
            if (!closing && runtime.IsReady) await RefreshDevicesAsync();
        }

        private async Task RefreshDevicesAsync()
        {
            if (closing || busy || !runtime.IsReady || session != null) return;
            string previous = SelectedDevice == null ? null : SelectedDevice.Serial;
            busy = true;
            UpdateControls();
            SetStatus("Looking for USB devices", "Unlock the phone and accept its USB debugging prompt if it appears.", false);
            try
            {
                List<UsbDevice> found = await new AdbService(runtime.AdbPath).GetUsbDevicesAsync(lifetime.Token);
                if (closing) return;
                updating = true;
                try
                {
                    devices.Items.Clear();
                    foreach (UsbDevice device in found) devices.Items.Add(device);
                    int selected = found.FindIndex(device => String.Equals(device.Serial, previous, StringComparison.Ordinal));
                    devices.SelectedIndex = selected >= 0 ? selected : found.Count > 0 ? 0 : -1;
                    string current = SelectedDevice == null ? null : SelectedDevice.Serial;
                    if (!String.Equals(current, previous, StringComparison.Ordinal)) ClearPackages();
                }
                finally { updating = false; }
                ApplyIdleStatus();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { ShowFailure("Could not refresh devices", error); }
            finally { busy = false; UpdateControls(); }
        }

        private async Task ListAppsAsync()
        {
            UsbDevice device = SelectedDevice;
            if (closing || busy || device == null || !device.IsReady || session != null) return;
            string entered = packages.Text.Trim();
            busy = true;
            UpdateControls();
            SetStatus("Reading installed apps", "Only installed package IDs are requested from the selected USB phone.", false);
            try
            {
                List<string> found = await new AdbService(runtime.AdbPath).ListPackagesAsync(device.Serial, lifetime.Token);
                if (closing) return;
                updating = true;
                try
                {
                    packages.Items.Clear();
                    packages.Items.AddRange(found.ToArray());
                    packages.SelectedIndex = -1;
                    packages.Text = entered;
                    xuperProfile.Visible = found.Contains(XuperPackage);
                    packageHelp.Text = found.Count + " installed packages. Choose one or enter its package ID.";
                }
                finally { updating = false; }
                ApplyIdleStatus();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { ShowFailure("Could not list apps", error); }
            finally { busy = false; UpdateControls(); }
        }

        private async Task StartSessionAsync()
        {
            UsbDevice device = SelectedDevice;
            if (closing || busy || session != null || device == null || !device.IsReady || !runtime.IsReady) return;
            LaunchOptions options = new LaunchOptions {
                PackageName = packages.Text.Trim(), Fullscreen = fullscreen.Checked,
                ScreenOff = screenOff.Checked, Audio = audio.Checked, RestartApp = restart.Checked
            };
            busy = true;
            UpdateControls();
            SetStatus("Checking your phone and app", "Reading the current display size and density for the virtual display.", false);
            try
            {
                SessionDetails details = await new AdbService(runtime.AdbPath).ReadSessionDetailsAsync(device.Serial, options.PackageName, lifetime.Token);
                if (closing) return;
                string controller = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppBridgeControls.exe");
                if (!File.Exists(controller))
                    throw new InvalidOperationException("AppBridgeControls.exe is missing. Reinstall AppBridge USB or extract the complete portable ZIP.");
                List<string> arguments = BuildSessionArguments(device.Serial, options, details.Display);
                arguments.Insert(0, runtime.ScrcpyPath);
                Process player = new Process();
                player.StartInfo = new ProcessStartInfo(controller, WindowsArguments.Join(arguments)) {
                    UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = runtime.ToolsDirectory
                };
                AdbService.UseLocalServer(player.StartInfo);
                player.StartInfo.EnvironmentVariables["ADB"] = runtime.AdbPath;
                player.EnableRaisingEvents = true;
                player.Exited += SessionExited;
                session = player;
                try
                {
                    if (!player.Start()) throw new InvalidOperationException("The session controller could not start. Try again.");
                }
                catch
                {
                    session = null;
                    player.Exited -= SessionExited;
                    player.Dispose();
                    throw;
                }
                SetStatus("Session running", "F12 enters fullscreen. Esc returns to a window. Close the session window to finish.", false);
                deviceHelp.Text = "Android API " + details.ApiLevel + "  /  Virtual display " + details.Display.ToScrcpyValue();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { ShowFailure("Could not start the session", error); }
            finally { busy = false; UpdateControls(); }
        }

        private static List<string> BuildSessionArguments(string serial, LaunchOptions options, DisplayMetrics display)
        {
            List<string> arguments = new List<string> {
                "--serial=" + serial,
                "--new-display=" + display.ToScrcpyValue(),
                "--start-app=" + (options.RestartApp ? "+" : "") + options.PackageName,
                "--no-vd-system-decorations", "--stay-awake", "--keep-active",
                "--max-size=1920", "--max-fps=30", "--video-bit-rate=6M",
                "--no-clipboard-autosync", "--background-color=000000",
                "--window-height=900", "--window-title=AppBridge USB"
            };
            if (options.Fullscreen) arguments.Add("--fullscreen");
            if (options.ScreenOff) arguments.Add("--turn-screen-off");
            arguments.Add(options.Audio ? "--audio-source=output" : "--no-audio");
            return arguments;
        }

        private void SessionExited(object sender, EventArgs args)
        {
            Process player = (Process)sender;
            int code;
            try { code = player.ExitCode; }
            catch (InvalidOperationException) { return; }
            if (closing || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)delegate {
                    if (closing || !Object.ReferenceEquals(session, player)) return;
                    session = null;
                    player.Exited -= SessionExited;
                    player.Dispose();
                    UpdateControls();
                    if (code == 0)
                        SetStatus("Session closed", "Your USB session has finished. You can start another session when ready.", false);
                    else
                        SetStatus("Session ended", "Check the USB cable and any message from the session controller. App and device compatibility varies in this beta.", true);
                });
            }
            catch (InvalidOperationException) { }
        }

        private UsbDevice SelectedDevice { get { return devices.SelectedItem as UsbDevice; } }

        private void ClearPackages()
        {
            bool wasUpdating = updating;
            updating = true;
            try
            {
                packages.Items.Clear();
                packages.Text = "";
                xuperProfile.Visible = false;
                packageHelp.Text = "Choose an installed package or enter its ID, e.g. org.videolan.vlc.";
            }
            finally { updating = wasUpdating; }
        }

        private void UpdateControls()
        {
            if (closing) return;
            bool ready = runtime.IsReady;
            bool idle = !busy && session == null;
            UsbDevice device = SelectedDevice;
            bool usable = ready && device != null && device.IsReady;
            prepare.Enabled = idle && !ready;
            prepare.Text = preparing ? "Preparing..." : ready ? "Components ready" : "&Prepare components";
            runtimeTitle.Text = ready ? "Components ready" : preparing ? "Preparing components" : "Prepare your components";
            if (!preparing)
            {
                runtimeDetail.Text = ready
                    ? "scrcpy 5.0.1 is available in your Windows user profile."
                    : "Download the official scrcpy release once. Its SHA-256 checksum is verified.";
                progress.Value = ready ? 100 : 0;
            }
            devices.Enabled = idle && ready;
            refresh.Enabled = idle && ready;
            packages.Enabled = idle && usable;
            listApps.Enabled = idle && usable;
            xuperProfile.Enabled = idle && usable;
            fullscreen.Enabled = screenOff.Enabled = audio.Enabled = restart.Enabled = idle;
            startSession.Enabled = idle && usable && AdbService.IsPackageName(packages.Text.Trim());
            startSession.Text = session != null ? "Session running" : busy && !preparing ? "Please wait..." : "&Start USB session";
            if (device == null)
                deviceHelp.Text = "Connect a phone with a data cable and allow USB debugging.";
            else if (!device.IsReady)
                deviceHelp.Text = device.State == "unauthorized" ? "Unlock the phone and accept its USB debugging prompt, then refresh." : "This USB phone is offline. Reconnect it, unlock it and refresh.";
            else if (session == null)
                deviceHelp.Text = "Physical USB connected. Android 12 or newer is required for this beta.";
        }

        private void ApplyIdleStatus()
        {
            if (closing || session != null) return;
            if (!runtime.IsReady)
                SetStatus("Prepare components to begin", "Select Prepare components to download the verified tools used by USB sessions.", false);
            else if (SelectedDevice == null)
                SetStatus("Connect your Android phone", "Use a USB data cable, enable USB debugging, then select Refresh devices.", false);
            else if (!SelectedDevice.IsReady)
                SetStatus("Phone authorization required", "Unlock the phone, allow USB debugging and select Refresh devices.", false);
            else if (!AdbService.IsPackageName(packages.Text.Trim()))
                SetStatus("Choose an Android app", "Select List apps or enter an installed Android package ID to continue.", false);
            else
                SetStatus("Ready to start", "Your selected app will open in a separate virtual display on this USB phone.", false);
        }

        private void SetStatus(string title, string detail, bool error)
        {
            if (closing) return;
            statusTitle.Text = title;
            statusTitle.ForeColor = error ? ErrorColor : Ink;
            statusDetail.Text = detail;
            statusDetail.ForeColor = error ? ErrorColor : Muted;
            statusTitle.AccessibleName = title;
            statusDetail.AccessibleName = detail;
            hints.SetToolTip(statusDetail, detail);
        }

        private void ShowFailure(string title, Exception error)
        {
            if (closing) return;
            // Device serials and raw ADB output are deliberately never surfaced or persisted.
            string message = error is System.ComponentModel.Win32Exception
                ? "A required component could not start. Check that the complete app and its prepared components are available."
                : error.Message;
            SetStatus(title, message, true);
            hints.SetToolTip(statusDetail, message);
        }

        private sealed class BridgeMark : Control
        {
            internal BridgeMark()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                AccessibleRole = AccessibleRole.Graphic;
                AccessibleName = "AppBridge USB";
            }

            protected override void OnPaint(PaintEventArgs args)
            {
                base.OnPaint(args);
                float scale = Math.Min(Width, Height) / 48F;
                args.Graphics.ScaleTransform(scale, scale);
                args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush background = new SolidBrush(Accent)) args.Graphics.FillRectangle(background, 0, 0, 48, 48);
                using (Pen pen = new Pen(Color.White, 2))
                {
                    args.Graphics.DrawRectangle(pen, 10, 10, 14, 28);
                    args.Graphics.DrawRectangle(pen, 28, 15, 11, 18);
                    args.Graphics.DrawLine(pen, 24, 24, 28, 24);
                    args.Graphics.DrawLine(pen, 14, 33, 20, 33);
                }
            }
        }
    }
}
