#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel.Models
{
    internal class AvroModel
    {
        internal List<AvroClass> Classes { get; set; } = new List<AvroClass>();
        internal List<AvroEnum> Enums { get; set; } = new List<AvroEnum>();
    }

    internal class AvroEnum
    {
        public string EnumName { get; set; }
        public List<string> Symbols { get; set; } = new List<string>();
    }

    internal class AvroClass
    {
        public string ClassName { get; set; }
        public string ClassNamespace { get; set; }
        public List<AvroField> Fields { get; set; } = new List<AvroField>();
    }

    internal class AvroField
    {
        public string FieldType { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Default { get; set; }
        public string Doc { get; set; }
    }
}
