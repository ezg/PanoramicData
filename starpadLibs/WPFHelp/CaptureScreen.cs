using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace starPadSDK.WPFHelp
{
    public class CaptureScreen
    {

        public static Bitmap CaptureDesktop()
        {
            Win32Stuff.SIZE size;
            IntPtr hBitmap;
            IntPtr hDC = Win32Stuff.GetDC(Win32Stuff.GetDesktopWindow());
            IntPtr hMemDC = GDIStuff.CreateCompatibleDC(hDC);

            size.cx = Win32Stuff.GetSystemMetrics
                      (Win32Stuff.SM_CXSCREEN);

            size.cy = Win32Stuff.GetSystemMetrics
                      (Win32Stuff.SM_CYSCREEN);

            hBitmap = GDIStuff.CreateCompatibleBitmap(hDC, size.cx, size.cy);

            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hOld = (IntPtr)GDIStuff.SelectObject
                                       (hMemDC, hBitmap);

                GDIStuff.BitBlt(hMemDC, 0, 0, size.cx , size.cy, hDC,
                                               0, 0, GDIStuff.SRCCOPY);

                GDIStuff.SelectObject(hMemDC, hOld);
                GDIStuff.DeleteDC(hMemDC);
                Win32Stuff.ReleaseDC(Win32Stuff.GetDesktopWindow(), hDC);
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                GDIStuff.DeleteObject(hBitmap);
                GC.Collect();
                return bmp;
            }
            return null;
      
        }
        public static Bitmap CaptureCursor() {
            int hotx= 0, hoty=0;
            int x = 0, y = 0;
            return CaptureCursor(ref x, ref y, ref hotx, ref hoty);
        } 
        public static Bitmap CaptureCursor(ref int x, ref int y, ref int hotx, ref int hoty) 
        {
            Bitmap bmp = null;
            Win32Stuff.ICONINFO icInfo;
            Win32Stuff.CURSORINFO ci = new Win32Stuff.CURSORINFO();
            ci.cbSize = Marshal.SizeOf(ci);
            if (Win32Stuff.GetCursorInfo(ref ci))
            {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING) {
                    IntPtr hicon = Win32Stuff.CopyIcon(ci.hCursor);
                    if (Win32Stuff.GetIconInfo(hicon, out icInfo)) {
                        hotx = ((int)icInfo.xHotspot);
                        hoty = ((int)icInfo.yHotspot);
                        x = ci.ptScreenPos.x - hotx;
                        y = ci.ptScreenPos.y - hoty;
                        if (icInfo.hbmColor != (IntPtr)0)
                            WPFHelp.ScreenCapture.GDI32.DeleteObject(icInfo.hbmColor);
                        if (icInfo.hbmMask != (IntPtr)0)
                            WPFHelp.ScreenCapture.GDI32.DeleteObject(icInfo.hbmMask);

                        Icon ic = Icon.FromHandle(hicon);
                        bmp = ic.ToBitmap();
                        ic.Dispose();
                    }
                    Win32Stuff.DestroyIcon(hicon);
                }
            }
            return bmp;
        }
        public static Bitmap CaptureDesktopWithCursor() {
            int cursorX = 0;
            int cursorY = 0;
            int hotx = 0;
            int hoty = 0;
            Bitmap desktopBMP;
            Bitmap cursorBMP;
            Graphics g;
            Rectangle r;

            desktopBMP = CaptureDesktop();
            cursorBMP = CaptureCursor(ref cursorX, ref cursorY, ref hotx, ref hoty);
            if (desktopBMP != null) {
                if (cursorBMP != null) {
                    r = new Rectangle(cursorX, cursorY, cursorBMP.Width, cursorBMP.Height);
                    g = Graphics.FromImage(desktopBMP);
                    g.DrawImage(cursorBMP, r);
                    g.Flush();

                    return desktopBMP;
                }
                else
                    return desktopBMP;
            }

            return null;

        }

        const int DI_MASK = 0x0001;
        const int DI_IMAGE     =   0x0002;
        const int DI_NORMAL  =     0x0003;
        const int DI_COMPAT   =    0x0004;
        const int SRCAND     = 0x008800C6; /* dest = source AND dest          */
        const int SRCCOPY = 0x00CC0020; /* dest = source                   */
        const int SRCPAINT = 0x00EE0086;
        const int SRCINVERT = 0x00660046; /* dest = source XOR dest          */
        const int NOTSRCCOPY = 0x00330008; /* dest = (NOT source)             */
        const int NOTSRCERASE = 0x001100A6; /* dest = (NOT src) AND (NOT dest) */
        const int MERGECOPY = 0x00C000CA; /* dest = (source AND pattern)     */
        const int MERGEPAINT = 0x00BB0226; /* dest = (NOT source) OR dest     */
        static void HackInPlaceOfXOR(Rectangle r, IntPtr compositeGHdc, IntPtr maskHdc) {
            IntPtr invertSrcHdc = WPFHelp.ScreenCapture.GDI32.CreateCompatibleDC(maskHdc);
            IntPtr invertSrc = WPFHelp.ScreenCapture.GDI32.CreateCompatibleBitmap(maskHdc, r.Width, r.Height);
            WPFHelp.ScreenCapture.GDI32.SelectObject(invertSrcHdc, invertSrc);
            GDIStuff.BitBlt(invertSrcHdc, 0, 0, r.Width, r.Height, maskHdc, 0, r.Height, NOTSRCCOPY);// (uint)0, (IntPtr)whiteBrush, DI_NORMAL);
            GDIStuff.BitBlt(compositeGHdc, r.Left, r.Top, r.Width, r.Height, invertSrcHdc, 0, 0, SRCAND);// (uint)0, (IntPtr)whiteBrush, DI_NORMAL)
            // real code for handling XORing of Icons onto Bitmaps
            //GDIStuff.BitBlt(compositeGHdc, r.Left, r.Top, r.Width, r.Height, maskHdc, 0, 0, SRCAND);
            //GDIStuff.BitBlt(compositeGHdc, r.Left, r.Top, r.Width, r.Height, maskHdc, 0, r.Height, SRCINVERT);
            WPFHelp.ScreenCapture.GDI32.DeleteObject(invertSrc);
            WPFHelp.ScreenCapture.GDI32.DeleteDC(invertSrcHdc);
        }

        public static Bitmap AddCursorToBitmap(Point where, Bitmap backgroundBMP) {
            Bitmap compositeBMP = backgroundBMP;
            Bitmap cursorBMP = CaptureCursor();
            if (backgroundBMP != null && cursorBMP != null)
                using (Graphics compositeG = Graphics.FromImage(compositeBMP)) {
                    Win32Stuff.CURSORINFO ci = new Win32Stuff.CURSORINFO();
                    ci.cbSize = Marshal.SizeOf(ci);

                    if (Win32Stuff.GetCursorInfo(ref ci) && ci.flags == Win32Stuff.CURSOR_SHOWING) {
                        int hotx, hoty;
                        IntPtr hicon;
                        Win32Stuff.ICONINFO icInfo = getIconInfo(ci, out hicon, out hotx, out hoty);
                        Rectangle r = new Rectangle(where.X-hotx, where.Y-hoty, cursorBMP.Width, cursorBMP.Height);
                        if (ci.hCursor == System.Windows.Forms.Cursors.IBeam.Handle && icInfo.hbmColor == (IntPtr)0) {
                            IntPtr maskHdc = WPFHelp.ScreenCapture.GDI32.CreateCompatibleDC((IntPtr)0);
                            WPFHelp.ScreenCapture.GDI32.SelectObject(maskHdc, icInfo.hbmMask);

                            // hack since SRCINVERT doesn't work -- this makes ibeam cursor show up
                            IntPtr compositeGHdc = compositeG.GetHdc();
                            HackInPlaceOfXOR(r, compositeGHdc, maskHdc);
                            compositeG.ReleaseHdc();

                            WPFHelp.ScreenCapture.GDI32.DeleteDC(maskHdc);
                        }
                        else
                            compositeG.DrawImage(cursorBMP, r);

                        Win32Stuff.DestroyIcon(hicon);

                        if (icInfo.hbmColor != (IntPtr)0)
                            WPFHelp.ScreenCapture.GDI32.DeleteObject(icInfo.hbmColor);
                        if (icInfo.hbmMask != (IntPtr)0)
                            WPFHelp.ScreenCapture.GDI32.DeleteObject(icInfo.hbmMask);
                        //ComSupport.DrawIconEx(compositeGHdc, r.Left, r.Top, ci.hCursor, r.Width, r.Height, (uint)0, (IntPtr)whiteBrush, DI_NORMAL);
                        //g.ReleaseHdc();
                        //Icon ic = Icon.FromHandle(hicon);
                        //compositeG.DrawIcon(ic, r);
                    }
                    compositeG.Flush();
                    compositeG.Dispose();
                }
            if (cursorBMP != null)
                cursorBMP.Dispose();

            return compositeBMP;
        }

        private static Win32Stuff.ICONINFO getIconInfo(Win32Stuff.CURSORINFO ci, out IntPtr hicon, out int hotx, out int hoty)
        {
            Win32Stuff.ICONINFO icInfo;
            hicon = Win32Stuff.CopyIcon(ci.hCursor);

            hotx = 0;
            hoty = 0;

            if (Win32Stuff.GetIconInfo(hicon, out icInfo))
            {
                hotx = ((int)icInfo.xHotspot);
                hoty = ((int)icInfo.yHotspot);
            }
            return icInfo;
        }
    }
}
