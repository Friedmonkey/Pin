using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
//using ServerRegistrationManager;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using FriedPipeV2;

using Application = System.Windows.Forms.Application;
using Image = System.Drawing.Image;

namespace FriedMonkey
{
    public enum ScrollMode
    { 
        None,
        All,
        Width,
        Height,
    }
    public partial class Form1 : Form
    {
        //private ServerRegistrationManager.Application App = new ServerRegistrationManager.Application(new outputService());

        private string LastUrl = "No url";
        private int mCentralXOffset;
        private int mCentralYOffset;
        private Size size;
        private int stepSize = 10;
        private int screenIndex = 0;
        //private bool ScrollEnabled = true;
        private ScrollMode scrollMode = ScrollMode.All;
        private bool ClipEnabled = false;
        private bool ResizeEnabled = false;

        private Color BorderColor = Color.Transparent;
        private int BorderWidth = 1;

        private const int GWL_EXSTYLE = -20; //the style

        private const int WS_EX_LAYERED = 0x80000; //allow it to have layers
        public const int LWA_ALPHA = 0x2; //make it invisible
        private const int WS_EX_TRANSPARENT = 0x20; //make it clicktrough
        private const int LWA_COLORKEY = 0x1; //the colorkey

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


        // External functions
        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint transparentColor, short alpha, uint action); // true is OK, 0=transparent, 255=opaque


        public Form1()
        {
            InitializeComponent();
            size = panFakeForm.Size;
            panFakeForm.MouseWheel += PanFakeForm_MouseWheel;
        }

        private void PanFakeForm_MouseWheel(object sender, MouseEventArgs e)
        {
            ScrollMode overRide = scrollMode;
            if (ModifierKeys == (Keys.Control & Keys.Shift))
            { 
                overRide = ScrollMode.All;
            }
            else if (ModifierKeys == Keys.Control)
            {
                overRide = ScrollMode.Width;
            }
            else if (ModifierKeys == Keys.Shift)
            {
                overRide = ScrollMode.Height;
            }
            int newx = 0;
            int newy = 0;
            int delta = e.Delta/stepSize;

            switch (overRide)
            {
                case ScrollMode.None:
                    return;
                case ScrollMode.All:
                    newx = panHolder.Size.Width + delta;
                    newy = panHolder.Size.Height + delta;
                    break;
                case ScrollMode.Width:
                    newx = panHolder.Size.Width + delta;
                    newy = panHolder.Size.Height;
                    break;
                case ScrollMode.Height:
                    newx = panHolder.Size.Width;
                    newy = panHolder.Size.Height + delta;
                    break;
                default:
                    break;
            }
            this.Refresh();
            if (delta < 0)
            {
                if (panFakeForm.Size.Width < 40 && scrollMode != ScrollMode.Height)
                    return;
                if (panFakeForm.Size.Height < 40 && scrollMode != ScrollMode.Width)
                    return;
            }
            this.panHolder.Size = new Size(newx, newy);
            this.Refresh();
        }

        private void ChangeTransparancyColor(Color color) 
        {
            this.BackColor = color;
            base.TransparencyKey = color;
        }

        private void ChangeTransparencyType() 
        {
            //this.BackColor = Color.White;
            //this.TransparencyKey = Color.Empty;

            IntPtr hWnd = this.Handle;
            uint origionalStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, origionalStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetLayeredWindowAttributes(hWnd, 0xFF00FF, 128, LWA_ALPHA);
        }


        public async Task<string> GetDirectGifLink(string tenorGifLink)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(tenorGifLink);

                    if (response.IsSuccessStatusCode)
                    {
                        string htmlContent = await response.Content.ReadAsStringAsync();

                        // Use regex to extract the direct link from the HTML content
                        Regex regex = new Regex(@"property=""og:image"" content=""(.*?)""");
                        Match match = regex.Match(htmlContent);

                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                return tenorGifLink;
            }
        }

        public string crapHash(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var base64 = System.Convert.ToBase64String(plainTextBytes);
            return base64.Substring(0,(base64.Length > 100) ? 100 : base64.Length);
        }

        public async Task<Image> getImage(String url)
        {
            LastUrl = url;
            var path0 = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), ".Pin");
            var path = Path.Combine(path0, crapHash(url));


            //chaching system if we already have it then just use this instead
            if (File.Exists(path))
                return Image.FromFile(path);

            if (!Directory.Exists(path0))
                Directory.CreateDirectory(path0);



            string realUrl = await GetDirectGifLink(url);
            LastUrl = realUrl;


            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(realUrl);
            HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream stream = httpWebReponse.GetResponseStream();
            //using (Stream stream = httpWebReponse.GetResponseStream())
            using (FileStream fs = File.Create(path))
            {
                stream.CopyTo(fs);
            }
            //return Image.FromStream(stream);
            return Image.FromFile(path);

            //Stream stream = httpWebReponse.GetResponseStream();

            //MemoryStream memoryStream = new MemoryStream();
            //stream.CopyTo(memoryStream);
            //memoryStream.Position = 0;
            //stream = memoryStream;

            //return Image.FromStream(stream,true,true);

        }

        private async Task LoadClip()
        {
            Image clipboardImage = null;
            var dataObj = Clipboard.GetDataObject();
            if (dataObj.GetFormats().Contains("PNG"))
            { 
                MemoryStream mem = (MemoryStream)dataObj.GetData("PNG");
                clipboardImage = new Bitmap(mem);
            }
            else
                clipboardImage = Clipboard.GetImage();


            if (clipboardImage != null)
            {
                panFakeForm.Image = clipboardImage;
                panHolder.Size = new Size(clipboardImage.Width, clipboardImage.Height);
                size = panHolder.Size;
            }
            else
            {
                var text = Clipboard.GetText();
                if (text.StartsWith("http"))
                {
                    clipboardImage = await getImage(text);
                    if (clipboardImage != null)
                    {
                        panFakeForm.Image = clipboardImage;
                        panHolder.Size = new Size(clipboardImage.Width, clipboardImage.Height);
                        size = panHolder.Size;
                    }
                }
                if (text == string.Empty)
                {
                    var dataObj2 = Clipboard.GetDataObject();
                    if (dataObj2.GetFormats().Contains("FileName"))
                    {
                        string[] data = (string[])dataObj2.GetData("FileName");
                        //MemoryStream mem = (MemoryStream)dataObj2.GetData("FileName");
                        //StreamReader reader = new StreamReader(mem);
                        //text = await reader.ReadToEndAsync();
                        text = data.FirstOrDefault();
                    }
                    if (text == string.Empty)
                    { 
                        MessageBox.Show($"No image found, got \"{text}\"");
                        return;
                    }

                    var path = text.Replace("\"", "");

                    if (File.Exists(path))
                    {
                        try
                        {
                            clipboardImage = Image.FromFile(path);
                        }
                        catch { }
                        if (clipboardImage != null)
                        {
                            panFakeForm.Image = clipboardImage;
                            panHolder.Size = new Size(clipboardImage.Width, clipboardImage.Height);
                            size = panHolder.Size;
                        }
                        else
                            MessageBox.Show($"No image found, got \"{text}\"");
                    }
                    else
                        MessageBox.Show($"No image found, got \"{text}\"");
                }
            }
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen screen = Screen.AllScreens[i];

                if (screen.Primary)
                    screenIndex = i;
            }
            //this.panHolder.BackColor = Color.Magenta;
            //this.BackColor = Color.Gray;
            ChangeTransparancyColor(Color.Gray);
            //base.TransparencyKey = Color.Fuchsia;
            base.FormBorderStyle = FormBorderStyle.None;
            //Screen.AllScreens.
            Rectangle bounds = Screen.AllScreens[screenIndex].Bounds;
            base.Size = this.CalcSize(bounds);
            base.Left = bounds.Left;
            base.Top = bounds.Top;
            base.TopMost = true;
            this.ShowInTaskbar = false;

            await LoadClip();
            //TextLabel.MouseDown += panFakeForm_MouseDown;
            //TextLabel.MouseMove += panFakeForm_MouseMove;
            //TextLabel.MaximumSize = new Size(this.panHolder.Size.Width, 0);

        }
        public Size CalcSize(Rectangle rect)
        {
            Size result = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            return result;
        }

        private void panFakeForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                // Simulate a mouse button down event at a specific position on Form1
                SimulateMouseButtonDownOnForm1(e, 100, 100); // Example: click at position (100, 100) on Form1
            }
            else
            {
                this.mCentralXOffset = e.X;
                this.mCentralYOffset = e.Y;
            }
        }

        private void panFakeForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                // Simulate a mouse button up event at a specific position on Form1
                SimulateMouseButtonUpOnForm1(e, 200, 200); // Example: click at position (200, 200) on Form1
            }
        }

        private void SimulateMouseButtonDownOnForm1(MouseEventArgs e, int x, int y)
        {
            // Calculate the absolute screen coordinates for the click
            Point clickPoint = Form1.ActiveForm.PointToScreen(new Point(x, y));

            // Determine which mouse button was pressed and simulate its down event
            switch (e.Button)
            {
                case MouseButtons.Left:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
                case MouseButtons.Right:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
                case MouseButtons.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
            }
        }

        private void SimulateMouseButtonUpOnForm1(MouseEventArgs e, int x, int y)
        {
            // Calculate the absolute screen coordinates for the click
            Point clickPoint = Form1.ActiveForm.PointToScreen(new Point(x, y));

            // Determine which mouse button was released and simulate its up event
            switch (e.Button)
            {
                case MouseButtons.Left:
                    mouse_event(MOUSEEVENTF_LEFTUP, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
                case MouseButtons.Right:
                    mouse_event(MOUSEEVENTF_RIGHTUP, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
                case MouseButtons.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEUP, clickPoint.X, clickPoint.Y, 0, 0);
                    break;
            }
        }

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;


        // Token: 0x06000011 RID: 17 RVA: 0x00002590 File Offset: 0x00000790
        private void panFakeForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Alt))
                return;
            bool flag = e.Button == MouseButtons.Left;
            if (flag)
            {
                int x = (sender as Control).Location.X;
                int y = (sender as Control).Location.Y;
                Point point = base.PointToClient(Cursor.Position);
                Point location = new Point(point.X - this.mCentralXOffset - x, point.Y - this.mCentralYOffset - y);
                ClampLocationWithinScreen(ref location,(sender as Control));
                (sender as Control).Parent.Location = location;
                this.Refresh();
            }
        }

        private void ClampLocationWithinScreen(ref Point location, Control movable)
        {
            if (!ClipEnabled)
                return;
            Rectangle screenBounds = Screen.AllScreens[screenIndex].Bounds;

            // Calculate the maximum X and Y coordinates within the screen bounds
            int maxX = screenBounds.Width - movable.Width;
            int maxY = screenBounds.Height - movable.Height;

            // Clamp the X and Y coordinates of the location
            location.X = Math.Max(0, Math.Min(location.X, maxX));
            location.Y = Math.Max(0, Math.Min(location.Y, maxY));
        }


        private void panFakeForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point point = base.PointToClient(Cursor.Position);
                rightClickMenuStrip.Show(this, point);
            }
            else if (e.Button == MouseButtons.Middle)
            { 
                if (closeTimer.Enabled)
                    closeTimer.Stop();
                else
                    closeTimer.Start();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void feedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //this.panFakeForm.Size = new Size(panFakeForm.Size.Width * 2, panFakeForm.Size.Height*2);
            this.panHolder.Size = new Size(panHolder.Size.Width * 2, panHolder.Size.Height*2);
            //TextLabel.MaximumSize = new Size(this.panHolder.Size.Width, 0);
        }

        private void unfeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //this.panFakeForm.Size = new Size(panHolder.Size.Width / 2, panFakeForm.Size.Height / 2);
            this.panHolder.Size = new Size(panHolder.Size.Width / 2, panHolder.Size.Height / 2);
            //TextLabel.MaximumSize = new Size(this.panHolder.Size.Width, 0);
        }

        private async void reloadImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadClip();
        }

        private void toggleClipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClipEnabled = !ClipEnabled;
        }

        private void grayToolStripMenuItem_Click(object sender, EventArgs e) => ChangeTransparancyColor(Color.Gray);

        private void fushiaToolStripMenuItem_Click(object sender, EventArgs e) => ChangeTransparancyColor(Color.Fuchsia);

        private void greenToolStripMenuItem_Click(object sender, EventArgs e) => ChangeTransparancyColor(Color.Green);

        private void whiteToolStripMenuItem1_Click(object sender, EventArgs e) => ChangeTransparancyColor(Color.White);

        private void resetSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.panHolder.Size = new Size(size.Width,size.Height);
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stepSize = 10;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            stepSize = 5;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            stepSize = 2;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            stepSize = 25;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            stepSize = 50;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            stepSize = 100;
        }

        private void toggleScrollToolStripMenuItem_Click(object sender, EventArgs e) { }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scrollMode = ScrollMode.None;
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scrollMode = ScrollMode.All;
        }

        private void widthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scrollMode = ScrollMode.Width;
        }

        private void heightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scrollMode = ScrollMode.Height;
        }

        private void closeTimer_Tick(object sender, EventArgs e)
        {
            int delta = 5;
            int newx = panHolder.Size.Width - delta;
            int newy = panHolder.Size.Height - delta;
            this.panHolder.Size = new Size(newx, newy);
            this.Refresh();
            if (panHolder.Size.Width == 0 || panHolder.Size.Height == 0)
                Application.Exit();
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panFakeForm.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void strechToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panFakeForm.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void switchScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var screen = Screen.AllScreens[screenIndex];
            MoveWindow(this.Handle, screen.WorkingArea.Right, screen.WorkingArea.Top, screen.WorkingArea.Width, screen.WorkingArea.Height, true);
            screenIndex++;
            if (screenIndex >= Screen.AllScreens.Count())
                screenIndex = 0;
            screen = Screen.AllScreens[screenIndex];
            this.Location = new Point(screen.WorkingArea.Left, screen.WorkingArea.Top);
            this.Size = this.CalcSize(screen.Bounds);
        }

        private void urlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(LastUrl);
        }

        private void imageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(panFakeForm.Image);
        }

        private void clearCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Image img = panFakeForm.Image;
                panFakeForm.Image = new Bitmap(panFakeForm.Width, panFakeForm.Height);
                if (img != null) img.Dispose();
                panFakeForm.Image = new Bitmap(panFakeForm.Width, panFakeForm.Height);
                using (Graphics g = Graphics.FromImage(panFakeForm.Image))
                {

                    // Fill the bitmap with white color
                    g.Clear(Color.White);
                    g.DrawRectangle(new Pen(Color.Red,4),2,2, panFakeForm.Width-4, panFakeForm.Height-4);
                    g.DrawLine(new Pen(Color.Red,4),2,2, panFakeForm.Width-4, panFakeForm.Height-4);
                    g.DrawLine(new Pen(Color.Red,4),panFakeForm.Width-4, 2, 2, panFakeForm.Height-4);
                }

                var path0 = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), ".Pin");
                Directory.Delete(path0, true);

            }
            catch { }
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #region border

        private void noneToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            BorderColor = Color.Transparent;
            this.Refresh();
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BorderColor = Color.Red;
            this.Refresh();
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BorderColor = Color.Blue;
            this.Refresh();
        }

        private void greenToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            BorderColor = Color.Green;
            this.Refresh();
        }

        private void blackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BorderColor = Color.Black;
            this.Refresh();
        }

        private void whiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BorderColor = Color.White;
            this.Refresh();
        }


        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            BorderWidth = 1;
            this.Refresh();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            BorderWidth = 2;
            this.Refresh();
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            BorderWidth = 3;
            this.Refresh();
        }

        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            BorderWidth = 4;
            this.Refresh();
        }

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            BorderWidth = 5;
            this.Refresh();
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            BorderWidth = 10;
            this.Refresh();
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            BorderWidth = 25;
            this.Refresh();
        }

        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            BorderWidth = 50;
            this.Refresh();
        }
#endregion
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            
            if (BorderColor != Color.Transparent)
            {
                Pen p = new Pen(BorderColor, BorderWidth);
                Graphics g = e.Graphics;
                int offset = 3;
                g.DrawRectangle(p, new Rectangle(panHolder.Location.X - offset, panHolder.Location.Y - offset, panHolder.Width + offset, panHolder.Height + offset));
            }
        }

        private void transparentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeTransparencyType();
        }

    }
}
