using Sandbox;
using System;

public sealed class PlayerTradeController : Component
{
	public sealed class TradePetView
	{
		public int SlotIndex { get; set; }
		public string DisplayName { get; set; } = "Pet";
		public string PetPrefabPath { get; set; } = string.Empty;
		public PetRarity Rarity { get; set; } = PetRarity.Common;
		public float CoinMultiplier { get; set; } = 1f;
		public int Damage { get; set; } = 1;
		public Texture PreviewTexture { get; set; }
	}

	public static PlayerTradeController LOCAL { get; private set; }
	public static int UiVersion { get; private set; }

	public bool IsDetailsOpen { get; private set; }
	public GameObject DetailsTarget { get; private set; }

	public GameObject InviteFromPlayer { get; private set; }
	public float InviteExpiresAt { get; private set; }
	public bool IsInviteVisible => InviteFromPlayer.IsValid() && Time.Now < InviteExpiresAt;

	public bool IsTradeOpen { get; private set; }
	public bool IsReviewPhase { get; private set; }
	public bool OwnSubmitted { get; private set; }
	public bool PartnerSubmitted { get; private set; }
	public bool OwnAccepted { get; private set; }
	public bool PartnerAccepted { get; private set; }
	public float CountdownRemaining { get; private set; } = TradeCountdownSeconds;
	public GameObject TradePartner { get; private set; }
	public IReadOnlyList<int> SelectedSlotIndexes => selectedSlotIndexes;

	private const float InviteLifetimeSeconds = 5f;
	private const float TradeCountdownSeconds = 5f;

	private readonly List<int> selectedSlotIndexes = new();
	private string partnerInventoryJson = string.Empty;
	private string partnerOfferJson = string.Empty;
	private bool isCountdownRunning;
	private float countdownStartedAt;
	private bool isCompletingTrade;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		LOCAL = this;

		if ( InviteFromPlayer.IsValid() && Time.Now >= InviteExpiresAt )
		{
			ClearInvite();
		}

		if ( IsInviteVisible && Input.Pressed( "Reload" ) )
		{
			AcceptTradeInvite();
		}

		UpdateTradeCountdown();
	}

	public void OpenPlayerDetails( GameObject targetPlayer )
	{
		if ( !targetPlayer.IsValid() || targetPlayer == GameObject )
			return;

		DetailsTarget = targetPlayer;
		IsDetailsOpen = true;
		BumpUi();
	}

	public void ClosePlayerDetails()
	{
		IsDetailsOpen = false;
		DetailsTarget = null;
		BumpUi();
	}

	public void SendTradeInvite( GameObject targetPlayer )
	{
		if ( !targetPlayer.IsValid() || targetPlayer == GameObject )
			return;

		var targetTrade = targetPlayer.GetComponent<PlayerTradeController>();
		if ( !targetTrade.IsValid() )
			return;

		targetTrade.ReceiveTradeInviteOwner( GameObject );
		ClosePlayerDetails();
	}

	public void AcceptTradeInvite()
	{
		if ( !IsInviteVisible )
			return;

		var partner = InviteFromPlayer;
		ClearInvite();
		StartTradeSession( partner );
		partner.GetComponent<PlayerTradeController>()?.NotifyTradeAcceptedOwner( GameObject );
		SendTradeStateToPartner();
	}

	public void DeclineTradeInvite()
	{
		ClearInvite();
	}

	public void ToggleSelectedPet( int slotIndex )
	{
		if ( !IsTradeOpen || IsReviewPhase || OwnSubmitted || !GetComponent<Inventory>().IsValid() )
			return;

		if ( !GetComponent<Inventory>().GetPet( slotIndex ).IsValid() )
			return;

		if ( selectedSlotIndexes.Contains( slotIndex ) )
		{
			selectedSlotIndexes.Remove( slotIndex );
		}
		else
		{
			selectedSlotIndexes.Add( slotIndex );
		}

		selectedSlotIndexes.Sort();
		SendTradeStateToPartner();
		BumpUi();
	}

	public void ToggleSubmitted()
	{
		if ( !IsTradeOpen || selectedSlotIndexes.Count == 0 )
			return;

		OwnSubmitted = !OwnSubmitted;
		OwnAccepted = false;
		PartnerAccepted = false;
		CountdownRemaining = TradeCountdownSeconds;
		isCountdownRunning = false;

		if ( !OwnSubmitted || !PartnerSubmitted )
		{
			IsReviewPhase = false;
		}
		else
		{
			IsReviewPhase = true;
		}

		SendTradeStateToPartner();
		BumpUi();
	}

	public void ToggleAccepted()
	{
		if ( !IsTradeOpen || !IsReviewPhase )
			return;

		if ( !OwnAccepted && !CanReceivePartnerOffer() )
		{
			Log.Warning( "You do not have enough inventory space to accept this trade." );
			return;
		}

		OwnAccepted = !OwnAccepted;
		if ( !OwnAccepted )
		{
			CountdownRemaining = TradeCountdownSeconds;
			isCountdownRunning = false;
		}

		SendTradeStateToPartner();
		BumpUi();
	}

	public void CancelTrade()
	{
		if ( TradePartner.IsValid() )
		{
			TradePartner.GetComponent<PlayerTradeController>()?.ReceiveTradeCancelledOwner( GameObject );
		}

		ResetTradeState();
		BumpUi();
	}

	public IReadOnlyList<TradePetView> GetOwnPets()
	{
		var inventory = GetComponent<Inventory>();
		if ( !inventory.IsValid() )
			return Array.Empty<TradePetView>();

		return BuildPetViews( inventory.Slots, includeSlotIndex: true );
	}

	public IReadOnlyList<TradePetView> GetOwnSelectedPets()
	{
		var inventory = GetComponent<Inventory>();
		if ( !inventory.IsValid() )
			return Array.Empty<TradePetView>();

		var pets = new List<TradePetView>();
		foreach ( var slotIndex in selectedSlotIndexes )
		{
			var slot = inventory.GetPet( slotIndex );
			var pet = BuildPetView( slot, slotIndex );
			if ( pet != null )
			{
				pets.Add( pet );
			}
		}

		return pets;
	}

	public IReadOnlyList<TradePetView> GetPartnerPets()
	{
		return ReadPetViewsJson( partnerInventoryJson );
	}

	public IReadOnlyList<TradePetView> GetPartnerSelectedPets()
	{
		return ReadPetViewsJson( partnerOfferJson );
	}

	public bool IsSlotSelected( int slotIndex )
	{
		return selectedSlotIndexes.Contains( slotIndex );
	}

	public bool CanReceivePartnerOffer()
	{
		var inventory = GetComponent<Inventory>();
		if ( !inventory.IsValid() )
			return false;

		var incomingCount = GetPartnerSelectedPets().Count;
		return inventory.Count - selectedSlotIndexes.Count + incomingCount <= inventory.InventorySize;
	}

	[Rpc.Owner]
	public void ReceiveTradeInviteOwner( GameObject fromPlayer )
	{
		if ( !fromPlayer.IsValid() || IsTradeOpen )
			return;

		InviteFromPlayer = fromPlayer;
		InviteExpiresAt = Time.Now + InviteLifetimeSeconds;
		BumpUi();
	}

	[Rpc.Owner]
	public void NotifyTradeAcceptedOwner( GameObject partner )
	{
		StartTradeSession( partner );
		SendTradeStateToPartner();
	}

	[Rpc.Owner]
	public void ReceiveTradeStateOwner( GameObject partner, string inventoryJson, string offerJson, bool submitted, bool accepted, bool reviewPhase )
	{
		if ( !IsTradeOpen )
		{
			StartTradeSession( partner );
		}

		if ( partner != TradePartner )
			return;

		partnerInventoryJson = inventoryJson ?? string.Empty;
		partnerOfferJson = offerJson ?? string.Empty;
		PartnerSubmitted = submitted;
		PartnerAccepted = accepted;

		if ( !PartnerSubmitted || !OwnSubmitted )
		{
			IsReviewPhase = false;
			OwnAccepted = false;
			PartnerAccepted = false;
			CountdownRemaining = TradeCountdownSeconds;
			isCountdownRunning = false;
		}
		else if ( reviewPhase || OwnSubmitted )
		{
			IsReviewPhase = true;
		}

		if ( !PartnerAccepted || !OwnAccepted )
		{
			CountdownRemaining = TradeCountdownSeconds;
			isCountdownRunning = false;
		}

		BumpUi();
	}

	[Rpc.Owner]
	public void ReceiveTradeCancelledOwner( GameObject partner )
	{
		if ( partner != TradePartner )
			return;

		ResetTradeState();
		BumpUi();
	}

	private void StartTradeSession( GameObject partner )
	{
		if ( !partner.IsValid() || partner == GameObject )
			return;

		TradePartner = partner;
		IsTradeOpen = true;
		IsReviewPhase = false;
		OwnSubmitted = false;
		PartnerSubmitted = false;
		OwnAccepted = false;
		PartnerAccepted = false;
		CountdownRemaining = TradeCountdownSeconds;
		isCountdownRunning = false;
		isCompletingTrade = false;
		selectedSlotIndexes.Clear();
		partnerInventoryJson = string.Empty;
		partnerOfferJson = string.Empty;
		ClosePlayerDetails();
		BumpUi();
	}

	private void SendTradeStateToPartner()
	{
		if ( !TradePartner.IsValid() )
			return;

		var partnerTrade = TradePartner.GetComponent<PlayerTradeController>();
		if ( !partnerTrade.IsValid() )
			return;

		partnerTrade.ReceiveTradeStateOwner( GameObject, BuildInventoryJson(), BuildOfferJson(), OwnSubmitted, OwnAccepted, IsReviewPhase );
	}

	private void UpdateTradeCountdown()
	{
		if ( !IsTradeOpen || !IsReviewPhase || !OwnAccepted || !PartnerAccepted )
		{
			isCountdownRunning = false;
			CountdownRemaining = TradeCountdownSeconds;
			return;
		}

		if ( !isCountdownRunning )
		{
			isCountdownRunning = true;
			countdownStartedAt = Time.Now;
		}

		CountdownRemaining = MathF.Max( 0f, TradeCountdownSeconds - (Time.Now - countdownStartedAt) );
		if ( CountdownRemaining <= 0f )
		{
			CompleteTrade();
		}

		BumpUi();
	}

	private void CompleteTrade()
	{
		if ( isCompletingTrade )
			return;

		isCompletingTrade = true;

		if ( !CanReceivePartnerOffer() )
		{
			Log.Warning( "Trade cancelled because the inventory does not have enough room." );
			CancelTrade();
			return;
		}

		var inventory = GetComponent<Inventory>();
		if ( !inventory.IsValid() )
			return;

		var incomingPets = GetPartnerSelectedPets().ToList();
		foreach ( var slotIndex in selectedSlotIndexes.Distinct().OrderByDescending( index => index ) )
		{
			inventory.RemovePet( slotIndex );
		}

		foreach ( var incomingPet in incomingPets )
		{
			var petPrefab = GameObject.GetPrefab( incomingPet.PetPrefabPath );
			if ( petPrefab.IsValid() )
			{
				inventory.AddPetPrefab( petPrefab, incomingPet.DisplayName, incomingPet.Rarity );
			}
		}

		ResetTradeState();
		BumpUi();
	}

	private void ResetTradeState()
	{
		IsTradeOpen = false;
		IsReviewPhase = false;
		OwnSubmitted = false;
		PartnerSubmitted = false;
		OwnAccepted = false;
		PartnerAccepted = false;
		CountdownRemaining = TradeCountdownSeconds;
		TradePartner = null;
		selectedSlotIndexes.Clear();
		partnerInventoryJson = string.Empty;
		partnerOfferJson = string.Empty;
		isCountdownRunning = false;
		isCompletingTrade = false;
	}

	private void ClearInvite()
	{
		InviteFromPlayer = null;
		InviteExpiresAt = 0f;
		BumpUi();
	}

	private string BuildInventoryJson()
	{
		var inventory = GetComponent<Inventory>();
		if ( !inventory.IsValid() )
			return "{}";

		return WritePetViewsJson( BuildPetViews( inventory.Slots, includeSlotIndex: true ) );
	}

	private string BuildOfferJson()
	{
		return WritePetViewsJson( GetOwnSelectedPets() );
	}

	private static List<TradePetView> BuildPetViews( IReadOnlyList<InventoryPetSlot> slots, bool includeSlotIndex )
	{
		var pets = new List<TradePetView>();
		for ( var i = 0; i < slots.Count; i++ )
		{
			var pet = BuildPetView( slots[i], includeSlotIndex ? i : -1 );
			if ( pet != null )
			{
				pets.Add( pet );
			}
		}

		return pets;
	}

	private static TradePetView BuildPetView( InventoryPetSlot slot, int slotIndex )
	{
		if ( !slot.IsValid() || !slot.HasPet )
			return null;

		var petPrefab = slot.GetPetPrefab();
		var petComponent = petPrefab?.GetComponent<PetComponent>() ?? petPrefab?.GetComponentInChildren<PetComponent>();
		var rarityMultiplier = slot.RarityValueMultiplier;
		var coinMultiplier = MathF.Max( 0f, petComponent?.CoinMultiplier ?? 1f ) * rarityMultiplier;
		var damage = Math.Max( 0, (int)MathF.Round( (petComponent?.Damage ?? 1) * rarityMultiplier ) );

		return new TradePetView
		{
			SlotIndex = slotIndex,
			DisplayName = slot.DisplayName,
			PetPrefabPath = slot.PetPrefabPath,
			Rarity = slot.Rarity,
			CoinMultiplier = coinMultiplier,
			Damage = damage,
			PreviewTexture = PetPreviewRenderer.Instance?.GetPreviewTexture( slot.PetPrefabPath, petPrefab )
		};
	}

	private static string WritePetViewsJson( IEnumerable<TradePetView> pets )
	{
		var data = new JSONObject();
		var petData = new List<JSONObject>();

		foreach ( var pet in pets )
		{
			var petObject = new JSONObject();
			petObject.Set( "SlotIndex", pet.SlotIndex );
			petObject.Set( "DisplayName", pet.DisplayName ?? "Pet" );
			petObject.Set( "PetPrefabPath", pet.PetPrefabPath ?? string.Empty );
			petObject.Set( "Rarity", pet.Rarity.ToString() );
			petObject.Set( "CoinMultiplier", pet.CoinMultiplier );
			petObject.Set( "Damage", pet.Damage );
			petData.Add( petObject );
		}

		data.Set( "Pets", petData );
		return JSONObject.ToJson( data );
	}

	private static IReadOnlyList<TradePetView> ReadPetViewsJson( string json )
	{
		if ( string.IsNullOrWhiteSpace( json ) )
			return Array.Empty<TradePetView>();

		var data = JSONObject.FromJson( json );
		if ( data == null )
			return Array.Empty<TradePetView>();

		var pets = new List<TradePetView>();
		var petData = data.Get<List<object>>( "Pets", new() );
		foreach ( var petObject in petData )
		{
			if ( petObject is not JSONObject petJson )
				continue;

			var prefabPath = petJson.Get( "PetPrefabPath", string.Empty );
			var prefab = !string.IsNullOrWhiteSpace( prefabPath ) ? GameObject.GetPrefab( prefabPath ) : null;
			Enum.TryParse( petJson.Get( "Rarity", nameof( PetRarity.Common ) ), out PetRarity rarity );

			pets.Add( new TradePetView
			{
				SlotIndex = petJson.Get( "SlotIndex", -1 ),
				DisplayName = petJson.Get( "DisplayName", "Pet" ),
				PetPrefabPath = prefabPath,
				Rarity = rarity,
				CoinMultiplier = petJson.Get( "CoinMultiplier", 1f ),
				Damage = petJson.Get( "Damage", 1 ),
				PreviewTexture = PetPreviewRenderer.Instance?.GetPreviewTexture( prefabPath, prefab )
			} );
		}

		return pets;
	}

	private static void BumpUi()
	{
		UiVersion++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}
}
