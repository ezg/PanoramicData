// SharpWin32App.cpp :
//  This file is compiled as unmanaged C++.  Yet it can call functions in BrownAPIWrapper.cpp which is compiled as managed C++.
//

#include "stdafx.h"
#include <stdio.h>
#include <iostream>


struct brownpt {
	double X;
	double Y;
};
extern void callBrown( brownpt *, int, char *, int);

brownpt createPt(double x, double y) {
	brownpt pt;
	pt.X = x;
	pt.Y = y;
	return pt;
}

int _tmain(int argc, _TCHAR* argv[])
{
	brownpt pts[2];
	pts[0] = createPt(0,0);
	pts[1] = createPt(100,0);
	char recog[255];
	callBrown(( brownpt *)pts, 2, recog, 255);
	std::cout<<"Brown Recognizer returned " << std::endl;
	std::cout << recog  << std::endl;
	
	std::cout<<"Type something and hit return to exit" << std::endl;
	std::cin >> recog;
	return 0;
}

