using System;
using System.Collections.Generic;

namespace Penetrator.DataPool
{
    public interface MasterBuilderConfig
    {
        public List<Type> MasterTables();
    }
}
