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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SearchAThing
{

    namespace MongoDB
    {

        public class MongoContext
        {

            #region ConnectionString [pgps]
            public string ConnectionString { get; private set; }
            #endregion

            public MongoContext(string connectionString)
            {
                ConnectionString = connectionString;
            }

            #region repository factory
            Dictionary<Type, IGenericMongoRepository> repositoryFactory = new Dictionary<Type, IGenericMongoRepository>();

            MongoRepository<T> GetRepository<T>() where T : MongoEntity
            {
                var type = typeof(T);

                IGenericMongoRepository repoObj = null;

                if (!repositoryFactory.TryGetValue(type, out repoObj))
                {
                    var repo = new MongoRepository<T>(ConnectionString);
                    repositoryFactory.Add(type, repo);
                    repoObj = repo;
                }

                return (MongoRepository<T>)repoObj;
            }
            #endregion

            List<AttachedMongoEntity> attachedEntities = new List<AttachedMongoEntity>();

            void Attach<T>(T ent, MongoEntityState state) where T : MongoEntity
            {
                if (ent.State != MongoEntityState.Detached)
                    throw new Exception($"context already assigned to this mongo entity");

                if (ent.State == MongoEntityState.Deleted)
                    throw new Exception($"can't attach a mongo entity with state set to detached");

                ent.MongoContext = this;
                ent.State = state;

                attachedEntities.Add(new AttachedMongoEntity(ent));
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

            public IEnumerable<T> FindAll<T>() where T : MongoEntity
            {
                var q = GetRepository<T>().FindAll();

                foreach (var ent in q)
                {
                    Attach(ent, MongoEntityState.Undefined);

                    yield return ent;
                }
            }

            public IEnumerable<T> Find<T>(Expression<Func<T, bool>> filter) where T : MongoEntity
            {
                var q = GetRepository<T>().Find(filter);

                foreach (var ent in q)
                {
                    Attach(ent, MongoEntityState.Undefined);

                    yield return ent;
                }
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
                                aent.Entity.BeforeSave();
                                repo.GenericInsert(aent.Entity);
                                aent.Entity.State = MongoEntityState.Undefined;
                                aent.ResetOrigEntity();
                                aent.Entity.AfterSave();
                            }
                            break;

                        case MongoEntityState.Undefined:
                            {
                                aent.Entity.BeforeSave();
                                repo.GenericUpdate(this, aent.Entity, aent.OrigEntity);
                                aent.Entity.AfterSave();
                            }
                            break;

                        case MongoEntityState.Deleted:
                            {
                                repo.GenericDelete(aent.Entity);
                            }
                            break;
                    }
                }
            }

        }

    }

}
