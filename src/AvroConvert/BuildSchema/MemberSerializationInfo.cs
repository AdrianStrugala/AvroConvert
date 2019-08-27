// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

namespace AvroConvert.BuildSchema
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    ///     Represents serialization information about a member of a class/struct.
    /// </summary>
    public sealed class MemberSerializationInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MemberSerializationInfo" /> class.
        /// </summary>
        public MemberSerializationInfo()
        {
            this.Aliases = new List<string>();
            this.Doc = string.Empty;
        }

        /// <summary>
        ///     Gets or sets the Avro name of the member.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets the aliases of the member.
        /// </summary>
        public ICollection<string> Aliases { get;  set; }

        /// <summary>
        ///     Gets or sets the doc attribute.
        /// </summary>
        public string Doc { get; set; }

        /// <summary>
        /// Gets or sets the runtime type of the member.
        /// </summary>
        public MemberInfo MemberInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether member is nullable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if member is nullable; otherwise, <c>false</c>.
        /// </value>
        public bool Nullable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default to use when the property is undefined.
        /// </summary>
        /// <value>
        ///   <c>null</c> or the chosen default value, when a property is undefined in a serialized Avro message.
        /// </value>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the default value has been set.
        /// </summary>
        /// <value>
        ///   <c>true</c> when the DefaultValue has been explicitly set.
        /// </value>
        public bool HasDefaultValue { get; set; }
    }
}
