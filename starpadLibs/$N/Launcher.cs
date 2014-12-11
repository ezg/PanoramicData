/**
 * The $N Multistroke Recognizer (C# version)
 *
 *	    Lisa Anthony, Ph.D.
 *		UMBC
 *		Information Systems Department
 * 		1000 Hilltop Circle
 *		Baltimore, MD 21250
 * 		lanthony@umbc.edu
 * 
 *      Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 * The Protractor enhancement was published by Yang Li and programmed
 * here by Lisa Anthony and Jacob O. Wobbrock.
 *
 *	Li, Y. (2010). Protractor: A fast and accurate gesture 
 *	  recognizer. Proceedings of the ACM Conference on Human 
 *	  Factors in Computing Systems (CHI '10). Atlanta, Georgia
 *	  (April 10-15, 2010). New York: ACM Press, pp. 2169-2172.
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2011, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington nor UMBC,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Lisa Anthony 
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
**/

// This class defines the main entry point for the NDollar recognizer.
// It handles the branching to launch NDollar as a GUI program 
// (i.e., for real-time recognition) or as a console program 
// (i.e., for easier batch testing).
// see: http://www.eggheadcafe.com/software/aspnet/33880366/creating-console-applicat.aspx
// and: http://www.rootsilver.com/2007/08/how-to-create-a-consolewindow

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace Recognizer.NDollar.Geometric
{
    /// <summary>
    /// Summary description for Launcher
    /// </summary>
    static class Launcher
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();
        [DllImport("kernel32", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] cmdLine = Environment.GetCommandLineArgs();
            // when invoking on the command line, must include an argument,
            // but it can be anything, since all parameters are in config.xml

            if (cmdLine.Length > 1)
            {
                // if we're already launching this in a console,
                // get the right process and attach to it;
                // otherwise we'll allocate our own
                IntPtr ptr = GetForegroundWindow();
                int u;
                GetWindowThreadProcessId(ptr, out u);
                Process process = Process.GetProcessById(u);
                if (!AttachConsole(process.Id)) 
                    AllocConsole();
                RunConsoleVersion();
                FreeConsole();
                return;
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }

        static void RunConsoleVersion()
        {
            // note: this code is similar to MainForm.TestBatch_Click, which is invoked
            // when running the GUI version

            string samplesDir = NDollarParameters.Instance.SamplesDirectory;
            bool include1D = NDollarParameters.Instance.Include1D;
            bool include2D = NDollarParameters.Instance.Include2D;

            SamplesCollection categoriesByUser;
            GeometricRecognizer _rec = new GeometricRecognizer();

            // create the set of filenames to read in
            Directory.SetCurrentDirectory(samplesDir);
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            FileInfo[] allXMLFiles = dir.GetFiles("*.xml");
            string[] allXMLFilenames = new string[allXMLFiles.Length];

            int count = 0;
            Console.Write("Counting Gesture files");
            foreach (FileInfo fi in allXMLFiles)
            {
                allXMLFilenames[count] = dir.FullName + "/" + fi.Name;
                count++;
                Console.Write(".");
            }
            Console.WriteLine();
            Console.WriteLine("Number of files: " + count);

            // read them
            categoriesByUser = _rec.AssembleBatch(allXMLFilenames, include1D, include2D);

            if (categoriesByUser != null)
            {
                // do the recognition
                if (_rec.TestBatch(categoriesByUser, dir.ToString()))
                {
                    Console.WriteLine("Testing complete.");
                }
                else
                {
                    Console.WriteLine("There was an error writing the output file during testing.");
                }
            }
            else // error assembling batch
            {
                Console.WriteLine("Unreadable files, or unbalanced number of gestures in categories.");
            }
        }
    }
}
