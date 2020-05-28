// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;



internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static unsafe bool GetRandomBytes(byte* pbBuffer, int count)
        {
            Debug.Assert(count >= 0);

            return CryptoNative_GetRandomBytes(pbBuffer, count);
        }

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool CryptoNative_GetRandomBytes(byte* buf, int num);
    }
}


internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetNonCryptographicallySecureRandomBytes")]
        internal static extern unsafe void GetNonCryptographicallySecureRandomBytes(byte* buffer, int length);
    }

    internal static unsafe void GetRandomBytes(byte* buffer, int length)
    {
        Sys.GetNonCryptographicallySecureRandomBytes(buffer, length);
    }
}

internal static partial class Interop
{
    internal static partial class Libraries
    {
        // Shims
        internal const string SystemNative = "libSystem.Native";
        internal const string NetSecurityNative = "libSystem.Net.Security.Native";
        internal const string CryptoNative = "libSystem.Security.Cryptography.Native.OpenSsl";
        internal const string CompressionNative = "libSystem.IO.Compression.Native";
        internal const string IOPortsNative = "libSystem.IO.Ports.Native";
        internal const string Libdl = "libdl";
        internal const string HostPolicy = "libhostpolicy";
    }
}
