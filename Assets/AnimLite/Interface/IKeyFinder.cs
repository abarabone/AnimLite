namespace AnimLite
{

    public interface IKeyFinderTimer
    {
        float IndexBlockTimeRange { get; }

        StreamingTimer Timer { get; set; }
    }

    public interface IKeyFinder<T> : IKeyFinderTimer
        where T : unmanaged
    {

        StreamData<T> Streams { get; }


        T get(int istream);

    }



    public interface IKeyFinderSemiTimer
    {
        float IndexBlockTimeRange { get; }
    }

    public interface IKeyFinderWithoutProcedure<T> : IKeyFinderSemiTimer
        where T : unmanaged
    {

        StreamData<T> Streams { get; }


        T get<TProcedure>(int istream, StreamingTimer timer, TProcedure procedure = default)
            where TProcedure : IStreamProcedure, new()
            ;
    }

}
