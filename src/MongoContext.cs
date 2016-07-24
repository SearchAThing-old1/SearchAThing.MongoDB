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
using MongoDB.Driver.Linq;
using SearchAThing.MongoDB;
using System;
using System.Collections.Generic;

namespace SearchAThing
{

    namespace MongoDB
    {

        public class MongoContext
        {

            #region ConnectionString [pgps]
            public string ConnectionString { get; private set; }
            #endregion

            public bool Debug { get; private set; }

            public MongoClient MongoClient { get; private set; }
            public string DbName { get; private set; }
            public IMongoDatabase Database { get; private set; }

            public MongoContext(string connectionString, bool debug = false)
            {
                ConnectionString = connectionString;
                Debug = debug;
                MongoClient = new MongoClient(ConnectionString);
                DbName = new MongoUrl(ConnectionString).DatabaseName;
                Database = MongoClient.GetDatabase(DbName);
            }

            #region collection factory
            Dictionary<Type, IGenericMongoRepository> repositoryFactory = new Dictionary<Type, IGenericMongoRepository>();
            object repositoryFactoryLck = new object();

            public ITypedMongoRepository<T> GetRepository<T>() where T : MongoEntity
            {
                var type = typeof(T);

                ITypedMongoRepository<T> typedRepoObj = null;
                IGenericMongoRepository repoObj = null;

                lock (repositoryFactoryLck)
                {
                    if (!repositoryFactory.TryGetValue(type, out repoObj))
                    {
                        typedRepoObj = new MongoRepository<T>(Database.GetCollection<T>(type.Name.ToLowerInvariant()));
                        repositoryFactory.Add(type, typedRepoObj);
                    }
                    else
                        typedRepoObj = (ITypedMongoRepository<T>)repoObj;
                }

                return typedRepoObj;
            }
            #endregion                        

            List<AttachedMongoEntity> attachedEntities = new List<AttachedMongoEntity>();

            public T Attach<T>(T ent, MongoEntityState state = MongoEntityState.Undefined) where T : MongoEntity
            {
                if (ent.State != MongoEntityState.Detached)
                    throw new Exception($"context already assigned to this mongo entity");

                ent.MongoContext = this;
                ent.State = state;

                attachedEntities.Add(new AttachedMongoEntity(ent));

                return ent;
            }

            public void Delete<T>(T x) where T : MongoEntity
            {
                if (x.State == MongoEntityState.Detached) Attach(x, MongoEntityState.Deleted);
            }

            /// <summary>
            /// Creates a new MongoEntity and attach as New to this context
            /// </summary>            
            public T New<T>() where T : MongoEntity, new()
            {
                var ensureRepo = GetRepository<T>();

                var ent = new T();

                Attach(ent, MongoEntityState.New);

                return ent;
            }

            /// <summary>
            /// Creates a new MongoEntity and attach as New to this context
            /// </summary>            
            public T New<T>(T pre) where T : MongoEntity//, new()
            {
                var ensureRepo = GetRepository<T>();

                var ent = pre;

                Attach(ent, MongoEntityState.New);

                return ent;
            }

            public void Save()
            {
                foreach (var aent in attachedEntities)
                {
                    var repo = repositoryFactory[aent.Entity.GetType()];

                    switch (aent.Entity.State)
                    {
                        case MongoEntityState.New:
                            {
                                aent.Entity.BeforeSaveAct(); // manage forward of event and call overridable OnBeforeSvae                                                                
                                repo.GenericInsert(this, aent.Entity);
                                aent.Entity.State = MongoEntityState.Undefined;
                                aent.ResetOrigEntity();
                                aent.Entity.AfterSaveAct(); // manage forward of event and call overridable OnBeforeSvae                                
                            }
                            break;

                        case MongoEntityState.Undefined:
                            {
                                aent.Entity.BeforeSaveAct(); // manage forward of event and call overridable OnBeforeSvae                                
                                repo.GenericUpdate(this, aent.Entity, aent.OrigEntity);
                                aent.ResetOrigEntity();
                                aent.Entity.AfterSaveAct(); // manage forward of event and call overridable OnBeforeSvae                                
                            }
                            break;

                        case MongoEntityState.Deleted:
                            {
                                repo.GenericDelete(this, aent.Entity);
                            }
                            break;
                    }
                }
            }

        }

    }

    public static partial class Extensions
    {

        public static IEnumerable<T> Attach<T>(this IMongoQueryable<T> q, MongoContext ctx) where T : MongoEntity
        {            
            foreach (var x in q) yield return ctx.Attach<T>(x);            
        }

    }


}
