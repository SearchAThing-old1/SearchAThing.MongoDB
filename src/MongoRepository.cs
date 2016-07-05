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
using SearchAThing.MongoDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SearchAThing.Core;

namespace SearchAThing
{

    namespace MongoDB
    {

        /// <summary>S
        /// [Abstraction Layer]
        /// Adds some internal action to overcome the templated repository arguments
        /// </summary>        
        public class MongoRepository<T> : Repository<T>, IGenericMongoRepository where T : MongoEntity
        {

            /// <summary>
            /// Insert a generic MongoEntity which is in New state
            /// </summary>            
            public void GenericInsert(MongoEntity ent)
            {
                Insert((T)ent);
            }

            /// <summary>
            /// Updates a generic MongoEntity which is in Modified state
            /// </summary>            
            public void GenericUpdate(MongoContext ctx, MongoEntity ent, MongoEntity origEnt)
            {
                var updatesChanges = new List<UpdateDefinition<T>>();
                var updatesAdd = new List<UpdateDefinition<T>>();
                var updatesDelete = new List<UpdateDefinition<T>>();

                var diffs = origEnt.Compare(ent).ToList();

                foreach (var diff in diffs)
                {
                    if (diff.NewPropertyValue == null && diff.OldPropertyValue == null)
                    {
                        foreach (var x in diff.CollectionElementsToAdd)
                            updatesAdd.Add(Updater.AddToSet(diff.PropertyFullPath, x));

                        foreach (var x in diff.CollectionElementsToRemove)
                            updatesDelete.Add(Updater.Pull(diff.PropertyFullPath, x));
                    }
                    else
                        updatesChanges.Add(Updater.Set(diff.PropertyFullPath, diff.NewPropertyValue));
                }

                //var updates = Changes(ctx, ent, origEnt, Updater, updatesAdd, updatesDelete).ToArray();

                if (updatesChanges.Count > 0) Update((T)ent, updatesChanges.ToArray()); // do field updates
                if (updatesAdd.Count > 0) Update((T)ent, updatesAdd.ToArray()); // do collection add
                if (updatesDelete.Count > 0) Update((T)ent, updatesDelete.ToArray()); // do collection del

                /*
                // do object add
                if (updatesAdd.Count > 0) repo.Update(obj, updatesAdd.ToArray());

                // do object del
                if (updatesDelete.Count > 0) repo.Update(obj, updatesDelete.ToArray());
                */
            }

            static Type tINotifyPropertyChanged = typeof(INotifyPropertyChanged);
            //static Type tIMongoEntityTrackChanges = typeof(IMongoEntityTrackChanges);
            static Type tICollection = typeof(ICollection);

            /// <summary>
            /// Retrieve list of field updates and clear the status of ChangedProperties.
            /// See MongoConcurrency example ( https://github.com/devel0/SearchAThing.Patterns )
            /// </summary>        
            static IEnumerable<UpdateDefinition<T>> Changes(MongoContext ctx, object obj, object objOrig,
                UpdateDefinitionBuilder<T> updater, List<UpdateDefinition<T>> updatesAdd, List<UpdateDefinition<T>> updatesDel,
                string prefix = "", Type _type = null)
            {
                var type = _type ?? typeof(T);

                //if (type.GetInterface(tINotifyPropertyChanged.Name) != tINotifyPropertyChanged) yield break;
                //if (type.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) yield break;

                Func<string, string> fullname = (x) =>
                {
                    if (string.IsNullOrEmpty(prefix))
                        return x;
                    else
                        return $"{prefix}.{x}";
                };

                //yield return updater.Set(fullname(cprop), type.GetProperty(cprop).GetMethod.Invoke(obj, null));

                // scan properties
                var props = type.GetProperties();

                foreach (var prop in props)
                {

                    var o = prop;
                    //if (prop.PropertyType.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) continue;
                    //if (prop.PropertyType.GetInterface(tINotifyPropertyChanged.Name) != tINotifyPropertyChanged) continue;

                    // recurse on property
                    /*foreach (var x in Changes(ctx, prop.GetMethod.Invoke(obj, null), updater,
                        updatesAdd, updatesDel, fullname(prop.Name), prop.PropertyType))
                        yield return x;*/
                }

                yield break;
            }

            public MongoRepository(string connectionString) : base(connectionString)
            {
            }

        }

    }

}
