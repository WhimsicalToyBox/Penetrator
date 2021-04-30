using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Penetrator.DataPool
{
    public class MasterData : DataPool
    {
        protected static class Cache<T>
        {
            public static IEnumerable<T> Records { get; set; }
        }

        protected static HashSet<Type> AvailableTypesInner = new HashSet<Type>();

        public List<Type> AvailableTypes()
        {
            return AvailableTypesByStaticCache();
        }

        public static List<Type> AvailableTypesByStaticCache()
        {
            return AvailableTypesInner.ToList();
        }

        public void Clear()
        {
            ClearStaticCache();
        }
        public static void ClearStaticCache()
        {
            var clearCacheMethod = typeof(MasterData).GetType().GetMethod("ClearStaticCache", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var type in AvailableTypesInner)
            {
                if (clearCacheMethod != null)
                {
                    var bindedMethod = clearCacheMethod.MakeGenericMethod(type);
                    bindedMethod.Invoke(null, new object[]{ });
                }
            }
            AvailableTypesInner = new HashSet<Type>();
        }
        private static void ClearStaticCache<T>()
        {
            Cache<T>.Records = null;
        }

        public void Set<T>(IEnumerable<T> records)
        {
            if (records == null)
            {
                return;
            }
            Cache<T>.Records = records;

            var type = typeof(T);
            if (!AvailableTypesInner.Contains(type))
            {
                AvailableTypesInner.Add(type);
            }
    
        }
        public void Set(Type type, IEnumerable<object> records)
        {
            if (records == null)
            {
                return;
            }
            MethodInfo method = GetType().GetMethod("SetWithCast", BindingFlags.Instance |  BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogError("SetWithCast is not get");
            }
            var bindedMethod = method.MakeGenericMethod(new Type[] { type });
            bindedMethod.Invoke(this, new[] { records });
        }
        private void SetWithCast<T>(IEnumerable<object> records)
        {
            var convertedMethod = records.Select(e => (T)e).ToList();
            Set(convertedMethod);
        }

        public IEnumerable<T> Get<T>()
        {
            if (Cache<T>.Records != null)
            {
                return Cache<T>.Records;
            } else
            {
                return new List<T>();
            }
        }

        public IEnumerable<object> Get(Type type)
        {
            var getterMethodBase = typeof(MasterData).GetMethod("GetWithCast", BindingFlags.Instance | BindingFlags.NonPublic);
            var getterMethod = getterMethodBase.MakeGenericMethod(new Type[] { type });

            var rawValue = getterMethod.Invoke(this, new object[] { });

            return rawValue == null ? new List<object>() : rawValue as IEnumerable<object>;
        }

        private IEnumerable<object> GetWithCast<T>()
        {
            IEnumerable<T> records
                = Cache<T>.Records != null
                    ? Get<T>() : new List<T>();

            return records.Select(e => (object) e).ToList();
        }
    }

}
