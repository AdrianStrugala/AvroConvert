#region license

/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/** Modifications copyright(C) 2021 Adrian Strugala **/

#endregion

using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader
{
    internal class Header
    {
        private Dictionary<string, byte[]> MetaData { get; }

        internal byte[] SyncData { get; set;}

        internal BuildSchema.Schema Schema { get; set; }

        internal Header()
        {
            MetaData = new Dictionary<string, byte[]>();
            SyncData = new byte[16];
        }

        internal void AddMetadata(string key, byte[] value)
        {
            MetaData.Add(key, value);
        }

           internal void AddMetadata(string key, string value)
        {
            MetaData.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        internal string GetMetadata(string key)
        {
            MetaData.TryGetValue(key, out var value);
            if(value == null){
                return null;
            }
            var valueAsString = System.Text.Encoding.UTF8.GetString(value);
            return valueAsString;
        }

        internal byte[] GetRawMetadata(string key)
        {
            MetaData.TryGetValue(key, out var value);
            return value;
        }

        internal Dictionary<string, byte[]> GetRawMetadata()
        {
            return MetaData;
        }

        internal int GetMetadataSize()
        {
           return MetaData.Count;
        }
    }
}
