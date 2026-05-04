using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PetPreviewRenderer : Component
{
	public static PetPreviewRenderer Instance { get; private set; }
	public static int PreviewVersion { get; private set; }

	private const string PreviewTag = "pet_preview";

	[Property] public int PreviewSize { get; set; } = 256;
	[Property] public int MaxPreviewsGeneratedPerFrame { get; set; } = 2;
	[Property] public int ActiveRenderFrames { get; set; } = 3;
	[Property] public Vector3 PreviewOrigin { get; set; } = new( 0f, 0f, -20000f );
	[Property] public float PreviewSlotSpacing { get; set; } = 320f;
	[Property] public Rotation PetRotation { get; set; } = Rotation.FromYaw( 35f+180f );
	[Property] public Color BackgroundColor { get; set; } = new( 0f, 0f, 0f, 0f );

	private readonly Dictionary<string, PreviewEntry> previews = new();
	private readonly Queue<string> pendingPreviewKeys = new();
	private GameObject previewRoot;
	private int nextPreviewSlot;

	protected override void OnStart()
	{
		Instance = this;
		EnsurePreviewRoot();
	}

	protected override void OnUpdate()
	{
		EnsurePreviewRoot();
		ProcessPendingPreviews();
		UpdateActivePreviewCameras();
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
		{
			Instance = null;
		}

		foreach ( var entry in previews.Values )
		{
			if ( entry.Container.IsValid() )
			{
				entry.Container.Destroy();
			}
		}

		previews.Clear();
		pendingPreviewKeys.Clear();
	}

	public Texture GetPreviewTexture( string petPrefabPath, GameObject petPrefab )
	{
		if ( string.IsNullOrWhiteSpace( petPrefabPath ) )
			petPrefabPath = petPrefab?.PrefabInstanceSource;

		if ( string.IsNullOrWhiteSpace( petPrefabPath ) )
			petPrefabPath = petPrefab?.Name;

		if ( string.IsNullOrWhiteSpace( petPrefabPath ) || !petPrefab.IsValid() )
			return null;

		if ( previews.TryGetValue( petPrefabPath, out var entry ) )
			return entry.Texture;

		previews[petPrefabPath] = new PreviewEntry
		{
			Prefab = petPrefab,
			PrefabPath = petPrefabPath
		};

		pendingPreviewKeys.Enqueue( petPrefabPath );
		return null;
	}

	private void ProcessPendingPreviews()
	{
		var previewsGenerated = 0;
		while ( previewsGenerated < MaxPreviewsGeneratedPerFrame && pendingPreviewKeys.Count > 0 )
		{
			var key = pendingPreviewKeys.Dequeue();
			if ( !previews.TryGetValue( key, out var entry ) || entry.Texture.IsValid() )
				continue;

			CreatePreview( entry );
			previewsGenerated++;
		}
	}

	private void UpdateActivePreviewCameras()
	{
		foreach ( var entry in previews.Values )
		{
			if ( entry.Camera == null || !entry.Camera.GameObject.IsValid() || !entry.Camera.Enabled )
				continue;

			entry.RenderFramesRemaining--;
			if ( entry.RenderFramesRemaining > 0 )
				continue;

			entry.Camera.Enabled = false;
			if ( entry.Pet.IsValid() )
			{
				entry.Pet.Enabled = false;
			}
		}
	}

	private void CreatePreview( PreviewEntry entry )
	{
		EnsurePreviewRoot();

		if ( !entry.Prefab.IsValid() )
			return;

		var slotPosition = PreviewOrigin + new Vector3( nextPreviewSlot++ * PreviewSlotSpacing, 0f, 0f );
		var container = new GameObject( previewRoot, true, $"PetPreview_{entry.Prefab.Name}" );
		container.WorldPosition = slotPosition;
		container.Tags.Add( PreviewTag );

		var pet = entry.Prefab.Clone();
		pet.Parent = container;
		pet.LocalPosition = Vector3.Zero;
		pet.LocalRotation = PetRotation;
		pet.Enabled = true;
		AddPreviewTagToHierarchy( pet );

		var bounds = pet.GetBounds();
		var center = bounds.Center;
		var size = bounds.Size;
		var height = MathF.Max( MathF.Max( size.x, MathF.Max( size.y, size.z ) ), 48f );

		var cameraObject = new GameObject( container, true, "PreviewCamera" );
		cameraObject.Tags.Add( PreviewTag );
		cameraObject.WorldPosition = center + new Vector3( -height * 1.35f, -height * 1.9f, height * 0.9f );
		cameraObject.WorldRotation = Rotation.LookAt( (center - cameraObject.WorldPosition).Normal, Vector3.Up );

		var camera = cameraObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = false;
		camera.ClearFlags = ClearFlags.Color | ClearFlags.Depth | ClearFlags.Stencil;
		camera.BackgroundColor = BackgroundColor;
		camera.EnablePostProcessing = false;
		camera.Orthographic = true;
		camera.OrthographicHeight = height * 1.45f;
		camera.ZNear = 1f;
		camera.ZFar = height * 12f;
		camera.RenderTags.Add( PreviewTag );
		camera.RenderTarget = Texture.CreateRenderTarget(
			$"PetPreview_{GetSafeTextureName( entry.PrefabPath )}",
			ImageFormat.RGBA8888,
			new Vector2( PreviewSize, PreviewSize )
		);

		entry.Container = container;
		entry.Pet = pet;
		entry.Camera = camera;
		entry.Texture = camera.RenderTarget;
		entry.RenderFramesRemaining = Math.Max( 1, ActiveRenderFrames );

		PreviewVersion++;
	}

	private void EnsurePreviewRoot()
	{
		if ( previewRoot.IsValid() )
			return;

		previewRoot = new GameObject( GameObject, true, "PetPreviewRendererRuntime" );
		previewRoot.WorldPosition = PreviewOrigin;
		previewRoot.Tags.Add( PreviewTag );
	}

	private static void AddPreviewTagToHierarchy( GameObject gameObject )
	{
		foreach ( var child in gameObject.GetAllObjects( true ) )
		{
			child.Tags.Add( PreviewTag );
		}
	}

	private static string GetSafeTextureName( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return "Pet";

		var characters = value.ToCharArray();
		for ( var i = 0; i < characters.Length; i++ )
		{
			if ( !char.IsLetterOrDigit( characters[i] ) )
			{
				characters[i] = '_';
			}
		}

		return new string( characters );
	}

	private sealed class PreviewEntry
	{
		public string PrefabPath { get; set; }
		public GameObject Prefab { get; set; }
		public GameObject Container { get; set; }
		public GameObject Pet { get; set; }
		public CameraComponent Camera { get; set; }
		public Texture Texture { get; set; }
		public int RenderFramesRemaining { get; set; }
	}
}
