// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

// Some routines inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
// http://graphics.stanford.edu/~seander/bithacks.html

namespace System.Numerics
{
    /// <summary>
    /// Utility methods for intrinsic bit-twiddling operations.
    /// The methods use hardware intrinsics when available on the underlying platform,
    /// otherwise they use optimized software fallbacks.
    /// </summary>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        static class BitOperations
    {


        [CLSCompliant(false)]
        public static uint RotateLeft(uint value, int offset)
     => (value << offset) | (value >> (32 - offset));
    }
}