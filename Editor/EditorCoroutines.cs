using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    [InitializeOnLoad]
    public static class EditorCoroutines
    {
        private static readonly List<Coroutine> coroutines = new List<Coroutine>();
        private static bool shuttingDown;

        static EditorCoroutines()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;
        }

        private static void Shutdown() { shuttingDown = true; StopAll(); }

        public static void Execute(IEnumerator enumerator, Action<bool> OnUpdate = null)
        {
            if (enumerator == null) throw new ArgumentNullException(nameof(enumerator));
            if (shuttingDown) throw new InvalidOperationException("The Editor coroutine scheduler is shutting down.");
            if (coroutines.Count == 0) EditorApplication.update += Update;
            var coroutine = new Coroutine { OnUpdate = OnUpdate };
            coroutine.stack.Push(enumerator);
            coroutines.Add(coroutine);
        }

        private static void Update()
        {
            foreach (var coroutine in coroutines.ToArray())
            {
                if (!coroutines.Contains(coroutine)) continue;
                try
                {
                    var current = coroutine.stack.Peek();
                    bool moved = current.MoveNext();
                    if (!coroutines.Contains(coroutine)) continue;
                    if (!moved)
                    {
                        coroutine.stack.Pop();
                        (current as IDisposable)?.Dispose();
                        if (!coroutines.Contains(coroutine)) continue;
                    }
                    else if (current.Current is IEnumerator nested)
                    {
                        if (coroutine.stack.Contains(nested))
                            throw new InvalidOperationException("An Editor coroutine cannot yield itself or an ancestor.");
                        coroutine.stack.Push(nested);
                    }
                    bool done = coroutine.stack.Count == 0;
                    if (done) Remove(coroutine);
                    coroutine.OnUpdate?.Invoke(done);
                }
                catch (Exception exception)
                {
                    Remove(coroutine);
                    Dispose(coroutine, exception);
                    break;
                }
            }
        }

        private static void Remove(Coroutine coroutine)
        {
            coroutines.Remove(coroutine);
            if (coroutines.Count == 0) EditorApplication.update -= Update;
        }

        private static void Dispose(Coroutine coroutine, Exception failure = null)
        {
            while (coroutine.stack.Count > 0)
            {
                try { (coroutine.stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception exception) { if (failure == null) failure = exception; }
            }
            if (failure != null) Debug.LogException(failure);
        }

        public static void StopAll()
        {
            var stopped = coroutines.ToArray();
            coroutines.Clear();
            EditorApplication.update -= Update;
            foreach (var coroutine in stopped) Dispose(coroutine);
        }

        private sealed class Coroutine
        {
            internal readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
            internal Action<bool> OnUpdate;
        }
    }
}
