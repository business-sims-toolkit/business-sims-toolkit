namespace LibCore
{
	public interface IFactory<T>
	{
		T Create ();
	}

	public interface IFactory<Parameters, Result>
	{
		Result Create (Parameters parameters);
	}
}