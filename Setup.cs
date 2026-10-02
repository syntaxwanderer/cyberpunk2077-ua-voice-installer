// Українська озвучка Cyberpunk 2077 (ШІ) — інсталятор. Відкритий код, нічого не завантажує з інтернету.
// Робить три речі: кладе UA_Voice.archive у archive\pc\mod, перевіряє його цілість
// і вмикає мову озвучення «Українська (ШІ)» у UserSettings.json (з резервною копією).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

public class Job
{
    public const string Ver = "0.5";
    public const string Carrier = "es-mx";          // слот меню, під яким звучить українська
    const string ArchiveName = "UA_Voice.archive";
    static readonly string[] Stale = { "UA_Voice_pilot.archive" };
    public string Here, Game;
    public bool SkipRunCheck;
    public Action<string> Log = delegate { };

    public static bool IsGame(string d)
    {
        return !string.IsNullOrEmpty(d) && File.Exists(Path.Combine(d, "bin\\x64\\Cyberpunk2077.exe")) && Directory.Exists(Path.Combine(d, "archive\\pc\\content"));
    }

    static void Add(List<string> c, string p) { if (!string.IsNullOrEmpty(p)) c.Add(p.Replace('/', '\\')); }

    public static string FindGame()
    {
        List<string> c = new List<string>();
        // Steam
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam"))
            {
                string sp = k == null ? null : k.GetValue("SteamPath") as string;
                if (sp != null)
                {
                    List<string> libs = new List<string>(); libs.Add(sp);
                    string vdf = Path.Combine(sp, "steamapps\\libraryfolders.vdf");
                    if (File.Exists(vdf))
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                            libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
                    foreach (string l in libs) Add(c, Path.Combine(l.Replace('/', '\\'), "steamapps\\common\\Cyberpunk 2077"));
                }
            }
        }
        catch { }
        // GOG: шукаємо будь-яку гру з назвою Cyberpunk 2077
        foreach (string root in new string[] { "SOFTWARE\\WOW6432Node\\GOG.com\\Games", "SOFTWARE\\GOG.com\\Games" })
        {
            try
            {
                using (RegistryKey g = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (g == null) continue;
                    foreach (string id in g.GetSubKeyNames())
                        using (RegistryKey k = g.OpenSubKey(id))
                        {
                            string name = k == null ? null : k.GetValue("gameName") as string;
                            if (id == "1423049311" || (name != null && name.IndexOf("Cyberpunk 2077", StringComparison.OrdinalIgnoreCase) >= 0))
                                Add(c, k.GetValue("path") as string);
                        }
                }
            }
            catch { }
        }
        // Epic Games
        try
        {
            string man = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic\\EpicGamesLauncher\\Data\\Manifests");
            if (Directory.Exists(man))
                foreach (string f in Directory.GetFiles(man, "*.item"))
                {
                    string t = File.ReadAllText(f);
                    if (t.IndexOf("Cyberpunk", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Match m = Regex.Match(t, "\"InstallLocation\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success) Add(c, m.Groups[1].Value.Replace("\\\\", "\\"));
                }
        }
        catch { }
        // типові місця
        foreach (DriveInfo d in DriveInfo.GetDrives())
        {
            if (d.DriveType != DriveType.Fixed) continue;
            string r = d.Name;
            Add(c, r + "Program Files (x86)\\Steam\\steamapps\\common\\Cyberpunk 2077");
            Add(c, r + "SteamLibrary\\steamapps\\common\\Cyberpunk 2077");
            Add(c, r + "GOG Games\\Cyberpunk 2077");
            Add(c, r + "Program Files (x86)\\GOG Galaxy\\Games\\Cyberpunk 2077");
            Add(c, r + "Program Files\\Epic Games\\Cyberpunk2077");
        }
        foreach (string d in c) if (IsGame(d)) return d;
        return null;
    }

    static bool GameRunning() { return Process.GetProcessesByName("Cyberpunk2077").Length > 0; }

    public static string SettingsFile()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CD Projekt Red\\Cyberpunk 2077\\UserSettings.json");
    }

    // Знаходить "value" опції VoiceOver у групі /language. Текстова заміна — решта файлу байт-у-байт та сама.
    static readonly Regex VoRx = new Regex("(\"name\"\\s*:\\s*\"VoiceOver\"[^{}]*?\"value\"\\s*:\\s*\")([^\"]*)(\")", RegexOptions.Singleline);

    static string ReadVo(string text)
    {
        Match m = VoRx.Match(text);
        return m.Success ? m.Groups[2].Value : null;
    }

    static bool WriteVo(string file, string value)
    {
        string t = File.ReadAllText(file, new UTF8Encoding(false));
        if (!VoRx.IsMatch(t)) return false;
        string n = VoRx.Replace(t, delegate (Match m) { return m.Groups[1].Value + value + m.Groups[3].Value; }, 1);
        if (n != t) File.WriteAllText(file, n, new UTF8Encoding(false));
        return true;
    }

    string Source()
    {
        foreach (string p in new string[] { Path.Combine(Here, "archive\\pc\\mod\\" + ArchiveName), Path.Combine(Here, ArchiveName) })
            if (File.Exists(p)) return p;
        return null;
    }

    // повертає null, якщо все добре, інакше текст помилки
    public string Install()
    {
        if (!IsGame(Game)) return "у теці немає гри Cyberpunk 2077 (очікую bin\\x64\\Cyberpunk2077.exe): " + Game;
        if (!SkipRunCheck && GameRunning()) return "гра запущена. Закрийте її і спробуйте ще раз.";
        string src = Source();
        if (src == null) return "поруч з інсталятором немає файлу " + ArchiveName + ". Розпакуйте архів мода повністю (має бути тека archive\\pc\\mod\\" + ArchiveName + " поруч з UA_Voice_Setup.exe) і запускайте інсталятор з розпакованої теки.";
        Log("перевіряю пакет...");
        long size = new FileInfo(src).Length;
        uint crc = Crc32.File(src);
        string modDir = Path.Combine(Game, "archive\\pc\\mod");
        Directory.CreateDirectory(modDir);
        string dst = Path.Combine(modDir, ArchiveName);
        if (File.Exists(dst) && new FileInfo(dst).Length == size && Crc32.File(dst) == crc)
            Log("архів озвучки вже встановлено (та сама версія)");
        else
        {
            long free = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(Game))).AvailableFreeSpace;
            if (free < size + (50L << 20)) return string.Format("замало місця на диску: треба {0:N0} МБ", size / 1048576.0);
            string tmp = dst + ".ua_tmp";
            Log(string.Format("копіюю {0} ({1:N1} МБ)...", ArchiveName, size / 1048576.0));
            File.Copy(src, tmp, true);
            if (new FileInfo(tmp).Length != size || Crc32.File(tmp) != crc) { File.Delete(tmp); return "самоперевірка копії не пройшла (диск?). Гру не змінено."; }
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(tmp, dst);
            Log("архів озвучки встановлено: " + dst);
        }
        foreach (string s in Stale)
        {
            string p = Path.Combine(modDir, s);
            if (File.Exists(p)) { File.Delete(p); Log("прибрано стару тестову версію: " + s); }
        }
        foreach (string f in Directory.GetFiles(modDir, "*.archive"))
        {
            string n = Path.GetFileName(f);
            if (Regex.IsMatch(n, "(?i)(voice|vo_|language|lang|dub)") && !n.Equals(ArchiveName, StringComparison.OrdinalIgnoreCase))
                Log("увага: у archive\\pc\\mod є інший мовний/голосовий мод (" + n + "). Якщо «Українська (ШІ)» не з'явиться в меню — він конфліктує.");
        }
        SetVoice();
        return null;
    }

    void SetVoice()
    {
        string us = SettingsFile();
        if (!File.Exists(us))
        {
            Log("налаштувань гри ще немає (гру не запускали). У грі: Налаштування → Мова → Озвучення → «Українська (ШІ)».");
            return;
        }
        string t = File.ReadAllText(us, new UTF8Encoding(false));
        string cur = ReadVo(t);
        if (cur == null) { Log("не знайшов у налаштуваннях опції озвучення — оберіть «Українська (ШІ)» у меню гри вручну."); return; }
        string bak = us + ".bak_ua";
        if (!File.Exists(bak) && cur != Carrier) File.Copy(us, bak);
        if (cur == Carrier) { Log("озвучення «Українська (ШІ)» вже увімкнене в налаштуваннях"); return; }
        if (WriteVo(us, Carrier)) Log("увімкнув озвучення «Українська (ШІ)» (було: " + cur + "; копія налаштувань: UserSettings.json.bak_ua)");
    }

    public string Uninstall()
    {
        if (!IsGame(Game)) return "у теці немає гри Cyberpunk 2077: " + Game;
        if (!SkipRunCheck && GameRunning()) return "гра запущена. Закрийте її і спробуйте ще раз.";
        int n = 0;
        string modDir = Path.Combine(Game, "archive\\pc\\mod");
        List<string> names = new List<string>(Stale); names.Add(ArchiveName);
        foreach (string s in names)
        {
            string p = Path.Combine(modDir, s);
            if (File.Exists(p)) { File.Delete(p); n++; Log("видалено " + s); }
        }
        string us = SettingsFile(), bak = us + ".bak_ua";
        if (File.Exists(us))
        {
            string cur = ReadVo(File.ReadAllText(us, new UTF8Encoding(false)));
            if (cur == Carrier)
            {
                string prev = File.Exists(bak) ? ReadVo(File.ReadAllText(bak, new UTF8Encoding(false))) : null;
                if (prev == null || prev == Carrier) prev = "en-us";
                WriteVo(us, prev); n++;
                Log("мову озвучення повернуто: " + prev);
            }
            if (File.Exists(bak)) File.Delete(bak);
        }
        if (n == 0) return "нічого видаляти: мод не встановлено.";
        return null;
    }
}

public static class Crc32
{
    static readonly uint[] T = MakeTable();
    static uint[] MakeTable()
    {
        uint[] t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[i] = c;
        }
        return t;
    }
    public static uint File(string path)
    {
        uint c = 0xFFFFFFFFu;
        byte[] b = new byte[1 << 20];
        using (FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
        {
            int n;
            while ((n = s.Read(b, 0, b.Length)) > 0)
                for (int i = 0; i < n; i++) c = T[(c ^ b[i]) & 0xFF] ^ (c >> 8);
        }
        return c ^ 0xFFFFFFFFu;
    }
}
