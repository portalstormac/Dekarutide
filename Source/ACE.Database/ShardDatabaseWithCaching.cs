using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.Entity.Enum.Properties;

namespace ACE.Database
{
    public class ShardDatabaseWithCaching : ShardDatabase
    {
        public TimeSpan PlayerBiotaRetentionTime { get; set; }
        public TimeSpan NonPlayerBiotaRetentionTime { get; set; }

        public ShardDatabaseWithCaching(TimeSpan playerBiotaRetentionTime, TimeSpan nonPlayerBiotaRetentionTime)
        {
            PlayerBiotaRetentionTime = playerBiotaRetentionTime;
            NonPlayerBiotaRetentionTime = nonPlayerBiotaRetentionTime;
        }


        private class CacheObject<T>
        {
            public DateTime LastSeen;
            public ShardDbContext Context;
            public T CachedObject;
        }

        private readonly object biotaCacheMutex = new object();

        private readonly Dictionary<uint, CacheObject<Biota>> biotaCache = new Dictionary<uint, CacheObject<Biota>>();

        private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromMinutes(1);

        private DateTime lastMaintenanceInterval;

        /// <summary>
        /// Make sure this is called from within a lock(biotaCacheMutex)
        /// </summary>
        private void TryPerformMaintenance()
        {
            if (lastMaintenanceInterval + MaintenanceInterval > DateTime.UtcNow)
                return;

            var removals = new Collection<uint>();

            foreach (var kvp in biotaCache)
            {
                if (ObjectGuid.IsPlayer(kvp.Key))
                {
                    if (kvp.Value.LastSeen + PlayerBiotaRetentionTime < DateTime.UtcNow)
                        removals.Add(kvp.Key);
                }
                else
                {
                    if (kvp.Value.LastSeen + NonPlayerBiotaRetentionTime < DateTime.UtcNow)
                        removals.Add(kvp.Key);
                }
            }

            foreach (var removal in removals)
                biotaCache.Remove(removal);

            lastMaintenanceInterval = DateTime.UtcNow;
        }

        private void TryAddToCache(ShardDbContext context, Biota biota)
        {
            lock (biotaCacheMutex)
            {
                if (ObjectGuid.IsPlayer(biota.Id))
                {
                    if (PlayerBiotaRetentionTime > TimeSpan.Zero)
                        biotaCache[biota.Id] = new CacheObject<Biota> {LastSeen = DateTime.UtcNow, Context = context, CachedObject = biota};
                }
                else if (NonPlayerBiotaRetentionTime > TimeSpan.Zero)
                    biotaCache[biota.Id] = new CacheObject<Biota> {LastSeen = DateTime.UtcNow, Context = context, CachedObject = biota};
            }
        }

        public List<uint> GetBiotaCacheKeys()
        {
            lock (biotaCacheMutex)
                return biotaCache.Keys.ToList();
        }


        public override Biota GetBiota(ShardDbContext context, uint id, bool doNotAddToCache = false)
        {
            lock (biotaCacheMutex)
            {
                TryPerformMaintenance();

                if (biotaCache.TryGetValue(id, out var cachedBiota))
                {
                    cachedBiota.LastSeen = DateTime.UtcNow;

                    return cachedBiota.CachedObject;
                }
            }

            var biota = base.GetBiota(context, id);

            if (biota != null && !doNotAddToCache)
                TryAddToCache(context, biota);

            return biota;
        }

        public override Biota GetBiota(uint id, bool doNotAddToCache = false)
        {
            if (ObjectGuid.IsPlayer(id))
            {
                if (PlayerBiotaRetentionTime > TimeSpan.Zero)
                {
                    var context = new ShardDbContext();

                    var biota = GetBiota(context, id, doNotAddToCache); // This will add the result into the caches

                    return biota;
                }
            }
            else if (NonPlayerBiotaRetentionTime > TimeSpan.Zero)
            {
                var context = new ShardDbContext();

                var biota = GetBiota(context, id, doNotAddToCache); // This will add the result into the caches

                return biota;
            }

            return base.GetBiota(id, doNotAddToCache);
        }

        public override bool SaveBiota(ACE.Entity.Models.Biota biota, ReaderWriterLockSlim rwLock)
        {
            if (biota.IsPartiallyPersistant)
            {
                ACE.Entity.Models.Biota partialBiota = new ACE.Entity.Models.Biota();
                partialBiota.Id = biota.Id;
                partialBiota.WeenieClassId = biota.WeenieClassId;
                partialBiota.WeenieType = biota.WeenieType;
                partialBiota.PartialPersitanceFilter = biota.PartialPersitanceFilter;

                foreach (var entry in biota.PartialPersitanceFilter)
                {
                    if (entry.PropertyType == PropertyType.PropertyBool)
                    {
                        if (biota.PropertiesBool.TryGetValue((PropertyBool)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesBool == null)
                                partialBiota.PropertiesBool = new Dictionary<PropertyBool, bool>();
                            partialBiota.PropertiesBool.Add((PropertyBool)entry.Property, entryValue);
                        }
                    }                    
                    else if (entry.PropertyType == PropertyType.PropertyDataId)
                    {
                        if (biota.PropertiesDID.TryGetValue((PropertyDataId)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesDID == null)
                                partialBiota.PropertiesDID = new Dictionary<PropertyDataId, uint>();
                            partialBiota.PropertiesDID.Add((PropertyDataId)entry.Property, entryValue);
                        }
                    }
                    else if (entry.PropertyType == PropertyType.PropertyFloat)
                    {
                        if (biota.PropertiesFloat.TryGetValue((PropertyFloat)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesFloat == null)
                                partialBiota.PropertiesFloat = new Dictionary<PropertyFloat, double>();
                            partialBiota.PropertiesFloat.Add((PropertyFloat)entry.Property, entryValue);
                        }
                    }
                    else if (entry.PropertyType == PropertyType.PropertyInstanceId)
                    {
                        if (biota.PropertiesIID.TryGetValue((PropertyInstanceId)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesIID == null)
                                partialBiota.PropertiesIID = new Dictionary<PropertyInstanceId, uint>();
                            partialBiota.PropertiesIID.Add((PropertyInstanceId)entry.Property, entryValue);
                        }
                    }
                    else if (entry.PropertyType == PropertyType.PropertyInt)
                    {
                        if (biota.PropertiesInt.TryGetValue((PropertyInt)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesInt == null)
                                partialBiota.PropertiesInt = new Dictionary<PropertyInt, int>();
                            partialBiota.PropertiesInt.Add((PropertyInt)entry.Property, entryValue);
                        }
                    }
                    else if (entry.PropertyType == PropertyType.PropertyInt64)
                    {
                        if (biota.PropertiesInt64.TryGetValue((PropertyInt64)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesInt64 == null)
                                partialBiota.PropertiesInt64 = new Dictionary<PropertyInt64, long>();
                            partialBiota.PropertiesInt64.Add((PropertyInt64)entry.Property, entryValue);
                        }
                    }
                    else if (entry.PropertyType == PropertyType.PropertyString)
                    {
                        if (biota.PropertiesString.TryGetValue((PropertyString)entry.Property, out var entryValue))
                        {
                            if (partialBiota.PropertiesString == null)
                                partialBiota.PropertiesString = new Dictionary<PropertyString, string>();
                            partialBiota.PropertiesString.Add((PropertyString)entry.Property, entryValue);
                        }
                    }
                    // Todo: Add partial collection support for the following properties if we ever need them:
                    // PropertiesPosition
                    // PropertiesSpellBook
                    // PropertiesAnimPart
                    // PropertiesPalette
                    // PropertiesTextureMap
                    // PropertiesCreateList
                    // PropertiesEmote
                    // PropertiesEventFilter
                    // PropertiesGenerator
                    // PropertiesAttribute
                    // PropertiesAttribute2nd
                    // PropertiesBodyPart
                    // PropertiesSkill
                    // PropertiesBook
                    // PropertiesBookPageData
                    // PropertiesAllegiance
                    // PropertiesEnchantmentRegistry
                    // HousePermissions
                }

                biota = partialBiota;
            }

            CacheObject<Biota> cachedBiota;

            lock (biotaCacheMutex)
                biotaCache.TryGetValue(biota.Id, out cachedBiota);

            if (cachedBiota != null)
            {
                cachedBiota.LastSeen = DateTime.UtcNow;

                rwLock.EnterReadLock();
                try
                {
                    ACE.Database.Adapter.BiotaUpdater.UpdateDatabaseBiota(cachedBiota.Context, biota, cachedBiota.CachedObject);
                }
                finally
                {
                    rwLock.ExitReadLock();
                }

                return DoSaveBiota(cachedBiota.Context, cachedBiota.CachedObject);
            }

            // Biota does not exist in the cache

            var context = new ShardDbContext();

            var existingBiota = base.GetBiota(context, biota.Id);

            rwLock.EnterReadLock();
            try
            {
                if (existingBiota == null)
                {
                    existingBiota = ACE.Database.Adapter.BiotaConverter.ConvertFromEntityBiota(biota);

                    context.Biota.Add(existingBiota);
                }
                else
                {
                    ACE.Database.Adapter.BiotaUpdater.UpdateDatabaseBiota(context, biota, existingBiota);
                }
            }
            finally
            {
                rwLock.ExitReadLock();
            }

            if (DoSaveBiota(context, existingBiota))
            {
                TryAddToCache(context, existingBiota);

                return true;
            }

            return false;
        }

        public override bool RemoveBiota(uint id)
        {
            lock (biotaCacheMutex)
                biotaCache.Remove(id);

            return base.RemoveBiota(id);
        }
    }
}
