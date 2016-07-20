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
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using SearchAThing.MongoDB;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;
using System.Diagnostics;
using MongoDB.Bson;

namespace SearchAThing
{

    namespace MongoDB
    {

        public enum MongoEntityState
        {

            /// <summary>
            /// entity not in a context
            /// </summary>
            Detached,

            /// <summary>
            /// the fact is unchanged or modified has to be detected through comparision with the original
            /// </summary>
            Undefined,

            /// <summary>
            /// entity in context as new
            /// </summary>
            New,

            /// <summary>
            /// entity in context as deleted
            /// </summary>
            Deleted

        }

        public class MongoEntity : INotifyPropertyChanged
        {
                        
            public ObjectId ObjectId
            {
                get
                {
                    if (Id == null) Id = ObjectId.GenerateNewId().ToString();
                    return ObjectId.Parse(Id);
                }
            }

            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            #region State [pgis]
            [BsonIgnore]
            public MongoEntityState State { get; internal set; } = MongoEntityState.Detached;
            #endregion

            #region MongoContext [pgis]
            [BsonIgnore]
            public MongoContext MongoContext { get; internal set; }
            #endregion

            #region INotifyPropertyChanged [pce]       
            public event PropertyChangedEventHandler PropertyChanged;
            protected void SendPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion

            public DateTime CreateTimestampFromObjectId() { return ObjectId.CreationTime; }

            public MongoEntity()
            {                
            }

            /// <summary>
            /// Sets the state to deleted
            /// </summary>
            public void Delete()
            {
                if (State == MongoEntityState.Detached)
                    throw new Exception($"can't delete detached entity. Use ctx.Delete(x)");
                State = MongoEntityState.Deleted;
            }

            public event EventHandler BeforeSaveEvent;

            public event EventHandler AfterSaveEvent;

            internal void BeforeSaveAct()
            {
                BeforeSaveEvent?.Invoke(this, null);
                BeforeSave();
            }

            internal protected virtual void BeforeSave()
            {
            }

            internal void AfterSaveAct()
            {
                AfterSaveEvent?.Invoke(this, null);
                AfterSave();
            }

            internal protected virtual void AfterSave()
            {

            }

        }

    }

}
