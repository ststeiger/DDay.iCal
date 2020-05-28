
namespace System
{
    class Environment2
    {

        public static int CurrentManagedThreadId
        {
            get 
            {
                return System.Threading.Thread.CurrentThread.ManagedThreadId;
            }
        }


    }


}
