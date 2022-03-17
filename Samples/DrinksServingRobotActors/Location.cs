// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class Location
    {
        public double X { get; private set; }

        public double Y { get; private set; }

        public Location(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return $"( {this.X}, {this.Y} )";
        }

        public override bool Equals(object obj)
        {
            var other = obj as Location;
            return other != null
                ? this.X == other.X && this.Y == other.Y
                : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new System.NotImplementedException();
        }
    }
}
