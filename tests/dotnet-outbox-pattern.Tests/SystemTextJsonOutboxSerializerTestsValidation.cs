#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="SystemTextJsonOutboxSerializerTests"/> instances.
/// </summary>
public static class SystemTextJsonOutboxSerializerTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An enumerable of validation messages; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this SystemTextJsonOutboxSerializerTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate constructor-related behavior
        try
        {
            var defaultCtor = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Constructor_WithDefaultOptions_CreatesSerializer),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (defaultCtor is null)
            {
                errors.Add("Missing method: Constructor_WithDefaultOptions_CreatesSerializer");
            }
        }
        catch
        {
            errors.Add("Failed to validate Constructor_WithDefaultOptions_CreatesSerializer method");
        }

        try
        {
            var customCtor = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Constructor_WithCustomOptions_CreatesSerializer),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (customCtor is null)
            {
                errors.Add("Missing method: Constructor_WithCustomOptions_CreatesSerializer");
            }
        }
        catch
        {
            errors.Add("Failed to validate Constructor_WithCustomOptions_CreatesSerializer method");
        }

        try
        {
            var nullCtor = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Constructor_WithNullOptions_ThrowsArgumentNullException),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (nullCtor is null)
            {
                errors.Add("Missing method: Constructor_WithNullOptions_ThrowsArgumentNullException");
            }
        }
        catch
        {
            errors.Add("Failed to validate Constructor_WithNullOptions_ThrowsArgumentNullException method");
        }

        // Validate Serialize methods
        try
        {
            var serializeNull = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Serialize_WithNullValue_ReturnsNullString),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (serializeNull is null)
            {
                errors.Add("Missing method: Serialize_WithNullValue_ReturnsNullString");
            }
        }
        catch
        {
            errors.Add("Failed to validate Serialize_WithNullValue_ReturnsNullString method");
        }

        try
        {
            var serializeSimple = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Serialize_WithSimpleObject_ReturnsJsonString),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (serializeSimple is null)
            {
                errors.Add("Missing method: Serialize_WithSimpleObject_ReturnsJsonString");
            }
        }
        catch
        {
            errors.Add("Failed to validate Serialize_WithSimpleObject_ReturnsJsonString method");
        }

        try
        {
            var serializeComplex = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Serialize_WithComplexObject_ReturnsValidJson),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (serializeComplex is null)
            {
                errors.Add("Missing method: Serialize_WithComplexObject_ReturnsValidJson");
            }
        }
        catch
        {
            errors.Add("Failed to validate Serialize_WithComplexObject_ReturnsValidJson method");
        }

        try
        {
            var serializePrimitive = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Serialize_WithPrimitiveValue_ReturnsJsonString),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (serializePrimitive is null)
            {
                errors.Add("Missing method: Serialize_WithPrimitiveValue_ReturnsJsonString");
            }
        }
        catch
        {
            errors.Add("Failed to validate Serialize_WithPrimitiveValue_ReturnsJsonString method");
        }

        try
        {
            var serializeString = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Serialize_WithStringValue_ReturnsJsonString),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (serializeString is null)
            {
                errors.Add("Missing method: Serialize_WithStringValue_ReturnsJsonString");
            }
        }
        catch
        {
            errors.Add("Failed to validate Serialize_WithStringValue_ReturnsJsonString method");
        }

        // Validate Deserialize methods
        try
        {
            var deserializeNull = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithNullJson_ReturnsDefault),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeNull is null)
            {
                errors.Add("Missing method: Deserialize_WithNullJson_ReturnsDefault");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithNullJson_ReturnsDefault method");
        }

        try
        {
            var deserializeEmpty = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithEmptyString_ReturnsDefault),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeEmpty is null)
            {
                errors.Add("Missing method: Deserialize_WithEmptyString_ReturnsDefault");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithEmptyString_ReturnsDefault method");
        }

        try
        {
            var deserializeWhitespace = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithWhitespaceString_ReturnsDefault),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeWhitespace is null)
            {
                errors.Add("Missing method: Deserialize_WithWhitespaceString_ReturnsDefault");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithWhitespaceString_ReturnsDefault method");
        }

        try
        {
            var deserializeInvalid = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithInvalidJson_ReturnsDefault),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeInvalid is null)
            {
                errors.Add("Missing method: Deserialize_WithInvalidJson_ReturnsDefault");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithInvalidJson_ReturnsDefault method");
        }

        try
        {
            var deserializeSimple = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithSimpleObject_ReturnsDeserializedObject),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeSimple is null)
            {
                errors.Add("Missing method: Deserialize_WithSimpleObject_ReturnsDeserializedObject");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithSimpleObject_ReturnsDeserializedObject method");
        }

        try
        {
            var deserializeNullString = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithNullString_ReturnsDefault),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeNullString is null)
            {
                errors.Add("Missing method: Deserialize_WithNullString_ReturnsDefault");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithNullString_ReturnsDefault method");
        }

        try
        {
            var deserializePrimitive = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithPrimitiveType_ReturnsDeserializedValue),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializePrimitive is null)
            {
                errors.Add("Missing method: Deserialize_WithPrimitiveType_ReturnsDeserializedValue");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithPrimitiveType_ReturnsDeserializedValue method");
        }

        try
        {
            var deserializeString = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithStringType_ReturnsDeserializedString),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeString is null)
            {
                errors.Add("Missing method: Deserialize_WithStringType_ReturnsDeserializedString");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithStringType_ReturnsDeserializedString method");
        }

        try
        {
            var deserializeType = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithTypeParameter_ReturnsDeserializedObject),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeType is null)
            {
                errors.Add("Missing method: Deserialize_WithTypeParameter_ReturnsDeserializedObject");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithTypeParameter_ReturnsDeserializedObject method");
        }

        try
        {
            var deserializeNullJsonType = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithNullJsonAndType_ReturnsNull),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeNullJsonType is null)
            {
                errors.Add("Missing method: Deserialize_WithNullJsonAndType_ReturnsNull");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithNullJsonAndType_ReturnsNull method");
        }

        try
        {
            var deserializeEmptyJsonType = value.GetType().GetMethod(
                nameof(SystemTextJsonOutboxSerializerTests.Deserialize_WithEmptyJsonAndType_ReturnsNull),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (deserializeEmptyJsonType is null)
            {
                errors.Add("Missing method: Deserialize_WithEmptyJsonAndType_ReturnsNull");
            }
        }
        catch
        {
            errors.Add("Failed to validate Deserialize_WithEmptyJsonAndType_ReturnsNull method");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SystemTextJsonOutboxSerializerTests value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid.</exception>
    public static void EnsureValid(this SystemTextJsonOutboxSerializerTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"SystemTextJsonOutboxSerializerTests instance is not valid. Problems: {string.Join(", ", errors)}");
        }
    }
}