namespace LCPD_First_Response.Engine.Timers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Helper class to call certain functions once after a given amount of time.
    /// </summary>
    public static class DelayedCaller
    {
        /// <summary>
        /// The internal queue holding all delegates, the amount of time to wait before invoking them and the parameter
        /// </summary>
        private static List<DelayedCalledInfo> queue;

        /// <summary>
        /// Same as queue, but for testing of the type functionality.
        /// </summary>
        private static Dictionary<object, List<DelayedCalledInfo>> assignedCalls;

        /// <summary>
        /// Items pending to be added to assignedCalls.
        /// </summary>
        private static List<KeyValuePair<object, List<DelayedCalledInfo>>> addQueue; 

        /// <summary>
        /// Initializes static members of the <see cref="DelayedCaller"/> class.
        /// </summary>
        static DelayedCaller()
        {
            assignedCalls = new Dictionary<object, List<DelayedCalledInfo>>();
            addQueue = new List<KeyValuePair<object, List<DelayedCalledInfo>>>();
            queue = new List<DelayedCalledInfo>();
        }

        /// <summary>
        /// Function that is called by the DelayedCaller. Function can take any amount of arguments.
        /// </summary>
        /// <param name="parameter">Parameter, if any.</param>
        public delegate void DelayedFunctionEventHandler(params object[] parameter);

        /// <summary>
        /// Function that is called by the DelayedCaller. Function can take any amount of arguments and return any value.
        /// </summary>
        /// <param name="parameter">Parameter, if any.</param>
        /// <typeparam name="T">The return type.</typeparam>
        /// <returns>The return value.</returns>
        public delegate T DelayedFunctionReturnValueEventHandler<T>(params object[] parameter);

        /// <summary>
        /// Adds <paramref name="function"/> to the internal queue and will call it after the amount of time in Milliseconds specified in <paramref name="time"/>.
        /// Returns immediately.
        /// </summary>
        /// <param name="function">The function to invoke.</param>
        /// <param name="time">The time to wait before invoking the function.</param>
        /// <param name="parameter">The parameter.</param>
        [Obsolete("Does not support proper garbage collecting and pending calls can't be stopped. Will be removed soon.")]
        internal static void Call(DelayedFunctionEventHandler function, int time, params object[] parameter)
        {
            // Store callback and time to call in queue
            queue.Add(new DelayedCalledInfo(function, time, false, parameter));
        }

        public static void Call(DelayedFunctionEventHandler function, object instance, int time, params object[] parameter)
        {
            Call(function, instance, time, false, parameter);
        }

        public static void Call(DelayedFunctionEventHandler function, object instance, int time, bool neverClear, params object[] parameter)
        {
            // Construct item
            DelayedCalledInfo delayedCalledInfo = new DelayedCalledInfo(function, time, neverClear, parameter);

            if (instance != null)
            {
                // If instance hasn't been registered yet, add
                if (!assignedCalls.ContainsKey(instance))
                {
                    // Add list of calls for instance
                    List<DelayedCalledInfo> list = new List<DelayedCalledInfo>();
                    list.Add(delayedCalledInfo);

                    // This call is about to change the enumeration, so we queue it up
                    addQueue.Add(new KeyValuePair<object, List<DelayedCalledInfo>>(instance, list));
                }
                else
                {
                    // Add to list of calls for instance
                    assignedCalls[instance].Add(delayedCalledInfo);
                }
            }
        }

        public static void ClearAllRunningCalls(bool forceAll, object instance)
        {
            if (assignedCalls.ContainsKey(instance))
            {
                List<DelayedCalledInfo> itemsToRemove = new List<DelayedCalledInfo>();

                // Add all items that can be cleared
                foreach (DelayedCalledInfo delayedCalledInfo in assignedCalls[instance])
                {
                    if (!delayedCalledInfo.NeverClear || forceAll)
                    {
                        itemsToRemove.Add(delayedCalledInfo);
                    }
                }

                // Remove them
                foreach (DelayedCalledInfo delayedCalledInfo in itemsToRemove)
                {
                    assignedCalls[instance].Remove(delayedCalledInfo);
                }
            }
        }

        /// <summary>
        /// Invokes the function on the main thread (the one executing natives), thus doing ALMOST the same as <see cref="Call"/>. In contrast to it, this waits until the call has finished, hence
        /// blocking the thread and can return a value.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="function">The function to invoke.</param>
        /// <param name="time">The time to wait before invoking the function.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The return value.</returns>
        public static T InvokeOnMainThread<T>(DelayedFunctionReturnValueEventHandler<T> function, int time, params object[] parameter)
        {
            T result = default(T);
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Action invokeLogic = delegate
                {
                    result = function.Invoke();
                    manualResetEvent.Set();
                };

            // Call the invoke logic the next tick in our main thread
            Call(delegate { invokeLogic();  }, time);

            // Wait for the invoke logic to finish
            manualResetEvent.WaitOne();
            return result;
        }

        /// <summary>
        /// Processes pending calls.
        /// </summary>
        internal static void Process()
        {
            List<DelayedCalledInfo> delegatesToRemove = new List<DelayedCalledInfo>();

            // Old legacy code - will be removed soon:
            for (int i = 0; i < queue.Count; i++)
            {
                DelayedCalledInfo delayedCalledInfo = queue[i];
                if (DateTime.Now.CompareTo(delayedCalledInfo.Time) > 0)
                {
                    // Invoke
                    delayedCalledInfo.Function.Invoke(delayedCalledInfo.Parameter);

                    delegatesToRemove.Add(delayedCalledInfo);
                }
            }

            foreach (DelayedCalledInfo delayedCalledInfo in delegatesToRemove)
            {
                queue.Remove(delayedCalledInfo);
            }

            // New code
            // Get current time
            DateTime time = DateTime.Now;
            List<object> entriesToRemove = new List<object>();

            // Get new objects
            foreach (KeyValuePair<object, List<DelayedCalledInfo>> keyValuePair in addQueue)
            {
                if (assignedCalls.ContainsKey(keyValuePair.Key))
                {
                    foreach (DelayedCalledInfo delayedCalledInfo in keyValuePair.Value)
                    {
                        assignedCalls[keyValuePair.Key].Add(delayedCalledInfo);
                    }
                }
                else
                {
                    assignedCalls.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            addQueue.Clear();

            // Get all assigned calls
            var enumerator = assignedCalls.GetEnumerator();
            for (int index = 0; index < assignedCalls.Count; index++)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                KeyValuePair<object, List<DelayedCalledInfo>> assignedCall = enumerator.Current;

                // Clear calls to remove in current object
                delegatesToRemove.Clear();

                // Get list of calls for current object
                for (int i = 0; i < assignedCall.Value.Count; i++)
                {
                    DelayedCalledInfo delayedCalledInfo = assignedCall.Value[i];
                    if (time.CompareTo(delayedCalledInfo.Time) > 0)
                    {
                        // Invoke
                        delayedCalledInfo.Function.Invoke(delayedCalledInfo.Parameter);

                        // Add to remove
                        delegatesToRemove.Add(delayedCalledInfo);
                    }
                }

                // Remove calls for current object
                foreach (DelayedCalledInfo delayedCalledInfo in delegatesToRemove)
                {
                    assignedCall.Value.Remove(delayedCalledInfo);
                }

                // If count has reached zero, so list is empty, delete
                if (assignedCall.Value.Count == 0)
                {
                    entriesToRemove.Add(assignedCall.Key);
                }
            }

            // Remove empty entris
            foreach (object o in entriesToRemove)
            {
                assignedCalls.Remove(o);
            }
        }

        /// <summary>
        /// Class to store information about functions that should be invoked later.
        /// </summary>
        internal class DelayedCalledInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DelayedCalledInfo"/> class.
            /// </summary>
            /// <param name="function">
            /// The function.
            /// </param>
            /// <param name="time">
            /// The time.
            /// </param>
            /// <param name="neverClear">
            /// Whether the function should remain even if the owning type asked to clear all assigned calls.
            /// </param>
            /// <param name="parameter">
            /// The parameter.
            /// </param>
            public DelayedCalledInfo(DelayedFunctionEventHandler function, int time, bool neverClear, params object[] parameter)
            {
                this.Function = function;
                this.Parameter = parameter;
                this.NeverClear = neverClear;
                this.Time = DateTime.Now.AddMilliseconds(time);
            }

            /// <summary>
            /// Gets the function to invoke.
            /// </summary>
            public DelayedFunctionEventHandler Function { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the callback should never be cleared, even though if the registering script has been terminated.
            /// </summary>
            public bool NeverClear { get; private set; }

            /// <summary>
            /// Gets the parameter for the function.
            /// </summary>
            public object[] Parameter { get; private set; }

            /// <summary>
            /// Gets the time when the function is supposed to be invoked.
            /// </summary>
            public DateTime Time { get; private set; }
        }
    }
}
