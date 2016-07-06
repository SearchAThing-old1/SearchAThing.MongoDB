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

            public MongoRepository(string connectionString) : base(connectionString)
            {
            }

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

                // do field updates
                if (updatesChanges.Count > 0) Update((T)ent, updatesChanges.ToArray());

                // do collection add
                if (updatesAdd.Count > 0) Update((T)ent, updatesAdd.ToArray());

                // do collection del
                if (updatesDelete.Count > 0) Update((T)ent, updatesDelete.ToArray());
            }

            /// <summary>
            /// Remove a generic MongoEntity which is in Deleted state
            /// </summary>
            /// <param name="ent"></param>
            public void GenericDelete(MongoEntity ent)
            {
                Delete((T)ent);
            }

        }

    }

}
