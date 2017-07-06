namespace ILMergeGui
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.ComponentModel;

    /*
     * 03-05-2012 - veg - Added GetProperty().
     * 04-05-2012 - veg - Disabled slow code.
     *                  - Added cArgs to the public methods to allow paremetrized constructors.
     *                  - Added Documentation.
     *                  - Renamed methods
     *                  - Made most methods private.
     *                  - Altered signatures of method to use Generics 
     *                  - Swapped HashTables for types Dictionaries.
     *                  - Removed unneccesary typecast.
     *                  
     * TODO - Add BindingFlags support other than BindingFlags.Default.
     * TODO - Look into params keyword.
     */

    /// <summary>
    /// A class that allows you to 
    /// 1) call to methods and 
    /// 2) get/set properties 
    /// on a dynamically loaded assembly.
    /// </summary>
    public class DynaInvoke
    {
        /*
        /// <summary>
        /// This way of invoking a function is slower when making multiple calls
        /// because the assembly is being instantiated each time.
        /// But this code is clearer as to what is going on
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="AssemblyName"></param>
        /// <param name="ClassName"></param>
        /// <param name="MethodName"></param>
        /// <param name="cArgs"></param>
        /// <param name="mArgs"></param>
        /// <returns></returns>
        public static T CallMethodSlow<T>(string AssemblyName,
                string ClassName, string MethodName, Object[] cArgs, Object[] mArgs)
        {
            // load the assemly

            Assembly assembly = Assembly.LoadFrom(AssemblyName);

            // Walk through each type in the assembly looking for our class

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + ClassName))
                    {
                        // create an instance of the object

                        //object ClassObj = Activator.CreateInstance(type);
                        object ClassObj = Activator.CreateInstance(type, cArgs);

                        // Dynamically Invoke the method

                        object Result = type.InvokeMember(MethodName,
                            BindingFlags.Default | BindingFlags.InvokeMethod,
                                null,
                                ClassObj,
                                mArgs);
                        return (T)Result;
                    }
                }
            }

            throw (new System.Exception("could not invoke method"));
        }
        */

        /// <summary>
        /// Now do it the efficient way by holding references to the assembly 
        /// and the class this is an inner class which holds the class instance info
        /// </summary>
        internal class DynaClassInfo
        {
            public Type type;
            public Object ClassObject;

            public DynaClassInfo()
            {
            }

            public DynaClassInfo(Type t, Object c)
            {
                type = t;
                ClassObject = c;
            }
        }

        private static Dictionary<String, Assembly> AssemblyReferences = new Dictionary<String, Assembly>();
        private static Dictionary<String, DynaClassInfo> ClassReferences = new Dictionary<String, DynaClassInfo>();

        internal static DynaClassInfo GetClassReference(string AssemblyName, string ClassName, Object[] cArgs = null)
        {
            if (PreLoadAssembly(AssemblyName, ClassName, cArgs))
            {
                return (ClassReferences[AssemblyName]);
            }
            return null;
        }

        /// <summary>
        /// Preload an Assembly
        /// </summary>
        /// <param name="AssemblyName">The Filename of the Assembly</param>
        /// <param name="ClassName">The Classname to instantiate</param>
        /// <param name="cArgs">Constructor Parameters (or null)</param>
        /// <returns>true if succeeeded</returns>
        public static Boolean PreLoadAssembly(string AssemblyName, string ClassName, Object[] cArgs = null)
        {
            if (ClassReferences.ContainsKey(AssemblyName) == false)
            {
                Assembly assembly;
                if (AssemblyReferences.ContainsKey(AssemblyName) == false)
                {
                    AssemblyReferences.Add(AssemblyName,
                          assembly = Assembly.LoadFrom(AssemblyName));
                }
                else
                    assembly = AssemblyReferences[AssemblyName];

                // Walk through each type in the assembly
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsClass == true)
                    {
                        // Doing it this way means that you don't have
                        // to specify the full namespace and class (just the class)

                        if (type.FullName.EndsWith("." + ClassName))
                        {
                            try
                            {
                                DynaClassInfo ci = new DynaClassInfo(type,
                                                   Activator.CreateInstance(type, cArgs));
                                ClassReferences.Add(AssemblyName, ci);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.Message);
                            }
                        }
                    }
                }
            }

            return ClassReferences.ContainsKey(AssemblyName);
        }

        private static T CallMethod<T>(DynaClassInfo ci,
                             string MethodName, Object[] mArgs)
        {
            // Dynamically Invoke the method
            Object Result = ci.type.InvokeMember(MethodName,
              BindingFlags.Default | BindingFlags.InvokeMethod,
                   null,
                   ci.ClassObject,
                   mArgs);
            return (T)Result;
        }

        private static T SetProperty<T>(DynaClassInfo ci,
                            string PropName, T arg)
        {
            // Dynamically Invoke the method
            PropertyInfo pi = ci.type.GetProperty(PropName);

            //Type type = typeof(T);
            Type ptype = pi.PropertyType;

            if (ptype.IsGenericType && ptype.GetGenericArguments().Length > 0 && ptype.GetGenericArguments()[0].IsEnum)
            {
                // Convert Int32 to Enum?
                TypeConverter convertSet = TypeDescriptor.GetConverter(pi.PropertyType);
                pi.SetValue(ci.ClassObject, convertSet.ConvertFrom(arg.ToString() + ""), new Object[] { });

                // Convert Enum? to Int32
                TypeConverter convertGet = TypeDescriptor.GetConverter(typeof(T));
                int tmp = (int)(pi.GetValue(ci.ClassObject, new Object[] { }));
                return (T)convertGet.ConvertFrom(tmp.ToString() + "");
            }
            else
            {
                pi.SetValue(ci.ClassObject, (T)arg, new Object[] { });

                return (T)(pi.GetValue(ci.ClassObject, new Object[] { }));
            }
        }

        private static T GetProperty<T>(DynaClassInfo ci,
                           string PropName)
        {
            // Dynamically Get a Property Value
            PropertyInfo pi = ci.type.GetProperty(PropName);

            Object value = pi.GetValue(ci.ClassObject, new Object[] { });
            if (value != null)
            {
                return (T)(value);
            }

            return default(T);
        }

        // --- these is the method that you invoke ------------

        /// <summary>
        /// Call a Generic typed Method on an (cached) Assembly.
        /// </summary>
        /// <typeparam name="T">The return Type</typeparam>
        /// <param name="AssemblyName">The Filename of the Assembly</param>
        /// <param name="ClassName">The Classname to instantiate</param>
        /// <param name="MethodName">The Method name to invoke</param>
        /// <param name="mArgs">Method Parameters</param>
        /// <param name="cArgs">Constructor Parameters (or null)</param>
        /// <returns>The method result if any</returns>
        public static T CallMethod<T>(String AssemblyName, String ClassName, String MethodName, Object[] mArgs, Object[] cArgs = null)
        {
            DynaClassInfo ci = GetClassReference(AssemblyName, ClassName, cArgs);

            return CallMethod<T>(ci, MethodName, mArgs);
        }

        /// <summary>
        /// Call a Method on an (cached) Assembly.
        /// </summary>
        /// <param name="AssemblyName">The Filename of the Assembly</param>
        /// <param name="ClassName">The Classname to instantiate</param>
        /// <param name="MethodName">The Method name to invoke</param>
        /// <param name="mArgs">Method Parameters</param>
        /// <param name="cArgs">Constructor Parameters (or null)</param>
        public static void CallMethod(String AssemblyName, String ClassName, String MethodName, Object[] mArgs, Object[] cArgs = null)
        {
            DynaClassInfo ci = GetClassReference(AssemblyName, ClassName, cArgs);

            CallMethod<Object>(ci, MethodName, mArgs);
        }

        /// <summary>
        /// Set a Property value on an (cached) Assembly.
        /// </summary>
        /// <typeparam name="T">The Property Type</typeparam>
        /// <param name="AssemblyName">The Filename of the Assembly</param>
        /// <param name="ClassName">The Classname to instantiate</param>
        /// <param name="PropName">The Property to set</param>
        /// <param name="pArg">Property Value</param>
        /// <param name="cArgs">Constructor Parameters (or null)</param>
        /// <returns>The (new) Property Value</returns>
        public static T SetProperty<T>(String AssemblyName, String ClassName, String PropName, T pArg, Object[] cArgs = null)
        {
            DynaClassInfo ci = GetClassReference(AssemblyName, ClassName, cArgs);

            return SetProperty<T>(ci, PropName, pArg);
        }

        /// <summary>
        /// Return a Property value of an (cached) Assembly.
        /// </summary>
        /// <typeparam name="T">The Property Type</typeparam>
        /// <param name="AssemblyName">The Filename of the Assembly</param>
        /// <param name="ClassName">The Classname to instantiate</param>
        /// <param name="PropName">The Property to set</param>
        /// <param name="cArgs">Constructor Parameters (or null)</param>
        /// <returns>The Property Value</returns>
        public static T GetProperty<T>(String AssemblyName, String ClassName, String PropName, Object[] cArgs = null)
        {
            DynaClassInfo ci = GetClassReference(AssemblyName, ClassName, cArgs);

            return GetProperty<T>(ci, PropName);
        }
    }
}

////Arguments for Class Constructor. If you want to use default class constructor w/o any arguments just leave the array empty like this Object[] classConstructorArgs = {};
//Object[] classConstructorArgs = {@"OneArgForClassConstructor"};

//Object[] methodArgs = {1, "2", 3.0};

//Object Result = DynaInvoke.InvokeMethodSlow("c:\FullPathToDll.DLL", 
//                                            "ClassName",
//                                            "MethodName",
//                                            classConstructorArgs,
//                                            methodArgs);


//Just a suggestion but:
// in the signature of your InvokeMethodSlow method, declare the 'args' parameter as 'params' so that users can either pass an already created object array or variable length parameters to the function that will in turn be passed to the dynamic method.

// public static Object InvokeMethodSlow(string AssemblyName, string ClassName, string MethodName, params Object[] args)


//string FullTypeName = AssemblyName + "." + ClassName
// Type type=TheAssembly.GetType(FullTypeName);

// creates the type object directly without having to walk the types in the assembly 


