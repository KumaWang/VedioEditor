namespace VedioEditor
{
    public interface IAccuracy
    {
        int Length { get; }

        float GetThreshold(int index);

        void SetThreshold(int index, float threshold);
    }
}
