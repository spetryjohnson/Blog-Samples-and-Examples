using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// These are the pieces you need to implement JOIN-less lookups using C# enums rather than 
/// lookup tables in the database. I dumped everything into a single file for convenience; move
/// the pieces to whatever organizational structure you want. 
///
/// This code uses a "do whatever you want with it" license. Just do whatever you want with it.
/// </summary>
namespace PetryJohnsonSamples {
	
	public interface IStringAttribute<T> {
		T Value { get; }
	}

	/// <summary>
	/// Maps an enum to a string constant that is used to represent the value when stored in the database.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class StringConstantAttribute : Attribute, IStringAttribute<string> {
		public string Value { get; set; }
		public StringConstantAttribute(string stringVal) { this.Value = stringVal; }
	}

	/// <summary>
	/// Maps an enum to a human-readable description string. This can be used to provide a more readable
	/// representation than ToString(), or when we want to specifically control the output string separately
	/// from the enum name.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DescriptionAttribute : Attribute, IStringAttribute<string> {
		public string Description { get; set; }
		public DescriptionAttribute(string p_description) { this.Description = p_description; }

		public string Value { get { return Description; } }
	}

	public static class EnumExtensions {

		/// <summary>
		/// Returns the StringConstant value for this enum, or the ToString() result if
		/// no StringConstant attribute is defined.
		/// </summary>
		public static string ToStringConstant(this Enum enumVal) {
			return enumVal.GetAttributeString<StringConstantAttribute>();
		}

		/// <summary>
		/// Returns the Description value for this enum, or the ToString() result if
		/// no Description attribute is defined.
		/// </summary>
		public static string ToDescription(this Enum enumVal) {
			return enumVal.GetAttributeString<DescriptionAttribute>();
		}

		public static T[] GetAttributes<T>(this Enum enumVal) where T : Attribute {
			try {
				string stringVal = enumVal.ToString();

				var attributes = (T[])enumVal
					.GetType()
					.GetField(stringVal)
					.GetCustomAttributes(typeof(T), false);

				return attributes;
			}
			catch (Exception ex) {
				throw new ApplicationException(
					"Error getting attributes for enum '" + enumVal.GetType().ToString()
					+ "', value '" + enumVal.ToString() + "': " + ex.Message, ex
				);
			}
		}

		public static string GetAttributeString<T>(this Enum enumVal) where T : Attribute, IStringAttribute<string> {
			var attributes = enumVal.GetAttributes<T>();

			return attributes.Any()
				? attributes[0].Value
				: enumVal.ToString();
		}
	}

	public static class StringExtensions {

		/// <summary>
		/// Converts a string into an instance of the specified enum by comparing its value
		/// against the enum's ToString() value, its StringConstant attribute [if one exists]
		/// and finally against its Description attribute [if one exists].
		/// 
		/// Throws an error if the conversion fails.
		/// </summary>
		public static T ToEnum<T>(this string stringVal) where T : struct {
			try {
				return (T)Enum.Parse(typeof(T), stringVal);
			}
			catch (ArgumentException ex) {
				foreach (Enum enumValue in Enum.GetValues(typeof(T))) {
					bool hasMatchingConstant = enumValue
						.GetAttributes<StringConstantAttribute>()
						.Where(a => a.Value.EqualsIgnoringCase(stringVal))
						.Any();

					if (hasMatchingConstant) {
						return (T)Enum.Parse(typeof(T), enumValue.ToString());
					}

					bool hasMatchingDescription = enumValue
						.GetAttributes<DescriptionAttribute>()
						.Where(a => a.Description == stringVal)
						.Any();

					if (hasMatchingDescription) {
						return (T)Enum.Parse(typeof(T), enumValue.ToString());
					}
				}

				throw new ArgumentException(
					String.Format("Could not convert '{0}' to a {1} value."
						, stringVal
						, typeof(T).ToString()
					), ex
				);
			}
		}

		/// <summary>
		/// Converts a string into an instance of the specified enum by comparing its value
		/// against the enum's ToString() value, its StringConstant attribute [if one exists]
		/// and finally against its Description attribute [if one exists].
		/// 
		/// If no matches are found, returns the specified default value.
		/// </summary>
		public static T ToEnum<T>(this string stringVal, T valIfParseFails) where T : struct {
			try {
				return stringVal.ToEnum<T>();
			}
			catch (ArgumentException) {
				return valIfParseFails;
			}
		}

		/// <summary>
		/// Performs a case-insensitive comparison and returns TRUE if the values are equal.
		/// </summary>
		public static bool EqualsIgnoringCase(this string thisValue, string compareTo) {
			bool thisIsNullOrEmpty = String.IsNullOrEmpty(thisValue);
			bool otherIsNullOrEmpty = String.IsNullOrEmpty(compareTo);

			if (thisIsNullOrEmpty && otherIsNullOrEmpty) {
				return true;
			}

			if (thisIsNullOrEmpty != otherIsNullOrEmpty) {
				return false;
			}

			return thisValue.Equals(compareTo, StringComparison.CurrentCultureIgnoreCase);
		}
	}
}
