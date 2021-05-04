#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroToJson;
using SolTechnology.Avro.Merge;
using SolTechnology.Avro.Schema.Abstract;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// TODO
        /// </summary>
        public static byte[] Merge<T>(IEnumerable<byte[]> avroObjects)
        {
            var mergeDecoder = new MergeDecoder();
            var itemSchema = BuildSchema.Schema.Create(typeof(T));
            //read header
            //collect object - iterate

            byte[] avroData = new byte[1];

           foreach (var avroObject in avroObjects)
           {
             avroData = mergeDecoder.ExtractAvroData(avroObject, itemSchema.ToString());
           }

           var x = avroObjects.First().Length;
           var y = avroData.Length;
           //write header with IEnumerable<T>
           //store objects

           //drink bear

           throw new NotImplementedException();
        }
    }
}
