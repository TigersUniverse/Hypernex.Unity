using System;

namespace Hypernex.Tools
{
    // https://forum.unity.com/threads/create-audioclip-from-byte-in-unity.1205572/
    public static class DataConversion
    {
        public static float[] ConvertByteToFloat(byte[] array)
        {
            float[] floatArr = new float[array.Length / 4];
            for (int i = 0; i < floatArr.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(array, i * 4, 4);
                floatArr[i] = BitConverter.ToSingle(array, i*4) / 0x80000000;
            }
            return floatArr;
        }
        
        public static byte[] ConvertFloatToByte(float[] array)
        {
            byte[] byteArr = new byte[array.Length * 4];
            for (int i = 0; i < array.Length; i++)
            {
                var bytes = BitConverter.GetBytes(array[i] * 0x80000000);
                Array.Copy(bytes, 0, byteArr, i * 4, bytes.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(byteArr, i * 4, 4);
            }
            return byteArr;
        }
    }
}