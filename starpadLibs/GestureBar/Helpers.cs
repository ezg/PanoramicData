using System;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;
using System.IO;
using System.Windows.Input;
using System.Runtime.InteropServices;


// Useful WPF helper functions
// By Andrew Bragdon

namespace starPadSDK.GestureBarLib.UICommon
{
    public class Helpers
    {
        public static FrameworkElement CloneUsingXaml(FrameworkElement o)
        {
            string xaml = XamlWriter.Save(o);

            xaml = xaml.Replace("Name=\"", "Name=\"" + MakeRandomName());

            FrameworkElement result = (FrameworkElement) XamlReader.Load(new XmlTextReader(new StringReader(xaml)));

            return result;
        }

        public static bool ToBool(bool? q)
        {
            if (q == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected class ManagedBeep
        {
            #region Win32
            [DllImport("user32.dll")]
            static extern void MessageBeep(uint uType);

            const uint MB_OK = 0x00000000;

            const uint MB_ICONHAND = 0x00000010;
            const uint MB_ICONQUESTION = 0x00000020;
            const uint MB_ICONEXCLAMATION = 0x00000030;
            const uint MB_ICONASTERISK = 0x00000040;

            #endregion

            public static void Main()
            {
                MessageBeep(MB_ICONEXCLAMATION);
            }
        }


        public static void MessageBeep()
        {
            ManagedBeep.Main();

            //Console.WriteLine("\a");
        }

        public static FrameworkElement GetParent(FrameworkElement elt)
        {
            return (FrameworkElement)elt.Parent;
        }

        public static ArrayList ExtractPaths(UserControl control)
        {
            ArrayList result = new ArrayList();
            Panel panel = (Panel)control.Content;

            if (panel != null)
            {
                foreach (UIElement elt in panel.Children)
                {
                    if (elt is System.Windows.Shapes.Path)
                    {
                        result.Add((System.Windows.Shapes.Path)elt);
                    }
                }
            }

            return result;
        }

        public static Panel ExtractPanel(UserControl control)
        {
            Panel panel = (Panel)control.Content;

            if (panel != null)
            {
                foreach (UIElement elt in panel.Children)
                {
                    if (elt is Panel)
                    {
                        return (Panel)elt;
                    }
                }
            }

            return null;
        }

        public static Panel ExtractPanelOfName(UserControl control, String matchName)
        {
            matchName = matchName.ToLower();
            Panel panel = (Panel)control.Content;

            if (panel != null)
            {
                foreach (UIElement elt in panel.Children)
                {
                    if (elt is Panel)
                    {
                        Panel found = (Panel)elt;

                        if (found.Name.ToLower().Contains(matchName))
                        {
                            return found;
                        }
                    }
                }
            }

            return null;
        }

        public static FrameworkElement GetParent(FrameworkElement elt, int n)
        {
            for (int i = 0; i < n; i++)
            {
                elt = (FrameworkElement)elt.Parent;
            }

            return elt;
        }

        public static void AddAnimationToStoryboard(AnimationTimeline anim, FrameworkElement elementToAnim, DependencyProperty property, Storyboard board)
        {
            if (elementToAnim.Name == null)
            {
                elementToAnim.RegisterName(MakeRandomName(), elementToAnim);
            }

            Storyboard.SetTargetName(anim, elementToAnim.Name);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            board.Children.Add(anim);
        }

        public static void AddKeyboardShortcut(FrameworkElement parent, 
            Key key, ModifierKeys modifierKeys, 
            ExecutedRoutedEventHandler cmdExecute, CanExecuteRoutedEventHandler cmdCanExecute)
        {
            RoutedCommand NewCommand = new RoutedCommand();

            CommandBinding Binding = new CommandBinding(
                NewCommand,
                cmdExecute,
                cmdCanExecute);

            parent.CommandBindings.Add(Binding);

            KeyGesture keyGesture = new KeyGesture(
                key,
                modifierKeys);

            KeyBinding CmdKeybinding = new KeyBinding(
                NewCommand,
                keyGesture);

            parent.InputBindings.Add(CmdKeybinding);
        }

        public class StoryboardTrigger
        {
            FrameworkContentElement _parent = null;
            string _animationName = "";

            public StoryboardTrigger(FrameworkContentElement parent, string animationName)
            {
                _parent = parent;
                _animationName = animationName;
            }

            public bool Exec
            {
                get
                {
                    return false;
                }

                set
                {
                    Storyboard penDown = (Storyboard)_parent.Resources[_animationName];
                    penDown.Begin(_parent);
                }
            }
        }

        public static bool IsNumeric(char c)
        {
            string find = c + "";
            string numeric = "1234567890";

            return numeric.Contains(find);
        }

        public static bool IsAlpha(char c)
        {
            string find = c + "";
            find = find.ToLower();
            string alpha = "abcdefghijklmnopqrstuvwxyz";

            return alpha.Contains(find);
        }

        public static bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsNumeric(c);
        }

        public static bool IsCSharpSymbol(char c)
        {
            string find = c + "";
            string numeric = ".-+=*/[]{}!%,<>?()&|";

            return numeric.Contains(find);
        }

        public static DateTime ToDateTime(object o)
        {
            try
            {
                return DateTime.Parse(o.ToString());
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public static int ToInt(string str)
        {
            try
            {
                return System.Convert.ToInt32(str);
            }
            catch
            {
                return 0;
            }
        }

        public static double ToDouble(object val)
        {
            try
            {
                return System.Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        public static double ToDouble(string str)
        {
            try
            {
                return System.Convert.ToDouble(str);
            }
            catch
            {
                return 0;
            }
        }

        public static long ToLong(string str)
        {
            try
            {
                return System.Convert.ToInt64(str);
            }
            catch
            {
                return 0;
            }
        }

        public static string ParseOut(string str, string start, string end)
        {
            int s = str.IndexOf(start);

            if (s < 0)
                return "";

            s += start.Length;
            int e = str.IndexOf(end, s + 1);

            if (e < 0)
                return "";

            string temp = str.Substring(s, e - s);
            return temp;
        }

        public static string ParseOut(string str, string start, string end, ref int position)
        {
            int s = str.IndexOf(start, position);

            if (s < 0)
                return "";

            s += start.Length;
            int e = str.IndexOf(end, s + 1);

            position = e + end.Length;

            if (e < 0)
                return "";

            string temp = str.Substring(s, e - s);
            return temp;
        }


        public static void BringToFront(FrameworkElement elt)
        {
            Canvas.SetZIndex(elt, GetHighestZIndex( (Panel) elt.Parent ) + 1);
        }

        public static void SendToBack(FrameworkElement elt)
        {
            Canvas.SetZIndex(elt, 0);
        }

        public static void SendToBack2(Panel panel)
        {
            Panel parent = (Panel)panel.Parent;
            parent.Children.Remove(panel);
            parent.Children.Insert(0, panel);
        }

        public static int GetHighestZIndex(Panel panel)
        {
            int result = 0;

            foreach (FrameworkElement elt in panel.Children)
            {
                result = Math.Max(Canvas.GetZIndex(elt), result);
            }

            return result;
        }

        public static double Max(double a, double b, double c)
        {
            return Math.Max(Math.Max(a, b), c);
        }

        public static double Max(double a, double b, double c, double d)
        {
            return Math.Max(Math.Max(a, b), Math.Max(c, d));
        }

        public static void SizeToFit(FrameworkElement elt, double desiredWidth, double desiredHeight)
        {
            Helpers.ForceUpdateLayout(elt);

            Rect rc = GetLocation(elt, (FrameworkElement)elt.Parent);

            double scaleFactorX = desiredWidth / rc.Width;
            double scaleFactorY = desiredHeight / rc.Height;

            ApplyScaleTransform(elt, scaleFactorX, scaleFactorY);
        }

        public static void ApplyScaleTransform(FrameworkElement elt, double scaleFactorX, double scaleFactorY)
        {
            TransformGroup group = new TransformGroup();

            group.Children.Add(elt.RenderTransform);
            group.Children.Add(new ScaleTransform(scaleFactorX, scaleFactorY));

            elt.RenderTransform = group;
        }

        public static void ApplyTranslateTransform(FrameworkElement elt, double deltaX, double deltaY)
        {
            TransformGroup group = new TransformGroup();

            group.Children.Add(elt.RenderTransform);
            group.Children.Add(new TranslateTransform(deltaX, deltaY));

            elt.RenderTransform = group;
        }

        public static void ApplyScaleTransform2(FrameworkElement elt, double scaleFactorX, double scaleFactorY)
        {
            TransformGroup group = new TransformGroup();

            group.Children.Add(new ScaleTransform(scaleFactorX, scaleFactorY));
            group.Children.Add(elt.RenderTransform);
            

            elt.RenderTransform = group;
        }

        public static Rect GetLocation(FrameworkElement elt)
        {
            FrameworkElement parent = (FrameworkElement)elt.Parent;

            return GetLocation(elt, parent);
        }

        public static Rect GetLocation(FrameworkElement elt, FrameworkElement parent)
        {
            if (parent == null)
            {
                return new Rect(0, 0, elt.Width, elt.Height);
            }

            elt.UpdateLayout();
            GeneralTransform trans = elt.TransformToAncestor(parent);

            Point topLeft = trans.Transform(new Point(0, 0));
            Point bottomRight = trans.Transform(new Point(elt.ActualWidth, elt.ActualHeight));

            Rect result = new Rect(topLeft, bottomRight);
            return result;
        }

        public static Rect GetLocationTrans(FrameworkElement elt, FrameworkElement transTo)
        {
            FrameworkElement parent = (FrameworkElement)elt.Parent;

            if (parent == null)
            {
                return new Rect(0, 0, elt.Width, elt.Height);
            }

            elt.UpdateLayout();
            GeneralTransform trans = elt.TransformToAncestor(parent);

            Point topLeft = trans.Transform(new Point(0, 0));
            Point bottomRight = trans.Transform(new Point(elt.ActualWidth, elt.ActualHeight));

            topLeft = TransformPointFromAtoB(topLeft, parent, transTo);
            bottomRight = TransformPointFromAtoB(bottomRight, parent, transTo);

            Rect result = new Rect(topLeft, bottomRight);
            return result;
        }

        public static int GetLastBackslash(string fileName)
        {
            int lastBackslash = 0;

            for (int i = 0; i < fileName.Length; i++)
            {
                if (fileName[i] == '\\')
                    lastBackslash = i;
            }

            return lastBackslash;
        }

        public static string Substring(string str, int start, int end)
        {
            return str.Substring(start, end - start);
        }

        public static string GetFileTitle(string fileName)
        {
            try
            {
                int lastBackslash = GetLastBackslash(fileName);

                string result = Substring(fileName, lastBackslash + 1, fileName.Length - 1);
                return result;
            }
            catch
            {
                return "";
            }
        }

        public static void EnumerateAncestors(FrameworkElement elt, ArrayList output)
        {
            if (elt == null)
                return;

            output.Add(elt);

            try
            {
                EnumerateAncestors((FrameworkElement)elt.Parent, output);
            }
            catch
            {
            }
        }

        public static FrameworkElement GetEldestAncestor(FrameworkElement elt)
        {
            ArrayList list = new ArrayList();
            EnumerateAncestors(elt, list);

            return (FrameworkElement)list[list.Count - 1];
        }

        public static Panel GetEldestPanelAncestor(FrameworkElement elt)
        {
            ArrayList list = new ArrayList();
            EnumerateAncestors(elt, list);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Panel)
                {
                    return (Panel)list[i];
                }
            }

            return null;
        }

        public static Panel GetEldestVisiblePanelAncestor(FrameworkElement elt)
        {
            ArrayList list = new ArrayList();
            EnumerateAncestors(elt, list);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Panel)
                {
                    Panel result = (Panel)list[i];

                    if (result.IsVisible)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        public static ArrayList GetAllChildren(Panel panel)
        {
            ArrayList list = new ArrayList();
            GetAllChildren(panel, list);

            return list;
        }

        public static void GetAllChildren(Panel panel, ArrayList output)
        {
            output.Add(panel);

            foreach (FrameworkElement elt in panel.Children)
            {
                if (elt is Panel)
                {
                    GetAllChildren((Panel)elt, output);
                }
                else if (elt is ContentControl)
                {
                    output.Add(elt);

                    if (((ContentControl)elt).Content is Panel)
                    {
                        GetAllChildren((Panel)((ContentControl)elt).Content, output);
                    }
                    else
                    {
                        output.Add(((ContentControl)elt).Content);
                    }
                }
                else if (elt is UserControl)
                {
                    output.Add(elt);

                    if (((UserControl)elt).Content is Panel)
                    {
                        GetAllChildren((Panel)((UserControl)elt).Content, output);
                    }
                    else
                    {
                        output.Add(((UserControl)elt).Content);
                    }
                }
                else if (elt is Decorator)
                {
                    output.Add(elt);

                    UIElement decChild = ((Decorator)elt).Child;

                    if (decChild is Panel)
                    {
                        GetAllChildren((Panel)decChild, output);
                    }
                    else
                    {
                        output.Add(decChild);
                    }
                }
                else
                {
                    output.Add(elt);
                }
            }
        }

        public static void ForceUpdateLayout(FrameworkElement elt)
        {
            GetEldestAncestor(elt).UpdateLayout();
        }

        public static FrameworkElement GetCommonAncestor(FrameworkElement eltA, FrameworkElement eltB)
        {
            ArrayList ancestorsA = new ArrayList();
            ArrayList ancestorsB = new ArrayList();

            EnumerateAncestors(eltA, ancestorsA);
            EnumerateAncestors(eltB, ancestorsB);

            foreach (FrameworkElement elt in ancestorsA)
            {
                if (ancestorsB.Contains(elt))
                    return elt;
            }

            return null;
        }

        public static Point TransformPointFromAtoB(Point pt, FrameworkElement srcElt, FrameworkElement destElt)
        {
            FrameworkElement eltParent = GetCommonAncestor(srcElt, destElt);

            GeneralTransform transToParent = srcElt.TransformToAncestor(eltParent);
            GeneralTransform transToDest = eltParent.TransformToDescendant(destElt);

            Point result = transToParent.Transform(pt);
            result = transToDest.Transform(result);

            return result;
        }

        public static void RemoveLastChar(TextBlock text)
        {
            text.ContentEnd.DeleteTextInRun(-1);
        }

        public static void RemoveFirstChar(TextBlock text)
        {
            text.ContentStart.DeleteTextInRun(1);
        }

        public static bool CheckBold(TextBlock text, int start, int end)
        {
            try
            {
                TextRange range = new TextRange(text.ContentStart.GetPositionAtOffset(start),
                    text.ContentStart.GetPositionAtOffset(end));

                FontWeight w = (FontWeight)range.GetPropertyValue(TextElement.FontWeightProperty);

                return w == FontWeights.Bold;
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckBold(TextBlock text, TextPointer start, TextPointer end)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                FontWeight w = (FontWeight)range.GetPropertyValue(TextElement.FontWeightProperty);

                return w == FontWeights.Bold;
            }
            catch
            {
                return false;
            }
        }

        public static void MakeBold(TextBlock text, int start, int end, bool set)
        {
            MakeBold(text, text.ContentStart.GetPositionAtOffset(start),
                text.ContentStart.GetPositionAtOffset(end), set);
        }

        public static void MakeFontSize(TextBlock text, TextPointer start, TextPointer end, double fontSize)
        {
            try
            {
                TextRange range = new TextRange(start, end);
                //Ming-Li 7/14 It seems to be needed
                text.FontSize = fontSize;

                range.ApplyPropertyValue(TextElement.FontSizeProperty,
                    fontSize);
            }
            catch
            {
            }
        }

        public static double CheckFontSize(TextBlock text, TextPointer start, TextPointer end)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                return (double)range.GetPropertyValue(TextElement.FontSizeProperty);
            }
            catch
            {
                return 0;
            }
        }

        public static void MakeFontFamily(TextBlock text, TextPointer start, TextPointer end, string fontName)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                range.ApplyPropertyValue(TextElement.FontFamilyProperty,
                    new FontFamily(fontName));
            }
            catch
            {
            }
        }

        public static string CheckFontFamily(TextBlock text, TextPointer start, TextPointer end)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                FontFamily v = (FontFamily)range.GetPropertyValue(TextElement.FontFamilyProperty);

                return v.Source;
            }
            catch
            {
                return "";
            }
        }

        public static void MakeBold(TextBlock text, TextPointer start, TextPointer end, bool set)
        {
            TextRange range = new TextRange(start, end);

            range.ApplyPropertyValue(TextElement.FontWeightProperty,
                set ? FontWeights.Bold : FontWeights.Normal);
        }

        public static void MakeColor(TextBlock text, TextPointer start, TextPointer end, Color color)
        {
            TextRange range = new TextRange(start, end);

            range.ApplyPropertyValue(TextElement.ForegroundProperty,
                new SolidColorBrush(color));
        }

        public static bool CheckItalic(TextBlock text, TextPointer start, TextPointer end)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                FontStyle s = (FontStyle)range.GetPropertyValue(TextElement.FontStyleProperty);

                return s == FontStyles.Italic;
            }
            catch
            {
                return false;
            }
        }

        public static void MakeItalic(TextBlock text, int start, int end, bool set)
        {
            MakeItalic(text, text.ContentStart.GetPositionAtOffset(start),
                text.ContentStart.GetPositionAtOffset(end), set);
        }

        public static void MakeVerticalAlignment(TextBlock text, System.Windows.BaselineAlignment align)
        {
            MakeVerticalAlignment(text, text.ContentStart, text.ContentEnd, align);
        }

        public static void MakeVerticalAlignment(TextBlock text, TextPointer start, TextPointer end, System.Windows.BaselineAlignment align)
        {
            TextRange range = new TextRange(start, end);

            range.ApplyPropertyValue(Inline.BaselineAlignmentProperty, align);
            text.VerticalAlignment = VerticalAlignment.Bottom;
        }

        public static void MakeItalic(TextBlock text, TextPointer start, TextPointer end, bool set)
        {
            TextRange range = new TextRange(start, end);

            range.ApplyPropertyValue(TextElement.FontStyleProperty,
                set ? FontStyles.Italic : FontStyles.Normal);
        }

        public static void MakeUnderline(TextBlock text, int start, int end, bool set)
        {
            MakeUnderline(text, text.ContentStart.GetPositionAtOffset(start),
                text.ContentStart.GetPositionAtOffset(end), set);
        }

        public static bool CheckUnderline(TextBlock text, TextPointer start, TextPointer end)
        {
            try
            {
                TextRange range = new TextRange(start, end);

                TextDecorationCollection col =
                    (TextDecorationCollection)range.GetPropertyValue(Inline.TextDecorationsProperty);

                foreach (TextDecoration d in col)
                {
                    if (d.Location == TextDecorationLocation.Underline)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void MakeUnderline(TextBlock text, TextPointer start, TextPointer end, bool set)
        {
            TextRange range = new TextRange(start, end);


            TextDecorationCollection col = new TextDecorationCollection();

            if (set)
            {
                col.Add(new TextDecoration(TextDecorationLocation.Underline,
                    new Pen(new SolidColorBrush(Colors.Black), 1.0), -1.0,
                    TextDecorationUnit.Pixel, TextDecorationUnit.Pixel));
            }

            range.ApplyPropertyValue(Inline.TextDecorationsProperty, col);
        }

        public static Rect GetSubstringRect(TextBlock myText, TextPointer start, TextPointer end)
        {
            Rect rcA = start.GetCharacterRect(LogicalDirection.Forward);
            Rect rcB = end.GetCharacterRect(LogicalDirection.Forward);

            return new Rect(rcA.TopLeft, rcB.BottomRight);
        }

        public static string GetAppDrive()
        {
            string dir = GetAppDir();

            try
            {
                if (dir.StartsWith("\\"))
                    dir = dir.Substring(2);
            }
            catch
            {
            }

            dir = dir.Substring(0, 2);

            if (dir.Substring(dir.Length - 1, 1).CompareTo("\\") != 0)
                dir += "\\";

            return dir;
        }

        public static string GetAppDir()
        {
            string ans = typeof(Helpers).Assembly.Location;//Application.ExecutablePath;

            while (ans.Length > 0)
            {
                if (ans[ans.Length - 1] == '\\')
                    break;
                else
                    ans = ans.Substring(0, ans.Length - 1);
            }

            if (ans.Substring(ans.Length - 1, 1) != "\\")
                ans += "\\";

            return ans;
        }

        public static Rect GetSubstringRect(TextBlock myText, int start, int end, Canvas invisibleCanvas)
        {
            Rect rcA = myText.ContentStart.GetPositionAtOffset(start).GetCharacterRect(LogicalDirection.Forward);
            Rect rcB = myText.ContentStart.GetPositionAtOffset(end).GetCharacterRect(LogicalDirection.Forward);

            return new Rect(rcA.TopLeft, rcB.BottomRight);



            TextBlock tempCopy = CloneTextBlock(myText);
            Helpers.MakeIncreasing(ref start, ref end);

            invisibleCanvas.Children.Add(tempCopy);
            tempCopy.UpdateLayout();

            double height = GetLocation(tempCopy, (FrameworkElement)tempCopy.Parent).Height;



            // Measure start

            for (int i = start /* +1 */; i < tempCopy.Text.Length; i++)
            {
                RemoveLastChar(tempCopy);
            }

            tempCopy.UpdateLayout();
            double left = GetLocation(tempCopy, (FrameworkElement)tempCopy.Parent).Right;

            // Measure end

            tempCopy = CloneTextBlock(myText);
            invisibleCanvas.Children.Add(tempCopy);

            for (int i = end + 1; i < myText.Text.Length; i++)
            {
                RemoveLastChar(tempCopy);
            }

            tempCopy.UpdateLayout();
            double right = GetLocation(tempCopy, (FrameworkElement)tempCopy.Parent).Right;

            Rect rectangle = new Rect(left, 0, right - left, height);

            invisibleCanvas.Children.Remove(tempCopy);



            // Done



            return rectangle;
        }

        public static Rect GetSubstringRect(TextBox myText, int start, int end)
        {
            Helpers.MakeIncreasing(ref start, ref end);

            myText.UpdateLayout();
            string str = myText.Text;

            // Start

            double left = 0;
            double right = 0;
            double height = Helpers.GetLocation(myText, (FrameworkElement)myText.Parent).Height;// myText.ActualHeight;

            if (start > 0)
            {
                myText.Text = str.Substring(0, start - 1);
                myText.UpdateLayout();

                left = Helpers.GetLocation(myText, (FrameworkElement)myText.Parent).Width;// myText.ActualWidth;
            }

            // End

            myText.Text = str.Substring(0, end + 1);
            myText.UpdateLayout();

            right = Helpers.GetLocation(myText, (FrameworkElement)myText.Parent).Width;// myText.ActualWidth;

            // Done

            myText.Text = str;

            return new Rect(Helpers.GetLocation(myText, (FrameworkElement)myText.Parent).Left + left,
                Helpers.GetLocation(myText, (FrameworkElement)myText.Parent).Top,
                right - left,
                height);

            //Rect rcStart = myText.GetRectFromCharacterIndex(start);
            //Rect rcEnd = myText.GetRectFromCharacterIndex(end);

            //return new Rect(rcStart.TopLeft, rcEnd.BottomRight);
        }

        public static Rect GetSubstringRect(string str, int start, int end)
        {
            Helpers.MakeIncreasing(ref start, ref end);

            TextBox myText = new TextBox();
            myText.Text = str;

            return GetSubstringRect(myText, start, end);
        }

        public static Rect GetSubstringRect(Label label, int start, int end, Canvas invisibleCanvas)
        {
            Helpers.MakeIncreasing(ref start, ref end);

            TextBox textbox = CloneLabel(label);

            invisibleCanvas.Children.Add(textbox);

            Rect result = GetSubstringRect(textbox, start, end);

            invisibleCanvas.Children.Remove(textbox);

            return result;
        }

        public static TextBox CloneLabel(Label label)
        {
            TextBox textbox = new TextBox();

            //    textbox.RenderTransform = label.RenderTransform;
            //    textbox.Width = label.Width;
            //    textbox.Height = label.Height;

            textbox.FontFamily = label.FontFamily;
            textbox.FontSize = label.FontSize;
            textbox.FontStretch = label.FontStretch;
            textbox.FontStyle = label.FontStyle;
            textbox.FontWeight = label.FontWeight;

            textbox.Text = (string)label.Content;
            textbox.IsHitTestVisible = false;
            textbox.Opacity = 0;

            return textbox;
        }

        public static TextBlock CloneTextBlock(TextBlock block)
        {


            TextBlock textbox = new TextBlock();

            textbox.FontFamily = block.FontFamily;
            textbox.FontSize = block.FontSize;
            textbox.FontStretch = block.FontStretch;
            textbox.FontStyle = block.FontStyle;
            textbox.FontWeight = block.FontWeight;

            textbox.Inlines.AddRange(block.Inlines);
            textbox.IsHitTestVisible = false;
            textbox.Opacity = 0;

            return textbox;
        }

        public static int HitTestString(Label label, Point pt, Canvas invisibleCanvas)
        {
            TextBox textbox = CloneLabel(label);

            invisibleCanvas.Children.Add(textbox);

            int result = HitTestString(textbox, pt);

            invisibleCanvas.Children.Remove(textbox);

            return result;
        }

        public static int HitTestString(TextBox textbox, Point pt)
        {
            // Hit test

            for (int i = 0; i < textbox.Text.Length; i++)
            {
                Rect rc = GetSubstringRect(textbox, i, i);
                //                Point localPoint = Helpers.TransformPointFromAtoB(pt, -, textbox);

                if (rc.Contains(pt)) //((pt.X >= rc.Left) && (pt.X <= rc.Right)) //
                    return i;
            }

            // No hit

            return -1;
        }

        public static TextPointer HitTestStringPos(TextBlock textbox, Point pt)
        {
            return textbox.GetPositionFromPoint(pt, true);
        }

        public static int HitTestString(TextBlock textbox, Point pt)
        {
            return Math.Abs(textbox.GetPositionFromPoint(pt, true).GetOffsetToPosition(textbox.ContentStart));



            //// Hit test

            //for (int i = 0; i < textbox.Text.Length; i++)
            //{
            //    Rect rc = GetSubstringRect(textbox, i, i);
            //    //                Point localPoint = Helpers.TransformPointFromAtoB(pt, -, textbox);

            //    if (rc.Contains(pt)) //((pt.X >= rc.Left) && (pt.X <= rc.Right)) //
            //        return i;
            //}

            //// No hit

            //return -1;
        }

        public static void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        public static void Swap(ref double a, ref double b)
        {
            double temp = a;
            a = b;
            b = temp;
        }

        public static void MakeIncreasing(ref int a, ref int b)
        {
            if (b < a)
            {
                Swap(ref a, ref b);
            }
        }

        public static int HitTestString(string str, Point pt)
        {
            TextBox box = new TextBox();
            box.Text = str;

            return HitTestString(box, pt);
        }

        public static string MakeRandomName()
        {
            string name = Guid.NewGuid().ToString();
            name = name.Replace("-", "");

            name = "x_" + name;

            return name;
        }
    }
}
