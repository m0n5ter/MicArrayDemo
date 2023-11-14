using System.Collections.Generic;

namespace TwoMicTest;

public readonly struct Range
{
    public float Min { get; }
    
    public float Max { get; }

    public float Average { get; }

    public float Amplitude => Max - Min;

    public Range(IList<float> samples)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        var sum = 0f;

        foreach (var sample in samples)
        {
            if (sample < min) min = sample;
            if(sample > max) max = sample;
            sum += sample;
        }

        Min = min;
        Max = max;
        
        Average = sum / samples.Count;
    }
}