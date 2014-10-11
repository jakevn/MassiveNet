// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System.Collections;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// The base request class for representing coroutine/async functionality that yields a result.
    /// </summary>
    public abstract class Request<T> {
        public abstract T Result { get; set; }
        public abstract bool IsSuccessful { get; set; }
        public abstract Coroutine WaitUntilDone { get; set; }
        public abstract IEnumerator RequestCoroutine();
    }
}