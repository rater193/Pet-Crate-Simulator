using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Resolves a saved pet to its prefab even when the stored <c>PetPrefabPath</c> doesn't resolve via
/// <see cref="GameObject.GetPrefab(string)"/> (e.g. an old/buggy path like "Prefabs/Pets/monarch.prefab"
/// when the real asset is "prefabs/pets/playtest/z1/monarch.prefab").
///
/// It builds a lookup from the pet prefabs actually referenced by the scene's crates / shop displays,
/// keyed by full path, by filename (last path segment, no extension), by root name, and by display name.
/// This both RECOVERS already-saved pets and makes future loads robust to path/case/subfolder changes.
/// </summary>
public static class PetPrefabResolver
{
	private static readonly Dictionary<string, GameObject> byKey = new( StringComparer.OrdinalIgnoreCase );
	private static bool built;

	public static GameObject Resolve( Scene scene, string prefabPath, string displayName )
	{
		EnsureBuilt( scene );

		if ( !string.IsNullOrWhiteSpace( prefabPath ) )
		{
			if ( byKey.TryGetValue( prefabPath, out var byPath ) && byPath.IsValid() )
				return byPath;

			var file = FileKey( prefabPath );
			if ( !string.IsNullOrWhiteSpace( file ) && byKey.TryGetValue( file, out var byFile ) && byFile.IsValid() )
				return byFile;
		}

		if ( !string.IsNullOrWhiteSpace( displayName ) && byKey.TryGetValue( displayName, out var byName ) && byName.IsValid() )
			return byName;

		return null;
	}

	public static void Invalidate()
	{
		built = false;
		byKey.Clear();
	}

	private static void EnsureBuilt( Scene scene )
	{
		// Rebuild if never built or the cached references died (e.g. after a hot-reload).
		if ( built && byKey.Values.Any( v => v.IsValid() ) )
			return;

		byKey.Clear();
		built = true;

		if ( scene == null )
			return;

		foreach ( var crate in scene.GetAllComponents<InteractBuyCrate>() )
		{
			if ( crate.Pets == null )
				continue;

			foreach ( var reward in crate.Pets )
				Index( reward?.PetPrefab );
		}

		foreach ( var shop in scene.GetAllComponents<CrateShopDisplay>() )
		{
			foreach ( var reward in shop.GetConfiguredRewards() )
				Index( reward?.PetPrefab );
		}
	}

	private static void Index( GameObject prefab )
	{
		if ( !prefab.IsValid() )
			return;

		var source = prefab.PrefabInstanceSource;
		if ( !string.IsNullOrWhiteSpace( source ) )
		{
			byKey[source] = prefab;
			var fileKey = FileKey( source );
			if ( !string.IsNullOrWhiteSpace( fileKey ) )
				byKey[fileKey] = prefab;
		}

		if ( !string.IsNullOrWhiteSpace( prefab.Name ) )
			byKey[prefab.Name] = prefab;

		var component = prefab.GetComponent<PetComponent>() ?? prefab.GetComponentInChildren<PetComponent>();
		if ( !string.IsNullOrWhiteSpace( component?.DisplayName ) )
			byKey[component.DisplayName] = prefab;
	}

	/// <summary>Last path segment without the .prefab extension, e.g. "Prefabs/Pets/monarch.prefab" -> "monarch".</summary>
	private static string FileKey( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
			return null;

		var normalized = path.Replace( '\\', '/' );
		var slash = normalized.LastIndexOf( '/' );
		if ( slash >= 0 )
			normalized = normalized.Substring( slash + 1 );

		if ( normalized.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) )
			normalized = normalized.Substring( 0, normalized.Length - ".prefab".Length );

		return normalized;
	}
}
