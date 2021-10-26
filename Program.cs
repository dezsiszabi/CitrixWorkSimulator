using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CitrixWorkSimulator
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int X
        {
            get { return Left; }
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get { return Top; }
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }
    }

    class Program
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;

        private static List<(string ClassName, IntPtr Handle)> _childWindows = new List<(string ClassName, IntPtr Handle)>();
        private static RegisteredWaitHandle _clicker;
        private static ManualResetEvent _stop;

        static void Main()
        {
            var processes = Process.GetProcessesByName("CDViewer");

            if (processes.Length == 0)
            {
                Console.WriteLine("CDViewer is not running! Exiting.");
                return;
            }

            var citrixMainWindowHandle = processes[0].MainWindowHandle;

            EnumChildWindows(citrixMainWindowHandle, Proc, IntPtr.Zero);

            var idx = _childWindows.FindIndex(elem => elem.ClassName == "CtxICADisp");

            if (idx == -1)
            {
                Console.WriteLine("Child window with class CtxICADisp was not found! Exiting.");
                return;
            }

            var ctxIcaWindowHandle = _childWindows[idx].Handle;

            Console.WriteLine("Starting click simulation... Press Ctrl+C to exit.");

            _stop = new ManualResetEvent(false);
            _clicker = ThreadPool.RegisterWaitForSingleObject(_stop, new WaitOrTimerCallback(ThreadFunc), ctxIcaWindowHandle, 1000, false);

            ConsoleKeyInfo key;

            while ((key = Console.ReadKey()).Key != ConsoleKey.Q)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Spacebar:
                        if (!_stop.WaitOne(0))
                        {
                            _stop.Set();
                        }
                        else
                        {
                            _stop = new ManualResetEvent(false);
                            _clicker = ThreadPool.RegisterWaitForSingleObject(_stop, new WaitOrTimerCallback(ThreadFunc), ctxIcaWindowHandle, 1000, false);
                        }
                        break;
                }
            }
        }

        static void ThreadFunc(object state, bool timedOut)
        {
            if (timedOut)
            {
                var ctxIcaWindowHandle = (IntPtr)state;

                RECT r;
                GetWindowRect(ctxIcaWindowHandle, out r);

                POINT p;
                GetCursorPos(out p);

                SetCursorPos((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);

                SendMessage(ctxIcaWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, new IntPtr(MAKELPARAM(0, 300)));
                SendMessage(ctxIcaWindowHandle, WM_LBUTTONUP, IntPtr.Zero, new IntPtr(MAKELPARAM(0, 300)));

                SetCursorPos(p.X, p.Y);
            }
            else
            {
                _clicker.Unregister(null);
            }
        }

        private static bool Proc(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            _childWindows.Add((className.ToString(), hWnd));
            return true;
        }

        private static int MAKELPARAM(int x, int y)
        {
            return (y << 16) | (x & 0xFFFF);
        }
    }
}
