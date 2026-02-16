namespace MarketZone.Helpers
{
	/// <summary>
	/// Provides utilities for geographic calculations
	/// </summary>
	public static class GeoHelper
	{
		/// <summary>
		/// Calculates the distance between two geographic coordinates using the Haversine formula
		/// </summary>
		/// <param name="lat1">Latitude of first point</param>
		/// <param name="lon1">Longitude of first point</param>
		/// <param name="lat2">Latitude of second point</param>
		/// <param name="lon2">Longitude of second point</param>
		/// <returns>Distance in kilometers</returns>
		public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
		{
			const double EarthRadiusKm = 6371;
			var dLat = ToRadians(lat2 - lat1);
			var dLon = ToRadians(lon2 - lon1);

			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
					Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
					Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			return EarthRadiusKm * c;
		}

		private static double ToRadians(double degrees)
		{
			return degrees * Math.PI / 180;
		}
	}
}
