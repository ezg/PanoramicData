using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.Windows.Ink;
using System.Threading;
using starPadSDK.Geom;
using System.Windows.Input;

using System.Diagnostics;

namespace starPadSDK.GestureBarLib
{
	public interface IDrawingCanvasFactory
	{
        FrameworkElement CreateDrawingCanvas();
	}
}
