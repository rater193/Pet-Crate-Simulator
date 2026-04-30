using Sandbox;

public sealed class PlayerData : Component
{
	public static PlayerData LOCALDATA;
	[Property, Sync] public int PlayerMoney { get; set; } = 0;

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			LOCALDATA = this;
			PlayerHud.SINGLETON.RenderedMoneyValue = PlayerMoney;
		}
	}
}
