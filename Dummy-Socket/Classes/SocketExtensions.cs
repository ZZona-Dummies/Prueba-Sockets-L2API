using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeltaSockets
{
    public class MinMax<T>
    {
        public T min, max;

        private MinMax()
        {
        }

        public MinMax(T mn, T mx)
        {
            min = mn;
            max = mx;
        }
    }

    public static class ArrayExtensions
    {
        public static IEnumerable<T> JoinMultipleArray<T>(this IEnumerable<T[]> array)
        {
            foreach (var el in array)
                for (int i = 0; i < el.Length; ++i)
                    yield return el[i];
        }
    }

    public static class SocketExtensions
    {
        public static int GetObjectSize(this object TestObject)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] Array;
                bf.Serialize(ms, TestObject);
                Array = ms.ToArray();
                return Array.Length;
            }
        }

        public static bool FindFirstMissingNumberFromSequence<T>(this IEnumerable<T> arr, out T n, MinMax<T> mnmx = null)
        {
            //Dupe

            if (!arr.GetItemType().IsNumericType())
            {
                Console.WriteLine("Type '{0}' can't be used as a numeric type!", typeof(T).Name);
                n = default(T);
                return false;
            }

            if (mnmx != null)
            {
                arr.Add(mnmx.min);
                arr.Add(mnmx.max);
            }

            IOrderedEnumerable<T> list = arr.OrderBy(x => x);

            //End dupe

            bool b = false;
            n = default(T);

            foreach (T num in list)
            {
                b = (dynamic)num - n > 1;
                if (b) break;
                else n = (dynamic)num;
            }

            n = (dynamic)n + 1;

            return b;
        }

        public static IEnumerable<T> FindMissingNumbersFromSequence<T>(IEnumerable<T> arr, MinMax<T> mnmx = null) where T : struct
        {
            if (!arr.GetItemType().IsNumericType())
            {
                Console.WriteLine("Type '{0}' can't be used as a numeric type!", typeof(T).Name);
                yield break;
            }

            if (mnmx != null)
            {
                arr.Add(mnmx.min);
                arr.Add(mnmx.max);
            }

            IOrderedEnumerable<T> list = arr.OrderBy(x => x);
            T n = default(T);

            foreach (T num in list)
            {
                T op = (dynamic)num - n;
                if ((dynamic)op > 1)
                {
                    int max = op.ConvertValue<int>();
                    for (int l = 1; l < max; ++l)
                        yield return (dynamic)n + l.ConvertValue<T>();
                }
                n = (dynamic)num;
            }
        }

        public static Type GetItemType<T>(this IEnumerable<T> enumerable)
        {
            return typeof(T);
        }

        //This is necessary because we cannot cast (using C# expression (Type)var, or var as Type) => Errors: CS0077, CS0030, CS0413
        public static T ConvertValue<T>(this object o) where T : struct
        {
            return (T)Convert.ChangeType(o, typeof(T));
        }

        public static bool IsNumericType<T>(this T o)
        {
            return typeof(T).IsNumericType();
        }

        public static bool IsNumericType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }

        public static IEnumerable<T> Add<T>(this IEnumerable<T> e, T value)
        {
            foreach (var cur in e)
            {
                yield return cur;
            }
            yield return value;
        }
    }
}