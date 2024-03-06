namespace AnimLite
{


    public interface IStreamIndex
    {
        float IndexBlockTimeRange { get; }

        int GetKeyIndexInBlock(int istream, float time);
    }

}
