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

This file describes briefly how to use and modify the NDollar
recognizer implementation in C#, provided for free here as a reference
implementation.

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
~ INSTALLING THE RECOGNIZER ~
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

There is no real installation of the recognizer. Simply unzip the
ndollar-<date>.zip file to a location on your hard drive. This
location will be referred to as ($NDOLLAR_HOME) throughout this
README, but $N does not create or modify any environment variables on
your computer.


~~~~~~~~~~~~~~~~~~~~~~~~~~
~ RUNNING THE RECOGNIZER ~
~~~~~~~~~~~~~~~~~~~~~~~~~~

You have two options of ways to run the $N recognizer, from the GUI
(which has a live mode and a batch mode) or in batch mode from the
command line.

*** Running and Using the GUI ***

   The GUI can be invoked by running the Recognizer.NDollar.exe
   executable Windows program located in either of the
   ($NDOLLAR_HOME)/bin/Release or ($NDOLLAR_HOME)/bin/Debug
   directories.

   1. LIVE MODE RECOGNITION

   To run the recognizer in "live" mode from the GUI, you must load
   one or more templates via the "Gestures"->"Load..." menu. Navigate
   to the directory containing the file(s) you want to load. Select
   one or more (it only loads exactly as many as you select and you
   may not choose directories--if you want to do that, look into batch
   testing). Then, you may draw on the canvas as many strokes as you
   like, and click "Recognize" when you have entered one character /
   symbol. The system will display a recognition result in the top
   right corner of the window, for example:

	 b~17~43~0: 0.4 (105.31px, -44.04(o))
	 (1 out of 1 comparisons made)

   This means that the system recognized the candidate you drew as a
   "b". The best match template name is given along with the score,
   the total distance from the candidate you drew and this template,
   and the angular rotation to achieve the best match. The number of
   comparisons may vary depending on the configuration options (see
   below).

   2. SAVING NEW GESTURES

   You may also save any gestures you have drawn by either drawing one
   and clicking "Save" or clicking the "Gestures"->"Record" menu,
   drawing it, and clicking "Save". When you click "Save", a file
   dialog box opens up to let you choose the name to save your
   gesture. Whatever filename is given to the gesture will be that
   gesture's name when later recognizing, so make it descriptive!

   3. VIEWING LOADED TEMPLATES

   At any time you may view all loaded templates by clicking the
   "Gestures"->"View" menu; a separate window pops up with a different
   tab for each loaded template. Be wary of using this with a lot of
   loaded templates!

   4. CREATING A ROTATION GRAPH

   $N can write out a rotation graph that compares two gestures at all
   possible rotation angles; click the "Graph"->"Rotation Graph..."
   menu and select two gestures (if the "Pair Similar" option was
   checked). The output of this test will be written to an
   Excel-readable file in the same directory as the samples you chose.

   5. RUNNING A BATCH TEST

   You may also use the GUI to launch a batch test of the $N
   recognizer. Click the "Gestures"->"Test Batch..." menu. A file
   dialogue box pops up to allow you to choose the samples to
   test. These samples will be both train and test (the batch test
   will iteratively choose gestures to load as templates and gestures
   to test as candidates). Select one file to load all the samples
   within that directory; select more than one file to load only those
   you have selected.

   Once you have chosen your samples, a set-up box appears, allowing
   you to choose the type of test (single-user or multiple-user:
   choose the appropriate one based on whether your samples come from
   one user or more than one user; this affects only the labels in the
   logfiles generated), and whether or not to include 1D, 2D or both
   types of gestures in the test. The system checks for this based on
   the _1DThreshold defined in NDollarRecognizer.cs; see below for a
   discussion.

   Once the options have been set, click "OK" and a progress bar will
   be drawn across the middle of the $N window. If you selected more
   than one user, the progress bar will restart at the end of every
   user's test. When the test is complete, a message box pops up
   saying "Testing complete!"

   The output of the tests will be written in two .csv files
   (ndollar_main_<timestamp>.csv and ndollar_data_<timestamp>.csv) in
   the same directory as your samples were located. The ndollar_main
   file contains a roll-up of the recognition accuracy per symbol and
   number of templates loaded; the ndollar_data file contains the
   output of every single recognition test performed during the
   batch. Note that for large batch tests, some versions of Excel may
   not be able to fully open the .csv file. The contents of these
   files should be fairly self-explanatory.


*** Running from the Command Line ***

   The command line version only supports batch testing.

   From a command prompt, navigate to either of the
   ($NDOLLAR_HOME)/bin/Release or ($NDOLLAR_HOME)/bin/Debug
   directories. Type the following:

   > ./Recognizer.NDollar.exe cmd

   This will begin the batch mode of the recognizer, which reads the
   ($NDOLLAR_HOME)/conf/config.xml file for the $N parameters,
   including the location of the gesture samples to batch test. Note
   that user-dependent testing is assumed; that is, $N currently only
   loads and tests samples from the same user. See below for a
   discussion of each $N parameter in config.xml.

   **Note: the actual value of the argument "cmd" does not matter; the
     program is only checking for the existence of an argument. If no
     argument exists, it will launch a GUI window.

   The command line program executes the batch test, displaying some
   progress messages in the command window, and writing results of the
   testing to the .csv files in the samples directory as above. When
   it is finished, the program will display the message "Done testing
   batch." and exit.


~~~~~~~~~~~~~~~~~
~ $N PARAMETERS ~
~~~~~~~~~~~~~~~~~

This is a list and brief description of the $N parameters, both those
configurable at runtime and those set in the code.

*** Runtime Configurable Parameters ***

The configurable parameters are all located in the
($NDOLLAR_HOME)/conf/config.xml file. The program always looks in this
location for the value of these parameters; if they are not there the
program will crash.

/* Use this parameter to name the gesture set being tested. For bookkeeping purposes only; written to logfiles but not used by $N. */
GestureSet="gesture" 

/* Location of the gesture samples to use in the command line batch test mode. This parameter is ignored in GUI mode (although the other parameters are all used). Can be in absolute or relative format (relative to execution directory: ($NDOLLAR_HOME)/bin/Release). */
SamplesDirectory="..\..\samples" 

/* Method to use to search for the optimal match between a candidate and a template, to determine the match score. The template with the best match score to the candidate is returned as the recognition result. Valid values include: Protractor (Li, CHI 2010, a much faster method of matching) and GSS (GoldenSectionSearch, the original method used in $1 and $N. /
SearchMethod="Protractor"

/* If true, runs $N in true rotation-invariant mode, which will recognize a '6' and '9' as the same symbol. If false, limits rotation variance to +-45 degrees. */
RotationInvariant="false" 

/* If true, $N will process unistrokes just like multistrokes (that is, the template will store the unistroke made in both possible directions). If false, $N will ignore unistrokes and store only their actual direction. */
ProcessUnistrokes="false"

/* If true, will include gestures that qualify as 1D or 2D, respectively, based on the threshold test. These are both ignored in GUI batch test mode. */
Include1D="true"
Include2D="true"

/* If true, will test for 1D nature of the gesture and scale it differently. If false, will scale all gestures in the same way. */
TestFor1D="true"

/* If true, will scale all gestures uniformly. If false, will scale only 1D gestures uniformly (2D gestures will be scaled non-uniformly). */
UseUniformScaling="false"

/* Optional optimization for $N. If true, will not compare candidates to templates unless they have the same number of strokes. If false, will compare all pairs. */
MatchSameNumberOfStrokes="true" 

/* Optional optimization for $N. If true, will only compare candidates to templates whose start angles (the angle from the centroid to a certain point) are within some threshold). */
DoStartAngleComparison="true" 

/* How many points to compute the start angle with in the optimization above. */
StartAngleIndex="8" 

/* What threshold to use in the optimization above. */
StartAngleThreshold="30"

/* Number of points to resample the gesture to. */
NumResamplePoints="96"


*** Non-Configurable Parameters ***

There are several non-configurable parameters, most of which are
retained from $1. They can be found in NDollarRecognizer.cs, and any
changes to them must be recompiled.

/* Size of the bounding box to scale the templates / candidates to. */
DX = 250.0;

/* Rotation bound to use when rotation-invariant. */
_RotationBound = 45.0;

/* Threshold of ratio of longer side to shorter side to use in determining whether a gesture is 1-dimensional (aka, a line) or not; below the threshold passes as 1D. This was empirically determined based on the algebra dataset. */
_1DThreshold = 0.30;

/* Minimum number of examples to allow testing of that user-symbol combination. That is, a user must have at least _MinExamples of a given symbol for that combination to be included in the batch. */
_MinExamples = 9;

/* number of random tests to run at each iteration of the batch per user per symbol. */
NumRandomTests = 100;


~~~~~~~~~~~~~~~~~~~~~~~~~~~
~ MODIFYING THE RECOGNIZER ~
~~~~~~~~~~~~~~~~~~~~~~~~~~~

This is a brief list of the source files included in the C#
implementation of the $N recognizer and their main purpose and
methods. The main recognition code is in NDollarRecognizer.cs, which
includes test batch methods, recognition methods, and file operations
such as loading and saving gestures.

*** Entry Points ***

Launcher.cs -- Main() method is located here; chooses GUI or command line version to run.


*** Recognition ***

NDollarRecognizer.cs -- bulk of the recognition code; see Recognize() and TestBatch().
NDollarParameters.cs -- reads runtime parameters from ($NDOLLAR_HOME)/conf/config.xml.


*** GUI Components ***

AboutForm.cs -- displays copyright information for $N.
DebugForm.cs / DebugForm.Designer.cs -- displays graphical output during recognition for debugging.
InfoForm.cs / InfoForm.Designer.cs -- GUI batch test experiment set-up window.
MainForm.cs -- main GUI frame; handles drawing, loading and saving gestures.
ViewForm.cs -- displays all gestures currently loaded as templates.


*** Geometry ***

PointR.cs -- (x, y) point in 2D coordinate space.
RectangleR.cs -- (x, y, width, height) rectangle defined in 2D coordinate space.
SizeR.cs -- (cx, cy) scale factor (similar to Java's java.awt.Dimension class).
Utils.cs -- recognizer helper methods to resample, resize, scale, translate points.


*** Data Structures ***

Gesture.cs -- stores one gesture as a list of strokes (lists of points).
Multistroke.cs -- stores all permutations of a multistroke gesture.
SamplesCollection.cs -- stores a gesture corpus as a dictionary of users mapped to dictionaries of gesture names (i.e., the type) and categories; provides commonly needed methods.
Category.cs -- stores a gesture name (i.e., type) and all of its associated samples (usually used for just one user, but this is not required).
NBestList.cs -- stores n-best list for each recognition test.


*** Helpers ***

ProgressEvent.cs -- displays a progress bar during batch test mode in the GUI.
AssemblyInfo.cs -- C# helper file for versioning of the application.

There is also a Visual Studio Express 2010 project file, NDollar.sln, and its
supporting files, if that is of use to you.


**Note: for debugging purposes, Gesture.cs and NDollarRecognizer.cs
  both have instances of a "DebugForm". If you want to see the process
  of recognition in a graphical way, $N can print out templates and
  candidates during the pre-processing steps and recognition. In
  NDollarRecognizer.cs, you must enable the "_debug" variable by
  setting it to true. It will pop up a window with each template
  loaded or candidate recognized. Warning: DO NOT use this in test
  batch mode!! To prevent accidents like that, this is not a
  config.xml parameter.


~~~~~~~~~~~~~~~~~~~
~ GESTURE SAMPLES ~
~~~~~~~~~~~~~~~~~~~

$N expects a certain format for the gesture samples to process them
correctly. Two gesture datasets that $N can use as is are here:

1. $1 unistroke gesture set from Wobbrock et al, 2007

   Wobbrock, J.O., Wilson, A.D. and Li, Y. (2007) Gestures without
   libraries, toolkits or training: A $1 recognizer for user interface
   prototypes. Proc. UIST '07. New York: ACM Press, 159-168.

   Download:
   http://depts.washington.edu/aimgroup/proj/dollar/xml.zip

2. Multi-stroke algebra gesture set from Anthony at al, 2007

   Anthony, L., Yang, J. and Koedinger, K.R. (2008) Toward
   next-generation, intelligent tutors: Adding natural handwriting
   input. IEEE Multimedia 15 (3), 64-68.

A few sample gestures from each dataset are provided in $N release,
located in the ($NDOLLAR_HOME)/samples/ directory.




(c) 2007-2011 Lisa Anthony and Jacob O. Wobbrock, last revised: 03/10/2011

--end
