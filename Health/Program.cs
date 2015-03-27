using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace Health
{
    class Program
    {
        private static int _nXPos;
        private static int _startOrb;
        private static int _endOrb;
        private static int _length;
        private static int _endBottomZeroes;
        private static int _startBottomZeroes;
        private static int _startTopZeroes;

        static void Main(string[] args)
        {
            var window = FindWindowByCaption(IntPtr.Zero, "Guild Wars 2");
            //  Console.WriteLine("window = {0}", window);

            RECT rect;
            GetClientRect(window, out rect);

            var width = rect.Width;
            var height = rect.Height;
            // Console.WriteLine("width = {0}, height = {1}", width, height);

            var dc = GetDC(window);
            // Console.WriteLine("dc = {0}", dc);

            _nXPos = width / 2;

            _startBottomZeroes = -1;
            _endBottomZeroes = -1;
            _endOrb = -1;

            _startTopZeroes = -1;
            _startOrb = -1;

            var previous = 0;

            for (var i = 0; i < 200; ++i)
            {
                var nYPos = height - i;
                var pixel = CalculatePixel(dc, _nXPos, nYPos);

                if (_startBottomZeroes == -1)
                {
                    if (pixel == 0)
                        _startBottomZeroes = nYPos;
                }
                else if (_endBottomZeroes == -1)
                {
                    if (pixel != 0)
                        _endBottomZeroes = nYPos;
                }
                else if (_endOrb == -1)
                {
                    if (pixel > previous && pixel - previous > 0x100000)
                    {
                        _endOrb = nYPos;
                    }
                }
                else if (_startTopZeroes == -1)
                {
                    if (pixel == 0)
                    {
                        _startTopZeroes = nYPos;
                        break;
                    }
                }

                previous = pixel;
            }

            for (var nYPos = _startTopZeroes - 1; nYPos < _endOrb; ++nYPos)
            {
                var pixel = CalculatePixel(dc, _nXPos, nYPos);

                if (pixel > previous && pixel - previous > 0x500000)
                {
                    _startOrb = nYPos;
                    break;
                }

                previous = pixel;
            }

            //Console.WriteLine("width = {0}, height = {1}", width, height);
           // Console.WriteLine("startOrb = {0}, endOrb = {1}", _startOrb, _endOrb);

            _length = _endOrb - _startOrb + 1;

            var timer = new Timer(1000);
            timer.Elapsed += (sender, eventArgs) => PrintPercentage(dc);
            timer.Start();

            Console.ReadLine();

            ReleaseDC(window, dc);
        }

        private static int GetPixelAtOffset(int offset, IntPtr dc)
        {
            var shift = Math.Min(offset, _length - offset - 1);
            var pixel = CalculatePixel(dc, _nXPos + shift, _startOrb + offset);
            return pixel;
        }

        private static void PrintPercentage(IntPtr dc)
        {
            var b = CalculatePixel(dc, _nXPos, _startTopZeroes - 1);
            if (b > 0)
            {
                Console.WriteLine("down");
                return;
            }

            //var val = LinearSearch(dc);
            var val = BinarySearch(dc);

            Console.WriteLine("{0}", (_length - val) * 100 / _length);
        }

        private static bool HasBlood(int pixel)
        {
            if (pixel == -1)
                return false;
            var r = pixel >> 16;
            var diff1 = Math.Abs((pixel & 0xff) - r);
            var diff2 = Math.Abs(((pixel >> 8) & 0xff) - r);
            //Console.WriteLine("diff1 = {0}, diff2 = {1}", diff1, diff2);
            return diff1 > 0x10 || diff2 > 0x10;
        }

        private static int LinearSearch(IntPtr dc)
        {
            var val = 0;
            for (; val < _length; ++val)
            {
                var pixel = GetPixelAtOffset(val, dc);
                if (HasBlood(pixel))
                {
                    //Console.WriteLine("current = {0:X6}, previous = {1:X6}", pixel ,GetPixelAtOffset(val - 1, dc));
                    break;
                }
            }
            return val;
        }

        private static int BinarySearch(IntPtr dc)
        {
            var start = 0;
            var end = _length;
            while (start + 1 < end)
            {
                var half = (start + end) / 2;
                //Console.WriteLine("start = {0}, half = {1}, end = {2}", start, half, end);
                var pixel = GetPixelAtOffset(half, dc);
                if (HasBlood(pixel))
                {
                    end = half;
                }
                else
                {
                    start = half;
                }
            }

            return start;
        }

        private static int CalculatePixel(IntPtr dc, int nXPos, int nYPos)
        {
            var pixel = GetPixel(dc, nXPos, nYPos);

            if (pixel > 0xffffff)
            {
                //Console.WriteLine("bad");
                return -1;
            }

            var r = pixel & 0xff;
            var g = pixel >> 8 & 0xff;
            var b = pixel >> 16 & 0xff;

            //Console.WriteLine("({0}, {1}) = 0x{2:X2}{3:X2}{4:X2}", nXPos, nYPos, r, g, b);

            return (int)((r << 16) + (g << 8) + b);
        }

        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        // You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    }
}
