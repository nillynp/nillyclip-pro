// ============================================================================
// NillyClip Pro — Ultra-Light Native Clipboard History, Pinned Snippets
//                 & DPAPI-Encrypted API Key Vault (Replaces Windows Win+V)
// created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// Features:
//  - 0% Idle CPU via Win32 AddClipboardFormatListener (replaces 178 MB TextInputHost!)
//  - Instant Global Popup (Ctrl+Shift+V / Ctrl+Alt+V) with Auto-Paste & Plain-Text Paste
//  - Live HEX/RGB Color Swatches, Image/Screenshot Thumbnails & URL Detection
//  - 1-Click Pin to Snippets & 1-Click Encrypt to API Keys Vault
//  - Windows DPAPI (CurrentUser AES) Encrypted Vault — ZERO plaintext secrets on disk!
//  - Per-Row Vector Eye (ματάκι) Reveal/Hide (auto re-masks on hide) + Safe Copy + 30s Auto-Clear
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("NillyClip Pro")]
[assembly: AssemblyDescription("Ultra-Light Clipboard History, Pinned Snippets & DPAPI-Encrypted API Key Vault — created by Nilly graphics (Nillygraphics · Sotiris Christos)")]
[assembly: AssemblyCompany("Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyClip Pro")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyTrademark("Nilly graphics / Nillygraphics — Sotiris Christos")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace NillyClipPro
{
    internal static class Native
    {
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint VK_V = 0x56;
        public const byte VK_CONTROL_B = 0x11;
        public const byte VK_V_B = 0x56;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", SetLastError = true)] public static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll", SetLastError = true)] public static extern bool OpenClipboard(IntPtr window);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool CloseClipboard();
        [DllImport("user32.dll", SetLastError = true)] public static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)] public static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegisterClipboardFormat(string name);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);

        public static void SetDarkTitlebar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int v = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref v, 4);
            }
            catch { }
        }

        public static void SendCtrlV()
        {
            try
            {
                keybd_event(VK_CONTROL_B, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V_B, 0, 0, UIntPtr.Zero);
                Thread.Sleep(18);
                keybd_event(VK_V_B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CONTROL_B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }
    }

    internal static class IconHelper
    {
        public static Bitmap LoadCrispIcon(string icoPath, int sz)
        {
            if (string.IsNullOrEmpty(icoPath) || !File.Exists(icoPath)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                if (data.Length < 22) return null;
                int count = BitConverter.ToUInt16(data, 4);
                int bestOff = -1, bestLen = 0, bestDim = -1;
                for (int i = 0; i < count; i++)
                {
                    int e = 6 + i * 16;
                    if (e + 16 > data.Length) break;
                    int w = data[e] == 0 ? 256 : data[e];
                    int len = BitConverter.ToInt32(data, e + 8);
                    int off = BitConverter.ToInt32(data, e + 12);
                    if (off < 0 || len <= 8 || off + len > data.Length) continue;
                    bool isPng = (data[off] == 0x89 && data[off + 1] == 0x50 && data[off + 2] == 0x4E && data[off + 3] == 0x47);
                    if (isPng && w > bestDim) { bestDim = w; bestOff = off; bestLen = len; }
                }
                if (bestOff >= 0)
                {
                    using (MemoryStream ms = new MemoryStream(data, bestOff, bestLen, false))
                    using (Image src = Image.FromStream(ms))
                    {
                        Bitmap dst = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(dst))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.DrawImage(src, 0, 0, sz, sz);
                        }
                        return dst;
                    }
                }
            }
            catch { }
            return null;
        }

        public static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
        {
            GraphicsPath p = new GraphicsPath();
            float d = r * 2f;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal enum ClipKind { Text, Image, ColorHex, Url }

    internal sealed class ClipEntry
    {
        public string Id;
        public ClipKind Kind;
        public string Title;      // For Pinned Snippets & Vault API Keys
        public string Category;   // e.g. "API Key", "Στοιχεία", "Link", "Χρώμα"
        public string TextValue;  // Text or secret value
        public string Note;       // Optional note
        public string ImageFile;  // Relative PNG filename in img\ for Image kind
        public int ImgW, ImgH;
        public Color ParsedColor;
        public DateTime CreatedUtc;
        public bool Revealed;     // UI-only transient state for Vault Eye (auto re-masked on hide!)

        public ClipEntry()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 10);
            CreatedUtc = DateTime.UtcNow;
        }

        public string MaskedValue()
        {
            string s = TextValue ?? "";
            if (s.Length <= 6) return "••••••••••••";
            if (s.Length <= 12) return s.Substring(0, 2) + "••••••••" + s.Substring(s.Length - 2);
            return s.Substring(0, 4) + "••••••••••••" + s.Substring(s.Length - 4);
        }
    }

    internal static class ClipDetector
    {
        private static readonly Regex HexRx = new Regex(@"^#?([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$");
        private static readonly Regex RgbRx = new Regex(@"^rgb\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$", RegexOptions.IgnoreCase);

        public static bool TryDetectColor(string s, out Color col)
        {
            col = Color.Empty;
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.Trim();
            if (t.Length > 26) return false;
            Match mh = HexRx.Match(t);
            if (mh.Success && t.StartsWith("#"))
            {
                string h = mh.Groups[1].Value;
                if (h.Length == 3) h = "" + h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
                try
                {
                    col = Color.FromArgb(
                        Convert.ToInt32(h.Substring(0, 2), 16),
                        Convert.ToInt32(h.Substring(2, 2), 16),
                        Convert.ToInt32(h.Substring(4, 2), 16));
                    return true;
                }
                catch { return false; }
            }
            Match mr = RgbRx.Match(t);
            if (mr.Success)
            {
                int r = int.Parse(mr.Groups[1].Value);
                int g = int.Parse(mr.Groups[2].Value);
                int b = int.Parse(mr.Groups[3].Value);
                if (r <= 255 && g <= 255 && b <= 255) { col = Color.FromArgb(r, g, b); return true; }
            }
            return false;
        }

        public static bool IsUrl(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.Trim();
            return t.Length < 400 && !t.Contains(" ") && !t.Contains("\n") &&
                   (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }
    }

    // ========================================================================
    // STORAGE ENGINE: PLAIN HISTORY/PINNED + WINDOWS DPAPI ENCRYPTED VAULT
    // ========================================================================
    internal sealed class ClipStore
    {
        private static readonly byte[] VaultEntropy = Encoding.UTF8.GetBytes("NillyClipPro::DPAPI::Vault::v1::NillyGraphics");

        public readonly string DataDir;
        public readonly string ImgDir;
        public readonly string HistoryPath;
        public readonly string PinnedPath;
        public readonly string VaultPath;

        public readonly List<ClipEntry> History = new List<ClipEntry>();
        public readonly List<ClipEntry> Pinned = new List<ClipEntry>();
        public readonly List<ClipEntry> Vault = new List<ClipEntry>();

        public ClipStore(string customDir)
        {
            DataDir = string.IsNullOrEmpty(customDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NillyClipPro")
                : customDir;
            ImgDir = Path.Combine(DataDir, "img");
            HistoryPath = Path.Combine(DataDir, "history.dat");
            PinnedPath = Path.Combine(DataDir, "pinned.dat");
            VaultPath = Path.Combine(DataDir, "vault.dpapi");
            try { Directory.CreateDirectory(DataDir); Directory.CreateDirectory(ImgDir); } catch { }
        }

        public void LoadAll()
        {
            History.Clear(); Pinned.Clear(); Vault.Clear();
            LoadListPlain(HistoryPath, History);
            LoadListPlain(PinnedPath, Pinned);
            LoadVaultEncrypted();
        }

        public void SaveHistory() { SaveListPlain(HistoryPath, History); PruneUnusedImages(); }
        public void SavePinned() { SaveListPlain(PinnedPath, Pinned); }

        public bool SaveVaultEncrypted()
        {
            byte[] plainBytes = null;
            string staged = VaultPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                plainBytes = Encoding.UTF8.GetBytes(SerializeEntries(Vault));
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, VaultEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(staged, cipherBytes);
                if (File.Exists(VaultPath)) File.Replace(staged, VaultPath, null);
                else File.Move(staged, VaultPath);
                return true;
            }
            catch { return false; }
            finally
            {
                if (plainBytes != null) Array.Clear(plainBytes, 0, plainBytes.Length);
                try { if (File.Exists(staged)) File.Delete(staged); } catch { }
            }
        }

        private void LoadVaultEncrypted()
        {
            try
            {
                if (!File.Exists(VaultPath)) return;
                byte[] cipherBytes = File.ReadAllBytes(VaultPath);
                if (cipherBytes == null || cipherBytes.Length == 0) return;
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, VaultEntropy, DataProtectionScope.CurrentUser);
                string text = Encoding.UTF8.GetString(plainBytes);
                Array.Clear(plainBytes, 0, plainBytes.Length);
                DeserializeEntries(text, Vault);
            }
            catch { }
        }

        private static void SaveListPlain(string path, List<ClipEntry> list)
        {
            try { File.WriteAllText(path, SerializeEntries(list), new UTF8Encoding(false)); } catch { }
        }

        private static void LoadListPlain(string path, List<ClipEntry> target)
        {
            try
            {
                if (!File.Exists(path)) return;
                DeserializeEntries(File.ReadAllText(path, Encoding.UTF8), target);
            }
            catch { }
        }

        // Line-based base64-safe format (immune to newlines, tabs, pipes, emojis, Greek, JSON chars!)
        private static string SerializeEntries(List<ClipEntry> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                ClipEntry e = list[i];
                sb.Append(e.Id).Append('|')
                  .Append((int)e.Kind).Append('|')
                  .Append(B64(e.Title)).Append('|')
                  .Append(B64(e.Category)).Append('|')
                  .Append(B64(e.TextValue)).Append('|')
                  .Append(B64(e.Note)).Append('|')
                  .Append(B64(e.ImageFile)).Append('|')
                  .Append(e.ImgW).Append('|').Append(e.ImgH).Append('|')
                  .Append(e.CreatedUtc.Ticks).Append("\r\n");
            }
            return sb.ToString();
        }

        private static void DeserializeEntries(string raw, List<ClipEntry> target)
        {
            if (string.IsNullOrEmpty(raw)) return;
            string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] p = lines[i].Split('|');
                if (p.Length < 10) continue;
                ClipEntry e = new ClipEntry();
                e.Id = p[0];
                int k; if (int.TryParse(p[1], out k)) e.Kind = (ClipKind)k;
                e.Title = FromB64(p[2]);
                e.Category = FromB64(p[3]);
                e.TextValue = FromB64(p[4]);
                e.Note = FromB64(p[5]);
                e.ImageFile = FromB64(p[6]);
                int w, h; int.TryParse(p[7], out w); int.TryParse(p[8], out h);
                e.ImgW = w; e.ImgH = h;
                long ticks; if (long.TryParse(p[9], out ticks) && ticks > 0) e.CreatedUtc = new DateTime(ticks, DateTimeKind.Utc);
                Color col;
                if (ClipDetector.TryDetectColor(e.TextValue, out col)) { e.ParsedColor = col; if (e.Kind == ClipKind.Text) e.Kind = ClipKind.ColorHex; }
                target.Add(e);
            }
        }

        private static string B64(string s) { return string.IsNullOrEmpty(s) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(s)); }
        private static string FromB64(string b) { if (string.IsNullOrEmpty(b)) return ""; try { return Encoding.UTF8.GetString(Convert.FromBase64String(b)); } catch { return ""; } }

        private void PruneUnusedImages()
        {
            try
            {
                HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < History.Count; i++) if (!string.IsNullOrEmpty(History[i].ImageFile)) used.Add(History[i].ImageFile);
                for (int i = 0; i < Pinned.Count; i++) if (!string.IsNullOrEmpty(Pinned[i].ImageFile)) used.Add(Pinned[i].ImageFile);
                foreach (string f in Directory.GetFiles(ImgDir, "*.png"))
                    if (!used.Contains(Path.GetFileName(f))) { try { File.Delete(f); } catch { } }
            }
            catch { }
        }
    }

    // ========================================================================
    // ADD / EDIT MODAL DIALOG FOR PINNED SNIPPETS & ENCRYPTED API KEYS (WITH EYE!)
    // ========================================================================
    internal sealed class EntryEditorDialog : Form
    {
        public string EntryTitle { get { return _txtTitle.Text.Trim(); } }
        public string EntryCategory { get { return _cmbCat.Text.Trim(); } }
        public string EntryValue { get { return _txtVal.Text; } }
        public string EntryNote { get { return _txtNote.Text.Trim(); } }

        private TextBox _txtTitle, _txtVal, _txtNote;
        private ComboBox _cmbCat;
        private Button _btnEye;
        private bool _masked;

        public EntryEditorDialog(bool isVault, ClipEntry existing, bool dark)
        {
            _masked = isVault;
            Text = isVault
                ? (existing == null ? "Νέο Κρυπτογραφημένο API Key / Secret (DPAPI)" : "Επεξεργασία Κλειδιού Κλειδοθήκης")
                : (existing == null ? "Νέο Καρφιτσωμένο Snippet" : "Επεξεργασία Καρφιτσωμένου Snippet");
            ClientSize = new Size(440, isVault ? 310 : 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;

            Color bg = dark ? Color.FromArgb(24, 25, 30) : Color.FromArgb(245, 247, 250);
            Color inp = dark ? Color.FromArgb(34, 36, 43) : Color.White;
            Color fg = dark ? Color.FromArgb(240, 242, 248) : Color.FromArgb(22, 24, 30);
            Color muted = dark ? Color.FromArgb(148, 156, 170) : Color.FromArgb(95, 105, 118);
            Color orange = Color.FromArgb(252, 150, 31);
            BackColor = bg; ForeColor = fg;
            Native.SetDarkTitlebar(Handle, dark);

            Label l1 = new Label { Left = 16, Top = 14, Width = 220, Text = isVault ? "Όνομα Υπηρεσίας / Κλειδιού:" : "Τίτλος Snippet:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _txtTitle = new TextBox { Left = 16, Top = 34, Width = 240, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Text = existing != null ? existing.Title : "" };

            Label l2 = new Label { Left = 266, Top = 14, Width = 158, Text = "Κατηγορία:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _cmbCat = new ComboBox { Left = 266, Top = 33, Width = 158, BackColor = inp, ForeColor = fg, Font = new Font("Segoe UI", 9.2f), DropDownStyle = ComboBoxStyle.DropDown };
            if (isVault) _cmbCat.Items.AddRange(new object[] { "API Key", "Token", "Password", "FTP / Hosting", "License", "Database" });
            else _cmbCat.Items.AddRange(new object[] { "Στοιχεία", "IBAN / ΑΦΜ", "Email", "Link", "Χρώμα", "Prompt", "Κώδικας" });
            _cmbCat.Text = (existing != null && !string.IsNullOrEmpty(existing.Category)) ? existing.Category : _cmbCat.Items[0].ToString();

            Label l3 = new Label { Left = 16, Top = 68, Width = 320, Text = isVault ? "Κλειδί / Secret (αποθηκεύεται κρυπτογραφημένο με Windows DPAPI):" : "Κείμενο / Τιμή:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            if (isVault)
            {
                _txtVal = new TextBox { Left = 16, Top = 88, Width = 368, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10f), UseSystemPasswordChar = true, Text = existing != null ? existing.TextValue : "" };
                _btnEye = new Button { Left = 388, Top = 87, Width = 36, Height = 25, FlatStyle = FlatStyle.Flat, BackColor = inp, ForeColor = orange, Text = "◉", Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand, TabStop = false };
                _btnEye.FlatAppearance.BorderColor = Color.FromArgb(60, 64, 76);
                _btnEye.Click += delegate
                {
                    _masked = !_masked;
                    _txtVal.UseSystemPasswordChar = _masked;
                    _btnEye.ForeColor = _masked ? orange : Color.FromArgb(166, 226, 46);
                };
                Controls.Add(_btnEye);
            }
            else
            {
                _txtVal = new TextBox { Left = 16, Top = 88, Width = 408, Height = 108, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Text = existing != null ? existing.TextValue : "" };
            }

            int noteY = isVault ? 128 : 206;
            Label l4 = new Label { Left = 16, Top = noteY, Width = 380, Text = "Σημείωση (προαιρετική — π.χ. λογαριασμός / χρήση):", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _txtNote = new TextBox { Left = 16, Top = noteY + 20, Width = 408, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f), Text = existing != null ? existing.Note : "" };

            int btnY = ClientSize.Height - 44;
            Button btnOk = new Button { Left = 266, Top = btnY, Width = 158, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = orange, ForeColor = Color.FromArgb(14, 18, 24), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Text = isVault ? "Αποθήκευση (DPAPI)" : "Αποθήκευση", Cursor = Cursors.Hand };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += delegate
            {
                if (string.IsNullOrEmpty(_txtVal.Text))
                {
                    MessageBox.Show(this, "Συμπλήρωσε την τιμή / κλειδί.", "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(_txtTitle.Text.Trim()))
                    _txtTitle.Text = isVault ? "API Key" : "Snippet";
                DialogResult = DialogResult.OK;
                Close();
            };

            Button btnCancel = new Button { Left = 172, Top = btnY, Width = 86, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = inp, ForeColor = fg, Font = new Font("Segoe UI", 9f), Text = "Άκυρο", DialogResult = DialogResult.Cancel, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 64, 76);

            Controls.AddRange(new Control[] { l1, _txtTitle, l2, _cmbCat, l3, _txtVal, l4, _txtNote, btnCancel, btnOk });
            CancelButton = btnCancel;
        }
    }

    // ========================================================================
    // CRISP OWNER-DRAWN MINIMAL CLIP CARD WITH VECTOR EYE (ΜΑΤΑΚΙ) & ACTIONS
    // ========================================================================
    internal sealed class ClipRowCard : Control
    {
        public readonly ClipEntry Entry;
        public readonly int TabKind; // 0=History, 1=Pinned, 2=Vault
        public int ShortcutIndex;    // 1..9 or 0
        public bool Selected;

        public event Action<ClipEntry, bool> PasteRequested;   // (entry, plainOnly)
        public event Action<ClipEntry> PinRequested;
        public event Action<ClipEntry> MoveToVaultRequested;
        public event Action<ClipEntry> EditRequested;
        public event Action<ClipEntry> DeleteRequested;

        private bool _dark = true;
        private bool _hovered = false;
        private int _hoverBtn = -1; // 0, 1, 2 (right-to-left icons)
        private readonly Rectangle[] _actRects = new Rectangle[3];
        private readonly string _imgDir;
        private Bitmap _thumb;

        private static readonly Font FontMain = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        private static readonly Font FontBody = new Font("Segoe UI", 8.6f);
        private static readonly Font FontMono = new Font("Consolas", 9f);
        private static readonly Font FontMini = new Font("Segoe UI", 7.4f, FontStyle.Bold);

        public ClipRowCard(ClipEntry entry, int tabKind, int width, string imgDir)
        {
            Entry = entry;
            TabKind = tabKind;
            _imgDir = imgDir;
            Width = width;
            Height = 66;
            Margin = new Padding(0, 0, 0, 6);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            LayoutButtons();
            LoadThumbIfImage();
        }

        private void LayoutButtons()
        {
            int bsz = 24, gap = 4, by = (Height - bsz) / 2;
            // 3 compact right-side vector icon buttons
            _actRects[2] = new Rectangle(Width - 10 - bsz, by, bsz, bsz);             // Rightmost: Delete
            _actRects[1] = new Rectangle(Width - 10 - 2 * bsz - gap, by, bsz, bsz);   // Middle: Vault/Edit/Copy
            _actRects[0] = new Rectangle(Width - 10 - 3 * bsz - 2 * gap, by, bsz, bsz); // Leftmost: Pin / Eye (ματάκι!)
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            LayoutButtons();
            base.OnSizeChanged(e);
        }

        private void LoadThumbIfImage()
        {
            if (Entry.Kind != ClipKind.Image || string.IsNullOrEmpty(Entry.ImageFile)) return;
            try
            {
                string full = Path.Combine(_imgDir, Entry.ImageFile);
                if (!File.Exists(full)) return;
                using (FileStream fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image src = Image.FromStream(fs))
                {
                    Bitmap tb = new Bitmap(44, 40, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(tb))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, 0, 0, 44, 40);
                    }
                    _thumb = tb;
                }
            }
            catch { }
        }

        public void SetTheme(bool dark) { _dark = dark; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color outerBg = _dark ? Color.FromArgb(20, 21, 25) : Color.FromArgb(242, 244, 248);
            Color cardBg = _dark
                ? (Selected || _hovered ? Color.FromArgb(37, 40, 48) : Color.FromArgb(29, 31, 37))
                : (Selected || _hovered ? Color.White : Color.FromArgb(250, 251, 253));
            Color border = (Selected || _hovered)
                ? Color.FromArgb(252, 150, 31)
                : (_dark ? Color.FromArgb(46, 50, 60) : Color.FromArgb(218, 224, 232));
            Color fgMain = _dark ? Color.FromArgb(242, 244, 248) : Color.FromArgb(22, 24, 30);
            Color fgMuted = _dark ? Color.FromArgb(148, 156, 170) : Color.FromArgb(95, 105, 120);
            Color orange = Color.FromArgb(252, 150, 31);
            Color cyan = Color.FromArgb(102, 217, 239);
            Color pink = Color.FromArgb(249, 38, 114);
            Color green = Color.FromArgb(166, 226, 46);

            g.Clear(outerBg);
            using (GraphicsPath gp = IconHelper.RoundRect(0.5f, 0.5f, Width - 2f, Height - 2f, 9f))
            using (SolidBrush br = new SolidBrush(cardBg))
            using (Pen bp = new Pen(border, (Selected || _hovered) ? 1.5f : 1f))
            {
                g.FillPath(br, gp);
                g.DrawPath(bp, gp);
            }

            int textX = 12;
            int rightLimit = _actRects[0].Left - 8;

            // Left Visual Badge (Color Swatch / Image Thumbnail / Vault Key Badge / Shortcut Num)
            if (Entry.Kind == ClipKind.ColorHex)
            {
                using (GraphicsPath sw = IconHelper.RoundRect(10, 17, 32, 32, 7))
                using (SolidBrush sb = new SolidBrush(Entry.ParsedColor))
                using (Pen sp = new Pen(Color.FromArgb(120, 255, 255, 255), 1.2f))
                { g.FillPath(sb, sw); g.DrawPath(sp, sw); }
                textX = 50;
            }
            else if (Entry.Kind == ClipKind.Image && _thumb != null)
            {
                g.DrawImage(_thumb, 10, 13, 44, 40);
                using (Pen ip = new Pen(border, 1f)) g.DrawRectangle(ip, 10, 13, 44, 40);
                textX = 62;
            }
            else if (TabKind == 2) // Vault Key badge
            {
                using (GraphicsPath kb = IconHelper.RoundRect(10, 18, 30, 30, 7))
                using (SolidBrush kbr = new SolidBrush(_dark ? Color.FromArgb(52, 26, 38) : Color.FromArgb(254, 226, 236)))
                { g.FillPath(kbr, kb); }
                using (Pen kp = new Pen(pink, 1.8f))
                {
                    g.DrawEllipse(kp, 16, 25, 9, 9);
                    g.DrawLine(kp, 25, 30, 34, 30);
                    g.DrawLine(kp, 31, 30, 31, 34);
                    g.DrawLine(kp, 34, 30, 34, 33);
                }
                textX = 48;
            }
            else if (TabKind == 1) // Pinned Snippet badge
            {
                using (GraphicsPath pb = IconHelper.RoundRect(10, 18, 30, 30, 7))
                using (SolidBrush pbr = new SolidBrush(_dark ? Color.FromArgb(54, 40, 22) : Color.FromArgb(255, 241, 224)))
                { g.FillPath(pbr, pb); }
                using (SolidBrush dot = new SolidBrush(orange)) g.FillEllipse(dot, 21, 29, 8, 8);
                textX = 48;
            }

            int textW = Math.Max(60, rightLimit - textX);

            if (TabKind == 2) // Encrypted API Key Row
            {
                string title = string.IsNullOrEmpty(Entry.Title) ? "API Key" : Entry.Title;
                string cat = string.IsNullOrEmpty(Entry.Category) ? "DPAPI" : Entry.Category;
                TextRenderer.DrawText(g, title, FontMain, new Rectangle(textX, 9, textW - 62, 19), fgMain, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, cat, FontMini, new Rectangle(rightLimit - 60, 11, 58, 16), pink, TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string secretDisplay = Entry.Revealed ? (Entry.TextValue ?? "") : Entry.MaskedValue();
                TextRenderer.DrawText(g, secretDisplay, FontMono, new Rectangle(textX, 30, textW, 18), Entry.Revealed ? green : orange, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (!string.IsNullOrEmpty(Entry.Note))
                    TextRenderer.DrawText(g, Entry.Note, FontMini, new Rectangle(textX, 48, textW, 14), fgMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else if (TabKind == 1) // Pinned Snippet Row
            {
                string title = string.IsNullOrEmpty(Entry.Title) ? "Snippet" : Entry.Title;
                string cat = string.IsNullOrEmpty(Entry.Category) ? "PIN" : Entry.Category;
                TextRenderer.DrawText(g, title, FontMain, new Rectangle(textX, 9, textW - 62, 19), fgMain, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, cat, FontMini, new Rectangle(rightLimit - 60, 11, 58, 16), orange, TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string body = (Entry.TextValue ?? "").Replace("\r\n", " ").Replace("\n", " ").Trim();
                TextRenderer.DrawText(g, body, FontBody, new Rectangle(textX, 31, textW, 24), fgMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else // Clipboard History Row
            {
                string preview = Entry.Kind == ClipKind.Image
                    ? ("Εικόνα / Screenshot (" + Entry.ImgW + " × " + Entry.ImgH + " px)")
                    : (Entry.TextValue ?? "").Replace("\r\n", "  ").Replace("\n", "  ").Trim();
                if (preview.Length > 180) preview = preview.Substring(0, 180) + "…";

                Color titleCol = Entry.Kind == ClipKind.Url ? cyan : fgMain;
                TextRenderer.DrawText(g, preview, FontBody, new Rectangle(textX, 9, textW, 34), titleCol,
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string tag = Entry.Kind == ClipKind.ColorHex ? "HEX" : (Entry.Kind == ClipKind.Image ? "IMG" : (Entry.Kind == ClipKind.Url ? "URL" : "TXT"));
                string num = (ShortcutIndex >= 1 && ShortcutIndex <= 9) ? ("Ctrl+" + ShortcutIndex + "  ·  ") : "";
                string meta = num + tag + "  ·  " + Entry.CreatedUtc.ToLocalTime().ToString("HH:mm");
                TextRenderer.DrawText(g, meta, FontMini, new Rectangle(textX, 46, textW, 15), fgMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // Draw the 3 Crisp Vector Mini-Icons on the Right!
            if (TabKind == 0)
            {
                DrawVecBtn(g, 0, "pin", fgMuted, orange);
                DrawVecBtn(g, 1, "key", fgMuted, pink);
                DrawVecBtn(g, 2, "del", fgMuted, pink);
            }
            else if (TabKind == 1)
            {
                DrawVecBtn(g, 0, "copy", orange, orange);
                DrawVecBtn(g, 1, "edit", fgMuted, cyan);
                DrawVecBtn(g, 2, "del", fgMuted, pink);
            }
            else // Vault: Eye (ματάκι!) · Edit · Delete (clicking card = Safe Copy!)
            {
                DrawVecBtn(g, 0, Entry.Revealed ? "eye_open" : "eye_closed", Entry.Revealed ? green : orange, orange);
                DrawVecBtn(g, 1, "edit", fgMuted, cyan);
                DrawVecBtn(g, 2, "del", fgMuted, pink);
            }
        }

        private void DrawVecBtn(Graphics g, int idx, string kind, Color baseCol, Color hoverCol)
        {
            Rectangle r = _actRects[idx];
            bool hov = (_hoverBtn == idx);
            if (hov)
            {
                using (GraphicsPath gp = IconHelper.RoundRect(r.X, r.Y, r.Width, r.Height, 6f))
                using (SolidBrush hb = new SolidBrush(_dark ? Color.FromArgb(56, 60, 72) : Color.FromArgb(226, 232, 240)))
                    g.FillPath(hb, gp);
            }
            Color col = hov ? hoverCol : baseCol;
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;

            using (Pen p = new Pen(col, 1.6f))
            using (SolidBrush br = new SolidBrush(col))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                if (kind == "eye_open" || kind == "eye_closed")
                {
                    // Crisp Vector Eye (Ματάκι!)
                    g.DrawArc(p, cx - 7f, cy - 4.5f, 14f, 9f, 200, 140);
                    g.DrawArc(p, cx - 7f, cy - 4.5f, 14f, 9f, 20, 140);
                    g.FillEllipse(br, cx - 2.3f, cy - 2.3f, 4.6f, 4.6f);
                    if (kind == "eye_closed")
                        g.DrawLine(p, cx - 6f, cy + 5.5f, cx + 6f, cy - 5.5f);
                }
                else if (kind == "pin")
                {
                    g.DrawEllipse(p, cx - 3.5f, cy - 5.5f, 7f, 7f);
                    g.DrawLine(p, cx, cy + 1.5f, cx, cy + 6f);
                }
                else if (kind == "key")
                {
                    g.DrawEllipse(p, cx - 5.5f, cy - 3f, 6f, 6f);
                    g.DrawLine(p, cx + 0.5f, cy, cx + 6.5f, cy);
                    g.DrawLine(p, cx + 4.5f, cy, cx + 4.5f, cy + 3f);
                }
                else if (kind == "copy")
                {
                    g.DrawRectangle(p, cx - 4.5f, cy - 2.5f, 7f, 8f);
                    g.DrawLine(p, cx - 2f, cy - 5f, cx + 5f, cy - 5f);
                    g.DrawLine(p, cx + 5f, cy - 5f, cx + 5f, cy + 3f);
                }
                else if (kind == "edit")
                {
                    g.DrawLine(p, cx - 4.5f, cy + 4.5f, cx + 4.5f, cy - 4.5f);
                    g.DrawLine(p, cx - 5f, cy + 5f, cx - 2f, cy + 4.5f);
                }
                else if (kind == "del")
                {
                    g.DrawLine(p, cx - 4f, cy - 4f, cx + 4f, cy + 4f);
                    g.DrawLine(p, cx + 4f, cy - 4f, cx - 4f, cy + 4f);
                }
            }
        }

        private int HitBtn(Point pt)
        {
            for (int i = 0; i < 3; i++) if (_actRects[i].Contains(pt)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hb = HitBtn(e.Location);
            if (!_hovered || hb != _hoverBtn) { _hovered = true; _hoverBtn = hb; Cursor = Cursors.Hand; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false; _hoverBtn = -1; Cursor = Cursors.Default; Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int hb = HitBtn(e.Location);
            if (hb == 2) { if (DeleteRequested != null) DeleteRequested(Entry); }
            else if (hb == 1)
            {
                if (TabKind == 0) { if (MoveToVaultRequested != null) MoveToVaultRequested(Entry); }
                else { if (EditRequested != null) EditRequested(Entry); }
            }
            else if (hb == 0)
            {
                if (TabKind == 0) { if (PinRequested != null) PinRequested(Entry); }
                else if (TabKind == 2) { Entry.Revealed = !Entry.Revealed; Invalidate(); }
                else { if (PasteRequested != null) PasteRequested(Entry, false); }
            }
            else
            {
                bool plain = (ModifierKeys & Keys.Shift) == Keys.Shift;
                if (PasteRequested != null) PasteRequested(Entry, plain);
            }
            base.OnMouseClick(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _thumb != null) { _thumb.Dispose(); _thumb = null; }
            base.Dispose(disposing);
        }
    }

    // ========================================================================
    // MAIN POPUP & ZERO-CPU TRAY CLIPBOARD DAEMON
    // ========================================================================
    internal sealed class ClipPopupForm : Form
    {
        private const int HK_CTRL_SHIFT_V = 101;
        private const int HK_CTRL_ALT_V = 102;
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_NAME = "NillyClipPro";

        private readonly ClipStore _store;
        private readonly bool _smokeMode;
        private bool _dark = true;
        private bool _paused = false;
        private bool _autoClearVault = true;
        private int _activeTab = 0; // 0=History, 1=Pinned, 2=Vault
        private int _selectedIdx = 0;
        private bool _allowClose = false;
        private IntPtr _prevForeground = IntPtr.Zero;
        private DateTime _ignoreClipUntilUtc = DateTime.MinValue;
        private uint _vaultClipboardSequence;
        private DateTime _vaultRetryUntil;
        private const string NoClipboardHistory = "ExcludeClipboardContentFromMonitorProcessing";
        private System.Windows.Forms.Timer _vaultClearTimer;
        private NotifyIcon _tray;
        private ToolStripMenuItem _miPause, _miAutoStart;
        private Bitmap _logoBmp;

        private Panel _header, _tabBar, _searchBar, _footer;
        private Button _btnPause, _btnAdd, _btnTheme, _btnHide;
        private readonly Button[] _tabBtns = new Button[3];
        private TextBox _txtSearch;
        private FlowLayoutPanel _listFlow;
        private readonly List<ClipRowCard> _rows = new List<ClipRowCard>();
        private Label _lblStatus, _lblAutoClear, _lblBrand;

        public ClipPopupForm(string customDir, bool smokeMode)
        {
            _smokeMode = smokeMode;
            _store = new ClipStore(customDir);
            _store.LoadAll();

            Text = "NillyClip Pro";
            Size = new Size(456, 580);
            MinimumSize = new Size(380, 420);
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyClipPro.ico");
            try
            {
                if (File.Exists(icoPath)) Icon = new Icon(icoPath);
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            _logoBmp = IconHelper.LoadCrispIcon(icoPath, 26);

            BuildUi();
            ApplyTheme();
            RebuildList();

            _vaultClearTimer = new System.Windows.Forms.Timer();
            _vaultClearTimer.Interval = 30000;
            _vaultClearTimer.Tick += delegate { ClearVaultClipboard(); };

            if (!smokeMode)
            {
                BuildTrayIcon();
                Deactivate += delegate { if (Visible) HideAndMask(); };
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetDarkTitlebar(Handle, _dark);
            if (!_smokeMode)
            {
                Native.AddClipboardFormatListener(Handle);
                Native.RegisterHotKey(Handle, HK_CTRL_SHIFT_V, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_V);
                Native.RegisterHotKey(Handle, HK_CTRL_ALT_V, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, Native.VK_V);
            }
        }

        private void BuildUi()
        {
            // 1. Minimal Top Header
            _header = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 10, 6) };
            PictureBox pic = new PictureBox { Left = 12, Top = 9, Width = 26, Height = 26, Image = _logoBmp, SizeMode = PictureBoxSizeMode.Zoom };
            Label lblTitle = new Label { Left = 44, Top = 10, Width = 140, Height = 24, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Text = "NillyClip Pro", UseMnemonic = false };

            FlowLayoutPanel rFlow = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 216, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            _btnHide = MkBtn("×", 28, delegate { HideAndMask(); });
            _btnTheme = MkBtn("☀", 28, delegate { _dark = !_dark; ApplyTheme(); });
            _btnPause = MkBtn("||", 28, delegate { TogglePause(); });
            _btnAdd = MkBtn("+ Νέο", 66, delegate { OpenAddEntryDialog(); });
            ToolTip tt = new ToolTip();
            tt.SetToolTip(_btnAdd, "Προσθήκη νέου Καρφιτσωμένου Snippet ή Κρυπτογραφημένου API Key");
            tt.SetToolTip(_btnPause, "Παύση / Συνέχιση καταγραφής Clipboard");
            tt.SetToolTip(_btnTheme, "Εναλλαγή Dark / Light");
            tt.SetToolTip(_btnHide, "Απόκρυψη στο Tray (Esc)");
            rFlow.Controls.AddRange(new Control[] { _btnHide, _btnTheme, _btnPause, _btnAdd });

            _header.Controls.AddRange(new Control[] { rFlow, pic, lblTitle });

            // 2. 3 Clean Segment Tabs
            _tabBar = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(12, 3, 12, 4) };
            string[] names = new string[] { "Ιστορικό", "Καρφιτσωμένα", "Κλειδοθήκη (API)" };
            int bw = 136;
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button b = MkBtn(names[i], bw, delegate { SwitchTab(idx); });
                b.Left = 12 + i * (bw + 6); b.Top = 3; b.Height = 28;
                _tabBtns[i] = b;
                _tabBar.Controls.Add(b);
            }

            // 3. Instant Search Box
            _searchBar = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(12, 4, 12, 6) };
            _txtSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            _txtSearch.TextChanged += delegate { _selectedIdx = 0; RebuildList(); };
            _searchBar.Controls.Add(_txtSearch);

            // 4. Bottom Footer
            _footer = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(10, 0, 10, 0) };
            _lblStatus = new Label { Dock = DockStyle.Left, Width = 142, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 7.8f), UseMnemonic = false, Text = "Ctrl+Shift+V" };
            _lblAutoClear = new Label { Dock = DockStyle.Left, Width = 104, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), Cursor = Cursors.Hand, UseMnemonic = false, Text = "Auto-Clear: 30s" };
            _lblAutoClear.Click += delegate { _autoClearVault = !_autoClearVault; UpdateAutoClearLabel(); };
            _lblBrand = new Label { Dock = DockStyle.Right, Width = 185, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 7.8f), Cursor = Cursors.Hand, UseMnemonic = false, Text = "by Nilly graphics · Sotiris" };
            _lblBrand.Click += delegate { try { Process.Start(new ProcessStartInfo("https://nillygraphics.com/") { UseShellExecute = true }); } catch { } };
            _footer.Controls.Add(_lblAutoClear);
            _footer.Controls.Add(_lblStatus);
            _footer.Controls.Add(_lblBrand);

            // 5. Scrollable Card List
            _listFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Padding = new Padding(12, 6, 12, 8)
            };
            _listFlow.SizeChanged += delegate { ResizeRowWidths(); };

            Controls.Add(_listFlow);
            Controls.Add(_searchBar);
            Controls.Add(_tabBar);
            Controls.Add(_header);
            Controls.Add(_footer);
        }

        private static Button MkBtn(string txt, int w, EventHandler onClick)
        {
            Button b = new Button { UseMnemonic = false, Text = txt, Width = w, Height = 26, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.4f, FontStyle.Bold), Cursor = Cursors.Hand, TabStop = false, Margin = new Padding(3, 1, 0, 0) };
            b.FlatAppearance.BorderSize = 1;
            b.Click += onClick;
            return b;
        }

        private void BuildTrayIcon()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Άνοιγμα NillyClip Pro  (Ctrl+Shift+V)", null, delegate { ShowPopup(); });
            menu.Items.Add("Κλειδοθήκη API Keys (DPAPI)", null, delegate { SwitchTab(2); ShowPopup(); });
            menu.Items.Add("Καρφιτσωμένα Snippets", null, delegate { SwitchTab(1); ShowPopup(); });
            menu.Items.Add(new ToolStripSeparator());
            _miPause = new ToolStripMenuItem("Παύση Καταγραφής");
            _miPause.Click += delegate { TogglePause(); };
            menu.Items.Add(_miPause);
            menu.Items.Add("Καθαρισμός Ιστορικού (κρατάει Καρφιτσωμένα & Κλειδοθήκη)", null, delegate { ClearHistoryOnly(); });
            menu.Items.Add(new ToolStripSeparator());
            _miAutoStart = new ToolStripMenuItem("Εκκίνηση με τα Windows");
            _miAutoStart.Checked = IsAutoStartEnabled();
            _miAutoStart.Click += delegate { ToggleAutoStart(); };
            menu.Items.Add(_miAutoStart);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("created by Nilly graphics (Sotiris Christos) — nillygraphics.com", null, delegate
            {
                try { Process.Start(new ProcessStartInfo("https://nillygraphics.com/") { UseShellExecute = true }); } catch { }
            });
            menu.Items.Add("Έξοδος", null, delegate { ExitApp(); });

            _tray = new NotifyIcon();
            _tray.Icon = Icon ?? SystemIcons.Application;
            _tray.Text = "NillyClip Pro (Ctrl+Shift+V)";
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;
            _tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Visible) HideAndMask(); else ShowPopup();
                }
            };
        }

        public void SwitchTab(int idx)
        {
            _activeTab = idx;
            _selectedIdx = 0;
            StyleTabs();
            RebuildList();
        }

        public void ApplyTheme()
        {
            Native.SetDarkTitlebar(Handle, _dark);
            Color bg = _dark ? Color.FromArgb(20, 21, 25) : Color.FromArgb(242, 244, 248);
            Color top = _dark ? Color.FromArgb(27, 29, 35) : Color.White;
            Color fg = _dark ? Color.FromArgb(242, 244, 248) : Color.FromArgb(22, 24, 30);
            Color muted = _dark ? Color.FromArgb(148, 156, 170) : Color.FromArgb(95, 105, 118);
            Color btnBg = _dark ? Color.FromArgb(38, 41, 49) : Color.FromArgb(236, 240, 246);
            Color border = _dark ? Color.FromArgb(54, 58, 68) : Color.FromArgb(214, 220, 228);
            Color orange = Color.FromArgb(252, 150, 31);

            BackColor = bg; _listFlow.BackColor = bg;
            _header.BackColor = top; _tabBar.BackColor = top; _searchBar.BackColor = top; _footer.BackColor = top;
            foreach (Control c in _header.Controls) if (c is Label) c.ForeColor = fg;
            _txtSearch.BackColor = _dark ? Color.FromArgb(22, 24, 29) : Color.White;
            _txtSearch.ForeColor = fg;
            _lblStatus.ForeColor = muted;
            _lblBrand.ForeColor = orange;
            _btnTheme.Text = _dark ? "☀" : "🌙";

            foreach (Button b in new Button[] { _btnPause, _btnTheme, _btnHide })
            { b.BackColor = btnBg; b.ForeColor = fg; b.FlatAppearance.BorderColor = border; }
            _btnAdd.BackColor = orange; _btnAdd.ForeColor = Color.FromArgb(14, 18, 24); _btnAdd.FlatAppearance.BorderColor = orange;
            UpdateAutoClearLabel();
            StyleTabs();
            for (int i = 0; i < _rows.Count; i++) _rows[i].SetTheme(_dark);
        }

        private void UpdateAutoClearLabel()
        {
            _lblAutoClear.Text = _autoClearVault ? "Auto-Clear: 30s" : "Auto-Clear: OFF";
            _lblAutoClear.ForeColor = _autoClearVault ? Color.FromArgb(166, 226, 46) : (_dark ? Color.FromArgb(130, 138, 152) : Color.Gray);
        }

        private void StyleTabs()
        {
            Color orange = Color.FromArgb(252, 150, 31);
            Color pink = Color.FromArgb(249, 38, 114);
            Color btnBg = _dark ? Color.FromArgb(36, 39, 46) : Color.FromArgb(236, 240, 246);
            Color fg = _dark ? Color.FromArgb(228, 232, 240) : Color.FromArgb(25, 28, 34);
            Color border = _dark ? Color.FromArgb(52, 56, 66) : Color.FromArgb(214, 220, 228);

            for (int i = 0; i < 3; i++)
            {
                bool act = (i == _activeTab);
                Color acc = (i == 2) ? pink : orange;
                _tabBtns[i].BackColor = act ? acc : btnBg;
                _tabBtns[i].ForeColor = act ? (i == 2 ? Color.White : Color.FromArgb(14, 18, 24)) : fg;
                _tabBtns[i].FlatAppearance.BorderColor = act ? acc : border;
            }
        }

        private List<ClipEntry> CurrentSourceList()
        {
            return _activeTab == 0 ? _store.History : (_activeTab == 1 ? _store.Pinned : _store.Vault);
        }

        public void RebuildList()
        {
            _listFlow.SuspendLayout();
            for (int i = 0; i < _rows.Count; i++) _rows[i].Dispose();
            _rows.Clear();
            _listFlow.Controls.Clear();

            List<ClipEntry> src = CurrentSourceList();
            string q = (_txtSearch.Text ?? "").Trim();
            int rowW = Math.Max(300, _listFlow.ClientSize.Width - 28);
            int num = 0;

            for (int i = 0; i < src.Count; i++)
            {
                ClipEntry e = src[i];
                if (q.Length > 0)
                {
                    bool hit = (e.Title != null && e.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (e.Category != null && e.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (e.Note != null && e.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (_activeTab != 2 && e.TextValue != null && e.TextValue.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!hit) continue;
                }
                num++;
                ClipRowCard card = new ClipRowCard(e, _activeTab, rowW, _store.ImgDir);
                card.ShortcutIndex = (_activeTab == 0 && num <= 9) ? num : 0;
                card.Selected = (_rows.Count == _selectedIdx);
                card.SetTheme(_dark);
                card.PasteRequested += OnPasteEntry;
                card.PinRequested += OnPinFromHistory;
                card.MoveToVaultRequested += OnSaveToVaultFromHistory;
                card.EditRequested += OnEditEntry;
                card.DeleteRequested += OnDeleteEntry;
                _rows.Add(card);
                _listFlow.Controls.Add(card);
            }

            if (_rows.Count == 0)
            {
                Label empty = new Label
                {
                    Width = rowW, Height = 90, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = _dark ? Color.FromArgb(130, 138, 152) : Color.Gray,
                    Font = new Font("Segoe UI", 9.2f),
                    Text = _activeTab == 0
                        ? "Το ιστορικό είναι άδειο.\r\nΑντέγραψε κείμενο, HEX χρώμα ή screenshot με Ctrl+C!"
                        : (_activeTab == 1
                            ? "Κανένα καρφιτσωμένο snippet.\r\nΠάτα «+ Νέο» ή το pin από το Ιστορικό (ΑΦΜ, IBAN, Emails, Links)."
                            : "Η κρυπτογραφημένη Κλειδοθήκη (DPAPI) είναι άδεια.\r\nΠάτα «+ Νέο» για να αποθηκεύσεις API Keys με ασφάλεια & ματάκι!")
                };
                _listFlow.Controls.Add(empty);
            }

            _listFlow.ResumeLayout();
            _tabBtns[0].Text = "Ιστορικό (" + _store.History.Count + ")";
            _tabBtns[1].Text = "Καρφιτσωμένα (" + _store.Pinned.Count + ")";
            _tabBtns[2].Text = "Κλειδοθήκη (" + _store.Vault.Count + ")";
            _lblStatus.Text = _paused ? "⏸ Παύση καταγραφής" : "Ctrl+Shift+V · Ενεργό";
        }

        private void ResizeRowWidths()
        {
            int w = Math.Max(300, _listFlow.ClientSize.Width - 28);
            for (int i = 0; i < _rows.Count; i++) _rows[i].Width = w;
        }

        // ====================================================================
        // CLIPBOARD CAPTURE (0% CPU EVENT-DRIVEN)
        // ====================================================================
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_CLIPBOARDUPDATE)
            {
                if (!_paused && DateTime.UtcNow >= _ignoreClipUntilUtc)
                    CaptureClipboardNow();
                return;
            }
            if (m.Msg == Native.WM_HOTKEY)
            {
                if (Visible) HideAndMask();
                else ShowPopup();
                return;
            }
            base.WndProc(ref m);
        }

        private void CaptureClipboardNow()
        {
            try
            {
                if (Clipboard.ContainsData(NoClipboardHistory)) return;
                if (Clipboard.ContainsText())
                {
                    string txt = Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(txt)) return;
                    // Avoid duplicate consecutive entries — move existing to top
                    _store.History.RemoveAll(delegate(ClipEntry e) { return e.Kind != ClipKind.Image && e.TextValue == txt; });

                    ClipEntry ne = new ClipEntry();
                    ne.TextValue = txt;
                    Color col;
                    if (ClipDetector.TryDetectColor(txt, out col)) { ne.Kind = ClipKind.ColorHex; ne.ParsedColor = col; }
                    else if (ClipDetector.IsUrl(txt)) ne.Kind = ClipKind.Url;
                    else ne.Kind = ClipKind.Text;

                    _store.History.Insert(0, ne);
                    if (_store.History.Count > 120) _store.History.RemoveRange(120, _store.History.Count - 120);
                    _store.SaveHistory();
                    if (Visible && _activeTab == 0) RebuildList();
                }
                else if (Clipboard.ContainsImage())
                {
                    using (Image img = Clipboard.GetImage())
                    {
                        if (img == null || img.Width < 4 || img.Height < 4) return;
                        ClipEntry ne = new ClipEntry();
                        ne.Kind = ClipKind.Image;
                        ne.ImgW = img.Width; ne.ImgH = img.Height;
                        ne.ImageFile = "clip_" + ne.Id + ".png";
                        img.Save(Path.Combine(_store.ImgDir, ne.ImageFile), ImageFormat.Png);
                        _store.History.Insert(0, ne);
                        if (_store.History.Count > 120) _store.History.RemoveRange(120, _store.History.Count - 120);
                        _store.SaveHistory();
                        if (Visible && _activeTab == 0) RebuildList();
                    }
                }
            }
            catch { }
        }

        // ====================================================================
        // POPUP SHOW / HIDE / AUTO-PASTE / VAULT SAFE COPY
        // ====================================================================
        public void ShowPopup()
        {
            _prevForeground = Native.GetForegroundWindow();
            _txtSearch.Text = "";
            _selectedIdx = 0;

            // Position cleanly near cursor without overflowing monitor bounds
            Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            int px = Math.Max(wa.Left + 12, Math.Min(wa.Right - Width - 12, Cursor.Position.X - Width / 2));
            int py = Math.Max(wa.Top + 12, Math.Min(wa.Bottom - Height - 12, Cursor.Position.Y - 40));
            Location = new Point(px, py);

            RebuildList();
            Show();
            WindowState = FormWindowState.Normal;
            Native.SetForegroundWindow(Handle);
            Activate();
            _txtSearch.Focus();
        }

        private void HideAndMask()
        {
            // Security: ALWAYS re-mask every revealed Vault eye when popup closes!
            for (int i = 0; i < _store.Vault.Count; i++) _store.Vault[i].Revealed = false;
            Hide();
            try { GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false); } catch { }
        }

        private void ClearVaultClipboard()
        {
            _vaultClearTimer.Stop();
            if (_vaultClipboardSequence == 0) return;
            uint exclusionFormat = Native.RegisterClipboardFormat(NoClipboardHistory);
            if (exclusionFormat != 0 && Native.OpenClipboard(Handle))
            {
                try
                {
                    if (Native.GetClipboardSequenceNumber() != _vaultClipboardSequence ||
                        !Native.IsClipboardFormatAvailable(exclusionFormat) || Native.EmptyClipboard())
                    {
                        _vaultClipboardSequence = 0;
                        return;
                    }
                }
                finally { Native.CloseClipboard(); }
            }
            if (DateTime.UtcNow < _vaultRetryUntil)
            {
                _vaultClearTimer.Interval = 500;
                _vaultClearTimer.Start();
            }
            else
            {
                _vaultClipboardSequence = 0;
                _lblStatus.Text = "Δεν έγινε αυτόματη εκκαθάριση. Καθάρισε το πρόχειρο χειροκίνητα.";
            }
        }

        private void CopyVaultEntry(ClipEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(entry.TextValue)) return;
                using (MemoryStream marker = new MemoryStream(new byte[4]))
                {
                    DataObject data = new DataObject();
                    data.SetText(entry.TextValue, TextDataFormat.UnicodeText);
                    data.SetData(NoClipboardHistory, false, marker);
                    Clipboard.SetDataObject(data, true, 3, 100);
                }
                _vaultClipboardSequence = _autoClearVault ? Native.GetClipboardSequenceNumber() : 0;
                _vaultClearTimer.Stop();
                _vaultRetryUntil = DateTime.UtcNow.AddSeconds(35);
                if (_vaultClipboardSequence != 0)
                {
                    _vaultClearTimer.Interval = 30000;
                    _vaultClearTimer.Start();
                }
                _lblStatus.Text = "Το κλειδί αντιγράφηκε. Επικόλλησέ το στο πεδίο που θέλεις.";
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                _lblStatus.Text = "Το πρόχειρο είναι απασχολημένο. Δοκίμασε ξανά.";
            }
        }

        private void OnPasteEntry(ClipEntry entry, bool plainOnly)
        {
            if (entry == null) return;
            if (_activeTab == 2)
            {
                CopyVaultEntry(entry);
                return;
            }
            _ignoreClipUntilUtc = DateTime.UtcNow.AddMilliseconds(900);

            try
            {
                if (entry.Kind == ClipKind.Image && !string.IsNullOrEmpty(entry.ImageFile))
                {
                    string full = Path.Combine(_store.ImgDir, entry.ImageFile);
                    if (File.Exists(full))
                    {
                        using (Image img = Image.FromFile(full)) Clipboard.SetImage(img);
                    }
                }
                else
                {
                    string val = entry.TextValue ?? "";
                    if (plainOnly) val = val.Trim();
                    Clipboard.SetText(val);
                }
            }
            catch { }

            if (_smokeMode) return;

            HideAndMask();
            // Auto-paste into previous active window!
            if (_prevForeground != IntPtr.Zero && _prevForeground != Handle)
            {
                IntPtr target = _prevForeground;
                Thread th = new Thread(delegate()
                {
                    Thread.Sleep(90);
                    Native.SetForegroundWindow(target);
                    Thread.Sleep(55);
                    Native.SendCtrlV();
                });
                th.IsBackground = true;
                th.Start();
            }
        }

        private void OnPinFromHistory(ClipEntry e)
        {
            if (e == null) return;
            ClipEntry pin = new ClipEntry();
            pin.Kind = e.Kind;
            pin.TextValue = e.TextValue;
            pin.ImageFile = e.ImageFile;
            pin.ImgW = e.ImgW; pin.ImgH = e.ImgH;
            pin.ParsedColor = e.ParsedColor;
            string s = (e.TextValue ?? "").Trim();
            pin.Title = s.Length > 26 ? s.Substring(0, 26) + "…" : (s.Length > 0 ? s : "Screenshot");
            pin.Category = e.Kind == ClipKind.ColorHex ? "Χρώμα" : (e.Kind == ClipKind.Url ? "Link" : "Στοιχεία");
            _store.Pinned.Insert(0, pin);
            _store.SavePinned();
            _lblStatus.Text = "✓ Καρφιτσώθηκε!";
            _tabBtns[1].Text = "Καρφιτσωμένα (" + _store.Pinned.Count + ")";
        }

        private void OnSaveToVaultFromHistory(ClipEntry e)
        {
            if (e == null || e.Kind == ClipKind.Image) return;
            ClipEntry draft = new ClipEntry { Title = "API Key", Category = "API Key", TextValue = e.TextValue };
            bool prevDeact = TopMost;
            TopMost = false;
            using (EntryEditorDialog dlg = new EntryEditorDialog(true, draft, _dark))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    draft.Title = dlg.EntryTitle;
                    draft.Category = dlg.EntryCategory;
                    draft.TextValue = dlg.EntryValue;
                    draft.Note = dlg.EntryNote;
                    _store.Vault.Insert(0, draft);
                    if (!_store.SaveVaultEncrypted())
                    {
                        _store.Vault.Remove(draft);
                        TopMost = prevDeact;
                        _lblStatus.Text = "Δεν αποθηκεύτηκε το κλειδί.";
                        return;
                    }
                    // Remove the original entry after successful encrypted storage.
                    _store.History.Remove(e);
                    _store.SaveHistory();
                    SwitchTab(2);
                    _lblStatus.Text = "✓ Κρυπτογραφήθηκε (DPAPI)!";
                }
            }
            TopMost = prevDeact;
        }

        private void OpenAddEntryDialog()
        {
            bool isVault = (_activeTab == 2);
            bool prevTop = TopMost;
            TopMost = false;
            using (EntryEditorDialog dlg = new EntryEditorDialog(isVault, null, _dark))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    ClipEntry ne = new ClipEntry();
                    ne.Title = dlg.EntryTitle;
                    ne.Category = dlg.EntryCategory;
                    ne.TextValue = dlg.EntryValue;
                    ne.Note = dlg.EntryNote;
                    Color c;
                    if (!isVault && ClipDetector.TryDetectColor(ne.TextValue, out c)) { ne.Kind = ClipKind.ColorHex; ne.ParsedColor = c; }
                    else if (!isVault && ClipDetector.IsUrl(ne.TextValue)) ne.Kind = ClipKind.Url;
                    else ne.Kind = ClipKind.Text;

                    if (isVault)
                    {
                        _store.Vault.Insert(0, ne);
                        if (!_store.SaveVaultEncrypted())
                        {
                            _store.Vault.Remove(ne);
                            TopMost = prevTop;
                            _lblStatus.Text = "Δεν αποθηκεύτηκε το κλειδί.";
                            return;
                        }
                    }
                    else { _store.Pinned.Insert(0, ne); _store.SavePinned(); if (_activeTab == 0) _activeTab = 1; }
                    StyleTabs();
                    RebuildList();
                }
            }
            TopMost = prevTop;
        }

        private void OnEditEntry(ClipEntry e)
        {
            if (e == null) return;
            bool isVault = (_activeTab == 2);
            bool prevTop = TopMost;
            TopMost = false;
            using (EntryEditorDialog dlg = new EntryEditorDialog(isVault, e, _dark))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string[] previous = { e.Title, e.Category, e.TextValue, e.Note };
                    e.Title = dlg.EntryTitle;
                    e.Category = dlg.EntryCategory;
                    e.TextValue = dlg.EntryValue;
                    e.Note = dlg.EntryNote;
                    if (isVault && !_store.SaveVaultEncrypted())
                    {
                        e.Title = previous[0];
                        e.Category = previous[1];
                        e.TextValue = previous[2];
                        e.Note = previous[3];
                        TopMost = prevTop;
                        _lblStatus.Text = "Δεν αποθηκεύτηκαν οι αλλαγές.";
                        return;
                    }
                    if (!isVault) _store.SavePinned();
                    RebuildList();
                }
            }
            TopMost = prevTop;
        }

        private void OnDeleteEntry(ClipEntry e)
        {
            if (e == null) return;
            if (_activeTab == 0) { _store.History.Remove(e); _store.SaveHistory(); }
            else if (_activeTab == 1) { _store.Pinned.Remove(e); _store.SavePinned(); }
            else
            {
                int position = _store.Vault.IndexOf(e);
                if (position < 0) return;
                _store.Vault.RemoveAt(position);
                if (!_store.SaveVaultEncrypted())
                {
                    _store.Vault.Insert(position, e);
                    _lblStatus.Text = "Δεν διαγράφηκε το κλειδί.";
                    return;
                }
            }
            RebuildList();
        }

        private void ClearHistoryOnly()
        {
            _store.History.Clear();
            _store.SaveHistory();
            if (Visible && _activeTab == 0) RebuildList();
        }

        private void TogglePause()
        {
            _paused = !_paused;
            if (_miPause != null) _miPause.Checked = _paused;
            _btnPause.Text = _paused ? "▶" : "||";
            _lblStatus.Text = _paused ? "Παύση καταγραφής" : "Ctrl+Shift+V · Ενεργό";
        }

        private static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
                    return k != null && k.GetValue(RUN_NAME) != null;
            }
            catch { return false; }
        }

        private void ToggleAutoStart()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    if (k == null) return;
                    if (IsAutoStartEnabled()) { k.DeleteValue(RUN_NAME, false); _miAutoStart.Checked = false; }
                    else { k.SetValue(RUN_NAME, "\"" + Application.ExecutablePath + "\" -tray"); _miAutoStart.Checked = true; }
                }
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { HideAndMask(); return true; }
            if (keyData == Keys.Down && _rows.Count > 0)
            {
                _selectedIdx = Math.Min(_rows.Count - 1, _selectedIdx + 1);
                UpdateRowSelection(); return true;
            }
            if (keyData == Keys.Up && _rows.Count > 0)
            {
                _selectedIdx = Math.Max(0, _selectedIdx - 1);
                UpdateRowSelection(); return true;
            }
            if ((keyData == Keys.Enter || keyData == (Keys.Shift | Keys.Enter)) && _rows.Count > 0 && _selectedIdx < _rows.Count)
            {
                OnPasteEntry(_rows[_selectedIdx].Entry, keyData == (Keys.Shift | Keys.Enter));
                return true;
            }
            // Ctrl+1..9 instant paste
            if ((keyData & Keys.Control) == Keys.Control)
            {
                Keys k = keyData & Keys.KeyCode;
                if (k >= Keys.D1 && k <= Keys.D9)
                {
                    int idx = (int)(k - Keys.D1);
                    if (idx < _rows.Count) { OnPasteEntry(_rows[idx].Entry, false); return true; }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateRowSelection()
        {
            for (int i = 0; i < _rows.Count; i++) { _rows[i].Selected = (i == _selectedIdx); _rows[i].Invalidate(); }
            if (_selectedIdx >= 0 && _selectedIdx < _rows.Count) _listFlow.ScrollControlIntoView(_rows[_selectedIdx]);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && !_smokeMode && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideAndMask();
                return;
            }
            if (!_smokeMode)
            {
                try
                {
                    Native.RemoveClipboardFormatListener(Handle);
                    Native.UnregisterHotKey(Handle, HK_CTRL_SHIFT_V);
                    Native.UnregisterHotKey(Handle, HK_CTRL_ALT_V);
                }
                catch { }
            }
            ClearVaultClipboard();
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            base.OnFormClosing(e);
        }

        private void ExitApp()
        {
            _allowClose = true;
            Close();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_logoBmp != null) { _logoBmp.Dispose(); _logoBmp = null; }
                if (_vaultClearTimer != null) _vaultClearTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class Program
    {
        private const string IPC_EVENT = "Local\\NillyClipPro_ShowPopup";

        [STAThread]
        public static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool startHiddenTray = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("--test-smoke", StringComparison.OrdinalIgnoreCase))
                        return RunSmokeTest(i + 1 < args.Length ? args[i + 1] : null, i + 2 < args.Length ? args[i + 2] : null);
                    if (args[i].Equals("-tray", StringComparison.OrdinalIgnoreCase))
                        startHiddenTray = true;
                }
            }

            bool createdNew;
            using (Mutex m = new Mutex(true, "Local\\NillyClipPro_SingleInstance_v1", out createdNew))
            {
                if (!createdNew)
                {
                    // Signal existing tray instance to show popup immediately!
                    try { using (EventWaitHandle ev = EventWaitHandle.OpenExisting(IPC_EVENT)) ev.Set(); } catch { }
                    return 0;
                }

                ClipPopupForm form = new ClipPopupForm(null, false);
                // Force handle creation so AddClipboardFormatListener & Hotkeys work immediately even when started hidden in tray!
                IntPtr h = form.Handle;

                EventWaitHandle ipc = new EventWaitHandle(false, EventResetMode.AutoReset, IPC_EVENT);
                bool running = true;
                Thread ipcTh = new Thread(delegate()
                {
                    while (running)
                    {
                        if (ipc.WaitOne(400) && running)
                        {
                            try { form.BeginInvoke((Action)delegate { form.ShowPopup(); }); } catch { }
                        }
                    }
                });
                ipcTh.IsBackground = true;
                ipcTh.Start();

                if (!startHiddenTray) form.ShowPopup();
                Application.Run();
                running = false;
                try { ipc.Set(); ipc.Dispose(); } catch { }
            }
            return 0;
        }

        private static int RunSmokeTest(string logPath, string previewPngPath)
        {
            StringBuilder sb = new StringBuilder();
            string tmpDir = Path.Combine(Path.GetTempPath(), "NillyClipPro_Smoke_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try
            {
                // 1. Test DPAPI Encryption round-trip AND verify ZERO plaintext on disk!
                string secret = "sk-proj-NillySecretKey9876543210XYZ";
                ClipStore st = new ClipStore(tmpDir);
                st.Vault.Add(new ClipEntry { Title = "OpenAI API Key", Category = "API Key", TextValue = secret, Note = "ChatGPT Work" });
                st.Vault.Add(new ClipEntry { Title = "GitHub Token", Category = "Token", TextValue = "ghp_nillynp_SuperSecretToken9999", Note = "github.com/nillynp" });
                st.Pinned.Add(new ClipEntry { Title = "Website", Category = "Link", Kind = ClipKind.Url, TextValue = "https://nillygraphics.com/" });
                st.Pinned.Add(new ClipEntry { Title = "Brand Orange", Category = "Χρώμα", Kind = ClipKind.ColorHex, TextValue = "#FC961F", ParsedColor = Color.FromArgb(252, 150, 31) });
                st.History.Add(new ClipEntry { Kind = ClipKind.ColorHex, TextValue = "#66D9EF", ParsedColor = Color.FromArgb(102, 217, 239) });
                st.History.Add(new ClipEntry { Kind = ClipKind.Text, TextValue = "Καλημέρα — NillyClip Pro Δοκιμή Ελληνικών" });
                st.SaveHistory();
                st.SavePinned();
                if (!st.SaveVaultEncrypted()) throw new IOException("Could not write the test vault.");

                byte[] rawCipher = File.ReadAllBytes(st.VaultPath);
                string rawAscii = Encoding.UTF8.GetString(rawCipher);
                bool zeroPlaintextOnDisk = !rawAscii.Contains("NillySecretKey") && !rawAscii.Contains("SuperSecretToken");

                ClipStore st2 = new ClipStore(tmpDir);
                st2.LoadAll();
                bool decryptOk = (st2.Vault.Count == 2 && st2.Vault[0].TextValue == secret);
                bool maskOk = (st2.Vault[0].MaskedValue().Contains("••••") && !st2.Vault[0].MaskedValue().Contains("NillySecretKey"));

                Color c;
                bool hexOk = ClipDetector.TryDetectColor("#FC961F", out c) && c.R == 252 && c.G == 150 && c.B == 31;

                sb.AppendLine("DPAPI_ZERO_PLAINTEXT_ON_DISK=" + zeroPlaintextOnDisk);
                sb.AppendLine("DPAPI_DECRYPT_OK=" + decryptOk);
                sb.AppendLine("SECRET_MASK_OK=" + maskOk);
                sb.AppendLine("HEX_COLOR_DETECT_OK=" + hexOk);

                // 2. Test UI Popup + Vault Tab with Eye icon + Preview PNG
                using (ClipPopupForm f = new ClipPopupForm(tmpDir, true))
                {
                    f.Show();
                    f.SwitchTab(2); // Show Encrypted Vault with Eye icons for visual check!
                    Application.DoEvents();
                    if (!string.IsNullOrEmpty(previewPngPath))
                    {
                        using (Bitmap shot = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot, new Rectangle(0, 0, f.Width, f.Height));
                            shot.Save(previewPngPath, ImageFormat.Png);
                        }
                    }
                    f.SwitchTab(0);
                    Application.DoEvents();
                    long ramMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                    sb.AppendLine("WINDOW_OPENED_OK=" + f.IsHandleCreated);
                    sb.AppendLine("WORKING_SET_MB=" + ramMb);
                    f.Close();
                }
                sb.AppendLine("SMOKE_RESULT=" + ((zeroPlaintextOnDisk && decryptOk && maskOk && hexOk) ? "PASS" : "FAIL"));
            }
            catch (Exception ex)
            {
                sb.AppendLine("SMOKE_RESULT=FAIL");
                sb.AppendLine(ex.ToString());
                if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return 1;
            }
            finally
            {
                try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
            }
            if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }
}
