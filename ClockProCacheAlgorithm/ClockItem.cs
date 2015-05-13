using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockProCacheAlgorithm
{
    /// <summary>
    /// Item of cache
    /// </summary>
    /// <typeparam name="TSavedItem">Type of object which saved in cache</typeparam>
    public class ClockItem<TSavedItem>
    {
        //Reference bit
        public bool Bit { get; set; }
        //Value of saved object
        public TSavedItem Value { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="bit">Reference bit value</param>
        /// <param name="itemValue">Value of saving object</param>
        public ClockItem(bool bit, TSavedItem itemValue)
        {
            Bit = bit;
            Value = itemValue;
        }
    }
}
