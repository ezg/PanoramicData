using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace starPadSDK.Utils {
    public interface IBatchLockable {
        int BatchLevel { get; set; }
        ulong ChangeSeq { get; }
        string Name { get; }
    }
    /// <summary>
    /// This class is used to help implement batch editing locks on various objects.
    /// </summary>
    public class BatchLock : IDisposable {
        private int _oldBatch;
        private ulong _startSeq;
        private IBatchLockable _o;
        private Action _fireatend;
        public BatchLock(IBatchLockable o, Action fireatend) {
            _o = o;
            _oldBatch = o.BatchLevel++;
            _startSeq = o.ChangeSeq;
            _fireatend = fireatend;
        }
        private bool _disposed = false;
        public void Dispose() {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue and prevent finalization (destructor) code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        // If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing) {
            if(!_disposed) {
                if(disposing) {
                    // Dispose managed resources, in this case effectively a lock
                    // also check to be sure we're not undoing locks in the wrong order
                    if(--_o.BatchLevel != _oldBatch) throw new Exception("Programmer error: attempt made to unwind batch editing locks on " + _o.Name + " in wrong order");
                    if(_o.BatchLevel == 0 && _startSeq != _o.ChangeSeq) _fireatend();
                    _o = null;
                }
                // Dispose unmanaged resources--but there are none for this class, so no code here.

                _disposed = true;
            }
        }
        // This destructor will run only if the Dispose method does not get called.
        // Do not provide destructors in types derived from this class.
        ~BatchLock() {
            Dispose(false);
        }
    }
}
