using System;
using System.Collections.Generic;

namespace Penetrator.DataPool
{
    public interface DataPool
    {
        public IEnumerable<T> Get<T>();
        public IEnumerable<object> Get(Type type);

        public void Set<T>(IEnumerable<T> records);
        public void Set(Type type, IEnumerable<object> records);

        public List<Type> AvailableTypes();
        public void Clear();
    }
}
