using System;
using System.Threading;

namespace AIT.DMF.Common
{
    /// <summary>
    /// Based on an implementation by Jeffrey Richter taken from:
    /// http://msdn.microsoft.com/en-us/magazine/cc163467.aspx
    /// </summary>
    public class AsyncResultNoResult : IAsyncResult
    {
        // Fields set at construction which never change while
        // operation is pending
        private readonly AsyncCallback _asyncCallback;
        private readonly object _asyncState;

        // Fields set at construction which do change after
        // operation completes
        private const int _statePending = 0;
        private const int _stateCompletedSynchronously = 1;
        private const int _stateCompletedAsynchronously = 2;
        private int _completedState = _statePending;

        // Field that may or may not get set depending on usage
        private ManualResetEvent _asyncWaitHandle;

        // Fields set when operation completes
        private Exception _exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultNoResult"/> class.
        /// </summary>
        /// <param name="asyncCallback">The async callback.</param>
        /// <param name="state">The state.</param>
        public AsyncResultNoResult(AsyncCallback asyncCallback, object state)
        {
            _asyncCallback = asyncCallback;
            _asyncState = state;
        }

        public void SetAsCompleted(Exception exception, bool completedSynchronously)
        {
            // Passing null for exception means no error occurred.
            // This is the common case
            _exception = exception;

            // The m_CompletedState field MUST be set prior calling the callback
            int prevState = Interlocked.Exchange(ref _completedState,
               completedSynchronously ? _stateCompletedSynchronously :
               _stateCompletedAsynchronously);
            if (prevState != _statePending)
                throw new InvalidOperationException("You can set a result only once");

            // If the event exists, set it
            if (_asyncWaitHandle != null) _asyncWaitHandle.Set();

            // If a callback method was set, call it
            if (_asyncCallback != null) _asyncCallback(this);
        }

        public void EndInvoke()
        {
            // This method assumes that only 1 thread calls EndInvoke
            // for this object
            if (!IsCompleted)
            {
                // If the operation isn't done, wait for it
                AsyncWaitHandle.WaitOne();
                AsyncWaitHandle.Close();
                _asyncWaitHandle = null;  // Allow early GC
            }

            // Operation is done: if an exception occured, throw it
            if (_exception != null) throw _exception;
        }

        #region Implementation of IAsyncResult
        public object AsyncState { get { return _asyncState; } }

        public bool CompletedSynchronously
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) ==
                    _stateCompletedSynchronously;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_asyncWaitHandle == null)
                {
                    bool done = IsCompleted;
                    ManualResetEvent mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref _asyncWaitHandle,
                       mre, null) != null)
                    {
                        // Another thread created this object's event; dispose
                        // the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            // If the operation wasn't done when we created
                            // the event but now it is done, set the event
                            _asyncWaitHandle.Set();
                        }
                    }
                }
                return _asyncWaitHandle;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) !=
                    _statePending;
            }
        }
        #endregion
    }

    public class AsyncResult<TResult> : AsyncResultNoResult
    {
        // Field set when operation completes
        private TResult _result;

        public AsyncResult(AsyncCallback asyncCallback, object state) :
            base(asyncCallback, state) { }

        public void SetAsCompleted(TResult result,
           bool completedSynchronously)
        {
            // Save the asynchronous operation's result
            _result = result;

            // Tell the base class that the operation completed
            // sucessfully (no exception)
            SetAsCompleted(null, completedSynchronously);
        }

        new public TResult EndInvoke()
        {
            base.EndInvoke(); // Wait until operation has completed
            return _result;  // Return the result (if above didn't throw)
        }
    }
}
