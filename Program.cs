using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

public class MainForm : Form
{
    TextBox path = new TextBox(), log = new TextBox();
    Button browse = new Button(), install = new Button(), remove = new Button();
    ProgressBar bar = new ProgressBar();
    string here = AppDomain.CurrentDomain.BaseDirectory;

    public MainForm()
    {
        Text = "Українська озвучка Cyberpunk 2077 (ШІ) — v" + Job.Ver;
        ClientSize = new Size(640, 430); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        Font = new Font("Segoe UI", 9.5f);
        Label t = new Label(); t.Text = "Фанатська українська озвучка (синтез ШІ). Неофіційна робота, не схвалена CD PROJEKT RED.\r\nОкремий пункт меню «Українська (ШІ)» — чужі озвучки не перезаписуються. Гра має бути закрита.";
        t.SetBounds(14, 10, 612, 42);
        Label l = new Label(); l.Text = "Тека гри:"; l.SetBounds(14, 62, 80, 22);
        path.SetBounds(94, 60, 432, 24);
        browse.Text = "Обрати…"; browse.SetBounds(534, 58, 92, 28);
        install.Text = "Встановити"; install.SetBounds(14, 96, 150, 34);
        remove.Text = "Видалити мод"; remove.SetBounds(172, 96, 150, 34);
        bar.SetBounds(334, 104, 292, 18); bar.Style = ProgressBarStyle.Marquee; bar.Visible = false;
        log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical;
        log.SetBounds(14, 140, 612, 276); log.BackColor = SystemColors.Window;
        Controls.AddRange(new Control[] { t, l, path, browse, install, remove, bar, log });
        path.Text = Job.FindGame() ?? "";
        Write(path.Text.Length > 0 ? "Знайдено гру: " + path.Text : "Гру не знайдено автоматично — вкажіть теку гри (де лежать bin і archive).");
        browse.Click += delegate
        {
            FolderBrowserDialog d = new FolderBrowserDialog(); d.Description = "Тека Cyberpunk 2077 (де лежать bin і archive)";
            if (d.ShowDialog(this) == DialogResult.OK) path.Text = d.SelectedPath;
        };
        install.Click += delegate { Run(false); };
        remove.Click += delegate { Run(true); };
    }

    void Write(string s)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string>(Write), s); return; }
        log.AppendText(s + "\r\n");
    }

    void Run(bool uninstall)
    {
        install.Enabled = remove.Enabled = browse.Enabled = path.Enabled = false; bar.Visible = true;
        Job j = new Job(); j.Here = here; j.Game = path.Text.Trim().Trim('"'); j.Log = Write;
        Thread th = new Thread(delegate ()
        {
            string err;
            try { err = uninstall ? j.Uninstall() : j.Install(); }
            catch (UnauthorizedAccessException) { err = "немає прав на запис у теку гри. Закрийте інсталятор і запустіть його правою кнопкою → «Запустити від імені адміністратора»."; }
            catch (Exception ex) { err = ex.Message; }
            BeginInvoke(new Action(delegate
            {
                bar.Visible = false; install.Enabled = remove.Enabled = browse.Enabled = path.Enabled = true;
                if (err != null) { Write("ПОМИЛКА: " + err); MessageBox.Show(this, err, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                else if (uninstall) { Write("Готово. Мод видалено, мову озвучення повернуто."); MessageBox.Show(this, "Мод видалено, мову озвучення повернуто.", "Готово"); }
                else { Write("Готово! Запускайте гру. Мова озвучення: «Українська (ШІ)»."); MessageBox.Show(this, "Встановлено!\r\nУ грі: Налаштування → Мова → Озвучення: «Українська (ШІ)» (вже увімкнено).\r\nПочніть нову гру — Кочівник.", "Готово"); }
            }));
        });
        th.IsBackground = true; th.Start();
    }

    [STAThread]
    public static int Main(string[] args)
    {
        // Режим без вікна для перевірок: --cli [--game <тека>] [--uninstall] [--test]
        if (Array.IndexOf(args, "--cli") >= 0)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Job j = new Job(); j.Here = AppDomain.CurrentDomain.BaseDirectory;
            int gi = Array.IndexOf(args, "--game");
            j.Game = gi >= 0 ? args[gi + 1] : Job.FindGame();
            j.Log = delegate (string s) { Console.WriteLine(s); };
            j.SkipRunCheck = Array.IndexOf(args, "--test") >= 0;
            Console.WriteLine("game: " + j.Game);
            string err;
            try { err = Array.IndexOf(args, "--uninstall") >= 0 ? j.Uninstall() : j.Install(); }
            catch (Exception ex) { err = ex.Message; }
            Console.WriteLine(err == null ? "OK" : "ERROR: " + err);
            return err == null ? 0 : 1;
        }
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
        return 0;
    }
}
