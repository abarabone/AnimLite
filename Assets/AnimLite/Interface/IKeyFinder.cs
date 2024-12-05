namespace AnimLite
{



    public interface IKeyFinder<T>
        where T : unmanaged
    {

        StreamData<T> Streams { get; }

        float IndexBlockTimeRange { get; }


        StreamingTimer Timer { get; set; }


        T get(int istream);

    }

    public interface IKeyFinderWithoutProcedure<T>
        where T : unmanaged
    {

        StreamData<T> Streams { get; }

        float IndexBlockTimeRange { get; }


        T get<TProcedure>(int istream, StreamingTimer timer, TProcedure procedure = default)
            where TProcedure : IStreamProcedure, new()
            ;
    }

}
