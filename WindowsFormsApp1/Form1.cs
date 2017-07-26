using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreentimeWatcher
{
    public partial class WatcherForm : Form
    {
        private const int RES = 64;
        private const int HIST = 8;
        private const int THRESHOLD = 6;
        private int dimx, dimy;
        private Color[,] lastPix;
        private Queue<bool> hist = new Queue<bool>();
        private static readonly HttpClient client = new HttpClient();
        private Timer statusTimer;
        private Timer canPlayTimer;
        private bool inTest = true; // currently testing someone
        private bool dirtyStreak = false; // set true when we get a streak, reset when sent on minute status
        private bool lockDown = true; // false in testing mode, we can close

        public WatcherForm()
        {
            InitializeComponent();

            dimx = Screen.PrimaryScreen.Bounds.Width / RES;
            dimy = Screen.PrimaryScreen.Bounds.Height / RES;

#if DEBUG
            Console.WriteLine("Mode=Debug");
//            lockDown = false;
#endif

            this.webBrowser1.Navigate("http://test.goproblems.com/test/lock/mul.php");

            this.Location = new Point(10, 100);
            InitTimer();
            InitTimerReborn();

            canPlayTimer = new Timer();
            canPlayTimer.Tick += new EventHandler(TimerCanPlay_Tick);
            canPlayTimer.Interval = 1000 * 5; // in miliseconds
            canPlayTimer.Start();

            statusTimer = new Timer();
            statusTimer.Tick += new EventHandler(TimerStatus_Tick);
            statusTimer.Interval = 1000 * 60; // in miliseconds
            statusTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void TimerCanPlay_Tick(object sender, EventArgs e)
        {
            String url = "http://test.goproblems.com/test/lock/canplay.php?uid=0";
            using (var wb = new WebClient())
            {
                try
                {
                    var response = wb.DownloadString(url);
                    Console.WriteLine(response);
                    if (response == "1")
                    {
                        Console.WriteLine("can play");
                    }
                    else
                    {
                        if (!inTest && dirtyStreak)
                        {
                            inTest = true;
                            this.WindowState = FormWindowState.Maximized;
                            this.webBrowser1.Navigate("http://test.goproblems.com/test/lock/mul.php");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }

                //                    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                //                   ser.DeserializeObject(json);
            }
            dirtyStreak = false;
        }

        private void TimerStatus_Tick(object sender, EventArgs e)
        {
            if (dirtyStreak)
            {
                Console.WriteLine("Sending dirty");
                String url = "http://test.goproblems.com/test/lock/srec.php?uid=0";
                //                var responseString = await client.GetStringAsync(url);
                using (var wb = new WebClient())
                {
                    var response = wb.DownloadString(url);
                    Console.WriteLine(response);

//                    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
//                   ser.DeserializeObject(json);
                }
                dirtyStreak = false;
            }
        }

        private Timer timer1;
        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 4000; // in miliseconds
            timer1.Start();
        }

        private void CaptureScreen()
        {
            using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                            Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    try
                    {
                        g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                     Screen.PrimaryScreen.Bounds.Y,
                                     0, 0,
                                     bmpScreenCapture.Size,
                                     CopyPixelOperation.SourceCopy);

                        Color c = bmpScreenCapture.GetPixel(100, 100);
                        Console.WriteLine(c);

                        Color[,] cc = ExtractPixels(bmpScreenCapture);
                        if (lastPix != null)
                        {
                            int delta = DeltaPix(lastPix, cc);
                            hist.Enqueue(delta > THRESHOLD);
                            if (hist.Count > HIST)
                                hist.Dequeue();
                            CheckHistoryStreak();
                        }
                        lastPix = cc;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            }
        }

        private void CheckHistoryStreak()
        {
            bool streak = true;
            foreach (bool b in hist)
            {
                if (!b) streak = false;
            }
            if (hist.Count < HIST) streak = false;
            Console.WriteLine("streak: {0}", streak);
            if (streak) dirtyStreak = true;
        }

        private int DeltaPix(Color[,] p1, Color[,] p2)
        {
            int cnt = 0;
            for (int y = 0; y < dimy; y++)
            {
                for (int x = 0; x < dimx; x++)
                {
                    bool eq = p1[x, y].Equals(p2[x, y]);
                    if (!eq) cnt++;
                    Console.Write(eq ? '.' : 'x');
                }
                Console.WriteLine();
            }
            return cnt;
        }

        private Color[,] ExtractPixels(Bitmap bmp)
        {
            Color[,] cc = new Color[dimx, dimy];
            for (int x = 0; x < dimx; x++)
                for (int y = 0; y < dimy; y++)
                {
                    cc[x, y] = bmp.GetPixel(x * RES, y * RES);
                }
            return cc;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //            isonline();
            if (lockDown)
            {
                this.Location = new Point(10, 100);
                this.Size = new Size(1900, 900);
            }
            CaptureScreen();
        }

        private Timer timerReborn;
        public void InitTimerReborn()
        {
            timerReborn = new Timer();
            timerReborn.Tick += new EventHandler(timerReborn_Tick);
            timerReborn.Interval = 1000 * 60 * 60 * 2; // in miliseconds
            timerReborn.Start();
        }

        private void timerReborn_Tick(object sender, EventArgs e)
        {
            //            isonline();
//            this.WindowState = FormWindowState.Maximized;
 //           this.webBrowser1.Navigate("http://test.goproblems.com/test/lock/mul.php");
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            System.Windows.Forms.HtmlDocument document =
                this.webBrowser1.Document;

            Console.WriteLine(e.Url.ToString());

            if (e.Url.PathAndQuery.EndsWith("fin.html")) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                timerReborn.Stop();
                timerReborn.Start();
                inTest = false;
            }

            if (document != null && document.All["userName"] != null &&
                String.IsNullOrEmpty(
                document.All["userName"].GetAttribute("value")))
            {
                e.Cancel = true;
                System.Windows.Forms.MessageBox.Show(
                    "You must enter your name before you can navigate to " +
                    e.Url.ToString());
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Console.WriteLine(e);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (lockDown)
                e.Cancel = true;
        }
    }
}
