using System;
using System.Windows;
using System.Runtime.Serialization;
using starPadSDK.Inq;
using starPadSDK.AppLib;

namespace PanoramicData.view.other
{
    /// <summary>
    /// APage is an example of what you might do to customize an InqScene to do application specific things
    /// It defines a Gesture CommandSet, and marks a special region of the inking surface as title region for use in a Tabbed display
    /// </summary>
    [Serializable]
    public class SimpleAPage : InqScene, ISerializable
    {
        /// <summary>
        /// This creates an InqScene and installs the initial gesture set
        /// </summary>
        public SimpleAPage()
        {
            StroqAddedEvent += new StroqHandler(stroqAddedEvent);
            StroqRemovedEvent += new StroqHandler(stroqRemovedEvent);
            StroqsAddedEvent += new StroqsHandler(stroqsAddedEvent);
            StroqsRemovedEvent += new StroqsHandler(stroqsRemovedEvent);
            ElementRemovedEvent += new ElementHandler(elementRemovedEvent);
            ElementsClearedEvent += new ElementsHandler(elementsClearedEvent);
            ElementAddedEvent += new ElementHandler(elementAddedEvent);
            ElementsAddedEvent += new ElementsHandler(elementsAddedEvent);
            
            SetImmediateDrag(true); // bcz: if false, then scale/rotate widgets are displayed

        }


        override protected CommandSet initCommands() { return new SimpleACommandSet(this); }

        void stroqAddedEvent(Stroq s)
        {
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
        }

        void stroqRemovedEvent(Stroq s)
        {
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
        }

        /// <summary>
        /// Callback to clean up everything if needed. 
        /// </summary>
        /// <param name="e"></param>
        void elementRemovedEvent(FrameworkElement e)
        {
        }

        void elementAddedEvent(FrameworkElement e)
        {
        }

        void elementsAddedEvent(FrameworkElement[] elems)
        {
        }

        /// <summary>
        /// Callback to clean up everything if needed. 
        /// </summary>
        /// <param name="e"></param>
        void elementsClearedEvent(FrameworkElement[] elems)
        {
        }
        /// <summary>
        /// initialization of the InqCanvas with a Gesture set and stuff for providing a page name
        /// </summary>
        override protected void init()
        {
            base.init();
        }
    }
}
