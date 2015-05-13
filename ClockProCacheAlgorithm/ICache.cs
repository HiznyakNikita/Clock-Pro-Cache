using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockProCacheAlgorithm
{
    /// <summary>
    /// Cache interface
    /// </summary>
    /// <typeparam name="TSavedItem">Type of object which saving in cache</typeparam>
    public interface ICache<TSavedItem>
    {
        bool IsCached(Guid key);
        void SetItem(Guid key, TSavedItem value);
        TSavedItem GetItem(Guid key);
        bool RemoveItemFromCache(Guid key);

    }
}
