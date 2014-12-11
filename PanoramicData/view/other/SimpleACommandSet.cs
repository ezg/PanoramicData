using System;
using System.Collections.Generic;
using starPadSDK.Inq;
using starPadSDK.AppLib;

namespace PanoramicData.view.other
{ 
    // This creates the set of gestures to recognize on this InqScene.  The gestures are broken down into right-button and 
    // stylus-button gesture sets.
    // The MathCommand and CurveCommand gestures activate special-purpose editing objects which want most other
    // gestures be deactivated to avoid conflicts.  So when one of these commands is recognized, InitGestures will get
    // called and it will install an appropriate set of gestures depending on which editing modes have been (de)activated.
    public class SimpleACommandSet : CommandSet
    {

        /// <summary>
        ///  this example handler is called when a Stroq has been drawn that the Gesture recognizer has
        ///  definitively determined is not a gesture.  By returning false, this method indicates that it has not
        ///  handled the non-gesture Stroq, and the underlying Stroq processing machinery will add the Stroq
        ///  to the Scene. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        virtual protected bool notAGestureHandler(object sender, object d, Stroq s) { return false; }
        /// <summary>
        ///  this method is called automatically to initialize the active Gestures when the CommandSet is created.
        ///  It is also called whenever a Gesture Command wants to change the active gestures, such as when activating
        ///  the Math or Curve editors.
        /// </summary>
        /// 
        SimpleAPage _aPage = null;

        protected override void InitGestures()
        {
            base.InitGestures();  // cleans out all current Gestures
        }
        protected override void InitRightGestures()
        {
            base.InitRightGestures();

            // Scribble delete gesture
            List<Type> t = new List<Type>();
            //t.Add(typeof(InkTable));
            OneStrokeGesture g = new ScribbleGesture(_can, t);
            _gest.Add(g);
        }

        public SimpleACommandSet(SimpleAPage scene) : base(scene)
        {
            _aPage = scene;
            NonGestureEvent += new Gesturizer.StrokeUnrecognizedHandler(notAGestureHandler);
        }
    }
}
