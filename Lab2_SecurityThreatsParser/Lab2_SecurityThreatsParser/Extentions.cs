using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
namespace Lab2_SecurityThreatsParser
{
    public static class Extentions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length) //Safe for wrong index and size
        {

            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

    }
}
