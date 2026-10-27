// 💊性能调控 (Honor Tune) — 单文件 WinForms 应用
// 编译: csc /codepage:65001 /target:winexe /out:HonorTune.exe HonorTune.cs /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("💊性能调控")]
[assembly: AssemblyDescription("💊性能调控 (Honor Tune)")]
[assembly: AssemblyProduct("Honor Tune")]
[assembly: AssemblyVersion("1.4.0.0")]
[assembly: AssemblyFileVersion("1.4.0.0")]
[assembly: AssemblyCopyright("MIT License")]

// ============================================================
//  主题与绘制工具
// ============================================================
static class Th
{
    public static Color Hex(string h) { return ColorTranslator.FromHtml(h); }

    public static readonly Color Bg = Hex("#0C0E13");
    public static readonly Color Bar = Hex("#141827");
    public const int BarH = 54;
    public const int WinRad = 16;
    // 用 DWM 圆角时改成 8，和系统圆角对齐；区域裁剪时保持 16
    public static int CornerRadius = WinRad;
    public static readonly Color Side = Hex("#11141D");
    public static readonly Color Card = Hex("#161A26");
    public static readonly Color CardHot = Hex("#1B2033");
    public static readonly Color Stroke = Hex("#242B3C");
    public static readonly Color StrokeHot = Hex("#35405A");
    public static readonly Color Text = Hex("#E7EBF5");
    public static readonly Color Text2 = Hex("#96A0B7");
    public static readonly Color Text3 = Hex("#5F6980");
    public static readonly Color A1 = Hex("#4C8DFF");
    public static readonly Color A2 = Hex("#8A5CFF");
    public static readonly Color Ok = Hex("#33D08A");
    public static readonly Color Warn = Hex("#FFB020");
    public static readonly Color Bad = Hex("#FF5C72");

    public static readonly Font FPage = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold);
    public static readonly Font FTitle3 = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold);
    public static readonly Font FHero = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold);
    public static readonly Font FH1 = new Font("Microsoft YaHei UI", 17f, FontStyle.Bold);
    public static readonly Font FH2 = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold);
    public static readonly Font FBody = new Font("Microsoft YaHei UI", 9.5f);
    public static readonly Font FSmall = new Font("Microsoft YaHei UI", 8.5f);
    public static readonly Font FTiny = new Font("Microsoft YaHei UI", 7.8f);
    public static readonly Font FNum = new Font("Segoe UI Semibold", 19f);
    public static readonly Font FNumSm = new Font("Segoe UI Semibold", 12.5f);
    public static readonly Font FMono = new Font("Consolas", 8.6f);

    public static GraphicsPath Round(Rectangle r, int rad)
    {
        GraphicsPath p = new GraphicsPath();
        if (rad <= 0) { p.AddRectangle(r); return p; }
        int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Fill(Graphics g, Rectangle r, int rad, Color c)
    {
        using (GraphicsPath p = Round(r, rad))
        using (SolidBrush b = new SolidBrush(c))
            g.FillPath(b, p);
    }

    public static void FillGrad(Graphics g, Rectangle r, int rad, Color c1, Color c2, float angle)
    {
        using (GraphicsPath p = Round(r, rad))
        using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), c1, c2, angle))
            g.FillPath(b, p);
    }

    public static void Frame(Graphics g, Rectangle r, int rad, Color c)
    {
        using (GraphicsPath p = Round(r, rad))
        using (Pen pen = new Pen(c, 1f))
            g.DrawPath(pen, p);
    }

    public static void FrameW(Graphics g, Rectangle r, int rad, Color c, float w)
    {
        using (GraphicsPath p = Round(r, rad))
        using (Pen pen = new Pen(c, w))
            g.DrawPath(pen, p);
    }

    public static void T(Graphics g, string s, Font f, Color c, Rectangle r, TextFormatFlags fl)
    {
        TextRenderer.DrawText(g, s, f, r, c, fl | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    public static void TL(Graphics g, string s, Font f, Color c, int x, int y, int w, int h)
    {
        T(g, s, f, c, new Rectangle(x, y, w, h),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    public static void TR(Graphics g, string s, Font f, Color c, int x, int y, int w, int h)
    {
        T(g, s, f, c, new Rectangle(x, y, w, h),
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    public static void TC(Graphics g, string s, Font f, Color c, int x, int y, int w, int h)
    {
        T(g, s, f, c, new Rectangle(x, y, w, h),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    public static void TTop(Graphics g, string s, Font f, Color c, int x, int y, int w, int h)
    {
        T(g, s, f, c, new Rectangle(x, y, w, h),
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }

    public static Size Measure(string s, Font f)
    {
        return TextRenderer.MeasureText(s, f, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    public static Color Alpha(Color c, int a)
    {
        return Color.FromArgb(a, c.R, c.G, c.B);
    }

    // 顶栏渐变。formY 是这块区域在窗口客户区里的 Y，子控件也能对上同一条渐变。
    public static void PaintBar(Graphics g, int x, int y, int w, int h, int formY)
    {
        GraphicsState st = g.Save();
        g.TranslateTransform(x, y - formY);
        using (LinearGradientBrush lb = new LinearGradientBrush(
            new Rectangle(0, 0, Math.Max(w, 2), BarH + 2), Bar, Bg, 90f))
            g.FillRectangle(lb, 0, formY, w, h);
        g.Restore(st);
    }

    public static void ClipWindow(Form f, int rad)
    {
        int w = f.ClientSize.Width;
        int h = f.ClientSize.Height;
        if (w < rad * 2 || h < rad * 2) return;
        using (GraphicsPath p = Round(new Rectangle(0, 0, w, h), rad))
            f.Region = new Region(p);
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // Win11 的 DWM 圆角是抗锯齿的；区域裁剪（Region）永远是硬边，1080p 下能看到台阶。
    // 成功返回 true，这时不要再设 Region，否则又会切回硬边。
    public static bool RoundWindow(Form f)
    {
        try
        {
            int pref = 2; // DWMWCP_ROUND
            return DwmSetWindowAttribute(f.Handle, 33, ref pref, sizeof(int)) == 0;
        }
        catch { return false; }
    }

    // 卡片底板
    public static void CardBox(Graphics g, Rectangle r, string title, string sub)
    {
        Fill(g, r, 12, Th.Card);
        Frame(g, r, 12, Th.Stroke);
        TL(g, title, FH2, Text, r.X + 20, r.Y + 14, r.Width - 40, 22);
        if (sub != null && sub.Length > 0)
            TL(g, sub, FTiny, Text3, r.X + 20, r.Y + 36, r.Width - 40, 18);
    }

    public static void Dot(Graphics g, int cx, int cy, int d, Color c)
    {
        using (SolidBrush b = new SolidBrush(c))
            g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
    }

    public static void Spark(Graphics g, Rectangle r, double[] vals, int count, Color c)
    {
        if (count < 2) return;
        double max = 1;
        for (int i = 0; i < count; i++) if (vals[i] > max) max = vals[i];
        PointF[] pts = new PointF[count];
        for (int i = 0; i < count; i++)
        {
            float x = r.X + (float)r.Width * i / (count - 1);
            float y = r.Bottom - (float)(Math.Min(vals[i], max) / max) * r.Height;
            pts[i] = new PointF(x, y);
        }
        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddLines(pts);
            path.AddLine(pts[count - 1].X, r.Bottom, pts[0].X, r.Bottom);
            path.CloseFigure();
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 2), Alpha(c, 70), Alpha(c, 4), 90f))
                g.FillPath(b, path);
        }
        using (Pen pen = new Pen(c, 1.6f))
        {
            pen.LineJoin = LineJoin.Round;
            g.DrawLines(pen, pts);
        }
    }
}

// ============================================================
//  Win32 / powrprof
// ============================================================
static class Nat
{
    public static readonly Guid SubProcessor = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid ProcthrottleMin = new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c");
    public static readonly Guid ProcthrottleMax = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");

    [DllImport("powrprof.dll")]
    public static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
    [DllImport("powrprof.dll")]
    public static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);
    [DllImport("powrprof.dll")]
    public static extern uint PowerReadACValueIndex(IntPtr root, ref Guid scheme, ref Guid sub, ref Guid set, out uint val);
    [DllImport("powrprof.dll")]
    public static extern uint PowerWriteACValueIndex(IntPtr root, ref Guid scheme, ref Guid sub, ref Guid set, uint val);
    [DllImport("powrprof.dll")]
    public static extern uint PowerReadDCValueIndex(IntPtr root, ref Guid scheme, ref Guid sub, ref Guid set, out uint val);
    [DllImport("powrprof.dll")]
    public static extern uint PowerWriteDCValueIndex(IntPtr root, ref Guid scheme, ref Guid sub, ref Guid set, uint val);
    [DllImport("powrprof.dll")]
    public static extern uint PowerSetActiveOverlayScheme(Guid g);
    [DllImport("powrprof.dll")]
    public static extern uint PowerGetActualOverlayScheme(out Guid g);
    [DllImport("powrprof.dll")]
    public static extern uint PowerReadFriendlyName(IntPtr root, ref Guid scheme, IntPtr sub, IntPtr set, IntPtr buf, ref uint size);
    [DllImport("powrprof.dll")]
    public static extern uint PowerDeleteScheme(IntPtr root, ref Guid scheme);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LocalFree(IntPtr p);

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME { public uint Low; public uint High; }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX s);

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS s);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, string lParam,
        uint flags, uint timeout, out IntPtr result);

    public static void Broadcast()
    {
        IntPtr r;
        SendMessageTimeout((IntPtr)0xFFFF, 0x001A, IntPtr.Zero, null, 0x0002, 800, out r);
    }

    public static Guid ActiveScheme()
    {
        IntPtr p;
        if (PowerGetActiveScheme(IntPtr.Zero, out p) != 0 || p == IntPtr.Zero) return Guid.Empty;
        try { return (Guid)Marshal.PtrToStructure(p, typeof(Guid)); }
        finally { LocalFree(p); }
    }

    public static bool SetActiveScheme(Guid g)
    {
        return PowerSetActiveScheme(IntPtr.Zero, ref g) == 0;
    }

    public static uint ReadAc(Guid scheme, Guid sub, Guid set)
    {
        uint v;
        if (PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref sub, ref set, out v) != 0) return 0xFFFFFFFF;
        return v;
    }

    public static uint ReadDc(Guid scheme, Guid sub, Guid set)
    {
        uint v;
        if (PowerReadDCValueIndex(IntPtr.Zero, ref scheme, ref sub, ref set, out v) != 0) return 0xFFFFFFFF;
        return v;
    }

    public static bool WriteAc(Guid scheme, Guid sub, Guid set, uint v)
    {
        return PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref sub, ref set, v) == 0;
    }

    public static bool WriteDc(Guid scheme, Guid sub, Guid set, uint v)
    {
        return PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref sub, ref set, v) == 0;
    }

    public static string FriendlyName(Guid g)
    {
        uint size = 1024;
        IntPtr buf = Marshal.AllocHGlobal((int)size);
        try
        {
            if (PowerReadFriendlyName(IntPtr.Zero, ref g, IntPtr.Zero, IntPtr.Zero, buf, ref size) != 0)
                return g.ToString("D");
            return Marshal.PtrToStringUni(buf);
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    // ---- 显示 ----
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields;
        public int dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplayDevices(string dev, uint i, ref DISPLAY_DEVICE dd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplaySettings(string dev, int mode, ref DEVMODE dm);
    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern int ChangeDisplaySettingsEx(string dev, ref DEVMODE dm, IntPtr hwnd, uint flags, IntPtr param);
}

// ============================================================
//  运行环境探测
// ============================================================
static class Env
{
    public static bool Writable;
    public static string Note = "";

    public static void Probe()
    {
        try
        {
            string d = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
            Directory.CreateDirectory(d);
            string f = System.IO.Path.Combine(d, ".probe");
            File.WriteAllText(f, "1");
            File.Delete(f);
            Writable = true;
        }
        catch (Exception ex)
        {
            Writable = false;
            Note = ex.GetType().Name;
        }
    }
}

// ============================================================
//  INI 配置
// ============================================================
class Cfg
{
    public string Path;
    Dictionary<string, Dictionary<string, string>> sec = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    public static Cfg Load()
    {
        Cfg c = new Cfg();
        Env.Probe();

        // 优先 %APPDATA%；若进程受限（低完整性级别），退回 exe 同目录，再退回临时目录
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
        if (!Env.Writable)
        {
            string exeDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            dir = System.IO.Path.Combine(exeDir, "HonorTune-data");
            try { Directory.CreateDirectory(dir); } catch { }
        }
        c.Path = System.IO.Path.Combine(dir, "config.ini");
        if (File.Exists(c.Path))
        {
            string cur = "";
            try
            {
                foreach (string raw in File.ReadAllLines(c.Path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]")) { cur = line.Substring(1, line.Length - 2); continue; }
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    c.Set(cur, k, v);
                }
            }
            catch { }
        }
        return c;
    }

    public Dictionary<string, string> Section(string s)
    {
        Dictionary<string, string> d;
        if (!sec.TryGetValue(s ?? "", out d))
        {
            d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            sec[s ?? ""] = d;
        }
        return d;
    }

    public string Get(string s, string k, string def)
    {
        Dictionary<string, string> d;
        if (sec.TryGetValue(s ?? "", out d))
        {
            string v;
            if (d.TryGetValue(k, out v)) return v;
        }
        return def;
    }

    public int GetInt(string s, string k, int def)
    {
        int v;
        if (int.TryParse(Get(s, k, ""), out v)) return v;
        return def;
    }

    public void Set(string s, string k, string v) { Section(s)[k] = v; }

    public void RemoveSection(string s) { sec.Remove(s); }

    public List<string> Sections(string prefix)
    {
        List<string> list = new List<string>();
        foreach (string k in sec.Keys)
            if (k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                list.Add(k);
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    public void Save()
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; 荣耀性能调控 配置文件");
            foreach (KeyValuePair<string, Dictionary<string, string>> kv in sec)
            {
                if (kv.Value.Count == 0) continue;
                sb.AppendLine("[" + kv.Key + "]");
                foreach (KeyValuePair<string, string> v in kv.Value)
                    sb.AppendLine(v.Key + "=" + v.Value);
                sb.AppendLine();
            }
            File.WriteAllText(Path, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }
}

// ============================================================
//  模式定义
// ============================================================
class ModeDef
{
    public string Key;
    public string Name;
    public string En;
    public string Tag;
    public string Desc;
    public string SchemeName;
    public Guid Template;
    public uint MaxAc, MaxDc, MinAc, MinDc;
    public Guid Overlay;
    public bool Anim;
    public bool BgLimit;
    public Color C1, C2;
    public string[] Bullets;
}

static class Modes
{
    public static readonly List<ModeDef> All = new List<ModeDef>();

    static Modes()
    {
        ModeDef eco = new ModeDef();
        eco.Key = "eco";
        eco.Name = "省电";
        eco.En = "ECO";
        eco.Tag = "续航优先";
        eco.Desc = "压低处理器上限并切到最佳能效，风扇更安静，适合外出办公、查资料、写文档。";
        eco.SchemeName = "荣耀调控 · 省电";
        eco.Template = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");
        eco.MaxAc = 55; eco.MaxDc = 40; eco.MinAc = 5; eco.MinDc = 5;
        eco.Overlay = new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a");
        eco.Anim = false; eco.BgLimit = true;
        eco.C1 = Th.Hex("#33D08A"); eco.C2 = Th.Hex("#1B9E77");
        eco.Bullets = new string[] { "处理器上限 55% / 40%", "电源模式：最佳能效", "关闭窗口与菜单动画", "限制后台应用运行" };
        All.Add(eco);

        ModeDef bal = new ModeDef();
        bal.Key = "balanced";
        bal.Name = "均衡";
        bal.En = "BALANCED";
        bal.Tag = "日常推荐";
        bal.Desc = "性能与续航兼顾，保留睿频余量，适合办公、网课与轻度创作。";
        bal.SchemeName = "荣耀调控 · 均衡";
        bal.Template = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
        bal.MaxAc = 90; bal.MaxDc = 75; bal.MinAc = 10; bal.MinDc = 5;
        bal.Overlay = Guid.Empty;
        bal.Anim = true; bal.BgLimit = false;
        bal.C1 = Th.Hex("#4C8DFF"); bal.C2 = Th.Hex("#8A5CFF");
        bal.Bullets = new string[] { "处理器上限 90% / 75%", "电源模式：平衡", "保留界面动画", "后台应用不限制" };
        All.Add(bal);

        ModeDef perf = new ModeDef();
        perf.Key = "perf";
        perf.Name = "性能";
        perf.En = "PERFORMANCE";
        perf.Tag = "满血释放";
        perf.Desc = "解除处理器上限，睿频全开，适合游戏、渲染、编译等重负载场景。";
        perf.SchemeName = "荣耀调控 · 性能";
        perf.Template = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        perf.MaxAc = 100; perf.MaxDc = 100; perf.MinAc = 25; perf.MinDc = 10;
        perf.Overlay = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");
        perf.Anim = true; perf.BgLimit = false;
        perf.C1 = Th.Hex("#FF8A3D"); perf.C2 = Th.Hex("#FF4D6D");
        perf.Bullets = new string[] { "处理器上限 100% / 100%", "电源模式：最佳性能", "保留界面动画", "后台应用不限制" };
        All.Add(perf);
    }

    public static ModeDef Get(string key)
    {
        foreach (ModeDef m in All) if (m.Key == key) return m;
        return null;
    }
}

// ============================================================
//  荣耀 EC：性能模式与风扇转速（OemWMIMethod）
//  已在本机核对：04/0E 读模式，04/0F 写模式，02/08 读转速。
//  模式 1 = 均衡（管家会弹出「均衡模式」），模式 3 = 高能，模式 0 = 智能。
// ============================================================
static class HonorEc
{
    const string FanKey = @"HKEY_CURRENT_USER\SOFTWARE\HONOR\PCManager\Performance";
    static ManagementObject inst;
    static bool dead;

    public static int Mode = -1;
    public static int Fan0 = -1;
    public static int Fan1 = -1;
    public static int FanIndex = -1;
    public static string Error = "";

    // 风扇转速指令按机型取自荣耀 FanSpeedConfig.dat
    public static string MachineType = "";
    public static int FanCmd = -1;
    public static int[] FanArg = new int[5];
    public static bool FanTableOk;
    static bool fanTried;

    static ManagementObject Inst()
    {
        if (inst != null) return inst;
        if (dead) return null;
        try
        {
            ManagementObjectSearcher s = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM OemWMIMethod");
            foreach (ManagementObject o in s.Get())
            {
                object name = o["InstanceName"];
                if (name != null && name.ToString().IndexOf("HWMI_0") >= 0)
                {
                    inst = o;
                    return inst;
                }
            }
            dead = true;
            Error = "未找到荣耀 WMI 接口";
        }
        catch (Exception ex)
        {
            dead = true;
            Error = ex.Message;
        }
        return null;
    }

    public static byte[] Call(byte group, byte cmd, byte value)
    {
        byte[] inp = new byte[64];
        inp[0] = group;
        inp[1] = cmd;
        inp[2] = value;
        return CallRaw(inp);
    }

    // WMI 一次收 64 字节，前三字节是 group/cmd/值，剩下的是这类指令的负载。
    public static byte[] CallRaw(byte[] payload)
    {
        ManagementObject o = Inst();
        if (o == null) return null;
        try
        {
            ManagementBaseObject pin = o.GetMethodParameters("OemWMIfun");
            pin["u8Input"] = payload;
            ManagementBaseObject pout = o.InvokeMethod("OemWMIfun", pin, null);
            if (pout == null) return null;
            return pout["u8Output"] as byte[];
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return null;
        }
    }

    // GPU 功耗三元组：group 3 / cmd 0x17，[2]=0xFF 表示后面带数据，
    // [3]=TGP-60，[4]=温度墙，[5]=CPU 瓦数。和 HNSDK_SetGPUToCPUMaxPower 逐字节一致。
    public static int WattTgp = 100;
    public static int WattTemp = 87;
    public static int WattCpu = 140;
    public static string WattError = "";
    public static bool WattApplied;
    public static string WattVariant = "";
    public static string WattReport = "";
    public static int WattPrefer = -1;

    // 电池充电阈值：读 group 3 / cmd 0x11（参数 0xFF），回包 [1]=恢复%、[2]=上限%；
    // 写 group 3 / cmd 0x10，[2]=恢复%、[3]=上限%。实测 40/80 能写进去并读回。
    public static int BatStop = -1;
    public static int BatResume = -1;
    public static readonly int[] BatStopPreset = new int[] { 70, 90, 100 };
    public static readonly int[] BatResumePreset = new int[] { 40, 70, 95 };
    public static string BatError = "";

    public static bool ReadBatteryLimit()
    {
        byte[] p = new byte[64];
        p[0] = 0x03;
        p[1] = 0x11;
        p[2] = 0xFF;
        byte[] r = CallRaw(p);
        if (r == null || r.Length < 3 || r[0] != 0) { BatError = r == null ? "EC 无响应" : "读取失败"; return false; }
        BatResume = r[1];
        BatStop = r[2];
        return true;
    }

    public static bool SetBatteryLimit(int stop, int resume)
    {
        if (stop < 60 || stop > 100) { BatError = "上限只能是 60–100%"; return false; }
        if (resume < 30 || resume > stop - 5) { BatError = "恢复点必须低于上限"; return false; }
        byte[] p = new byte[64];
        p[0] = 0x03;
        p[1] = 0x10;
        p[2] = (byte)resume;
        p[3] = (byte)stop;
        byte[] r = CallRaw(p);
        if (r == null || r.Length == 0 || r[0] != 0) { BatError = r == null ? "EC 无响应" : ("EC 返回 " + r[0]); return false; }
        if (!ReadBatteryLimit() || BatStop != stop || BatResume != resume)
        {
            BatError = "写进去了但读回来不一致：" + BatStop + "/" + BatResume;
            return false;
        }
        BatError = "";
        return true;
    }

    static string[] VarName = new string[] { "A", "B", "C", "D" };

    public static int VariantIndex(string name)
    {
        for (int i = 0; i < VarName.Length; i++) if (VarName[i] == name) return i;
        return -1;
    }

    // 同一个 group 3 指令在这台机器上试过的几种排布：
    // A = 营地的写法（[2]=0xFF，数据在 3/4/5）
    // B = 不写标记，数据在 3/4/5
    // C = 数据整体前移到 2/3/4
    // D = cmd 换成 0x16
    static byte[] BuildWatt(int kind, int tgp, int temp, int cpu)
    {
        byte[] p = new byte[64];
        p[0] = 0x03;
        p[1] = kind == 3 ? (byte)0x16 : (byte)0x17;
        if (kind == 0) { p[2] = 0xFF; p[3] = (byte)(tgp - 60); p[4] = (byte)temp; p[5] = (byte)cpu; }
        else if (kind == 1) { p[2] = 0x00; p[3] = (byte)(tgp - 60); p[4] = (byte)temp; p[5] = (byte)cpu; }
        else if (kind == 2) { p[2] = (byte)(tgp - 60); p[3] = (byte)temp; p[4] = (byte)cpu; }
        else { p[2] = 0xFF; p[3] = (byte)(tgp - 60); p[4] = (byte)temp; p[5] = (byte)cpu; }
        return p;
    }

    static bool SendWatt(int kind, int tgp, int temp, int cpu, out byte[] raw)
    {
        raw = CallRaw(BuildWatt(kind, tgp, temp, cpu));
        return raw != null && raw.Length > 0 && raw[0] == 0;
    }

    public static bool SetGpuWatt(int tgp, int temp, int cpuWatt)
    {
        if (tgp < 60 || tgp > 115) { WattError = "TGP 只能是 60–115 W"; return false; }
        if (temp < 75 || temp > 87) { WattError = "温度墙只能是 75–87 ℃"; return false; }
        if (cpuWatt < 30 || cpuWatt > 160) { WattError = "CPU 功耗只能是 30–160 W"; return false; }
        StringBuilder rep = new StringBuilder();
        int[] order = new int[4];
        if (WattPrefer >= 0 && WattPrefer < 4)
            for (int i = 0; i < 4; i++) order[i] = (WattPrefer + i) & 3;
        else
            for (int i = 0; i < 4; i++) order[i] = i;
        for (int n = 0; n < 4; n++)
        {
            int kind = order[n];
            byte[] raw;
            bool ok = SendWatt(kind, tgp, temp, cpuWatt, out raw);
            rep.Append(VarName[kind]).Append('=').Append(raw == null ? "无响应" : raw[0].ToString());
            if (n < 3) rep.Append(" · ");
            if (ok)
            {
                WattTgp = tgp;
                WattTemp = temp;
                WattCpu = cpuWatt;
                WattApplied = true;
                WattVariant = VarName[kind];
                WattPrefer = kind;
                WattError = "";
                WattReport = rep.ToString();
                return true;
            }
        }
        WattError = "EC 拒绝了写入（" + rep + "）";
        WattReport = rep.ToString();
        return false;
    }

    // 把几种排布和几档 TGP 都跑一遍，结果落到 %APPDATA%\HonorTune\power-diag.txt
    public static string WattDiag(int temp, int cpu)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("HonorTune 功耗墙诊断 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("温度墙=" + temp + " CPU=" + cpu);
        int[] tgps = new int[] { 0, 60, 80, 100 };
        foreach (int t in tgps)
        {
            int tgp = t == 0 ? WattTgp : t;
            sb.Append("TGP " + tgp + " (" + (tgp - 60) + ") :");
            for (int kind = 0; kind < 4; kind++)
            {
                byte[] raw;
                bool ok = SendWatt(kind, tgp, temp, cpu, out raw);
                sb.Append("  ").Append(VarName[kind]).Append('=');
                if (raw == null) sb.Append("null");
                else
                {
                    sb.Append(raw[0]).Append(ok ? "(OK)" : "");
                    sb.Append('[');
                    for (int i = 1; i < Math.Min(8, raw.Length); i++)
                        sb.Append(raw[i].ToString("X2")).Append(i < 7 ? " " : "");
                    sb.Append(']');
                }
            }
            sb.AppendLine();
        }
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(System.IO.Path.Combine(dir, "power-diag.txt"), sb.ToString(), Encoding.UTF8);
        }
        catch { }
        return sb.ToString();
    }

    static int Word(byte[] b)
    {
        if (b == null || b.Length < 3 || b[0] != 0) return -1;
        return b[1] + b[2] * 256;
    }

    public static void Refresh()
    {
        EnsureFanTable();
        byte[] m = Call(0x04, 0x0E, 0);
        if (m != null && m.Length > 1 && m[0] == 0) Mode = m[1];
        Fan0 = Word(Call(0x02, 0x08, 0));
        Fan1 = Word(Call(0x02, 0x08, 1));
        try
        {
            object v = Registry.GetValue(FanKey, "UserFanIndex", null);
            if (v != null) FanIndex = Convert.ToInt32(v);
        }
        catch { }
    }

    public static bool SetMode(int mode)
    {
        if (mode != 0 && mode != 1 && mode != 3) return false;
        byte[] r = Call(0x04, 0x0F, (byte)mode);
        bool ok = r != null && r.Length > 0 && r[0] == 0;
        if (ok)
        {
            byte[] back = Call(0x04, 0x0E, 0);
            if (back != null && back.Length > 1 && back[0] == 0) Mode = back[1];
            else Mode = mode;
        }
        return ok && Mode == mode;
    }

    static string RegStr(string key, string name)
    {
        try
        {
            object v = Registry.GetValue(key, name, null);
            return v == null ? "" : v.ToString();
        }
        catch { return ""; }
    }

    static int ParseHex(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        try
        {
            s = s.Trim();
            if (s.StartsWith("0x") || s.StartsWith("0X")) return Convert.ToInt32(s.Substring(2), 16);
            return Convert.ToInt32(s, 10);
        }
        catch { return -1; }
    }

    // 从荣耀自己的配置里按当前机型取风扇指令参数，不再把数值写死在代码里
    public static void EnsureFanTable()
    {
        if (fanTried) return;
        fanTried = true;
        FanTableOk = false;
        for (int i = 0; i < 5; i++) FanArg[i] = -1;
        try
        {
            string dir = RegStr(@"HKEY_LOCAL_MACHINE\SOFTWARE\HONOR\PCManager", "InstallPath");
            if (string.IsNullOrEmpty(dir)) dir = @"C:\Program Files\HONOR\PCManager";
            string path = Path.Combine(dir, @"config\FanSpeedConfig.dat");
            if (!File.Exists(path))
            {
                Error = "未找到风扇配置文件";
                return;
            }

            if (MachineType.Length == 0) MachineType = RegStr(@"HKEY_LOCAL_MACHINE\SOFTWARE\HONOR\PCManager", "ProductName");
            if (MachineType.Length == 0) MachineType = RegStr(@"HKEY_LOCAL_MACHINE\SOFTWARE\HONOR\BIOS", "DeviceTypeEx");
            if (MachineType.Length == 0)
            {
                Error = "读不到机型";
                return;
            }

            // 文件是「XML 头 + 加密尾」，每个字节都加了 0x80；只取可读的那段
            byte[] raw = File.ReadAllBytes(path);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                int v = (raw[i] - 0x80) & 0xFF;
                if (v == 9 || v == 10 || v == 13 || (v >= 32 && v < 127)) sb.Append((char)v);
                else break;
            }
            string text = sb.ToString();

            MatchCollection prods = Regex.Matches(text, "<Product\\s+MachineType=\"([^\"]+)\"[^>]*?input1=\"([^\"]+)\"");
            Match hit = null;
            foreach (Match p in prods)
            {
                if (p.Groups[1].Value == MachineType) { hit = p; break; }
            }
            if (hit == null)
            {
                Error = "配置里没有机型 " + MachineType;
                return;
            }

            FanCmd = ParseHex(hit.Groups[2].Value);
            int start = hit.Index + hit.Length;
            int end = text.Length;
            foreach (Match p in prods)
            {
                if (p.Index > start) { end = p.Index; break; }
            }
            string body = text.Substring(start, end - start);

            int got = 0;
            foreach (Match f in Regex.Matches(body, "<Fan\\s+index=\"(\\d)\"\\s+input2=\"([^\"]+)\""))
            {
                int idx = Convert.ToInt32(f.Groups[1].Value);
                int val = ParseHex(f.Groups[2].Value);
                if (idx >= 0 && idx < 5 && val >= 0) { FanArg[idx] = val; got++; }
            }

            // 可读段可能正好截在最后一档中间（本机就是这样），
            // 若只缺第 5 档、前四档是等差，就按同样步长补上
            if (FanCmd >= 0 && FanArg[0] >= 0 && FanArg[1] >= 0 && FanArg[2] >= 0 && FanArg[3] >= 0 && FanArg[4] < 0)
            {
                int d1 = FanArg[1] - FanArg[0];
                int d2 = FanArg[2] - FanArg[1];
                int d3 = FanArg[3] - FanArg[2];
                if (d1 == d2 && d2 == d3) FanArg[4] = FanArg[3] + d3;
            }

            bool all = true;
            for (int i = 0; i < 5; i++) if (FanArg[i] < 0) all = false;
            FanTableOk = FanCmd >= 0 && all;
            if (!FanTableOk) Error = "机型 " + MachineType + " 的档位不完整";
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            FanTableOk = false;
        }
    }

    // 真实转速指令：WMI 07 / input1 / input2（按机型取自 FanSpeedConfig.dat）
    public static bool SetFanIndex(int idx)
    {
        if (idx < 0 || idx > 4) return false;
        EnsureFanTable();
        if (!FanTableOk)
        {
            if (Error.Length == 0) Error = "本机没有可用的转速配置";
            return false;
        }
        if (Mode != 3)
        {
            Error = "高能档以外不响应转速档位";
            return false;
        }
        byte[] r = Call(0x07, (byte)FanCmd, (byte)FanArg[idx]);
        bool ok = r != null && r.Length > 0 && r[0] == 0;
        if (!ok)
        {
            Error = "转速指令被拒绝";
            return false;
        }
        FanIndex = idx;
        try { Registry.SetValue(FanKey, "UserFanIndex", idx, RegistryValueKind.DWord); }
        catch { }
        return true;
    }

    public static string ModeName(int m)
    {
        if (m == 0) return "智能";
        if (m == 1) return "性能";
        if (m == 3) return "高能";
        if (m < 0) return "—";
        return "模式 " + m;
    }
}

// ============================================================
//  GPU 频率偏移。和猎人营地 HNSDK.dll 同一条路：
//  nvapi64!nvapi_QueryInterface → NvAPI_GPU_SetPstates20 (0x0F4DAE6B)
//  结构版本 0x31CF8，只改 P0 的一个 clock，单位 kHz。
//  核心 domain=0，显存 domain=4。界面上的 MHz 在这里乘 1000，和猎人营地相同。
// ============================================================
static class GpuOc
{
    public const int CoreMax = 200;
    public const int MemMax = 400;
    const int StructSize = 0x1CF8;
    const int Ver = 0x00031CF8;
    const uint IdInit = 0x0150E828;
    const uint IdEnum = 0xE5AC921F;
    const uint IdSet = 0x0F4DAE6B;
    const uint IdGet = 0x6FF81213;
    const int PrivDenied = -137;

    public static int CoreMhz;
    public static int MemMhz;
    public static int LiveCore = -1;
    public static int LiveMem = -1;
    public static bool Ready;
    public static string Error = "";
    public static string Status = "尚未下发";

    delegate IntPtr QueryFn(uint id);
    delegate int InitFn();
    delegate int EnumFn(IntPtr handles, IntPtr count);
    delegate int SetFn(IntPtr gpu, IntPtr info);

    static QueryFn query;
    static IntPtr gpu;
    static bool opened;

    public static void Load(Cfg cfg)
    {
        CoreMhz = Clamp(cfg.GetInt("gpu", "core", 0), 0, CoreMax);
        MemMhz = Clamp(cfg.GetInt("gpu", "mem", 0), 0, MemMax);
    }

    public static int Clamp(int v, int lo, int hi)
    {
        if (v < lo) return lo;
        if (v > hi) return hi;
        return v;
    }

    public static bool Probe()
    {
        return Ensure();
    }

    public static bool Apply(int coreMhz, int memMhz)
    {
        coreMhz = Clamp(coreMhz, 0, CoreMax);
        memMhz = Clamp(memMhz, 0, MemMax);
        if (!Ensure()) return false;
        int c = SetDomain(0, coreMhz * 1000);
        int m = SetDomain(4, memMhz * 1000);
        if (c != 0 || m != 0)
        {
            Error = (c == PrivDenied || m == PrivDenied)
                ? "需要管理员权限，驱动拒绝了这次写入"
                : "NVAPI 核心 " + Hex(c) + " · 显存 " + Hex(m);
            Status = "下发失败";
            opened = false;
            return false;
        }
        ReadLive();
        if (LiveCore >= 0) coreMhz = LiveCore;
        if (LiveMem >= 0) memMhz = LiveMem;
        CoreMhz = coreMhz;
        MemMhz = memMhz;
        Error = "";
        Status = coreMhz == 0 && memMhz == 0
            ? "已还原为 0"
            : "已下发 核心 +" + coreMhz + " · 显存 +" + memMhz + " MHz";
        return true;
    }

    public static bool ReadLive()
    {
        if (!Ensure()) return false;
        IntPtr fn = query(IdGet);
        if (fn == IntPtr.Zero) return false;
        IntPtr mem = Marshal.AllocHGlobal(StructSize);
        try
        {
            for (int i = 0; i < StructSize; i++) Marshal.WriteByte(mem, i, 0);
            Marshal.WriteInt32(mem, Ver);
            int rc = ((SetFn)Marshal.GetDelegateForFunctionPointer(fn, typeof(SetFn)))(gpu, mem);
            if (rc != 0) return false;
            LiveCore = Clamp(Marshal.ReadInt32(mem, 0x28) / 1000, 0, CoreMax);
            LiveMem = Clamp(Marshal.ReadInt32(mem, 0x54) / 1000, 0, MemMax);
            return true;
        }
        catch { return false; }
        finally { Marshal.FreeHGlobal(mem); }
    }

    public static string ApplyResultPath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
        try { Directory.CreateDirectory(dir); } catch { }
        return System.IO.Path.Combine(dir, "gpu-apply.txt");
    }

    public static void WriteApplyResult(bool ok, int core, int mem, string error, string extra)
    {
        try
        {
            File.WriteAllText(ApplyResultPath(),
                (ok ? "ok" : "fail") + "\r\n" + core + "\r\n" + mem + "\r\n" + (error ?? "") + "\r\n" + (extra ?? "") + "\r\n",
                Encoding.UTF8);
        }
        catch { }
    }

    static string Hex(int rc)
    {
        return "0x" + ((uint)rc).ToString("X8");
    }

    static bool Ensure()
    {
        if (opened && gpu != IntPtr.Zero && query != null) return true;
        try
        {
            IntPtr mod = LoadLibrary("nvapi64.dll");
            if (mod == IntPtr.Zero)
            {
                Error = "找不到 nvapi64.dll";
                Status = "没有 NVIDIA 驱动";
                return false;
            }
            IntPtr q = GetProcAddress(mod, "nvapi_QueryInterface");
            if (q == IntPtr.Zero)
            {
                Error = "nvapi_QueryInterface 不存在";
                return false;
            }
            query = (QueryFn)Marshal.GetDelegateForFunctionPointer(q, typeof(QueryFn));
            IntPtr init = query(IdInit);
            if (init == IntPtr.Zero)
            {
                Error = "NvAPI_Initialize 未导出";
                return false;
            }
            int rc = ((InitFn)Marshal.GetDelegateForFunctionPointer(init, typeof(InitFn)))();
            if (rc != 0)
            {
                Error = "NvAPI_Initialize " + Hex(rc);
                return false;
            }
            IntPtr en = query(IdEnum);
            if (en == IntPtr.Zero)
            {
                Error = "NvAPI_EnumPhysicalGPUs 未导出";
                return false;
            }
            IntPtr handles = Marshal.AllocHGlobal(64 * IntPtr.Size);
            IntPtr countPtr = Marshal.AllocHGlobal(4);
            try
            {
                for (int i = 0; i < 64 * IntPtr.Size; i++) Marshal.WriteByte(handles, i, 0);
                Marshal.WriteInt32(countPtr, 0);
                rc = ((EnumFn)Marshal.GetDelegateForFunctionPointer(en, typeof(EnumFn)))(handles, countPtr);
                int count = Marshal.ReadInt32(countPtr);
                gpu = count > 0 ? Marshal.ReadIntPtr(handles) : IntPtr.Zero;
                if (rc != 0 || count < 1 || gpu == IntPtr.Zero)
                {
                    Error = "没有枚举到独显 " + Hex(rc);
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(handles);
                Marshal.FreeHGlobal(countPtr);
            }
            if (query(IdSet) == IntPtr.Zero)
            {
                Error = "NvAPI_GPU_SetPstates20 未导出";
                return false;
            }
            opened = true;
            Ready = true;
            if (Status == "尚未下发" || Status.Length == 0) Status = "NVAPI 已就绪";
            Error = "";
            return true;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            opened = false;
            return false;
        }
    }

    static int SetDomain(int domain, int khz)
    {
        IntPtr fn = query(IdSet);
        if (fn == IntPtr.Zero) return -1;
        byte[] buf = new byte[StructSize];
        WriteI32(buf, 0x00, Ver);
        WriteI32(buf, 0x08, 1);
        WriteI32(buf, 0x0C, 1);
        WriteI32(buf, 0x1C, domain);
        WriteI32(buf, 0x28, khz);
        IntPtr mem = Marshal.AllocHGlobal(StructSize);
        try
        {
            Marshal.Copy(buf, 0, mem, StructSize);
            SetFn set = (SetFn)Marshal.GetDelegateForFunctionPointer(fn, typeof(SetFn));
            return set(gpu, mem);
        }
        finally
        {
            Marshal.FreeHGlobal(mem);
        }
    }

    static void WriteI32(byte[] b, int off, int v)
    {
        b[off] = (byte)v;
        b[off + 1] = (byte)(v >> 8);
        b[off + 2] = (byte)(v >> 16);
        b[off + 3] = (byte)(v >> 24);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr LoadLibrary(string name);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr module, string name);
}

// ============================================================
//  荣耀自己的那一组数
//  来源：C:\Program Files\HONOR\HunterCamp\Xml\HunterCampCustomMode.xml
//  GPUBase(5060/5070) 给 TGP/温度墙/CPU 功耗，GPUAdvanced 给超频。
//  营地应用一次预设时是一起下发的，这里照同样方式打包。
// ============================================================
static class HonorProfile
{
    public const int Tgp = 100;
    public const int Temp = 87;
    public const int Cpu = 140;
    public const int Core = 150;
    public const int Mem = 300;
    public const int OneCore = 200;
    public const int OneMem = 400;

    public static bool ApplyPower(Cfg cfg, out string msg)
    {
        bool ok = HonorEc.SetGpuWatt(Tgp, Temp, Cpu);
        if (ok)
        {
            cfg.Set("power", "tgp", Tgp.ToString());
            cfg.Set("power", "temp", Temp.ToString());
            cfg.Set("power", "cpu", Cpu.ToString());
            cfg.Set("power", "variant", HonorEc.WattVariant);
            cfg.Save();
            msg = "功耗墙同步荣耀数值：TGP " + Tgp + " W · 温度墙 " + Temp + " ℃ · CPU " + Cpu
                + " W（编码 " + HonorEc.WattVariant + "）";
        }
        else msg = "功耗墙没写进去：" + HonorEc.WattError;
        return ok;
    }
}

// ============================================================
//  核心：系统操作
// ============================================================
class Core
{
    public Cfg Cfg;
    public event Action<string, bool> OnLog;

    public Core(Cfg c) { Cfg = c; }

    void Log(string msg, bool ok)
    {
        Action<string, bool> h = OnLog;
        if (h != null) h(msg, ok);
    }

    static string PowerCfgPath()
    {
        return System.IO.Path.Combine(Environment.SystemDirectory, "powercfg.exe");
    }

    public static string Pc(string args)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(PowerCfgPath(), args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.Default;
            psi.StandardErrorEncoding = Encoding.Default;
            using (Process p = Process.Start(psi))
            {
                string outp = p.StandardOutput.ReadToEnd();
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                return outp + err;
            }
        }
        catch (Exception ex) { return "ERR:" + ex.Message; }
    }

    // ---------- 快照 / 恢复 ----------
    public void EnsureSnapshot()
    {
        if (Cfg.Get("snapshot", "captured", "0") == "1") return;
        Guid active = Nat.ActiveScheme();
        Guid ov;
        Nat.PowerGetActualOverlayScheme(out ov);
        Cfg.Set("snapshot", "plan", active.ToString("D"));
        Cfg.Set("snapshot", "overlay", ov.ToString("D"));
        Cfg.Set("snapshot", "refresh", Disp.CurrentHz().ToString());
        Cfg.Set("snapshot", "minanimate", AnimEnabled() ? "1" : "0");
        Cfg.Set("snapshot", "menudelay", MenuDelay().ToString());
        Cfg.Set("snapshot", "bgdisabled", BgLimited() ? "1" : "0");
        Cfg.Set("snapshot", "captured", "1");
        Cfg.Set("snapshot", "time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Cfg.Save();
        Log("已记录当前系统状态作为还原基线", true);
    }

    public void RestoreDefaults()
    {
        EnsureSnapshot();
        Guid g;
        if (Guid.TryParse(Cfg.Get("snapshot", "plan", ""), out g) && g != Guid.Empty)
        {
            if (Nat.SetActiveScheme(g)) Log("已切回原始电源方案", true);
            else Log("切回原始电源方案失败（方案可能已被删除）", false);
        }
        Guid ov;
        if (Guid.TryParse(Cfg.Get("snapshot", "overlay", ""), out ov))
        {
            if (Nat.PowerSetActiveOverlayScheme(ov) == 0) Log("已还原电源模式叠加设置", true);
        }
        int hz = Cfg.GetInt("snapshot", "refresh", 0);
        if (hz > 0)
        {
            Disp.Load();
            if (Disp.Apply(hz) == 0) Log("已还原刷新率 " + hz + " Hz", true);
        }
        SetAnimations(Cfg.Get("snapshot", "minanimate", "1") != "0", Cfg.GetInt("snapshot", "menudelay", 400));
        SetBgLimit(Cfg.Get("snapshot", "bgdisabled", "0") == "1");
        Cfg.Set("mode", "current", "custom");
        Cfg.Save();
        DisposePlans();
        Log("已恢复到初始状态", true);
    }

    void DisposePlans()
    {
        foreach (ModeDef m in Modes.All)
        {
            string s = Cfg.Get("plans", m.Key, "");
            Guid g;
            if (Guid.TryParse(s, out g) && g != Guid.Empty && g != Nat.ActiveScheme())
            {
                Pc("-delete " + g.ToString("D"));
                Cfg.Set("plans", m.Key, "");
            }
        }
        Cfg.Save();
    }

    // ---------- 电源方案 ----------
    public bool PlanExists(Guid g)
    {
        if (g == Guid.Empty) return false;
        string s = Pc("/list");
        return s.IndexOf(g.ToString("D"), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public Guid PlanFor(string key)
    {
        string s = Cfg.Get("plans", key, "");
        Guid g;
        if (Guid.TryParse(s, out g) && g != Guid.Empty && PlanExists(g)) return g;
        return Guid.Empty;
    }

    public Guid EnsurePlan(ModeDef m)
    {
        Guid g = PlanFor(m.Key);
        if (g != Guid.Empty) return g;

        string outp = Pc("-duplicatescheme " + m.Template.ToString("D"));
        Match mm = Regex.Match(outp,
            "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        if (!mm.Success)
        {
            Log("创建电源方案失败：" + outp.Trim(), false);
            return Guid.Empty;
        }
        g = new Guid(mm.Value);
        Pc("-changename " + g.ToString("D") + " \"" + m.SchemeName + "\"");
        Cfg.Set("plans", m.Key, g.ToString("D"));
        Cfg.Save();
        Log("已创建电源方案「" + m.SchemeName + "」", true);
        return g;
    }

    public string ActiveModeKey()
    {
        Guid act = Nat.ActiveScheme();
        foreach (ModeDef m in Modes.All)
        {
            string s = Cfg.Get("plans", m.Key, "");
            Guid g;
            if (Guid.TryParse(s, out g) && g == act) return m.Key;
        }
        return "custom";
    }

    public string ActivePlanName() { return Nat.FriendlyName(Nat.ActiveScheme()); }

    public int ActiveMaxAc()
    {
        uint v = Nat.ReadAc(Nat.ActiveScheme(), Nat.SubProcessor, Nat.ProcthrottleMax);
        return v == 0xFFFFFFFF ? -1 : (int)v;
    }

    public int ActiveMaxDc()
    {
        uint v = Nat.ReadDc(Nat.ActiveScheme(), Nat.SubProcessor, Nat.ProcthrottleMax);
        return v == 0xFFFFFFFF ? -1 : (int)v;
    }

    public int ActiveMinAc()
    {
        uint v = Nat.ReadAc(Nat.ActiveScheme(), Nat.SubProcessor, Nat.ProcthrottleMin);
        return v == 0xFFFFFFFF ? -1 : (int)v;
    }

    // ---------- 应用模式 ----------
    public bool ApplyMode(string key)
    {
        ModeDef m = Modes.Get(key);
        if (m == null) { Log("未知模式：" + key, false); return false; }
        EnsureSnapshot();

        Guid g = EnsurePlan(m);
        if (g == Guid.Empty) return false;

        bool sv = Nat.WriteAc(g, Nat.SubProcessor, Nat.ProcthrottleMax, m.MaxAc) &&
                  Nat.WriteDc(g, Nat.SubProcessor, Nat.ProcthrottleMax, m.MaxDc) &&
                  Nat.WriteAc(g, Nat.SubProcessor, Nat.ProcthrottleMin, m.MinAc) &&
                  Nat.WriteDc(g, Nat.SubProcessor, Nat.ProcthrottleMin, m.MinDc);
        if (!sv) Log("写入处理器上限失败，可能需要在右键菜单中选择「以管理员身份运行」", false);

        bool sa = Nat.SetActiveScheme(g);
        if (!sa) Log("切换电源方案失败，可能需要在右键菜单中选择「以管理员身份运行」", false);

        if (Nat.PowerSetActiveOverlayScheme(m.Overlay) != 0)
            Log("电源模式叠加不可用（已跳过，不影响其它设置）", false);

        SetAnimations(m.Anim, m.Anim ? 400 : 0);
        SetBgLimit(m.BgLimit);

        Cfg.Set("mode", "current", key);
        Cfg.Save();
        Log("已切换到「" + m.Name + "」模式：" + m.Tag, sv && sa);
        return sv && sa;
    }

    // ---------- 动画 / 后台 ----------
    public static bool AnimEnabled()
    {
        object v = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics", "MinAnimate", "1");
        return Convert.ToString(v) != "0";
    }

    public static int MenuDelay()
    {
        object v = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay", "400");
        int n;
        if (int.TryParse(Convert.ToString(v), out n)) return n;
        return 400;
    }

    public static bool BgLimited()
    {
        object v = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
            "GlobalUserDisabled", 0);
        try { return Convert.ToInt32(v) == 1; } catch { return false; }
    }

    public void SetAnimations(bool enabled, int menuDelay)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                if (k != null) k.SetValue("MenuShowDelay", menuDelay.ToString(), RegistryValueKind.String);
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics", true))
                if (k != null) k.SetValue("MinAnimate", enabled ? "1" : "0", RegistryValueKind.String);
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", true))
                if (k != null) k.SetValue("TaskbarAnimations", enabled ? 1 : 0, RegistryValueKind.DWord);
            Nat.Broadcast();
            Log(enabled ? "界面动画：已开启（菜单延迟 " + menuDelay + " ms）" : "界面动画：已关闭",
                true);
        }
        catch (Exception ex) { Log("修改动画设置失败：" + ex.Message, false); }
    }

    public void SetBgLimit(bool limited)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                if (k != null) k.SetValue("GlobalUserDisabled", limited ? 1 : 0, RegistryValueKind.DWord);
            Log(limited ? "后台应用：已限制" : "后台应用：已放开", true);
        }
        catch (Exception ex) { Log("修改后台应用设置失败：" + ex.Message, false); }
    }

    // ---------- 诊断 ----------
    public string StatusText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("当前电源方案: " + ActivePlanName());
        sb.AppendLine("当前模式: " + ActiveModeKey());
        sb.AppendLine("处理器上限 AC/DC: " + ActiveMaxAc() + "% / " + ActiveMaxDc() + "%");
        sb.AppendLine("处理器下限 AC: " + ActiveMinAc() + "%");
        sb.AppendLine("刷新率: " + Disp.CurrentHz() + " Hz");
        sb.AppendLine("界面动画: " + (AnimEnabled() ? "开" : "关") + " / 菜单延迟 " + MenuDelay() + "ms");
        sb.AppendLine("后台限制: " + (BgLimited() ? "开" : "关"));
        sb.AppendLine("配置: " + Cfg.Path);
        return sb.ToString();
    }
}

// ============================================================
//  显示
// ============================================================
static class Disp
{
    public static string Device = "";
    public static int W, H, Hz;
    public static readonly List<int> Rates = new List<int>();

    public static void Load()
    {
        Rates.Clear();
        string dev = null;
        Nat.DISPLAY_DEVICE dd = new Nat.DISPLAY_DEVICE();
        dd.cb = Marshal.SizeOf(typeof(Nat.DISPLAY_DEVICE));
        for (uint i = 0; Nat.EnumDisplayDevices(null, i, ref dd, 0); i++)
        {
            if ((dd.StateFlags & 0x4) != 0) { dev = dd.DeviceName; break; }
            dd.cb = Marshal.SizeOf(typeof(Nat.DISPLAY_DEVICE));
        }
        if (dev == null) return;
        Device = dev;

        Nat.DEVMODE cur = new Nat.DEVMODE();
        cur.dmSize = (short)Marshal.SizeOf(typeof(Nat.DEVMODE));
        if (!Nat.EnumDisplaySettings(dev, -1, ref cur)) return;
        W = cur.dmPelsWidth; H = cur.dmPelsHeight; Hz = cur.dmDisplayFrequency;

        Nat.DEVMODE m = new Nat.DEVMODE();
        m.dmSize = (short)Marshal.SizeOf(typeof(Nat.DEVMODE));
        for (int k = 0; Nat.EnumDisplaySettings(dev, k, ref m); k++)
        {
            if (m.dmPelsWidth != W || m.dmPelsHeight != H) continue;
            if (!Rates.Contains(m.dmDisplayFrequency)) Rates.Add(m.dmDisplayFrequency);
        }
        Rates.Sort();
    }

    public static int CurrentHz()
    {
        if (Device.Length == 0) { Load(); return Hz; }
        Nat.DEVMODE cur = new Nat.DEVMODE();
        cur.dmSize = (short)Marshal.SizeOf(typeof(Nat.DEVMODE));
        if (Nat.EnumDisplaySettings(Device, -1, ref cur)) { Hz = cur.dmDisplayFrequency; W = cur.dmPelsWidth; H = cur.dmPelsHeight; }
        return Hz;
    }

    public static int Apply(int hz)
    {
        if (Device.Length == 0) Load();
        if (Device.Length == 0) return -99;
        Nat.DEVMODE cur = new Nat.DEVMODE();
        cur.dmSize = (short)Marshal.SizeOf(typeof(Nat.DEVMODE));
        if (!Nat.EnumDisplaySettings(Device, -1, ref cur)) return -99;
        cur.dmDisplayFrequency = hz;
        cur.dmFields = 0x400000; // DM_DISPLAYFREQUENCY
        return Nat.ChangeDisplaySettingsEx(Device, ref cur, IntPtr.Zero, 0x1, IntPtr.Zero); // CDS_UPDATEREGISTRY
    }

    public static string LastError(int rc)
    {
        switch (rc)
        {
            case 0: return "成功";
            case 1: return "需要重启后生效";
            case -1: return "显示模式不支持";
            case -2: return "驱动不支持该设置";
            case -3: return "写入注册表失败";
            case -4: return "无法保存显示模式";
            default: return "失败 (rc=" + rc + ")";
        }
    }
}

// ============================================================
//  监控
// ============================================================
class Metrics
{
    public double Cpu;
    public double MemPct;
    public double MemUsedGb, MemTotalGb;
    public int Battery = -1;
    public bool Ac = true;
    public bool Charging;
    public double Mhz = -1;
    public double GpuTemp = -1;
    public double GpuUtil = -1;
    public int GpuSm = -1;
    public int GpuMemClk = -1;
    public int GpuPowerLimit = -1;
    public int GpuPowerDefault = -1;
    public int GpuPowerMax = -1;
    public string GpuName = "";

    ulong prevIdle, prevKernel, prevUser;
    bool first = true;
    PerformanceCounter perf;
    bool perfDead;
    int tick;
    int gpuBusy;

    public void Sample()
    {
        CpuSample();
        MemSample();
        BatSample();
        FreqSample();
        if (tick % 5 == 0) GpuAsync();
        tick++;
    }

    void CpuSample()
    {
        Nat.FILETIME idle, kern, user;
        if (!Nat.GetSystemTimes(out idle, out kern, out user)) return;
        ulong i = ((ulong)idle.High << 32) | idle.Low;
        ulong k = ((ulong)kern.High << 32) | kern.Low;
        ulong u = ((ulong)user.High << 32) | user.Low;
        if (first) { prevIdle = i; prevKernel = k; prevUser = u; first = false; return; }
        ulong di = i - prevIdle, dk = k - prevKernel, du = u - prevUser;
        prevIdle = i; prevKernel = k; prevUser = u;
        ulong busy = dk + du - di;
        ulong total = dk + du;
        if (total == 0) return;
        Cpu = Math.Max(0, Math.Min(100, 100.0 * busy / total));
    }

    void MemSample()
    {
        Nat.MEMORYSTATUSEX ms = new Nat.MEMORYSTATUSEX();
        ms.dwLength = (uint)Marshal.SizeOf(typeof(Nat.MEMORYSTATUSEX));
        if (!Nat.GlobalMemoryStatusEx(ref ms)) return;
        MemPct = ms.dwMemoryLoad;
        MemTotalGb = ms.ullTotalPhys / 1073741824.0;
        MemUsedGb = (ms.ullTotalPhys - ms.ullAvailPhys) / 1073741824.0;
    }

    void BatSample()
    {
        Nat.SYSTEM_POWER_STATUS s = new Nat.SYSTEM_POWER_STATUS();
        if (!Nat.GetSystemPowerStatus(ref s)) return;
        Ac = s.ACLineStatus == 1;
        Battery = s.BatteryLifePercent == 255 ? -1 : s.BatteryLifePercent;
        Charging = Ac && (s.BatteryFlag & 0x08) != 0;
    }

    void FreqSample()
    {
        if (perfDead) return;
        try
        {
            if (perf == null) perf = new PerformanceCounter("Processor Information", "Actual Frequency", "_Total");
            float f = perf.NextValue();
            if (f > 100) Mhz = f;
        }
        catch { perfDead = true; perf = null; }
    }

    void GpuAsync()
    {
        if (Interlocked.CompareExchange(ref gpuBusy, 1, 0) != 0) return;
        Thread t = new Thread(delegate()
        {
            try
            {
                string exe = System.IO.Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe");
                if (!File.Exists(exe)) { GpuTemp = -1; return; }
                ProcessStartInfo psi = new ProcessStartInfo(exe,
                    "--query-gpu=name,temperature.gpu,utilization.gpu,clocks.sm,clocks.mem,power.limit,power.default_limit,power.max_limit --format=csv,noheader,nounits");
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    string s = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(2500);
                    string[] parts = s.Trim().Split(',');
                    if (parts.Length >= 3)
                    {
                        GpuName = parts[0].Trim();
                        double v;
                        if (double.TryParse(parts[1].Trim(), out v)) GpuTemp = v;
                        if (double.TryParse(parts[2].Trim(), out v)) GpuUtil = v;
                        int clk;
                        if (parts.Length >= 5 && int.TryParse(parts[3].Trim(), out clk)) GpuSm = clk;
                        if (parts.Length >= 5 && int.TryParse(parts[4].Trim(), out clk)) GpuMemClk = clk;
                        int pw;
                        if (parts.Length >= 8)
                        {
                            GpuPowerLimit = int.TryParse(parts[5].Trim(), out pw) ? pw : -1;
                            GpuPowerDefault = int.TryParse(parts[6].Trim(), out pw) ? pw : -1;
                            GpuPowerMax = int.TryParse(parts[7].Trim(), out pw) ? pw : -1;
                        }
                    }
                }
            }
            catch { }
            finally { Interlocked.Exchange(ref gpuBusy, 0); }
        });
        t.IsBackground = true;
        t.Start();
    }
}

// ============================================================
//  控件
// ============================================================
abstract class Px : Control
{
    protected bool Hot, Down;

    public Px()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        BackColor = Th.Card;
    }

    protected override void OnMouseEnter(EventArgs e) { Hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { Hot = false; Down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { Down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { Down = false; Invalidate(); base.OnMouseUp(e); }
}

class WinBtn : Px
{
    public int Kind; // 0 = min, 1 = close
    public WinBtn()
    {
        Cursor = Cursors.Hand;
        BackColor = Th.Bar;
        TabStop = false;
        SetStyle(ControlStyles.Opaque, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Th.PaintBar(e.Graphics, 0, 0, Width, Height, Top);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        Th.PaintBar(g, 0, 0, Width, Height, Top);
        if (Hot || Down)
        {
            Color fill = Kind == 1
                ? (Down ? Th.Hex("#C93642") : Th.Hex("#E5484D"))
                : Th.Alpha(Th.Text, Down ? 40 : 26);
            Th.Fill(g, new Rectangle(1, 1, Width - 2, Height - 2), 8, fill);
        }
        Color ink = (Kind == 1 && (Hot || Down)) ? Color.White : (Hot ? Th.Text : Th.Text2);
        using (Pen p = new Pen(ink, 1.7f))
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            float cx = Width / 2f;
            float cy = Height / 2f + 0.5f;
            if (Kind == 0)
                g.DrawLine(p, cx - 5.5f, cy, cx + 5.5f, cy);
            else
            {
                float d = 4.4f;
                g.DrawLine(p, cx - d, cy - d, cx + d, cy + d);
                g.DrawLine(p, cx + d, cy - d, cx - d, cy + d);
            }
        }
    }
}

class NavBtn : Px
{
    public int Index;
    public string Label;
    public bool Active;
    public NavBtn() { BackColor = Th.Side; Height = 44; }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (Active)
        {
            Th.Fill(g, r, 9, Th.Card);
            Th.Frame(g, r, 9, Th.Stroke);
            Th.Fill(g, new Rectangle(0, Height / 2 - 9, 3, 18), 2, Th.A1);
        }
        else if (Hot)
        {
            Th.Fill(g, r, 9, Th.Alpha(Th.CardHot, 140));
        }
        Color fg = Active ? Th.Text : (Hot ? Th.Text2 : Th.Text3);
        Color ic = Active ? Th.A1 : fg;
        int cx = 30, cy = Height / 2;
        using (Pen p = new Pen(ic, 1.5f))
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            if (Index == 0)
            {
                using (SolidBrush b1 = new SolidBrush(Th.Alpha(ic, 210)))
                using (SolidBrush b2 = new SolidBrush(Th.Alpha(ic, 120)))
                {
                    g.FillRectangle(b1, cx - 7, cy - 7, 5, 5);
                    g.FillRectangle(b2, cx + 2, cy - 7, 5, 5);
                    g.FillRectangle(b2, cx - 7, cy + 2, 5, 5);
                    g.FillRectangle(b1, cx + 2, cy + 2, 5, 5);
                }
            }
            else if (Index == 1)
            {
                g.DrawLine(p, cx - 7, cy - 5, cx + 7, cy - 5);
                g.DrawLine(p, cx - 7, cy + 5, cx + 7, cy + 5);
                g.DrawLine(p, cx - 7, cy, cx + 7, cy);
                using (SolidBrush fill = new SolidBrush(Th.Side)) g.FillEllipse(fill, cx - 3, cy - 8, 6, 6);
                g.DrawEllipse(p, cx - 3, cy - 8, 6, 6);
                using (SolidBrush fill = new SolidBrush(Th.Side)) g.FillEllipse(fill, cx + 1, cy + 2, 6, 6);
                g.DrawEllipse(p, cx + 1, cy + 2, 6, 6);
            }
            else if (Index == 2)
            {
                // 电池：外壳 + 正极 + 电量条
                Rectangle body = new Rectangle(cx - 8, cy - 5, 14, 10);
                g.DrawRectangle(p, body);
                g.DrawLine(p, cx + 7, cy - 2, cx + 7, cy + 2);
                using (SolidBrush fill = new SolidBrush(Th.Alpha(ic, 200)))
                    g.FillRectangle(fill, body.X + 2, body.Y + 2, 5, body.Height - 4);
            }
            else
            {
                g.DrawRectangle(p, cx - 7, cy - 7, 10, 10);
                g.DrawRectangle(p, cx - 3, cy - 3, 10, 10);
            }
        }
        Th.TL(g, Label, Active ? Th.FBody : Th.FBody, fg, 54, 0, Width - 64, Height);
    }
}

class ModeCard : Px
{
    public ModeDef Mode;
    public bool Selected;
    public ModeCard()
    {
        BackColor = Th.Card;
        Width = 290;
        Height = 330;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);

        if (Selected)
        {
            Th.Fill(g, r, 14, Th.Alpha(Mode.C1, 16));
            Th.Fill(g, new Rectangle(1, 1, Width - 2, 4), 2, Mode.C1);
        }
        else
        {
            Th.Fill(g, r, 14, Hot ? Th.CardHot : Th.Card);
        }
        Th.FrameW(g, r, 14, Selected ? Th.Alpha(Mode.C1, 170) : (Hot ? Th.StrokeHot : Th.Stroke), Selected ? 1.6f : 1f);

        // 图标
        Rectangle ic = new Rectangle(22, 22, 44, 44);
        Th.FillGrad(g, ic, 12, Mode.C1, Mode.C2, 45f);
        DrawGlyph(g, ic);

        // 状态徽标
        if (Selected)
        {
            string badge = "当前使用";
            Size bs = Th.Measure(badge, Th.FTiny);
            Rectangle br = new Rectangle(Width - 22 - bs.Width - 18, 30, bs.Width + 18, 22);
            Th.Fill(g, br, 11, Th.Alpha(Mode.C1, 40));
            Th.TL(g, badge, Th.FTiny, Mode.C1, br.X, br.Y, br.Width, br.Height);
        }

        Th.TL(g, Mode.Name, Th.FH1, Th.Text, 22, 78, 200, 30);
        Th.TL(g, Mode.En, Th.FTiny, Th.Text3, 22, 106, 200, 16);

        Rectangle dr = new Rectangle(22, 130, Width - 44, 46);
        Th.T(g, Mode.Desc, Th.FSmall, Th.Text2, dr,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);

        for (int i = 0; i < Mode.Bullets.Length; i++)
        {
            int y = 184 + i * 21;
            Th.Dot(g, 27, y + 7, 5, Th.Alpha(Mode.C1, 200));
            Th.TL(g, Mode.Bullets[i], Th.FSmall, Th.Text2, 38, y, Width - 60, 16);
        }

        // 底部按钮
        Rectangle btn = new Rectangle(22, Height - 52, Width - 44, 34);
        if (Selected)
        {
            Th.Fill(g, btn, 9, Th.Alpha(Mode.C1, 26));
            Th.TL(g, "已应用", Th.FSmall, Mode.C1, btn.X, btn.Y, btn.Width, btn.Height);
        }
        else
        {
            if (Hot)
            {
                Th.FillGrad(g, btn, 9, Mode.C1, Mode.C2, 0f);
                Th.TL(g, "应用此模式", Th.FSmall, Color.White, btn.X, btn.Y, btn.Width, btn.Height);
            }
            else
            {
                Th.Fill(g, btn, 9, Th.Alpha(Mode.C1, 20));
                Th.Frame(g, btn, 9, Th.Alpha(Mode.C1, 80));
                Th.TL(g, "应用此模式", Th.FSmall, Mode.C1, btn.X, btn.Y, btn.Width, btn.Height);
            }
        }
    }

    void DrawGlyph(Graphics g, Rectangle ic)
    {
        Glyph.Mode(g, ic, Mode.Key, Color.White);
    }
}

static class Glyph
{
    public static void Mode(Graphics g, Rectangle ic, string key, Color fg)
    {
        int cx = ic.X + ic.Width / 2, cy = ic.Y + ic.Height / 2;
        using (SolidBrush b = new SolidBrush(fg))
        using (Pen p = new Pen(fg, 2f))
        {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
            if (key == "perf")
            {
                Point[] bolt = new Point[] {
                    new Point(cx + 2, cy - 11), new Point(cx - 7, cy + 1), new Point(cx - 1, cy + 1),
                    new Point(cx - 3, cy + 11), new Point(cx + 7, cy - 1), new Point(cx + 1, cy - 1) };
                g.DrawLines(p, bolt);
            }
            else if (key == "eco")
            {
                g.DrawRectangle(p, cx - 11, cy - 7, 20, 14);
                g.FillRectangle(b, cx + 9, cy - 3, 3, 6);
                g.FillRectangle(b, cx - 8, cy - 4, 5, 8);
            }
            else if (key == "balanced")
            {
                g.DrawLine(p, cx - 9, cy - 6, cx + 9, cy - 6);
                g.DrawLine(p, cx - 9, cy + 6, cx + 9, cy + 6);
                g.DrawLine(p, cx - 5, cy, cx + 5, cy);
            }
            else
            {
                g.DrawLine(p, cx - 8, cy - 6, cx + 8, cy - 6);
                g.DrawLine(p, cx - 8, cy + 6, cx + 8, cy + 6);
                g.DrawLine(p, cx - 2, cy - 11, cx - 2, cy - 1);
                g.DrawLine(p, cx + 4, cy + 1, cx + 4, cy + 11);
            }
        }
    }
}

class Toggle : Px
{
    bool _on;
    public event EventHandler Changed;

    public Toggle() { Width = 52; Height = 28; BackColor = Th.Card; }

    public bool On
    {
        get { return _on; }
        set { if (_on != value) { _on = value; Invalidate(); } }
    }

    protected override void OnClick(EventArgs e)
    {
        _on = !_on;
        Invalidate();
        EventHandler h = Changed;
        if (h != null) h(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle track = new Rectangle(1, 4, Width - 2, Height - 8);
        if (_on) Th.FillGrad(g, track, track.Height / 2, Th.A1, Th.A2, 0f);
        else Th.Fill(g, track, track.Height / 2, Hot ? Th.StrokeHot : Th.Stroke);
        int kx = _on ? track.Right - track.Height : track.X;
        using (SolidBrush b = new SolidBrush(_on ? Color.White : Th.Text3))
            g.FillEllipse(b, kx + 2, track.Y + 2, track.Height - 4, track.Height - 4);
    }
}

class Slider : Px
{
    int _min, _max, _val;
    bool drag;
    public event EventHandler Changed;

    public Slider() { Height = 26; BackColor = Th.Card; }

    public int Min { get { return _min; } set { _min = value; Invalidate(); } }
    public int Max { get { return _max; } set { _max = value; Invalidate(); } }
    public int Value
    {
        get { return _val; }
        set
        {
            int v = Math.Max(_min, Math.Min(_max, value));
            if (v != _val) { _val = v; Invalidate(); }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e) { drag = true; SetFromX(e.X); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { drag = false; base.OnMouseUp(e); }
    protected override void OnMouseMove(MouseEventArgs e) { if (drag) SetFromX(e.X); base.OnMouseMove(e); }

    void SetFromX(int x)
    {
        int pad = 12;
        int w = Width - pad * 2;
        int v = _min + (int)Math.Round((double)(Math.Max(0, Math.Min(w, x - pad))) / w * (_max - _min));
        if (v != _val)
        {
            _val = v;
            Invalidate();
            EventHandler h = Changed;
            if (h != null) h(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int pad = 12;
        int cy = Height / 2;
        double t = _max == _min ? 0 : (double)(_val - _min) / (_max - _min);
        int kx = pad + (int)((Width - pad * 2) * t);
        using (Pen p = new Pen(Th.Stroke, 3f)) g.DrawLine(p, pad, cy, Width - pad, cy);
        using (Pen p = new Pen(Th.A1, 3f)) g.DrawLine(p, pad, cy, kx, cy);
        using (SolidBrush b = new SolidBrush(Th.Alpha(Th.A1, 50))) g.FillEllipse(b, kx - 9, cy - 9, 18, 18);
        using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, kx - 6, cy - 6, 12, 12);
    }
}

class Chip : Px
{
    public new string Text;
    public bool Selected;
    public Chip() { BackColor = Th.Card; Height = 34; }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (Selected) Th.Fill(g, r, 8, Th.Alpha(Th.A1, 34));
        else Th.Fill(g, r, 8, Hot ? Th.CardHot : Th.Alpha(Th.Stroke, 90));
        Th.Frame(g, r, 8, Selected ? Th.Alpha(Th.A1, 160) : Th.Stroke);
        Th.TC(g, Text, Th.FSmall, Selected ? Th.A1 : (Hot ? Th.Text : Th.Text2), 0, 0, Width, Height);
    }
}

class PillBtn : Px
{
    public new string Text;
    public bool Primary;
    public Color Accent = Color.Empty;
    public PillBtn() { BackColor = Th.Card; Height = 34; }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (!Enabled)
        {
            Th.Fill(g, r, 8, Th.Alpha(Th.Stroke, 40));
            Th.Frame(g, r, 8, Th.Alpha(Th.Stroke, 80));
            Th.TC(g, Text, Th.FSmall, Th.Text3, 0, 0, Width, Height);
        }
        else if (Primary)
        {
            Th.FillGrad(g, r, 8, Down ? Th.A2 : Th.A1, Down ? Th.A1 : Th.A2, 0f);
            Th.TC(g, Text, Th.FSmall, Color.White, 0, 0, Width, Height);
        }
        else if (Accent != Color.Empty)
        {
            Th.Fill(g, r, 8, Th.Alpha(Accent, Hot ? 46 : 20));
            Th.Frame(g, r, 8, Th.Alpha(Accent, Hot ? 210 : 110));
            Th.TC(g, Text, Th.FSmall, Accent, 0, 0, Width, Height);
        }
        else
        {
            Th.Fill(g, r, 8, Hot ? Th.CardHot : Th.Alpha(Th.Stroke, 70));
            Th.Frame(g, r, 8, Hot ? Th.StrokeHot : Th.Stroke);
            Th.TC(g, Text, Th.FSmall, Hot ? Th.Text : Th.Text2, 0, 0, Width, Height);
        }
    }
}


// ============================================================
//  主窗口
// ============================================================
class MainForm : Form
{
    const int W = 1180, H = 740;
    const int TitleH = Th.BarH;
    const int SideW = 220;

    Core core;
    Metrics mon = new Metrics();
    Cfg cfg;
    System.Windows.Forms.Timer tick;
    int page = 0;

    NavBtn[] navs = new NavBtn[4];
    WinBtn winMin, winClose;
    Toggle tgAnim, tgBg, tgBoot;
    Slider slDelay;
    List<Chip> chips = new List<Chip>();
    PillBtn[] quick = new PillBtn[3];
    PillBtn[] hwFan = new PillBtn[5];
    PillBtn btnRestore;
    Slider slCore, slMem;
    PillBtn btnGpuApply, btnGpuReset, btnGpuHonor, btnGpuOne;
    Toggle tgBat, tgAuto;
    PillBtn[] batSeg = new PillBtn[3];
    int batMode = 1;
    bool batOn = true;
    bool autoMode = true;
    int autoPreset = -1;
    bool ecoForced;
    bool gpuWait;
    int gpuWaitCore, gpuWaitMem;
    DateTime gpuWaitSince;

    // 荣耀自己用的那组数，见 HonorProfile
    const int HonorTgp = HonorProfile.Tgp, HonorTemp = HonorProfile.Temp, HonorCpu = HonorProfile.Cpu;
    const int HonorCore = HonorProfile.Core, HonorMem = HonorProfile.Mem;
    const int OneCore = HonorProfile.OneCore, OneMem = HonorProfile.OneMem;

    List<string[]> logs = new List<string[]>();
    double[] hist = new double[60];
    int histCount;
    string curMode = "custom";
    bool syncing;

    public MainForm() : this(0) { }

    public MainForm(int startPage)
    {
        cfg = Cfg.Load();
        GpuOc.Load(cfg);
        HonorEc.WattTgp = Math.Max(60, Math.Min(100, cfg.GetInt("power", "tgp", 100)));
        HonorEc.WattTemp = Math.Max(75, Math.Min(87, cfg.GetInt("power", "temp", 87)));
        HonorEc.WattCpu = Math.Max(30, Math.Min(140, cfg.GetInt("power", "cpu", 140)));
        HonorEc.WattPrefer = HonorEc.VariantIndex(cfg.Get("power", "variant", ""));
        batMode = Math.Max(0, Math.Min(2, cfg.GetInt("battery", "mode", 1)));
        batOn = cfg.Get("battery", "on", "1") != "0";
        autoMode = cfg.Get("auto", "on", "1") != "0";
        autoPreset = cfg.GetInt("auto", "preset", -1);
        SyncAutoKeep();
        core = new Core(cfg);
        core.OnLog += AppendLog;
        core.EnsureSnapshot();

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(W, H);
        BackColor = Th.Bg;
        Text = "荣耀性能调控";
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        DoubleBuffered = true;
        KeyPreview = true;

        BuildChrome();
        BuildPage0();
        BuildPage2();
        BuildGpuPage();
        BuildBatteryPage();
        BuildRestore();

        Disp.Load();
        mon.Sample();
        HonorEc.Refresh();
        SeedKeep();
        SyncStartup();
        HighlightHonor();
        SyncAll();
        NoteResume();

        AppendLog("启动完成 · " + ModelName() + " · " + DateTime.Now.ToString("HH:mm"), true);

        if (!Env.Writable) AppendLog("检测到受限运行环境，修改可能不会生效", false);

        // Win11 用 DWM 圆角（抗锯齿），拿不到再退回区域裁剪
        dwmRound = Th.RoundWindow(this);
        if (!dwmRound) Th.ClipWindow(this, Th.WinRad);
        else { Th.CornerRadius = 8; Region = null; }

        tick = new System.Windows.Forms.Timer();
        tick.Interval = 1000;
        tick.Tick += OnTick;
        tick.Start();

        page = startPage;
        ShowPage(startPage);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!Env.Writable) BeginInvoke(new MethodInvoker(ShowLowIntegrityWarning));
    }

    void ShowLowIntegrityWarning()
    {
        MessageBox.Show(
            "当前程序运行在受限环境（进程令牌为低完整性级别），无法写入配置文件，也无法修改电源方案与注册表。\r\n\r\n" +
            "常见原因：程序位于带沙箱标记的工作区目录（该目录带 Low 完整性标签）。\r\n" +
            "解决办法：把 HonorTune.exe 复制到普通文件夹（例如 D:\\Tools\\HonorTune\\），再从那里运行。\r\n\r\n" +
            "本次仍可查看监控数据，但「应用模式」等修改会失败。",
            "荣耀性能调控 · 权限受限", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void OnTick(object s, EventArgs e)
    {
        mon.Sample();
        if (histCount % 2 == 0) HonorEc.Refresh();
        if (page == 1 && histCount % 2 == 0) PollGpu();
        AutoTick();
        HighlightHonor();
        hist[histCount % hist.Length] = mon.Cpu;
        histCount++;
        Invalidate();
        int idx = histCount % 60;
        if (idx % 10 == 0) RecheckMode();
    }

    void RecheckMode()
    {
        string m = core.ActiveModeKey();
        if (m != curMode)
        {
            curMode = m;
            Invalidate();
        }
    }

    // ---------- 构建 ----------
    void BuildChrome()
    {
        winClose = new WinBtn();
        winClose.Kind = 1;
        winClose.Bounds = new Rectangle(W - 46, 12, 34, 30);
        winClose.Click += delegate { Close(); };
        Controls.Add(winClose);

        winMin = new WinBtn();
        winMin.Kind = 0;
        winMin.Bounds = new Rectangle(W - 84, 12, 34, 30);
        winMin.Click += delegate { WindowState = FormWindowState.Minimized; };
        Controls.Add(winMin);

        string[] names = new string[] { "概览", "GPU 超频", "电池保护", "细节调节" };
        for (int i = 0; i < navs.Length; i++)
        {
            NavBtn b = new NavBtn();
            b.Index = i;
            b.Label = names[i];
            b.Bounds = new Rectangle(12, 76 + i * 50, SideW - 30, 44);
            int idx = i;
            b.Click += delegate { ShowPage(idx); };
            navs[i] = b;
            Controls.Add(b);
        }

        tgBoot = new Toggle();
        tgBoot.BackColor = Th.Side;
        tgBoot.Bounds = new Rectangle(SideW - 52 - 16, H - 176, 52, 28);
        tgBoot.Changed += delegate
        {
            if (syncing) return;
            SetStartup(tgBoot.On);
        };
        Controls.Add(tgBoot);
    }

    void BuildPage0()
    {
        int[] modes = new int[] { 0, 1, 3 };
        string[] t = new string[] { "智能档", "性能档", "高能档" };
        Color[] colors = new Color[] { Th.Ok, Th.A1, Th.Warn };
        for (int i = 0; i < 3; i++)
        {
            PillBtn p = new PillBtn();
            p.Text = t[i];
            p.Accent = colors[i];
            p.Bounds = new Rectangle(268 + i * 138, 248, 120, 34);
            p.Tag = modes[i];
            p.Click += HwModeClick;
            quick[i] = p;
            Controls.Add(p);
        }
        string[] fans = new string[] { "静音", "偏静", "标准", "偏冷", "强冷" };
        for (int i = 0; i < 5; i++)
        {
            PillBtn p = new PillBtn();
            p.Text = fans[i];
            p.Bounds = new Rectangle(268 + i * 82, 352, 70, 32);
            p.Tag = i;
            p.Click += HwFanClick;
            hwFan[i] = p;
            Controls.Add(p);
        }
    }

    void BuildPage2()
    {
        tgAnim = new Toggle();
        tgAnim.Bounds = new Rectangle(1154 - 52 - 22, 268 + 54, 52, 28);
        tgAnim.Changed += delegate { if (syncing) return; core.SetAnimations(tgAnim.On, slDelay.Value); SyncAll(); };
        Controls.Add(tgAnim);

        slDelay = new Slider();
        slDelay.Bounds = new Rectangle(246 + 330, 268 + 88, 400, 26);
        slDelay.Min = 0;
        slDelay.Max = 400;
        slDelay.Value = 400;
        slDelay.Changed += delegate { if (syncing) return; core.SetAnimations(tgAnim.On, slDelay.Value); Invalidate(); };
        Controls.Add(slDelay);

        tgBg = new Toggle();
        tgBg.Bounds = new Rectangle(1154 - 52 - 22, 420 + 54, 52, 28);
        tgBg.Changed += delegate { if (syncing) return; core.SetBgLimit(tgBg.On); };
        Controls.Add(tgBg);

        for (int i = 0; i < 7; i++)
        {
            Chip c = new Chip();
            c.Bounds = new Rectangle(246 + 22 + i * 86, 132 + 74, 78, 34);
            c.Visible = false;
            chips.Add(c);
            Controls.Add(c);
        }
    }

    void BuildGpuPage()
    {
        slCore = new Slider();
        slCore.Bounds = new Rectangle(324, 322, 560, 26);
        slCore.Min = 0;
        slCore.Max = GpuOc.CoreMax;
        slCore.Value = GpuOc.CoreMhz;
        slCore.Changed += delegate { if (!syncing) Invalidate(); };
        Controls.Add(slCore);

        slMem = new Slider();
        slMem.Bounds = new Rectangle(324, 368, 560, 26);
        slMem.Min = 0;
        slMem.Max = GpuOc.MemMax;
        slMem.Value = GpuOc.MemMhz;
        slMem.Changed += delegate { if (!syncing) Invalidate(); };
        Controls.Add(slMem);

        btnGpuApply = new PillBtn();
        btnGpuApply.Text = "应用偏移";
        btnGpuApply.Primary = true;
        btnGpuApply.Bounds = new Rectangle(268, 414, 128, 34);
        btnGpuApply.Click += GpuApplyClick;
        Controls.Add(btnGpuApply);

        btnGpuReset = new PillBtn();
        btnGpuReset.Text = "还原为 0";
        btnGpuReset.Bounds = new Rectangle(408, 414, 128, 34);
        btnGpuReset.Click += GpuResetClick;
        Controls.Add(btnGpuReset);

        btnGpuHonor = new PillBtn();
        btnGpuHonor.Text = "荣耀 " + HonorCore + "/" + HonorMem;
        btnGpuHonor.Bounds = new Rectangle(548, 414, 150, 34);
        btnGpuHonor.Click += GpuHonorClick;
        Controls.Add(btnGpuHonor);

        btnGpuOne = new PillBtn();
        btnGpuOne.Text = "一键 " + OneCore + "/" + OneMem;
        btnGpuOne.Bounds = new Rectangle(708, 414, 150, 34);
        btnGpuOne.Click += GpuOneClick;
        Controls.Add(btnGpuOne);
    }

    void GpuHonorClick(object s, EventArgs e)
    {
        if (syncing) return;
        syncing = true;
        slCore.Value = HonorCore;
        slMem.Value = HonorMem;
        syncing = false;
        AppendLog("已填入荣耀自定义档：核心 +" + HonorCore + " · 显存 +" + HonorMem + " MHz，点「应用偏移」写入", true);
        Invalidate();
    }

    void GpuOneClick(object s, EventArgs e)
    {
        if (syncing) return;
        syncing = true;
        slCore.Value = OneCore;
        slMem.Value = OneMem;
        syncing = false;
        AppendLog("已填入荣耀一键档：核心 +" + OneCore + " · 显存 +" + OneMem + " MHz，点「应用偏移」写入", true);
        Invalidate();
    }

    void GpuApplyClick(object s, EventArgs e)
    {
        if (syncing) return;
        int core = slCore.Value;
        int mem = slMem.Value;
        if (!Elev.IsAdmin())
        {
            try
            {
                gpuWait = true;
                gpuWaitCore = core;
                gpuWaitMem = mem;
                gpuWaitSince = DateTime.Now;
                Elev.Relaunch("--gpu-oc " + core + " " + mem);
                AppendLog("请在 UAC 中允许，才会把核心 +" + core + "、显存 +" + mem + " MHz 写进驱动", true);
            }
            catch
            {
                gpuWait = false;
                AppendLog("未获得管理员权限，偏移没有写入", false);
            }
            Invalidate();
            return;
        }
        bool ok = GpuOc.Apply(core, mem);
        if (ok)
        {
            syncing = true;
            slCore.Value = GpuOc.CoreMhz;
            slMem.Value = GpuOc.MemMhz;
            syncing = false;
            RememberGpu(GpuOc.CoreMhz, GpuOc.MemMhz);
        }
        AppendLog(ok ? GpuOc.Status : ("GPU 偏移失败：" + GpuOc.Error), ok);
        if (ok && (core > 0 || mem > 0))
        {
            string msg;
            bool pok = HonorProfile.ApplyPower(cfg, out msg);
            AppendLog(msg, pok);
        }
        Invalidate();
    }

    void PollGpu()
    {
        GpuOc.ReadLive();
        if (!gpuWait) return;
        if ((DateTime.Now - gpuWaitSince).TotalSeconds > 90)
        {
            gpuWait = false;
            AppendLog("UAC 等待超时，偏移可能没写进去", false);
            return;
        }
        string path = GpuOc.ApplyResultPath();
        if (!File.Exists(path) || File.GetLastWriteTime(path) < gpuWaitSince.AddSeconds(-1)) return;
        string[] lines;
        try { lines = File.ReadAllLines(path, Encoding.UTF8); }
        catch { return; }
        gpuWait = false;
        bool ok = lines.Length > 0 && lines[0] == "ok";
        int c, m;
        if (ok && lines.Length > 2 && int.TryParse(lines[1], out c) && int.TryParse(lines[2], out m))
        {
            syncing = true;
            slCore.Value = c;
            slMem.Value = m;
            syncing = false;
            cfg.Set("gpu", "core", c.ToString());
            cfg.Set("gpu", "mem", m.ToString());
            AppendLog("已通过 UAC 写入 核心 +" + c + " · 显存 +" + m + " MHz", true);
            if (lines.Length > 4 && lines[4].Trim().Length > 0)
                AppendLog(lines[4].Trim(), lines[4].IndexOf("没写进去") < 0);
        }
        else
        {
            string err = lines.Length > 3 ? lines[3] : "未知错误";
            AppendLog("UAC 后写入失败：" + err, false);
        }
    }

    void RememberGpu(int core, int mem)
    {
        cfg.Set("gpu", "core", core.ToString());
        cfg.Set("gpu", "mem", mem.ToString());
        cfg.Save();
        GpuOc.CoreMhz = core;
        GpuOc.MemMhz = mem;
        Boot.PublishKeep(cfg.GetInt("keep", "mode", HonorEc.Mode), cfg.GetInt("keep", "fan", HonorEc.FanIndex),
            cfg.Get("keep", "startup", "1") != "0");
    }

    void GpuResetClick(object s, EventArgs e)
    {
        if (syncing) return;
        syncing = true;
        slCore.Value = 0;
        slMem.Value = 0;
        syncing = false;
        GpuApplyClick(s, e);
    }

    void BuildBatteryPage()
    {
        tgBat = new Toggle();
        tgBat.Bounds = new Rectangle(1080, 262, 52, 28);
        tgBat.Changed += delegate
        {
            if (syncing) return;
            batOn = tgBat.On;
            ApplyBattery();
        };
        Controls.Add(tgBat);

        string[] names = new string[] { "家用 70%", "办公 90%", "差旅 100%" };
        for (int i = 0; i < 3; i++)
        {
            PillBtn p = new PillBtn();
            p.Text = names[i];
            p.Bounds = new Rectangle(268 + i * 288, 352, 260, 34);
            p.Tag = i;
            p.Click += BatClick;
            batSeg[i] = p;
            Controls.Add(p);
        }

        tgAuto = new Toggle();
        tgAuto.Bounds = new Rectangle(1080, 462, 52, 28);
        tgAuto.Changed += delegate
        {
            if (syncing) return;
            autoMode = tgAuto.On;
            cfg.Set("auto", "on", autoMode ? "1" : "0");
            cfg.Save();
            ecoForced = false;
            SyncAutoKeep();
          };
        Controls.Add(tgAuto);
    }

    void BatClick(object s, EventArgs e)
    {
        if (syncing) return;
        batMode = (int)((Control)s).Tag;
        if (!batOn)
        {
            batOn = true;
            syncing = true;
            tgBat.On = true;
            syncing = false;
        }
        ApplyBattery();
    }

    void ApplyBattery()
    {
        int stop = batOn ? HonorEc.BatStopPreset[batMode] : 100;
        int resume = batOn ? HonorEc.BatResumePreset[batMode] : 95;
        bool ok = HonorEc.SetBatteryLimit(stop, resume);
        if (ok)
        {
            cfg.Set("battery", "mode", batMode.ToString());
            cfg.Set("battery", "on", batOn ? "1" : "0");
            cfg.Save();
            AppendLog(batOn
                ? "充电阈值已设为 上限 " + stop + "% · 掉到 " + resume + "% 再充"
                : "已关闭充电上限，充到 100%", true);
        }
        else AppendLog("充电阈值写入失败：" + HonorEc.BatError, false);
        SyncBattery();
        Invalidate();
    }

    void SyncBattery()
    {
        HonorEc.ReadBatteryLimit();
        syncing = true;
        tgBat.On = batOn;
        tgAuto.On = autoMode;
        for (int i = 0; i < batSeg.Length; i++)
        {
            batSeg[i].Primary = i == batMode && batOn;
            batSeg[i].Invalidate();
        }
        syncing = false;
    }

    void SyncAutoKeep()
    {
        Boot.Preset = autoPreset;
        Boot.AutoOn = autoMode ? 1 : 0;
        Boot.PublishKeep(cfg.GetInt("keep", "mode", HonorEc.Mode),
            cfg.GetInt("keep", "fan", HonorEc.FanIndex),
            cfg.Get("keep", "startup", "1") != "0");
    }

    // 不插电强制智能档；插电且电量 > 20% 切回用户之前选的档位
    void AutoTick()
    {
        if (!autoMode) return;
        PowerStatus ps = SystemInformation.PowerStatus;
        bool ac = ps.PowerLineStatus == PowerLineStatus.Online;
        int pct = (int)Math.Round(ps.BatteryLifePercent * 100);

        if (!ac)
        {
            if (!ecoForced)
            {
                if (HonorEc.Mode == 1 || HonorEc.Mode == 3)
                {
                    autoPreset = HonorEc.Mode;
                    cfg.Set("auto", "preset", autoPreset.ToString());
                    cfg.Save();
                    SyncAutoKeep();
                }
                ecoForced = true;
            }
            if (HonorEc.Mode != 0 && HonorEc.SetMode(0))
            {
                Remember(0, -1);
                AppendLog("未接电源，已切到智能档", true);
                HighlightHonor();
            }
            return;
        }

        if (ecoForced && pct > 20)
        {
            ecoForced = false;
            int back = (autoPreset == 1 || autoPreset == 3) ? autoPreset : 1;
            if (HonorEc.Mode != back)
            {
                int fan = back == 3 ? cfg.GetInt("keep", "fan", HonorEc.FanIndex) : -1;
                ApplyHonorChoice(back, fan);
                Remember(back, fan);
                AppendLog("已接电源且电量 " + pct + "%，切回" + (back == 1 ? "性能档" : "高能档"), true);
                HighlightHonor();
            }
        }
    }

    void BuildRestore()
    {
        btnRestore = new PillBtn();
        btnRestore.Text = "恢复到初始状态";
        btnRestore.Bounds = new Rectangle(1154 - 160, 688, 160, 34);
        btnRestore.Click += delegate { Restore(); };
        Controls.Add(btnRestore);
    }

    // 荣耀档位对应的 Windows 电源计划：智能档走平衡，性能/高能走 Honor Performance
    static readonly Guid PlanBalance = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
    static readonly Guid PlanHigh = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    // 厂商插件 HonorPowerPlan::EnterHighPerfMode 里写死的 Honor Performance 方案
    static readonly Guid PlanHonorOem = new Guid("b8a2c9f4-7d3e-4a1b-9c2f-5e8d6a3b1c4f");
    // 厂商模板 HonorPowerPlan.pow 里自带的方案 GUID
    static readonly Guid PlanHonorTpl = new Guid("435f715e-6820-4e83-89e5-8ae5f5db032e");

    static bool PlanExists(Guid g)
    {
        try
        {
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\" + g.ToString("D")))
                return k != null;
        }
        catch { return false; }
    }

    // 方案名可能是 "Honor Performance" 或 "Honor Performance Plan" 之类，
    // 要求同时出现 Honor 和 Performance，避免误匹配本程序自己建的“荣耀调控”方案
    static bool NameMatches(string fn, string want)
    {
        fn = fn.Trim();
        if (fn.Equals(want, StringComparison.OrdinalIgnoreCase)) return true;
        return fn.IndexOf("Honor", StringComparison.OrdinalIgnoreCase) >= 0
            && fn.IndexOf("Performance", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // 各机型的 Honor Performance 方案 GUID 不固定（本机就是本机生成的），所以按名字找
    static Guid FindPlanByName(string want)
    {
        try
        {
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes"))
            {
                if (k == null) return Guid.Empty;
                foreach (string name in k.GetSubKeyNames())
                {
                    if (name.Length != 36) continue;
                    Guid g;
                    if (!Guid.TryParse(name, out g)) continue;
                    using (RegistryKey s = k.OpenSubKey(name))
                    {
                        if (s == null) continue;
                        object v = s.GetValue("FriendlyName");
                        if (v == null) continue;
                        string fn = v.ToString();
                        int c = fn.LastIndexOf(',');
                        if (c >= 0) fn = fn.Substring(c + 1);
                        if (NameMatches(fn, want)) return g;
                    }
                }
            }
        }
        catch { }
        return Guid.Empty;
    }

    static Guid HonorPlan()
    {
        // 厂商插件 PerfCommonPlugin.dll 里 HonorPowerPlan::EnterHighPerfMode 写死的方案
        if (PlanExists(PlanHonorOem)) return PlanHonorOem;
        Guid g = FindPlanByName("Honor Performance");
        if (g != Guid.Empty) return g;
        if (PlanExists(PlanHonorTpl)) return PlanHonorTpl;
        return PlanHigh;
    }

    void HwModeClick(object s, EventArgs e)
    {
        int mode = (int)((Control)s).Tag;
        if (mode != 0 && !OnAcPower())
        {
            AppendLog("未接电源时只能选智能档，插上电源再切", false);
            HighlightHonor();
            return;
        }
        int fan = cfg.GetInt("keep", "fan", HonorEc.FanIndex);
        string msg = ApplyHonorChoice(mode, mode == 3 ? fan : -1);
        bool ok = HonorEc.Mode == mode;
        if (ok) Remember(mode, mode == 3 ? fan : -1);
        if (ok && (mode == 1 || mode == 3))
        {
            autoPreset = mode;
            cfg.Set("auto", "preset", autoPreset.ToString());
            cfg.Save();
            SyncAutoKeep();
        }
        AppendLog(msg, ok);
        HighlightHonor();
        Invalidate();
    }

    void HwFanClick(object s, EventArgs e)
    {
        if (HonorEc.Mode != 3)
        {
            AppendLog("风扇档位只在高能模式下可调整", false);
            return;
        }
        int idx = (int)((Control)s).Tag;
        bool ok = HonorEc.SetFanIndex(idx);
        string msg = ok ? "已下发风扇转速档位 " + (idx + 1) + "/5"
                        : "下发风扇转速档位失败 " + (idx + 1) + "/5";
        if (!ok && HonorEc.Error.Length > 0) msg += "：" + HonorEc.Error;
        if (ok) Remember(3, idx);
        AppendLog(msg, ok);
        HighlightHonor();
        Invalidate();
    }

    // 登录后重选上次档位。fan < 0 表示这次不动风扇。
    public static string ApplyHonorChoice(int mode, int fan)
    {
        if (mode != 0 && mode != 1 && mode != 3) return "没有可重选的性能档";
        bool ok = HonorEc.SetMode(mode);
        HonorEc.Refresh();
        string msg = (ok ? "已重选性能档：" : "重选性能档失败：") + HonorEc.ModeName(mode);
        if (!ok && HonorEc.Error.Length > 0) msg += "：" + HonorEc.Error;
        if (!ok) return msg;
        Guid want = mode == 0 ? PlanBalance : HonorPlan();
        bool planOk = Nat.ActiveScheme() == want || Nat.SetActiveScheme(want);
        msg += planOk ? "，电源计划已对齐" : "，电源计划切换失败";
        if (mode == 3 && fan >= 0 && fan <= 4)
        {
            bool fanOk = HonorEc.SetFanIndex(fan);
            msg += fanOk ? "，风扇 " + (fan + 1) + "/5" : "，风扇未生效";
            if (!fanOk && HonorEc.Error.Length > 0) msg += "（" + HonorEc.Error + "）";
        }
        return msg;
    }

    void Remember(int mode, int fan)
    {
        if (mode == 0 || mode == 1 || mode == 3)
            cfg.Set("keep", "mode", mode.ToString());
        if (fan >= 0 && fan <= 4)
            cfg.Set("keep", "fan", fan.ToString());
        cfg.Save();
        Boot.PublishKeep(cfg.GetInt("keep", "mode", -1), cfg.GetInt("keep", "fan", -1), cfg.Get("keep", "startup", "1") != "0");
    }

    void SeedKeep()
    {
        if (cfg.Get("keep", "mode", "").Length == 0 &&
            (HonorEc.Mode == 0 || HonorEc.Mode == 1 || HonorEc.Mode == 3))
            cfg.Set("keep", "mode", HonorEc.Mode.ToString());
        if (cfg.Get("keep", "fan", "").Length == 0 && HonorEc.FanIndex >= 0 && HonorEc.FanIndex <= 4)
            cfg.Set("keep", "fan", HonorEc.FanIndex.ToString());
        if (cfg.Get("keep", "startup", "").Length == 0)
            cfg.Set("keep", "startup", "1");
        cfg.Save();
        Boot.PublishKeep(cfg.GetInt("keep", "mode", -1), cfg.GetInt("keep", "fan", -1), cfg.Get("keep", "startup", "1") != "0");
    }

    void SyncStartup()
    {
        syncing = true;
        tgBoot.On = Boot.ServiceInstalled();
        syncing = false;
    }

    void SetStartup(bool on)
    {
        cfg.Set("keep", "startup", on ? "1" : "0");
        cfg.Save();
        Boot.PublishKeep(cfg.GetInt("keep", "mode", HonorEc.Mode), cfg.GetInt("keep", "fan", HonorEc.FanIndex), on);
        if (Elev.IsAdmin())
        {
            bool ok = on ? Boot.InstallService() : Boot.RemoveService();
            syncing = true;
            tgBoot.On = Boot.ServiceInstalled();
            syncing = false;
            AppendLog(ok
                ? (on ? "已安装后台服务：掉档会重设，不会打开主窗口" : "已停止并移除后台服务")
                : "服务安装失败，请在 UAC 中允许后再试", ok);
            return;
        }
        try
        {
            Elev.Relaunch(on ? "--install-service" : "--remove-service");
            AppendLog(on ? "请在 UAC 中允许安装后台服务，主窗口不会开机自启" : "请在 UAC 中允许移除后台服务", true);
        }
        catch
        {
            syncing = true;
            tgBoot.On = Boot.ServiceInstalled();
            syncing = false;
            AppendLog("未获得管理员权限，服务未更改", false);
        }
    }

    void NoteResume()
    {
        try
        {
            string p = System.IO.Path.Combine(Boot.DataDir(), "service.log");
            if (!File.Exists(p)) return;
            if ((DateTime.Now - File.GetLastWriteTime(p)).TotalHours > 18) return;
            string[] lines = File.ReadAllLines(p, Encoding.UTF8);
            string last = "";
            for (int i = lines.Length - 1; i >= 0; i--)
                if (lines[i].Trim().Length > 0) { last = lines[i].Trim(); break; }
            if (last.Length > 0)
                AppendLog("开机保持 · " + last, last.IndexOf("失败") < 0 && last.IndexOf("未生效") < 0);
        }
        catch { }
    }

    bool OnAcPower()
    {
        return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
    }

    void HighlightHonor()
    {
        int[] qModes = new int[] { 0, 1, 3 };
        bool ac = OnAcPower();
        for (int i = 0; i < 3; i++)
        {
            if (quick[i] == null) continue;
            quick[i].Primary = HonorEc.Mode == qModes[i];
            // 未接电源时不给选性能档和高能档
            quick[i].Enabled = ac || qModes[i] == 0;
            quick[i].Invalidate();
        }
        bool fanOn = HonorEc.Mode == 3 && HonorEc.FanTableOk;
        for (int i = 0; i < 5; i++)
        {
            hwFan[i].Enabled = fanOn;
            hwFan[i].Primary = fanOn && HonorEc.FanIndex == i;
            hwFan[i].Invalidate();
        }
    }

    // ---------- 页面 ----------
    void ShowPage(int idx)
    {
        page = idx;
        for (int i = 0; i < navs.Length; i++) { navs[i].Active = i == idx; navs[i].Invalidate(); }
        for (int i = 0; i < 5; i++) hwFan[i].Visible = idx == 0;
        quick[0].Visible = quick[1].Visible = quick[2].Visible = idx == 0;
        bool detail = idx == 3;
        tgAnim.Visible = tgBg.Visible = slDelay.Visible = detail;
        foreach (Chip c in chips) c.Visible = false;
        if (detail) BuildRateChips();
        btnRestore.Visible = detail;
        bool gpu = idx == 1;
        slCore.Visible = slMem.Visible = btnGpuApply.Visible = btnGpuReset.Visible = gpu;
        btnGpuHonor.Visible = btnGpuOne.Visible = gpu;
        if (gpu) { GpuOc.Probe(); GpuOc.ReadLive(); }
        bool bat = idx == 2;
        tgBat.Visible = bat;
        tgAuto.Visible = bat;
        for (int i = 0; i < batSeg.Length; i++) batSeg[i].Visible = bat;
        if (bat) SyncBattery();
        Invalidate();
    }

    // ---------- 操作 ----------
    void SyncAll()
    {
        syncing = true;
        tgAnim.On = Core.AnimEnabled();
        slDelay.Value = Core.MenuDelay();
        tgBg.On = Core.BgLimited();
        curMode = core.ActiveModeKey();
        BuildRateChips();
        syncing = false;
        Invalidate();
    }

    void BuildRateChips()
    {
        Disp.Load();
        for (int i = 0; i < chips.Count; i++) chips[i].Visible = false;
        int n = Math.Min(chips.Count, Disp.Rates.Count);
        // 按实际数量在「显示」卡片里居中排布
        int cw = 78, gap = 10;
        int total = n * cw + (n - 1) * gap;
        int startX = 246 + (908 - total) / 2;
        for (int i = 0; i < n; i++)
        {
            int hz = Disp.Rates[Disp.Rates.Count - 1 - i];
            Chip c = chips[i];
            c.Bounds = new Rectangle(startX + i * (cw + gap), 206, cw, 34);
            c.Text = hz + " Hz";
            c.Selected = hz == Disp.Hz;
            c.Visible = page == 3;
            int target = hz;
            c.Click -= ChipClick;
            c.Click += ChipClick;
            c.Tag = target;
        }
    }

    void ChipClick(object s, EventArgs e)
    {
        if (syncing) return;
        int hz = (int)((Control)s).Tag;
        Disp.Load();
        if (hz == Disp.Hz) return;
        int before = Disp.Hz;
        int rc = Disp.Apply(hz);
        if (rc != 0)
        {
            MessageBox.Show("切换刷新率失败：" + Disp.LastError(rc), "荣耀性能调控",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SyncAll();
            return;
        }
        Disp.Load();
        if (!ConfirmKeep("刷新率已切换为 " + hz + " Hz"))
        {
            Disp.Apply(before);
            Disp.Load();
            AppendLog("刷新率设置已撤销（" + before + " Hz）", false);
        }
        else
        {
            AppendLog("刷新率已设置为 " + hz + " Hz", true);
        }
        SyncAll();
    }

    bool ConfirmKeep(string msg)
    {
        using (Form d = new Form())
        {
            d.FormBorderStyle = FormBorderStyle.None;
            d.StartPosition = FormStartPosition.CenterScreen;
            d.ClientSize = new Size(400, 150);
            d.BackColor = Th.Card;
            if (!Th.RoundWindow(d)) Th.ClipWindow(d, 12);
            d.TopMost = true;
            d.ShowInTaskbar = false;

            Label l = new Label();
            l.Text = msg + "\r\n\r\n是否保留此设置？";
            l.ForeColor = Th.Text;
            l.Font = Th.FBody;
            l.Bounds = new Rectangle(24, 22, 352, 56);
            d.Controls.Add(l);

            Label cd = new Label();
            cd.Text = "15 秒后自动还原";
            cd.ForeColor = Th.Text3;
            cd.Font = Th.FTiny;
            cd.Bounds = new Rectangle(24, 78, 200, 18);
            d.Controls.Add(cd);

            bool keep = false;
            Button ok = new Button();
            ok.Text = "保留";
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderColor = Th.A1;
            ok.BackColor = Th.A1;
            ok.ForeColor = Color.White;
            ok.Bounds = new Rectangle(220, 100, 70, 30);
            ok.Click += delegate { keep = true; d.Close(); };
            d.Controls.Add(ok);

            Button no = new Button();
            no.Text = "还原";
            no.FlatStyle = FlatStyle.Flat;
            no.FlatAppearance.BorderColor = Th.StrokeHot;
            no.BackColor = Th.CardHot;
            no.ForeColor = Th.Text2;
            no.Bounds = new Rectangle(300, 100, 70, 30);
            no.Click += delegate { keep = false; d.Close(); };
            d.Controls.Add(no);

            int left = 15;
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 1000;
            t.Tick += delegate
            {
                left--;
                cd.Text = left + " 秒后自动还原";
                if (left <= 0) { t.Stop(); keep = false; d.Close(); }
            };
            t.Start();
            d.ShowDialog(this);
            t.Stop();
            return keep;
        }
    }

    void Restore()
    {
        if (MessageBox.Show("将把电源方案、刷新率、动画与后台限制恢复到本应用首次运行前的状态，并删除本应用创建的三个电源方案。\r\n\r\n确定继续？",
            "恢复到初始状态", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        core.RestoreDefaults();
        SyncAll();
    }

    // ---------- 日志 ----------
    void AppendLog(string msg, bool ok)
    {
        logs.Add(new string[] { DateTime.Now.ToString("HH:mm:ss"), msg, ok ? "1" : "0" });
        if (logs.Count > 50) logs.RemoveAt(0);
        Invalidate();
    }

    // ---------- 系统信息 ----------
    static string ModelName()
    {
        try
        {
            object m = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", "");
            object v = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", "");
            string s = Convert.ToString(v) + " " + Convert.ToString(m);
            s = s.Trim();
            if (s.Length == 0) return "未知机型";
            if (s.Length > 26) s = s.Substring(0, 26) + "…";
            return s;
        }
        catch { return "未知机型"; }
    }

    // ---------- 绘制 ----------
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        g.Clear(Th.Bg);

        // 顶栏
        Th.PaintBar(g, 0, 0, W, TitleH, 0);
        using (Pen p = new Pen(Th.Stroke)) g.DrawLine(p, 0, TitleH, W, TitleH);
        using (Pen p = new Pen(Th.Stroke)) g.DrawLine(p, SideW, TitleH, SideW, H);

        // 侧栏底
        using (SolidBrush b = new SolidBrush(Th.Side)) g.FillRectangle(b, 0, TitleH, SideW, H - TitleH);

        // Logo
        Rectangle lg = new Rectangle(22, 14, 26, 26);
        Th.FillGrad(g, lg, 7, Th.A1, Th.A2, 45f);
        using (SolidBrush b = new SolidBrush(Color.White))
        {
            Point[] bolt = new Point[] { new Point(36, 19), new Point(30, 27), new Point(35, 27),
                new Point(33, 35), new Point(41, 25), new Point(36, 25), new Point(38, 19) };
            g.FillPolygon(b, bolt);
        }
        Th.TL(g, "荣耀性能调控", Th.FTitle3, Th.Text, 58, 12, 400, 20);
        Th.TL(g, "HONOR TUNE", Th.FTiny, Th.Text3, 58, 30, 400, 14);

        // 侧栏：后台服务，不启动主窗口
        Th.TL(g, "后台服务", Th.FSmall, Th.Text2, 18, H - 176, 120, 20);
        Th.TL(g, "掉档就重设", Th.FTiny, Th.Text3, 18, H - 156, 120, 16);

        // 侧栏底部
        Th.TL(g, ModelName(), Th.FTiny, Th.Text3, 22, H - 92, SideW - 40, 16);
        Th.TL(g, "v1.4.0 · 荣耀性能调整", Th.FTiny, Th.Alpha(Th.Text3, 160), 22, H - 72, SideW - 40, 16);

        if (page == 0) PaintPage0(g);
        else if (page == 1) PaintGpu(g);
        else if (page == 2) PaintBattery(g);
        else PaintPage2(g);

        if (!Env.Writable)
            Th.TR(g, "⚠ 低权限运行 · 修改不会生效，请把 HonorTune.exe 复制到普通文件夹后再运行",
                Th.FTiny, Th.Warn, 300, 15, 776, 24);

        Th.Frame(g, new Rectangle(1, 1, ClientSize.Width - 3, ClientSize.Height - 3),
            Th.CornerRadius - 1, Th.Hex("#2B3448"));
    }

    void Header(Graphics g, string title, string sub)
    {
        Th.TL(g, title, Th.FPage, Th.Text, 246, 68, 600, 32);
        Th.TL(g, sub, Th.FTiny, Th.Text3, 246, 100, 700, 18);
    }

    string ModeNameOf(string key)
    {
        ModeDef m = Modes.Get(key);
        return m == null ? "自定义" : m.Name;
    }

    Color ModeColorOf(string key)
    {
        ModeDef m = Modes.Get(key);
        return m == null ? Th.Text2 : m.C1;
    }

    void PaintPage0(Graphics g)
    {
        Header(g, "概览", "当前档位与实时负载");

        Rectangle mc = new Rectangle(246, 132, 444, 268);
        Th.Fill(g, mc, 12, Th.Card);
        Th.Frame(g, mc, 12, Th.Stroke);
        Th.TL(g, "当前模式", Th.FTiny, Th.Text3, mc.X + 24, mc.Y + 14, 200, 16);

        Color c1 = Th.Text2;
        Color c2 = Th.Text3;
        string glyph = "custom";
        string tag = "";
        if (HonorEc.Mode == 0) { c1 = Th.Ok; c2 = Th.Hex("#1B9E77"); glyph = "eco"; tag = "智能档"; }
        else if (HonorEc.Mode == 1) { c1 = Th.A1; c2 = Th.A2; glyph = "balanced"; tag = "性能档"; }
        else if (HonorEc.Mode == 3) { c1 = Th.Warn; c2 = Th.Hex("#FF4D6D"); glyph = "perf"; tag = "高能档"; }

        Rectangle ic = new Rectangle(mc.X + 24, mc.Y + 40, 44, 44);
        Th.FillGrad(g, ic, 12, c1, c2, 45f);
        Glyph.Mode(g, ic, glyph, Color.White);

        Th.TL(g, tag.Length == 0 ? "—" : tag, Th.FHero, Th.Text,
            mc.X + 84, mc.Y + 40, 260, 32);

        Th.TL(g, "风扇 " + Rpm(HonorEc.Fan0) + " / " + Rpm(HonorEc.Fan1),
            Th.FSmall, Th.Text2, mc.X + 24, mc.Y + 188, 300, 18);
        string fanHint = !HonorEc.FanTableOk ? "本机无转速配置"
            : (HonorEc.Mode == 3 ? "可调整" : "仅高能档可调");
        bool fanReady = HonorEc.FanTableOk && HonorEc.Mode == 3;
        Th.TL(g, fanHint,
            Th.FTiny, fanReady ? Th.Ok : Th.Text3, mc.Right - 150, mc.Y + 188, 130, 18);

        // 右侧两块指标：宽度和总高度都与左侧「当前模式」对齐
        DrawTile(g, new Rectangle(710, 132, 444, 128), "CPU 占用", mon.Cpu.ToString("0") + "%", null,
            "实时", Th.A1, true);
        DrawTile(g, new Rectangle(710, 272, 444, 128), "内存", mon.MemUsedGb.ToString("0.0") + " / " +
            mon.MemTotalGb.ToString("0.0") + " GB", null, mon.MemPct.ToString("0") + "% 使用中", Th.A2, false);

        // 第二排
        DrawTile(g, new Rectangle(246, 412, 218, 100), "CPU 频率",
            mon.Mhz > 0 ? (mon.Mhz / 1000.0).ToString("0.00") : "—",
            mon.Mhz > 0 ? "GHz" : null, "当前实时频率", Th.A1, false);
        DrawTile(g, new Rectangle(476, 412, 218, 100), "GPU 温度",
            mon.GpuTemp >= 0 ? mon.GpuTemp.ToString("0") : "—",
            mon.GpuTemp >= 0 ? "°C" : null,
            mon.GpuUtil >= 0 ? "占用 " + mon.GpuUtil.ToString("0") + "%" : "未检测到独显", Th.Warn, false);
        DrawTile(g, new Rectangle(706, 412, 218, 100), "电池",
            mon.Battery >= 0 ? mon.Battery.ToString() : "—",
            mon.Battery >= 0 ? "%" : null,
            mon.Ac ? (mon.Charging ? "已接通电源 · 充电中" : "已接通电源") : "使用电池供电", Th.Ok, false);
        DrawTile(g, new Rectangle(936, 412, 218, 100), "刷新率",
            Disp.Hz.ToString(), "Hz", Disp.W + " × " + Disp.H, Th.A2, false);

        // 日志
        Rectangle lr = new Rectangle(246, 524, 908, 190);
        Th.CardBox(g, lr, "运行记录", "本应用做过的每一次改动都会记录在这里");
        int y = lr.Y + 62;
        int shown = 0;
        for (int i = logs.Count - 1; i >= 0 && shown < 4; i--, shown++)
        {
            string[] it = logs[i];
            Th.TL(g, it[0], Th.FMono, Th.Text3, lr.X + 20, y, 66, 18);
            Th.Dot(g, lr.X + 100, y + 9, 6, it[2] == "1" ? Th.Ok : Th.Bad);
            Th.TL(g, it[1], Th.FSmall, Th.Text2, lr.X + 116, y, lr.Width - 140, 18);
            y += 26;
        }
    }



    string FormatPct(int v)
    {
        return v < 0 ? "—" : v + "%";
    }

    string Rpm(int v)
    {
        return v < 0 ? "—" : v + " RPM";
    }



    void DrawTile(Graphics g, Rectangle r, string label, string value, string unit, string sub, Color accent, bool spark)
    {
        Th.Fill(g, r, 12, Th.Card);
        Th.Frame(g, r, 12, Th.Stroke);
        Th.TL(g, label, Th.FTiny, Th.Text3, r.X + 16, r.Y + 12, r.Width - 32, 16);

        Size vs = Th.Measure(value, Th.FNum);
        Th.TL(g, value, Th.FNum, Th.Text, r.X + 14, r.Y + 30, vs.Width + 8, 32);
        if (unit != null)
            Th.TL(g, unit, Th.FSmall, Th.Text2, r.X + 16 + vs.Width + 8, r.Y + 38, 90, 20);
        if (sub != null)
            Th.TL(g, sub, Th.FTiny, Th.Alpha(Th.Text3, 220), r.X + 16, r.Bottom - 26, r.Width - 40, 16);

        if (spark && histCount > 1)
        {
            int n = Math.Min(histCount, hist.Length);
            double[] d = new double[n];
            for (int i = 0; i < n; i++)
                d[i] = hist[(histCount - n + i) % hist.Length];
            Th.Spark(g, new Rectangle(r.Right - 150, r.Y + 20, 134, r.Height - 42), d, n, Th.Alpha(accent, 210));
        }
    }

    void PaintPage2(Graphics g)
    {
        Header(g, "细节调节", "独立于模式的细粒度开关，改动立即写入系统并记录");

        Rectangle c1 = new Rectangle(246, 132, 908, 124);
        Th.CardBox(g, c1, "显示", "刷新率会立即生效；若 15 秒内未确认，将自动还原到原来的设置");
        Th.TR(g, "当前 " + Disp.W + " × " + Disp.H + " @ " + Disp.Hz + " Hz",
            Th.FSmall, Th.Text2, c1.Right - 320, c1.Y + 14, 298, 22);

        Rectangle c2 = new Rectangle(246, 268, 908, 140);
        Th.CardBox(g, c2, "界面动画", "关闭窗口最小化/菜单淡入等动画，让操作响应更干脆");
        Th.TL(g, "关闭动画", Th.FSmall, Th.Text2, c2.X + 22, c2.Y + 54, 90, 26);
        Th.TL(g, "窗口最小化、菜单淡入与任务栏动画", Th.FTiny, Th.Text3, c2.X + 118, c2.Y + 56, 540, 22);
        Th.TL(g, "菜单显示延迟", Th.FSmall, Th.Text2, c2.X + 22, c2.Y + 90, 200, 26);
        Th.TL(g, slDelay.Value + " ms", Th.FSmall, Th.A1, c2.X + 330 + 404 + 8, c2.Y + 90, 76, 26);

        Rectangle c3 = new Rectangle(246, 420, 908, 124);
        Th.CardBox(g, c3, "后台应用", "限制应用在后台运行，减少无谓唤醒与耗电");
        Th.TL(g, "限制后台应用", Th.FSmall, Th.Text2, c3.X + 22, c3.Y + 54, 240, 26);
        Th.TL(g, "对应 Windows 设置 › 隐私和安全性 › 后台应用", Th.FTiny, Th.Text3,
            c3.X + 22, c3.Y + 84, 620, 18);

        Rectangle c4 = new Rectangle(246, 556, 908, 124);
        Th.CardBox(g, c4, "当前生效设置", "只读：直接读取系统真实状态，便于核对");

        string[] labels = new string[] { "电源方案", "处理器上限", "电源模式", "界面动画", "后台限制" };
        string[] values = new string[] {
            Trim(core.ActivePlanName(), 22),
            FormatPct(core.ActiveMaxAc()) + " / " + FormatPct(core.ActiveMaxDc()),
            OverlayName(),
            Core.AnimEnabled() ? "开启" : "关闭",
            Core.BgLimited() ? "限制" : "放开" };
        for (int i = 0; i < labels.Length; i++)
        {
            int x = c4.X + 22 + i * 176;
            Th.TL(g, labels[i], Th.FTiny, Th.Text3, x, c4.Y + 62, 160, 16);
            Th.TL(g, values[i], Th.FSmall, Th.Text, x, c4.Y + 80, 160, 20);
        }

        Th.TL(g, "从首次运行前的状态还原，可反复执行", Th.FTiny, Th.Text3, c4.X + 22, 698, 600, 20);
    }

    void PaintGpu(Graphics g)
    {
        Header(g, "GPU 超频", "和猎人营地同一条 NVAPI 偏移，只抬 P0 的核心和显存，不锁死频率");

        Rectangle info = new Rectangle(246, 132, 908, 112);
        Th.CardBox(g, info, "当前独显", GpuOc.Ready ? "NVAPI 已连通" : "点应用时才会真正下发");
        string name = mon.GpuName.Length > 0 ? mon.GpuName : "NVIDIA GPU";
        Th.TL(g, name, Th.FSmall, Th.Text, info.X + 22, info.Y + 58, 520, 22);
        string live = GpuOc.LiveCore < 0 ? "尚未读到驱动偏移"
            : "显卡实际 核心 +" + GpuOc.LiveCore + " · 显存 +" + GpuOc.LiveMem + " MHz";
        Th.TL(g, live, Th.FTiny, Th.Text3, info.X + 22, info.Y + 82, 560, 18);
        Th.TR(g, GpuOc.Status, Th.FSmall, GpuOc.Error.Length > 0 ? Th.Bad : Th.Ok,
            info.Right - 360, info.Y + 58, 336, 22);

        Rectangle card = new Rectangle(246, 256, 908, 230);
        Th.CardBox(g, card, "频率偏移", "拖动后要点「应用偏移」才会写入驱动");
        Th.TL(g, "核心", Th.FSmall, Th.Text2, card.X + 22, 322, 52, 26);
        Th.TL(g, "+" + slCore.Value + " MHz", Th.FSmall, Th.A1, 896, 322, 120, 26);
        Th.TL(g, "显存", Th.FSmall, Th.Text2, card.X + 22, 368, 52, 26);
        Th.TL(g, "+" + slMem.Value + " MHz", Th.FSmall, Th.A1, 896, 368, 120, 26);
        Th.TL(g, "核心 0–" + GpuOc.CoreMax + " · 显存 0–" + GpuOc.MemMax + "。过高可能花屏或掉驱动，不稳就还原为 0",
            Th.FTiny, Th.Text3, card.X + 22, 458, 860, 18);
    }

    void PaintBattery(Graphics g)
    {
        Header(g, "电池保护", "到上限就停充，掉到恢复点再充。写的是荣耀 EC 里那组阈值");

        Rectangle info = new Rectangle(246, 132, 908, 112);
        Th.CardBox(g, info, "电池状态", "电量来自 Windows，阈值来自 EC");
        PowerStatus ps = SystemInformation.PowerStatus;
        int pct = (int)Math.Round(ps.BatteryLifePercent * 100);
        bool ch = (ps.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;
        string ac = ps.PowerLineStatus == PowerLineStatus.Online ? "已接电源" : "使用电池";
        Th.TL(g, pct + "%", Th.FNum, Th.Text, info.X + 22, info.Y + 58, 120, 34);
        Th.TL(g, ac + " · " + (ch ? "充电中" : "未充电"), Th.FSmall, Th.Text2,
            info.X + 150, info.Y + 64, 320, 22);
        string cur = HonorEc.BatStop < 0 ? "EC 里的阈值还没读到"
            : "EC 当前：上限 " + HonorEc.BatStop + "% · 掉到 " + HonorEc.BatResume + "% 再充";
        Th.TR(g, cur, Th.FTiny, Th.Text3, info.Right - 420, info.Y + 64, 398, 20);

        Rectangle card = new Rectangle(246, 256, 908, 300);
        Th.CardBox(g, card, "充电上限", batOn ? "开启：到上限停充，掉到恢复点再充" : "已关闭：一直充到 100%");
        Th.TL(g, batOn ? "开启" : "关闭", Th.FSmall, batOn ? Th.Ok : Th.Text3,
            card.Right - 130, 266, 60, 24);
        Th.TL(g, "三档只是预设，EC 里存的是「上限 / 恢复」两个百分比。",
            Th.FTiny, Th.Text3, card.X + 22, 316, 860, 18);
        Th.TL(g, "选的这档会用上面的开关状态一起下发。",
            Th.FTiny, Th.Text3, card.X + 22, 404, 860, 18);
        Th.TL(g, "家用 70% 长期插电最护电池；办公 90%；差旅 100% 等于不限制。",
            Th.FTiny, Th.Text3, card.X + 22, 428, 860, 18);
        Th.TL(g, "不插电自动切智能档，插电且电量高于 20% 切回之前选的档位。",
            Th.FSmall, Th.Text2, card.X + 22, 466, 780, 22);
        if (HonorEc.BatError.Length > 0)
            Th.TL(g, HonorEc.BatError, Th.FTiny, Th.Bad, card.X + 22, 520, 860, 18);
    }

    static string Trim(string s, int n)
    {
        if (s == null) return "";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }

    string OverlayName()
    {
        Guid g;
        if (Nat.PowerGetActualOverlayScheme(out g) != 0) return "—";
        if (g == Guid.Empty) return "平衡";
        if (g == new Guid("ded574b5-45a0-4f42-8737-46345c09c238")) return "最佳性能";
        if (g == new Guid("961cc777-2547-4f9d-8174-7d86181b7a7a")) return "最佳能效";
        if (g == new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a")) return "最佳能效";
        return "自定义";
    }

    Size clipped;
    bool dwmRound;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized || clipped == ClientSize) return;
        clipped = ClientSize;
        // 用 DWM 圆角时不能设 Region，否则又会变成硬边
        if (!dwmRound) Th.ClipWindow(this, Th.WinRad);
    }

    // ---------- 无边框窗口拖动 ----------
    Point dragFrom;
    bool dragging;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y < TitleH && e.X > SideW - 60)
        {
            dragging = true;
            dragFrom = e.Location;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (dragging)
        {
            Point p = PointToScreen(e.Location);
            Location = new Point(p.X - dragFrom.X, p.Y - dragFrom.Y);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        dragging = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (e.Y < TitleH) WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal : FormWindowState.Maximized;
        base.OnMouseDoubleClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
        base.OnKeyDown(e);
    }
}

// ============================================================
//  命令行
// ============================================================
static class Cli
{
    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int pid);

    public static string LogPath()
    {
        return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "honortune-cli.log");
    }

    public static void Run(string[] args)
    {
        AttachConsole(-1);
        StringBuilder sb = new StringBuilder();
        Cfg cfg = Cfg.Load();
        Core core = new Core(cfg);
        core.OnLog += delegate(string m, bool ok)
        {
            string line = (ok ? "[ OK ] " : "[FAIL] ") + m;
            sb.AppendLine(line);
            try { Console.WriteLine(line); } catch { }
        };

        try
        {
            core.EnsureSnapshot();
            switch (args[0])
            {
                case "--apply":
                    if (args.Length > 1) core.ApplyMode(args[1]);
                    else sb.AppendLine("用法: --apply eco|balanced|perf");
                    break;
                case "--restore":
                    core.RestoreDefaults();
                    break;
                case "--refresh":
                    if (args.Length > 1)
                    {
                        Disp.Load();
                        int hz = int.Parse(args[1]);
                        sb.AppendLine("change rc=" + Disp.Apply(hz));
                    }
                    break;
                case "--status":
                    sb.AppendLine(core.StatusText());
                    break;
                case "--list-refresh":
                    Disp.Load();
                    sb.AppendLine(Disp.W + "x" + Disp.H + " @" + Disp.Hz + "Hz -> " +
                        string.Join(",", Disp.Rates.ConvertAll(delegate(int x) { return x.ToString(); }).ToArray()));
                    break;
                case "--diag":
                    sb.AppendLine(Diag.Dump(args.Length > 1 ? args[1] : LogPath()));
                    break;
                case "--gpu-oc":
                    if (args.Length > 2)
                    {
                        int gCore = int.Parse(args[1]);
                        int gMem = int.Parse(args[2]);
                        bool ok = GpuOc.Apply(gCore, gMem);
                        sb.AppendLine(ok ? GpuOc.Status : ("FAIL " + GpuOc.Error));
                        string pmsg = "";
                        if (ok)
                        {
                            cfg.Set("gpu", "core", GpuOc.CoreMhz.ToString());
                            cfg.Set("gpu", "mem", GpuOc.MemMhz.ToString());
                            cfg.Save();
                            Boot.PublishGpu(GpuOc.CoreMhz, GpuOc.MemMhz);
                            if (GpuOc.CoreMhz > 0 || GpuOc.MemMhz > 0)
                            {
                                HonorProfile.ApplyPower(cfg, out pmsg);
                                sb.AppendLine(pmsg);
                            }
                        }
                        GpuOc.WriteApplyResult(ok, GpuOc.CoreMhz, GpuOc.MemMhz, GpuOc.Error, pmsg);
                    }
                    else sb.AppendLine("用法: --gpu-oc <核心MHz> <显存MHz>");
                    break;
                case "--power-diag":
                    sb.Append(HonorEc.WattDiag(
                        cfg.GetInt("power", "temp", 87), cfg.GetInt("power", "cpu", 140)));
                    break;
                case "--battery-probe":
                    sb.Append(BatteryProbe.Run(args));
                    break;
                case "--gpu-reset":
                    sb.AppendLine(GpuOc.Apply(0, 0) ? GpuOc.Status : ("FAIL " + GpuOc.Error));
                    break;
                case "--gpu-probe":
                    sb.AppendLine(GpuOc.Probe() ? "NVAPI ready" : ("FAIL " + GpuOc.Error));
                    break;
                default:
                    sb.AppendLine("未知参数: " + args[0]);
                    break;
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("ERR " + ex);
        }

        try { File.WriteAllText(LogPath(), sb.ToString(), Encoding.UTF8); } catch { }
        try { Console.Out.Flush(); } catch { }
    }
}

// ============================================================
//  电池充电上限试探
//  荣耀这边不是 group 3，而是 group 6。PowerPolicyPlugin 里一共用了
//  0x12 0x13 0x1B 0x25 0x26 0x34 0x3C 0x3F 这八条，静态看不出哪条是充电上限，
//  只能一条条发、看充电状态。参数默认 80% 停 / 40% 恢复，随时能用管家改回去。
// ============================================================
static class BatteryProbe
{
    public static string Path()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
        try { Directory.CreateDirectory(dir); } catch { }
        return System.IO.Path.Combine(dir, "battery-probe.txt");
    }

    static string State()
    {
        PowerStatus ps = SystemInformation.PowerStatus;
        int pct = (int)Math.Round(ps.BatteryLifePercent * 100);
        string ac = ps.PowerLineStatus == PowerLineStatus.Online ? "已接电源" : "电池供电";
        bool charging = (ps.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;
        return pct + "%  " + ac + "  " + (charging ? "充电中" : "未充电");
    }

    static string Hex(byte[] b)
    {
        return Hex(b, 10);
    }

    static string Hex(byte[] b, int n)
    {
        if (b == null) return "null";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < Math.Min(n, b.Length); i++)
            sb.Append(b[i].ToString("X2")).Append(i < n - 1 ? " " : "");
        return sb.ToString();
    }

    static readonly int[] Candidates = new int[] { 0x25, 0x26, 0x1B, 0x34, 0x3C, 0x3F, 0x12, 0x13 };

    static bool Charging()
    {
        return (SystemInformation.PowerStatus.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;
    }

    static bool OnAc()
    {
        return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
    }

    static byte[] SecRead()
    {
        byte[] p = new byte[64];
        p[0] = 0x03;
        p[1] = 0x11;
        p[2] = 0xFF;
        return HonorEc.CallRaw(p);
    }

    static bool Has(byte[] b, int v)
    {
        if (b == null) return false;
        for (int i = 0; i < b.Length; i++) if (b[i] == (byte)v) return true;
        return false;
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++) if (a[i] != b[i]) return false;
        return true;
    }

    static byte[] Send(int cmd, int hi, int lo)
    {
        return SendRaw(cmd, hi, lo);
    }

    static byte[] SendRaw(int cmd, int hi, int lo)
    {
        byte[] p = new byte[64];
        p[0] = 0x06;
        p[1] = (byte)cmd;
        p[2] = (byte)hi;
        p[3] = (byte)lo;
        return HonorEc.CallRaw(p);
    }

    // 判断标准：现在电池是「未充电」，发一条 100/95，如果它开始充电，就说明这条是充电上限。
    public static string Run(string[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("HonorTune 电池充电试探 " + DateTime.Now.ToString("HH:mm:ss"));
        if (args.Length < 2)
        {
            sb.AppendLine("用法: --battery-probe all          依次试八条候选，用 100/95 看是否开始充电");
            sb.AppendLine("      --battery-probe <命令号> [上限%] [恢复%]");
            sb.AppendLine("候选: 0x25 0x26 0x1B 0x34 0x3C 0x3F 0x12 0x13");
            return Finish(sb);
        }
        if (!OnAc())
        {
            sb.AppendLine("请先接上电源再试");
            return Finish(sb);
        }
        sb.AppendLine("电池 前: " + State());

        string mode = args[1].Trim();
        if (mode.Equals("sec", StringComparison.OrdinalIgnoreCase))
        {
            // 0x11 是 HNSDK_IsSecBatteryManageOpen 用的查询命令（group 3），
            // 写命令按惯例是相邻的 0x10，写完再读 0x11 看有没有变。
            sb.AppendLine("读 group=3 cmd=0x11 参数 0xFF:");
            byte[] a = SecRead();
            sb.AppendLine("  [" + Hex(a, 16) + "]");
            if (a == null) return Finish(sb);
            for (int v = 1; v <= 3; v++)
            {
                byte[] wp = new byte[64];
                wp[0] = 0x03; wp[1] = 0x10; wp[2] = (byte)v;
                byte[] wr = HonorEc.CallRaw(wp);
                System.Threading.Thread.Sleep(300);
                byte[] r2 = SecRead();
                sb.AppendLine("写 0x10=" + v + "  返回[" + Hex(wr, 6) + "]  再读[" + Hex(r2, 16) + "]");
                if (r2 != null && a.Length == r2.Length && !Same(a, r2))
                {
                    sb.AppendLine(">>> 0x10 写进去了，读到变化，值 " + v);
                    break;
                }
            }
            return Finish(sb);
        }
        if (mode.Equals("secset", StringComparison.OrdinalIgnoreCase))
        {
            // 0x11 读到 [1]=0x46(70) [2]=0x5A(90)，就是「70% 恢复 / 90% 停止」。
            // 写命令按同样的字节顺序试，写完用 0x11 读回来验证。
            byte[] a = SecRead();
            sb.AppendLine("当前 [" + Hex(a, 16) + "]");
            int stop = 80, resume = 40;
            int[] cmds = new int[] { 0x10, 0x12, 0x0F, 0x1F };
            int okCmd = -1, okLayout = -1;
            foreach (int c in cmds)
            {
                for (int lay = 0; lay < 2 && okCmd < 0; lay++)
                {
                    byte[] wp = new byte[64];
                    wp[0] = 0x03;
                    wp[1] = (byte)c;
                    wp[2] = (byte)(lay == 0 ? stop : resume);
                    wp[3] = (byte)(lay == 0 ? resume : stop);
                    byte[] wr = HonorEc.CallRaw(wp);
                    System.Threading.Thread.Sleep(400);
                    byte[] r2 = SecRead();
                    bool hit = Has(r2, stop) && Has(r2, resume);
                    sb.AppendLine("写 0x" + c.ToString("X2") + " " + stop + "/" + resume
                        + " 返回[" + Hex(wr, 6) + "] 读回[" + Hex(r2, 16) + "]" + (hit ? "  <<< 对上了" : ""));
                    if (hit) { okCmd = c; okLayout = lay; break; }
                }
                if (okCmd >= 0) break;
            }
            if (okCmd < 0)
            {
                sb.AppendLine("都没对上，写命令不是这几条或参数顺序不同");
                return Finish(sb);
            }
            // 还原成 90/70，别把机器留在 80/40
            byte[] rp = new byte[64];
            rp[0] = 0x03;
            rp[1] = (byte)okCmd;
            rp[2] = (byte)(okLayout == 0 ? 90 : 70);
            rp[3] = (byte)(okLayout == 0 ? 70 : 90);
            HonorEc.CallRaw(rp);
            System.Threading.Thread.Sleep(400);
            sb.AppendLine("已还原 90/70，读回 [" + Hex(SecRead(), 16) + "]");
            sb.AppendLine(">>> 写命令是 0x" + okCmd.ToString("X2") + "，参数顺序 " + (okLayout == 0 ? "上限,恢复" : "恢复,上限"));
            return Finish(sb);
        }
        if (mode.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (Charging())
            {
                sb.AppendLine("现在已经在充电，判断不了。先等它停充（比如到 90%）再试");
                return Finish(sb);
            }
            int found = -1;
            foreach (int c in Candidates)
            {
                byte[] rr = SendRaw(c, 0, 0);
                if (rr == null || rr.Length == 0)
                {
                    sb.AppendLine("cmd 0x" + c.ToString("X2") + "  无响应");
                    continue;
                }
                string hex = Hex(rr, 16);
                string mark = "";
                for (int k = 1; k < Math.Min(16, rr.Length); k++)
                {
                    if (rr[k] == 0x5A) mark += "  含 5A(90%)@+" + k;
                    else if (rr[k] == 0x46) mark += "  含 46(70%)@+" + k;
                    else if (rr[k] == 0x64) mark += "  含 64(100%)@+" + k;
                }
                sb.AppendLine("cmd 0x" + c.ToString("X2") + "  [" + hex + "]" + mark);
                if (rr[0] != 0) continue;
                for (int i = 0; i < 4; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (Charging()) { found = c; break; }
                }
                if (found >= 0)
                {
                    sb.AppendLine(">>> 0x" + found.ToString("X2") + " 让电池开始充电");
                    break;
                }
            }
            if (found >= 0)
                sb.AppendLine(">>> 现在会充到 100%。要回到 90% 用: --battery-probe 0x"
                    + found.ToString("X2") + " 90 70");
            else sb.AppendLine("没有一条让它开始充电；看上面哪条的回包里带 5A/46/64，那条更可能是读上限的");
            return Finish(sb);
        }

        int cmd;
        string cs = mode;
        try
        {
            cmd = cs.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt32(cs.Substring(2), 16) : Convert.ToInt32(cs);
        }
        catch { sb.AppendLine("命令号看不懂: " + cs); return Finish(sb); }

        int hi = 100, lo = 95;
        if (args.Length > 2) int.TryParse(args[2], out hi);
        if (args.Length > 3) int.TryParse(args[3], out lo);

        sb.AppendLine("发送 group=6 cmd=0x" + cmd.ToString("X2") + "  上限 " + hi + "%  恢复 " + lo + "%");
        byte[] res = Send(cmd, hi, lo);
        sb.AppendLine("EC 返回 [" + Hex(res, 16) + "]  状态 " +
            (res == null || res.Length == 0 ? "无响应" : res[0].ToString("X2")));
        bool was = Charging();
        for (int i = 0; i < 15; i++)
        {
            System.Threading.Thread.Sleep(2000);
            sb.AppendLine("  +" + ((i + 1) * 2) + "s  " + State());
            if (!was && Charging())
            {
                sb.AppendLine(">>> 开始充电了，这条就是充电上限");
                break;
            }
        }
        return Finish(sb);
    }

    static string Finish(StringBuilder sb)
    {
        string s = sb.ToString();
        try { File.AppendAllText(Path(), s + "\r\n", Encoding.UTF8); } catch { }
        // 这个程序是窗口程序，从“运行”里启动不会有控制台，结果直接弹出来
        try { MessageBox.Show(s, "电池充电试探 · 结果", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch { }
        return s;
    }
}

// ============================================================
//  诊断
// ============================================================
static class Diag
{
    public static string Dump(string path)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            sb.AppendLine("CWD=" + Environment.CurrentDirectory);
            sb.AppendLine("ApplicationData=" + Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            sb.AppendLine("TempPath=" + System.IO.Path.GetTempPath());
            sb.AppendLine("SystemDirectory=" + Environment.SystemDirectory);
            sb.AppendLine("User=" + Environment.UserName + " Interactive=" + Environment.UserInteractive);
            sb.AppendLine("OS=" + Environment.OSVersion);

            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonorTune");
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(System.IO.Path.Combine(dir, "_write.txt"), "ok");
                sb.AppendLine("APPDATA write: OK -> " + dir);
            }
            catch (Exception ex) { sb.AppendLine("APPDATA write: FAIL " + ex.GetType().Name + " " + ex.Message); }

            try
            {
                string tp = System.IO.Path.GetTempPath();
                File.WriteAllText(System.IO.Path.Combine(tp, "_honortune_tmp.txt"), "ok");
                sb.AppendLine("TEMP write: OK");
            }
            catch (Exception ex) { sb.AppendLine("TEMP write: FAIL " + ex.Message); }

            sb.AppendLine("--- powercfg /list ---");
            sb.AppendLine(Run("powercfg", "/list"));
            sb.AppendLine("--- powercfg -duplicatescheme ---");
            string dup = Run("powercfg", "-duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            sb.AppendLine(dup);
            Match mm = Regex.Match(dup,
                "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
            if (mm.Success)
            {
                sb.AppendLine("GUID found: " + mm.Value);
                Core.Pc("-delete " + mm.Value);
            }
            else sb.AppendLine("GUID NOT found");
        }
        catch (Exception ex) { sb.AppendLine("DIAG EXCEPTION " + ex); }

        try { File.WriteAllText(path, sb.ToString(), Encoding.UTF8); } catch { }
        return sb.ToString();
    }

    static string Run(string exe, string args)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            using (Process p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd();
                string e = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                sb.AppendLine("exit=" + p.ExitCode + " outLen=" + o.Length + " errLen=" + e.Length);
                sb.AppendLine("decoded(default): [" + o.Trim() + "]");
                byte[] raw = null;
                try { raw = Encoding.Default.GetBytes(o); } catch { }
                if (raw != null)
                {
                    sb.AppendLine("as UTF8: [" + Encoding.UTF8.GetString(raw).Trim() + "]");
                    sb.AppendLine("as GBK : [" + Encoding.GetEncoding(936).GetString(raw).Trim() + "]");
                }
            }
        }
        catch (Exception ex) { sb.AppendLine("RUN EXCEPTION " + ex.Message); }
        return sb.ToString();
    }
}

// ============================================================
//  UAC：只在安装/卸载后台服务时申请，不把主窗口提权后直接跑起来
// ============================================================
static class Elev
{
    public static bool IsAdmin()
    {
        try
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal pr = new WindowsPrincipal(id);
            return pr.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    public static bool Relaunch(string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = Application.ExecutablePath;
        psi.Arguments = args ?? "";
        psi.UseShellExecute = true;
        psi.Verb = "runas";
        Process.Start(psi);
        return true;
    }
}

// ============================================================
//  后台服务：保持上次性能档和风扇，不打开主窗口
// ============================================================
static class Boot
{
    const string SvcName = "HonorTuneKeep";
    const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "HonorTune";

    public static string DataDir()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HonorTune");
        try { Directory.CreateDirectory(dir); } catch { }
        return dir;
    }

    public static string InstallDir()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HonorTune");
        try { Directory.CreateDirectory(dir); } catch { }
        return dir;
    }

    public static string KeepPath()
    {
        return System.IO.Path.Combine(DataDir(), "keep.ini");
    }

    public static bool ServiceInstalled()
    {
        return Sc("query " + SvcName) == 0;
    }

    // 插电自动切档：preset 是用户选的档位，auto=1 表示启用
    public static int Preset = -1;
    public static int AutoOn = -1;

    public static void PublishKeep(int mode, int fan, bool on)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("mode=" + mode);
            sb.AppendLine("fan=" + fan);
            sb.AppendLine("on=" + (on ? "1" : "0"));
            sb.AppendLine("gpucore=" + GpuOc.CoreMhz);
            sb.AppendLine("gpumem=" + GpuOc.MemMhz);
            if (Preset >= 0) sb.AppendLine("preset=" + Preset);
            if (AutoOn >= 0) sb.AppendLine("auto=" + AutoOn);
            File.WriteAllText(KeepPath(), sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    public static void PublishGpu(int core, int mem)
    {
        GpuOc.CoreMhz = core;
        GpuOc.MemMhz = mem;
        int mode, fan, gc, gm;
        bool on;
        ReadKeep(out mode, out fan, out on, out gc, out gm);
        if (mode != 0 && mode != 1 && mode != 3) mode = HonorEc.Mode;
        PublishKeep(mode, fan, on);
    }

    public static bool ReadKeep(out int mode, out int fan, out bool on, out int gpuCore, out int gpuMem)
    {
        int preset, auto;
        return ReadKeep(out mode, out fan, out on, out gpuCore, out gpuMem, out preset, out auto);
    }

    public static bool ReadKeep(out int mode, out int fan, out bool on, out int gpuCore, out int gpuMem,
        out int preset, out int auto)
    {
        mode = -1;
        fan = -1;
        on = false;
        gpuCore = 0;
        gpuMem = 0;
        preset = -1;
        auto = -1;
        try
        {
            if (!File.Exists(KeepPath())) return false;
            foreach (string raw in File.ReadAllLines(KeepPath(), Encoding.UTF8))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string k = raw.Substring(0, eq).Trim();
                string v = raw.Substring(eq + 1).Trim();
                if (k == "mode") int.TryParse(v, out mode);
                else if (k == "fan") int.TryParse(v, out fan);
                else if (k == "on") on = v != "0";
                else if (k == "gpucore") int.TryParse(v, out gpuCore);
                else if (k == "gpumem") int.TryParse(v, out gpuMem);
                else if (k == "preset") int.TryParse(v, out preset);
                else if (k == "auto") int.TryParse(v, out auto);
            }
            gpuCore = GpuOc.Clamp(gpuCore, 0, GpuOc.CoreMax);
            gpuMem = GpuOc.Clamp(gpuMem, 0, GpuOc.MemMax);
            return mode == 0 || mode == 1 || mode == 3;
        }
        catch { return false; }
    }

    public static bool InstallService()
    {
        try
        {
            Cfg cfg = Cfg.Load();
            GpuOc.Load(cfg);
            HonorEc.Refresh();
            int mode = cfg.GetInt("keep", "mode", HonorEc.Mode);
            int fan = cfg.GetInt("keep", "fan", HonorEc.FanIndex);
            if (mode != 0 && mode != 1 && mode != 3) mode = HonorEc.Mode;
            Preset = cfg.GetInt("auto", "preset", -1);
            AutoOn = cfg.Get("auto", "on", "1") != "0" ? 1 : 0;
            PublishKeep(mode, fan, true);
            cfg.Set("keep", "startup", "1");
            if (mode == 0 || mode == 1 || mode == 3) cfg.Set("keep", "mode", mode.ToString());
            if (fan >= 0 && fan <= 4) cfg.Set("keep", "fan", fan.ToString());
            cfg.Save();

            string exe = System.IO.Path.Combine(InstallDir(), "HonorTune.exe");
            string src = Application.ExecutablePath;
            Sc("stop " + SvcName);
            Thread.Sleep(600);
            if (!src.Equals(exe, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Copy(src, exe, true); }
                catch (Exception ex)
                {
                    WriteLog("复制服务程序失败：" + ex.Message);
                    exe = src;
                }
            }
            Sc("delete " + SvcName);
            Thread.Sleep(400);
            ClearLaunchers();
            string bin = "binPath= \"\\\"" + exe + "\\\" --service\" start= auto DisplayName= \"HonorTune Keep\"";
            int rc = Sc("create " + SvcName + " " + bin);
            if (rc != 0)
            {
                WriteLog("创建服务失败 rc=" + rc);
                return false;
            }
            Sc("description " + SvcName + " \"Reapply last performance mode and fan. Does not open the app.\"");
            Sc("failure " + SvcName + " reset= 86400 actions= restart/5000/restart/15000/restart/60000");
            rc = Sc("start " + SvcName);
            WriteLog("服务已安装 mode=" + mode + " fan=" + fan + " start=" + rc + " exe=" + exe);
            return rc == 0 || ServiceInstalled();
        }
        catch (Exception ex)
        {
            WriteLog("安装服务失败：" + ex.Message);
            return false;
        }
    }

    public static bool RemoveService()
    {
        PublishKeep(-1, -1, false);
        Sc("stop " + SvcName);
        Thread.Sleep(500);
        int rc = Sc("delete " + SvcName);
        ClearLaunchers();
        WriteLog("服务已移除 rc=" + rc);
        return rc == 0 || !ServiceInstalled();
    }

    static void ClearLaunchers()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunPath, true))
                if (k != null) k.DeleteValue(RunName, false);
        }
        catch { }
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", "/Delete /TN \"" + RunName + "\" /F");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            using (Process p = Process.Start(psi))
            {
                p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit(8000);
            }
        }
        catch { }
    }

    static int Sc(string args)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("sc.exe", args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            using (Process p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd();
                string e = p.StandardError.ReadToEnd();
                if (!p.WaitForExit(15000)) return -1;
                if (p.ExitCode != 0 && args.StartsWith("create", StringComparison.OrdinalIgnoreCase))
                    WriteLog("sc " + args + " rc=" + p.ExitCode + " " + (o + e).Trim());
                return p.ExitCode;
            }
        }
        catch { return -1; }
    }

    public static void WriteLog(string text)
    {
        try
        {
            File.WriteAllText(System.IO.Path.Combine(DataDir(), "service.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + text + "\r\n", Encoding.UTF8);
        }
        catch { }
    }
}

class KeepService : ServiceBase
{
    Thread worker;
    volatile bool stop;
    bool forced;
    int quiet;

    public KeepService()
    {
        ServiceName = "HonorTuneKeep";
        CanStop = true;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        stop = false;
        forced = false;
        worker = new Thread(Loop);
        worker.IsBackground = true;
        worker.Start();
    }

    protected override void OnStop()
    {
        stop = true;
        if (worker != null) worker.Join(4000);
    }

    void Loop()
    {
        Boot.WriteLog("服务启动，不打开主窗口");
        for (int i = 0; i < 20 && !stop; i++)
        {
            HonorEc.Refresh();
            if (HonorEc.Mode >= 0) break;
            Thread.Sleep(2000);
        }
        int n = 0;
        while (!stop)
        {
            try { Tick(n < 6); }
            catch (Exception ex) { Boot.WriteLog("保持失败：" + ex.Message); }
            n++;
            int wait = n < 8 ? 15000 : 30000;
            for (int i = 0; i < wait / 400 && !stop; i++) Thread.Sleep(400);
        }
    }

    void Tick(bool soon)
    {
        int mode, fan, gpuCore, gpuMem, preset, auto;
        bool on;
        if (!Boot.ReadKeep(out mode, out fan, out on, out gpuCore, out gpuMem, out preset, out auto) || !on) return;
        HonorEc.Refresh();

        // 插电自动切档：不插电强制智能档，插电且电量 > 20% 回到预设档
        int target = mode;
        if (auto == 1)
        {
            PowerStatus ps = SystemInformation.PowerStatus;
            bool ac = ps.PowerLineStatus == PowerLineStatus.Online;
            int pct = (int)Math.Round(ps.BatteryLifePercent * 100);
            if (!ac) target = 0;
            else if (pct <= 20) target = 0;
            else if (preset == 1 || preset == 3) target = preset;
        }

        bool drift = HonorEc.Mode != target;
        if (target == 3 && fan >= 0 && fan <= 4 && HonorEc.FanIndex != fan) drift = true;
        quiet++;
        bool reassert = soon || quiet >= 4;
        if (reassert && (gpuCore > 0 || gpuMem > 0))
        {
            if (!GpuOc.Apply(gpuCore, gpuMem))
                Boot.WriteLog("GPU 偏移未生效：" + GpuOc.Error);
        }
        if (!drift && forced && !reassert) return;
        if (reassert) quiet = 0;
        string msg = MainForm.ApplyHonorChoice(target, fan);
        forced = true;
        if (drift || msg.IndexOf("失败") >= 0 || msg.IndexOf("未生效") >= 0)
            Boot.WriteLog("目标档位 " + target + "：" + msg);
    }
}

// ============================================================
//  入口
// ============================================================
static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args != null && args.Length > 0 && args[0] == "--service")
        {
            ServiceBase.Run(new KeepService());
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args != null && args.Length > 0 &&
            (args[0] == "--install-service" || args[0] == "--remove-service" || args[0] == "--arm"))
        {
            if (!Elev.IsAdmin())
            {
                try { if (Elev.Relaunch(args[0])) return; }
                catch (Exception ex)
                {
                    Boot.WriteLog("未获得管理员权限：" + ex.Message);
                    return;
                }
            }
            try
            {
                if (args[0] == "--remove-service") Boot.RemoveService();
                else Boot.InstallService();
            }
            catch (Exception ex) { Boot.WriteLog("ERR " + ex.Message); }
            return;
        }

        if (args.Length > 1 && args[0] == "--page")
        {
            try { Application.Run(new MainForm(int.Parse(args[1]))); }
            catch (Exception ex) { MessageBox.Show(ex.ToString(), "荣耀性能调控 - 启动失败"); }
            return;
        }

        if (args.Length > 0 && args[0].StartsWith("--", StringComparison.Ordinal))
        {
            Cli.Run(args);
            return;
        }

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "荣耀性能调控 - 启动失败");
        }
    }
}
