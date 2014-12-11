using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using starPadSDK.Inq;

namespace SharpShapes
{
    public static class SharpShapesCommands
    {
        private static SwitchModeCommand _switchModeCommand;
        public static SwitchModeCommand SwitchModeCommand
        {
            get
            {
                if (_switchModeCommand == null)
                {
                    _switchModeCommand = new SwitchModeCommand();
                }
                return _switchModeCommand;
            }
        }

        private static RecognizeCommand _recognizeCommand;
        public static RecognizeCommand RecognizeCommand
        {
            get
            {
                if (_recognizeCommand == null)
                {
                    _recognizeCommand = new RecognizeCommand();
                }
                return _recognizeCommand;
            }
        }

        private static ClearCommand _clearCommand;
        public static ClearCommand ClearCommand
        {
            get
            {
                if (_clearCommand == null)
                {
                    _clearCommand = new ClearCommand();
                }
                return _clearCommand;
            }
        }

        private static UndoCommand _undoCommand;
        public static UndoCommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                {
                    _undoCommand = new UndoCommand();
                }
                return _undoCommand;
            }
        }

        private static OpenCommand _openCommand;
        public static OpenCommand OpenCommand
        {
            get
            {
                if (_openCommand == null)
                {
                    _openCommand = new OpenCommand();
                }
                return _openCommand;
            }
        }

        private static SaveCommand _saveCommand;
        public static SaveCommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new SaveCommand();
                }
                return _saveCommand;
            }
        }

        public static void UpdateAllCanExecute()
        {
            _switchModeCommand.FireCanExecuteChanged();
            _recognizeCommand.FireCanExecuteChanged();
            _clearCommand.FireCanExecuteChanged();
            _undoCommand.FireCanExecuteChanged();
        }
    }

    public class SwitchModeCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.shapeCanvas.InqCanvas.Children.Clear();
            mw.shapeCanvas.InqCanvas.Stroqs.Clear();

            if (mw.shapeCanvas.RecognitionMode == Mode.ShapeRecognition)
            {
                mw.shapeCanvas.RecognitionMode = Mode.TemplateRecognitionFeedback;
                mw.grpMode.Header = "Template Recognition Feedback";
            }
            else if (mw.shapeCanvas.RecognitionMode == Mode.TemplateRecognitionFeedback)
            {
                mw.shapeCanvas.RecognitionMode = Mode.TemplateRecognitionDirect;
                mw.grpMode.Header = "Template Recognition Direct";
            }
            else
            {
                mw.shapeCanvas.RecognitionMode = Mode.ShapeRecognition;
                mw.grpMode.Header = "Shape Recognition";
            }
            mw.btnCleanUp.IsEnabled = false;
            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }

    public class RecognizeCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            if (mw.shapeCanvas != null)
            {
                return mw.shapeCanvas.RecognitionMode == Mode.TemplateRecognitionDirect;
            }
            return false;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.shapeCanvas.RecognizeTemplate();

            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }

    public class ClearCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.shapeCanvas.InqCanvas.Children.Clear();
            mw.shapeCanvas.InqCanvas.Stroqs.Clear();
            mw.btnCleanUp.IsEnabled = false;
            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }

    public class UndoCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            if (mw.shapeCanvas != null)
            {
                return mw.shapeCanvas.LastCleanedUpTemplate != null && mw.shapeCanvas.RecognitionMode != Mode.ShapeRecognition;
            }
            return false;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.shapeCanvas.Undo();

            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }

    public class OpenCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "MyStroqs";
            dlg.DefaultExt = ".stroqs";
            dlg.Filter = "Stroqs (.stroqs)|*.stroqs";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                mw.shapeCanvas.InqCanvas.Children.Clear();
                mw.shapeCanvas.InqCanvas.Stroqs.Clear();

                string filename = dlg.FileName;
                Stream stream = File.Open(filename, FileMode.Open);
                BinaryFormatter bFormatter = new BinaryFormatter();
                StroqCollection sc = (StroqCollection)bFormatter.Deserialize(stream);
                stream.Close();
                foreach (Stroq s in sc)
                {
                    //s.Move(new Vec(-400, 0));
                    mw.shapeCanvas.InqCanvas.Stroqs.Add(s);
                    mw.shapeCanvas.StroqAdded(s);
                }
            }
            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }

    public class SaveCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "MyStroqs";
            dlg.DefaultExt = ".stroqs";
            dlg.Filter = "Stroqs (.stroqs)|*.stroqs";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                Stream stream = File.Open(filename, FileMode.Create);
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, mw.shapeCanvas.InqCanvas.Stroqs);
                stream.Close();
            }
            SharpShapesCommands.UpdateAllCanExecute();
        }

        public void FireCanExecuteChanged()
        {
            this.CanExecuteChanged.Invoke(null, null);
        }
    }
}
