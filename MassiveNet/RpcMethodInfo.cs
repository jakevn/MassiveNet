// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MassiveNet {
    /// <summary> Contains needed invoke data for a method marked with the NetRPC attribute. </summary>
    internal class RpcMethodInfo {
        // The method name:
        public string Name;

        // The ordered types of the parameters:
        public List<Type> ParamTypes = new List<Type>();

        // Classes that contain the RPC method definition:
        public Dictionary<Type, MethodInfo> MethodInfoLookup;

        // True if one or more parameters is a Request type:
        public bool TakesRequests;
    }

    internal class RpcInfoCache {
        private static readonly Dictionary<string, RpcMethodInfo> Cache = new Dictionary<string, RpcMethodInfo>();

        /// <summary> True if CacheRpcs has been run. </summary>
        private static bool cached;

        internal static int Count {
            get { return Cache.Count; }
        }

        /// <summary>
        /// Returns the Dictionary containing the Rpc cache, running CacheRpcs beforehand if necessary.
        /// </summary>
        internal static Dictionary<string, RpcMethodInfo> RpcMethods() {
            if (!cached) CacheRpcs();
            return Cache;
        }

        /// <summary>
        ///  Identifies all methods in Monobehaviour-inhereting classes with the NetRPC attribute
        ///  and adds the relevant method information to the RPC cache. Ideally, this is run on
        ///  startup instead of during operation since it is costly.
        ///  </summary>
        internal static void CacheRpcs() {
            IEnumerable<Type> classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof (MonoBehaviour)));

            foreach (Type classType in classes) CacheType(classType);

            cached = true;
        }

        /// <summary>
        /// Returns true if the Rpc cache contains a method that matches the supplied method name.
        /// </summary>
        internal static bool Exists(string methodName) {
            return Cache.ContainsKey(methodName);
        }

        internal static RpcMethodInfo Get(string methodName) {
            return Cache[methodName];
        }

        /// <summary> Returns a List of parameter types for the provided RPC name. </summary>
        internal static List<Type> ParamTypes(string rpcName) {
            return Cache[rpcName].ParamTypes;
        }

        /// <summary> Returns true if the RPC method has a return value or has a NetRequest parameter. </summary>
        internal static bool TakesRequests(string methodName) {
            return Cache[methodName].TakesRequests;
        }

        /// <summary> Iterates through the methods of the supplied type and adds any RPCs to the local RPC cache. </summary>
        private static void CacheType(Type t) {

            foreach (MethodInfo method in
                    t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance)) {
                if (!HasAttribute(method)) continue;
                if (Cache.ContainsKey(method.Name)) AddToExisting(method, t);
                else AddToNew(method, t);
            }
        }

        private static void AddToNew(MethodInfo method, Type type) {

            ParameterInfo[] parameters = method.GetParameters();

            RpcMethodInfo rpcMethod = new RpcMethodInfo {
                                                            Name = method.Name,
                                                            MethodInfoLookup = new Dictionary<Type, MethodInfo>()
                                                        };

            rpcMethod.MethodInfoLookup.Add(type, method);

            foreach (ParameterInfo parameter in parameters) {
                if (parameter.ParameterType.IsGenericType &&
                    parameter.ParameterType.GetGenericTypeDefinition() == typeof (NetRequest<>))
                    rpcMethod.TakesRequests = true;
                rpcMethod.ParamTypes.Add(parameter.ParameterType);
            }

            if (method.ReturnType != typeof (void) && method.ReturnType != typeof (IEnumerator)) {
                rpcMethod.TakesRequests = true;
            }

            Cache.Add(rpcMethod.Name, rpcMethod);
        }

        private static void AddToExisting(MethodInfo method, Type type) {
            RpcMethodInfo rpc = Cache[method.Name];
            ParameterInfo[] paramArray = method.GetParameters();

            if (paramArray.Length != rpc.ParamTypes.Count)
                throw new Exception("Duplicate RPC method with incompatible param count: " + method.Name);
            if (method.ReturnType != rpc.MethodInfoLookup.Values.First().ReturnType)
                throw new Exception("Duplicate RPC method with incompatible return type: " + method.Name);
            for (int i = 0; i < paramArray.Length; i++) {
                if (paramArray[i].ParameterType != rpc.ParamTypes[i])
                    throw new Exception("Duplicate RPC method with incompatible param types: " + method.Name);
            }
            rpc.MethodInfoLookup.Add(type, method);
        }

        /// <summary> Returns true if member method has NetRPC attribute. </summary>
        private static bool HasAttribute(MemberInfo member) {

            foreach (object attribute in member.GetCustomAttributes(true)) {
                if (attribute is NetRPCAttribute) return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a Dictionary with RPC method names as the key and the Monobehaviour instance for that method as the value.
        /// </summary>
        internal static Dictionary<string, object> CreateInstanceLookup(GameObject netObject) {

            MonoBehaviour[] monoBehaviours = netObject.GetComponents<MonoBehaviour>();
            Dictionary<string, object> assignments = new Dictionary<string, object>();

            foreach (KeyValuePair<string, RpcMethodInfo> cachedRpc in RpcMethods()) {
                foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
                    if (!cachedRpc.Value.MethodInfoLookup.ContainsKey(monoBehaviour.GetType())) continue;
                    assignments.Add(cachedRpc.Value.Name, monoBehaviour);
                    break;
                }
            }

            return assignments;
        }

    }
}