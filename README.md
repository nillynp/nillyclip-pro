# NillyClip Pro — Ultra-Light Clipboard History, Pinned Snippets & DPAPI-Encrypted API Key Vault

> **Πανάλαφρος (`~84 KB`!), 100% Native Διαχειριστής Προχείρου (Clipboard History), Καρφιτσωμένα Snippets & Ασφαλής Κρυπτογραφημένη Κλειδοθήκη API Keys & Κωδικών (`AES-256 + HMAC-SHA256 + Windows DPAPI`) για Windows 10/11!**
> **Αντικαθιστά το βαρύ Windows Clipboard History (`TextInputHost ~178 MB RAM` + Microsoft Cloud sync) με `0%` idle CPU, 100% τοπική ιδιωτικότητα, διπλή κρυπτογράφηση με PIN και vector ματάκι (👁) απόκρυψης/προβολής μυστικών!**
>
> **created by [Nilly graphics (Nillygraphics · Sotiris Christos)](https://nillygraphics.com/)**

---

## Σύγκριση

| Εργαλείο | Μέγεθος | Idle CPU / RAM | Κλειδοθήκη Κωδικών & API Keys | Προστασία & Auto-Clear 30" | HEX Χρώματα & Snippets | Τιμή |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| Windows 11 Clipboard (`Win+V`) | System App | ~178 MB RAM | Όχι (Cloud sync) | Όχι | Όχι | — |
| **NillyClip Pro** | **~84 KB** | **0% CPU · ~10 MB** | **AES-256 + HMAC-SHA256 + DPAPI** | **PIN, Anti-Capture, Cloud-Block, 30s** | **Ναι (Swatches + Pin)** | **0€ (MIT)** |

---

## Κύριες Δυνατότητες

1. **⚡ Ιστορικό Clipboard (`Win + V` ή `Ctrl + Shift + V` οπουδήποτε):**
   - Αντικαθιστά απευθείας το **`Win + V`** των Windows (χωρίς να ανοίγει το Start Menu ή το παλιό παράθυρο των Windows!).
   - Event-driven Win32 `AddClipboardFormatListener` (0% CPU polling!).
   - Καταγράφει κείμενα, links, **screenshots/εικόνες** (με thumbnail) και ανιχνεύει αυτόματα **HEX/RGB χρώματα** με ζωντανό color swatch.
   - `Enter` = αυτόματη επικόλληση στο ενεργό παράθυρο · `Shift+Enter` = Plain Text · `Ctrl+1..9` = αστραπιαία επικόλληση.
2. **📌 Καρφιτσωμένα Snippets:**
   - Καρφιτσώνεις με 1 κλικ στοιχεία από το Ιστορικό ή προσθέτεις δικά σου (ΑΦΜ, IBAN, emails, links, prompts, brand HEX) — δεν σβήνονται ποτέ.
3. **🔐 Ασφαλής Κρυπτογραφημένη Κλειδοθήκη API Keys & Κωδικών (Όχι bloat, μέγιστη ασφάλεια):**
   - **Διπλή Κρυπτογράφηση (με PIN):** `AES-256-CBC` + `HMAC-SHA256` (Encrypt-then-MAC, `100.000` PBKDF2 iterations, ανεξάρτητα Enc+MAC κλειδιά) **μέσα σε Windows DPAPI (`CurrentUser`)** — **0 plaintext bytes στον δίσκο**!
   - **Μηδέν μυστικά στη RAM όσο είναι κλειδωμένη:** Το αρχείο `vault.dpapi` ΔΕΝ αποκρυπτογραφείται καν στη μνήμη μέχρι να δώσεις το PIN σου, και καθαρίζεται αμέσως στο Κλείδωμα (ή αυτόματα μετά από 5 λεπτά) + προστασία brute-force cooldown.
   - **Απλά Logins & API Keys:** Προαιρετικό πεδίο **Username / Email** με κουμπάκι 1-κλικ copy/paste, **1-κλικ Γεννήτρια ισχυρών κωδικών (`⚡`)** με `RNGCryptoServiceProvider`, και **vector ματάκι (👁)** με αυτόματη επανα-απόκρυψη.
   - **Anti-Screen-Capture & Cloud-Block:** Ενεργοποιεί αυτόματα το Win32 `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` στην Κλειδοθήκη (μπλοκάρει καταγραφή οθόνης/spyware), βάζει τα επίσημα Windows Secret Clipboard flags (`CanIncludeInClipboardHistory=0`, `CanUploadToCloudClipboard=0`) και **Auto-Clear Clipboard μετά από 30"**.
   - **Φορητό Backup (`.nvault`):** 1-κλικ Εξαγωγή / Επαναφορά με `AES-256 + HMAC-SHA256` (tamper-proof) για μεταφορά σε νέο PC ή μετά από format!

---

## Εγκατάσταση (100% Portable · Χωρίς Admin)

1. Κατέβασε το **`NillyClip-Pro-v1.0-Portable.zip`** και αποσυμπίεσέ το όπου θες (π.χ. `C:\Tools\NillyClipPro`).
2. Τρέξε με διπλό κλικ το **`Install_NillyClipPro.bat`** *(δεν απαιτεί Admin!)*.
   - Μεταγλωττίζει το `NillyClipPro.exe` (`~62 KB`), απενεργοποιεί το Windows Cloud Clipboard History, το βάζει στην αυτόματη εκκίνηση στο Tray και φτιάχνει συντομεύσεις.
3. Πάτα **`Ctrl + Shift + V`** οπουδήποτε στα Windows!

### Απεγκατάσταση
Τρέξε το **`Uninstall_NillyClipPro.bat`** — αφαιρεί καθαρά συντομεύσεις & αυτόματη εκκίνηση χωρίς να αγγίξει την κρυπτογραφημένη Κλειδοθήκη σου.

---

## Οικοσύστημα Δωρεάν Εργαλείων Windows — created by Nilly graphics (Nillygraphics · Sotiris Christos)

| Εργαλείο | Τι αντικαθιστά | Μέγεθος | Σύνδεσμος |
| :--- | :--- | :---: | :--- |
| **NillyHub** | All-in-One Nilly Suite Launcher & Auto-Updater | ~72 KB | [GitHub](https://github.com/nillynp/nillyhub) |
| **NillyClip Pro** | Windows Clipboard History (`Win+V` ~178 MB) + Encrypted API Vault | ~62 KB | [GitHub](https://github.com/nillynp/nillyclip-pro) |
| **NillyDisk Pro** | TreeSize / WinDirStat / Duplicate Finder / Cache Cleaner | ~79 KB | [GitHub](https://github.com/nillynp/nillydisk-pro) |
| **NillyNote Pro** | Windows Notepad (18 MB) | ~78 KB | [GitHub](https://github.com/nillynp/nillynote-pro) |
| **NillyCode Pro** | Sublime Text (58 MB) | ~104 KB | [GitHub](https://github.com/nillynp/nillycode-pro) |
| **NillyCalc Pro** | Windows Calculator (18 MB) | ~72 KB | [GitHub](https://github.com/nillynp/nillycalc-pro) |
| **NillyBatch Pro** | Adobe Bridge (1.95 GB) | ~66 KB | [GitHub](https://github.com/nillynp/nillybatch-pro) |
| **NillyMedia Lite** | Windows Media Player & Voice Recorder (46 MB) | ~52 KB | [GitHub](https://github.com/nillynp/nillymedia-lite) |
| **NillyQuickSnap** | Windows Snipping Tool (586 MB) | ~49 KB | [GitHub](https://github.com/nillynp/nillyquicksnap) |
| **NillyView Pro** | Windows Photos & Paint | ~48 KB | [GitHub](https://github.com/nillynp/nillyview-pro) |
| **NillyDoc Pro** | Heavy PDF Viewers / Editors | ~2 MB | [GitHub](https://github.com/nillynp/nillydoc-pro) |
| **PC Manager & Tools** | All-in-One Studio & PC Maintenance Suite | Portable | [GitHub](https://github.com/nillynp/nilly-pc-manager-tools) |

---

## Author, Credits & SEO

- **Created by:** **[Nilly graphics (Nillygraphics · Sotiris Christos)](https://nillygraphics.com/)**
- **Website:** [https://nillygraphics.com/](https://nillygraphics.com/)
- **GitHub:** [https://github.com/nillynp](https://github.com/nillynp)
- **License:** MIT License — 100% Free & Open Source (0€)

> **English Summary:** **NillyClip Pro** is a free, ultra-lightweight (~62 KB), open-source native Windows 10/11 clipboard history manager, pinned snippets organizer, and Windows DPAPI-encrypted API key vault with per-item eye reveal/hide masking, HEX color swatches, screenshot thumbnails, and 30s clipboard auto-clear — **created by Nilly graphics (Nillygraphics · Sotiris Christos)** ([nillygraphics.com](https://nillygraphics.com/)).
> **Keywords:** `NillyClip Pro`, `Nilly graphics`, `Nillygraphics`, `Sotiris Christos`, `nillygraphics.com`, `Windows 11 clipboard manager`, `Win+V replacement`, `encrypted API key vault Windows DPAPI`, `lightweight clipboard history`, `pinned snippets HEX color swatch`
