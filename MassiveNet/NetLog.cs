// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
namespace MassiveNet {

    internal class NetLog {
        
        internal static bool EnableInfo = true;

        internal static void Error(string error) {
            UnityEngine.Debug.LogError("MassiveNet: " + error);
        }

        internal static void Warning(string warning) {
            UnityEngine.Debug.LogWarning("MassiveNet: " + warning);
        }

        internal static void Info(string info) {
            if (EnableInfo) UnityEngine.Debug.Log("MassiveNet.Info: " + info);
        }

#if TRACE
        internal static void Trace(string trace) {
            UnityEngine.Debug.Log("MassiveNet.Trace: " + trace);
        }
#endif

#if DEBUG
        internal static void Debug(string debug) {
            UnityEngine.Debug.Log("MassiveNet.Debug: " + debug);
        }
#endif

    }
}