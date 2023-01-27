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

/** Modifications copyright(C) 2022 Adrian Strugala **/
#endregion

using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class WriteStep
    {
        internal Encoder.WriteItem WriteField { get; set; }
        internal string FiledName { get; set; }
    }
}