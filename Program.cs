using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WindowsStriper
{
    public class MainForm : Form
    {
        private Label lblUser;
        private Label lblMachine;
        private Label lblEdition;
        private Label lblStatus;
        private Label lblKey;
        private Label lblChannel;
        private Label lblGenuine;
        private Label lblOffice;
        private string osppPath;
        private TextBox txtLog;
        private Button btnRefresh;
        private Button btnStrip;
        private Button btnTest;
        private LinkLabel lnkUpdate;

        // ApplicationId cố định cho sản phẩm Windows
        private const string WindowsAppId = "55c92734-d682-4d71-983e-d6ec3f16059f";

        // ===== Cấu hình cập nhật OTA (đổi sang GitHub của bạn) =====
        private const string AppVersion  = "1.0.0";
        private const string UpdateOwner = "cybervinh2077";    // <-- GitHub username
        private const string UpdateRepo  = "Windows-Stripper"; // <-- tên repo
        // Tool sẽ đọc release mới nhất tại:
        //   https://api.github.com/repos/{owner}/{repo}/releases/latest
        // và tải asset .exe đính kèm trong release đó.

        public MainForm()
        {
#if TEST_BUILD
            Text = "Windows License Remover v" + AppVersion + "  [TEST MODE - KHÔNG THỰC THI]";
#else
            Text = "Windows License Remover v" + AppVersion;
#endif
            AutoScaleMode = AutoScaleMode.Dpi;   // co giãn theo DPI màn hình
            Size = new Size(640, 640);
            MinimumSize = new Size(520, 420);    // vẫn dùng được trên màn hình nhỏ
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;  // cho phép kéo to/nhỏ
            MaximizeBox = true;
            AutoScroll = true;                   // hiện thanh cuộn khi cửa sổ nhỏ hơn nội dung
            Font = new Font("Segoe UI", 9.5f);

            // Dùng icon của chính file exe cho thanh tiêu đề cửa sổ
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

#if TEST_BUILD
            BackColor = Color.FromArgb(235, 242, 252); // nền xanh nhạt: nhận biết bản TEST
#endif

            int x = 20, y = 20, lh = 30;

            AddHeader("THÔNG TIN MÁY", ref y);
            lblUser    = AddInfoRow("Tên người dùng:", x, ref y, lh);
            lblMachine = AddInfoRow("Tên máy:",        x, ref y, lh);
            lblEdition = AddInfoRow("Phiên bản Windows:", x, ref y, lh);

            y += 8;
            AddHeader("TRẠNG THÁI BẢN QUYỀN", ref y);
            lblStatus  = AddInfoRow("Trạng thái:",   x, ref y, lh);
            lblKey     = AddInfoRow("Product Key:",  x, ref y, lh);
            lblChannel = AddInfoRow("Kênh kích hoạt:", x, ref y, lh);
            lblGenuine = AddInfoRow("Đánh giá nguồn gốc:", x, ref y, lh);
            lblGenuine.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblOffice  = AddInfoRow("Office:", x, ref y, lh);

            y += 12;
            btnRefresh = new Button
            {
                Text = "🔄 Làm mới",
                Location = new Point(x, y),
                Size = new Size(110, 36)
            };
            btnRefresh.Click += (s, e) => LoadInfo();

            btnTest = new Button
            {
                Text = "🧪 Test (giả lập)",
                Location = new Point(x + 120, y),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(60, 110, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnTest.Click += (s, e) => SimulateStrip();

            btnStrip = new Button
            {
                Text = "🗑 Xoá bản quyền",
                Location = new Point(x + 280, y),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnStrip.Click += BtnStrip_Click;

            Controls.Add(btnRefresh);
            Controls.Add(btnTest);
            Controls.Add(btnStrip);

            // Các nút neo theo mép trên (giữ vị trí khi cửa sổ co giãn dọc)
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnTest.Anchor    = AnchorStyles.Top | AnchorStyles.Left;
            btnStrip.Anchor   = AnchorStyles.Top | AnchorStyles.Left;

            y += 48;
            txtLog = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(ClientSize.Width - 2 * x, ClientSize.Height - y - 28),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                // Neo 4 cạnh: ô log tự giãn theo cả chiều ngang lẫn dọc
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(txtLog);

            var lblWatermark = new Label
            {
#if TEST_BUILD
                Text = "made by Vinhdd96  ·  TEST",
#else
                Text = "made by Vinhdd96",
#endif
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 150)
            };
            Controls.Add(lblWatermark);
            // Neo watermark ở góc dưới bên phải, tự dịch theo kích thước form
            lblWatermark.Location = new Point(
                ClientSize.Width - lblWatermark.PreferredWidth - 14,
                ClientSize.Height - lblWatermark.PreferredHeight - 8);
            lblWatermark.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            lnkUpdate = new LinkLabel
            {
                Text = "Kiểm tra cập nhật",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(60, 110, 190),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            lnkUpdate.Location = new Point(14, ClientSize.Height - lnkUpdate.PreferredHeight - 8);
            lnkUpdate.LinkClicked += (s, e) => StartUpdateCheck(false);
            Controls.Add(lnkUpdate);

            Load += (s, e) =>
            {
                LoadInfo();
                StartUpdateCheck(true); // tự kiểm tra ngầm khi mở (không làm phiền nếu đã mới nhất)
            };
        }

        private void AddHeader(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 90, 180)
            };
            Controls.Add(lbl);
            y += 28;
        }

        private Label AddInfoRow(string caption, int x, ref int y, int lh)
        {
            var cap = new Label
            {
                Text = caption,
                Location = new Point(x, y),
                Size = new Size(150, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var val = new Label
            {
                Text = "...",
                Location = new Point(x + 155, y),
                Size = new Size(ClientSize.Width - (x + 155) - x, 24),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoEllipsis = true,   // cắt gọn bằng "..." nếu hẹp, không tràn
                // Neo trên + 2 bên: giá trị giãn ngang theo cửa sổ
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(cap);
            Controls.Add(val);
            y += lh;
            return val;
        }

        private void Log(string msg)
        {
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine);
        }

        private void LoadInfo()
        {
            try
            {
                lblUser.Text    = Environment.UserName;
                lblMachine.Text = Environment.MachineName;

                // Phiên bản Windows từ WMI Win32_OperatingSystem
                string edition = "Không xác định";
                using (var s = new ManagementObjectSearcher(
                    "SELECT Caption, Version FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        edition = (o["Caption"] ?? "").ToString().Trim()
                                + "  (build " + (o["Version"] ?? "").ToString() + ")";
                        break;
                    }
                }
                lblEdition.Text = edition;

                // Trạng thái bản quyền từ SoftwareLicensingProduct
                ReadLicense();

                // Trạng thái bản quyền Office (qua ospp.vbs)
                ReadOffice();
            }
            catch (Exception ex)
            {
                Log("Lỗi đọc thông tin: " + ex.Message);
            }
        }

        private void ReadLicense()
        {
            lblStatus.Text  = "Không tìm thấy SKU Windows";
            lblKey.Text     = "—";
            lblChannel.Text = "—";
            lblGenuine.Text = "—";

            try
            {
                using (var s = new ManagementObjectSearcher(
                    "root\\CIMV2",
                    "SELECT Name, Description, ID, LicenseStatus, PartialProductKey, ProductKeyChannel, " +
                    "GracePeriodRemaining, KeyManagementServiceMachine, KeyManagementServiceProductKeyID, " +
                    "DiscoveredKeyManagementServiceMachineName, VLActivationType, LicenseFamily " +
                    "FROM SoftwareLicensingProduct WHERE ApplicationID='" + WindowsAppId +
                    "' AND PartialProductKey <> null"))
                {
                    bool found = false;
                    foreach (ManagementObject o in s.Get())
                    {
                        found = true;
                        int status = Convert.ToInt32(o["LicenseStatus"]);
                        string channel = (o["ProductKeyChannel"] ?? "").ToString();
                        long grace = ToLong(o["GracePeriodRemaining"]);
                        string kmsHost = (o["KeyManagementServiceMachine"] ?? "").ToString();
                        string discoveredKms = (o["DiscoveredKeyManagementServiceMachineName"] ?? "").ToString();
                        if (string.IsNullOrEmpty(kmsHost)) kmsHost = discoveredKms;
                        int vlType = (int)ToLong(o["VLActivationType"]);

                        lblStatus.Text  = DescribeStatus(status);
                        lblStatus.ForeColor = (status == 1)
                            ? Color.FromArgb(0, 140, 0)
                            : Color.FromArgb(190, 120, 0);
                        lblKey.Text     = "****-" + (o["PartialProductKey"] ?? "").ToString();
                        lblChannel.Text = string.IsNullOrEmpty(channel) ? "—" : channel;

                        ClassifyGenuine(status, channel, grace, kmsHost, vlType);
                        DumpLicenseInfo(o, status, channel, grace, kmsHost, discoveredKms, vlType);
                        break;
                    }

                    if (!found)
                    {
                        // Không có key cài đặt -> đã ở trạng thái chưa kích hoạt
                        lblStatus.Text = "Chưa kích hoạt (không có product key)";
                        lblStatus.ForeColor = Color.FromArgb(190, 120, 0);
                        lblKey.Text = "Không có";
                        lblGenuine.Text = "Chưa kích hoạt — không đánh giá được";
                        lblGenuine.ForeColor = Color.FromArgb(190, 120, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Lỗi đọc license: " + ex.Message);
            }
        }

        // In toàn bộ thông tin thô để người dùng tự đánh giá (tránh phụ thuộc kết luận heuristic)
        private void DumpLicenseInfo(ManagementObject o, int status, string channel,
                                     long grace, string kmsHost, string discoveredKms, int vlType)
        {
            Log("──────── THÔNG TIN CHI TIẾT (tự đánh giá) ────────");
            Log("  Tên SKU        : " + (o["Name"] ?? "—"));
            Log("  Mô tả          : " + (o["Description"] ?? "—"));
            Log("  License Family : " + (o["LicenseFamily"] ?? "—"));
            Log("  Activation ID  : " + (o["ID"] ?? "—"));
            Log("  Trạng thái     : " + status + " (" + DescribeStatus(status) + ")");
            Log("  Kênh kích hoạt : " + (string.IsNullOrEmpty(channel) ? "—" : channel));
            Log("  Kiểu VL        : " + vlType + " (" + DescribeVlType(vlType) + ")");
            Log("  KMS đang dùng  : " + (string.IsNullOrEmpty(kmsHost) ? "(không có)" : kmsHost));
            Log("  KMS tự dò      : " + (string.IsNullOrEmpty(discoveredKms) ? "(không có)" : discoveredKms));
            Log("  KMS PKey ID    : " + (o["KeyManagementServiceProductKeyID"] ?? "—"));
            if (grace > 0)
                Log("  Hạn còn lại    : " + grace + " phút (~" + (grace / 1440) + " ngày)");
            else
                Log("  Hạn còn lại    : 0 (kích hoạt vĩnh viễn hoặc chưa kích hoạt)");

            // Gợi ý cách tự đọc
            Log("  • Retail/OEM/Digital + grace=0  => thường là CHÍNH HÃNG");
            Log("  • Volume:GVLK / VL=2 (KMS)      => máy cá nhân thường là CRACK");
            Log("  • KMS trỏ 127.x/localhost/IP nội bộ => KMS emulator (CRACK)");
            Log("  • Đối chiếu thêm: Settings > Activation để xem digital license");
            Log("───────────────────────────────────────────────");
        }

        private string DescribeVlType(int t)
        {
            switch (t)
            {
                case 0: return "Không phải Volume / chưa xác định";
                case 1: return "Active Directory (AD-based)";
                case 2: return "KMS";
                case 3: return "Token-based";
                default: return "?";
            }
        }

        private long ToLong(object v)
        {
            try { return v == null ? 0 : Convert.ToInt64(v); }
            catch { return 0; }
        }

        // Đánh giá nguồn gốc dựa trên kênh, kiểu kích hoạt KMS và máy chủ KMS
        private void ClassifyGenuine(int status, string channel, long grace, string kmsHost, int vlType)
        {
            channel = (channel ?? "").ToLowerInvariant();
            kmsHost = (kmsHost ?? "").Trim();

            if (status != 1)
            {
                lblGenuine.Text = "Chưa kích hoạt — không đánh giá được nguồn gốc";
                lblGenuine.ForeColor = Color.FromArgb(190, 120, 0);
                return;
            }

            bool isKms = vlType == 2 || channel.Contains("gvlk") || !string.IsNullOrEmpty(kmsHost);

            if (isKms)
            {
                bool localKms = kmsHost.StartsWith("127.") || kmsHost == "localhost" ||
                                kmsHost == Environment.MachineName.ToLowerInvariant() ||
                                kmsHost.StartsWith("::1") || kmsHost.StartsWith("0.0.0.0");

                if (localKms)
                {
                    lblGenuine.Text = "❌ NHIỀU KHẢ NĂNG LÀ CRACK (KMS emulator nội bộ" +
                                      (kmsHost.Length > 0 ? ": " + kmsHost : "") + ")";
                    lblGenuine.ForeColor = Color.FromArgb(200, 40, 40);
                }
                else
                {
                    lblGenuine.Text = "⚠ Kích hoạt kiểu KMS (Volume) — máy cá nhân thường là CRACK; " +
                                      "doanh nghiệp có thể chính hãng" +
                                      (kmsHost.Length > 0 ? "  [KMS: " + kmsHost + "]" : "");
                    lblGenuine.ForeColor = Color.FromArgb(190, 120, 0);
                }
                return;
            }

            if (channel.Contains("retail") || channel.Contains("oem") ||
                channel.Contains("mak") || channel.Length == 0)
            {
                lblGenuine.Text = "✔ Nhiều khả năng BẢN QUYỀN chính hãng (Retail/OEM/Digital)";
                lblGenuine.ForeColor = Color.FromArgb(0, 140, 0);
                return;
            }

            lblGenuine.Text = "Đã kích hoạt — chưa xác định rõ nguồn gốc (kênh: " + channel + ")";
            lblGenuine.ForeColor = Color.FromArgb(90, 90, 90);
        }

        private string DescribeStatus(int code)
        {
            switch (code)
            {
                case 0: return "Chưa cấp phép (Unlicensed)";
                case 1: return "Đã kích hoạt (Licensed)";
                case 2: return "Hết hạn dùng thử ban đầu (OOB Grace)";
                case 3: return "Hết hạn gia hạn (OOT Grace)";
                case 4: return "Bản không hợp lệ (Non-Genuine Grace)";
                case 5: return "Chế độ thông báo (Notification)";
                case 6: return "Gia hạn mở rộng (Extended Grace)";
                default: return "Mã trạng thái: " + code;
            }
        }

        // Giả lập toàn bộ thao tác mà KHÔNG thực thi gì cả
        private void SimulateStrip()
        {
            Log("════════ CHẾ ĐỘ TEST — GIẢ LẬP (KHÔNG thực thi) ════════");
            Log("Các bước tool SẼ làm khi bấm \"Xoá bản quyền\":");
            Log("");
            Log("  [1] Bật lại service Software Protection:");
            Log("        sc config sppsvc start= demand");
            Log("        sc start sppsvc");
            Log("  [2] Gỡ product key (đưa về Unlicensed):");
            Log("        cscript //Nologo slmgr.vbs /upk");
            Log("  [3] Xoá product key khỏi Registry:");
            Log("        cscript //Nologo slmgr.vbs /cpky");
            Log("  [3b] Gỡ bản quyền Office (nếu phát hiện ospp.vbs):");
            string osppSim = FindOspp();
            if (osppSim == null)
            {
                Log("        (không tìm thấy ospp.vbs — sẽ bỏ qua Office)");
            }
            else
            {
                var keysSim = ParseOfficeKeys(RunCapture("cscript.exe", "//Nologo \"" + osppSim + "\" /dstatus"));
                if (keysSim.Count == 0)
                    Log("        (Office không có product key dạng gỡ được — bỏ qua)");
                else
                    foreach (var k in keysSim)
                        Log("        cscript //Nologo ospp.vbs /unpkey:" + k);
            }
            Log("  [4] Gỡ khoá registry che watermark:");
            Log("        HKLM\\...\\Software Protection Platform\\NoGenTicket (xoá nếu có)");
            Log("  [5] Khởi động lại explorer để hiện watermark ngay:");
            Log("        taskkill /f /im explorer.exe  ->  explorer.exe");
            Log("");
            Log(">> ĐÃ GIẢ LẬP XONG. KHÔNG có thay đổi nào được thực hiện trên máy.");
            Log("════════════════════════════════════════════════════════");

            MessageBox.Show(
                "Đây là chế độ TEST.\n\nTool chỉ liệt kê các bước sẽ làm trong khung log, " +
                "KHÔNG thực hiện bất kỳ thay đổi nào trên máy của bạn.",
                "Test (giả lập)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void BtnStrip_Click(object sender, EventArgs e)
        {
#if TEST_BUILD
            string confirmMsg =
                "[BẢN TEST] Thao tác này CHỈ GIẢ LẬP, không thay đổi gì trên máy.\n\n" +
                "Bạn có muốn xem các bước tool sẽ làm không?";
#else
            string confirmMsg =
                "Thao tác này sẽ GỠ product key của Windows (và Office nếu có) " +
                "rồi đưa về trạng thái CHƯA KÍCH HOẠT.\n\n" +
                "Hoạt động với cả bản quyền và bản đã crack.\n\n" +
                "Bạn có chắc chắn muốn tiếp tục?";
#endif
            var confirm = MessageBox.Show(
                confirmMsg,
                "Xác nhận huỷ kích hoạt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

#if TEST_BUILD
            // Bản TEST: mọi thao tác đều bị ép thành giả lập, an toàn tuyệt đối
            Log("[BẢN TEST] Nút \"Xoá bản quyền\" cũng chỉ chạy giả lập.");
            SimulateStrip();
            return;
#endif

            btnStrip.Enabled = false;
            btnRefresh.Enabled = false;

            Log("Bắt đầu xoá bản quyền...");

            // Bật lại service Software Protection (bản crack thường tắt để ẩn watermark)
            EnableSppsvc();

            // /upk : gỡ product key đã cài (đưa về Unlicensed)
            RunSlmgr("/upk", "Gỡ product key (/upk)");
            // /cpky: xoá product key khỏi Registry để không bị lộ key
            RunSlmgr("/cpky", "Xoá product key khỏi Registry (/cpky)");

            // Gỡ bản quyền Office (nếu phát hiện ospp.vbs + có product key)
            RemoveOfficeLicense();

            // Gỡ các tinh chỉnh che watermark + khởi động lại explorer để hiện lại ngay
            RestoreActivationWatermark();

            Log("Hoàn tất. Đang làm mới trạng thái...");
            LoadInfo();

            btnStrip.Enabled = true;
            btnRefresh.Enabled = true;

            MessageBox.Show(
                "Đã đưa Windows (và Office nếu có) về trạng thái chưa kích hoạt.\n" +
                "Khuyến nghị khởi động lại máy để áp dụng hoàn toàn.",
                "Xong",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void RunSlmgr(string args, string label)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cscript.exe",
                    Arguments = "//Nologo " +
                                Environment.GetFolderPath(Environment.SpecialFolder.System) +
                                "\\slmgr.vbs " + args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (var p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    string err  = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);

                    Log("> " + label);
                    if (!string.IsNullOrWhiteSpace(outp)) Log("  " + outp.Trim());
                    if (!string.IsNullOrWhiteSpace(err))  Log("  [lỗi] " + err.Trim());
                }
            }
            catch (Exception ex)
            {
                Log("  [ngoại lệ] " + ex.Message);
            }
        }

        // ===================== OFFICE =====================

        // Tìm ospp.vbs ở các vị trí cài Office phổ biến (C2R lẫn MSI, x64 lẫn x86)
        private string FindOspp()
        {
            string[] roots =
            {
                Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files",
                Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? @"C:\Program Files (x86)"
            };
            string[] subs =
            {
                @"\Microsoft Office\root\Office16\ospp.vbs",   // Office 2016/2019/2021/365 (C2R)
                @"\Microsoft Office\Office16\ospp.vbs",
                @"\Microsoft Office\root\Office15\ospp.vbs",   // Office 2013 C2R
                @"\Microsoft Office\Office15\ospp.vbs",
                @"\Microsoft Office\Office14\ospp.vbs"          // Office 2010
            };
            foreach (var r in roots)
                foreach (var s in subs)
                {
                    string p = r + s;
                    if (System.IO.File.Exists(p)) return p;
                }
            return null;
        }

        private string RunCapture(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                using (var p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    string err  = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);
                    return (outp + "\n" + err).Trim();
                }
            }
            catch (Exception ex) { return "[lỗi] " + ex.Message; }
        }

        // Lấy danh sách 5 ký tự cuối của các product key Office đang cài
        private System.Collections.Generic.List<string> ParseOfficeKeys(string dstatus)
        {
            var list = new System.Collections.Generic.List<string>();
            var m = System.Text.RegularExpressions.Regex.Matches(
                dstatus,
                @"Last 5 characters of installed product key:\s*([A-Za-z0-9]{5})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match x in m)
            {
                string k = x.Groups[1].Value.ToUpperInvariant();
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        private void ReadOffice()
        {
            osppPath = FindOspp();
            if (osppPath == null)
            {
                lblOffice.Text = "Không tìm thấy ospp.vbs (chưa cài Office, hoặc bản Store/365 theo tài khoản)";
                lblOffice.ForeColor = Color.FromArgb(120, 120, 120);
                return;
            }

            string outp = RunCapture("cscript.exe", "//Nologo \"" + osppPath + "\" /dstatus");
            var keys = ParseOfficeKeys(outp);

            string statusLine = "";
            var sm = System.Text.RegularExpressions.Regex.Match(outp, @"LICENSE STATUS:\s*(.+)");
            if (sm.Success) statusLine = sm.Groups[1].Value.Trim();

            if (keys.Count == 0)
            {
                lblOffice.Text = "Phát hiện Office nhưng KHÔNG có product key để gỡ (có thể là 365/Store)";
                lblOffice.ForeColor = Color.FromArgb(120, 120, 120);
            }
            else
            {
                lblOffice.Text = "Phát hiện " + keys.Count + " key Office ("
                               + string.Join(", ", keys.ToArray()) + ")"
                               + (statusLine.Length > 0 ? " — " + statusLine : "");
                lblOffice.ForeColor = statusLine.ToUpperInvariant().Contains("LICENSED")
                    ? Color.FromArgb(0, 140, 0) : Color.FromArgb(190, 120, 0);
            }

            // Dump chi tiết để người dùng tự đối chiếu
            Log("──────── OFFICE (ospp.vbs) ────────");
            Log("  ospp.vbs: " + osppPath);
            if (!string.IsNullOrWhiteSpace(outp))
                foreach (var line in outp.Split('\n'))
                    if (line.Trim().Length > 0) Log("  " + line.TrimEnd());
            Log("───────────────────────────────────");
        }

        // Gỡ toàn bộ product key Office
        private void RemoveOfficeLicense()
        {
            string ospp = FindOspp();
            if (ospp == null)
            {
                Log("> Office: không tìm thấy ospp.vbs — bỏ qua.");
                return;
            }
            string outp = RunCapture("cscript.exe", "//Nologo \"" + ospp + "\" /dstatus");
            var keys = ParseOfficeKeys(outp);
            if (keys.Count == 0)
            {
                Log("> Office: không có product key dạng gỡ được (có thể là 365/Store).");
                return;
            }
            foreach (var k in keys)
            {
                Log("> Gỡ key Office /unpkey:" + k);
                string r = RunCapture("cscript.exe", "//Nologo \"" + ospp + "\" /unpkey:" + k);
                if (!string.IsNullOrWhiteSpace(r)) Log("  " + r.Replace("\n", " ").Trim());
            }
        }

        // =================================================

        private void RunHidden(string file, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi))
            {
                p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit(20000);
            }
        }

        private void EnableSppsvc()
        {
            try
            {
                Log("> Bật lại service Software Protection (sppsvc)");
                // Đặt kiểu khởi động = Demand (3), bỏ trạng thái Disabled (4) mà crack hay đặt
                RunHidden("sc.exe", "config sppsvc start= demand");
                RunHidden("sc.exe", "start sppsvc");
            }
            catch (Exception ex)
            {
                Log("  [lỗi] " + ex.Message);
            }
        }

        private void RestoreActivationWatermark()
        {
            try
            {
                Log("> Khôi phục hiển thị watermark kích hoạt");

                // Một số crack đặt khoá che watermark trong policy SoftwareProtectionPlatform
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform", true))
                {
                    if (k != null)
                    {
                        if (k.GetValue("NoGenTicket") != null) k.DeleteValue("NoGenTicket", false);
                        if (k.GetValue("KeyManagementServiceName") != null) k.DeleteValue("KeyManagementServiceName", false);
                    }
                }

                // Khởi động lại explorer để desktop vẽ lại và hiện watermark ngay (không cần reboot)
                Log("  Khởi động lại explorer...");
                RunHidden("taskkill.exe", "/f /im explorer.exe");
                System.Threading.Thread.Sleep(800);
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe");
            }
            catch (Exception ex)
            {
                Log("  [lỗi] " + ex.Message);
            }
        }

        // ===================== CẬP NHẬT OTA =====================

        private void StartUpdateCheck(bool silent)
        {
            if (!silent) Log("> Đang kiểm tra cập nhật...");
            lnkUpdate.Enabled = false;
            var t = new Thread(() => UpdateWorker(silent));
            t.IsBackground = true;
            t.Start();
        }

        private void UpdateWorker(bool silent)
        {
            string latestTag = null, downloadUrl = null, err = null;
            try
            {
                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)3072 | (SecurityProtocolType)768; // TLS 1.2 + 1.1

                string api = "https://api.github.com/repos/" + UpdateOwner + "/" +
                             UpdateRepo + "/releases/latest";
                string json = HttpGet(api);

                var mTag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (mTag.Success) latestTag = mTag.Groups[1].Value;

                // Ưu tiên asset .exe; ưu tiên đúng tên file đang chạy nếu khớp
                string self = Path.GetFileName(Application.ExecutablePath);
                foreach (Match u in Regex.Matches(json,
                    "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe)\""))
                {
                    string url = u.Groups[1].Value;
                    if (url.EndsWith("/" + self, StringComparison.OrdinalIgnoreCase))
                    { downloadUrl = url; break; }
                    if (downloadUrl == null) downloadUrl = url;
                }
            }
            catch (Exception ex) { err = ex.Message; }

            BeginInvoke((Action)(() =>
            {
                lnkUpdate.Enabled = true;
                if (err != null)
                {
                    if (!silent) Log("  [lỗi cập nhật] " + err);
                    return;
                }
                if (string.IsNullOrEmpty(latestTag))
                {
                    if (!silent) Log("  Không đọc được thông tin phát hành.");
                    return;
                }

                if (IsNewer(latestTag, AppVersion) && downloadUrl != null)
                {
                    Log("  Có bản mới: " + latestTag + " (hiện tại v" + AppVersion + ")");
                    var r = MessageBox.Show(
                        "Đã có phiên bản mới: " + latestTag + "\n" +
                        "Phiên bản hiện tại: v" + AppVersion + "\n\n" +
                        "Tải về và cập nhật ngay?",
                        "Có bản cập nhật",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (r == DialogResult.Yes) DownloadAndApply(downloadUrl);
                }
                else
                {
                    Log("  Đang dùng bản mới nhất (v" + AppVersion + ").");
                    if (!silent)
                        MessageBox.Show("Bạn đang dùng phiên bản mới nhất (v" + AppVersion + ").",
                            "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));
        }

        private string HttpGet(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "WindowsStriper-Updater";      // GitHub API bắt buộc User-Agent
            req.Accept = "application/vnd.github+json";
            req.Timeout = 15000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream()))
                return sr.ReadToEnd();
        }

        // So sánh phiên bản dạng "v1.2.3" / "1.2.3"
        private bool IsNewer(string remote, string local)
        {
            try
            {
                Version vr = new Version(CleanVer(remote));
                Version vl = new Version(CleanVer(local));
                return vr > vl;
            }
            catch { return false; }
        }

        private string CleanVer(string v)
        {
            v = (v ?? "").Trim().TrimStart('v', 'V');
            var m = Regex.Match(v, @"\d+(\.\d+){0,3}");
            return m.Success ? m.Value : "0.0";
        }

        private void DownloadAndApply(string url)
        {
            try
            {
                Log("  Đang tải bản cập nhật...");
                string curExe = Application.ExecutablePath;
                string newExe = curExe + ".new";

                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)3072 | (SecurityProtocolType)768;
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "WindowsStriper-Updater");
                    wc.DownloadFile(url, newExe);
                }

                // Tạo .bat: chờ tool thoát -> ghi đè exe -> khởi động lại
                string bat = Path.Combine(Path.GetTempPath(), "ws_update.bat");
                string content =
                    "@echo off\r\n" +
                    "set CUR=\"" + curExe + "\"\r\n" +
                    "set NEW=\"" + newExe + "\"\r\n" +
                    ":wait\r\n" +
                    "ping -n 2 127.0.0.1 >nul\r\n" +
                    "del %CUR% >nul 2>&1\r\n" +
                    "if exist %CUR% goto wait\r\n" +
                    "move /y %NEW% %CUR% >nul\r\n" +
                    "start \"\" %CUR%\r\n" +
                    "del \"%~f0\"\r\n";
                File.WriteAllText(bat, content);

                Process.Start(new ProcessStartInfo
                {
                    FileName = bat,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                Log("  Đã tải xong. Đang đóng để cập nhật...");
                Application.Exit();
            }
            catch (Exception ex)
            {
                Log("  [lỗi tải] " + ex.Message);
                MessageBox.Show("Không tải được bản cập nhật:\n" + ex.Message,
                    "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========================================================

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
