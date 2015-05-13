using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockProCacheAlgorithm
{
    /// <summary>
    /// Clock metadata item. Represent type of page(hot,cold,test) and key
    /// </summary>
    public class ClockMetadataItem
    {
        /// <summary>
        /// type of page
        /// </summary>
        public PageType Type;
        /// <summary>
        /// page key
        /// </summary>
        public Guid Key;
    }
}
