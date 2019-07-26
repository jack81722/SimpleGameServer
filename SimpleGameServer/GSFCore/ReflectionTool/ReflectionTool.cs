using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ReflectionTool
{
    public static class ReflectionTool
    {
        /// <summary>
        /// Invoke method in source by specific method name and arguments
        /// </summary>
        /// <param name="source">source instance of method</param>
        /// <param name="methodName">method name</param>
        /// <param name="args">method arguments</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static object InvokeMethod(object source, string methodName, params object[] args)
        {
            Type type = source.GetType();
            MethodInfo[] mInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < mInfos.Length; i++)
            {
                MethodInfo info = mInfos[i];
                // check if both name is matched
                if (info.Name.Equals(methodName))
                {
                    // if method is matched, then execute it
                    if (_CompareParams(info, args))
                        return info.Invoke(source, args);
                }
            }
            // not found method
            throw new InvalidOperationException("Method not found.");
        }

        /// <summary>
        /// Try to invoke method in source by specific method name and arguments
        /// </summary>
        /// <param name="source">source instance of method</param>
        /// <param name="methodName">method name</param>
        /// <param name="result">method result</param>
        /// <param name="args">method arguments</param>
        /// <returns>boolean of method found or not</returns>
        public static bool TryInvokeMethod(object source, string methodName, out object result, params object[] args)
        {
            Type type = source.GetType();
            MethodInfo[] mInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < mInfos.Length; i++)
            {
                MethodInfo info = mInfos[i];
                // check if both name is matched
                if (info.Name.Equals(methodName))
                {
                    // if method is matched, then execute it
                    if (_CompareParams(info, args))
                    {
                        result = info.Invoke(source, args);
                        return true;
                    }
                }
            }
            // not found method
            result = null;
            return false;
        }

        /// <summary>
        /// Compare all parameters of method and arguments
        /// </summary>
        /// <param name="methodInfo">method information</param>
        /// <param name="args">arguments</param>
        /// <returns>compare result</returns>
        private static bool _CompareParams(MethodInfo methodInfo, params object[] args)
        {   
            // check if all of parameters are valid
            ParameterInfo[] pInfos = methodInfo.GetParameters();
            if (args.Length <= pInfos.Length)
            {
                for (int j = 0; j < pInfos.Length; j++)
                {
                    if (j < args.Length)                    // check if parameters are matched
                    {
                        if (pInfos[j].ParameterType != args[j].GetType())
                            return false;
                    }
                    else if (!pInfos[j].HasDefaultValue)    // check if parameter has default value
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deep clone object by reflection
        /// </summary>
        /// <param name="source">cloned source</param>
        public static object Clone(object source)
        {
            // create clone instance
            Type type = source.GetType();
            object clone = Activator.CreateInstance(type);
            // clone all of fields
            FieldInfo[] fInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fInfos.Length; i++)
            {
                object value = fInfos[i].GetValue(source);
                fInfos[i].SetValue(clone, value);
            }
            // clone all of properties
            PropertyInfo[] pInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < pInfos.Length; i++)
            {
                if (pInfos[i].CanRead && pInfos[i].CanWrite)
                {
                    object value = pInfos[i].GetValue(source);
                    pInfos[i].SetValue(clone, value);
                }
            }
            return clone;
        }

        /// <summary>
        /// Deep clone object by reflection
        /// </summary>
        /// <param name="source">cloned source</param>
        public static T Clone<T>(T source) where T : new()
        {
            // create clone instance
            Type type = typeof(T);
            T clone = Activator.CreateInstance<T>();
            // clone all of fields
            FieldInfo[] fInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fInfos.Length; i++)
            {
                object value = fInfos[i].GetValue(source);
                fInfos[i].SetValue(clone, value);
            }
            // clone all of properties
            PropertyInfo[] pInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < pInfos.Length; i++)
            {
                if (pInfos[i].CanRead && pInfos[i].CanWrite)
                {
                    object value = pInfos[i].GetValue(source);
                    pInfos[i].SetValue(clone, value);
                }
            }
            return clone;
        }

        /// <summary>
        /// Deep clone object by reflection
        /// </summary>
        /// <param name="source">cloned source</param>
        public static object DeepClone(object source)
        {
            // create clone instance
            Type type = source.GetType();
            object clone = Activator.CreateInstance(type);
            // clone all of fields
            FieldInfo[] fInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fInfos.Length; i++)
            {
                object value = fInfos[i].GetValue(source);
                EventInfo eInfo;
                // if value is valuetype, then set directly, else deep clone the data
                if (fInfos[i].FieldType.IsValueType)
                    fInfos[i].SetValue(clone, value);
                else if (value != null && (eInfo = type.GetEvent(fInfos[i].Name)) != null)
                {
                    Delegate eValue = value as Delegate;
                    Delegate[] delegates = eValue.GetInvocationList();
                    for (int j = 0; j < delegates.Length; j++)
                    {
                        // if type has same method
                        if (delegates[j].Method.DeclaringType == type)
                        {
                            // get same method from clone
                            MethodInfo cloneMethod = type.GetMethod(delegates[j].Method.Name);
                            Delegate cloneDelegate = Delegate.CreateDelegate(eInfo.EventHandlerType, clone, cloneMethod);
                            eInfo.AddEventHandler(clone, cloneDelegate);
                        }
                        else
                        {
                            eInfo.AddMethod.Invoke(delegates[j], null);
                        }
                    }
                }
                else if (value != null)
                    fInfos[i].SetValue(clone, Clone(value));
            }
            // clone all of properties
            PropertyInfo[] pInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < pInfos.Length; i++)
            {
                if (pInfos[i].CanRead && pInfos[i].CanWrite)
                {
                    object value = pInfos[i].GetValue(source);
                    if (pInfos[i].PropertyType.IsValueType)
                        pInfos[i].SetValue(clone, value);
                    else if (value != null)
                        pInfos[i].SetValue(clone, Clone(value));
                }
            }
            return clone;
        }

        /// <summary>
        /// Deep clone object by reflection
        /// </summary>
        /// <param name="source">cloned source</param>
        public static T DeepClone<T>(T source) where T : new()
        {
            // create clone instance
            Type type = typeof(T);
            T clone = Activator.CreateInstance<T>();
            // clone all of fields
            FieldInfo[] fInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fInfos.Length; i++)
            {
                object value = fInfos[i].GetValue(source);
                EventInfo eInfo;
                // if value is valuetype, then set directly, else deep clone the data
                if (fInfos[i].FieldType.IsValueType)
                    fInfos[i].SetValue(clone, value);
                else if(value != null && (eInfo = type.GetEvent(fInfos[i].Name)) != null)
                {
                    Delegate eValue = value as Delegate;
                    Delegate[] delegates = eValue.GetInvocationList();
                    for (int j = 0; j < delegates.Length; j++)
                    {
                        // if type has same method
                        if (delegates[j].Method.DeclaringType == type)
                        {
                            // get same method from clone
                            MethodInfo cloneMethod = type.GetMethod(delegates[j].Method.Name);
                            Delegate cloneDelegate = Delegate.CreateDelegate(eInfo.EventHandlerType, clone, cloneMethod);
                            eInfo.AddEventHandler(clone, cloneDelegate);
                        }
                        else
                        {
                            eInfo.AddMethod.Invoke(delegates[j], null);
                        }
                    }
                }
                else if (value != null)
                {   
                    fInfos[i].SetValue(clone, Clone(value));
                }
            }
            // clone all of properties
            PropertyInfo[] pInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < pInfos.Length; i++)
            {
                if (pInfos[i].CanRead && pInfos[i].CanWrite)
                {
                    object value = pInfos[i].GetValue(source);
                    if (pInfos[i].PropertyType.IsValueType)
                        pInfos[i].SetValue(clone, value);
                    else if (value != null)
                        pInfos[i].SetValue(clone, Clone(value));
                }
            }
            return clone;
        }

        public static FieldInfo SearchFieldInfo(Type sourceType, string name, BindingFlags bindingFlags, bool baseType = true)
        {
            Type currentType = sourceType;
            FieldInfo fInfo = sourceType.GetField(name, bindingFlags);
            while(fInfo == null && currentType.BaseType != null)
            {
                currentType = currentType.BaseType;
                fInfo = currentType.GetField(name, bindingFlags);
            }
            return fInfo;
        }

        public static IEnumerable<MethodInfo> GetSubscribedMethods(Type sourceType, EventInfo eventInfo, object source)
        {
            var eInfo = sourceType.GetField(eventInfo.Name,
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Public);
            if (eInfo == null)
            {
                Type currentType = sourceType;
                while (eInfo == null && currentType.BaseType != null)
                {
                    eInfo = currentType.BaseType.GetField(eventInfo.Name,
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Public);
                }
                if(eInfo == null)
                    throw new InvalidOperationException("Event info not found.");
            }
            var eValue = eInfo.GetValue(source);
            var dValue = eValue as Delegate;
            if (dValue != null)
            {
                var delegates = dValue.GetInvocationList();
                return delegates.Select<Delegate, MethodInfo>(d => d.Method);
            }
            else
            {
                return new List<MethodInfo>();
            }
            
        }
    }
}
