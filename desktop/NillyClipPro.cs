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
[assembly: AssemblyVersion("1.2.2.0")]
[assembly: AssemblyFileVersion("1.2.2.0")]

namespace NillyClipPro
{
    internal static class Native
    {
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_TOGGLE_POPUP = 0x0400 + 202;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint VK_V = 0x56;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const byte VK_MASK_DUMMY = 0xE8; // Unassigned VK — suppresses Start Menu when swallowing Win+V!
        public const byte VK_CONTROL_B = 0x11;
        public const byte VK_V_B = 0x56;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint WDA_NONE = 0x00000000;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
        [DllImport("user32.dll")] public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
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
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);

        public static bool IsKeyDown(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }

        public static void SuppressWinStartMenu()
        {
            try
            {
                keybd_event(VK_MASK_DUMMY, 0, 0, UIntPtr.Zero);
                keybd_event(VK_MASK_DUMMY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }

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
        public string Title;      // For Pinned Snippets & Vault API Keys / Logins
        public string Category;   // e.g. "API Key", "Login / Web", "Στοιχεία"
        public string Username;   // Optional Username / Email for Vault logins
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
            if (!string.IsNullOrEmpty(Category) &&
                (Category.IndexOf("Κάρτα", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 Category.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 Category.IndexOf("IBAN", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string clean = s.Replace(" ", "").Replace("-", "");
                if (clean.Length >= 8) return "•••• •••• •••• " + clean.Substring(clean.Length - 4);
                return "•••• •••• •••• ••••";
            }
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
        public readonly string PinPath;

        public readonly List<ClipEntry> History = new List<ClipEntry>();
        public readonly List<ClipEntry> Pinned = new List<ClipEntry>();
        public readonly List<ClipEntry> Vault = new List<ClipEntry>();
        public string PinSaltAndHash; // Stored DPAPI-encrypted; empty = no PIN lock
        private byte[] _sessionPinDpapi; // Active PIN wrapped in DPAPI in RAM (cleared on lock!)
        private int _failedPinAttempts = 0;
        private DateTime _pinLockoutUntilUtc = DateTime.MinValue;

        public ClipStore(string customDir)
        {
            DataDir = string.IsNullOrEmpty(customDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NillyClipPro")
                : customDir;
            ImgDir = Path.Combine(DataDir, "img");
            HistoryPath = Path.Combine(DataDir, "history.dat");
            PinnedPath = Path.Combine(DataDir, "pinned.dat");
            VaultPath = Path.Combine(DataDir, "vault.dpapi");
            PinPath = Path.Combine(DataDir, "vault_pin.dpapi");
            try { Directory.CreateDirectory(DataDir); Directory.CreateDirectory(ImgDir); } catch { }
        }

        public void LoadAll()
        {
            History.Clear(); Pinned.Clear(); Vault.Clear();
            LoadListPlain(HistoryPath, History);
            LoadListPlain(PinnedPath, Pinned);
            LoadPinHash();
            // Security: if PIN is configured, DO NOT decrypt Vault into RAM until user unlocks!
            if (!HasPin) LoadVaultEncrypted(null);
        }

        public bool HasPin { get { return !string.IsNullOrEmpty(PinSaltAndHash); } }
        public bool IsLocked { get { return HasPin && _sessionPinDpapi == null; } }
        public int LockoutSecondsRemaining
        {
            get
            {
                double rem = (_pinLockoutUntilUtc - DateTime.UtcNow).TotalSeconds;
                return rem > 0 ? (int)Math.Ceiling(rem) : 0;
            }
        }

        public void LockVault()
        {
            if (_sessionPinDpapi != null)
            {
                Array.Clear(_sessionPinDpapi, 0, _sessionPinDpapi.Length);
                _sessionPinDpapi = null;
            }
            // Security: purge decrypted secrets from RAM immediately on lock!
            if (HasPin)
            {
                for (int i = 0; i < Vault.Count; i++) { Vault[i].Revealed = false; Vault[i].TextValue = null; }
                Vault.Clear();
            }
        }

        public bool UnlockVault(string pin, out string errMsg)
        {
            errMsg = null;
            if (!HasPin) return true;
            int waitSec = LockoutSecondsRemaining;
            if (waitSec > 0) { errMsg = "Περίμενε " + waitSec + " δευτ. (προστασία brute-force)."; return false; }
            if (!VerifyPin(pin))
            {
                _failedPinAttempts++;
                if (_failedPinAttempts >= 3)
                {
                    int pen = Math.Min(30, 3 * (1 << Math.Min(3, _failedPinAttempts - 3)));
                    _pinLockoutUntilUtc = DateTime.UtcNow.AddSeconds(pen);
                    errMsg = "Λάθος PIN — αναμονή " + pen + " δευτ.";
                }
                else errMsg = "Λάθος PIN (" + _failedPinAttempts + "/3).";
                return false;
            }
            _failedPinAttempts = 0;
            _pinLockoutUntilUtc = DateTime.MinValue;
            SetSessionPin(pin);
            Vault.Clear();
            LoadVaultEncrypted(pin);
            return true;
        }

        private void SetSessionPin(string pin)
        {
            if (_sessionPinDpapi != null) { Array.Clear(_sessionPinDpapi, 0, _sessionPinDpapi.Length); _sessionPinDpapi = null; }
            if (string.IsNullOrEmpty(pin)) return;
            byte[] p = Encoding.UTF8.GetBytes(pin);
            _sessionPinDpapi = ProtectedData.Protect(p, VaultEntropy, DataProtectionScope.CurrentUser);
            Array.Clear(p, 0, p.Length);
        }

        private string UnwrapSessionPin()
        {
            if (_sessionPinDpapi == null) return null;
            byte[] p = ProtectedData.Unprotect(_sessionPinDpapi, VaultEntropy, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(p); }
            finally { Array.Clear(p, 0, p.Length); }
        }

        public void SetPin(string pin)
        {
            if (string.IsNullOrEmpty(pin))
            {
                PinSaltAndHash = null;
                SetSessionPin(null);
                try { if (File.Exists(PinPath)) File.Delete(PinPath); } catch { }
                // Re-save Vault with DPAPI-only layer so existing entries stay readable
                SaveVaultEncrypted();
                return;
            }
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);
            string hash = HashPin(pin, salt);
            PinSaltAndHash = Convert.ToBase64String(salt) + ":" + hash;
            try
            {
                byte[] plain = Encoding.UTF8.GetBytes(PinSaltAndHash);
                byte[] enc = ProtectedData.Protect(plain, VaultEntropy, DataProtectionScope.CurrentUser);
                Array.Clear(plain, 0, plain.Length);
                File.WriteAllBytes(PinPath, enc);
            }
            catch { }
            SetSessionPin(pin);
            // Upgrade vault.dpapi immediately to Double-Layer (AES-256 + HMAC-SHA256 + DPAPI)!
            SaveVaultEncrypted();
        }

        public bool VerifyPin(string pin)
        {
            if (!HasPin) return true;
            if (string.IsNullOrEmpty(pin)) return false;
            try
            {
                string[] parts = PinSaltAndHash.Split(':');
                if (parts.Length != 2) return false;
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                byte[] actual = HashPinBytes(pin, salt);
                return FixedTimeEquals(expected, actual);
            }
            catch { return false; }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= (a[i] ^ b[i]);
            return diff == 0;
        }

        private static byte[] HashPinBytes(string pin, byte[] salt)
        {
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(pin, salt, 100000))
                return kdf.GetBytes(32);
        }

        private static string HashPin(string pin, byte[] salt)
        {
            return Convert.ToBase64String(HashPinBytes(pin, salt));
        }

        private void LoadPinHash()
        {
            PinSaltAndHash = null;
            try
            {
                if (!File.Exists(PinPath)) return;
                byte[] enc = File.ReadAllBytes(PinPath);
                byte[] plain = ProtectedData.Unprotect(enc, VaultEntropy, DataProtectionScope.CurrentUser);
                PinSaltAndHash = Encoding.UTF8.GetString(plain).Trim();
                Array.Clear(plain, 0, plain.Length);
            }
            catch { }
        }

        // Encrypt-then-MAC: AES-256-CBC + HMAC-SHA256 (100k PBKDF2, split 32B EncKey + 32B MacKey)
        // Binary layout: "NVL2"(4) || salt(16) || iv(16) || hmac(32) || ciphertext
        private static byte[] EncryptAuthenticated(byte[] plain, string passphrase)
        {
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) { rng.GetBytes(salt); rng.GetBytes(iv); }
            byte[] derived;
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(passphrase, salt, 100000)) derived = kdf.GetBytes(64);
            byte[] encKey = new byte[32]; Buffer.BlockCopy(derived, 0, encKey, 0, 32);
            byte[] macKey = new byte[32]; Buffer.BlockCopy(derived, 32, macKey, 0, 32);
            Array.Clear(derived, 0, derived.Length);

            byte[] cipher;
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encKey; aes.IV = iv;
                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plain, 0, plain.Length);
                        cs.FlushFinalBlock();
                        cipher = ms.ToArray();
                    }
                }
                byte[] magic = Encoding.ASCII.GetBytes("NVL2");
                byte[] mac;
                using (HMACSHA256 hmac = new HMACSHA256(macKey))
                {
                    hmac.TransformBlock(magic, 0, 4, null, 0);
                    hmac.TransformBlock(salt, 0, 16, null, 0);
                    hmac.TransformBlock(iv, 0, 16, null, 0);
                    hmac.TransformFinalBlock(cipher, 0, cipher.Length);
                    mac = hmac.Hash;
                }
                byte[] output = new byte[68 + cipher.Length];
                Buffer.BlockCopy(magic, 0, output, 0, 4);
                Buffer.BlockCopy(salt, 0, output, 4, 16);
                Buffer.BlockCopy(iv, 0, output, 20, 16);
                Buffer.BlockCopy(mac, 0, output, 36, 32);
                Buffer.BlockCopy(cipher, 0, output, 68, cipher.Length);
                return output;
            }
            finally
            {
                Array.Clear(encKey, 0, encKey.Length);
                Array.Clear(macKey, 0, macKey.Length);
            }
        }

        private static byte[] DecryptAuthenticated(byte[] blob, string passphrase)
        {
            if (blob == null || blob.Length < 84 || blob[0] != 'N' || blob[1] != 'V' || blob[2] != 'L' || blob[3] != '2')
                throw new InvalidDataException("Μη έγκυρη μορφή κρυπτογραφημένων δεδομένων (NVL2).");

            byte[] magic = new byte[4]; Buffer.BlockCopy(blob, 0, magic, 0, 4);
            byte[] salt = new byte[16]; Buffer.BlockCopy(blob, 4, salt, 0, 16);
            byte[] iv = new byte[16]; Buffer.BlockCopy(blob, 20, iv, 0, 16);
            byte[] storedMac = new byte[32]; Buffer.BlockCopy(blob, 36, storedMac, 0, 32);
            byte[] cipher = new byte[blob.Length - 68]; Buffer.BlockCopy(blob, 68, cipher, 0, cipher.Length);

            byte[] derived;
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(passphrase, salt, 100000)) derived = kdf.GetBytes(64);
            byte[] encKey = new byte[32]; Buffer.BlockCopy(derived, 0, encKey, 0, 32);
            byte[] macKey = new byte[32]; Buffer.BlockCopy(derived, 32, macKey, 0, 32);
            Array.Clear(derived, 0, derived.Length);

            try
            {
                // Verify HMAC-SHA256 in constant time BEFORE decrypting (blocks tampering & padding oracles!)
                byte[] expectedMac;
                using (HMACSHA256 hmac = new HMACSHA256(macKey))
                {
                    hmac.TransformBlock(magic, 0, 4, null, 0);
                    hmac.TransformBlock(salt, 0, 16, null, 0);
                    hmac.TransformBlock(iv, 0, 16, null, 0);
                    hmac.TransformFinalBlock(cipher, 0, cipher.Length);
                    expectedMac = hmac.Hash;
                }
                if (!FixedTimeEquals(storedMac, expectedMac))
                    throw new CryptographicException("Λάθος κωδικός ή αλλοιωμένο αρχείο.");

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encKey; aes.IV = iv;
                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipher, 0, cipher.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
            finally
            {
                Array.Clear(encKey, 0, encKey.Length);
                Array.Clear(macKey, 0, macKey.Length);
            }
        }

        // Portable AES-256 + HMAC-SHA256 + PBKDF2 encrypted Vault Backup (.nvault) — works across PCs & Windows formats!
        public void ExportVaultBackup(string destFile, string passphrase)
        {
            byte[] plain = Encoding.UTF8.GetBytes("NILLYVAULT2\n" + SerializeEntries(Vault));
            try
            {
                byte[] encrypted = EncryptAuthenticated(plain, passphrase);
                File.WriteAllBytes(destFile, encrypted);
            }
            finally { Array.Clear(plain, 0, plain.Length); }
        }

        public int ImportVaultBackup(string srcFile, string passphrase)
        {
            byte[] raw = File.ReadAllBytes(srcFile);
            byte[] plain = DecryptAuthenticated(raw, passphrase);
            string text;
            try { text = Encoding.UTF8.GetString(plain); }
            finally { Array.Clear(plain, 0, plain.Length); }
            if (!text.StartsWith("NILLYVAULT2\n", StringComparison.Ordinal))
                throw new CryptographicException("Λάθος κωδικός backup.");

            List<ClipEntry> imported = new List<ClipEntry>();
            DeserializeEntries(text.Substring(12), imported);
            int added = 0;
            for (int i = 0; i < imported.Count; i++)
            {
                ClipEntry imp = imported[i];
                bool exists = false;
                for (int j = 0; j < Vault.Count; j++)
                    if (Vault[j].Title == imp.Title && Vault[j].TextValue == imp.TextValue) { exists = true; break; }
                if (!exists) { Vault.Add(imp); added++; }
            }
            if (added > 0) SaveVaultEncrypted();
            return added;
        }

        public static string GenerateStrongPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*?";
            byte[] buf = new byte[length];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) rng.GetBytes(buf);
            char[] res = new char[length];
            for (int i = 0; i < length; i++) res[i] = chars[buf[i] % chars.Length];
            return new string(res);
        }

        public void SaveHistory() { SaveListPlain(HistoryPath, History); PruneUnusedImages(); }
        public void SavePinned() { SaveListPlain(PinnedPath, Pinned); }

        public void SaveVaultEncrypted()
        {
            try
            {
                if (IsLocked) return; // Never overwrite locked vault with empty RAM list!
                string serialized = SerializeEntries(Vault);
                byte[] plainBytes = Encoding.UTF8.GetBytes(serialized);
                byte[] innerPayload;
                string activePin = UnwrapSessionPin();
                if (HasPin && !string.IsNullOrEmpty(activePin))
                {
                    // Layer 1: AES-256-CBC + HMAC-SHA256 derived from User PIN (100k PBKDF2)
                    innerPayload = EncryptAuthenticated(plainBytes, activePin);
                }
                else
                {
                    innerPayload = plainBytes;
                }
                // Layer 2: Windows DPAPI CurrentUser encryption — ZERO plaintext secrets on disk!
                byte[] cipherBytes = ProtectedData.Protect(innerPayload, VaultEntropy, DataProtectionScope.CurrentUser);
                Array.Clear(plainBytes, 0, plainBytes.Length);
                if (innerPayload != plainBytes) Array.Clear(innerPayload, 0, innerPayload.Length);
                File.WriteAllBytes(VaultPath, cipherBytes);
            }
            catch { }
        }

        private void LoadVaultEncrypted(string activePin)
        {
            try
            {
                if (!File.Exists(VaultPath)) return;
                byte[] cipherBytes = File.ReadAllBytes(VaultPath);
                if (cipherBytes == null || cipherBytes.Length == 0) return;
                byte[] inner = ProtectedData.Unprotect(cipherBytes, VaultEntropy, DataProtectionScope.CurrentUser);
                byte[] plainBytes;
                bool isNvl2 = (inner.Length >= 84 && inner[0] == 'N' && inner[1] == 'V' && inner[2] == 'L' && inner[3] == '2');
                if (isNvl2)
                {
                    if (string.IsNullOrEmpty(activePin)) { Array.Clear(inner, 0, inner.Length); return; }
                    plainBytes = DecryptAuthenticated(inner, activePin);
                    Array.Clear(inner, 0, inner.Length);
                }
                else
                {
                    plainBytes = inner;
                }
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
                  .Append(e.CreatedUtc.Ticks).Append('|')
                  .Append(B64(e.Username)).Append("\r\n");
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
                if (p.Length >= 11) e.Username = FromB64(p[10]);
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
        public string EntryUsername { get { return _txtUser != null ? _txtUser.Text.Trim() : ""; } }
        public string EntryValue { get { return _txtVal.Text; } }
        public string EntryNote { get { return _txtNote.Text.Trim(); } }

        private TextBox _txtTitle, _txtUser, _txtVal, _txtNote;
        private ComboBox _cmbCat;
        private Button _btnEye;
        private bool _masked;

        public EntryEditorDialog(bool isVault, ClipEntry existing, bool dark)
        {
            _masked = isVault;
            Text = isVault
                ? (existing == null ? "Νέο Κλειδί / Κωδικός (DPAPI)" : "Επεξεργασία Κλειδοθήκης")
                : (existing == null ? "Νέο Καρφιτσωμένο Snippet" : "Επεξεργασία Snippet");
            ClientSize = new Size(440, isVault ? 356 : 320);
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

            Label l1 = new Label { Left = 16, Top = 14, Width = 220, Text = isVault ? "Υπηρεσία / Τίτλος:" : "Τίτλος Snippet:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _txtTitle = new TextBox { Left = 16, Top = 34, Width = 240, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Text = existing != null ? existing.Title : "" };

            Label l2 = new Label { Left = 266, Top = 14, Width = 158, Text = "Κατηγορία:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _cmbCat = new ComboBox { Left = 266, Top = 33, Width = 158, BackColor = inp, ForeColor = fg, Font = new Font("Segoe UI", 9.2f), DropDownStyle = ComboBoxStyle.DropDown };
            if (isVault) _cmbCat.Items.AddRange(new object[] { "API Key", "Login / Web", "Κάρτα / IBAN", "Token", "FTP / Hosting", "Email", "License" });
            else _cmbCat.Items.AddRange(new object[] { "Στοιχεία", "IBAN / ΑΦΜ", "Email", "Link", "Χρώμα", "Prompt" });
            _cmbCat.Text = (existing != null && !string.IsNullOrEmpty(existing.Category)) ? existing.Category : _cmbCat.Items[0].ToString();

            int valTop = 68;
            if (isVault)
            {
                Label lUser = new Label { Left = 16, Top = 68, Width = 380, Text = "Username / Email (προαιρετικό — άφησέ το κενό για API Keys):", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
                _txtUser = new TextBox { Left = 16, Top = 88, Width = 408, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Text = existing != null ? existing.Username : "" };
                Controls.AddRange(new Control[] { lUser, _txtUser });
                valTop = 122;
            }

            Label l3 = new Label { Left = 16, Top = valTop, Width = 360, Text = isVault ? "Κωδικός / API Key (DPAPI κρυπτογραφημένο):" : "Κείμενο / Τιμή:", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            if (isVault)
            {
                _txtVal = new TextBox { Left = 16, Top = valTop + 20, Width = 334, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10f), UseSystemPasswordChar = true, Text = existing != null ? existing.TextValue : "" };
                _btnEye = new Button { Left = 354, Top = valTop + 19, Width = 34, Height = 25, FlatStyle = FlatStyle.Flat, BackColor = inp, ForeColor = orange, Text = "◉", Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand, TabStop = false };
                _btnEye.FlatAppearance.BorderColor = Color.FromArgb(60, 64, 76);
                _btnEye.Click += delegate
                {
                    _masked = !_masked;
                    _txtVal.UseSystemPasswordChar = _masked;
                    _btnEye.ForeColor = _masked ? orange : Color.FromArgb(166, 226, 46);
                };
                Button btnGen = new Button { Left = 390, Top = valTop + 19, Width = 34, Height = 25, FlatStyle = FlatStyle.Flat, BackColor = inp, ForeColor = Color.FromArgb(252, 150, 31), Text = "↻", Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand, TabStop = false };
                btnGen.FlatAppearance.BorderColor = Color.FromArgb(60, 64, 76);
                btnGen.Click += delegate
                {
                    _txtVal.Text = ClipStore.GenerateStrongPassword(16);
                    _masked = false;
                    _txtVal.UseSystemPasswordChar = false;
                    _btnEye.ForeColor = Color.FromArgb(166, 226, 46);
                };
                ToolTip gtt = new ToolTip();
                gtt.SetToolTip(_btnEye, "Προβολή / Απόκρυψη");
                gtt.SetToolTip(btnGen, "Δημιουργία ισχυρού τυχαίου κωδικού (16 χαρ.)");
                Controls.AddRange(new Control[] { _btnEye, btnGen });
            }
            else
            {
                _txtVal = new TextBox { Left = 16, Top = valTop + 20, Width = 408, Height = 108, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Text = existing != null ? existing.TextValue : "" };
            }

            int noteY = isVault ? (valTop + 58) : 206;
            Label l4 = new Label { Left = 16, Top = noteY, Width = 380, Text = "Σημείωση (προαιρετική):", ForeColor = muted, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _txtNote = new TextBox { Left = 16, Top = noteY + 20, Width = 408, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f), Text = existing != null ? existing.Note : "" };

            int btnY = ClientSize.Height - 44;
            Button btnOk = new Button { Left = 266, Top = btnY, Width = 158, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = orange, ForeColor = Color.FromArgb(14, 18, 24), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Text = isVault ? "Αποθήκευση (DPAPI)" : "Αποθήκευση", Cursor = Cursors.Hand };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += delegate
            {
                if (string.IsNullOrEmpty(_txtVal.Text))
                {
                    MessageBox.Show(this, "Συμπλήρωσε τον κωδικό / κλειδί.", "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    internal sealed class PromptSecretDialog : Form
    {
        public string SecretText { get { return _txt.Text; } }
        private TextBox _txt;

        public PromptSecretDialog(string title, string prompt, string okLabel, bool allowEmpty, bool dark)
        {
            Text = title;
            ClientSize = new Size(370, 146);
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

            Label lbl = new Label { Left = 16, Top = 14, Width = 338, Height = 34, Text = prompt, ForeColor = muted, Font = new Font("Segoe UI", 8.6f) };
            _txt = new TextBox { Left = 16, Top = 52, Width = 338, BackColor = inp, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10.5f), UseSystemPasswordChar = true };

            Button btnCancel = new Button { Left = 176, Top = 100, Width = 84, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = inp, ForeColor = fg, Font = new Font("Segoe UI", 8.8f), Text = "Άκυρο", DialogResult = DialogResult.Cancel, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 64, 76);

            Button btnOk = new Button { Left = 268, Top = 100, Width = 86, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = orange, ForeColor = Color.FromArgb(14, 18, 24), Font = new Font("Segoe UI", 8.8f, FontStyle.Bold), Text = okLabel, Cursor = Cursors.Hand };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += delegate
            {
                if (!allowEmpty && _txt.Text.Length < 4)
                {
                    MessageBox.Show(this, "Πληκτρολόγησε τουλάχιστον 4 χαρακτήρες.", "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { lbl, _txt, btnCancel, btnOk });
            AcceptButton = btnOk;
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
        public event Action<ClipEntry> CopyUserRequested;      // safe-copy username (Vault)
        public event Action<ClipEntry> PinRequested;
        public event Action<ClipEntry> MoveToVaultRequested;
        public event Action<ClipEntry> EditRequested;
        public event Action<ClipEntry> DeleteRequested;
        public event Action<ClipEntry, Point> ContextMenuRequested;

        private bool _dark = true;
        private bool _hovered = false;
        private int _hoverBtn = -1; // 0, 1, 2, 3 (3=optional Vault User button)
        private readonly bool _hasUserBtn;
        private readonly Rectangle[] _actRects = new Rectangle[4];
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
            _hasUserBtn = (tabKind == 2 && !string.IsNullOrEmpty(entry.Username));
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
            // Compact right-side vector icon buttons
            _actRects[2] = new Rectangle(Width - 10 - bsz, by, bsz, bsz);               // Rightmost: Delete
            _actRects[1] = new Rectangle(Width - 10 - 2 * bsz - gap, by, bsz, bsz);     // Middle: Vault/Edit
            _actRects[0] = new Rectangle(Width - 10 - 3 * bsz - 2 * gap, by, bsz, bsz); // Left: Pin / Copy / Eye (ματάκι!)
            _actRects[3] = _hasUserBtn
                ? new Rectangle(Width - 10 - 4 * bsz - 3 * gap, by, bsz, bsz)           // Optional 4th: Copy Username
                : Rectangle.Empty;
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
            int rightLimit = (_hasUserBtn ? _actRects[3].Left : _actRects[0].Left) - 8;

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
                TextRenderer.DrawText(g, title, FontMain, new Rectangle(textX, 9, textW - 76, 19), fgMain, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, cat, FontMini, new Rectangle(rightLimit - 74, 11, 74, 16), pink, TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string secretDisplay = Entry.Revealed ? (Entry.TextValue ?? "") : Entry.MaskedValue();
                TextRenderer.DrawText(g, secretDisplay, FontMono, new Rectangle(textX, 30, textW, 18), Entry.Revealed ? green : orange, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                string sub = "";
                if (!string.IsNullOrEmpty(Entry.Username) && !string.IsNullOrEmpty(Entry.Note))
                    sub = Entry.Username + "  ·  " + Entry.Note;
                else if (!string.IsNullOrEmpty(Entry.Username))
                    sub = Entry.Username;
                else if (!string.IsNullOrEmpty(Entry.Note))
                    sub = Entry.Note;
                if (sub.Length > 0)
                    TextRenderer.DrawText(g, sub, FontMini, new Rectangle(textX, 48, textW, 14),
                        !string.IsNullOrEmpty(Entry.Username) ? cyan : fgMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
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
            else // Vault: [User] · Eye (ματάκι!) · Edit · Delete (clicking card = Safe Copy!)
            {
                if (_hasUserBtn) DrawVecBtn(g, 3, "user", cyan, cyan);
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
                else if (kind == "user")
                {
                    // Crisp vector User silhouette (copy Username/Email!)
                    g.DrawEllipse(p, cx - 2.6f, cy - 5.5f, 5.2f, 5.2f);
                    g.DrawArc(p, cx - 5.5f, cy + 0.8f, 11f, 8f, 180, 180);
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
            if (_hasUserBtn && _actRects[3].Contains(pt)) return 3;
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
            if (e.Button == MouseButtons.Right)
            {
                if (ContextMenuRequested != null) ContextMenuRequested(Entry, this.PointToScreen(e.Location));
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            int hb = HitBtn(e.Location);
            if (hb == 3) { if (CopyUserRequested != null) CopyUserRequested(Entry); }
            else if (hb == 2) { if (DeleteRequested != null) DeleteRequested(Entry); }
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
    // IMMORTAL HIDDEN MESSAGE HOST (never loses handle when Form toggles TopMost!)
    // ========================================================================
    internal sealed class ClipMessageHost : NativeWindow, IDisposable
    {
        public event Action ClipboardChanged;
        public event Action ToggleRequested;
        public ClipMessageHost() { CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_CLIPBOARDUPDATE && ClipboardChanged != null)
                ClipboardChanged();
            else if ((m.Msg == Native.WM_HOTKEY || m.Msg == Native.WM_TOGGLE_POPUP) && ToggleRequested != null)
                ToggleRequested();
            base.WndProc(ref m);
        }
        public void Dispose() { DestroyHandle(); }
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
        private const string NoClipboardHistory = "ExcludeClipboardContentFromMonitorProcessing";

        private readonly ClipStore _store;
        private readonly bool _smokeMode;
        private ClipMessageHost _msgHost;
        private bool _dark = true;
        private bool _paused = false;
        private bool _autoClearVault = true;
        private int _activeTab = 0; // 0=History, 1=Pinned, 2=Vault
        private int _selectedIdx = 0;
        private bool _allowClose = false;
        private bool _inModal = false;
        private DateTime _vaultUnlockedUntilUtc = DateTime.MinValue;
        private IntPtr _prevForeground = IntPtr.Zero;
        private DateTime _ignoreClipUntilUtc = DateTime.MinValue;
        private uint _vaultClipboardSequence;
        private DateTime _vaultRetryUntil = DateTime.MinValue;
        private System.Windows.Forms.Timer _vaultClearTimer;
        private NotifyIcon _tray;
        private ToolStripMenuItem _miPause, _miAutoStart;
        private Bitmap _logoBmp;
        private IntPtr _kbHook = IntPtr.Zero;
        private Native.LowLevelKeyboardProc _kbHookProc;
        private bool _lWinDown = false, _rWinDown = false, _swallowedV = false;
        private int _lastWinTick = 0;

        private Panel _header, _tabBar, _searchBar, _vaultBar, _footer;
        private Button _btnPause, _btnAdd, _btnTheme, _btnHide;
        private Button _btnVaultPin, _btnVaultPinCfg, _btnVaultExport, _btnVaultImport;
        private readonly Button[] _tabBtns = new Button[3];
        private TextBox _txtSearch;
        private FlowLayoutPanel _listFlow;
        private readonly List<ClipRowCard> _rows = new List<ClipRowCard>();
        private Label _lblStatus, _lblAutoClear, _lblBrand;

        private bool IsVaultLocked { get { return _store.IsLocked; } }

        private void CheckVaultAutoRelock()
        {
            if (_store.HasPin && !_store.IsLocked && DateTime.UtcNow > _vaultUnlockedUntilUtc)
                _store.LockVault();
        }

        private void UpdateDisplayAffinity()
        {
            if (_smokeMode || !IsHandleCreated) return;
            try
            {
                // Anti-screen-capture shield while viewing Vault tab (blocks screen recorders & spyware!)
                Native.SetWindowDisplayAffinity(Handle, _activeTab == 2 ? Native.WDA_EXCLUDEFROMCAPTURE : Native.WDA_NONE);
            }
            catch { }
        }

        public ClipPopupForm(string customDir, bool smokeMode)
        {
            _smokeMode = smokeMode;
            _dark = ReadSuiteDark(true);
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
            if (!File.Exists(icoPath))
            {
                DirectoryInfo par = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
                if (par != null) { string alt = Path.Combine(par.FullName, "NillyClipPro.ico"); if (File.Exists(alt)) icoPath = alt; }
            }
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
                _msgHost = new ClipMessageHost();
                _msgHost.ClipboardChanged += delegate
                {
                    if (!_paused && DateTime.UtcNow >= _ignoreClipUntilUtc) CaptureClipboardNow();
                };
                _msgHost.ToggleRequested += delegate
                {
                    Native.SuppressWinStartMenu();
                    if (Visible) HideAndMask();
                    else ShowPopup();
                };
                Native.AddClipboardFormatListener(_msgHost.Handle);
                Native.RegisterHotKey(_msgHost.Handle, HK_CTRL_SHIFT_V, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_V);
                Native.RegisterHotKey(_msgHost.Handle, HK_CTRL_ALT_V, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, Native.VK_V);

                Native.IsKeyDown(Native.VK_LWIN);
                _kbHookProc = new Native.LowLevelKeyboardProc(KeyboardHookCallback);
                _kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _kbHookProc, IntPtr.Zero, 0);

                BuildTrayIcon();
                Deactivate += delegate { if (Visible && !_inModal) HideAndMask(); };
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetDarkTitlebar(Handle, _dark);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool isDown = (msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN);
                bool isUp = (msg == Native.WM_KEYUP || msg == Native.WM_SYSKEYUP);
                if (isDown || isUp)
                {
                    int vk = Marshal.ReadInt32(lParam);
                    if (vk == Native.VK_LWIN) { _lWinDown = isDown; if (isDown) _lastWinTick = Environment.TickCount; }
                    else if (vk == Native.VK_RWIN) { _rWinDown = isDown; if (isDown) _lastWinTick = Environment.TickCount; }
                    else if (vk == (int)Native.VK_V)
                    {
                        if (isUp)
                        {
                            if (_swallowedV) { _swallowedV = false; return (IntPtr)1; }
                        }
                        else if (_swallowedV)
                        {
                            return (IntPtr)1;
                        }
                        else
                        {
                            int now = Environment.TickCount;
                            bool win = Native.IsKeyDown(Native.VK_LWIN) || Native.IsKeyDown(Native.VK_RWIN) ||
                                ((_lWinDown || _rWinDown) && unchecked(now - _lastWinTick) < 2500);
                            if (win)
                            {
                                bool ctrl = Native.IsKeyDown(Native.VK_CONTROL);
                                bool alt = Native.IsKeyDown(Native.VK_MENU);
                                bool shift = Native.IsKeyDown(Native.VK_SHIFT);
                                if (!ctrl && !alt && !shift)
                                {
                                    _swallowedV = true;
                                    if (_msgHost != null)
                                        Native.PostMessage(_msgHost.Handle, Native.WM_TOGGLE_POPUP, IntPtr.Zero, IntPtr.Zero);
                                    return (IntPtr)1; // Consume Win+V before Windows TextInputHost opens!
                                }
                            }
                        }
                    }
                }
            }
            return Native.CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private void BuildUi()
        {
            // 1. Minimal Top Header
            _header = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 10, 6) };
            PictureBox pic = new PictureBox { Left = 12, Top = 9, Width = 26, Height = 26, Image = _logoBmp, SizeMode = PictureBoxSizeMode.Zoom };
            Label lblTitle = new Label { Left = 44, Top = 10, Width = 140, Height = 24, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Text = "NillyClip Pro", UseMnemonic = false };

            FlowLayoutPanel rFlow = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 216, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            _btnHide = MkBtn("×", 28, delegate { HideAndMask(); });
            _btnTheme = MkBtn("◐", 28, delegate { _dark = !_dark; ApplyTheme(); });
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

            // 3b. Minimal Vault Action Strip (visible ONLY on Κλειδοθήκη tab!)
            _vaultBar = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(12, 2, 12, 4), Visible = false };
            _btnVaultPin = MkBtn("PIN: OFF", 92, delegate { OnVaultPinClick(); });
            _btnVaultPin.Left = 12; _btnVaultPin.Top = 2; _btnVaultPin.Height = 24;
            _btnVaultPin.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);

            _btnVaultPinCfg = MkBtn("Αλλαγή PIN", 84, delegate { OnVaultPinConfig(); });
            _btnVaultPinCfg.Left = 110; _btnVaultPinCfg.Top = 2; _btnVaultPinCfg.Height = 24;
            _btnVaultPinCfg.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
            _btnVaultPinCfg.Visible = false;

            FlowLayoutPanel vRight = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 194, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            _btnVaultImport = MkBtn("↓ Επαναφορά", 94, delegate { OnVaultImport(); });
            _btnVaultImport.Height = 24; _btnVaultImport.Margin = new Padding(5, 0, 0, 0);
            _btnVaultImport.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);

            _btnVaultExport = MkBtn("↑ Backup", 84, delegate { OnVaultExport(); });
            _btnVaultExport.Height = 24; _btnVaultExport.Margin = new Padding(0, 0, 0, 0);
            _btnVaultExport.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
            vRight.Controls.AddRange(new Control[] { _btnVaultImport, _btnVaultExport });

            tt.SetToolTip(_btnVaultPin, "Προαιρετικό PIN κλείδωμα Κλειδοθήκης (AES-256 + HMAC-SHA256 + DPAPI)");
            tt.SetToolTip(_btnVaultPinCfg, "Αλλαγή ή απενεργοποίηση PIN");
            tt.SetToolTip(_btnVaultExport, "Κρυπτογραφημένο Backup Κλειδοθήκης (AES-256 + HMAC .nvault)");
            tt.SetToolTip(_btnVaultImport, "Επαναφορά Κλειδοθήκης από αρχείο .nvault");
            _vaultBar.Controls.AddRange(new Control[] { vRight, _btnVaultPin, _btnVaultPinCfg });

            // 4. Bottom Footer
            _footer = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(10, 0, 10, 0) };
            _lblStatus = new Label { Dock = DockStyle.Left, Width = 142, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 7.8f), UseMnemonic = false, Text = "Win+V · Ενεργό" };
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
            Controls.Add(_vaultBar);
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
            menu.Items.Add("Άνοιγμα NillyClip Pro  (Win+V / Ctrl+Shift+V)", null, delegate { ShowPopup(); });
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
            _tray.Text = "NillyClip Pro (Win+V / Ctrl+Shift+V)";
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
            CheckVaultAutoRelock();
            _activeTab = idx;
            _selectedIdx = 0;
            UpdateDisplayAffinity();
            StyleTabs();
            UpdateVaultBar();
            RebuildList();
        }

        private void UpdateVaultBar()
        {
            if (_vaultBar == null) return;
            _vaultBar.Visible = (_activeTab == 2);
            if (_activeTab != 2) return;

            bool hasPin = _store.HasPin;
            bool locked = _store.IsLocked;
            _btnVaultPin.Text = !hasPin ? "PIN: OFF" : (locked ? "Κλειδωμένο" : "Κλείδωμα");
            _btnVaultPinCfg.Visible = hasPin && !locked;
            _btnVaultExport.Enabled = !locked && _store.Vault.Count > 0;
            _btnVaultImport.Enabled = !locked;
            _txtSearch.Enabled = !locked;

            Color orange = Color.FromArgb(252, 150, 31);
            Color green = Color.FromArgb(166, 226, 46);
            Color pink = Color.FromArgb(249, 38, 114);
            Color btnBg = _dark ? Color.FromArgb(36, 39, 46) : Color.FromArgb(236, 240, 246);
            Color border = _dark ? Color.FromArgb(54, 58, 68) : Color.FromArgb(214, 220, 228);
            Color fg = _dark ? Color.FromArgb(228, 232, 240) : Color.FromArgb(25, 28, 34);

            _btnVaultPin.BackColor = btnBg;
            _btnVaultPin.ForeColor = !hasPin ? orange : (locked ? pink : green);
            _btnVaultPin.FlatAppearance.BorderColor = !hasPin ? orange : (locked ? pink : green);

            foreach (Button b in new Button[] { _btnVaultPinCfg, _btnVaultExport, _btnVaultImport })
            { b.BackColor = btnBg; b.ForeColor = b.Enabled ? fg : Color.Gray; b.FlatAppearance.BorderColor = border; }
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
            _header.BackColor = top; _tabBar.BackColor = top; _searchBar.BackColor = top; _vaultBar.BackColor = top; _footer.BackColor = top;
            foreach (Control c in _header.Controls) if (c is Label) c.ForeColor = fg;
            _txtSearch.BackColor = _dark ? Color.FromArgb(22, 24, 29) : Color.White;
            _txtSearch.ForeColor = fg;
            _lblStatus.ForeColor = muted;
            _lblBrand.ForeColor = orange;
            _btnTheme.Text = "◐";

            foreach (Button b in new Button[] { _btnPause, _btnTheme, _btnHide })
            { b.BackColor = btnBg; b.ForeColor = fg; b.FlatAppearance.BorderColor = border; }
            _btnAdd.BackColor = orange; _btnAdd.ForeColor = Color.FromArgb(14, 18, 24); _btnAdd.FlatAppearance.BorderColor = orange;
            UpdateAutoClearLabel();
            StyleTabs();
            UpdateVaultBar();
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

        private static bool ReadSuiteDark(bool fallback)
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\suite.theme");
                if (File.Exists(p))
                {
                    string s = File.ReadAllText(p, Encoding.ASCII).Trim();
                    if (s.Equals("light", StringComparison.OrdinalIgnoreCase)) return false;
                    if (s.Equals("dark", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return fallback;
        }

        private static string FindSiblingExe(string folder, string exeName)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\" + folder + @"\" + exeName);
                if (File.Exists(appData)) return appData;
                DirectoryInfo par = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
                if (par != null)
                {
                    string sib = Path.Combine(Path.Combine(par.FullName, folder), exeName);
                    if (File.Exists(sib)) return sib;
                }
            }
            catch { }
            return null;
        }

        private static string TryExtractHex(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string s = text.Trim();
            if (s.Length == 7 || s.Length == 4)
            {
                if (s[0] == '#')
                {
                    bool ok = true;
                    for (int i = 1; i < s.Length; i++)
                    {
                        char c = s[i];
                        if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) { ok = false; break; }
                    }
                    if (ok) return s;
                }
            }
            return null;
        }

        private void OnCardContextMenu(ClipEntry entry, Point screenPt)
        {
            if (entry == null || _activeTab == 2) return; // Never dump Vault secrets to temp files
            ContextMenuStrip cm = new ContextMenuStrip();
            if (entry.Kind == ClipKind.Text && !string.IsNullOrEmpty(entry.TextValue))
            {
                string noteExe = FindSiblingExe("NillyNotePro", "NillyNotePro.exe");
                if (noteExe != null)
                {
                    cm.Items.Add("↗ Άνοιγμα σε NillyNote Pro", null, delegate
                    {
                        try
                        {
                            string tmp = Path.Combine(Path.GetTempPath(), "NillyClip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                            File.WriteAllText(tmp, entry.TextValue, new UTF8Encoding(false));
                            Process.Start(new ProcessStartInfo(noteExe, "\"" + tmp + "\"") { WorkingDirectory = Path.GetDirectoryName(noteExe), UseShellExecute = true });
                        }
                        catch { }
                    });
                }
                string codeExe = FindSiblingExe("NillyCodePro", "NillyCodePro.exe");
                if (codeExe != null)
                {
                    cm.Items.Add("</> Επεξεργασία σε NillyCode Pro", null, delegate
                    {
                        try
                        {
                            string tmp = Path.Combine(Path.GetTempPath(), "NillyClip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                            File.WriteAllText(tmp, entry.TextValue, new UTF8Encoding(false));
                            Process.Start(new ProcessStartInfo(codeExe, "\"" + tmp + "\"") { WorkingDirectory = Path.GetDirectoryName(codeExe), UseShellExecute = true });
                        }
                        catch { }
                    });
                }
                string hex = TryExtractHex(entry.TextValue);
                if (hex != null)
                {
                    string calcExe = FindSiblingExe("NillyCalcPro", "NillyCalcPro.exe");
                    if (calcExe != null)
                    {
                        cm.Items.Add("Ανάλυση Χρώματος (" + hex + ") σε NillyCalc Pro", null, delegate
                        {
                            try { Process.Start(new ProcessStartInfo(calcExe, "--hex \"" + hex + "\"") { WorkingDirectory = Path.GetDirectoryName(calcExe), UseShellExecute = true }); } catch { }
                        });
                    }
                }
            }
            else if (entry.Kind == ClipKind.Image && !string.IsNullOrEmpty(entry.ImageFile))
            {
                string imgPath = Path.Combine(_store.ImgDir, entry.ImageFile);
                string viewExe = FindSiblingExe("NillyViewPro", "NillyViewPro.exe");
                if (viewExe != null && File.Exists(imgPath))
                {
                    cm.Items.Add("Προβολή / Επεξεργασία σε NillyView Pro", null, delegate
                    {
                        try { Process.Start(new ProcessStartInfo(viewExe, "\"" + imgPath + "\"") { WorkingDirectory = Path.GetDirectoryName(viewExe), UseShellExecute = true }); } catch { }
                    });
                }
            }
            if (cm.Items.Count > 0) cm.Show(screenPt);
            else cm.Dispose();
        }

        private List<ClipEntry> CurrentSourceList()
        {
            return _activeTab == 0 ? _store.History : (_activeTab == 1 ? _store.Pinned : _store.Vault);
        }

        public void RebuildList()
        {
            CheckVaultAutoRelock();
            _listFlow.SuspendLayout();
            for (int i = 0; i < _rows.Count; i++) _rows[i].Dispose();
            _rows.Clear();
            _listFlow.Controls.Clear();

            int rowW = Math.Max(300, _listFlow.ClientSize.Width - 28);
            _txtSearch.Enabled = !(_activeTab == 2 && IsVaultLocked);
            UpdateVaultBar();

            if (_activeTab == 2 && IsVaultLocked)
            {
                BuildInlinePinLockCard(rowW);
                _listFlow.ResumeLayout();
                _tabBtns[0].Text = "Ιστορικό (" + _store.History.Count + ")";
                _tabBtns[1].Text = "Καρφιτσωμένα (" + _store.Pinned.Count + ")";
                _tabBtns[2].Text = "Κλειδοθήκη [PIN]";
                _lblStatus.Text = "AES-256 + DPAPI · Κλειδωμένο";
                return;
            }

            List<ClipEntry> src = CurrentSourceList();
            string q = (_txtSearch.Text ?? "").Trim();
            int num = 0;

            for (int i = 0; i < src.Count; i++)
            {
                ClipEntry e = src[i];
                if (q.Length > 0)
                {
                    bool hit = (e.Title != null && e.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (e.Category != null && e.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (e.Username != null && e.Username.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
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
                card.CopyUserRequested += OnCopyUsername;
                card.PinRequested += OnPinFromHistory;
                card.MoveToVaultRequested += OnSaveToVaultFromHistory;
                card.ContextMenuRequested += OnCardContextMenu;
                card.EditRequested += OnEditEntry;
                card.DeleteRequested += OnDeleteEntry;
                _rows.Add(card);
                _listFlow.Controls.Add(card);
            }

            if (_rows.Count == 0)
            {
                Label empty = new Label
                {
                    Width = rowW, Height = 96, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = _dark ? Color.FromArgb(130, 138, 152) : Color.Gray,
                    Font = new Font("Segoe UI", 9.1f),
                    Text = _activeTab == 0
                        ? "Το ιστορικό είναι άδειο.\r\nΑντέγραψε κείμενο, HEX χρώμα ή screenshot με Ctrl+C!"
                        : (_activeTab == 1
                            ? "Κανένα καρφιτσωμένο snippet.\r\nΠάτα «+ Νέο» ή το pin από το Ιστορικό (ΑΦΜ, IBAN, Emails, Links)."
                            : "Η Κλειδοθήκη είναι άδεια.\r\nΠάτα «+ Νέο» για API Keys ή Κωδικούς — και βάλε PIN για διπλή AES-256 + DPAPI προστασία!")
                };
                _listFlow.Controls.Add(empty);
            }

            _listFlow.ResumeLayout();
            _tabBtns[0].Text = "Ιστορικό (" + _store.History.Count + ")";
            _tabBtns[1].Text = "Καρφιτσωμένα (" + _store.Pinned.Count + ")";
            _tabBtns[2].Text = "Κλειδοθήκη (" + _store.Vault.Count + ")";
            _lblStatus.Text = _paused ? "Παύση καταγραφής" : "Win+V · Ενεργό";
        }

        private void BuildInlinePinLockCard(int rowW)
        {
            Color cardBg = _dark ? Color.FromArgb(29, 31, 38) : Color.White;
            Color inpBg = _dark ? Color.FromArgb(20, 22, 27) : Color.FromArgb(245, 247, 250);
            Color fg = _dark ? Color.FromArgb(242, 244, 248) : Color.FromArgb(22, 24, 30);
            Color muted = _dark ? Color.FromArgb(148, 156, 170) : Color.FromArgb(95, 105, 118);
            Color orange = Color.FromArgb(252, 150, 31);
            Color pink = Color.FromArgb(249, 38, 114);

            Panel p = new Panel { Width = rowW, Height = 192, BackColor = cardBg, Margin = new Padding(0, 12, 0, 6) };
            Label l1 = new Label { Left = 16, Top = 20, Width = rowW - 32, Height = 24, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = fg, Text = "Κλειδωμένη Κλειδοθήκη" };
            Label l2 = new Label { Left = 16, Top = 46, Width = rowW - 32, Height = 22, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.2f), ForeColor = muted, Text = "AES-256 + HMAC-SHA256 + DPAPI · Μηδέν μυστικά στη RAM" };

            TextBox txtPin = new TextBox { Left = (rowW - 210) / 2, Top = 82, Width = 210, BackColor = inpBg, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 11f), UseSystemPasswordChar = true, TextAlign = HorizontalAlignment.Center };
            Label lblErr = new Label { Left = 16, Top = 152, Width = rowW - 32, Height = 20, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.2f, FontStyle.Bold), ForeColor = pink, Text = "" };
            Button btnUnlock = new Button { Left = (rowW - 140) / 2, Top = 116, Width = 140, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = orange, ForeColor = Color.FromArgb(14, 18, 24), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Text = "Ξεκλείδωμα", Cursor = Cursors.Hand };
            btnUnlock.FlatAppearance.BorderSize = 0;

            Action doUnlock = delegate
            {
                string err;
                if (_store.UnlockVault(txtPin.Text, out err))
                {
                    txtPin.Text = "";
                    _vaultUnlockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                    RebuildList();
                    _lblStatus.Text = "✓ Ξεκλειδώθηκε (5 λ.)";
                }
                else
                {
                    txtPin.Text = "";
                    lblErr.Text = err ?? "Λάθος PIN.";
                    txtPin.Focus();
                }
            };
            btnUnlock.Click += delegate { doUnlock(); };
            txtPin.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.KeyCode == Keys.Enter) { ke.SuppressKeyPress = true; doUnlock(); } };

            p.Controls.AddRange(new Control[] { l1, l2, txtPin, btnUnlock, lblErr });
            _listFlow.Controls.Add(p);
            if (Visible) BeginInvoke((Action)delegate { try { txtPin.Focus(); } catch { } });
        }

        private void ResizeRowWidths()
        {
            int w = Math.Max(300, _listFlow.ClientSize.Width - 28);
            for (int i = 0; i < _rows.Count; i++) _rows[i].Width = w;
        }

        // ====================================================================
        // CLIPBOARD CAPTURE (0% CPU EVENT-DRIVEN via ClipMessageHost)
        // ====================================================================
        private void CaptureClipboardNow()
        {
            try
            {
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

            CheckVaultAutoRelock();
            RebuildList();
            Show();
            UpdateDisplayAffinity();
            WindowState = FormWindowState.Normal;
            Native.SetForegroundWindow(Handle);
            Activate();
            if (_txtSearch.Enabled) _txtSearch.Focus();
        }

        private void HideAndMask()
        {
            // Security: ALWAYS re-mask every revealed Vault eye when popup closes!
            for (int i = 0; i < _store.Vault.Count; i++) _store.Vault[i].Revealed = false;
            _swallowedV = false;
            _lWinDown = _rWinDown = false;
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
            if (entry == null || string.IsNullOrEmpty(entry.TextValue)) return;
            _ignoreClipUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
            try
            {
                using (MemoryStream mExcl = new MemoryStream(BitConverter.GetBytes(1)))
                using (MemoryStream mHist = new MemoryStream(BitConverter.GetBytes(0)))
                using (MemoryStream mCloud = new MemoryStream(BitConverter.GetBytes(0)))
                {
                    DataObject data = new DataObject();
                    data.SetText(entry.TextValue, TextDataFormat.UnicodeText);
                    data.SetData(NoClipboardHistory, false, mExcl);
                    data.SetData("CanIncludeInClipboardHistory", false, mHist);
                    data.SetData("CanUploadToCloudClipboard", false, mCloud);
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
                _lblStatus.Text = "✓ Αντιγράφηκε με ασφάλεια (Cloud-Block · 30s).";
            }
            catch (ExternalException)
            {
                _lblStatus.Text = "Το πρόχειρο είναι απασχολημένο. Δοκίμασε ξανά.";
            }
        }

        private static void SetClipboardSafeText(string val, bool isSecret)
        {
            if (!isSecret)
            {
                Clipboard.SetText(val);
                return;
            }
            using (MemoryStream mExcl = new MemoryStream(BitConverter.GetBytes(1)))
            using (MemoryStream mHist = new MemoryStream(BitConverter.GetBytes(0)))
            using (MemoryStream mCloud = new MemoryStream(BitConverter.GetBytes(0)))
            {
                DataObject data = new DataObject();
                data.SetText(val, TextDataFormat.UnicodeText);
                data.SetData(NoClipboardHistory, false, mExcl);
                data.SetData("CanIncludeInClipboardHistory", false, mHist);
                data.SetData("CanUploadToCloudClipboard", false, mCloud);
                Clipboard.SetDataObject(data, true, 3, 100);
            }
        }

        private void OnPasteEntry(ClipEntry entry, bool plainOnly)
        {
            if (entry == null) return;
            if (_activeTab == 2)
            {
                // Security: Vault secrets are safe-copied with Cloud-Block + 30s Auto-Clear (user pastes manually into trusted target!)
                CopyVaultEntry(entry);
                if (!_smokeMode) HideAndMask();
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
                    SetClipboardSafeText(val, false);
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

        private void OnCopyUsername(ClipEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Username)) return;
            _ignoreClipUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
            try { SetClipboardSafeText(entry.Username, true); } catch { }
            _lblStatus.Text = "✓ Αντιγράφηκε το Username";
            if (_smokeMode) return;
            HideAndMask();
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

        private void OnVaultPinClick()
        {
            if (!_store.HasPin)
            {
                bool prevTop = TopMost; TopMost = false; _inModal = true;
                try
                {
                    using (PromptSecretDialog dlg = new PromptSecretDialog(
                        "Ορισμός PIN Κλειδοθήκης",
                        "Βάλε ένα PIN ή κωδικό (4+ χαρ.) για διπλή κρυπτογράφηση AES-256 + HMAC-SHA256 + DPAPI:",
                        "Ενεργοποίηση", false, _dark))
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            _store.SetPin(dlg.SecretText);
                            _vaultUnlockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                            RebuildList();
                            _lblStatus.Text = "✓ PIN Ενεργό (AES-256 + DPAPI)";
                        }
                    }
                }
                finally { _inModal = false; TopMost = prevTop; }
            }
            else if (!_store.IsLocked)
            {
                _store.LockVault();
                _vaultUnlockedUntilUtc = DateTime.MinValue;
                try { GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false); } catch { }
                RebuildList();
                _lblStatus.Text = "Κλειδώθηκε · RAM καθαρή";
            }
        }

        private void OnVaultPinConfig()
        {
            if (!_store.HasPin || _store.IsLocked) return;
            bool prevTop = TopMost; TopMost = false; _inModal = true;
            try
            {
                using (PromptSecretDialog dlg = new PromptSecretDialog(
                    "Αλλαγή ή Αφαίρεση PIN",
                    "Γράψε νέο PIN (4+ χαρ.) ή άφησέ το ΚΕΝΟ για απενεργοποίηση του PIN:",
                    "Αποθήκευση", true, _dark))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        string np = dlg.SecretText;
                        if (np.Length > 0 && np.Length < 4)
                        {
                            MessageBox.Show(this, "Το νέο PIN πρέπει να έχει τουλάχιστον 4 χαρακτήρες (ή κενό για αφαίρεση).", "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        _store.SetPin(np);
                        if (!string.IsNullOrEmpty(np)) _vaultUnlockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                        RebuildList();
                        _lblStatus.Text = string.IsNullOrEmpty(np) ? "PIN απενεργοποιήθηκε" : "✓ Το PIN ενημερώθηκε";
                    }
                }
            }
            finally { _inModal = false; TopMost = prevTop; }
        }

        private void OnVaultExport()
        {
            if (_store.IsLocked || _store.Vault.Count == 0) return;
            bool prevTop = TopMost; TopMost = false; _inModal = true;
            try
            {
                using (PromptSecretDialog pd = new PromptSecretDialog(
                    "Κρυπτογραφημένο Backup (.nvault)",
                    "Βάλε κωδικό προστασίας για το backup (AES-256 + HMAC-SHA256, 100k PBKDF2):",
                    "Συνέχεια", false, _dark))
                {
                    if (pd.ShowDialog(this) != DialogResult.OK) return;
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Title = "Αποθήκευση Κρυπτογραφημένου Backup Κλειδοθήκης";
                        sfd.Filter = "NillyVault Backup (*.nvault)|*.nvault";
                        sfd.FileName = "NillyVault_" + DateTime.Now.ToString("yyyy-MM-dd") + ".nvault";
                        if (sfd.ShowDialog(this) == DialogResult.OK)
                        {
                            _store.ExportVaultBackup(sfd.FileName, pd.SecretText);
                            _lblStatus.Text = "✓ Εξαγωγή .nvault (AES-256)";
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _inModal = false; TopMost = prevTop; }
        }

        private void OnVaultImport()
        {
            if (_store.IsLocked) return;
            bool prevTop = TopMost; TopMost = false; _inModal = true;
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Επιλογή Κρυπτογραφημένου Backup (.nvault)";
                    ofd.Filter = "NillyVault Backup (*.nvault)|*.nvault";
                    if (ofd.ShowDialog(this) != DialogResult.OK) return;
                    using (PromptSecretDialog pd = new PromptSecretDialog(
                        "Επαναφορά Backup (.nvault)",
                        "Εισάγετε τον κωδικό προστασίας του backup αρχείου:",
                        "Επαναφορά", false, _dark))
                    {
                        if (pd.ShowDialog(this) != DialogResult.OK) return;
                        int added = _store.ImportVaultBackup(ofd.FileName, pd.SecretText);
                        RebuildList();
                        _lblStatus.Text = "✓ Εισήχθησαν " + added + " εγγραφές";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "NillyClip Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { _inModal = false; TopMost = prevTop; }
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
            if (_store.IsLocked) { SwitchTab(2); _lblStatus.Text = "Ξεκλείδωσε πρώτα την Κλειδοθήκη."; return; }
            ClipEntry draft = new ClipEntry { Title = "API Key", Category = "API Key", TextValue = e.TextValue };
            bool prevDeact = TopMost;
            TopMost = false; _inModal = true;
            try
            {
                using (EntryEditorDialog dlg = new EntryEditorDialog(true, draft, _dark))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        draft.Title = dlg.EntryTitle;
                        draft.Category = dlg.EntryCategory;
                        draft.Username = dlg.EntryUsername;
                        draft.TextValue = dlg.EntryValue;
                        draft.Note = dlg.EntryNote;
                        _store.Vault.Insert(0, draft);
                        _store.SaveVaultEncrypted();
                        // Remove the secret from plain clipboard history for safety!
                        _store.History.Remove(e);
                        _store.SaveHistory();
                        SwitchTab(2);
                        _lblStatus.Text = "✓ Κρυπτογραφήθηκε!";
                    }
                }
            }
            finally { _inModal = false; TopMost = prevDeact; }
        }

        private void OpenAddEntryDialog()
        {
            bool isVault = (_activeTab == 2);
            if (isVault && _store.IsLocked) { _lblStatus.Text = "Ξεκλείδωσε πρώτα την Κλειδοθήκη."; return; }
            bool prevTop = TopMost;
            TopMost = false; _inModal = true;
            try
            {
                using (EntryEditorDialog dlg = new EntryEditorDialog(isVault, null, _dark))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ClipEntry ne = new ClipEntry();
                        ne.Title = dlg.EntryTitle;
                        ne.Category = dlg.EntryCategory;
                        ne.Username = isVault ? dlg.EntryUsername : "";
                        ne.TextValue = dlg.EntryValue;
                        ne.Note = dlg.EntryNote;
                        Color c;
                        if (!isVault && ClipDetector.TryDetectColor(ne.TextValue, out c)) { ne.Kind = ClipKind.ColorHex; ne.ParsedColor = c; }
                        else if (!isVault && ClipDetector.IsUrl(ne.TextValue)) ne.Kind = ClipKind.Url;
                        else ne.Kind = ClipKind.Text;

                        if (isVault) { _store.Vault.Insert(0, ne); _store.SaveVaultEncrypted(); }
                        else { _store.Pinned.Insert(0, ne); _store.SavePinned(); if (_activeTab == 0) _activeTab = 1; }
                        StyleTabs();
                        RebuildList();
                    }
                }
            }
            finally { _inModal = false; TopMost = prevTop; }
        }

        private void OnEditEntry(ClipEntry e)
        {
            if (e == null) return;
            bool isVault = (_activeTab == 2);
            if (isVault && _store.IsLocked) return;
            bool prevTop = TopMost;
            TopMost = false; _inModal = true;
            try
            {
                using (EntryEditorDialog dlg = new EntryEditorDialog(isVault, e, _dark))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        e.Title = dlg.EntryTitle;
                        e.Category = dlg.EntryCategory;
                        if (isVault) e.Username = dlg.EntryUsername;
                        e.TextValue = dlg.EntryValue;
                        e.Note = dlg.EntryNote;
                        if (isVault) _store.SaveVaultEncrypted(); else _store.SavePinned();
                        RebuildList();
                    }
                }
            }
            finally { _inModal = false; TopMost = prevTop; }
        }

        private void OnDeleteEntry(ClipEntry e)
        {
            if (e == null) return;
            if (_activeTab == 0) { _store.History.Remove(e); _store.SaveHistory(); }
            else if (_activeTab == 1) { _store.Pinned.Remove(e); _store.SavePinned(); }
            else { _store.Vault.Remove(e); _store.SaveVaultEncrypted(); }
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
            _lblStatus.Text = _paused ? "Παύση καταγραφής" : "Win+V · Ενεργό";
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
                    if (_kbHook != IntPtr.Zero) { Native.UnhookWindowsHookEx(_kbHook); _kbHook = IntPtr.Zero; }
                    if (_msgHost != null)
                    {
                        Native.RemoveClipboardFormatListener(_msgHost.Handle);
                        Native.UnregisterHotKey(_msgHost.Handle, HK_CTRL_SHIFT_V);
                        Native.UnregisterHotKey(_msgHost.Handle, HK_CTRL_ALT_V);
                        _msgHost.Dispose();
                        _msgHost = null;
                    }
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
                Native.TrimMemory();
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
                // 1. Test Double-Layer Encryption (AES-256 + HMAC-SHA256 + DPAPI), Zero-RAM-While-Locked, Generator & .nvault Backup!
                string secret = "sk-proj-NillySecretKey9876543210XYZ";
                ClipStore st = new ClipStore(tmpDir);
                st.Vault.Add(new ClipEntry { Title = "OpenAI API Key", Category = "API Key", TextValue = secret, Note = "ChatGPT Work" });
                st.Vault.Add(new ClipEntry { Title = "GitHub (nillynp)", Category = "Login / Web", Username = "sotiris@nillygraphics.com", TextValue = "ghp_nillynp_SuperSecretToken9999", Note = "github.com/nillynp" });
                st.Pinned.Add(new ClipEntry { Title = "Website", Category = "Link", Kind = ClipKind.Url, TextValue = "https://nillygraphics.com/" });
                st.Pinned.Add(new ClipEntry { Title = "Brand Orange", Category = "Χρώμα", Kind = ClipKind.ColorHex, TextValue = "#FC961F", ParsedColor = Color.FromArgb(252, 150, 31) });
                st.History.Add(new ClipEntry { Kind = ClipKind.ColorHex, TextValue = "#66D9EF", ParsedColor = Color.FromArgb(102, 217, 239) });
                st.History.Add(new ClipEntry { Kind = ClipKind.Text, TextValue = "Καλημέρα — NillyClip Pro Δοκιμή Ελληνικών" });
                st.SaveHistory();
                st.SavePinned();

                // Activate PIN -> upgrades vault.dpapi to Double-Layer (NVL2 AES-256+HMAC inside DPAPI)!
                st.SetPin("Nilly#2026");
                byte[] rawCipher = File.ReadAllBytes(st.VaultPath);
                string rawAscii = Encoding.UTF8.GetString(rawCipher);
                bool zeroPlaintextOnDisk = !rawAscii.Contains("NillySecretKey") && !rawAscii.Contains("SuperSecretToken") && !rawAscii.Contains("sotiris@nillygraphics.com");

                // Verify even raw DPAPI Unprotect without PIN still yields NVL2 AES-256+HMAC ciphertext (NOT plaintext!)
                byte[] dpapiLayer = ProtectedData.Unprotect(rawCipher, Encoding.UTF8.GetBytes("NillyClipPro::DPAPI::Vault::v1::NillyGraphics"), DataProtectionScope.CurrentUser);
                bool doubleLayerOk = dpapiLayer.Length >= 84 && dpapiLayer[0] == 'N' && dpapiLayer[1] == 'V' && dpapiLayer[2] == 'L' && dpapiLayer[3] == '2'
                                     && !Encoding.UTF8.GetString(dpapiLayer).Contains("NillySecretKey");

                // Reload in fresh store: while locked, RAM Vault MUST be empty (0 entries)!
                ClipStore st2 = new ClipStore(tmpDir);
                st2.LoadAll();
                bool zeroRamWhileLocked = st2.IsLocked && st2.Vault.Count == 0;
                string err;
                bool wrongPinBlocked = !st2.UnlockVault("wrongpin", out err);
                bool unlockOk = st2.UnlockVault("Nilly#2026", out err) && st2.Vault.Count == 2
                                && st2.Vault[0].TextValue == secret && st2.Vault[1].Username == "sotiris@nillygraphics.com";
                bool maskOk = unlockOk && st2.Vault[0].MaskedValue().Contains("••••") && !st2.Vault[0].MaskedValue().Contains("NillySecretKey");

                // Verify LockVault purges RAM
                st2.LockVault();
                bool ramPurgedOnLock = st2.IsLocked && st2.Vault.Count == 0;

                // Verify Strong Password Generator
                string genPwd = ClipStore.GenerateStrongPassword(16);
                bool genOk = !string.IsNullOrEmpty(genPwd) && genPwd.Length == 16;

                // Verify AES-256 + HMAC-SHA256 .nvault Backup Export, Tamper Detection & Import
                string bkPath = Path.Combine(tmpDir, "test.nvault");
                st.ExportVaultBackup(bkPath, "BackupPass!99");
                byte[] bkBytes = File.ReadAllBytes(bkPath);
                bool bkZeroPlain = bkBytes.Length >= 84 && bkBytes[0] == 'N' && bkBytes[1] == 'V' && bkBytes[2] == 'L' && bkBytes[3] == '2'
                                   && !Encoding.UTF8.GetString(bkBytes).Contains("NillySecretKey");
                // Test tamper detection (flip 1 bit in ciphertext!)
                string tamperPath = Path.Combine(tmpDir, "tamper.nvault");
                byte[] tampered = (byte[])bkBytes.Clone();
                tampered[tampered.Length - 1] ^= 0x01;
                File.WriteAllBytes(tamperPath, tampered);
                bool tamperBlocked = false;
                try { new ClipStore(Path.Combine(tmpDir, "t1")).ImportVaultBackup(tamperPath, "BackupPass!99"); }
                catch (CryptographicException) { tamperBlocked = true; }
                ClipStore stImp = new ClipStore(Path.Combine(tmpDir, "imp"));
                int impCount = stImp.ImportVaultBackup(bkPath, "BackupPass!99");
                bool backupRoundTripOk = (impCount == 2 && stImp.Vault[1].Username == "sotiris@nillygraphics.com");

                // Remove PIN for unlocked UI preview capture (shows cards + User icon + Eye + Vault bar cleanly!)
                st.SetPin("");

                Color c;
                bool hexOk = ClipDetector.TryDetectColor("#FC961F", out c) && c.R == 252 && c.G == 150 && c.B == 31;

                sb.AppendLine("DPAPI_ZERO_PLAINTEXT_ON_DISK=" + zeroPlaintextOnDisk);
                sb.AppendLine("DPAPI_DECRYPT_OK=" + unlockOk);
                sb.AppendLine("DOUBLE_LAYER_AES256_HMAC_OK=" + doubleLayerOk);
                sb.AppendLine("ZERO_RAM_WHILE_LOCKED=" + zeroRamWhileLocked);
                sb.AppendLine("WRONG_PIN_BLOCKED=" + wrongPinBlocked);
                sb.AppendLine("PIN_UNLOCK_AND_USER_OK=" + unlockOk);
                sb.AppendLine("RAM_PURGED_ON_LOCK=" + ramPurgedOnLock);
                sb.AppendLine("PWD_GENERATOR_OK=" + genOk);
                sb.AppendLine("NVAULT_ZERO_PLAINTEXT=" + bkZeroPlain);
                sb.AppendLine("NVAULT_TAMPER_BLOCKED=" + tamperBlocked);
                sb.AppendLine("NVAULT_ROUNDTRIP_OK=" + backupRoundTripOk);
                sb.AppendLine("SECRET_MASK_OK=" + maskOk);
                sb.AppendLine("HEX_COLOR_DETECT_OK=" + hexOk);

                // 2. Test UI Popup + Vault Tab with User/Eye icons + Vault bar + Preview PNG
                using (ClipPopupForm f = new ClipPopupForm(tmpDir, true))
                {
                    f.Show();
                    f.SwitchTab(2); // Show Encrypted Vault with User/Eye icons + PIN/Backup bar!
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
                bool allOk = zeroPlaintextOnDisk && doubleLayerOk && zeroRamWhileLocked && wrongPinBlocked &&
                             unlockOk && ramPurgedOnLock && genOk && bkZeroPlain && tamperBlocked && backupRoundTripOk && maskOk && hexOk;
                sb.AppendLine("SMOKE_RESULT=" + (allOk ? "PASS" : "FAIL"));
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
