using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HootLib
{
    /// <summary>
    /// A specialised coroutine that advances a coroutine multiple times per frame up to a specified time budget.
    /// </summary>
    public class RushedCoroutine
    {
        private Stack<IEnumerator> _coroutines = new Stack<IEnumerator>();
        private float _budget;

        public RushedCoroutine(IEnumerator coroutine, float frameBudget = 1f / 60f)
        {
            _coroutines.Push(coroutine);
            _budget = frameBudget;
        }

        /// <summary>
        /// Advance the coroutine while respecting the frame budget.
        /// </summary>
        public IEnumerator Advance()
        {
            while (_coroutines.Count > 0)
            {
                var startTime = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - startTime < _budget)
                {
                    if (_coroutines.Count == 0)
                        yield break;

                    // Try to advance the most recent coroutine.
                    var coroutine = _coroutines.Peek();
                    try
                    {
                        if (!coroutine.MoveNext())
                        {
                            // This coroutine has been exhausted. Remove it.
                            _coroutines.Pop();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If any error occurs, log it and break this coroutine entirely.
                        Debug.LogException(ex);
                        yield break;
                    }

                    // Make sure that any nested coroutines are resolved first.
                    if (coroutine.Current is IEnumerator nested)
                    {
                        _coroutines.Push(nested);
                        continue;
                    }

                    // Skip returning if the coroutine wanted to wait for a frame and instead do it next loop.
                    if (coroutine.Current != null)
                        yield return coroutine.Current;
                }
                
                // Wait for a new frame.
                yield return null;
            }
        }
    }
}