namespace OpenRA.Mods.Kknd.Traits.Technicians
{
	internal class RepairTask
	{
		public int Duration;
		public int Amount;

		public RepairTask(int amount, int duration)
		{
			Amount = amount;
			Duration = duration;
		}
	}
}
