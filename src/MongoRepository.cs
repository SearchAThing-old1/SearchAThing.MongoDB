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
using System.Collections.Generic;
using System.Linq;
using SearchAThing.Core;
using System.Diagnostics;

namespace SearchAThing
{

    namespace MongoDB
    {

        /// <summary>
        /// [Abstraction Layer]
        /// Adds some internal action to overcome the templated repository arguments
        /// </summary>        
        public class MongoRepository<T> : ITypedMongoRepository<T> where T : MongoEntity
        {

            public IMongoCollection<T> Collection { get; private set; }

            public MongoRepository(IMongoCollection<T> collection)
            {
                Collection = collection;
            }

            /// <summary>
            /// Insert a generic MongoEntity which is in New state
            /// </summary>            
            public void GenericInsert(MongoContext ctx, MongoEntity ent)
            {
                if (ctx.Debug) Debug.WriteLine($"Insert ent type={ent.GetType()} id={ent.Id}");

                Collection.InsertOne((T)ent);
            }

            /// <summary>
            /// Updates a generic MongoEntity which is in Modified state
            /// </summary>            
            public void GenericUpdate(MongoContext ctx, MongoEntity ent, MongoEntity origEnt)
            {
                var updatesChanges = new List<UpdateDefinition<T>>();
                var updatesCollAdd = new List<UpdateDefinition<T>>();
                var updatesCollDel = new List<UpdateDefinition<T>>();

                var diffs = origEnt.Compare(ent).ToList();

                foreach (var diff in diffs)
                {
                    if (diff.NewPropertyValue == null && diff.OldPropertyValue == null)
                    {
                        foreach (var x in diff.CollectionElementsToAdd)
                        {
                            updatesCollAdd.Add(Builders<T>.Update.Push(diff.PropertyFullPath, x));
                        }

                        foreach (var x in diff.CollectionElementsToRemove)
                        {
                            updatesCollDel.Add(Builders<T>.Update.Pull(diff.PropertyFullPath, x));
                        }
                    }
                    else
                    {
                        updatesChanges.Add(Builders<T>.Update.Set(diff.PropertyFullPath, diff.NewPropertyValue));
                    }
                }

                // do field updates
                if (updatesChanges.Count > 0)
                {
                    if (ctx.Debug)
                    {
                        Debug.WriteLine($"Record Updates {updatesChanges.Count}");
                        foreach (var x in updatesChanges)
                        {
                            Debug.WriteLine($"\t{x.ToString()}");
                        }
                    }                    

                    var filter = Builders<T>.Filter.Eq((t) => t.Id, ent.Id);
                    var update = Builders<T>.Update.Combine(updatesChanges);
                    Collection.UpdateMany(filter, update);
                }
               
                // do collection add
                if (updatesCollAdd.Count > 0)
                {
                    if (ctx.Debug) Debug.WriteLine($"Coll Add {updatesCollAdd.Count}");
                    foreach (var x in updatesCollAdd)
                    {
                        if (ctx.Debug) Debug.WriteLine($"\t{x.ToString()}");

                        var filter = Builders<T>.Filter.Eq((y) => y.Id, ent.Id);
                        Collection.UpdateOne(filter, x);
                    }
                }

                // do collection del
                if (updatesCollDel.Count > 0)
                {
                    if (ctx.Debug) Debug.WriteLine($"Coll Del {updatesCollDel.Count}");

                    foreach (var x in updatesCollDel)
                    {
                        if (ctx.Debug) Debug.WriteLine($"\t{x.ToString()}");

                        var filter = Builders<T>.Filter.Eq((y) => y.Id, ent.Id);
                        Collection.UpdateOne(filter, x);
                    }
                }
            }

            /// <summary>
            /// Remove a generic MongoEntity which is in Deleted state
            /// </summary>
            /// <param name="ent"></param>
            public void GenericDelete(MongoContext ctx, MongoEntity ent)
            {
                if (ctx.Debug) Debug.WriteLine($"Delete ent type={ent.GetType()} id={ent.Id}");

                var filter = Builders<T>.Filter.Eq((x) => x.Id, ent.Id);
                Collection.DeleteOne(filter);
            }

        }

    }

}
