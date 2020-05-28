

namespace System.Threading
{


    public static unsafe class Volatile
    {

        
        public static void Write(ref int location, int value)
        {
            location = value;
        }


    }


}
