//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace SideLoader.Helpers
//{
//    public static class AtExtensions
//    {
//        //static void Test()
//        //{
//        //    //// if the member is definitely contained in this instance's type (and not inherited)
//        //    //var x = new Weapon().GetField<string>("m_someWeaponField");
//        //    // if the member is inherited
//        //    var y = new Weapon().GetField<string, Item>("m_someItemField");
//        //    // static fields will be fine as long as you use the correct type
//        //    var z = typeof(Item).GetField<string>("s_someStaticField");
//        //}

//        // ========= field extensions

//        /// <summary>
//        /// Helper to get the value of a non-static field on an object instance. <br/><br/>
//        /// If the field is <see langword="private"/>, it must be contained inside the <see cref="Type"/> of <typeparamref name="T"/>.
//        /// </summary>
//        /// <typeparam name="R">The Type of the return value you are expecting</typeparam>
//        /// <typeparam name="T">The type which contains the field, can be inherited if not private.</typeparam>
//        /// <param name="instance">The instance you want to get a field from</param>
//        /// <param name="fieldName">The name of the field to get</param>
//        /// <returns>The value from the field if successful, and/or null.</returns>
//        public static R GetField<R, T>(this T instance, string fieldName)
//            => At.Internal_GetField<R>(typeof(T), fieldName, instance);

//        /// <summary>
//        /// Helper to get the value of a static field inside a Type. 
//        /// </summary>
//        /// <typeparam name="R">The Type of the return value you are expecting</typeparam>
//        /// <param name="type">The type which contains the field you want to get.</param>
//        /// <param name="fieldName">The name of the field to get</param>
//        /// <returns>The value from the field if successful, and/or null.</returns>
//        public static R GetField<R>(this Type type, string fieldName)
//            => At.Internal_GetField<R>(type, fieldName, null);

//        /// <summary>
//        /// Helper to set the value of a field.<br/><br/>
//        /// If the field is <see langword="private"/>, it must be contained inside the <see cref="Type"/> of <typeparamref name="T"/>.
//        /// </summary>
//        /// <typeparam name="T">The type which contains the field, can be inherited if not private.</typeparam>
//        /// <param name="instance">The instance you want to set the field on</param>
//        /// <param name="fieldName">The name of the field to set</param>
//        /// <param name="value">The value you want to set on the field</param>
//        public static void SetField<T>(this T instance, string fieldName, object value)
//            => At.Internal_SetField(typeof(T), fieldName, instance, value);

//        // TODO DOC
//        public static void SetField(this Type type, string fieldName, object value)
//            => At.Internal_SetField(type, fieldName, null, value);

//        // ========= property extensions

//        // TODO DOC
//        public static R GetProperty<R, T>(this T instance, string propertyName)
//            => (R)At.Internal_GetProperty(typeof(T), propertyName, instance);

//        // TODO DOC
//        public static R GetProperty<R>(this Type type, string propertyName)
//            => At.Internal_GetProperty<R>(type, propertyName, null);

//        // TODO DOC
//        public static void SetProperty<T>(this T instance, string propertyName, object value)
//            => At.Internal_SetProperty(typeof(T), propertyName, value, instance);

//        // TODO DOC
//        public static void SetProperty(this Type type, string propertyName, object value)
//            => At.Internal_SetProperty(type, propertyName, value, null);

//        // ========= method extensions

//        // TODO DOC
//        public static R Invoke<R, T>(this T instance, string methodName, Type[] argumentTypes = null, params object[] args)
//            => At.Internal_Invoke<R>(typeof(T), methodName, argumentTypes, instance, args);

//        // TODO DOC
//        public static R Invoke<R>(this Type type, string methodName, Type[] argumentTypes = null, params object[] args)
//            => At.Internal_Invoke<R>(type, methodName, argumentTypes, null, args);


//    }
//}
