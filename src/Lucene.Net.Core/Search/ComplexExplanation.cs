using System;

namespace Lucene.Net.Search
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Expert: Describes the score computation for document and query, and
    /// can distinguish a match independent of a positive value.
    /// </summary>
    public class ComplexExplanation : Explanation
    {
        private bool? match;

        public ComplexExplanation()
            : base()
        {
        }

        public ComplexExplanation(bool match, float value, string description)
            : base(value, description)
        {
            // NOTE: use of "boolean" instead of "Boolean" in params is conscious
            // choice to encourage clients to be specific.
            this.match = Convert.ToBoolean(match);
        }

        /// <summary>
        /// The match status of this explanation node. </summary>
        /// <returns> May be null if match status is unknown </returns>
        public virtual bool? Match
        {
            get
            {
                return match;
            }
            set
            {
                this.match = value;
            }
        }

        /// <summary>
        /// Indicates whether or not this Explanation models a good match.
        ///
        /// <p>
        /// If the match status is explicitly set (i.e.: not null) this method
        /// uses it; otherwise it defers to the superclass.
        /// </p> </summary>
        /// <seealso cref= #getMatch </seealso>
        public override bool IsMatch
        {
            get
            {
                bool? m = Match;
                return (null != m ? (bool)m : base.IsMatch);
            }
        }

        protected internal override string Summary
        {
            get
            {
                if (null == Match)
                {
                    return base.Summary;
                }

                return Value + " = " + (IsMatch ? "(MATCH) " : "(NON-MATCH) ") + Description;
            }
        }
    }
}