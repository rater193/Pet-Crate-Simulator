using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class JSONObject
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false
	};

	private readonly Dictionary<string, object> values = new();

	public IReadOnlyDictionary<string, object> Values => new ReadOnlyDictionary<string, object>( values );

	public object this[string key]
	{
		get => Get<object>( key );
		set => Set( key, value );
	}

	public static string ToJson( JSONObject jsonObject )
	{
		return jsonObject?.ToJson() ?? "{}";
	}

	public static JSONObject FromJson( string json )
	{
		if ( string.IsNullOrWhiteSpace( json ) )
			return new JSONObject();

		var node = JsonNode.Parse( json );
		if ( node is not JsonObject jsonObject )
			throw new ArgumentException( "JSON root must be an object.", nameof( json ) );

		return FromJsonObject( jsonObject );
	}

	public string ToJson( bool writeIndented = false )
	{
		var options = new JsonSerializerOptions( JsonOptions )
		{
			WriteIndented = writeIndented
		};

		return ToJsonObject().ToJsonString( options );
	}

	public JSONObject Set( string key, object value )
	{
		ValidateKey( key );
		values[key] = NormalizeValue( value );
		return this;
	}

	public static JSONObject Set( JSONObject jsonObject, string key, object value )
	{
		jsonObject ??= new JSONObject();
		return jsonObject.Set( key, value );
	}

	public bool Exists( string key )
	{
		ValidateKey( key );
		return values.ContainsKey( key );
	}

	public static bool Exists( JSONObject jsonObject, string key )
	{
		return jsonObject?.Exists( key ) ?? false;
	}

	public bool Remove( string key )
	{
		ValidateKey( key );
		return values.Remove( key );
	}

	public static bool Remove( JSONObject jsonObject, string key )
	{
		return jsonObject?.Remove( key ) ?? false;
	}

	public T Get<T>( string key, T defaultValue = default )
	{
		ValidateKey( key );

		if ( !values.TryGetValue( key, out var value ) || value is null )
			return defaultValue;

		if ( value is T typedValue )
			return typedValue;

		try
		{
			return JsonSerializer.Deserialize<T>( JsonSerializer.Serialize( value, JsonOptions ), JsonOptions );
		}
		catch
		{
			return defaultValue;
		}
	}

	public bool TryGet<T>( string key, out T value )
	{
		value = Get<T>( key );
		return Exists( key );
	}

	public JSONObject GetObject( string key )
	{
		return Get<JSONObject>( key );
	}

	public void Clear()
	{
		values.Clear();
	}

	private JsonObject ToJsonObject()
	{
		var jsonObject = new JsonObject();

		foreach ( var pair in values )
		{
			jsonObject[pair.Key] = ToJsonNode( pair.Value );
		}

		return jsonObject;
	}

	private static JSONObject FromJsonObject( JsonObject jsonObject )
	{
		var result = new JSONObject();

		foreach ( var pair in jsonObject )
		{
			result.values[pair.Key] = FromJsonNode( pair.Value );
		}

		return result;
	}

	private static object FromJsonNode( JsonNode node )
	{
		if ( node is null )
			return null;

		if ( node is JsonObject jsonObject )
			return FromJsonObject( jsonObject );

		if ( node is JsonArray jsonArray )
		{
			var list = new List<object>();
			foreach ( var item in jsonArray )
			{
				list.Add( FromJsonNode( item ) );
			}

			return list;
		}

		var element = node.Deserialize<JsonElement>( JsonOptions );
		return element.ValueKind switch
		{
			JsonValueKind.String => element.GetString(),
			JsonValueKind.Number when element.TryGetInt32( out var intValue ) => intValue,
			JsonValueKind.Number when element.TryGetInt64( out var longValue ) => longValue,
			JsonValueKind.Number when element.TryGetDouble( out var doubleValue ) => doubleValue,
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			_ => null
		};
	}

	private static JsonNode ToJsonNode( object value )
	{
		value = NormalizeValue( value );

		if ( value is null )
			return null;

		if ( value is JSONObject jsonObject )
			return jsonObject.ToJsonObject();

		if ( value is JsonNode node )
			return node.DeepClone();

		if ( value is JsonElement element )
			return JsonNode.Parse( element.GetRawText() );

		if ( value is IDictionary dictionary )
		{
			var dictionaryObject = new JsonObject();

			foreach ( DictionaryEntry entry in dictionary )
			{
				if ( entry.Key is null )
					continue;

				dictionaryObject[entry.Key.ToString()] = ToJsonNode( entry.Value );
			}

			return dictionaryObject;
		}

		if ( value is IEnumerable enumerable && value is not string )
		{
			var jsonArray = new JsonArray();

			foreach ( var item in enumerable )
			{
				jsonArray.Add( ToJsonNode( item ) );
			}

			return jsonArray;
		}

		return JsonSerializer.SerializeToNode( value, value.GetType(), JsonOptions );
	}

	private static object NormalizeValue( object value )
	{
		return value switch
		{
			decimal decimalValue => (double)decimalValue,
			float floatValue => (double)floatValue,
			_ => value
		};
	}

	private static void ValidateKey( string key )
	{
		if ( string.IsNullOrWhiteSpace( key ) )
			throw new ArgumentException( "JSON key cannot be empty.", nameof( key ) );
	}
}
