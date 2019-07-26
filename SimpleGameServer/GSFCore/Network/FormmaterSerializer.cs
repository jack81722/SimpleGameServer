using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace GameSystem.GameCore.Network
{
    public class FormmaterSerializer : LazySingleton<FormmaterSerializer>, ISerializer
    {
        public T Deserialize<T>(byte[] dgram)
        {
            //GSFPacket packet = (GSFPacket)ToObject(dgram);
            //return PacketUtility.Unpack<T>(packet);
            return (T)ToObject(dgram);
        }

        public object Deserialize(byte[] dgram)
        {   
            return ToObject(dgram);
        }

        public byte[] Serialize(object obj)
        {   
            return ToByteArray(obj);
        }

        private byte[] ToByteArray(object source)
        {
            var Formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new System.IO.MemoryStream())
            {
                Formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        private object ToObject(byte[] source)
        {
            try
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                using (var stream = new MemoryStream(source))
                {
                    formatter.Binder = new CurrentAssemblyDeserializationBinder();
                    formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    return formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                return new object();
            }
        }

        private sealed class CurrentAssemblyDeserializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                Type result = null;
                var v = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var typeIndex = $"{typeName}, {assembly.FullName}";
                    result = Type.GetType(typeIndex);
                    if (result != null)
                    {
                        break;
                    }
                }

                if (result == null)
                {
                    Console.WriteLine($"Serialize Error : can't find {typeName} in all assembly");
                }
                return result;
            }
        }
    }

}
