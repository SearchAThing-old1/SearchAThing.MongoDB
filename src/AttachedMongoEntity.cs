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
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace SearchAThing
{

    namespace MongoDB
    {

        public class AttachedMongoEntity
        {

            public Type NominalType { get; private set; }

            public MongoEntity Entity { get; private set; }

            byte[] origEntityBytes;
            MongoEntity _OrigEntity;
            public MongoEntity OrigEntity
            {
                get
                {
                    if (_OrigEntity == null) _OrigEntity = BsonSerializer.Deserialize(origEntityBytes, NominalType) as MongoEntity;
                    return _OrigEntity;
                }
            }

            internal void ResetOrigEntity()
            {                
                origEntityBytes = BsonExtensionMethods.ToBson(Entity, NominalType);
                _OrigEntity = null;
            }

            /// <summary>
            /// enclose the given entity taking a backup
            /// </summary>            
            public AttachedMongoEntity(MongoEntity entity, Type type = null)
            {
                type = type ?? entity.GetType();

                NominalType = type;

                Entity = entity;

                ResetOrigEntity();
            }


        }

    }

}

