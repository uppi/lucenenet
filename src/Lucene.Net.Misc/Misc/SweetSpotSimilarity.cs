﻿using System;

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

namespace org.apache.lucene.misc
{

	using DefaultSimilarity = org.apache.lucene.search.similarities.DefaultSimilarity;
	using FieldInvertState = org.apache.lucene.index.FieldInvertState;

	/// <summary>
	/// <para>
	/// A similarity with a lengthNorm that provides for a "plateau" of
	/// equally good lengths, and tf helper functions.
	/// </para>
	/// <para>
	/// For lengthNorm, A min/max can be specified to define the
	/// plateau of lengths that should all have a norm of 1.0.
	/// Below the min, and above the max the lengthNorm drops off in a
	/// sqrt function.
	/// </para>
	/// <para>
	/// For tf, baselineTf and hyperbolicTf functions are provided, which
	/// subclasses can choose between.
	/// </para>
	/// </summary>
	/// <seealso cref= <a href="doc-files/ss.gnuplot">A Gnuplot file used to generate some of the visualizations refrenced from each function.</a>  </seealso>
	public class SweetSpotSimilarity : DefaultSimilarity
	{

	  private int ln_min = 1;
	  private int ln_max = 1;
	  private float ln_steep = 0.5f;

	  private float tf_base = 0.0f;
	  private float tf_min = 0.0f;

	  private float tf_hyper_min = 0.0f;
	  private float tf_hyper_max = 2.0f;
	  private double tf_hyper_base = 1.3d;
	  private float tf_hyper_xoffset = 10.0f;

	  public SweetSpotSimilarity() : base()
	  {
	  }

	  /// <summary>
	  /// Sets the baseline and minimum function variables for baselineTf
	  /// </summary>
	  /// <seealso cref= #baselineTf </seealso>
	  public virtual void setBaselineTfFactors(float @base, float min)
	  {
		tf_min = min;
		tf_base = @base;
	  }

	  /// <summary>
	  /// Sets the function variables for the hyperbolicTf functions
	  /// </summary>
	  /// <param name="min"> the minimum tf value to ever be returned (default: 0.0) </param>
	  /// <param name="max"> the maximum tf value to ever be returned (default: 2.0) </param>
	  /// <param name="base"> the base value to be used in the exponential for the hyperbolic function (default: 1.3) </param>
	  /// <param name="xoffset"> the midpoint of the hyperbolic function (default: 10.0) </param>
	  /// <seealso cref= #hyperbolicTf </seealso>
	  public virtual void setHyperbolicTfFactors(float min, float max, double @base, float xoffset)
	  {
		tf_hyper_min = min;
		tf_hyper_max = max;
		tf_hyper_base = @base;
		tf_hyper_xoffset = xoffset;
	  }

	  /// <summary>
	  /// Sets the default function variables used by lengthNorm when no field
	  /// specific variables have been set.
	  /// </summary>
	  /// <seealso cref= #computeLengthNorm </seealso>
	  public virtual void setLengthNormFactors(int min, int max, float steepness, bool discountOverlaps)
	  {
		this.ln_min = min;
		this.ln_max = max;
		this.ln_steep = steepness;
		this.discountOverlaps = discountOverlaps;
	  }

	  /// <summary>
	  /// Implemented as <code> state.getBoost() *
	  /// computeLengthNorm(numTokens) </code> where
	  /// numTokens does not count overlap tokens if
	  /// discountOverlaps is true by default or true for this
	  /// specific field. 
	  /// </summary>
	  public override float lengthNorm(FieldInvertState state)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numTokens;
		int numTokens;

		if (discountOverlaps)
		{
		  numTokens = state.Length - state.NumOverlap;
		}
		else
		{
		  numTokens = state.Length;
		}

		return state.Boost * computeLengthNorm(numTokens);
	  }

	  /// <summary>
	  /// Implemented as:
	  /// <code>
	  /// 1/sqrt( steepness * (abs(x-min) + abs(x-max) - (max-min)) + 1 )
	  /// </code>.
	  /// 
	  /// <para>
	  /// This degrades to <code>1/sqrt(x)</code> when min and max are both 1 and
	  /// steepness is 0.5
	  /// </para>
	  /// 
	  /// <para>
	  /// :TODO: potential optimization is to just flat out return 1.0f if numTerms
	  /// is between min and max.
	  /// </para>
	  /// </summary>
	  /// <seealso cref= #setLengthNormFactors </seealso>
	  /// <seealso cref= <a href="doc-files/ss.computeLengthNorm.svg">An SVG visualization of this function</a>  </seealso>
	  public virtual float computeLengthNorm(int numTerms)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int l = ln_min;
		int l = ln_min;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int h = ln_max;
		int h = ln_max;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float s = ln_steep;
		float s = ln_steep;

		return (float)(1.0f / Math.Sqrt((s * (float)(Math.Abs(numTerms - l) + Math.Abs(numTerms - h) - (h - l))) + 1.0f));
	  }

	  /// <summary>
	  /// Delegates to baselineTf
	  /// </summary>
	  /// <seealso cref= #baselineTf </seealso>
	  public override float tf(float freq)
	  {
		return baselineTf(freq);
	  }

	  /// <summary>
	  /// Implemented as:
	  /// <code>
	  ///  (x &lt;= min) &#63; base : sqrt(x+(base**2)-min)
	  /// </code>
	  /// ...but with a special case check for 0.
	  /// <para>
	  /// This degrates to <code>sqrt(x)</code> when min and base are both 0
	  /// </para>
	  /// </summary>
	  /// <seealso cref= #setBaselineTfFactors </seealso>
	  /// <seealso cref= <a href="doc-files/ss.baselineTf.svg">An SVG visualization of this function</a>  </seealso>
	  public virtual float baselineTf(float freq)
	  {

		if (0.0f == freq)
		{
			return 0.0f;
		}

		return (freq <= tf_min) ? tf_base : (float)Math.Sqrt(freq + (tf_base * tf_base) - tf_min);
	  }

	  /// <summary>
	  /// Uses a hyperbolic tangent function that allows for a hard max...
	  /// 
	  /// <code>
	  /// tf(x)=min+(max-min)/2*(((base**(x-xoffset)-base**-(x-xoffset))/(base**(x-xoffset)+base**-(x-xoffset)))+1)
	  /// </code>
	  /// 
	  /// <para>
	  /// This code is provided as a convenience for subclasses that want
	  /// to use a hyperbolic tf function.
	  /// </para>
	  /// </summary>
	  /// <seealso cref= #setHyperbolicTfFactors </seealso>
	  /// <seealso cref= <a href="doc-files/ss.hyperbolicTf.svg">An SVG visualization of this function</a>  </seealso>
	  public virtual float hyperbolicTf(float freq)
	  {
		if (0.0f == freq)
		{
			return 0.0f;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float min = tf_hyper_min;
		float min = tf_hyper_min;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float max = tf_hyper_max;
		float max = tf_hyper_max;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double base = tf_hyper_base;
		double @base = tf_hyper_base;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float xoffset = tf_hyper_xoffset;
		float xoffset = tf_hyper_xoffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double x = (double)(freq - xoffset);
		double x = (double)(freq - xoffset);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float result = min + (float)((max-min) / 2.0f * (((Math.pow(base,x) - Math.pow(base,-x)) / (Math.pow(base,x) + Math.pow(base,-x))) + 1.0d));
		float result = min + (float)((max - min) / 2.0f * (((Math.Pow(@base,x) - Math.Pow(@base,-x)) / (Math.Pow(@base,x) + Math.Pow(@base,-x))) + 1.0d));

		return float.IsNaN(result) ? max : result;

	  }

	}

}