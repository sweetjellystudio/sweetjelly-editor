using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SWeetJelly.Editor.Tests
{
    public sealed class EditorCoroutineTests
    {
        private static void Tick() => typeof(EditorCoroutines).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        private sealed class Probe : IEnumerator, IDisposable
        {
            internal int steps, disposed;
            internal Func<int, bool> step = _ => false;
            internal object current;
            internal Action dispose;
            public bool MoveNext() { return step(++steps); }
            public object Current => current;
            public void Reset() { throw new NotSupportedException(); }
            public void Dispose() { disposed++; dispose?.Invoke(); }
        }
        [TearDown] public void TearDown() { EditorCoroutines.StopAll(); }
        [Test] public void NullEnumeratorIsRejected() { Assert.Throws<ArgumentNullException>(() => EditorCoroutines.Execute(null)); }
        [Test] public void CompletionDisposesBeforeSuccessNotification()
        {
            var probe = new Probe();
            bool completed = false;
            EditorCoroutines.Execute(probe, done => { Assert.That(probe.disposed, Is.EqualTo(1)); completed = done; });
            Tick(); Tick();
            Assert.That(completed, Is.True);
            Assert.That(probe.steps, Is.EqualTo(1));
        }
        [Test] public void StopAllDisposesNestedIteratorsWithoutSuccess()
        {
            var child = new Probe { step = _ => true };
            var parent = new Probe { step = _ => true, current = child };
            bool completed = false;
            EditorCoroutines.Execute(parent, done => completed |= done);
            Tick(); Tick(); EditorCoroutines.StopAll(); Tick();
            Assert.That(parent.disposed, Is.EqualTo(1));
            Assert.That(child.disposed, Is.EqualTo(1));
            Assert.That(completed, Is.False);
        }
        [Test] public void FailureDisposesAndIndependentWorkContinuesNextTick()
        {
            var bad = new Probe { step = _ => throw new InvalidOperationException("iterator failed") };
            var good = new Probe();
            EditorCoroutines.Execute(bad);
            EditorCoroutines.Execute(good);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: iterator failed");
            Tick();
            Assert.That(bad.disposed, Is.EqualTo(1));
            Assert.That(good.steps, Is.Zero);
            Tick();
            Assert.That(good.disposed, Is.EqualTo(1));
        }
        [Test] public void AddedWorkStartsNextTick()
        {
            var added = new Probe();
            var first = new Probe { step = _ => { EditorCoroutines.Execute(added); return false; } };
            EditorCoroutines.Execute(first);
            Tick();
            Assert.That(added.steps, Is.Zero);
            Tick();
            Assert.That(added.steps, Is.EqualTo(1));
        }
        [Test] public void StopAllFromMoveNextDoesNotAdvanceDisposedIterator()
        {
            var probe = new Probe { step = _ => { EditorCoroutines.StopAll(); return true; } };
            EditorCoroutines.Execute(probe);
            Assert.DoesNotThrow(Tick);
            Assert.That(probe.disposed, Is.EqualTo(1));
        }
        [Test] public void OriginalFailureSurvivesDisposeFailure()
        {
            var probe = new Probe { step = _ => throw new InvalidOperationException("original"), dispose = () => throw new Exception("cleanup") };
            EditorCoroutines.Execute(probe);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: original");
            Tick();
            Assert.That(probe.disposed, Is.EqualTo(1));
        }
        [Test] public void CallbackFailureDoesNotRepeatCompletion()
        {
            int calls = 0;
            EditorCoroutines.Execute(new Probe(), done => { calls++; throw new InvalidOperationException("callback failed"); });
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: callback failed");
            Tick(); Tick();
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test] public void StopAllInsideYieldIteratorRunsItsFinally()
        {
            int disposed = 0;
            IEnumerator Run()
            {
                try { EditorCoroutines.StopAll(); yield return null; }
                finally { disposed++; }
            }
            EditorCoroutines.Execute(Run());
            Tick();
            Assert.That(disposed, Is.EqualTo(1));
        }
    }
}
