
namespace System.Reflection
{


    public class TypeInfo
    {

        protected System.Type m_baseType;

        public bool IsGenericType
        {
            get
            {
                return m_baseType.IsGenericType;
            }
        }

        public bool IsValueType
        {
            get
            {
                return m_baseType.IsValueType;
            }
        }


        public System.Reflection.FieldInfo[] GetFields()
        {
            return this.m_baseType.GetFields();
        }

        public System.Reflection.PropertyInfo[] GetProperties()
        {
            return this.m_baseType.GetProperties();
        }



        public System.Type GetGenericTypeDefinition()
        {
            return m_baseType.GetGenericTypeDefinition();
        }


        //public static implicit operator decimal(TypeInfo rhs)
        //{
        //    return 12.0M;
        //}

        public static implicit operator System.Type(TypeInfo rhs)
        {
            return rhs.m_baseType;
        }


        public static implicit operator TypeInfo(System.Type rhs)
        {
            TypeInfo ti = new TypeInfo();
            ti.m_baseType = rhs;
            return ti;
        }


    }


    public class IntrospectionExtensions
    {

        public static System.Type GetTypeInfo(System.Type t)
        {
            return t;
        }

    }
}
