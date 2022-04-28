using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvroConvertComponentTests.Headless
{
    public readonly struct StructWithPrivateFields : IEquatable<StructWithPrivateFields>, IComparable<StructWithPrivateFields>, IFormattable, IComparable
    {
        // These correspond to -9998-01-01 and 9999-12-31 respectively.
        internal const int MinDays = -4371222;
        internal const int MaxDays = 2932896;

        private const long MinTicks = MinDays * 10;
        private const long MaxTicks = (MaxDays + 1) * 10 - 1;
        private const long MinMilliseconds = MinDays * (long)100;
        private const long MaxMilliseconds = (MaxDays + 1) * (long)100 - 1;
        private const long MinSeconds = MinDays * (long)1000;
        private const long MaxSeconds = (MaxDays + 1) * (long)1000 - 1;


        public static StructWithPrivateFields MinValue { get; } = new StructWithPrivateFields(MinDays);

        public static StructWithPrivateFields MaxValue { get; } = new StructWithPrivateFields(MaxDays);


        private readonly int duration;

        private StructWithPrivateFields(int days, bool deliberatelyInvalid)
        {
            this.duration = days;
        }

        public StructWithPrivateFields(int duration)
        {
            this.duration = duration;
        }


     
        internal int TimeSinceEpoch => duration;



        #region IComparable<Instant> and IComparable Members
        /// <summary>
        /// Compares the current object with another object of the same type.
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   A 32-bit signed integer that indicates the relative order of the objects being compared.
        ///   The return value has the following meanings:
        ///   <list type = "table">
        ///     <listheader>
        ///       <term>Value</term>
        ///       <description>Meaning</description>
        ///     </listheader>
        ///     <item>
        ///       <term>&lt; 0</term>
        ///       <description>This object is less than the <paramref name = "other" /> parameter.</description>
        ///     </item>
        ///     <item>
        ///       <term>0</term>
        ///       <description>This object is equal to <paramref name = "other" />.</description>
        ///     </item>
        ///     <item>
        ///       <term>&gt; 0</term>
        ///       <description>This object is greater than <paramref name = "other" />.</description>
        ///     </item>
        ///   </list>
        /// </returns>
        public int CompareTo(StructWithPrivateFields other) => duration.CompareTo(other.duration);

        /// <summary>
        /// Implementation of <see cref="IComparable.CompareTo"/> to compare two instants.
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <remarks>
        /// This uses explicit interface implementation to avoid it being called accidentally. The generic implementation should usually be preferred.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="obj"/> is non-null but does not refer to an instance of <see cref="Instant"/>.</exception>
        /// <param name="obj">The object to compare this value with.</param>
        /// <returns>The result of comparing this instant with another one; see <see cref="CompareTo(NodaTime.Instant)"/> for general details.
        /// If <paramref name="obj"/> is null, this method returns a value greater than 0.
        /// </returns>
        int IComparable.CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }
            return CompareTo((StructWithPrivateFields)obj);
        }
        #endregion

        #region Object overrides

        public override bool Equals(object? obj) => obj is StructWithPrivateFields other && Equals(other);

        public override int GetHashCode() => duration.GetHashCode();
        #endregion  // Object overrides


        /// <summary>
        /// Implements the operator == (equality).
        /// See the type documentation for a description of equality semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if values are equal to each other, otherwise <c>false</c>.</returns>
        public static bool operator ==(StructWithPrivateFields left, StructWithPrivateFields right) => left.duration == right.duration;

        /// <summary>
        /// Implements the operator != (inequality).
        /// See the type documentation for a description of equality semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if values are not equal to each other, otherwise <c>false</c>.</returns>
        public static bool operator !=(StructWithPrivateFields left, StructWithPrivateFields right) => !(left == right);

        /// <summary>
        /// Implements the operator &lt; (less than).
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if the left value is less than the right value, otherwise <c>false</c>.</returns>
        public static bool operator <(StructWithPrivateFields left, StructWithPrivateFields right) => left.duration < right.duration;

        /// <summary>
        /// Implements the operator &lt;= (less than or equal).
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if the left value is less than or equal to the right value, otherwise <c>false</c>.</returns>
        public static bool operator <=(StructWithPrivateFields left, StructWithPrivateFields right) => left.duration <= right.duration;

        /// <summary>
        /// Implements the operator &gt; (greater than).
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if the left value is greater than the right value, otherwise <c>false</c>.</returns>
        public static bool operator >(StructWithPrivateFields left, StructWithPrivateFields right) => left.duration > right.duration;

        /// <summary>
        /// Implements the operator &gt;= (greater than or equal).
        /// See the type documentation for a description of ordering semantics.
        /// </summary>
        /// <param name="left">The left hand side of the operator.</param>
        /// <param name="right">The right hand side of the operator.</param>
        /// <returns><c>true</c> if the left value is greater than or equal to the right value, otherwise <c>false</c>.</returns>
        public static bool operator >=(StructWithPrivateFields left, StructWithPrivateFields right) => left.duration >= right.duration;
  

        #region Convenience methods

        public static StructWithPrivateFields Max(StructWithPrivateFields x, StructWithPrivateFields y)
        {
            return x > y ? x : y;
        }

        public static StructWithPrivateFields Min(StructWithPrivateFields x, StructWithPrivateFields y) => x < y ? x : y;
        #endregion


        #region IEquatable<Instant> Members
        /// <summary>
        /// Indicates whether the value of this instant is equal to the value of the specified instant.
        /// See the type documentation for a description of equality semantics.
        /// </summary>
        /// <param name="other">The value to compare with this instance.</param>
        /// <returns>
        /// true if the value of this instant is equal to the value of the <paramref name="other" /> parameter;
        /// otherwise, false.
        /// </returns>
        public bool Equals(StructWithPrivateFields other) => this == other;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return duration.ToString(format, formatProvider);
        }
        #endregion

    }
}
