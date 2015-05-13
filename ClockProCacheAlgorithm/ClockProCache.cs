using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockProCacheAlgorithm
{
    public class ClockProCache<TSavedItem> : ICache<TSavedItem>
    {
        private int _maxSize;
        private int _maxCold;

        private readonly Dictionary<Guid, ClockItem<TSavedItem>> _dataCache = new Dictionary<Guid, ClockItem<TSavedItem>>();

        //cached data dictionary that contains key and item value If the value is null it means that this is entry for a test page.
        public Dictionary<Guid, ClockItem<TSavedItem>> DataCache { get { return _dataCache; } }

        //Dictionary contains page index for maintaining data order in clock, and each entry contains page type and key
        private List<ClockMetadataItem> _metadata = new List<ClockMetadataItem>();

        private int _handPositionHot;
        private int _handPositionCold;
        private int _handPositionTest;

        private int _hotCount;
        private int _coldCount;
        private int _testCount;

        //Get and Set hits and misses of cache. Statistics values
        private int _statGetHits;
        private int _statGetMisses;
        private int _statSetHits;
        private int _statSetMisses;

        //Delegate for read value from database (for misses)
        public delegate TSavedItem ReadItemFromDatabase(Guid key);
        private ReadItemFromDatabase _readFromDb;

        //Delegate fro write value from cache to database(write-back)
        public delegate void WriteItemToDatabase(TSavedItem value);
        private WriteItemToDatabase _writeToDb;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="size">Max size of cache</param>
        public ClockProCache(int size)
        {
            _maxCold = size;
            _maxSize = size;
        }

        //Reset cache values
        private void ClearCacheResetValues()
        {
            _dataCache.Clear();
            _metadata.Clear();

            _handPositionCold = 0;
            _handPositionHot = 0;
            _handPositionTest = 0;

            _statGetHits = 0;
            _statGetMisses = 0;
            _statSetHits = 0;
            _statSetMisses = 0;

            _testCount = 0;
            _hotCount = 0;
            _coldCount = 0;
        }

        /// <summary>
        /// Check is item with this key cached or no
        /// </summary>
        /// <param name="key">key of item</param>
        /// <returns>true if item cached, false if not</returns>
        public bool IsCached(Guid key)
        {
            bool result = false;
            if (_dataCache.Keys.Contains(key))
                if (_dataCache[key] != null)
                    result = true;
            return result;

        }

        /// <summary>
        /// Remove item from cache by key
        /// </summary>
        /// <param name="key">key of item to remove</param>
        /// <returns>true if removing is successfull, false if not</returns>
        public bool RemoveItemFromCache(Guid key)
        {
            try
            {
                ClockMetadataItem meta =  _metadata.Where(c => c.Key == key).First();//find item from metadata
                meta.Type = PageType.Test; //set type of page to test page
                _dataCache[key] = null;//discard item in dataCache
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets item from cache by key
        /// </summary>
        /// <param name="key">key of item</param>
        /// <returns>item from cache by key</returns>
        public TSavedItem GetItem(Guid key)
        {
            ClockItem<TSavedItem> data;
            if (_dataCache.Keys.Contains(key))
            {
                data = _dataCache[key];
                if (data != null)
                {
                    data.Bit = true;
                    _statGetHits++;
                    return data.Value;
                }
                else
                    return _readFromDb(key);
            }
            else
            {
                _statGetMisses++;
                try
                {
                    TSavedItem item = _readFromDb(key);
                    SetItem(key, item);
                    return item;
                }
                catch (Exception)
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        /// <summary>
        /// Insert item to cache
        /// </summary>
        /// <param name="key">key of item</param>
        /// <param name="value">value of item</param>
        public void SetItem(Guid key, TSavedItem value)
        {
            //check if key is in data
            if (_dataCache.Keys.Contains(key))
            {
                //check if key is a key of test page
                if (_dataCache[key] != null)
                {
                    if (_coldCount < _maxSize)//check if coldCount is equal to max size
                        _coldCount++;//increment count of cold pages in cache
                    _dataCache[key] = new ClockItem<TSavedItem>(false, value);//store value and set ref to false
                    DeleteMetadata(new ClockMetadataItem { Key = key, Type = PageType.Test });
                    _testCount--;
                    AddMetadata(new ClockMetadataItem { Type = PageType.Hot, Key = key });
                    _hotCount++;
                    _statSetMisses++;
                }
                else // if key is key of hot or cold page
                {
                    _dataCache[key].Bit = true;
                    _dataCache[key].Value = value;
                    _statSetHits++;
                }
            }
            else // key is not in cache
            {
                try
                {
                    _dataCache.Add(key,new ClockItem<TSavedItem>(false,value));
                     AddMetadata(new ClockMetadataItem { Key = key, Type = PageType.Cold });
                    _coldCount++;
                    _statSetMisses++;
                }
                catch (Exception)
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        private void HandHotAction()
        {
            if (_handPositionHot == _handPositionTest)//check if hot and test hands have the same positions
                HandTestAction();
            ClockMetadataItem meta = _metadata[_handPositionHot]; // get metadata from handPositionHot
            ClockItem<TSavedItem> data;
            if (meta.Type == PageType.Hot)// if page type is hot
            {
                data = _dataCache[meta.Key];// fetch data for key
                if (data.Bit)//if page bit is true(has reference)
                    data.Bit = false; // set false clock algorithm
                else
                {
                    meta.Type = PageType.Cold;//turn this page into cold page
                    data.Bit = false;//set to false bit of item
                    _hotCount--;
                    _coldCount++;
                }
            }
            _handPositionHot++; // move hot hand for one position forward
            if (_handPositionHot >= _metadata.Count)//if hot hand do full circle update hot hand position to start position
                _handPositionHot = 0;
        }

        private void HandTestAction()
        {
            if (_handPositionTest == _handPositionCold)//check if test and cold hands have the same positions
                HandColdAction();
            ClockMetadataItem meta = _metadata[_handPositionTest]; // get metadata from handPositionTest
            if (meta.Type == PageType.Test) // if  current page is test page
            {
                _dataCache.Remove(meta.Key); // remove item from dataCache
                DeleteMetadata(meta); //remove metadata
                _testCount--;
                if (_maxCold > 1) // if can decrease cold max count
                    _maxCold--;
            }
            _handPositionTest++; // move test hand for one position forward
            if (_handPositionTest >= _metadata.Count)//if test hand do full circle update test hand position to start position
                _handPositionTest = 0;
        }

        private void HandColdAction()
        {
            ClockMetadataItem meta = _metadata[_handPositionCold];// get metadata information by handPositionCold
            ClockItem<TSavedItem> data;
            if (meta.Type == PageType.Cold)// if metadata page type is cold page
            {
                data = _dataCache[meta.Key]; // get dataCache item
                if (data.Bit) // if item is referenced
                {
                    meta.Type = PageType.Hot; // change type of page to hot page
                    data.Bit = false;
                    _hotCount++;
                    _coldCount--;
                }
                else // if item is not referenced
                {
                    //TODO
                    //SynchronizeWithServer() update object, which removed from cache, on server side
                   // _writeToDb(data.Value);
                    meta.Type = PageType.Test; //change page type to test page
                    _dataCache[meta.Key] = null; // discard data in cache
                    _coldCount--;
                    _testCount++;
                    while (_maxSize < _testCount) // if test page count is so big delete test pages by test hand action
                        HandTestAction();
                }
                _handPositionCold++; // move cold hand for one position forward
                if (_handPositionCold >= _metadata.Count)//if cold hand do full circle update cold hand position to start position
                    _handPositionCold = 0;
                while (_maxSize - _maxCold < _hotCount) // actions on hot pages (remove, set false flag atc.)
                    HandHotAction();
            }
        }

        //Evict items from cache if required
        private void EvictItems()
        {
            while (_maxSize <= _coldCount + _hotCount)//evict items from cache by call HandColdAction
                HandColdAction();
        }

        //add metadata after hand hot, evict data if it required, update hands
        private void AddMetadata(ClockMetadataItem meta)
        {
            EvictItems();
            _metadata.Insert(_handPositionHot, meta);
            int maxPosition = _metadata.Count;
            if (_handPositionCold > _handPositionHot) // if true we update position of cold hand because we add item after hot and position of cold hand increment for one position
            {
                _handPositionCold++;
                if (_handPositionCold > maxPosition)
                    _handPositionCold = 0;
            }
            if (_handPositionTest > _handPositionHot) // if true we update position of test hand because we add item after hot and position of test hand increment for one position
            {
                _handPositionTest++;
                if (_handPositionTest > maxPosition)
                    _handPositionTest = 0;
            }
            _handPositionHot++; // increment hot hand position for one point because we add new item before on handHotPosition
            if (_handPositionHot > maxPosition)
                _handPositionHot = 0;
        }

        //delete metadata,update hands
        private void DeleteMetadata(ClockMetadataItem meta)
        {
            int metaIndex = _metadata.IndexOf(meta);
            _metadata.RemoveAt(metaIndex);
            int maxPosition = _metadata.Count - 1;
            if (_handPositionHot >= metaIndex)// if true we update position of hot hand because we remove item before hot and position of hot hand decrement for one position
            {
                _handPositionHot--;
                if (_handPositionHot < 0)
                    _handPositionHot = maxPosition;
            }
            if (_handPositionCold >= metaIndex) // if true we update position of cold hand because we remove item before cold and position of cold hand decrement for one position
            {
                _handPositionCold--;
                if (_handPositionCold < 0)
                    _handPositionCold = maxPosition;
            }
            if (_handPositionTest >= metaIndex) // if true we update position of test hand because we remove item before test and position of test hand decrement for one position
            {
                _handPositionTest--;
                if (_handPositionTest < 0)
                    _handPositionTest = maxPosition;
            }
        }

        /// <summary>
        /// Set function for reading information from storage(database etc.)
        /// </summary>
        /// <param name="function">function of delefate type ReadItemFromDatabase</param>
        public void SetReadFunction(ReadItemFromDatabase function)
        {
            _readFromDb = function;
        }

        /// <summary>
        /// Set function for writing information to storage(database etc.)
        /// </summary>
        /// <param name="function">function of delefate type WriteItemToDatabase</param>
        public void SetWriteFunction(WriteItemToDatabase function)
        {
            _writeToDb = function;
        }
    }
}
