using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DataQI.Analyzers.Parity.Test
{
    // The runtime parser only reads method names, so emitted interfaces need no implementation.
    internal static class RuntimeMethods
    {
        private static readonly object Gate = new object();
        private static readonly ModuleBuilder Module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("DataQI.Analyzers.Parity.Dynamic"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main");
        private static int counter;

        public static IReadOnlyDictionary<string, MethodInfo> Define(IEnumerable<string> distinctNames)
        {
            lock (Gate)
            {
                var type = Module.DefineType(
                    $"IQueries{Interlocked.Increment(ref counter)}",
                    TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

                foreach (var name in distinctNames)
                {
                    type.DefineMethod(
                        name,
                        MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual
                            | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                        typeof(object),
                        Type.EmptyTypes);
                }

                return type.CreateType().GetMethods().ToDictionary(method => method.Name);
            }
        }
    }
}
