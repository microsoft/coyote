// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// Represents the line in the form: y = a + b.x where a is the offset and b is the slope of the line.
    /// </summary>
    public struct Line
    {
        /// <summary>
        /// The y-offset of the line.
        /// </summary>
        public double Offset;

        /// <summary>
        /// The slope of the line.
        /// </summary>
        public double Slope;

        /// <summary>
        /// Initializes a new instance of the <see cref="Line"/> struct.
        /// </summary>
        public Line(double offset, double slope)
        {
            this.Offset = offset;
            this.Slope = slope;
        }
    }

    /// <summary>
    /// A simple x, y value for 2D math.
    /// </summary>
    public struct DataPoint
    {
        /// <summary>
        /// The X value.
        /// </summary>
        public double X;

        /// <summary>
        /// The Y value.
        /// </summary>
        public double Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPoint"/> struct.
        /// </summary>
        public DataPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>
    /// A set of helper methods that do math.
    /// </summary>
    public static class MathHelpers
    {
        /// <summary>
        /// Return the Mean of the given numbers.
        /// </summary>
        public static double Mean(IEnumerable<double> values)
        {
            double sum = 0;
            double count = 0;
            foreach (double d in values)
            {
                sum += d;
                count++;
            }

            if (count == 0)
            {
                return 0;
            }

            return sum / count;
        }

        /// <summary>
        /// Return the standard deviation of the given values.
        /// </summary>
        public static double StandardDeviation(IEnumerable<double> values)
        {
            double mean = Mean(values);
            double totalSquares = 0;
            int count = 0;
            foreach (double v in values)
            {
                count++;
                double diff = mean - v;
                totalSquares += diff * diff;
            }

            if (count == 0)
            {
                return 0;
            }

            return Math.Sqrt(totalSquares / count);
        }

        /// <summary>
        /// Trim values outside of +/- the given range from the mean.
        /// </summary>
        internal static IEnumerable<double> Trim(IEnumerable<double> times, double range)
        {
            double mean = Mean(times);
            foreach (var item in times)
            {
                if (item <= mean + range && item >= mean - range)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Trim values outside mean + range.
        /// </summary>
        internal static IEnumerable<double> TrimHigh(IEnumerable<double> times, double range)
        {
            double mean = Mean(times);
            foreach (var item in times)
            {
                if (item <= mean + range)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Return the variance, sum of the difference between each value and the mean, squared.
        /// </summary>
        public static double Variance(IEnumerable<double> values)
        {
            double mean = Mean(values);
            double variance = 0;
            foreach (double d in values)
            {
                double diff = d - mean;
                variance += diff * diff;
            }

            return variance;
        }

        /// <summary>
        /// Convert the list of doubles into a list of DataPoints.
        /// </summary>
        public static List<DataPoint> ToDataPoints(IEnumerable<double> values)
        {
            int index = 0;
            return new List<DataPoint>(from v in values select new DataPoint(index++, v));
        }

        /// <summary>
        /// Return the covariance in the given x,y values.
        /// The sum of the difference between x and its mean times the difference between y and its mean.
        /// </summary>
        public static double Covariance(IEnumerable<DataPoint> pts)
        {
            double xsum = 0;
            double ysum = 0;
            double count = 0;
            foreach (var d in pts)
            {
                xsum += d.X;
                ysum += d.Y;
                count++;
            }

            if (count == 0)
            {
                return 0;
            }

            double xMean = xsum / count;
            double yMean = ysum / count;
            double covariance = 0;
            foreach (var d in pts)
            {
                covariance += (d.X - xMean) * (d.Y - yMean);
            }

            return covariance;
        }

        /// <summary>
        /// Compute the trend line through the given points, and return the line in the form:
        /// y = a + b.x.
        /// </summary>
        /// <param name="pts">The data to analyze.</param>
        public static Line LinearRegression(IEnumerable<DataPoint> pts)
        {
            double xMean = Mean(from p in pts select p.X);
            double yMean = Mean(from p in pts select p.Y);
            double xVariance = Variance(from p in pts select p.X);
            double yVariance = Variance(from p in pts select p.Y);
            double covariance = Covariance(pts);
            double a = 0;
            double b = 0;
            if (xVariance == 0)
            {
                a = yMean;
                b = 1;
            }
            else
            {
                b = covariance / xVariance;
                a = yMean - (b * xMean);
            }

            return new Line(a, b);
        }
    }
}
