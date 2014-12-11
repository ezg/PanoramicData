using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using starPadSDK.GestureBarLib;

namespace starPadSDK.GestureBarLib
{
	public interface IGestureButton
	{
        UIElement Context
        {
            get;
            set;
        }

        string Title
        {
            get;
            set;
        }

        ArrayList GetStroke1();
        ArrayList GetStroke2();
        ArrayList GetStroke3();
        ArrayList GetStroke4();

        string Stroke1FeatureDescription
        {
            get;
            set;
        }

        string Stroke2FeatureDescription
        {
            get;
            set;
        }

        string Stroke3FeatureDescription
        {
            get;
            set;
        }

        string Stroke4FeatureDescription
        {
            get;
            set;
        }

        GestureButton.ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction1
        {
            get;
            set;
        }

        GestureButton.ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction2
        {
            get;
            set;
        }

        GestureButton.ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction3
        {
            get;
            set;
        }

        GestureButton.ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction4
        {
            get;
            set;
        }

        int AnimationSpeed
        {
            get;
            set;
        }

        int AnimationSpeed2
        {
            get;
            set;
        }

        int AnimationSpeed3
        {
            get;
            set;
        }

        int AnimationSpeed4
        {
            get;
            set;
        }

        UIElement DisplayIcon
        {
            get;
            set;
        }

        Panel Stroke1DetailsLayer
        {
            get;
        }

        Panel Stroke2DetailsLayer
        {
            get;
        }

        Panel Stroke3DetailsLayer
        {
            get;
        }

        Panel Stroke4DetailsLayer
        {
            get;
        }

        Panel Stroke1Context
        {
            get;
        }

        Panel Stroke2Context
        {
            get;
        }

        Panel Stroke3Context
        {
            get;
        }

        Panel Stroke4Context
        {
            get;
        }

        FrameworkElement ToFrameworkElement();
	}
}
