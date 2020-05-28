
namespace CoreDb
{

    public delegate object Getter_t<T>(T obj);
    public delegate void Setter_t<T>(T obj, object value);


    internal class LinqHelper
    {
        
        private static System.Reflection.MethodInfo m_FlexibleChangeType;

        private static object FlexibleChangeType(object objVal, System.Type targetType)
        {
            System.Reflection.TypeInfo ti = System.Reflection.IntrospectionExtensions.GetTypeInfo(targetType);
            bool typeIsNullable = (ti.IsGenericType && object.ReferenceEquals(targetType.GetGenericTypeDefinition(), typeof(System.Nullable<>)));
            bool typeCanBeAssignedNull = !ti.IsValueType || typeIsNullable;

            if (objVal == null || object.ReferenceEquals(objVal, System.DBNull.Value))
            {
                if (typeCanBeAssignedNull)
                    return null;
                else
                    throw new System.ArgumentNullException("objVal ([DataSource] => SetProperty => FlexibleChangeType => you're trying to assign NULL to a type that NULL cannot be assigned to...)");
            } // End if (objVal == null || object.ReferenceEquals(objVal, System.DBNull.Value))

            // Get base-type
            System.Type tThisType = objVal.GetType();

            if (typeIsNullable)
            {
                targetType = System.Nullable.GetUnderlyingType(targetType);
            } // End if (typeIsNullable) 


            if (object.ReferenceEquals(tThisType, targetType))
                return objVal;

            // Convert Guid => string
            if (object.ReferenceEquals(targetType, typeof(string)) && object.ReferenceEquals(tThisType, typeof(System.Guid)))
            {
                return objVal.ToString();
            } // End if (object.ReferenceEquals(targetType, typeof(string)) && object.ReferenceEquals(tThisType, typeof(System.Guid)))

            // Convert string => System.Net.IPAddress
            if (object.ReferenceEquals(targetType, typeof(System.Net.IPAddress)) && object.ReferenceEquals(tThisType, typeof(string)))
            {
                return System.Net.IPAddress.Parse(objVal.ToString());
            } // End if (object.ReferenceEquals(targetType, typeof(System.Net.IPAddress)) && object.ReferenceEquals(tThisType, typeof(string)))

            // Convert string => TimeSpan
            if (object.ReferenceEquals(targetType, typeof(System.TimeSpan)) && object.ReferenceEquals(tThisType, typeof(string)))
            {
                // https://stackoverflow.com/questions/11719055/why-does-timespan-parseexact-not-work
                // This is grotesque... ParseExact ignores the 12/24 hour convention...
                // return System.TimeSpan.ParseExact(objVal.ToString(), "HH':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture); // Exception 
                // return System.TimeSpan.ParseExact(objVal.ToString(), "hh\\:mm\\:ss", System.Globalization.CultureInfo.InvariantCulture); // This works, bc of lowercase ?
                // return System.TimeSpan.ParseExact(objVal.ToString(), "hh':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture); // Yep, lowercase - no 24 notation...

                // return System.TimeSpan.Parse(objVal.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                return System.TimeSpan.Parse(System.Convert.ToString(objVal, System.Globalization.CultureInfo.InvariantCulture));
            } // End if (object.ReferenceEquals(targetType, typeof(System.TimeSpan)) && object.ReferenceEquals(tThisType, typeof(string))) 

            // Convert string => DateTime
            if (object.ReferenceEquals(targetType, typeof(System.DateTime)) && object.ReferenceEquals(tThisType, typeof(string)))
            {
                return System.DateTime.Parse(objVal.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            } // End if (object.ReferenceEquals(targetType, typeof(System.DateTime)) && object.ReferenceEquals(tThisType, typeof(string)))

            // Convert string => Guid 
            if (object.ReferenceEquals(targetType, typeof(System.Guid)) && object.ReferenceEquals(tThisType, typeof(string)))
            {
                return new System.Guid(objVal.ToString());
            } // End else if (object.ReferenceEquals(targetType, typeof(System.Guid)) && object.ReferenceEquals(tThisType, typeof(string))) 

            return System.Convert.ChangeType(objVal, targetType);
        } // End Function FlexibleChangeType



        static LinqHelper()
        {
            m_FlexibleChangeType = System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(LinqHelper))
                .GetMethod("FlexibleChangeType", System.Reflection.BindingFlags.Static 
                | System.Reflection.BindingFlags.NonPublic
            );
        } // End static Constructor 



        // https://stackoverflow.com/questions/321650/how-do-i-set-a-field-value-in-an-c-sharp-expression-tree
#if false 
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        //public static System.Action<T, object> GetSetter<T>(string fieldName)
        public static Setter_t<T> GetSetter<T>(string fieldName)
        {
            // Class in which to set value
            System.Linq.Expressions.ParameterExpression targetExp = System.Linq.Expressions.Expression.Parameter(typeof(T), "target");

            // Object's type:
            System.Linq.Expressions.ParameterExpression valueExp = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");


            // Expression.Property can be used here as well
            System.Linq.Expressions.MemberExpression memberExp = null;
            try
            {
                // memberExp = System.Linq.Expressions.Expression.Field(targetExp, fieldName);
                // memberExp = System.Linq.Expressions.Expression.Property(targetExp, fieldName);
                memberExp = System.Linq.Expressions.Expression.PropertyOrField(targetExp, fieldName);
            }
            catch (System.Exception ex)
            {
                return null;
            }
            

            // http://www.dotnet-tricks.com/Tutorial/linq/RJX7120714-Understanding-Expression-and-Expression-Trees.html
            System.Linq.Expressions.ConstantExpression targetType = System.Linq.Expressions.Expression.Constant(memberExp.Type);

            // http://stackoverflow.com/questions/913325/how-do-i-make-a-linq-expression-to-call-a-method
            System.Linq.Expressions.MethodCallExpression mce = System.Linq.Expressions.Expression.Call(m_FlexibleChangeType, valueExp, targetType);


            //System.Linq.Expressions.UnaryExpression conversionExp = System.Linq.Expressions.Expression.Convert(valueExp, memberExp.Type);
            System.Linq.Expressions.UnaryExpression conversionExp = System.Linq.Expressions.Expression.Convert(mce, memberExp.Type);


            System.Linq.Expressions.BinaryExpression assignExp =
                // System.Linq.Expressions.Expression.Assign(memberExp, valueExp); // Without conversion 
                System.Linq.Expressions.Expression.Assign(memberExp, conversionExp);

            

            // System.Action<TTarget, TValue> setter = System.Linq.Expressions.Expression
            // System.Action<T, object> setter = System.Linq.Expressions.Expression
            // .Lambda<System.Action<T, object>>(assignExp, targetExp, valueExp).Compile();

            Setter_t<T> setter = System.Linq.Expressions.Expression
                .Lambda<Setter_t<T>>(assignExp, targetExp, valueExp).Compile();

            return setter;
        } // End Function GetGetter 


#if false 
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        // public static System.Func<T, object> GetGetter<T>(string fieldName)
        public static Getter_t<T> GetGetter<T>(string fieldName)
        {
            System.Linq.Expressions.ParameterExpression p = System.Linq.Expressions.Expression.Parameter(typeof(T), "TODOname");
            System.Linq.Expressions.MemberExpression prop = System.Linq.Expressions.Expression.PropertyOrField(p, fieldName);
            System.Linq.Expressions.UnaryExpression con = System.Linq.Expressions.Expression.Convert(prop, typeof(object));
            
            System.Linq.Expressions.Expression<Getter_t<T>> exp = System.Linq.Expressions.Expression.Lambda<Getter_t<T>>(con, p);
            return exp.Compile();

            //System.Linq.Expressions.LambdaExpression exp = System.Linq.Expressions.Expression.Lambda(con, p);
            //// Getter_t<T> getter = (Getter_t<T>)exp.Compile();
            //System.Func<T, object> getter = (System.Func<T, object>)exp.Compile();
            //// Getter_t<T> getter2 = (Getter_t<T>)(arg1 => getter(arg1));
            //Getter_t<T> getter2 = (Getter_t<T>)delegate (T arg1)
            //{
            //    return getter(arg1);
            //};

            // return getter;
            // return getter2;
        } // End Function GetGetter 



        // ========================================================================================================================



        public static System.Reflection.MemberInfo[] GetFieldsAndProperties(System.Type t)
        {
            System.Reflection.TypeInfo ti = System.Reflection.IntrospectionExtensions.GetTypeInfo(t);

            System.Reflection.FieldInfo[] fis = ti.GetFields();
            System.Reflection.PropertyInfo[] pis = ti.GetProperties();
            System.Reflection.MemberInfo[] mis = new System.Reflection.MemberInfo[fis.Length + pis.Length];
            System.Array.Copy(fis, mis, fis.Length);
            System.Array.Copy(pis, 0, mis, fis.Length, pis.Length);

            return mis;
        } // End Function GetFieldsAndProperties 


        public static Getter_t<T>[] GetGetters<T>()
        {
            System.Reflection.MemberInfo[] mis = GetFieldsAndProperties(typeof(T));

            string[] memberNames = new string[mis.Length];
            for (int i = 0; i < mis.Length; ++i)
            {
                memberNames[i] = mis[i].Name;
            }

            return GetGetters<T>(memberNames);
        } // End Function GetGetters 


        public static Getter_t<T>[] GetGetters<T>(string[] fieldNames)
        {
            Getter_t<T>[] iisLogGetters = new Getter_t<T>[fieldNames.Length];

            for (int i = 0; i < fieldNames.Length; ++i)
            {
                iisLogGetters[i] = GetGetter<T>(fieldNames[i]);
            }

            return iisLogGetters;
        } // End Function GetGetters 


        public static Setter_t<T>[] GetSetters<T>()
        {
            System.Reflection.MemberInfo[] mis = GetFieldsAndProperties(typeof(T));

            string[] memberNames = new string[mis.Length];
            for (int i = 0; i < mis.Length; ++i)
            {
                memberNames[i] = mis[i].Name;
            }

            return GetSetters<T>(memberNames);
        } // End Function GetSetters 


        public static Setter_t<T>[] GetSetters<T>(string[] fieldNames)
        {
            // System.Action<T, object>[] iisLogSetters = new System.Action<T, object>[fieldNames.Length];
            Setter_t<T>[] iisLogSetters = new Setter_t<T>[fieldNames.Length];

            for (int i = 0; i < fieldNames.Length; ++i)
            {
                iisLogSetters[i] = GetSetter<T>(fieldNames[i]);
            }

            return iisLogSetters;
        } // End Function GetSetters 


    } // End Class LinqHelper 


} // End Namespace MyLogParser 
