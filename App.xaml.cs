using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Threading;

namespace ProcessLoad
{
    public partial class App : System.Windows.Application
    {
        private NotifyIcon _TrayIconCPU;
        private NotifyIcon _TrayIconRAM;
        private List<PerformanceCounter> _CPUCounters;
        private PerformanceCounter _RAMCounter;
        private DispatcherTimer _UpdateTimer;
        private float _TotalMemory;
        public App()
        {
            _TrayIconCPU = new();
            _TrayIconRAM = new();
            _CPUCounters = [..Enumerable
                .Range(0, Environment.ProcessorCount)
                .Select(i => new PerformanceCounter("Processor", "% Processor Time", i.ToString()))];
            _RAMCounter = new PerformanceCounter("Memory", "Available MBytes");
            _UpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _TotalMemory = GetTotalMemory();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _TrayIconCPU.Visible = true;
            _TrayIconRAM.Visible = true;
            _TrayIconCPU.ContextMenuStrip = new ContextMenuStrip();
            _TrayIconCPU.ContextMenuStrip.Items.Add("Exit", null, (_, _) =>
            {
                _TrayIconCPU.Visible = false;
                _TrayIconRAM.Visible = false;
                _UpdateTimer.Stop();
                Shutdown();
            });
            _UpdateTimer.Tick += UpdateValues;
            _UpdateTimer.Start();

        }
        private void UpdateValues(object? sender, EventArgs e)
        {
            int cpuUsage = (int)GetCpuUsage();
            _TrayIconCPU!.Icon = GetIcon(cpuUsage);
            _TrayIconCPU.Text = $"CPU usage: {cpuUsage}%";
            int ramUsage = (int)GetRamUsage();
            _TrayIconRAM!.Icon = GetIcon(ramUsage);
            _TrayIconRAM.Text = $"RAM usage: {ramUsage}%";
        }
        #region Получение значений
        private float GetCpuUsage()
        {
            float totalUsage = 0;
            foreach (var counter in _CPUCounters)
            {
                totalUsage += counter.NextValue();
            }
            return totalUsage/_CPUCounters.Count;
        }
        private float GetRamUsage()
        {
            return (_TotalMemory - _RAMCounter.NextValue()) / _TotalMemory * 100;
        }
        private float GetTotalMemory()
        {
            ulong totalMemory = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    totalMemory = (ulong)obj["TotalPhysicalMemory"];
                }
            }
            return totalMemory / (1024 * 1024);
        }
        #endregion
        #region Отрисовка иконок
        private Icon GetIcon(int usagePerscent)
        {
            using Bitmap bmp = new(64, 64);
            using Graphics graphic = Graphics.FromImage(bmp);
            graphic.Clear(Color.Transparent);
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.InterpolationMode = InterpolationMode.HighQualityBilinear;
            //рамка для полосы
            graphic.DrawPath(new Pen(Color.FromArgb(240, 245, 245), 4), GetRoundedRectangle(64, 20));
            Color colorBrush = GetColor(usagePerscent);
            //полоска
            graphic.FillPath(new SolidBrush(colorBrush), GetRoundedRectangle((int)(0.64 * usagePerscent), 20));
            //текст 
            graphic.DrawString(usagePerscent.ToString(), new Font("Arial", 28), new SolidBrush(colorBrush), new PointF(0, 21));
            using MemoryStream memoryStream = new();
            bmp.Save(memoryStream, ImageFormat.Png);
            using var iconBmp = new Bitmap(memoryStream);
            IntPtr hIcon = iconBmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
        private GraphicsPath GetRoundedRectangle(int width, int height)
        {
            // по дефолту в одной полоске 3 круга
            int horizontalDiameter = width > 3 ? width / 3 : 1;
            int radius = horizontalDiameter / 2;
            var roundedRectangle = new GraphicsPath();
            // left 
            roundedRectangle.AddArc(1, 1, horizontalDiameter, height, 90, 180);
            // top
            roundedRectangle.AddLine(radius, 1, width - radius, 1);
            // right 
            roundedRectangle.AddArc(horizontalDiameter * 2, 1, horizontalDiameter, height, 270, 180);
            // bottom
            roundedRectangle.AddLine(width - radius, height, radius, height);
            return roundedRectangle;
        }
        private Color GetColor(int usagePerscent)
        {
            return usagePerscent <= 40
                ? Color.FromArgb(112, 219, 112)
                : usagePerscent > 40 && usagePerscent <= 80
                ? Color.FromArgb(255, 204, 128)
                : usagePerscent > 80 ? Color.FromArgb(255, 92, 51) : Color.Transparent;
        }
        #endregion
    }
}
