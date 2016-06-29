#region SearchAThing.Core, Copyright(C) 2015-2016 Lorenzo Delana, License under MIT
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
using SearchAThing.Patterns.MongoDBWpf.Ents;
using System.Collections;

namespace SearchAThing
{

    public static partial class Extensions
    {

        static Type tIMongoEntityTrackChanges = typeof(IMongoEntityTrackChanges);
        static Type tICollection = typeof(ICollection);

        /// <summary>
        /// Retrieve list of field updates and clear the status of ChangedProperties.
        /// See MongoConcurrency example ( https://github.com/devel0/SearchAThing.Patterns )
        /// </summary>        
        public static IEnumerable<UpdateDefinition<T>> Changes<T>(this IMongoEntityTrackChanges obj, UpdateDefinitionBuilder<T> updater) where T : Entity
        {
            var type = typeof(T);

            if (type.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) yield break;

            // collect changes
            foreach (var cprop in obj.ChangedProperties)
            {
                yield return updater.Set(cprop, type.GetProperty(cprop).GetMethod.Invoke(obj, null));
            }

            obj.ChangedProperties.Clear();

            // scan properties
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.PropertyType.GetInterface(tICollection.Name) == tICollection) // sweep collection
                {
                    var coll = (ICollection)prop.GetMethod.Invoke(obj, null);

                    if (coll != null && coll.Count > 0)
                    {
                        var en = coll.GetEnumerator();
                        en.MoveNext();

                        var collElementType = en.Current.GetType();

                        // check if the first collection object is compatible with IMongoEntityTrackChanges
                        if (collElementType.GetInterface(tIMongoEntityTrackChanges.Name) == tIMongoEntityTrackChanges)
                        {
                            int idx = 0;

                            // sweep collection elements
                            foreach (var y in coll)
                            {
                                // recurse on property
                                foreach (var x in ChangesRecurse((IMongoEntityTrackChanges)y, updater, $"{prop.Name}.{idx}", collElementType))
                                    yield return x;

                                ++idx;
                            }
                        }
                    }

                    continue;
                }

                // skip non IMongoEntityTrackChanges propertie
                if (prop.PropertyType.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) continue;

                // recurse on property
                foreach (var x in ChangesRecurse(prop.GetMethod.Invoke(obj, null) as IMongoEntityTrackChanges, updater, prop.Name, prop.PropertyType))
                    yield return x;
            }
        }

        static IEnumerable<UpdateDefinition<T>> ChangesRecurse<T>(this IMongoEntityTrackChanges obj,
            UpdateDefinitionBuilder<T> updater, string prefix, Type type) where T : Entity
        {
            // collect changes
            foreach (var cprop in obj.ChangedProperties)
            {
                var fullname = $"{prefix}.{cprop}";

                yield return updater.Set(fullname, type.GetProperty(cprop).GetMethod.Invoke(obj, null));
            }

            obj.ChangedProperties.Clear();

            // scan properties
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.PropertyType.GetInterface(tIMongoEntityTrackChanges.Name) != tIMongoEntityTrackChanges) continue;

                foreach (var x in ChangesRecurse(prop.GetMethod.Invoke(obj, null) as IMongoEntityTrackChanges, updater, $"{prefix}.{prop.Name}", prop.PropertyType))
                    yield return x;
            }
        }

    }

}
