using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// Class MultiPointDeviceDriver.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 25. November 2008
    /// </summary>

    public abstract class MultiPointDeviceDriver : InputDeviceDriver
    {
        public delegate void MultiPointEventHandler(Object sender, MultiPointEventArgs multiPointEventArgs);

        public event MultiPointEventHandler MultiPointEvent;

        private Dictionary<long, MultiPointEventArgs> mPointsState;

        public MultiPointDeviceDriver()
        {
            mPointsState = new Dictionary<long, MultiPointEventArgs>();
        }

        protected void OnMultiPointEvent(MultiPointEventArgs multiPointEventArgs)
        {
            savePointEvent(multiPointEventArgs);

            if (MultiPointEvent != null)
            {
                MultiPointEvent(this, multiPointEventArgs);
            }
        }

        private void savePointEvent(MultiPointEventArgs multiPointEventArgs)
        {
            // if we have an up delete the key from the dictionary
            if (multiPointEventArgs.MultiPointEventType == MultiPointEventType.Up)
            {
                mPointsState.Remove(multiPointEventArgs.PointID);
            }
            else
            {
                mPointsState[multiPointEventArgs.PointID] = multiPointEventArgs;
            }
        }

        public Dictionary<long, MultiPointEventArgs>.Enumerator getPointsState()
        {
            return mPointsState.GetEnumerator();
        }

        public MultiPointEventArgs getPoint(int pointID) 
        {
            if(mPointsState.ContainsKey(pointID)) {
                return mPointsState[pointID];
            }

            return null;
        }
    }
}
