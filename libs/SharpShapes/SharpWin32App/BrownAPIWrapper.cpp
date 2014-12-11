/*

   This file is compiled as managed C++ so that it can directly call C# functions

*/
#include "stdafx.h"
using namespace System;
using namespace System::Windows;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;
#include <stdio.h>

 struct brownpt {
	double X;
	double Y;
};
 
void callBrown(brownpt *cpts,     // array of points for one single stroke
	           int      numPts,   // number of points in the array
			   char    *output,   // an output buffer for the name of the recognized shape
			   int      len       // the length of the output buffer
			   ) 
{
	// create a managed array of Points from input pts
	array<Point>^ pts = gcnew array<Point>(numPts);
	for (int i = 0; i < numPts; i++)
		pts[i] = Point(cpts[i].X, cpts[i].Y);
	
	// Shape Recognition
	BrownRecognitionCommon::BrownInputStroke^ stroke = gcnew BrownRecognitionCommon::BrownInputStroke(pts, gcnew Double());
	BrownRecognitionCommon::BrownShape^ brownShape = BrownRecognitionAPI::API::RecognizeBrownShape(stroke);
	
	// then you can iterate over the managed points and copy them to an unmanaged array.
	// here, we're just ovewriting the first element of the unmanaged array to demonstrate how managed->unmanaged copying works
	for (int i = 0; i < brownShape->ShapePoints.Length; i++) {
		cpts[0].X = brownShape->ShapePoints[i].X;
		cpts[0].Y = brownShape->ShapePoints[i].Y;
	}

	// Template Recognition
	List<BrownRecognitionCommon::BrownInputStroke^>^ inputStrokes = gcnew List<BrownRecognitionCommon::BrownInputStroke^>();
	inputStrokes->Add(stroke);
	List<BrownRecognitionCommon::BrownTemplate^>^ templates = BrownRecognitionAPI::API::RecognizeBrownTemplate(inputStrokes);
	

	// Settings
	System::String^ settings = BrownRecognitionAPI::API::GetBrownRecognitionSettings();
	BrownRecognitionAPI::API::SetBrownRecognitionSettings(settings);

	// copy the enumeration name out to an unmanaged C++ character array
	char* umstring = (char *)Marshal::StringToHGlobalAnsi(settings).ToPointer();
	for ( char *umstr=umstring;*umstr;)
		*output++ = *umstr++;
	*output='\0';
	Marshal::FreeHGlobal(IntPtr((void*)umstring));

	/*


	// call Brown recognizer with unistroke input
	ShapeRecognizer::BrownShape^ x = ShapeRecognizer::Recognizer::RecognizeBrownShape(pts);

	// if input stroke is a line or polyline, see if it combines with other lines to make a shape
	if (x->SType == ShapeRecognizer::ShapeType::Polyline || x->SType == ShapeRecognizer::ShapeType::StraightLine) {
		List<ShapeRecognizer::BrownLine^>^ lines = gcnew List<ShapeRecognizer::BrownLine^>
		();

		// collect a list of previously drawn lines (could be the last N-lines, or the all lines drawn in the last N seconds, or all lines since the last shape, or just all lines on the display)
		// e.g., 

		// foreach line on the display
		// 	  lines->Add(gcnew ShapeRecognizer::BrownLine(line->Start, line->End, line));  // pass the application line data structure to the BrownLine constructor as the last parameter

		// x = ShapeRecognizer::Recognizer::RecognizeBrownShape(lines);

		           // lines now contains all of the lines that were uesed to create the shape 'x'
		// foreach line in lines
		//     remove line->Data  from scene  // the Data field of the line object is the application line object passed to the BrownLine constructor
	}

	// BrownShape has two fields:  Points which is an array of Points that represent the cleaned up geometric figure
	//                       and:  SType which is a ShapeType enumeration: None, StraightLine, Triangle, RightTriangle, IsoscelesTriangle, RoundedRect, Rect, Square, Parallelogram, Circle, Trapezoid, Ellipse, Diamond 


	// copy the enumeration name out to an unmanaged C++ character array
	char* umstring = (char *)Marshal::StringToHGlobalAnsi(x->SType.ToString()).ToPointer();
	for ( char *umstr=umstring;*umstr;)
		*output++ = *umstr++;
	*output='\0';
	Marshal::FreeHGlobal(IntPtr((void*)umstring));

	// then you can iterate over the managed points and copy them to an unmanaged array.
	// here, we're just ovewriting the first element of the unmanaged array to demonstrate how managed->unmanaged copying works
	for (int i = 0; i < x->Points.Length; i++) {
		cpts[0].X = x->Points[i].X;
		cpts[0].Y = x->Points[i].Y;
	}*/
}