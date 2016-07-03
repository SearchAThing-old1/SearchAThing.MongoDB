#region SearchAThing.MongoDB, Copyright(C) 2016 Lorenzo Delana, License under MIT
/*
* The MIT License(MIT)
* Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
*
* Permission is hereby granted, free of charge, to any person obtaining a
* copy of this software and associated documentation files (the "Software"),
* to deal in the Software without restriction, including without limitation
* the rights to use, copy, modify, merge, publish, distribute, sublicense,
* and/or sell copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
* DEALINGS IN THE SOFTWARE.
*/
#endregion

using MongoDB.Driver;
using Repository.Mongo;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using SearchAThing.MongoDB;

namespace SearchAThing
{

    namespace MongoDB
    {

        public class MongoEntityTrackChanges
        {

            #region ChangedProperties [pgps]
            public HashSet<string> ChangedProperties { get; private set; }
            #endregion

            #region NewItems [pgps]
            public HashSet<IMongoEntityTrackChanges> NewItems { get; private set; }
            #endregion

            #region DeletedItems [pgps]
            public Dictionary<ICollection, HashSet<IMongoEntityTrackChanges>> DeletedItems { get; private set; }
            #endregion

            public MongoEntityTrackChanges()
            {
                ChangedProperties = new HashSet<string>();
                NewItems = new HashSet<IMongoEntityTrackChanges>();
                DeletedItems = new Dictionary<ICollection, HashSet<IMongoEntityTrackChanges>>();
            }

            public void Clear()
            {
                ChangedProperties.Clear();
                NewItems.Clear();
                DeletedItems.Clear();
            }

        }

    }

    public static partial class Extensions
    {

        static Type tIMongoEntityTrackChanges = typeof(IMongoEntityTrackChanges);
        static Type tICollection = typeof(ICollection);

        public static void UpdateWithTrack<T>(this T obj, Repository<T> repo) where T : Entity, IMongoEntityTrackChanges
        {
            var updatesAdd = new List<UpdateDefinition<T>>();
            var updatesDelete = new List<UpdateDefinition<T>>();

            var updates = obj.Changes(repo.Updater, updatesAdd, updatesDelete).ToArray();
            repo.Update(obj, updates); // do field updates

            // do object add
            if (updatesAdd.Count > 0) repo.Update(obj, updatesAdd.ToArray());

            // do object del
            if (updatesDelete.Count > 0) repo.Update(obj, updatesDelete.ToArray());

            obj.TrackChanges.Clear();
        }

        /// <summary>
        /// Retrieve list of field updates and clear the status of ChangedProperties.
        /// See MongoConcurrency example ( https://github.com/devel0/SearchAThing.Patterns )
        /// </summary>        
        public static IEnumerable<UpdateDefinition<T>> Changes<T>(this IMongoEntityTrackChanges obj,
            UpdateDefinitionBuilder<T> updater, List<UpdateDefinition<T>> updatesAdd, List<UpdateDefinition<T>> updatesDel, string prefix = "", Type _type = null) where T : Entity
        {
            var type = _type ?? typeof(T);

            if (type.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) yield break;

            Func<string, string> fullname = (x) =>
            {
                if (string.IsNullOrEmpty(prefix))
                    return x;
                else
                    return $"{prefix}.{x}";
            };

            // collect changes
            foreach (var cprop in obj.TrackChanges.ChangedProperties)
            {
                yield return updater.Set(fullname(cprop), type.GetProperty(cprop).GetMethod.Invoke(obj, null));
            }

            HashSet<IMongoEntityTrackChanges> hsDeleted = null;

            // scan properties
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.PropertyType.GetInterface(tICollection.Name) == tICollection) // sweep collection
                {
                    var coll = (ICollection)prop.GetMethod.Invoke(obj, null);
                    var collGenericArguments = prop.PropertyType.GetGenericArguments();
                    if (collGenericArguments.Length != 1) continue; // search for types ICollection<T>

                    var collElementType = collGenericArguments[0];
                    var collMatchType = collElementType.GetInterface(tIMongoEntityTrackChanges.Name) == tIMongoEntityTrackChanges;

                    if (coll != null && collMatchType)
                    {
                        // check deleted items
                        if (obj.TrackChanges.DeletedItems.TryGetValue(coll, out hsDeleted))
                        {
                            foreach (var x in hsDeleted) updatesDel.Add(updater.Pull(fullname(prop.Name), x));
                            obj.TrackChanges.DeletedItems.Remove(coll);
                        }

                        // check modified/added items
                        if (coll.Count > 0)
                        {
                            int idx = 0;

                            // sweep collection elements
                            foreach (var y in coll.Cast<IMongoEntityTrackChanges>())
                            {
                                if (obj.TrackChanges.NewItems.Contains(y))
                                {
                                    updatesAdd.Add(updater.AddToSet(prop.Name, y));
                                }
                                else
                                {
                                    // recurse on property
                                    foreach (var x in Changes(y, updater, updatesAdd, updatesDel, $"{fullname(prop.Name)}.{idx}", collElementType))
                                        yield return x;
                                }

                                ++idx;
                            }
                        }
                    }

                    continue;
                }

                // skip non IMongoEntityTrackChanges propertie
                if (prop.PropertyType.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) continue;

                // recurse on property
                foreach (var x in Changes(prop.GetMethod.Invoke(obj, null) as IMongoEntityTrackChanges, updater,
                    updatesAdd, updatesDel, fullname(prop.Name), prop.PropertyType))
                    yield return x;
            }
        }

        public static void SetAsNew<T>(this ICollection<T> coll, IMongoEntityTrackChanges collectionOwner, T newItem) where T : IMongoEntityTrackChanges
        {
            collectionOwner.TrackChanges.NewItems.Add(newItem);
        }

        public static void SetAsDeleted<T>(this ICollection coll, IMongoEntityTrackChanges collectionOwner, T oldItem) where T : IMongoEntityTrackChanges
        {
            HashSet<IMongoEntityTrackChanges> hs = null;

            if (!collectionOwner.TrackChanges.DeletedItems.TryGetValue(coll, out hs))
            {
                hs = new HashSet<IMongoEntityTrackChanges>();
                collectionOwner.TrackChanges.DeletedItems.Add(coll, hs);
            }
            hs.Add(oldItem);
        }

    }

}
