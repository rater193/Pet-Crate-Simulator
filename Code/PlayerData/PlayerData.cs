using Sandbox;

public sealed class PlayerData : Component
{
	public static PlayerData LOCALDATA;
	[Property, Sync] public int PlayerMoney { get; set; } = 0;

	[Rpc.Owner]
	public void AddMoney( int amount )
	{
		if ( amount <= 0 )
			return;

		PlayerMoney += amount;
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			LOCALDATA = this;
			PlayerHud.SINGLETON.RenderedMoneyValue = PlayerMoney;
		}
	}
}
