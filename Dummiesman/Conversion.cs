using System;
namespace MapMod.objLoader
{
    public class Conversion
    {
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> arry)
        {
            Il2CppSystem.Collections.Generic.List<T> list = new Il2CppSystem.Collections.Generic.List<T>();
            for (int i = 0; i < arry.Count; i++)
            {
                list.Add(arry[i]);
            }
            return list;
        }
    }
}
