//
// ThreadAssist.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;

namespace Banshee.Base
{
    public static class ThreadAssist
    {
        private static Thread main_thread;
        
        public static void InitializeMainThread ()
        {
            main_thread = Thread.CurrentThread;
        }
        
        public static bool InMainThread {
            get {
                if (main_thread == null) {
                    throw new ApplicationException ("ThreadAssist.InitializeMainThread must be called first");
                }
 
                return main_thread.Equals (Thread.CurrentThread); 
            }
        }

        public static void AssertNotInMainThread ()
        {
            if (ApplicationContext.Debugging && Banshee.Base.ThreadAssist.InMainThread) {
                Hyena.Log.Warning ("In GUI thread, will probably block it", System.Environment.StackTrace);
            }
        }
        
        public static void AssertInMainThread ()
        {
            if (ApplicationContext.Debugging && !InMainThread) {
                Hyena.Log.Warning ("Not in main thread!", System.Environment.StackTrace);
            }
        }
        
        public static void ProxyToMain (InvokeHandler handler)
        {
            if (!InMainThread) {
                Banshee.ServiceStack.Application.Invoke (handler);
            } else {
                handler ();
            }
        }

        public static void SpawnFromMain (ThreadStart threadedMethod)
        {
            if (InMainThread) {
                Spawn (threadedMethod, true);
            } else {
                threadedMethod ();
            }
        }
        
        public static Thread Spawn (ThreadStart threadedMethod, bool autoStart)
        {
            Thread thread = new Thread (threadedMethod);
            thread.IsBackground = true;
            if (autoStart) {
                thread.Start ();
            }
            return thread;
        }
        
        public static Thread Spawn (ThreadStart threadedMethod)
        {
            return Spawn (threadedMethod, true);
        }
    }
}
