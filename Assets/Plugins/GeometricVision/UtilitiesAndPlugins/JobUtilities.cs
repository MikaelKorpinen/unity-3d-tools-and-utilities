using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Plugins.GeometricVision.Utilities
{
    public static class JobUtilities
    {
        public static unsafe NativeArray<T> CopyDatafromArrayToNative<T>(T[] array, int offset, int count, Allocator allocator) where T : unmanaged
        {
            var dst = new NativeArray<T>(count, allocator);
            fixed (T * srcPtr = array)
            {
                var dstPtr = dst.GetUnsafePtr();
                UnsafeUtility.MemCpy(dstPtr,srcPtr + offset, sizeof(T) * count);
            }
            return dst;
        }        
        public static unsafe NativeArray<T> CopyDataFromNativeToRegular<T>(T[] array, int offset, int count, Allocator allocator) where T : unmanaged
        {
            var dst = new NativeArray<T>(count, allocator);
            fixed (T * srcPtr = array)
            {
                var dstPtr = dst.GetUnsafePtr();
                UnsafeUtility.MemCpy(dstPtr,srcPtr + offset, sizeof(T) * count);
            }
            return dst;
        }
    }
}