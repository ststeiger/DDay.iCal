
namespace System
{
    internal class SR
    {
        // Strings.resx
        public static string ArgumentException_ValueTupleIncorrectType = "Argument must be of type {0}.";
        public static string ArgumentException_ValueTupleLastArgumentNotAValueTuple = "The last element of an eight element ValueTuple must be a ValueTuple.";


        public static string LockRecursionException_RecursiveReadNotAllowed = "Recursive read lock acquisitions not allowed in this mode.";
        public static string LockRecursionException_RecursiveWriteNotAllowed = "Recursive write lock acquisitions not allowed in this mode.";

        public static string LockRecursionException_ReadAfterWriteNotAllowed = "A read lock may not be acquired with the write lock held in this mode.";
        public static string LockRecursionException_WriteAfterReadNotAllowed = "Write lock may not be acquired with read lock held. This pattern is prone to deadlocks. Please ensure that read locks are released before taking a write lock. If an upgrade is necessary, use an upgrade lock in place of the read lock.";

        
        public static string LockRecursionException_UpgradeAfterReadNotAllowed = "Upgradeable lock may not be acquired with read lock held.";
        public static string LockRecursionException_UpgradeAfterWriteNotAllowed = "Upgradeable lock may not be acquired with write lock held in this mode. Acquiring Upgradeable lock gives the ability to read along with an option to upgrade to a writer.";
        public static string LockRecursionException_RecursiveUpgradeNotAllowed = "Recursive upgradeable lock acquisitions not allowed in this mode.";

        public static string SynchronizationLockException_MisMatchedRead = "The read lock is being released without being held.";
        public static string SynchronizationLockException_MisMatchedWrite = "The write lock is being released without being held.";
        public static string SynchronizationLockException_MisMatchedUpgrade = "The upgradeable lock is being released without being held.";

        public static string SynchronizationLockException_IncorrectDispose = "The lock is being disposed while still being used. It either is being held by a thread and/or has active waiters waiting to acquire the lock.";



        public static string GetString(string s)
        {
            return s;
        }
        

        public static string Format(string resourceFormat, params object[] args)
        {
            if (resourceFormat != null && args != null)
            {
                return string.Format(resourceFormat, args);
            }

            if (resourceFormat == null)
                return "";

            return resourceFormat;
        }


    }
}
