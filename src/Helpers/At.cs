using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BF = System.Reflection.BindingFlags;

namespace SideLoader
{
    public static class At
    {
        public static readonly BF FLAGS = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        // ============ Main public API ============
        
        /// <summary>Helper to set an instance value on a non-static class. Use SetFieldStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the field in it.</typeparam>
        /// <param name="value">The value you want to set.</param>
        /// <param name="fieldName">The name of the field you want to set.</param>
        /// <param name="instance">The instance to use. Can be used to implicitly declare T if not null.</param>
        public static void SetField<T>(T instance, string fieldName, object value)
            => Internal_SetField(typeof(T), fieldName, instance, value);
        
        /// <summary>Helper to set a static value on a non-static class. Use SetFieldStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the field in it.</typeparam>
        /// <param name="value">The value you want to set.</param>
        /// <param name="fieldName">The name of the field you want to set.</param>
        public static void SetField<T>(string fieldName, object value)
            => Internal_SetField(typeof(T), fieldName, null, value);
        
        /// <summary>Helper to set a value on a Static Class (not just a static member of a class, use SetField&lt;T&gt; for that).</summary>
        /// <param name="value">The value you want to set.</param>
        /// <param name="type">The declaring class with the field in it.</param>
        /// <param name="fieldName">The name of the field you want to set.</param>
        public static void SetFieldStatic(Type type, string fieldName, object value)
            => Internal_SetField(type, fieldName, null, value);
        
        internal static void Internal_SetField(Type type, string fieldName, object instance, object value)
        {
            if (type == null)
                return;
        
            var fi = GetFieldInfo(type, fieldName);
            if (fi == null)
            {
                SL.LogWarning($"Could not find FieldInfo for Type '{type?.FullName ?? "<null>"}', field '{fieldName}'!");
                return;
            }
        
            if (fi.IsStatic)
                fi.SetValue(null, value);
            else
                fi.SetValue(instance, value);
        }
        
        /// <summary>Helper to get an instance value on a non-static class. Use GetFieldStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the field in it.</typeparam>
        /// <param name="fieldName">The name of the field you want to get.</param>
        /// <param name="instance">The instance to use, or null for static members. Can be used to implicitly declare T if not null.</param>
        public static object GetField<T>(T instance, string fieldName)
            => Internal_GetField(typeof(T), fieldName, instance);
        
        /// <summary>Helper to get a static value on a non-static class. Use GetFieldStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the field in it.</typeparam>
        /// <param name="fieldName">The name of the field you want to get.</param>
        public static object GetField<T>(string fieldName)
            => Internal_GetField(typeof(T), fieldName, null);
        
        /// <summary>Helper to get a value on a Static Class (not just a static member of a class, use GetField&lt;T&gt; for that).</summary>
        /// <param name="type">The declaring class with the field in it.</param>
        /// <param name="fieldName">The name of the field you want to get.</param>
        public static object GetFieldStatic(Type type, string fieldName)
            => Internal_GetField(type, fieldName, null);
        
        internal static object Internal_GetField(Type type, string fieldName, object instance)
            => Internal_GetField<object>(type, fieldName, instance);
        
        internal static R Internal_GetField<R>(Type type, string fieldName, object instance)
        {
            if (type == null)
                return default;
        
            var fi = GetFieldInfo(type, fieldName);
            if (fi == null)
            {
                SL.LogWarning($"Could not find FieldInfo for Type '{type?.FullName ?? "<null>"}', field '{fieldName}'!");
                return default;
            }
        
            if (fi.IsStatic)
                return (R)fi.GetValue(null);
            else
                return (R)fi.GetValue(instance);
        }
        
        /// <summary>Helper to set an instance value on a non-static class. Use SetPropertyStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the property in it.</typeparam>
        /// <param name="value">The value you want to set.</param>
        /// <param name="propertyName">The name of the property you want to set.</param>
        /// <param name="instance">The instance to use, or null for static members. Can be used to implicitly declare T if not null.</param>
        public static void SetProperty<T>(T instance, string propertyName, object value)
            => Internal_SetProperty(typeof(T), propertyName, value, instance);
        
        /// <summary>Helper to set a static value on a non-static class. Use SetPropertyStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the property in it.</typeparam>
        /// <param name="value">The value you want to set.</param>
        /// <param name="propertyName">The name of the property you want to set.</param>
        public static void SetProperty<T>(string propertyName, object value)
            => Internal_SetProperty(typeof(T), propertyName, value, null);
        
        /// <summary>Helper to set a value on a Static Class (not just a static member of a class, use SetProperty&lt;T&gt; for that).</summary>
        /// <param name="value">The value you want to set.</param>
        /// <param name="type">The declaring class with the property in it.</param>
        /// <param name="propertyName">The name of the property you want to set.</param>
        public static void SetPropertyStatic(Type type, string propertyName, object value)
            => Internal_SetProperty(type, propertyName, value, null);
        
        internal static void Internal_SetProperty(Type type, string propertyName, object value, object instance)
        {
            if (type == null)
                return;
        
            var pi = GetPropertyInfo(type, propertyName);
            if (pi == null || !pi.CanWrite)
            {
                SL.LogWarning($"Could not find setter PropertyInfo for Type '{type?.FullName ?? "<null>"}', field '{propertyName}'!");
                return;
            }
        
            try
            {
                var setter = pi.GetSetMethod(true);
                if (setter.IsStatic)
                    setter.Invoke(null, new object[] { value });
                else
                    setter.Invoke(instance, new object[] { value });
            }
            catch (Exception e)
            {
                SL.Log($"{e.GetType()} setting property {pi.Name}: {e.Message}\r\n{e.StackTrace}");
            }
        }
        
        /// <summary>Helper to get an instance value on a non-static class. Use GetPropertyStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the property in it.</typeparam>
        /// <param name="propertyName">The name of the property you want to get.</param>
        /// <param name="instance">The instance to use, or null for static members. Can be used to implicitly declare T if not null.</param>
        public static object GetProperty<T>(T instance, string propertyName)
            => Internal_GetProperty(typeof(T), propertyName, instance);
        
        /// <summary>Helper to get a static value on a non-static class. Use GetPropertyStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the property in it.</typeparam>
        /// <param name="propertyName">The name of the property you want to get.</param>
        public static object GetProperty<T>(string propertyName)
            => Internal_GetProperty(typeof(T), propertyName, null);
        
        /// <summary>Helper to get a value on a Static Class (not just a static member of a class, use GetProperty&lt;T&gt; for that).</summary>
        /// <param name="type">The declaring class with the property in it.</param>
        /// <param name="propertyName">The name of the property you want to get.</param>
        public static object GetPropertyStatic(Type type, string propertyName)
            => Internal_GetProperty(type, propertyName, null);
        
        internal static object Internal_GetProperty(Type type, string propertyName, object instance)
            => Internal_GetProperty<object>(type, propertyName, instance);
        
        internal static R Internal_GetProperty<R>(Type type, string propertyName, object instance)
        {
            if (type == null)
                return default;
        
            var pi = GetPropertyInfo(type, propertyName);
            if (pi == null || !pi.CanRead)
            {
                SL.LogWarning($"Could not find getter PropertyInfo for Type '{type?.FullName ?? "<null>"}', field '{propertyName}'!");
                return default;
            }
        
            try
            {
                var getter = pi.GetGetMethod(true);
                if (getter.IsStatic)
                    return (R)getter.Invoke(null, null);
                else
                    return (R)getter.Invoke(instance, null);
            }
            catch (Exception e)
            {
                SL.Log($"{e.GetType()} getting property {pi.Name}: {e.Message}\r\n{e.StackTrace}");
                return default;
            }
        }
        
        /// <summary>Helper to call an instance method on a non-static class. Use InvokeStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the method in it.</typeparam>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="instance">The instance to invoke on, or null for static methods.</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object Invoke<T>(T instance, string methodName, params object[] args)
            => Internal_Invoke(typeof(T), methodName, null, instance, args);
        
        /// <summary>Helper to call a static method on a non-static class. Use InvokeStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the method in it.</typeparam>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object Invoke<T>(string methodName, params object[] args)
            => Internal_Invoke(typeof(T), methodName, null, null, args);
        
        /// <summary>Helper to call an ambiguous instance method on a non-static class. Use InvokeStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the method in it.</typeparam>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="instance">The instance to invoke on, or null for static methods.</param>
        /// <param name="argumentTypes">Optional, for ambiguous methods you can provide an array corresponding to the Types of the arguments.</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object Invoke<T>(T instance, string methodName, Type[] argumentTypes, params object[] args)
            => Internal_Invoke(typeof(T), methodName, argumentTypes, instance, args);
        
        /// <summary>Helper to call an ambiguous static method on a non-static class. Use InvokeStatic for Static Classes.</summary>
        /// <typeparam name="T">The declaring class with the method in it.</typeparam>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="argumentTypes">Optional, for ambiguous methods you can provide an array corresponding to the Types of the arguments.</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object Invoke<T>(string methodName, Type[] argumentTypes, params object[] args)
            => Internal_Invoke(typeof(T), methodName, argumentTypes, null, args);
        
        /// <summary>Helper to call a method on a Static Class (not just a static member of a class, use Invoke&lt;T&gt; for that).</summary>
        /// <param name="type">The declaring class with the method in it.</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object InvokeStatic(Type type, string methodName, params object[] args)
            => Internal_Invoke(type, methodName, null, null, args);
        
        /// <summary>Helper to call an ambiguous method on a Static Class (not just a static member of a class, use Invoke&lt;T&gt; for that).</summary>
        /// <param name="type">The declaring class with the method in it.</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="argumentTypes">Optional, for ambiguous methods you can provide an array corresponding to the Types of the arguments.</param>
        /// <param name="args">The arguments you want to provide for invocation.</param>
        /// <returns>The return value of the method.</returns>
        public static object InvokeStatic(Type type, string methodName, Type[] argumentTypes, params object[] args)
            => Internal_Invoke(type, methodName, argumentTypes, null, args);
        
        internal static object Internal_Invoke(Type type, string methodName, Type[] argumentTypes, object instance, params object[] args)
            => Internal_Invoke<object>(type, methodName, argumentTypes, instance, args);
        
        internal static R Internal_Invoke<R>(Type type, string methodName, Type[] argumentTypes, object instance, params object[] args)
        {
            if (type == null)
                return default;
        
            var mi = GetMethodInfo(type, methodName, argumentTypes);
            if (mi == null)
            {
                SL.LogWarning($"Could not find MethodInfo for Type '{type?.FullName ?? "<null>"}', field '{methodName}'!");
                return default;
            }
        
            try
            {
                if (mi.IsStatic)
                    return (R)mi.Invoke(null, args);
                else
                    return (R)mi.Invoke(instance, args);
            }
            catch (Exception e)
            {
                SL.LogWarning("Exception invoking method: " + mi.ToString());
                SL.LogInnerException(e);
                return default;
            }
        }

        // ============ MISC TOOLS ============

        /// <summary>
        /// Get all Types from the Assembly. Guaranteed to not throw an exception or return null.
        /// </summary>
        /// <param name="asm">The assembly to get Types from</param>
        /// <returns>An <c>IEnumerable&lt;Type&gt;</c> of the Types from the Assembly, or an <c>Enumerable.Empty</c> if none could be retrieved.</returns>
        public static IEnumerable<Type> GetTypesSafe(this Assembly asm)
        {
            try
            {
                return asm.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(it => it != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Try to create an instance of the provided type. This is not guaranteed to work, but it should work if the type
        /// has a default constructor, or is a string or Array.
        /// </summary>
        /// <param name="type">The type to try to create an instance of. Guaranteed to work only for strings, Arrays, and types with a default constructor.</param>
        /// <returns>An instance of the type, if successful, otherwise null.</returns>
        public static object TryCreateDefault(Type type)
        {
            object instance;
            if (type == typeof(string))
                instance = string.Empty;
            else if (type.IsArray)
                instance = Array.CreateInstance(type.GetElementType(), 0);
            else
                instance = Activator.CreateInstance(type);
            return instance;
        }

        internal static readonly Dictionary<Type, HashSet<Type>> s_cachedTypeInheritance = new Dictionary<Type, HashSet<Type>>();
        internal static int s_lastAssemblyCount;

        /// <summary>
        /// Get all non-abstract implementations of the provided type (include itself, if not abstract) in the current AppDomain.
        /// </summary>
        /// <param name="baseType">The base type, which can optionally be abstract / interface.</param>
        /// <returns>All implementations of the type in the current AppDomain.</returns>
        public static HashSet<Type> GetImplementationsOf(this Type baseType)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (!s_cachedTypeInheritance.ContainsKey(baseType) || assemblies.Length != s_lastAssemblyCount)
            {
                if (assemblies.Length != s_lastAssemblyCount)
                {
                    s_cachedTypeInheritance.Clear();
                    s_lastAssemblyCount = assemblies.Length;
                }

                var set = new HashSet<Type>();

                if (!baseType.IsAbstract && !baseType.IsInterface)
                    set.Add(baseType);

                foreach (var asm in assemblies)
                {
                    foreach (var t in asm.GetTypesSafe().Where(t => !t.IsAbstract && !t.IsInterface))
                    {
                        if (baseType.IsAssignableFrom(t) && !set.Contains(t))
                            set.Add(t);
                    }
                }

                s_cachedTypeInheritance.Add(baseType, set);
            }

            return s_cachedTypeInheritance[baseType];
        }

        /// <summary>
        /// Helper to get a Type by providing the 'Type.FullName'.
        /// </summary>
        /// <param name="fullName">The full name, eg "System.String"</param>
        /// <returns>The type, if found.</returns>
        public static Type GetTypeByName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypesSafe())
                {
                    if (type.FullName == fullName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A helper to get all the fields from one class instance, and set them to another.
        /// </summary>
        /// <param name="copyTo">The object which you are setting values to.</param>
        /// <param name="copyFrom">The object which you are getting values from.</param>
        /// <param name="declaringType">Optional, manually define the declaring class type.</param>
        /// <param name="recursive">Whether to recursively dive into the BaseTypes and copy those fields too</param>
        public static void CopyFields(object copyTo, object copyFrom, Type declaringType = null, bool recursive = false)
        {
            var type = declaringType ?? copyFrom.GetType();

            if (type.IsAssignableFrom(copyTo.GetType()) && type.IsAssignableFrom(copyFrom.GetType()))
            {
                foreach (FieldInfo fi in type.GetFields(FLAGS))
                {
                    try
                    {
                        fi.SetValue(copyTo, fi.GetValue(copyFrom));
                    }
                    catch { }
                }
            }

            if (recursive && type.BaseType is Type baseType)
            {
                // We don't want to copy Unity low-level types, such as MonoBehaviour or Component.
                // Copying these fields causes serious errors.
                if (baseType != typeof(MonoBehaviour) && baseType != typeof(Component))
                {
                    CopyFields(copyTo, copyFrom, type.BaseType, true);
                }
            }

            return;
        }

        /// <summary>
        /// A helper to get all the properties from one class instance, and set them to another.
        /// </summary>
        /// <param name="copyTo">The object which you are setting values to.</param>
        /// <param name="copyFrom">The object which you are getting values from.</param>
        /// <param name="declaringType">Optional, manually define the declaring class type.</param>
        /// <param name="recursive">Whether to recursively dive into the BaseTypes and copy those properties too</param>
        public static void CopyProperties(object copyTo, object copyFrom, Type declaringType = null, bool recursive = false)
        {
            var type = declaringType ?? copyFrom.GetType();

            if (type.IsAssignableFrom(copyTo.GetType()) && type.IsAssignableFrom(copyFrom.GetType()))
            {
                foreach (var pi in type.GetProperties(FLAGS).Where(x => x.CanWrite))
                {
                    try
                    {
                        pi.SetValue(copyTo, pi.GetValue(copyFrom, null), null);
                    }
                    catch { }
                }
            }

            if (recursive && type.BaseType is Type baseType)
            {
                // We don't want to copy Unity low-level types, such as MonoBehaviour or Component.
                // Copying these fields causes serious errors.
                if (baseType != typeof(MonoBehaviour) && baseType != typeof(Component))
                {
                    CopyProperties(copyTo, copyFrom, type.BaseType, true);
                }
            }
        }

        // ========= These methods are used to cache all MemberInfos used by this class =========
        // Can also be used publicly if anyone should want to.

        internal static Dictionary<Type, Dictionary<string, FieldInfo>> s_cachedFieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (!s_cachedFieldInfos.ContainsKey(type))
                s_cachedFieldInfos.Add(type, new Dictionary<string, FieldInfo>());

            if (!s_cachedFieldInfos[type].ContainsKey(fieldName))
                s_cachedFieldInfos[type].Add(fieldName, type.GetField(fieldName, FLAGS));

            return s_cachedFieldInfos[type][fieldName];
        }

        internal static Dictionary<Type, Dictionary<string, PropertyInfo>> s_cachedPropInfos = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            if (!s_cachedPropInfos.ContainsKey(type))
                s_cachedPropInfos.Add(type, new Dictionary<string, PropertyInfo>());

            if (!s_cachedPropInfos[type].ContainsKey(propertyName))
                s_cachedPropInfos[type].Add(propertyName, type.GetProperty(propertyName, FLAGS));

            return s_cachedPropInfos[type][propertyName];
        }

        internal static Dictionary<Type, Dictionary<string, MethodInfo>> s_cachedMethodInfos = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static MethodInfo GetMethodInfo(Type type, string methodName, Type[] argumentTypes)
        {
            if (!s_cachedMethodInfos.ContainsKey(type))
                s_cachedMethodInfos.Add(type, new Dictionary<string, MethodInfo>());

            var sig = methodName;

            if (argumentTypes != null)
            {
                sig += "(";
                for (int i = 0; i < argumentTypes.Length; i++)
                {
                    if (i > 0)
                        sig += ",";
                    sig += argumentTypes[i].FullName;
                }
                sig += ")";
            }

            try
            {
                if (!s_cachedMethodInfos[type].ContainsKey(sig))
                {
                    if (argumentTypes != null)
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, FLAGS, null, argumentTypes, null));
                    else
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, FLAGS));
                }

                return s_cachedMethodInfos[type][sig];
            }
            catch (AmbiguousMatchException)
            {
                SL.LogWarning($"AmbiguousMatchException trying to get method '{sig}'");
                return null;
            }
            catch (Exception e)
            {
                SL.LogWarning($"{e.GetType()} trying to invoke method '{sig}': {e.Message}\r\n{e.StackTrace}");
                return null;
            }
        }

        // ~~~~~~~~~~~~~~~~~ Deprecated ~~~~~~~~~~~~~~~~~ //

        [Obsolete("Use SetField<T> or SetFieldStatic.")]
        public static void SetValue<T>(T value, Type type, object obj, string field)
            => Internal_SetField(type, field, obj, value);

        [Obsolete("Use GetField<T> or GetFieldStatic.")]
        public static object GetValue(Type type, object obj, string field)
            => Internal_GetField(type, field, obj);

        [Obsolete("Use Invoke<T> or InvokeStatic.")]
        public static object Call(Type type, object obj, string method, Type[] argumentTypes, params object[] args)
            => Internal_Invoke(type, method, argumentTypes, obj, args);

        [Obsolete("Use SetProperty<T> or SetPropertyStatic.")]
        public static void SetProp<T>(T value, Type type, object obj, string property)
            => Internal_SetProperty(type, property, value, obj);

        [Obsolete("Use GetProperty<T> or GetPropertyStatic.")]
        public static object GetProp(Type type, object obj, string property)
            => Internal_GetProperty(type, property, obj);
    }
}
